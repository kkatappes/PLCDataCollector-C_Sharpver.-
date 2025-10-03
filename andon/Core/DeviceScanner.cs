using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SlmpClient.Constants;
using SlmpClient.Exceptions;

namespace SlmpClient.Core
{
    /// <summary>
    /// デバイススキャナー
    /// 指定された範囲のデバイスを段階的にスキャンしてアクティブデバイスを検出
    /// </summary>
    public class DeviceScanner
    {
        private readonly ISlmpClientFull _slmpClient;
        private readonly ILogger<DeviceScanner> _logger;
        private readonly ActiveDeviceThreshold _activeThreshold;
        private readonly UnifiedLogWriter _unifiedLogWriter;

        // 詳細解析設定フィールド（SOLID原則: 単一責任原則）
        private readonly bool _enableDetailedFrameAnalysis;
        private readonly bool _enableDetailedDataAnalysis;
        private readonly bool _enableEnhancedHexDump;
        private readonly bool _hexDumpShowPrefix;
        private string? _currentOperationType; // データ型解析用

        public DeviceScanner(ISlmpClientFull slmpClient, ILogger<DeviceScanner> logger, UnifiedLogWriter unifiedLogWriter, ActiveDeviceThreshold? activeThreshold = null)
        {
            _slmpClient = slmpClient ?? throw new ArgumentNullException(nameof(slmpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unifiedLogWriter = unifiedLogWriter ?? throw new ArgumentNullException(nameof(unifiedLogWriter));
            _activeThreshold = activeThreshold ?? new ActiveDeviceThreshold();

            // デフォルト設定（後方互換性維持）
            _enableDetailedFrameAnalysis = false;
            _enableDetailedDataAnalysis = false;
            _enableEnhancedHexDump = true;
            _hexDumpShowPrefix = false;
        }

        // 新しいコンストラクタオーバーロード（SOLID原則: 開放/閉鎖原則、依存性逆転原則）
        public DeviceScanner(ISlmpClientFull slmpClient, ILogger<DeviceScanner> logger, UnifiedLogWriter unifiedLogWriter,
            Microsoft.Extensions.Configuration.IConfiguration configuration, ActiveDeviceThreshold? activeThreshold = null)
        {
            _slmpClient = slmpClient ?? throw new ArgumentNullException(nameof(slmpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unifiedLogWriter = unifiedLogWriter ?? throw new ArgumentNullException(nameof(unifiedLogWriter));
            _activeThreshold = activeThreshold ?? new ActiveDeviceThreshold();

            // 詳細解析設定を読み込み（依存性逆転原則: 抽象のIConfigurationに依存）
            var diagnosticSettings = configuration.GetSection("DiagnosticSettings");
            _enableDetailedFrameAnalysis = diagnosticSettings.GetValue<bool>("EnableDetailedFrameAnalysis", false);
            _enableDetailedDataAnalysis = diagnosticSettings.GetValue<bool>("EnableDetailedDataAnalysis", false);
            _enableEnhancedHexDump = diagnosticSettings.GetValue<bool>("EnableEnhancedHexDump", true);
            _hexDumpShowPrefix = diagnosticSettings.GetValue<bool>("HexDumpShowPrefix", true);
        }

        /// <summary>
        /// デバイス探索設定に基づいて全デバイスをスキャン
        /// </summary>
        /// <param name="configuration">探索設定</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>スキャン結果</returns>
        public async Task<List<DeviceScanResult>> ScanDevicesAsync(
            DeviceDiscoveryConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _logger.LogInformation("デバイススキャン開始: ビット{BitCount}種類, ワード{WordCount}種類",
                configuration.BitDevices.Length, configuration.WordDevices.Length);

            var results = new List<DeviceScanResult>();
            var semaphore = new SemaphoreSlim(configuration.MaxConcurrentScans, configuration.MaxConcurrentScans);
            var tasks = new List<Task<DeviceScanResult>>();

            try
            {
                // ビットデバイスのスキャンタスクを作成
                foreach (var deviceCode in configuration.BitDevices)
                {
                    if (configuration.ScanRanges.TryGetValue(deviceCode, out var range))
                    {
                        var task = ScanBitDeviceRangeWithSemaphore(deviceCode, range, configuration.BatchSize, semaphore, cancellationToken);
                        tasks.Add(task);
                    }
                }

                // ワードデバイスのスキャンタスクを作成
                foreach (var deviceCode in configuration.WordDevices)
                {
                    if (configuration.ScanRanges.TryGetValue(deviceCode, out var range))
                    {
                        var task = ScanWordDeviceRangeWithSemaphore(deviceCode, range, configuration.BatchSize, semaphore, cancellationToken);
                        tasks.Add(task);
                    }
                }

                // 全タスクの完了を待機
                var completedTasks = await Task.WhenAll(tasks);
                results.AddRange(completedTasks);

                // 優先度順にソート
                results.Sort((a, b) => b.ScannedRange.Priority.CompareTo(a.ScannedRange.Priority));

                _logger.LogInformation("デバイススキャン完了: {DeviceCount}種類, アクティブデバイス{ActiveCount}個",
                    results.Count, results.Sum(r => r.ActiveDevices.Count));

                return results;
            }
            finally
            {
                semaphore.Dispose();
            }
        }

        /// <summary>
        /// ビットデバイス範囲をスキャン（セマフォ制御付き）
        /// </summary>
        private async Task<DeviceScanResult> ScanBitDeviceRangeWithSemaphore(
            DeviceCode deviceCode,
            DeviceRange range,
            int batchSize,
            SemaphoreSlim semaphore,
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await ScanBitDeviceRangeAsync(deviceCode, range, batchSize, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// ワードデバイス範囲をスキャン（セマフォ制御付き）
        /// </summary>
        private async Task<DeviceScanResult> ScanWordDeviceRangeWithSemaphore(
            DeviceCode deviceCode,
            DeviceRange range,
            int batchSize,
            SemaphoreSlim semaphore,
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await ScanWordDeviceRangeAsync(deviceCode, range, batchSize, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// ビットデバイス範囲をスキャン
        /// </summary>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="range">スキャン範囲</param>
        /// <param name="batchSize">バッチサイズ</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>スキャン結果</returns>
        public async Task<DeviceScanResult> ScanBitDeviceRangeAsync(
            DeviceCode deviceCode,
            DeviceRange range,
            int batchSize,
            CancellationToken cancellationToken = default)
        {
            var result = new DeviceScanResult
            {
                DeviceCode = deviceCode,
                ScannedRange = range,
                CompletedAt = DateTime.Now
            };

            var startTime = DateTime.Now;
            var totalScans = 0;
            var successfulScans = 0;
            var failedScans = 0;

            _logger.LogDebug("ビットデバイススキャン開始: {DeviceCode} {Range}", deviceCode, range);

            try
            {
                var currentAddress = range.Start;
                var maxBatchSize = Math.Min(batchSize, 7168); // SLMP制限

                while (currentAddress <= range.End)
                {
                    var remainingCount = (int)(range.End - currentAddress + 1);
                    var currentBatchSize = Math.Min(maxBatchSize, remainingCount);

                    totalScans++;
                    var scanStartTime = DateTime.Now;

                    try
                    {
                        var data = await _slmpClient.ReadBitDevicesAsync(
                            deviceCode, currentAddress, (ushort)currentBatchSize, 0, cancellationToken);

                        var scanTime = (DateTime.Now - scanStartTime).TotalMilliseconds;
                        result.Statistics.TotalScanTimeMs += scanTime;

                        // アクティブデバイスを検出
                        var activeDevices = FindActiveBitDevices(data, currentAddress, _activeThreshold.BitDevice);
                        result.ActiveDevices.AddRange(activeDevices);

                        // 個別デバイス値を保存（新機能）
                        var deviceValues = SaveAllBitDeviceValues(data, currentAddress, deviceCode, _activeThreshold.BitDevice);
                        result.DeviceValues.AddRange(deviceValues);

                        // 個別デバイス値の詳細ログ出力
                        LogBitDeviceValues(deviceValues, deviceCode, currentAddress, currentBatchSize);

                        // 通信詳細記録
                        var sessionId = $"session_{DateTime.Now:yyyyMMdd_HHmmss}";
                        var communicationInfo = new CommunicationInfo
                        {
                            SessionId = sessionId,
                            CycleNumber = 4,
                            PhaseInfo = new PhaseInfo
                            {
                                Phase = "BitDeviceRead",
                                StatusMessage = $"{deviceCode}{currentAddress}~{currentAddress + currentBatchSize - 1} スキャン中...",
                                DeviceAddress = $"{deviceCode}{currentAddress}"
                            },
                            CommunicationDetails = new CommunicationDetails
                            {
                                OperationType = "BitDeviceRead",
                                DeviceCode = deviceCode.ToString(),
                                DeviceNumber = currentAddress,
                                DeviceAddress = $"{deviceCode}{currentAddress}",
                                Values = data.Cast<object>().ToArray(),
                                ResponseTimeMs = scanTime,
                                Success = true,
                                DeviceValues = deviceValues.ToArray(),
                                BatchReadEfficiency = $"1通信で{currentBatchSize}デバイス取得"
                            }
                        };

                        // 生データ解析（SlmpClientから取得）
                        var rawDataAnalysis = new RawDataAnalysis();
                        if (_slmpClient is SlmpClient slmpClient)
                        {
                            rawDataAnalysis.RequestFrameHex = slmpClient.LastSentFrame != null ? Convert.ToHexString(slmpClient.LastSentFrame) : "";
                            rawDataAnalysis.ResponseFrameHex = slmpClient.LastReceivedFrame != null ? Convert.ToHexString(slmpClient.LastReceivedFrame) : "";

                            // 強化された16進ダンプ（プレフィックス対応）
                            rawDataAnalysis.RequestHexDump = slmpClient.LastSentFrame != null ? GenerateHexDump(slmpClient.LastSentFrame, "REQ") : "";
                            rawDataAnalysis.HexDump = slmpClient.LastReceivedFrame != null ? GenerateHexDump(slmpClient.LastReceivedFrame, "RES") : "";

                            // データ型別詳細解析（統合機能）
                            if (slmpClient.LastReceivedFrame != null && slmpClient.LastReceivedFrame.Length > 11)
                            {
                                var dataBytes = slmpClient.LastReceivedFrame.Skip(11).ToArray(); // データ部分のみ抽出
                                _currentOperationType = "bitdeviceread"; // ビットデバイススキャンの場合
                                rawDataAnalysis.DetailedDataAnalysis = AnalyzeDataByType(dataBytes, _currentOperationType, _logger);
                            }

                            // 動的SLMPフレーム解析（統合機能）
                            rawDataAnalysis.FrameAnalysis = AnalyzeSlmpFrameStructure(slmpClient.LastReceivedFrame);
                        }

                        await _unifiedLogWriter.WriteCommunicationAsync(communicationInfo, rawDataAnalysis);

                        successfulScans++;

                        _logger.LogTrace("ビットデバイススキャン成功: {DeviceCode}:{Address}-{EndAddress} ({Count}個, アクティブ{ActiveCount}個)",
                            deviceCode, currentAddress, currentAddress + currentBatchSize - 1, currentBatchSize, activeDevices.Count);
                    }
                    catch (SlmpException ex)
                    {
                        failedScans++;
                        _logger.LogWarning("ビットデバイススキャンエラー: {DeviceCode}:{Address}-{EndAddress} - {Error}",
                            deviceCode, currentAddress, currentAddress + currentBatchSize - 1, ex.Message);
                    }
                    catch (Exception ex)
                    {
                        failedScans++;
                        _logger.LogError(ex, "ビットデバイススキャン予期しないエラー: {DeviceCode}:{Address}", deviceCode, currentAddress);
                    }

                    currentAddress += (uint)currentBatchSize;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ビットデバイス範囲スキャンでエラー: {DeviceCode} {Range}", deviceCode, range);
            }

            // 統計情報を更新
            result.Statistics.SuccessfulScans = successfulScans;
            result.Statistics.FailedScans = failedScans;

            var totalTime = (DateTime.Now - startTime).TotalMilliseconds;
            _logger.LogDebug("ビットデバイススキャン完了: {DeviceCode} - 成功{Success}/{Total}, アクティブ{Active}個, 時間{Time:F1}ms",
                deviceCode, successfulScans, totalScans, result.ActiveDevices.Count, totalTime);

            return result;
        }

        /// <summary>
        /// ワードデバイス範囲をスキャン
        /// </summary>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="range">スキャン範囲</param>
        /// <param name="batchSize">バッチサイズ</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>スキャン結果</returns>
        public async Task<DeviceScanResult> ScanWordDeviceRangeAsync(
            DeviceCode deviceCode,
            DeviceRange range,
            int batchSize,
            CancellationToken cancellationToken = default)
        {
            var result = new DeviceScanResult
            {
                DeviceCode = deviceCode,
                ScannedRange = range,
                CompletedAt = DateTime.Now
            };

            var startTime = DateTime.Now;
            var totalScans = 0;
            var successfulScans = 0;
            var failedScans = 0;

            _logger.LogDebug("ワードデバイススキャン開始: {DeviceCode} {Range}", deviceCode, range);

            try
            {
                var currentAddress = range.Start;
                var maxBatchSize = Math.Min(batchSize, 960); // SLMP制限

                while (currentAddress <= range.End)
                {
                    var remainingCount = (int)(range.End - currentAddress + 1);
                    var currentBatchSize = Math.Min(maxBatchSize, remainingCount);

                    totalScans++;
                    var scanStartTime = DateTime.Now;

                    try
                    {
                        var data = await _slmpClient.ReadWordDevicesAsync(
                            deviceCode, currentAddress, (ushort)currentBatchSize, 0, cancellationToken);

                        var scanTime = (DateTime.Now - scanStartTime).TotalMilliseconds;
                        result.Statistics.TotalScanTimeMs += scanTime;

                        // アクティブデバイスを検出
                        var activeDevices = FindActiveWordDevices(data, currentAddress, _activeThreshold.WordDevice);
                        result.ActiveDevices.AddRange(activeDevices);

                        // 個別デバイス値を保存（新機能）
                        var deviceValues = SaveAllWordDeviceValues(data, currentAddress, deviceCode, _activeThreshold.WordDevice);
                        result.DeviceValues.AddRange(deviceValues);

                        // 個別デバイス値の詳細ログ出力
                        LogWordDeviceValues(deviceValues, deviceCode, currentAddress, currentBatchSize);

                        // 通信詳細記録
                        var sessionId = $"session_{DateTime.Now:yyyyMMdd_HHmmss}";
                        var communicationInfo = new CommunicationInfo
                        {
                            SessionId = sessionId,
                            CycleNumber = 4,
                            PhaseInfo = new PhaseInfo
                            {
                                Phase = "WordDeviceRead",
                                StatusMessage = $"{deviceCode}{currentAddress}~{currentAddress + currentBatchSize - 1} スキャン中...",
                                DeviceAddress = $"{deviceCode}{currentAddress}"
                            },
                            CommunicationDetails = new CommunicationDetails
                            {
                                OperationType = "WordDeviceRead",
                                DeviceCode = deviceCode.ToString(),
                                DeviceNumber = currentAddress,
                                DeviceAddress = $"{deviceCode}{currentAddress}",
                                Values = data.Cast<object>().ToArray(),
                                ResponseTimeMs = scanTime,
                                Success = true,
                                DeviceValues = deviceValues.ToArray(),
                                BatchReadEfficiency = $"1通信で{currentBatchSize}デバイス取得"
                            }
                        };

                        // 生データ解析（SlmpClientから取得）
                        var rawDataAnalysis = new RawDataAnalysis();
                        if (_slmpClient is SlmpClient slmpClient)
                        {
                            rawDataAnalysis.RequestFrameHex = slmpClient.LastSentFrame != null ? Convert.ToHexString(slmpClient.LastSentFrame) : "";
                            rawDataAnalysis.ResponseFrameHex = slmpClient.LastReceivedFrame != null ? Convert.ToHexString(slmpClient.LastReceivedFrame) : "";

                            // 強化された16進ダンプ（プレフィックス対応）
                            rawDataAnalysis.RequestHexDump = slmpClient.LastSentFrame != null ? GenerateHexDump(slmpClient.LastSentFrame, "REQ") : "";
                            rawDataAnalysis.HexDump = slmpClient.LastReceivedFrame != null ? GenerateHexDump(slmpClient.LastReceivedFrame, "RES") : "";

                            // データ型別詳細解析（統合機能）
                            if (slmpClient.LastReceivedFrame != null && slmpClient.LastReceivedFrame.Length > 11)
                            {
                                var dataBytes = slmpClient.LastReceivedFrame.Skip(11).ToArray(); // データ部分のみ抽出
                                _currentOperationType = "bitdeviceread"; // ビットデバイススキャンの場合
                                rawDataAnalysis.DetailedDataAnalysis = AnalyzeDataByType(dataBytes, _currentOperationType, _logger);
                            }

                            // 動的SLMPフレーム解析（統合機能）
                            rawDataAnalysis.FrameAnalysis = AnalyzeSlmpFrameStructure(slmpClient.LastReceivedFrame);
                        }

                        await _unifiedLogWriter.WriteCommunicationAsync(communicationInfo, rawDataAnalysis);

                        successfulScans++;

                        _logger.LogTrace("ワードデバイススキャン成功: {DeviceCode}:{Address}-{EndAddress} ({Count}個, アクティブ{ActiveCount}個)",
                            deviceCode, currentAddress, currentAddress + currentBatchSize - 1, currentBatchSize, activeDevices.Count);
                    }
                    catch (SlmpException ex)
                    {
                        failedScans++;
                        _logger.LogWarning("ワードデバイススキャンエラー: {DeviceCode}:{Address}-{EndAddress} - {Error}",
                            deviceCode, currentAddress, currentAddress + currentBatchSize - 1, ex.Message);
                    }
                    catch (Exception ex)
                    {
                        failedScans++;
                        _logger.LogError(ex, "ワードデバイススキャン予期しないエラー: {DeviceCode}:{Address}", deviceCode, currentAddress);
                    }

                    currentAddress += (uint)currentBatchSize;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ワードデバイス範囲スキャンでエラー: {DeviceCode} {Range}", deviceCode, range);
            }

            // 統計情報を更新
            result.Statistics.SuccessfulScans = successfulScans;
            result.Statistics.FailedScans = failedScans;

            var totalTime = (DateTime.Now - startTime).TotalMilliseconds;
            _logger.LogDebug("ワードデバイススキャン完了: {DeviceCode} - 成功{Success}/{Total}, アクティブ{Active}個, 時間{Time:F1}ms",
                deviceCode, successfulScans, totalScans, result.ActiveDevices.Count, totalTime);

            return result;
        }

        /// <summary>
        /// 単一デバイスのクイックテスト
        /// 指定されたデバイスが応答するかどうかを高速チェック
        /// </summary>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="address">アドレス</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>応答があればtrue</returns>
        public async Task<bool> QuickTestDeviceAsync(
            DeviceCode deviceCode,
            uint address,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (IsWordDevice(deviceCode))
                {
                    var data = await _slmpClient.ReadWordDevicesAsync(deviceCode, address, 1, 0, cancellationToken);
                    return data.Length > 0; // データが取得できれば応答あり
                }
                else
                {
                    var data = await _slmpClient.ReadBitDevicesAsync(deviceCode, address, 1, 0, cancellationToken);
                    return data.Length > 0; // データが取得できれば応答あり
                }
            }
            catch (SlmpException)
            {
                return false; // SLMPエラーは応答なし
            }
            catch (Exception ex)
            {
                _logger.LogTrace("デバイスクイックテストでエラー: {DeviceCode}:{Address} - {Error}", deviceCode, address, ex.Message);
                return false; // その他のエラーも応答なし
            }
        }

        /// <summary>
        /// アクティブビットデバイスを検出
        /// </summary>
        /// <param name="data">読み取りデータ</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="threshold">判定基準</param>
        /// <returns>アクティブデバイスのアドレス一覧</returns>
        private List<uint> FindActiveBitDevices(bool[] data, uint startAddress, BitDeviceThreshold threshold)
        {
            var activeDevices = new List<uint>();

            switch (threshold)
            {
                case BitDeviceThreshold.AnyTrue:
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (data[i])
                        {
                            activeDevices.Add(startAddress + (uint)i);
                        }
                    }
                    break;

                case BitDeviceThreshold.AllTrue:
                    // 全てがTrueの場合のみアクティブ（実用性は低い）
                    if (data.All(b => b))
                    {
                        for (int i = 0; i < data.Length; i++)
                        {
                            activeDevices.Add(startAddress + (uint)i);
                        }
                    }
                    break;

                case BitDeviceThreshold.MajorityTrue:
                    // 過半数がTrueの場合にアクティブ
                    var trueCount = data.Count(b => b);
                    if (trueCount > data.Length / 2)
                    {
                        for (int i = 0; i < data.Length; i++)
                        {
                            if (data[i])
                            {
                                activeDevices.Add(startAddress + (uint)i);
                            }
                        }
                    }
                    break;
            }

            return activeDevices;
        }

        /// <summary>
        /// ビットデバイスの全値を保存（個別デバイス値確認機能）
        /// </summary>
        /// <param name="data">読み取りデータ</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="deviceCode">デバイス種類</param>
        /// <param name="threshold">判定基準</param>
        /// <returns>全デバイスの値情報</returns>
        private List<DeviceValueInfo> SaveAllBitDeviceValues(bool[] data, uint startAddress, DeviceCode deviceCode, BitDeviceThreshold threshold)
        {
            var deviceValues = new List<DeviceValueInfo>();
            var readTime = DateTime.Now;

            for (int i = 0; i < data.Length; i++)
            {
                var address = startAddress + (uint)i;
                var value = data[i];
                var isActive = DetermineIfBitDeviceIsActive(data, i, threshold);

                deviceValues.Add(new DeviceValueInfo
                {
                    Address = address,
                    Value = value,
                    DeviceCode = deviceCode,
                    DeviceName = $"{deviceCode}{address}",
                    ReadAt = readTime,
                    ValueType = DeviceValueType.Bit,
                    IsActive = isActive
                });
            }

            return deviceValues;
        }

        /// <summary>
        /// ビットデバイスがアクティブかどうかを判定
        /// </summary>
        /// <param name="data">全データ</param>
        /// <param name="index">対象インデックス</param>
        /// <param name="threshold">判定基準</param>
        /// <returns>アクティブかどうか</returns>
        private bool DetermineIfBitDeviceIsActive(bool[] data, int index, BitDeviceThreshold threshold)
        {
            switch (threshold)
            {
                case BitDeviceThreshold.AnyTrue:
                    return data[index];

                case BitDeviceThreshold.AllTrue:
                    return data.All(b => b) && data[index];

                case BitDeviceThreshold.MajorityTrue:
                    var trueCount = data.Count(b => b);
                    return trueCount > data.Length / 2 && data[index];

                default:
                    return data[index];
            }
        }

        /// <summary>
        /// アクティブワードデバイスを検出
        /// </summary>
        /// <param name="data">読み取りデータ</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="threshold">判定基準</param>
        /// <returns>アクティブデバイスのアドレス一覧</returns>
        private List<uint> FindActiveWordDevices(ushort[] data, uint startAddress, WordDeviceThreshold threshold)
        {
            var activeDevices = new List<uint>();

            switch (threshold)
            {
                case WordDeviceThreshold.NonZero:
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (data[i] != 0)
                        {
                            activeDevices.Add(startAddress + (uint)i);
                        }
                    }
                    break;

                case WordDeviceThreshold.AboveThreshold:
                    // 実装時に閾値を設定可能にする（現在は1000以上）
                    const ushort thresholdValue = 1000;
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (data[i] >= thresholdValue)
                        {
                            activeDevices.Add(startAddress + (uint)i);
                        }
                    }
                    break;

                case WordDeviceThreshold.HasChanged:
                    // 変化検出は過去値との比較が必要（現在は非ゼロで代用）
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (data[i] != 0)
                        {
                            activeDevices.Add(startAddress + (uint)i);
                        }
                    }
                    break;
            }

            return activeDevices;
        }

        /// <summary>
        /// ワードデバイスの全値を保存（個別デバイス値確認機能）
        /// </summary>
        /// <param name="data">読み取りデータ</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="deviceCode">デバイス種類</param>
        /// <param name="threshold">判定基準</param>
        /// <returns>全デバイスの値情報</returns>
        private List<DeviceValueInfo> SaveAllWordDeviceValues(ushort[] data, uint startAddress, DeviceCode deviceCode, WordDeviceThreshold threshold)
        {
            var deviceValues = new List<DeviceValueInfo>();
            var readTime = DateTime.Now;

            for (int i = 0; i < data.Length; i++)
            {
                var address = startAddress + (uint)i;
                var value = data[i];
                var isActive = DetermineIfWordDeviceIsActive(data, i, threshold);

                deviceValues.Add(new DeviceValueInfo
                {
                    Address = address,
                    Value = value,
                    DeviceCode = deviceCode,
                    DeviceName = $"{deviceCode}{address}",
                    ReadAt = readTime,
                    ValueType = DeviceValueType.Word,
                    IsActive = isActive
                });
            }

            return deviceValues;
        }

        /// <summary>
        /// ワードデバイスがアクティブかどうかを判定
        /// </summary>
        /// <param name="data">全データ</param>
        /// <param name="index">対象インデックス</param>
        /// <param name="threshold">判定基準</param>
        /// <returns>アクティブかどうか</returns>
        private bool DetermineIfWordDeviceIsActive(ushort[] data, int index, WordDeviceThreshold threshold)
        {
            switch (threshold)
            {
                case WordDeviceThreshold.NonZero:
                    return data[index] != 0;

                case WordDeviceThreshold.AboveThreshold:
                    const ushort thresholdValue = 1000;
                    return data[index] >= thresholdValue;

                case WordDeviceThreshold.HasChanged:
                    // 変化検出は過去値との比較が必要（現在は非ゼロで代用）
                    return data[index] != 0;

                default:
                    return data[index] != 0;
            }
        }

        /// <summary>
        /// デバイスコードがワードデバイスかどうかを判定
        /// </summary>
        /// <param name="deviceCode">デバイスコード</param>
        /// <returns>ワードデバイスの場合はtrue</returns>
        private static bool IsWordDevice(DeviceCode deviceCode)
        {
            return deviceCode switch
            {
                DeviceCode.D => true,
                DeviceCode.W => true,
                DeviceCode.R => true,
                DeviceCode.ZR => true,
                DeviceCode.TN => true,
                DeviceCode.CN => true,
                DeviceCode.SW => true,
                DeviceCode.SD => true,
                _ => false
            };
        }

        /// <summary>
        /// ビットデバイス値の詳細ログ出力
        /// </summary>
        /// <param name="deviceValues">デバイス値情報</param>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="batchSize">バッチサイズ</param>
        private void LogBitDeviceValues(List<DeviceValueInfo> deviceValues, DeviceCode deviceCode, uint startAddress, int batchSize)
        {
            _logger.LogInformation("📡 {DeviceCode}{StartAddress}-{EndAddress} スキャン結果:",
                deviceCode, startAddress, startAddress + batchSize - 1);

            var activeCount = 0;
            foreach (var device in deviceValues)
            {
                var status = device.IsActive ? "✅" : "⭕";
                var valueText = device.IsActive ? "true (ON状態)" : "false (OFF)";
                if (device.IsActive) valueText += " ← アクティブ";

                _logger.LogInformation("  {Status} {DeviceName} = {Value}",
                    status, device.DeviceName, valueText);

                if (device.IsActive) activeCount++;
            }

            _logger.LogInformation("  📊 バッチ読み取り: 1回の通信で{BatchSize}個取得", batchSize);
            _logger.LogInformation("  📊 アクティブ: {ActiveCount}個 / {TotalCount}個スキャン",
                activeCount, deviceValues.Count);
        }

        /// <summary>
        /// ワードデバイス値の詳細ログ出力
        /// </summary>
        /// <param name="deviceValues">デバイス値情報</param>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="batchSize">バッチサイズ</param>
        private void LogWordDeviceValues(List<DeviceValueInfo> deviceValues, DeviceCode deviceCode, uint startAddress, int batchSize)
        {
            _logger.LogInformation("📡 {DeviceCode}{StartAddress}-{EndAddress} スキャン結果:",
                deviceCode, startAddress, startAddress + batchSize - 1);

            var activeCount = 0;
            foreach (var device in deviceValues)
            {
                var status = device.IsActive ? "✅" : "⭕";
                var valueText = device.Value.ToString();
                if (device.IsActive) valueText += " ← アクティブ";

                _logger.LogInformation("  {Status} {DeviceName} = {Value}",
                    status, device.DeviceName, valueText);

                if (device.IsActive) activeCount++;
            }

            _logger.LogInformation("  📊 バッチ読み取り: 1回の通信で{BatchSize}個取得", batchSize);
            _logger.LogInformation("  📊 アクティブ: {ActiveCount}個 / {TotalCount}個スキャン",
                activeCount, deviceValues.Count);
        }

        /// <summary>
        /// 16進数ダンプを生成（後方互換性維持）
        /// </summary>
        /// <param name="data">バイナリデータ</param>
        /// <returns>16進数ダンプ文字列</returns>
        private string GenerateHexDump(byte[] data)
        {
            return GenerateHexDump(data, "");
        }

        /// <summary>
        /// 16進数ダンプを生成（プレフィックス対応）
        /// SOLID原則: 開放/閉鎖原則に従った機能拡張
        /// </summary>
        /// <param name="data">バイナリデータ</param>
        /// <param name="prefix">プレフィックス（"REQ", "RES"等）</param>
        /// <returns>16進数ダンプ文字列</returns>
        private string GenerateHexDump(byte[] data, string prefix)
        {
            if (data == null || data.Length == 0)
                return string.Empty;

            const int bytesPerLine = 16;
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < data.Length; i += bytesPerLine)
            {
                // アドレス部分（プレフィックス対応）
                if (_enableEnhancedHexDump && _hexDumpShowPrefix && !string.IsNullOrEmpty(prefix))
                {
                    sb.AppendFormat("   {0}{1:X8}: ", prefix.PadRight(4), i);
                }
                else
                {
                    sb.AppendFormat("{0:X8}: ", i);
                }

                // 16進数部分
                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (i + j < data.Length)
                    {
                        sb.AppendFormat("{0:X2} ", data[i + j]);
                    }
                    else
                    {
                        sb.Append("   ");
                    }

                    // 8バイトごとに区切り（既存機能を維持）
                    if (j == 7) sb.Append(" ");
                }

                sb.Append(" |");

                // ASCII部分
                for (int j = 0; j < bytesPerLine && i + j < data.Length; j++)
                {
                    byte b = data[i + j];
                    sb.Append(b >= 32 && b <= 126 ? (char)b : '.');
                }

                sb.AppendLine("|");
            }

            return sb.ToString();
        }

        /// <summary>
        /// データ型別詳細解析を実行
        /// SOLID原則: 単一責任原則 - 各解析タイプを個別の責任として分離
        /// </summary>
        /// <param name="dataBytes">解析するデータ</param>
        /// <param name="operationType">操作タイプ</param>
        /// <param name="logger">ロガー</param>
        /// <returns>解析結果文字列</returns>
        private string AnalyzeDataByType(byte[] dataBytes, string operationType, ILogger logger)
        {
            if (!_enableDetailedDataAnalysis || dataBytes == null || dataBytes.Length == 0)
                return string.Empty;

            var analysisResults = new System.Text.StringBuilder();

            switch (operationType.ToLowerInvariant())
            {
                case "worddeviceread":
                    analysisResults.Append(AnalyzeWordDeviceData(dataBytes, logger));
                    break;
                case "bitdeviceread":
                    analysisResults.Append(AnalyzeBitDeviceData(dataBytes, logger));
                    break;
                case "mixeddeviceread":
                    analysisResults.Append(AnalyzeMixedDeviceData(dataBytes, logger));
                    break;
                default:
                    analysisResults.Append(AnalyzeGenericData(dataBytes, logger));
                    break;
            }

            return analysisResults.ToString();
        }

        /// <summary>
        /// ワードデバイスデータ解析
        /// SOLID原則: 単一責任原則 - ワードデータ解析のみ担当
        /// </summary>
        /// <param name="dataBytes">ワードデータ</param>
        /// <param name="logger">ロガー</param>
        /// <returns>解析結果</returns>
        private string AnalyzeWordDeviceData(byte[] dataBytes, ILogger logger)
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine("     📊 ワードデバイスデータ:");

            logger.LogInformation("     📊 ワードデバイスデータ:");
            for (int i = 0; i < dataBytes.Length; i += 2)
            {
                if (i + 1 < dataBytes.Length)
                {
                    var value = BitConverter.ToUInt16(dataBytes, i);
                    var logMessage = $"       Word[{i / 2}]: 0x{value:X4} ({value}) = {Convert.ToString(value, 2).PadLeft(16, '0')}";
                    logger.LogInformation(logMessage);
                    result.AppendLine(logMessage);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// ビットデバイスデータ解析
        /// SOLID原則: 単一責任原則 - ビットデータ解析のみ担当
        /// </summary>
        /// <param name="dataBytes">ビットデータ</param>
        /// <param name="logger">ロガー</param>
        /// <returns>解析結果</returns>
        private string AnalyzeBitDeviceData(byte[] dataBytes, ILogger logger)
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine("     🔢 ビットデバイスデータ:");

            logger.LogInformation("     🔢 ビットデバイスデータ:");
            for (int i = 0; i < dataBytes.Length; i++)
            {
                var bits = Convert.ToString(dataBytes[i], 2).PadLeft(8, '0');
                var logMessage = $"       Byte[{i}]: 0x{dataBytes[i]:X2} = {dataBytes[i]} (bits: {bits})";
                logger.LogInformation(logMessage);
                result.AppendLine(logMessage);
            }

            return result.ToString();
        }

        /// <summary>
        /// 混合デバイスデータ解析
        /// SOLID原則: 単一責任原則 - 混合データ解析のみ担当
        /// </summary>
        /// <param name="dataBytes">混合データ</param>
        /// <param name="logger">ロガー</param>
        /// <returns>解析結果</returns>
        private string AnalyzeMixedDeviceData(byte[] dataBytes, ILogger logger)
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine("     🔀 混合デバイスデータ (詳細解析には追加情報が必要):");

            logger.LogInformation("     🔀 混合デバイスデータ (詳細解析には追加情報が必要):");
            result.Append(AnalyzeGenericData(dataBytes, logger));

            return result.ToString();
        }

        /// <summary>
        /// 汎用データ解析
        /// SOLID原則: 単一責任原則 - 汎用データ解析のみ担当
        /// </summary>
        /// <param name="dataBytes">汎用データ</param>
        /// <param name="logger">ロガー</param>
        /// <returns>解析結果</returns>
        private string AnalyzeGenericData(byte[] dataBytes, ILogger logger)
        {
            var result = new System.Text.StringBuilder();
            var maxDisplay = Math.Min(dataBytes.Length, 32); // 最初の32バイトまで表示

            for (int i = 0; i < maxDisplay; i += 4)
            {
                var segment = dataBytes.Skip(i).Take(4).ToArray();
                var hex = string.Join(" ", segment.Select(b => $"{b:X2}"));
                var ascii = string.Join("", segment.Select(b => b >= 32 && b <= 126 ? (char)b : '.'));
                var logMessage = $"       [{i:X4}]: {hex,-11} |{ascii}|";
                logger.LogInformation(logMessage);
                result.AppendLine(logMessage);
            }

            if (dataBytes.Length > maxDisplay)
            {
                var remainingMessage = $"       ... (残り{dataBytes.Length - maxDisplay}バイト)";
                logger.LogInformation(remainingMessage);
                result.AppendLine(remainingMessage);
            }

            return result.ToString();
        }

        /// <summary>
        /// SLMPフレーム構造解析
        /// SOLID原則: 単一責任原則 - SLMPフレーム解析のみ担当
        /// </summary>
        /// <param name="frameData">フレームデータ</param>
        /// <returns>解析結果</returns>
        private FrameAnalysis AnalyzeSlmpFrameStructure(byte[]? frameData)
        {
            if (frameData == null || frameData.Length < 11)
            {
                return new FrameAnalysis
                {
                    SubHeader = "不明",
                    SubHeaderDescription = "フレームデータ不足",
                    EndCode = "不明",
                    EndCodeDescription = "解析不可"
                };
            }

            try
            {
                // サブヘッダー解析
                var subHeader = BitConverter.ToUInt16(frameData, 0);
                var subHeaderDesc = subHeader switch
                {
                    0x5000 => "3Eフレーム",
                    0x5400 => "4Eフレーム",
                    _ => "不明フレーム"
                };

                // 終了コード解析（既存のEndCode.csを活用）
                var endCode = BitConverter.ToUInt16(frameData, 9);
                var endCodeEnum = (EndCode)endCode;
                var endCodeDesc = endCodeEnum.GetJapaneseMessage();

                // 詳細ログ出力（設定により制御）
                if (_enableDetailedFrameAnalysis)
                {
                    LogDetailedFrameAnalysis(frameData);
                }

                return new FrameAnalysis
                {
                    SubHeader = $"0x{subHeader:X4}",
                    SubHeaderDescription = subHeaderDesc,
                    EndCode = $"0x{endCode:X4}",
                    EndCodeDescription = endCodeDesc
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SLMPフレーム解析中にエラーが発生しました");
                return new FrameAnalysis
                {
                    SubHeader = "エラー",
                    SubHeaderDescription = "解析エラー",
                    EndCode = "エラー",
                    EndCodeDescription = ex.Message
                };
            }
        }

        /// <summary>
        /// 詳細フレーム解析ログ出力
        /// SOLID原則: 単一責任原則 - 詳細ログ出力のみ担当
        /// </summary>
        /// <param name="frameData">フレームデータ</param>
        private void LogDetailedFrameAnalysis(byte[] frameData)
        {
            _logger.LogInformation("🔍 SLMPフレーム詳細解析:");

            // サブヘッダー
            var subHeader = BitConverter.ToUInt16(frameData, 0);
            _logger.LogInformation("   サブヘッダー: 0x{0:X4} ({1})", subHeader,
                subHeader == 0x5000 ? "3Eフレーム" : subHeader == 0x5400 ? "4Eフレーム" : "不明");

            // ネットワーク番号
            _logger.LogInformation("   ネットワーク番号: 0x{0:X2} ({0})", frameData[2]);

            // PC番号
            _logger.LogInformation("   PC番号: 0x{0:X2} ({0})", frameData[3]);

            // 要求先ユニットI/O番号
            var unitIO = BitConverter.ToUInt16(frameData, 4);
            _logger.LogInformation("   要求先ユニットI/O番号: 0x{0:X4} ({0})", unitIO);

            // 要求先ユニット局番号
            _logger.LogInformation("   要求先ユニット局番号: 0x{0:X2} ({0})", frameData[6]);

            // 応答データ長
            var dataLength = BitConverter.ToUInt16(frameData, 7);
            _logger.LogInformation("   応答データ長: 0x{0:X4} ({0} bytes)", dataLength);

            // 終了コード（EndCode.csを活用）
            var endCode = BitConverter.ToUInt16(frameData, 9);
            var endCodeEnum = (EndCode)endCode;
            _logger.LogInformation("   終了コード: 0x{0:X4} ({1})", endCode, endCodeEnum.GetJapaneseMessage());

            // データ部の存在確認
            if (frameData.Length > 11)
            {
                var dataBytes = frameData.Skip(11).ToArray();
                _logger.LogInformation("   データ部: {0} bytes", dataBytes.Length);

                // データ型別解析の呼び出し
                if (_enableDetailedDataAnalysis && !string.IsNullOrEmpty(_currentOperationType))
                {
                    var detailedAnalysis = AnalyzeDataByType(dataBytes, _currentOperationType, _logger);
                    if (!string.IsNullOrEmpty(detailedAnalysis))
                    {
                        _logger.LogInformation("   詳細データ解析結果:\n{DetailedAnalysis}", detailedAnalysis);
                    }
                }
            }
        }

        /// <summary>
        /// スキャン進行状況の概要を取得
        /// </summary>
        /// <param name="results">スキャン結果一覧</param>
        /// <returns>進行状況の文字列</returns>
        public string GetScanSummary(IList<DeviceScanResult> results)
        {
            if (results == null || results.Count == 0)
                return "スキャン結果なし";

            var totalActiveDevices = results.Sum(r => r.ActiveDevices.Count);
            var totalSuccessfulScans = results.Sum(r => r.Statistics.SuccessfulScans);
            var totalFailedScans = results.Sum(r => r.Statistics.FailedScans);
            var totalScanTime = results.Sum(r => r.Statistics.TotalScanTimeMs);
            var averageResponseTime = totalSuccessfulScans > 0 ? totalScanTime / totalSuccessfulScans : 0;

            return $"スキャン概要: デバイス種類{results.Count}, " +
                   $"アクティブデバイス{totalActiveDevices}個, " +
                   $"成功{totalSuccessfulScans}/失敗{totalFailedScans}, " +
                   $"平均応答{averageResponseTime:F1}ms";
        }
    }
}