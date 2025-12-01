using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Utilities;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Andon.Core.Controllers;

/// <summary>
/// Step2-7データ処理サイクル実行制御
/// </summary>
public class ExecutionOrchestrator : Interfaces.IExecutionOrchestrator
{
    private readonly IOptions<DataProcessingConfig> _dataProcessingConfig;
    private readonly Interfaces.ITimerService? _timerService;
    private readonly Interfaces.IConfigToFrameManager? _configToFrameManager;
    private readonly Interfaces.IDataOutputManager? _dataOutputManager;
    private readonly Interfaces.ILoggingManager? _loggingManager;

    /// <summary>
    /// コンストラクタ（Phase 1 Step 1-2対応）
    /// </summary>
    public ExecutionOrchestrator()
    {
        // デフォルトコンストラクタ（既存のテストとの互換性維持）
        _dataProcessingConfig = Options.Create(new DataProcessingConfig());
    }

    /// <summary>
    /// コンストラクタ（DIコンテナ対応、Phase 1 Step 1-2対応）
    /// </summary>
    public ExecutionOrchestrator(IOptions<DataProcessingConfig> dataProcessingConfig)
    {
        _dataProcessingConfig = dataProcessingConfig;
    }

    /// <summary>
    /// コンストラクタ（DIコンテナ対応、Phase 1 Step 1-2対応、TimerService対応）
    /// </summary>
    public ExecutionOrchestrator(
        Interfaces.ITimerService timerService,
        IOptions<DataProcessingConfig> dataProcessingConfig)
    {
        _timerService = timerService;
        _dataProcessingConfig = dataProcessingConfig;
    }

    /// <summary>
    /// コンストラクタ（Phase 継続実行モード対応、完全依存性注入）
    /// </summary>
    public ExecutionOrchestrator(
        Interfaces.ITimerService timerService,
        IOptions<DataProcessingConfig> dataProcessingConfig,
        Interfaces.IConfigToFrameManager configToFrameManager,
        Interfaces.IDataOutputManager dataOutputManager,
        Interfaces.ILoggingManager loggingManager)
    {
        _timerService = timerService;
        _dataProcessingConfig = dataProcessingConfig;
        _configToFrameManager = configToFrameManager;
        _dataOutputManager = dataOutputManager;
        _loggingManager = loggingManager;
    }

    /// <summary>
    /// 監視間隔取得（Phase 1 Step 1-2 TDDサイクル1）
    /// </summary>
    public TimeSpan GetMonitoringInterval()
    {
        var intervalMs = _dataProcessingConfig.Value.MonitoringIntervalMs;
        return TimeSpan.FromMilliseconds(intervalMs);
    }

    /// <summary>
    /// 継続データサイクル実行（Phase 1 Step 1-2 TDDサイクル2）
    /// Phase 継続実行モード: PlcConfiguration追加
    /// </summary>
    public async Task RunContinuousDataCycleAsync(
        List<PlcConfiguration> plcConfigs,
        List<Interfaces.IPlcCommunicationManager> plcManagers,
        CancellationToken cancellationToken)
    {
        await (_loggingManager?.LogDebug($"[DEBUG] RunContinuousDataCycleAsync called - plcConfigs={plcConfigs?.Count ?? 0}, plcManagers={plcManagers?.Count ?? 0}") ?? Task.CompletedTask);
        Console.WriteLine($"[DEBUG] RunContinuousDataCycleAsync called - plcConfigs={plcConfigs?.Count ?? 0}, plcManagers={plcManagers?.Count ?? 0}");

        if (_timerService == null)
        {
            await (_loggingManager?.LogError(null, "[ERROR] TimerService is null") ?? Task.CompletedTask);
            Console.WriteLine("[ERROR] TimerService is null");
            throw new InvalidOperationException("TimerServiceが設定されていません");
        }

        var interval = GetMonitoringInterval();
        await (_loggingManager?.LogInfo($"[INFO] Starting timer with interval: {interval.TotalMilliseconds}ms") ?? Task.CompletedTask);
        Console.WriteLine($"[INFO] Starting timer with interval: {interval.TotalMilliseconds}ms");

        await _timerService.StartPeriodicExecution(
            async () => await ExecuteMultiPlcCycleAsync_Internal(plcConfigs, plcManagers, cancellationToken),
            interval,
            cancellationToken);
    }

