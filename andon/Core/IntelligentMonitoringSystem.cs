using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SlmpClient.Constants;
using SlmpClient.Exceptions;
using TypeCode = SlmpClient.Constants.TypeCode;

namespace SlmpClient.Core
{
    /// <summary>
    /// インテリジェント監視システム
    /// デバイス探索から動的監視まで統合制御するメインシステム
    /// TDD手法REFACTOR Phase: コード最適化・定数統一・責任分離・SOLID原則強化
    /// 6ステップフロー：
    /// 1. 設定ファイルで接続するPLCを決定
    /// 2. PLCに接続し機器情報を取得
    /// 3. 機器情報からシリーズを判定し、デバイスコードを抽出
    /// 4. 全デバイスコード＋一般的な機器番号で網羅的スキャン
    /// 5. 応答があった(非ゼロデータ)デバイスを抽出
    /// 6. 抽出したデバイスのデータのみを継続的に取得
    /// </summary>
    public class IntelligentMonitoringSystem
    {
        #region Constants

        // システム情報定数
        private const string ApplicationName = "Andon SLMP Client - Comprehensive Device Discovery System";
        private const string ApplicationVersion = "2.0.0-comprehensive";
        private const string ApplicationEnvironment = "Production";

        // 設定キー定数
        private const string ConfigKey_PlcConnection = "PlcConnection";
        private const string ConfigKey_DeviceDiscoverySettings = "DeviceDiscoverySettings";
        private const string ConfigKey_ContinuitySettings = "ContinuitySettings";
        private const string ConfigKey_TimeoutSettings = "TimeoutSettings";
        private const string ConfigKey_UnifiedLoggingSettings = "UnifiedLoggingSettings";

        // 設定値定数
        private const string ConfigKey_DisplayName = "DisplayName";
        private const string ConfigKey_IpAddress = "IpAddress";
        private const string ConfigKey_Port = "Port";
        private const string ConfigKey_UseTcp = "UseTcp";
        private const string ConfigKey_FrameVersion = "FrameVersion";
        private const string ConfigKey_IsBinary = "IsBinary";
        private const string ConfigKey_DiscoveryMode = "DiscoveryMode";
        private const string ConfigKey_Mode = "Mode";
        private const string ConfigKey_ReceiveTimeoutMs = "ReceiveTimeoutMs";
        private const string ConfigKey_ConnectTimeoutMs = "ConnectTimeoutMs";
        private const string ConfigKey_LogFilePath = "LogFilePath";

        // デフォルト値定数
        private const string DefaultPlcDisplayName = "PLC Device";
        private const string DefaultIpAddress = "192.168.1.10";
        private const int DefaultPort = 5007;
        private const bool DefaultUseTcp = true;
        private const string DefaultFrameVersion = "4E";
        private const bool DefaultIsBinary = true;
        private const int DefaultReceiveTimeoutMs = 3000;
        private const int DefaultConnectTimeoutMs = 10000;
        private const string DefaultContinuityMode = "ReturnDefaultAndContinue";
        private const string DefaultLogFilePath = "logs/slmp_unified_log.log";
        private const string DefaultRawDataLogging = "Enabled";

        // 6ステップフロー定数
        private const string Step1_Message = "Step 1実行: 設定ファイルからPLC接続情報を取得";
        private const string Step2_Message = "Step 2実行: PLC {0} に接続し機器情報を取得";
        private const string Step3_Message = "Step 3実行: TypeCode {0} からシリーズ判定・デバイスコード抽出";
        private const string Step4_Message = "Step 4実行: 全デバイスコード＋一般的機器番号で網羅的スキャン";
        private const string Step5_Message = "Step 5実行: 非ゼロデータデバイス抽出";
        private const string Step6_Message = "Step 6実行: 抽出した {0}個のデバイスの継続監視開始";

        // ログメッセージ定数
        private const string LogMessage_SixStepFlowStart = "=== 6ステップフロー実行開始 ===";
        private const string LogMessage_SixStepFlowComplete = "=== 6ステップフロー実行完了 ===";
        private const string LogMessage_FallbackStart = "フォールバック処理開始: ReadTypeName代替手段を試行";
        private const string LogMessage_BasicConnectionTest = "基本接続テスト実行: 最小限のSLMP通信確認";

