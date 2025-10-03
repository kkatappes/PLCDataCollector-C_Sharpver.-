using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SlmpClient.Constants;
using SlmpClient.Exceptions;

namespace SlmpClient.Core
{
    /// <summary>
    /// アダプティブ監視管理
    /// アクティブデバイスの動的監視とデータ収集最適化
    /// </summary>
    public class AdaptiveMonitoringManager
    {
        private readonly ISlmpClientFull _slmpClient;
        private readonly ILogger<AdaptiveMonitoringManager> _logger;
        private readonly ConcurrentDictionary<string, DeviceValue> _lastValues;
        private readonly ConcurrentDictionary<string, DateTime> _lastUpdateTimes;
        private readonly ConcurrentDictionary<string, int> _changeCounters;
        private readonly ActiveDeviceThreshold _activeThreshold;

        // 監視状態管理
        private volatile bool _isMonitoring = false;
        private CancellationTokenSource? _monitoringCancellation;
        private Task? _monitoringTask;
        private readonly object _monitoringLock = new();

        // 監視設定
        public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromMilliseconds(1000);
        public int MaxBatchSize { get; set; } = 32;
        public int MaxConcurrentOperations { get; set; } = 4;
        public TimeSpan InactiveDeviceTimeout { get; set; } = TimeSpan.FromMinutes(5);

        // 統計情報
        public MonitoringStatistics Statistics { get; } = new();