    /// <summary>
    /// テスト用: ExecuteMultiPlcCycleAsync_Internal() の公開ラッパー
    /// Phase 継続実行モード Phase 1-1 Green
    /// </summary>
    /// <remarks>
    /// 本番コード: internal または削除予定
    /// テスト目的: 周期実行ロジックの単体テスト
    /// </remarks>
    public async Task ExecuteSingleCycleAsync(
        List<PlcConfiguration> plcConfigs,
        List<Interfaces.IPlcCommunicationManager> plcManagers,
        CancellationToken cancellationToken)
    {
        await ExecuteMultiPlcCycleAsync_Internal(plcConfigs, plcManagers, cancellationToken);
    }

    /// <summary>
    /// 内部用：複数PLC並列実行（Phase 1 Step 1-2対応）
    /// Phase 継続実行モード: PlcConfiguration追加
    /// Phase 1-1 Green: 最小限実装（単一PLC）
    /// Phase 1-1 Refactor: エラーハンドリング追加
    /// Phase 1-2 Green: 複数PLC対応（foreachループ）
    /// </summary>
    private async Task ExecuteMultiPlcCycleAsync_Internal(
        List<PlcConfiguration> plcConfigs,
        List<Interfaces.IPlcCommunicationManager> plcManagers,
        CancellationToken cancellationToken)
    {
        await (_loggingManager?.LogDebug($"[DEBUG] ExecuteMultiPlcCycleAsync_Internal called - plcManagers={plcManagers?.Count ?? 0}, plcConfigs={plcConfigs?.Count ?? 0}") ?? Task.CompletedTask);
        Console.WriteLine($"[DEBUG] ExecuteMultiPlcCycleAsync_Internal called - plcManagers={plcManagers?.Count ?? 0}, plcConfigs={plcConfigs?.Count ?? 0}");

        // Phase 1-1 Refactor: 入力検証
        if (plcManagers == null || plcManagers.Count == 0)
        {
            await (_loggingManager?.LogError(null, "[ERROR] plcManagers is null or empty") ?? Task.CompletedTask);
            Console.WriteLine("[ERROR] plcManagers is null or empty");
            return;
        }

        if (plcConfigs == null || plcConfigs.Count == 0)
        {
            await (_loggingManager?.LogError(null, "[ERROR] plcConfigs is null or empty") ?? Task.CompletedTask);
            Console.WriteLine("[ERROR] plcConfigs is null or empty");
            return;
        }

        if (plcManagers.Count != plcConfigs.Count)
        {
            await (_loggingManager?.LogError(null, $"[ERROR] plcManagers.Count({plcManagers.Count}) != plcConfigs.Count({plcConfigs.Count})") ?? Task.CompletedTask);
            Console.WriteLine($"[ERROR] plcManagers.Count({plcManagers.Count}) != plcConfigs.Count({plcConfigs.Count})");
            return;
        }

        await (_loggingManager?.LogInfo($"[INFO] Starting PLC cycle for {plcManagers.Count} PLC(s)") ?? Task.CompletedTask);
        Console.WriteLine($"[INFO] Starting PLC cycle for {plcManagers.Count} PLC(s)");

        // Phase 1-2 Green: foreachループですべてのPLCを処理
        for (int i = 0; i < plcManagers.Count; i++)
        {
            var manager = plcManagers[i];
            var config = plcConfigs[i];

            try
            {
                // キャンセルチェック
                cancellationToken.ThrowIfCancellationRequested();

                // デバッグログ: サイクル開始
                await (_loggingManager?.LogDebug($"[DEBUG] PLC #{i+1} cycle starting - IP:{config.IpAddress}:{config.Port}") ?? Task.CompletedTask);

                // Step2: フレーム構築（Phase 1-3実装）
                await (_loggingManager?.LogDebug($"[DEBUG] Step2: Building frame for PLC #{i+1}") ?? Task.CompletedTask);
                var frame = _configToFrameManager!.BuildReadRandomFrameFromConfig(config);
                await (_loggingManager?.LogDebug($"[DEBUG] Step2: Frame built successfully, size={frame.Length} bytes") ?? Task.CompletedTask);

                // Step3-6: 完全サイクル実行
                var connectionConfig = new ConnectionConfig
                {
                    IpAddress = config.IpAddress,
                    Port = config.Port,
                    UseTcp = config.ConnectionMethod == "TCP",
                    IsBinary = config.IsBinary  // Binary/ASCII形式を設定から適用
                };

                var timeoutConfig = new TimeoutConfig
                {
                    ConnectTimeoutMs = config.Timeout,
                    SendTimeoutMs = config.Timeout,
                    ReceiveTimeoutMs = config.Timeout
                };

                // Phase8.5暫定対策: PlcConfigurationからDeviceSpecificationsを設定
                var deviceRequestInfo = new ProcessedDeviceRequestInfo
                {
                    DeviceSpecifications = config.Devices?.ToList(), // ReadRandom用デバイス指定
                    FrameType = config.FrameVersion == "4E" ? FrameType.Frame4E : FrameType.Frame3E,
                    RequestedAt = DateTime.UtcNow
                };

                await (_loggingManager?.LogDebug($"[DEBUG] Step3-6: Starting full cycle for PLC #{i+1}") ?? Task.CompletedTask);
                var result = await manager.ExecuteFullCycleAsync(
                    connectionConfig,
                    timeoutConfig,
                    frame,
                    deviceRequestInfo,
                    cancellationToken);

                if (result.IsSuccess)
                {
                    await (_loggingManager?.LogDebug($"[DEBUG] Step3-6: Full cycle completed successfully for PLC #{i+1}") ?? Task.CompletedTask);
                }
                else
                {
                    await (_loggingManager?.LogError(null, $"[ERROR] Step3-6: Full cycle failed for PLC #{i+1} - {result.ErrorMessage}") ?? Task.CompletedTask);
                }

                // Step7: データ出力（Phase 1-4実装）
                if (result.IsSuccess && result.ProcessedData != null)
                {
                    // TODO: Phase 1-4 Refactor - outputDirectoryを設定から取得
                    var outputDirectory = "C:/Users/PPESAdmin/Desktop/x/output";  // 実機環境の出力先パス

                    // TODO: Phase 1-4 Refactor - deviceConfigをPlcConfiguration.Devicesから構築
                    var deviceConfig = new Dictionary<string, DeviceEntryInfo>();  // 仮実装

                    _dataOutputManager?.OutputToJson(
                        result.ProcessedData,
                        outputDirectory,
                        config.IpAddress,
                        config.Port,
                        deviceConfig);
                }
            }
            catch (OperationCanceledException)
            {
                // キャンセル要求は正常なフロー
                await (_loggingManager?.LogInfo($"[INFO] PLC #{i+1} cycle cancelled") ?? Task.CompletedTask);
                throw;
            }
            catch (Exception ex)
            {
                // エラー発生時もログ出力して継続
                // 継続実行モードでは、1つのPLCでエラーが発生しても他のPLCは処理継続
                // ここでは例外を再スローしない
                await (_loggingManager?.LogError(ex, $"[ERROR] PLC #{i+1} cycle failed - IP:{config.IpAddress}:{config.Port}") ?? Task.CompletedTask);
                Console.WriteLine($"[ERROR] PLC #{i+1} cycle failed - IP:{config.IpAddress}:{config.Port} - Exception: {ex.Message}");
                Console.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
            }
        }
    }
    /// <summary>
    /// 複数PLCサイクル実行（並列/順次）
    /// </summary>
    /// <param name="config">複数PLC設定</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>複数PLC実行結果</returns>
    public async Task<MultiPlcExecutionResult> ExecuteMultiPlcCycleAsync(
        MultiPlcConfig config,
        CancellationToken cancellationToken = default)
    {
        var overallStartTime = DateTime.UtcNow;
        List<PlcExecutionResult> plcResults;

        // 並列 vs 順次処理の振り分け
        if (config.ParallelConfig.EnableParallel)
        {
            plcResults = await MultiPlcCoordinator.ExecuteParallelAsync(
                config.PlcConnections,
                config.ParallelConfig,
                ExecuteSinglePlcAsync,
                cancellationToken
            );
        }
        else
        {
            plcResults = await MultiPlcCoordinator.ExecuteSequentialAsync(
                config.PlcConnections,
                ExecuteSinglePlcAsync,
                cancellationToken
            );
        }

        // 結果集計
        var result = new MultiPlcExecutionResult
        {
            StartTime = overallStartTime,
            EndTime = DateTime.UtcNow,
            PlcResults = plcResults.ToDictionary(r => r.PlcId, r => r),
            SuccessCount = plcResults.Count(r => r.IsSuccess),
            FailureCount = plcResults.Count(r => !r.IsSuccess),
            IsSuccess = plcResults.All(r => r.IsSuccess)
        };

        result.TotalDuration = result.EndTime - result.StartTime;
        return result;
    }