        // エラーメッセージ定数
        private const string ErrorMessage_Step1Failed = "Step 1失敗: 設定ファイル読み込みエラー";
        private const string ErrorMessage_Step3Failed = "Step 3失敗: シリーズ判定・デバイスコード抽出エラー";
        private const string ErrorMessage_Step4Failed = "Step 4失敗: デバイス網羅的スキャンエラー";
        private const string ErrorMessage_Step5Failed = "Step 5失敗: 非ゼロデータ抽出エラー";
        private const string ErrorMessage_Step6Failed = "Step 6失敗: 継続監視開始エラー";
        private const string ErrorMessage_SixStepFlowError = "6ステップフロー実行中にエラーが発生";
        private const string ErrorMessage_MonitoringStopError = "監視停止中にエラーが発生";
        private const string ErrorMessage_ResourceDisposeError = "リソース解放中にエラーが発生";

        // その他定数
        private const int MaxDisplayDeviceCount = 10;
        private const string UnknownTypeName = "Unknown";
        private const string SessionIdFormat = "session_{0:yyyyMMdd_HHmmss}";

        #endregion
        private readonly ISlmpClientFull _slmpClient;
        private readonly ILogger<IntelligentMonitoringSystem> _logger;
        private readonly DeviceDiscoveryManager _discoveryManager;
        private readonly DeviceScanner _deviceScanner;
        private readonly AdaptiveMonitoringManager _monitoringManager;
        private readonly UnifiedLogWriter _unifiedLogWriter;

        // システム状態
        private IntelligentMonitoringState _currentState = IntelligentMonitoringState.Idle;
        private DeviceDiscoveryResult? _lastDiscoveryResult;
        private readonly object _stateLock = new();

        // 設定
        public TimeSpan DiscoveryInterval { get; set; } = TimeSpan.FromMinutes(30);
        public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromSeconds(1);
        public bool EnableAutoRediscovery { get; set; } = true;
        public int MinActiveDevicesForMonitoring { get; set; } = 1;