        public AdaptiveMonitoringManager(
            ISlmpClientFull slmpClient,
            ILogger<AdaptiveMonitoringManager> logger,
            ActiveDeviceThreshold? activeThreshold = null)
        {
            _slmpClient = slmpClient ?? throw new ArgumentNullException(nameof(slmpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeThreshold = activeThreshold ?? new ActiveDeviceThreshold();
            _lastValues = new ConcurrentDictionary<string, DeviceValue>();
            _lastUpdateTimes = new ConcurrentDictionary<string, DateTime>();
            _changeCounters = new ConcurrentDictionary<string, int>();
        }

        /// <summary>
        /// 監視対象デバイスを登録
        /// </summary>
        /// <param name="devices">監視対象デバイス一覧</param>
        public void RegisterDevices(IList<(DeviceCode deviceCode, uint address)> devices)
        {
            if (devices == null)
                throw new ArgumentNullException(nameof(devices));

            var now = DateTime.Now;

            foreach (var device in devices)
            {
                var deviceKey = GetDeviceKey(device.deviceCode, device.address);

                // 初期値を登録（未知の値として）
                var initialValue = new DeviceValue
                {
                    DeviceCode = device.deviceCode,
                    Address = device.address,
                    Value = IsWordDevice(device.deviceCode) ? (ushort)0 : false,
                    ValueType = IsWordDevice(device.deviceCode) ? DeviceValueType.Word : DeviceValueType.Bit,
                    Timestamp = now
                };

                _lastValues.TryAdd(deviceKey, initialValue);
                _lastUpdateTimes.TryAdd(deviceKey, now);
                _changeCounters.TryAdd(deviceKey, 0);
            }

            _logger.LogInformation("監視対象デバイス登録: {DeviceCount}個", devices.Count);
        }

        /// <summary>
        /// アダプティブ監視を開始
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>監視開始タスク</returns>
        public Task StartMonitoringAsync(CancellationToken cancellationToken = default)
        {
            lock (_monitoringLock)
            {
                if (_isMonitoring)
                {
                    _logger.LogWarning("監視は既に開始されています");
                    return Task.CompletedTask;
                }

                _isMonitoring = true;
                _monitoringCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _monitoringTask = MonitoringLoopAsync(_monitoringCancellation.Token);

                _logger.LogInformation("アダプティブ監視開始: 間隔{Interval}ms, バッチサイズ{BatchSize}",
                    MonitoringInterval.TotalMilliseconds, MaxBatchSize);

                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// アダプティブ監視を停止
        /// </summary>
        /// <returns>監視停止タスク</returns>
        public async Task StopMonitoringAsync()
        {
            lock (_monitoringLock)
            {
                if (!_isMonitoring)
                {
                    _logger.LogDebug("監視は既に停止されています");
                    return;
                }

                _isMonitoring = false;
                _monitoringCancellation?.Cancel();
            }

            if (_monitoringTask != null)
            {
                try
                {
                    await _monitoringTask;
                    _logger.LogInformation("アダプティブ監視停止完了");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("監視タスクがキャンセルされました");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "監視停止中にエラーが発生");
                }
            }

            _monitoringCancellation?.Dispose();
            _monitoringCancellation = null;
            _monitoringTask = null;
        }

        /// <summary>
        /// 現在の監視状態を取得
        /// </summary>
        /// <returns>監視中の場合はtrue</returns>
        public bool IsMonitoring => _isMonitoring;

        /// <summary>
        /// 監視対象デバイス数を取得
        /// </summary>
        public int MonitoredDeviceCount => _lastValues.Count;

        /// <summary>
        /// アクティブデバイス数を取得（最近更新されたデバイス）
        /// </summary>
        public int ActiveDeviceCount
        {
            get
            {
                var cutoffTime = DateTime.Now - InactiveDeviceTimeout;
                return _lastUpdateTimes.Count(kvp => kvp.Value > cutoffTime);
            }
        }

        /// <summary>
        /// 単一監視サイクルを実行
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>監視サイクル結果</returns>
        public async Task<MonitoringCycleResult> ExecuteMonitoringCycleAsync(CancellationToken cancellationToken = default)
        {
            var cycleResult = new MonitoringCycleResult
            {
                StartTime = DateTime.Now
            };

            List<(DeviceCode deviceCode, uint address)>? devicesToMonitor = null;

            try
            {
                // 監視対象デバイスを取得（最近アクティブなデバイスを優先）
                devicesToMonitor = GetDevicesToMonitor();

                if (devicesToMonitor.Count == 0)
                {
                    cycleResult.ErrorMessage = "監視対象デバイスがありません";
                    return cycleResult;
                }

                // デバイス種別ごとにグループ化
                var bitDevices = devicesToMonitor.Where(d => !IsWordDevice(d.deviceCode)).ToList();
                var wordDevices = devicesToMonitor.Where(d => IsWordDevice(d.deviceCode)).ToList();

                // 並列監視実行
                var tasks = new List<Task<List<DeviceValue>>>();

                if (bitDevices.Count > 0)
                {
                    tasks.Add(MonitorBitDevicesAsync(bitDevices, cancellationToken));
                }

                if (wordDevices.Count > 0)
                {
                    tasks.Add(MonitorWordDevicesAsync(wordDevices, cancellationToken));
                }

                // 全監視タスクの完了を待機
                var results = await Task.WhenAll(tasks);

                // 結果をマージ
                foreach (var deviceValues in results)
                {
                    cycleResult.DeviceValues.AddRange(deviceValues);
                }

                // 変化検出と統計更新
                ProcessMonitoringResults(cycleResult);

                cycleResult.SuccessfulReads = cycleResult.DeviceValues.Count;
                Statistics.IncrementSuccessfulCycles();

                _logger.LogTrace("監視サイクル完了: {DeviceCount}デバイス, 変化{ChangedCount}個, 時間{Duration}ms",
                    cycleResult.DeviceValues.Count, cycleResult.ChangedDevicesCount, cycleResult.Duration.TotalMilliseconds);
            }
            catch (OperationCanceledException)
            {
                cycleResult.ErrorMessage = "監視サイクルがキャンセルされました";
                Statistics.IncrementFailedCycles();
            }
            catch (Exception ex)
            {
                cycleResult.ErrorMessage = ex.Message;
                cycleResult.FailedReads = devicesToMonitor?.Count ?? 0;
                Statistics.IncrementFailedCycles();

                _logger.LogError(ex, "監視サイクルでエラーが発生");
            }
            finally
            {
                cycleResult.EndTime = DateTime.Now;
            }

            return cycleResult;
        }

        /// <summary>
        /// 監視ループ（メインタスク）
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>監視ループタスク</returns>
        private async Task MonitoringLoopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("監視ループ開始");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var cycleStart = DateTime.Now;

                    try
                    {
                        var result = await ExecuteMonitoringCycleAsync(cancellationToken);

                        if (!result.IsSuccessful)
                        {
                            _logger.LogWarning("監視サイクル失敗: {Error}", result.ErrorMessage);
                        }

                        // 統計情報を更新
                        Statistics.UpdateLastCycleTime(result.Duration);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "監視ループで予期しないエラー");
                        Statistics.IncrementFailedCycles();
                    }

                    // インターバル調整（処理時間を考慮）
                    var elapsed = DateTime.Now - cycleStart;
                    var waitTime = MonitoringInterval - elapsed;

                    if (waitTime > TimeSpan.Zero)
                    {
                        await Task.Delay(waitTime, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("監視ループがキャンセルされました");
            }
            finally
            {
                _logger.LogInformation("監視ループ終了");
            }
        }

        /// <summary>
        /// 監視対象デバイスを決定（アダプティブ選択）
        /// </summary>
        /// <returns>監視対象デバイス一覧</returns>
        private List<(DeviceCode deviceCode, uint address)> GetDevicesToMonitor()
        {
            var now = DateTime.Now;
            var cutoffTime = now - InactiveDeviceTimeout;

            return _lastUpdateTimes
                .Where(kvp => kvp.Value > cutoffTime) // アクティブなデバイスのみ
                .OrderByDescending(kvp => _changeCounters.GetValueOrDefault(kvp.Key, 0)) // 変化頻度順
                .Take(MaxBatchSize * MaxConcurrentOperations) // 制限内に収める
                .Select(kvp => ParseDeviceKey(kvp.Key))
                .ToList();
        }

        /// <summary>
        /// ビットデバイス群を監視
        /// </summary>
        /// <param name="devices">ビットデバイス一覧</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>監視結果</returns>
        private async Task<List<DeviceValue>> MonitorBitDevicesAsync(
            IList<(DeviceCode deviceCode, uint address)> devices,
            CancellationToken cancellationToken)
        {
            var results = new List<DeviceValue>();

            // デバイスコード別にグループ化
            var deviceGroups = devices.GroupBy(d => d.deviceCode);

            foreach (var group in deviceGroups)
            {
                var deviceCode = group.Key;
                var addresses = group.Select(d => d.address).OrderBy(a => a).ToList();

                // 連続アドレスをバッチ処理
                var batches = CreateAddressBatches(addresses, MaxBatchSize);

                foreach (var batch in batches)
                {
                    try
                    {
                        var data = await _slmpClient.ReadBitDevicesAsync(
                            deviceCode, batch.Start, (ushort)batch.Count, 0, cancellationToken);

                        for (int i = 0; i < data.Length; i++)
                        {
                            var deviceValue = new DeviceValue
                            {
                                DeviceCode = deviceCode,
                                Address = batch.Start + (uint)i,
                                Value = data[i],
                                ValueType = DeviceValueType.Bit,
                                Timestamp = DateTime.Now
                            };
                            results.Add(deviceValue);
                        }
                    }
                    catch (SlmpException ex)
                    {
                        _logger.LogWarning("ビットデバイス読み取りエラー: {DeviceCode}:{Start}-{End} - {Error}",
                            deviceCode, batch.Start, batch.Start + batch.Count - 1, ex.Message);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// ワードデバイス群を監視
        /// </summary>
        /// <param name="devices">ワードデバイス一覧</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>監視結果</returns>
        private async Task<List<DeviceValue>> MonitorWordDevicesAsync(
            IList<(DeviceCode deviceCode, uint address)> devices,
            CancellationToken cancellationToken)
        {
            var results = new List<DeviceValue>();

            // デバイスコード別にグループ化
            var deviceGroups = devices.GroupBy(d => d.deviceCode);

            foreach (var group in deviceGroups)
            {
                var deviceCode = group.Key;
                var addresses = group.Select(d => d.address).OrderBy(a => a).ToList();

                // 連続アドレスをバッチ処理
                var batches = CreateAddressBatches(addresses, Math.Min(MaxBatchSize, 960)); // SLMP制限

                foreach (var batch in batches)
                {
                    try
                    {
                        var data = await _slmpClient.ReadWordDevicesAsync(
                            deviceCode, batch.Start, (ushort)batch.Count, 0, cancellationToken);

                        for (int i = 0; i < data.Length; i++)
                        {
                            var deviceValue = new DeviceValue
                            {
                                DeviceCode = deviceCode,
                                Address = batch.Start + (uint)i,
                                Value = data[i],
                                ValueType = DeviceValueType.Word,
                                Timestamp = DateTime.Now
                            };
                            results.Add(deviceValue);
                        }
                    }
                    catch (SlmpException ex)
                    {
                        _logger.LogWarning("ワードデバイス読み取りエラー: {DeviceCode}:{Start}-{End} - {Error}",
                            deviceCode, batch.Start, batch.Start + batch.Count - 1, ex.Message);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 監視結果を処理（変化検出・統計更新）
        /// </summary>
        /// <param name="cycleResult">監視サイクル結果</param>
        private void ProcessMonitoringResults(MonitoringCycleResult cycleResult)
        {
            var now = DateTime.Now;
            var changedCount = 0;

            foreach (var deviceValue in cycleResult.DeviceValues)
            {
                var deviceKey = GetDeviceKey(deviceValue.DeviceCode, deviceValue.Address);

                // 前回値と比較
                if (_lastValues.TryGetValue(deviceKey, out var lastValue))
                {
                    if (!ValuesEqual(lastValue.Value, deviceValue.Value))
                    {
                        changedCount++;
                        _changeCounters.AddOrUpdate(deviceKey, 1, (key, count) => count + 1);

                        _logger.LogTrace("デバイス値変化: {DeviceAddress} {OldValue} -> {NewValue}",
                            deviceValue.DeviceAddress, lastValue.Value, deviceValue.Value);
                    }
                }
                else
                {
                    // 新規デバイス
                    changedCount++;
                    _changeCounters.TryAdd(deviceKey, 1);
                }

                // 値を更新
                _lastValues.AddOrUpdate(deviceKey, deviceValue, (key, oldValue) => deviceValue);
                _lastUpdateTimes.AddOrUpdate(deviceKey, now, (key, oldTime) => now);
            }

            cycleResult.ChangedDevicesCount = changedCount;
        }

        /// <summary>
        /// 連続アドレスをバッチに分割
        /// </summary>
        /// <param name="addresses">アドレス一覧（ソート済み）</param>
        /// <param name="maxBatchSize">最大バッチサイズ</param>
        /// <returns>バッチ一覧</returns>
        private List<(uint Start, int Count)> CreateAddressBatches(IList<uint> addresses, int maxBatchSize)
        {
            var batches = new List<(uint Start, int Count)>();

            if (addresses.Count == 0)
                return batches;

            var currentStart = addresses[0];
            var currentCount = 1;

            for (int i = 1; i < addresses.Count; i++)
            {
                if (addresses[i] == addresses[i-1] + 1 && currentCount < maxBatchSize)
                {
                    // 連続アドレス
                    currentCount++;
                }
                else
                {
                    // バッチを確定
                    batches.Add((currentStart, currentCount));
                    currentStart = addresses[i];
                    currentCount = 1;
                }
            }

            // 最後のバッチを追加
            batches.Add((currentStart, currentCount));

            return batches;
        }

        /// <summary>
        /// デバイスキーを生成
        /// </summary>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="address">アドレス</param>
        /// <returns>デバイスキーの文字列</returns>
        private static string GetDeviceKey(DeviceCode deviceCode, uint address)
        {
            return $"{deviceCode}:{address}";
        }

        /// <summary>
        /// デバイスキーを解析
        /// </summary>
        /// <param name="deviceKey">デバイスキー</param>
        /// <returns>デバイスコードとアドレス</returns>
        private static (DeviceCode deviceCode, uint address) ParseDeviceKey(string deviceKey)
        {
            var parts = deviceKey.Split(':');
            if (parts.Length != 2)
                throw new ArgumentException($"Invalid device key format: {deviceKey}");

            if (!Enum.TryParse<DeviceCode>(parts[0], out var deviceCode))
                throw new ArgumentException($"Invalid device code: {parts[0]}");

            if (!uint.TryParse(parts[1], out var address))
                throw new ArgumentException($"Invalid address: {parts[1]}");

            return (deviceCode, address);
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
        /// 値の等価性をチェック
        /// </summary>
        /// <param name="value1">値1</param>
        /// <param name="value2">値2</param>
        /// <returns>等しい場合はtrue</returns>
        private static bool ValuesEqual(object value1, object value2)
        {
            return Equals(value1, value2);
        }

        /// <summary>
        /// リソースを解放
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await StopMonitoringAsync();
        }
    }

    /// <summary>
    /// 監視統計情報
    /// </summary>
    public class MonitoringStatistics
    {
        private long _successfulCycles = 0;
        private long _failedCycles = 0;
        private TimeSpan _lastCycleDuration = TimeSpan.Zero;
        private readonly object _lock = new();

        /// <summary>
        /// 成功したサイクル数
        /// </summary>
        public long SuccessfulCycles => _successfulCycles;

        /// <summary>
        /// 失敗したサイクル数
        /// </summary>
        public long FailedCycles => _failedCycles;

        /// <summary>
        /// 総サイクル数
        /// </summary>
        public long TotalCycles => _successfulCycles + _failedCycles;

        /// <summary>
        /// 成功率（%）
        /// </summary>
        public double SuccessRate => TotalCycles > 0 ? (double)_successfulCycles / TotalCycles * 100 : 0;

        /// <summary>
        /// 最後のサイクル実行時間
        /// </summary>
        public TimeSpan LastCycleDuration => _lastCycleDuration;

        /// <summary>
        /// 成功サイクル数を増加
        /// </summary>
        public void IncrementSuccessfulCycles()
        {
            Interlocked.Increment(ref _successfulCycles);
        }

        /// <summary>
        /// 失敗サイクル数を増加
        /// </summary>
        public void IncrementFailedCycles()
        {
            Interlocked.Increment(ref _failedCycles);
        }

        /// <summary>
        /// 最後のサイクル時間を更新
        /// </summary>
        /// <param name="duration">実行時間</param>
        public void UpdateLastCycleTime(TimeSpan duration)
        {
            lock (_lock)
            {
                _lastCycleDuration = duration;
            }
        }

        /// <summary>
        /// 統計情報をリセット
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _successfulCycles = 0;
                _failedCycles = 0;
                _lastCycleDuration = TimeSpan.Zero;
            }
        }

        /// <summary>
        /// 統計情報の文字列表現
        /// </summary>
        /// <returns>統計情報文字列</returns>
        public override string ToString()
        {
            return $"監視統計: 成功{SuccessfulCycles}/失敗{FailedCycles} (成功率{SuccessRate:F1}%), 最終サイクル時間{LastCycleDuration.TotalMilliseconds:F1}ms";
        }
    }
}