    /// <summary>
    /// 単一PLC処理（PlcCommunicationManagerを活用）
    /// </summary>
    private async Task<PlcExecutionResult> ExecuteSinglePlcAsync(
        PlcConnectionConfig plcConfig,
        CancellationToken cancellationToken)
    {
        var result = new PlcExecutionResult
        {
            PlcId = plcConfig.PlcId,
            PlcName = plcConfig.PlcName,
            StartTime = DateTime.UtcNow
        };

        try
        {
            var connectionConfig = new ConnectionConfig
            {
                IpAddress = plcConfig.IPAddress,
                Port = plcConfig.Port,
                UseTcp = plcConfig.ConnectionMethod == "TCP",
                IsBinary = plcConfig.IsBinary  // Binary/ASCII形式を設定から適用
            };

            var timeoutConfig = new TimeoutConfig
            {
                ConnectTimeoutMs = plcConfig.Timeout,
                SendTimeoutMs = plcConfig.Timeout,
                ReceiveTimeoutMs = plcConfig.Timeout
            };

            // 既存のPlcCommunicationManagerを使用（コンストラクタに設定を渡す）
            var manager = new PlcCommunicationManager(
                connectionConfig,
                timeoutConfig
            );

            // フレーム構築（既存ユーティリティ使用）
            var devices = plcConfig.Devices.Select(d => d.ToDeviceSpecification()).ToList();
            var frame = SlmpFrameBuilder.BuildReadRandomRequest(
                devices,
                plcConfig.FrameVersion,  // "3E" or "4E"
                (ushort)(plcConfig.Timeout / 250)
            );

            // 通信実行（既存メソッド活用）
            var cycleResult = await manager.ExecuteStep3to5CycleAsync(
                connectionConfig,
                timeoutConfig,
                frame,
                cancellationToken
            );

            result.IsSuccess = cycleResult.IsSuccess;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
            result.DeviceData = cycleResult.ReceiveResult?.ResponseData;  // RawDataではなくResponseData
            result.ErrorMessage = cycleResult.ErrorMessage;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"エラー: {ex.Message}";
            result.Exception = ex;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
        }

        return result;
    }
}