        public IntelligentMonitoringSystem(
            ISlmpClientFull slmpClient,
            ILogger<IntelligentMonitoringSystem> logger,
            UnifiedLogWriter unifiedLogWriter,
            IConfiguration configuration,
            ActiveDeviceThreshold? activeThreshold = null)
        {
            _slmpClient = slmpClient ?? throw new ArgumentNullException(nameof(slmpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unifiedLogWriter = unifiedLogWriter ?? throw new ArgumentNullException(nameof(unifiedLogWriter));

            var threshold = activeThreshold ?? new ActiveDeviceThreshold();

            // 各コンポーネントを初期化
            _discoveryManager = new DeviceDiscoveryManager(
                logger as ILogger<DeviceDiscoveryManager> ??
                Microsoft.Extensions.Logging.Abstractions.NullLogger<DeviceDiscoveryManager>.Instance,
                threshold);

            // 設定からDiscoveryModeを読み込み適用
            var discoverySettings = configuration.GetSection("DeviceDiscoverySettings");
            var discoveryModeValue = discoverySettings["DiscoveryMode"];
            _logger.LogInformation("設定値読み込み: DeviceDiscoverySettings:DiscoveryMode = '{DiscoveryModeValue}'", discoveryModeValue);

            if (!string.IsNullOrEmpty(discoveryModeValue) &&
                Enum.TryParse<DeviceDiscoveryManager.DiscoveryMode>(discoveryModeValue, out var discoveryMode))
            {
                _discoveryManager.Mode = discoveryMode;
                _logger.LogInformation("✅ DiscoveryMode設定適用成功: {DiscoveryMode}", discoveryMode);
            }
            else
            {
                _logger.LogWarning("❌ DiscoveryMode設定が見つからないかパースエラー。値='{Value}', デフォルト値を使用: {DefaultMode}",
                    discoveryModeValue, _discoveryManager.Mode);
            }

            _deviceScanner = new DeviceScanner(_slmpClient,
                logger as ILogger<DeviceScanner> ??
                Microsoft.Extensions.Logging.Abstractions.NullLogger<DeviceScanner>.Instance,
                _unifiedLogWriter,
                threshold);

            _monitoringManager = new AdaptiveMonitoringManager(_slmpClient,
                logger as ILogger<AdaptiveMonitoringManager> ??
                Microsoft.Extensions.Logging.Abstractions.NullLogger<AdaptiveMonitoringManager>.Instance,
                threshold);

            // 監視間隔を設定
            _monitoringManager.MonitoringInterval = MonitoringInterval;
        }

        /// <summary>
        /// 現在のシステム状態
        /// </summary>
        public IntelligentMonitoringState CurrentState
        {
            get
            {
                lock (_stateLock)
                {
                    return _currentState;
                }
            }
            private set
            {
                lock (_stateLock)
                {
                    var oldState = _currentState;
                    _currentState = value;

                    if (oldState != value)
                    {
                        _logger.LogInformation("システム状態変更: {OldState} → {NewState}", oldState, value);
                    }
                }
            }
        }

        /// <summary>
        /// 最後のデバイス探索結果
        /// </summary>
        public DeviceDiscoveryResult? LastDiscoveryResult => _lastDiscoveryResult;

        /// <summary>
        /// 監視マネージャーの統計情報
        /// </summary>
        public MonitoringStatistics MonitoringStatistics => _monitoringManager.Statistics;

        /// <summary>
        /// 6ステップフロー自動実行：設計文書通りの完全な6ステップフローを実行
        /// </summary>
        /// <param name="config">設定オブジェクト</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>実行結果</returns>
        public async Task<IntelligentMonitoringResult> RunSixStepFlowAsync(IConfiguration config, CancellationToken cancellationToken = default)
        {
            var result = new IntelligentMonitoringResult
            {
                StartTime = DateTime.Now
            };

            // セッション開始記録
            var sessionId = await StartSessionAsync(config);

            try
            {
                // 6ステップフロー実行
                await ExecuteSixStepFlowAsync(config, result, cancellationToken);

                // セッション終了記録
                await EndSessionSuccessAsync(sessionId, result);

                _logger.LogInformation(LogMessage_SixStepFlowComplete);
                return result;
            }
            catch (Exception ex)
            {
                CurrentState = IntelligentMonitoringState.Error;
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.Summary = $"6ステップフロー実行失敗: {ex.Message}";

                // エラー時のセッション終了記録
                await EndSessionErrorAsync(sessionId, result, ex);

                _logger.LogError(ex, ErrorMessage_SixStepFlowError);
                return result;
            }
            finally
            {
                result.EndTime = DateTime.Now;
            }
        }

        #region Session Management

        /// <summary>
        /// セッション開始処理
        /// </summary>
        /// <param name="config">設定オブジェクト</param>
        /// <returns>セッションID</returns>
        private async Task<string> StartSessionAsync(IConfiguration config)
        {
            var sessionId = string.Format(SessionIdFormat, DateTime.Now);
            var sessionInfo = new SessionStartInfo
            {
                SessionId = sessionId,
                ProcessId = Environment.ProcessId,
                ApplicationName = ApplicationName,
                Version = ApplicationVersion,
                Environment = ApplicationEnvironment
            };

            var configDetails = new ConfigurationDetails
            {
                ConfigFile = "appsettings.json",
                ConnectionTarget = config.GetSection(ConfigKey_PlcConnection)[ConfigKey_DisplayName] ?? "Unknown PLC",
                SlmpSettings = "6ステップフロー - 完全デバイス探索",
                ContinuityMode = config.GetSection(ConfigKey_ContinuitySettings)[ConfigKey_Mode] ?? DefaultContinuityMode,
                RawDataLogging = DefaultRawDataLogging,
                LogOutputPath = config.GetSection(ConfigKey_UnifiedLoggingSettings)[ConfigKey_LogFilePath] ?? DefaultLogFilePath
            };

            await _unifiedLogWriter.WriteSessionStartAsync(sessionInfo, configDetails);
            return sessionId;
        }

        /// <summary>
        /// セッション終了処理（成功時）
        /// </summary>
        /// <param name="sessionId">セッションID</param>
        /// <param name="result">実行結果</param>
        private async Task EndSessionSuccessAsync(string sessionId, IntelligentMonitoringResult result)
        {
            await _unifiedLogWriter.WriteSessionEndAsync(new SessionSummary
            {
                SessionId = sessionId,
                Duration = $"{(DateTime.Now - result.StartTime).TotalSeconds:F1}s",
                FinalStatus = "Success",
                ExitReason = "6ステップフロー正常完了",
                TotalLogEntries = 0, // TODO: ログエントリ数のカウント実装
                FinalMessage = result.Summary
            });
        }

        /// <summary>
        /// セッション終了処理（エラー時）
        /// </summary>
        /// <param name="sessionId">セッションID</param>
        /// <param name="result">実行結果</param>
        /// <param name="ex">発生した例外</param>
        private async Task EndSessionErrorAsync(string sessionId, IntelligentMonitoringResult result, Exception ex)
        {
            await _unifiedLogWriter.WriteSessionEndAsync(new SessionSummary
            {
                SessionId = sessionId,
                Duration = $"{(DateTime.Now - result.StartTime).TotalSeconds:F1}s",
                FinalStatus = "Error",
                ExitReason = $"例外発生: {ex.GetType().Name}",
                TotalLogEntries = 0, // TODO: ログエントリ数のカウント実装
                FinalMessage = result.Summary
            });
        }

        /// <summary>
        /// 6ステップフロー実行処理
        /// </summary>
        /// <param name="config">設定オブジェクト</param>
        /// <param name="result">実行結果</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        private async Task ExecuteSixStepFlowAsync(IConfiguration config, IntelligentMonitoringResult result, CancellationToken cancellationToken)
        {
            CurrentState = IntelligentMonitoringState.Discovering;
            _logger.LogInformation(LogMessage_SixStepFlowStart);

            // Step 1: 設定ファイルからPLC接続情報取得
            var plcConnectionInfo = Step1_LoadPlcConfiguration(config);
            _logger.LogInformation("Step 1完了: PLC接続先='{DisplayName}' ({IpAddress}:{Port})",
                plcConnectionInfo.DisplayName, plcConnectionInfo.IpAddress, plcConnectionInfo.Port);

            // Step 2: PLC接続と機器情報取得
            var connectionResult = await Step2_ConnectAndGetDeviceInfoAsync(plcConnectionInfo, cancellationToken);
            _logger.LogInformation("Step 2完了: 接続成功={IsSuccess}, 型名='{TypeName}', TypeCode={TypeCode}",
                connectionResult.IsConnectionSuccessful, connectionResult.TypeName, connectionResult.TypeCode);

            result.PlcTypeName = connectionResult.TypeName;
            result.PlcTypeCode = connectionResult.TypeCode;

            // Step 3: シリーズ判定とデバイスコード抽出
            var discoveryConfig = Step3_DetermineSeriesAndExtractDeviceCodes(connectionResult);
            _logger.LogInformation("Step 3完了: ビット{BitCount}種類, ワード{WordCount}種類",
                discoveryConfig.BitDevices.Length, discoveryConfig.WordDevices.Length);

            // Step 4: 全デバイスコード網羅的スキャン
            var scanResults = await Step4_ComprehensiveScanAllDevices(discoveryConfig, cancellationToken);
            var totalScanned = scanResults.Sum(r => r.ScannedRange.Count);
            var totalActive = scanResults.Sum(r => r.ActiveDevices.Count);
            _logger.LogInformation("Step 4完了: 総スキャン数 {TotalScanned}個, アクティブ {ActiveCount}個を検出",
                totalScanned, totalActive);

            // Step 5: 非ゼロデータデバイス抽出
            var extractionResult = Step5_ExtractNonZeroDataDevices(scanResults);
            _logger.LogInformation("Step 5完了: 非ゼロデータデバイス {ActiveCount}個を抽出",
                extractionResult.ActiveDevices.Count);

            // Step 6: 継続監視開始
            if (extractionResult.ActiveDevices.Count > 0)
            {
                var monitoringResult = await Step6_StartContinuousMonitoring(extractionResult, cancellationToken);
                _logger.LogInformation("Step 6完了: 継続監視開始={IsStarted}, 監視デバイス数={DeviceCount}",
                    monitoringResult.IsStarted, monitoringResult.MonitoringDeviceCount);
                result.MonitoringStarted = monitoringResult.IsStarted;
            }
            else
            {
                _logger.LogInformation("Step 6: 非ゼロデータデバイスが0個のため継続監視はスキップ");
                result.MonitoringStarted = false;
            }

            result.Success = true;
            result.Summary = "6ステップフロー実行完了";
            CurrentState = result.MonitoringStarted ? IntelligentMonitoringState.Monitoring : IntelligentMonitoringState.Idle;
        }

        #endregion

        #region 6ステップフロー実装

        /// <summary>
        /// Step 1: 設定ファイルで接続するPLCを決定
        /// </summary>
        /// <param name="config">設定オブジェクト</param>
        /// <returns>PLC接続情報</returns>
        public PlcConnectionInfo Step1_LoadPlcConfiguration(IConfiguration config)
        {
            _logger.LogInformation(Step1_Message);

            try
            {
                var plcSettings = config.GetSection(ConfigKey_PlcConnection);
                var connectionInfo = new PlcConnectionInfo
                {
                    DisplayName = plcSettings[ConfigKey_DisplayName] ?? DefaultPlcDisplayName,
                    IpAddress = plcSettings[ConfigKey_IpAddress] ?? DefaultIpAddress,
                    Port = plcSettings.GetValue<int>(ConfigKey_Port, DefaultPort),
                    UseTcp = plcSettings.GetValue<bool>(ConfigKey_UseTcp, DefaultUseTcp),
                    FrameVersion = plcSettings[ConfigKey_FrameVersion] ?? DefaultFrameVersion,
                    ReceiveTimeoutMs = config.GetSection(ConfigKey_TimeoutSettings).GetValue<int>(ConfigKey_ReceiveTimeoutMs, DefaultReceiveTimeoutMs),
                    ConnectTimeoutMs = config.GetSection(ConfigKey_TimeoutSettings).GetValue<int>(ConfigKey_ConnectTimeoutMs, DefaultConnectTimeoutMs),
                    IsBinary = plcSettings.GetValue<bool>(ConfigKey_IsBinary, DefaultIsBinary)
                };

                _logger.LogInformation("Step 1成功: PLC接続先='{DisplayName}' ({IpAddress}:{Port})",
                    connectionInfo.DisplayName, connectionInfo.IpAddress, connectionInfo.Port);
                return connectionInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ErrorMessage_Step1Failed);
                throw;
            }
        }

        /// <summary>
        /// Step 2: PLCに接続し機器情報を取得
        /// </summary>
        /// <param name="connectionInfo">PLC接続情報</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>PLC接続結果</returns>
        public async Task<PlcConnectionResult> Step2_ConnectAndGetDeviceInfoAsync(
            PlcConnectionInfo connectionInfo, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(string.Format(Step2_Message, connectionInfo.DisplayName));

            try
            {
                // PLC接続を確立
                await _slmpClient.ConnectAsync(cancellationToken);
                _logger.LogInformation("Step 2: PLC接続成功 ({DisplayName})", connectionInfo.DisplayName);

                // PLC接続と型名取得
                var (typeName, typeCode) = await _slmpClient.ReadTypeNameAsync(0, cancellationToken);

                var result = new PlcConnectionResult
                {
                    TypeName = typeName,
                    TypeCode = typeCode,
                    ConnectionInfo = $"接続成功: {connectionInfo} → {typeName}",
                    IsConnectionSuccessful = true,
                    ConnectedAt = DateTime.Now
                };

                _logger.LogInformation("Step 2成功: 接続先='{DisplayName}', 型名='{TypeName}', TypeCode={TypeCode}",
                    connectionInfo.DisplayName, typeName, typeCode);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Step 2でReadTypeName失敗、フォールバック処理を実行: {ErrorMessage}", ex.Message);
                return await TryFallbackDeviceInfoAsync(connectionInfo, ex, cancellationToken);
            }
        }

        /// <summary>
        /// ReadTypeName失敗時のフォールバック処理
        /// 手順書に基づく代替手段で機器情報取得を試行
        /// </summary>
        /// <param name="connectionInfo">PLC接続情報</param>
        /// <param name="originalException">元の例外</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>フォールバック処理結果</returns>
        private async Task<PlcConnectionResult> TryFallbackDeviceInfoAsync(
            PlcConnectionInfo connectionInfo, Exception originalException, CancellationToken cancellationToken)
        {
            _logger.LogInformation(LogMessage_FallbackStart);

            try
            {
                // フォールバック手順1: より基本的なコマンドで接続確認
                await TryBasicConnectionTestAsync(cancellationToken);

                // フォールバック手順2: デフォルト値で継続
                var fallbackResult = new PlcConnectionResult
                {
                    TypeName = UnknownTypeName,
                    TypeCode = TypeCode.Q00CPU, // デフォルト値
                    ConnectionInfo = $"フォールバック成功: {connectionInfo} (ReadTypeName失敗により推定値使用)",
                    IsConnectionSuccessful = true,
                    ConnectedAt = DateTime.Now,
                    FallbackUsed = true,
                    OriginalError = originalException.Message
                };

                _logger.LogInformation("Step 2フォールバック成功: 接続先='{DisplayName}', TypeCode={TypeCode} (推定値)",
                    connectionInfo.DisplayName, fallbackResult.TypeCode);

                return fallbackResult;
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "フォールバック処理も失敗");
                throw new AggregateException("ReadTypeNameと全フォールバック処理が失敗", originalException, fallbackEx);
            }
        }

        /// <summary>
        /// 基本接続テスト（最小限のSLMP通信確認）
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>接続テスト結果</returns>
        private async Task TryBasicConnectionTestAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug(LogMessage_BasicConnectionTest);

            try
            {
                // 最も基本的なデバイス読取で接続確認（M0を1点読取）
                var testResult = await _slmpClient.ReadBitDevicesAsync(DeviceCode.M, 0, 1, 0, cancellationToken);
                _logger.LogDebug("基本接続テスト成功: M0読取 = {Result}", testResult?.FirstOrDefault());
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "基本接続テスト失敗、ただし継続可能");
                // 基本接続テストが失敗しても、フォールバック処理は継続
            }
        }

        /// <summary>
        /// Step 3: 機器情報からシリーズを判定し、対応するデバイスコードを抽出
        /// </summary>
        /// <param name="connectionResult">PLC接続結果</param>
        /// <returns>デバイス探索設定</returns>
        public DeviceDiscoveryConfiguration Step3_DetermineSeriesAndExtractDeviceCodes(PlcConnectionResult connectionResult)
        {
            _logger.LogInformation(string.Format(Step3_Message, connectionResult.TypeCode));

            try
            {
                var config = _discoveryManager.GetDiscoveryConfigurationForTypeCode(connectionResult.TypeCode);

                if (!_discoveryManager.ValidateConfiguration(config))
                {
                    throw new InvalidOperationException("探索設定の検証に失敗");
                }

                var summary = _discoveryManager.GetConfigurationSummary(config);
                _logger.LogInformation("Step 3成功: {ConfigSummary}", summary);
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ErrorMessage_Step3Failed);
                throw;
            }
        }

        /// <summary>
        /// Step 4: 全デバイスコード＋一般的な機器番号で網羅的にスキャンして応答を確認
        /// </summary>
        /// <param name="configuration">探索設定</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>スキャン結果</returns>
        public async Task<List<DeviceScanResult>> Step4_ComprehensiveScanAllDevices(
            DeviceDiscoveryConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(Step4_Message);

            // Step4開始記録
            var sessionId = string.Format(SessionIdFormat, DateTime.Now);
            await _unifiedLogWriter.WriteCycleStartAsync(new CycleStartInfo
            {
                SessionId = sessionId,
                CycleNumber = 4,
                StartMessage = "Step 4実行: 全デバイスコード＋一般的機器番号で網羅的スキャン",
                IntervalFromPrevious = 0
            });

            try
            {
                var results = await _deviceScanner.ScanDevicesAsync(configuration, cancellationToken);
                var totalScanned = results.Sum(r => r.ScannedRange.Count);
                var totalActive = results.Sum(r => r.ActiveDevices.Count);

                _logger.LogInformation("Step 4成功: 総スキャン数 {TotalScanned}個, アクティブ {ActiveDevices}個を検出",
                    totalScanned, totalActive);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ErrorMessage_Step4Failed);
                throw;
            }
        }

        /// <summary>
        /// Step 5: 応答があった(非ゼロデータ)デバイスを抽出
        /// </summary>
        /// <param name="scanResults">スキャン結果</param>
        /// <returns>非ゼロデータ抽出結果</returns>
        public NonZeroDataExtractionResult Step5_ExtractNonZeroDataDevices(List<DeviceScanResult> scanResults)
        {
            _logger.LogInformation(Step5_Message);

            try
            {
                var activeDevices = new List<ActiveDeviceInfo>();
                var totalScanned = scanResults.Sum(r => r.ScannedRange.Count);
                var now = DateTime.Now;

                foreach (var scanResult in scanResults)
                {
                    foreach (var deviceAddress in scanResult.ActiveDevices)
                    {
                        // デバイス値を取得（実際のスキャン結果から）
                        var value = GetDeviceValueFromScanResult(scanResult, deviceAddress);

                        // 非ゼロ判定
                        bool isNonZero = IsNonZeroValue(value, scanResult.DeviceCode);

                        if (isNonZero)
                        {
                            activeDevices.Add(new ActiveDeviceInfo
                            {
                                DeviceCode = scanResult.DeviceCode,
                                Address = deviceAddress,
                                Value = value,
                                ValueType = scanResult.DeviceCode.IsBitDevice() ? DeviceValueType.Bit : DeviceValueType.Word,
                                DetectedAt = now
                            });
                        }
                    }
                }

                var result = new NonZeroDataExtractionResult
                {
                    ActiveDevices = activeDevices,
                    TotalScannedDevices = (int)totalScanned,
                    ExtractedAt = now
                };

                _logger.LogInformation("Step 5成功: {ActiveCount}個の非ゼロデータデバイスを抽出 ({ExtractionRate:F1}%)",
                    activeDevices.Count, result.ExtractionRate);

                // 抽出されたデバイス詳細をログ出力（最大10個まで）
                _logger.LogInformation("=== Step 5: 抽出されたアクティブデバイス詳細 ===");
                var displayCount = Math.Min(activeDevices.Count, MaxDisplayDeviceCount);
                for (int i = 0; i < displayCount; i++)
                {
                    var device = activeDevices[i];
                    var valueText = device.ValueType == DeviceValueType.Bit
                        ? $"{device.Value} ({((bool)device.Value ? "ON" : "OFF")})"
                        : device.Value.ToString();

                    _logger.LogInformation("  ✅ {DeviceName} = {ValueText} ({ValueType}デバイス)",
                        device.DeviceName, valueText, device.ValueType == DeviceValueType.Bit ? "ビット" : "ワード");
                }

                if (activeDevices.Count > MaxDisplayDeviceCount)
                {
                    _logger.LogInformation("  ... 他 {RemainingCount}個のアクティブデバイス",
                        activeDevices.Count - MaxDisplayDeviceCount);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ErrorMessage_Step5Failed);
                throw;
            }
        }

        /// <summary>
        /// Step 6: 抽出したデバイスのデータのみを継続的に取得
        /// </summary>
        /// <param name="extractionResult">非ゼロデータ抽出結果</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>継続監視結果</returns>
        public async Task<ContinuousMonitoringResult> Step6_StartContinuousMonitoring(
            NonZeroDataExtractionResult extractionResult,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(string.Format(Step6_Message, extractionResult.ActiveDevices.Count));

            try
            {
                if (extractionResult.ActiveDevices.Count < MinActiveDevicesForMonitoring)
                {
                    var result = new ContinuousMonitoringResult
                    {
                        IsStarted = false,
                        MonitoringDeviceCount = 0,
                        StartedAt = DateTime.Now,
                        MonitoringSettings = $"アクティブデバイス数不足 ({extractionResult.ActiveDevices.Count} < {MinActiveDevicesForMonitoring})"
                    };

                    _logger.LogWarning("Step 6スキップ: {MonitoringSettings}", result.MonitoringSettings);
                    return result;
                }

                // アクティブデバイスを監視マネージャーに登録
                var deviceList = extractionResult.ActiveDevices
                    .Select(d => (d.DeviceCode, d.Address))
                    .ToList();

                _monitoringManager.RegisterDevices(deviceList);
                await _monitoringManager.StartMonitoringAsync(cancellationToken);

                var successResult = new ContinuousMonitoringResult
                {
                    IsStarted = true,
                    MonitoringDeviceCount = extractionResult.ActiveDevices.Count,
                    StartedAt = DateTime.Now,
                    MonitoringSettings = $"間隔={MonitoringInterval.TotalSeconds}s, 対象={extractionResult.ActiveDevices.Count}デバイス"
                };

                _logger.LogInformation("Step 6成功: 継続監視開始 - {MonitoringSettings}", successResult.MonitoringSettings);
                return successResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ErrorMessage_Step6Failed);
                throw;
            }
        }

        /// <summary>
        /// スキャン結果からデバイス値を取得
        /// </summary>
        private object GetDeviceValueFromScanResult(DeviceScanResult scanResult, uint address)
        {
            // 新機能：保存されたDeviceValuesから実際の値を取得
            var deviceValue = scanResult.DeviceValues.FirstOrDefault(dv => dv.Address == address);
            if (deviceValue != null)
            {
                return deviceValue.Value;
            }

            // フォールバック処理（旧式の処理、通常は使用されない）
            if (scanResult.DeviceCode.IsBitDevice())
            {
                return true; // アクティブデバイスとして検出されたということはtrue
            }
            else
            {
                return (ushort)1; // 非ゼロ値として仮定
            }
        }

        /// <summary>
        /// 値が非ゼロかどうかを判定
        /// </summary>
        private bool IsNonZeroValue(object value, DeviceCode deviceCode)
        {
            if (deviceCode.IsBitDevice())
            {
                return value is bool boolValue && boolValue; // true の場合のみアクティブ
            }
            else
            {
                return value switch
                {
                    ushort ushortValue => ushortValue != 0,
                    uint uintValue => uintValue != 0,
                    int intValue => intValue != 0,
                    _ => false
                };
            }
        }

        #endregion

        #region 既存4ステップフロー（後方互換性維持）





        /// <summary>
        /// 監視停止
        /// </summary>
        /// <returns>停止完了タスク</returns>
        public async Task StopMonitoringAsync()
        {
            try
            {
                CurrentState = IntelligentMonitoringState.Stopping;
                await _monitoringManager.StopMonitoringAsync();
                CurrentState = IntelligentMonitoringState.Idle;

                _logger.LogInformation("監視システム停止完了");
            }
            catch (Exception ex)
            {
                CurrentState = IntelligentMonitoringState.Error;
                _logger.LogError(ex, ErrorMessage_MonitoringStopError);
                throw;
            }
        }

        /// <summary>
        /// システム状態レポートを取得
        /// </summary>
        /// <returns>状態レポート</returns>
        public IntelligentMonitoringStatusReport GetStatusReport()
        {
            return new IntelligentMonitoringStatusReport
            {
                CurrentState = CurrentState,
                LastDiscoveryTime = _lastDiscoveryResult?.EndTime,
                MonitoredDeviceCount = _monitoringManager.MonitoredDeviceCount,
                ActiveDeviceCount = _monitoringManager.ActiveDeviceCount,
                IsMonitoring = _monitoringManager.IsMonitoring,
                MonitoringStatistics = _monitoringManager.Statistics,
                PlcTypeInfo = _lastDiscoveryResult != null ?
                    $"{_lastDiscoveryResult.PlcTypeName} ({_lastDiscoveryResult.PlcTypeCode})" : "未取得",
                TotalActiveDevices = _lastDiscoveryResult?.TotalActiveDevices ?? 0,
                LastDiscoveryDuration = _lastDiscoveryResult?.TotalDuration ?? TimeSpan.Zero
            };
        }

        /// <summary>
        /// 成功時の概要文字列を作成
        /// </summary>
        /// <param name="result">実行結果</param>
        /// <returns>概要文字列</returns>
        private string CreateSuccessSummary(IntelligentMonitoringResult result)
        {
            var duration = result.EndTime - result.StartTime;
            var activeDevices = result.DiscoveryResult?.TotalActiveDevices ?? 0;
            var monitoringStatus = result.MonitoringStarted ? "監視開始済み" : "監視対象なし";

            return $"実行成功: PLC型名'{result.PlcTypeName}' ({result.PlcTypeCode}), " +
                   $"アクティブデバイス{activeDevices}個検出, {monitoringStatus}, " +
                   $"実行時間{duration.TotalSeconds:F1}秒";
        }

        /// <summary>
        /// リソースを解放
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            try
            {
                await StopMonitoringAsync();
                await _monitoringManager.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ErrorMessage_ResourceDisposeError);
            }
        }
    }

    /// <summary>
    /// インテリジェント監視システムの状態
    /// </summary>
    public enum IntelligentMonitoringState
    {
        /// <summary>待機中</summary>
        Idle,
        /// <summary>デバイス探索中</summary>
        Discovering,
        /// <summary>監視中</summary>
        Monitoring,
        /// <summary>停止中</summary>
        Stopping,
        /// <summary>エラー状態</summary>
        Error
    }

    /// <summary>
    /// インテリジェント監視システム実行結果
    /// </summary>
    public class IntelligentMonitoringResult
    {
        /// <summary>実行開始時刻</summary>
        public DateTime StartTime { get; set; } = DateTime.Now;

        /// <summary>実行終了時刻</summary>
        public DateTime EndTime { get; set; } = DateTime.Now;

        /// <summary>実行時間</summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>実行成功フラグ</summary>
        public bool Success { get; set; } = false;

        /// <summary>PLC型名</summary>
        public string PlcTypeName { get; set; } = string.Empty;

        /// <summary>PLC TypeCode</summary>
        public TypeCode PlcTypeCode { get; set; }

        /// <summary>デバイス探索結果</summary>
        public DeviceDiscoveryResult? DiscoveryResult { get; set; }

        /// <summary>監視開始フラグ</summary>
        public bool MonitoringStarted { get; set; } = false;

        /// <summary>エラーメッセージ</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>実行概要</summary>
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// インテリジェント監視システム状態レポート
    /// </summary>
    public class IntelligentMonitoringStatusReport
    {
        /// <summary>現在の状態</summary>
        public IntelligentMonitoringState CurrentState { get; set; }

        /// <summary>最後の探索時刻</summary>
        public DateTime? LastDiscoveryTime { get; set; }

        /// <summary>監視対象デバイス数</summary>
        public int MonitoredDeviceCount { get; set; }

        /// <summary>アクティブデバイス数</summary>
        public int ActiveDeviceCount { get; set; }

        /// <summary>監視実行中フラグ</summary>
        public bool IsMonitoring { get; set; }

        /// <summary>監視統計情報</summary>
        public MonitoringStatistics? MonitoringStatistics { get; set; }

        /// <summary>PLC型式情報</summary>
        public string PlcTypeInfo { get; set; } = "未取得";

        /// <summary>総アクティブデバイス数</summary>
        public int TotalActiveDevices { get; set; }

        /// <summary>最後の探索実行時間</summary>
        public TimeSpan LastDiscoveryDuration { get; set; }

        /// <summary>
        /// 状態レポートの文字列表現
        /// </summary>
        /// <returns>状態レポート文字列</returns>
        public override string ToString()
        {
            var monitoringInfo = IsMonitoring ?
                $"監視中 ({MonitoredDeviceCount}対象/{ActiveDeviceCount}アクティブ)" : "待機中";

            var discoveryInfo = LastDiscoveryTime.HasValue ?
                $"最終探索: {LastDiscoveryTime:HH:mm:ss}" : "探索未実行";

            return $"システム状態: {CurrentState}, {monitoringInfo}, {discoveryInfo}, PLC: {PlcTypeInfo}";
        }

        #endregion
    }
}