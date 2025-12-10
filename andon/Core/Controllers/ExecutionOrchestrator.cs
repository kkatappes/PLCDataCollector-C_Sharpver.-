using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Andon.Core.Controllers;

/// <summary>
/// Step2-7データ処理サイクル実行制御（Excel設定ベース）
/// PlcConfigurationモデルを使用した統一設計
/// ⚠️ 注意: PlcConnectionConfig、MultiPlcConfig、DeviceEntryは削除済み（JSON設定廃止により不要）
/// Phase 2-2完了: IOptions&lt;DataProcessingConfig&gt;依存を削除し、各PlcConfiguration.MonitoringIntervalMsから直接監視間隔を取得
/// </summary>
public class ExecutionOrchestrator : Interfaces.IExecutionOrchestrator
{
    // Phase 2-2: IOptions<DataProcessingConfig> _dataProcessingConfig フィールド削除
    // MonitoringIntervalMsは各PlcConfigurationから直接取得
    private readonly Interfaces.ITimerService? _timerService;
    private readonly Interfaces.IConfigToFrameManager? _configToFrameManager;
    private readonly Interfaces.IDataOutputManager? _dataOutputManager;
    private readonly Interfaces.ILoggingManager? _loggingManager;
    // Phase 4-1: ParallelExecutionController統合
    private readonly Interfaces.IParallelExecutionController? _parallelController;

    /// <summary>
    /// コンストラクタ（Phase 1 Step 1-2対応）
    /// Phase 2-2: IOptions<DataProcessingConfig>依存を削除
    /// </summary>
    public ExecutionOrchestrator()
    {
        // デフォルトコンストラクタ（既存のテストとの互換性維持）
        // Phase 2-2: _dataProcessingConfig初期化削除
    }

    /// <summary>
    /// コンストラクタ（DIコンテナ対応、TimerService対応）
    /// Phase 2-2: IOptions<DataProcessingConfig>依存を削除
    /// </summary>
    public ExecutionOrchestrator(Interfaces.ITimerService timerService)
    {
        _timerService = timerService;
    }

    /// <summary>
    /// コンストラクタ（Phase12テスト用、TimerService不要）
    /// Phase 2-2: IOptions<DataProcessingConfig>依存を削除
    /// </summary>
    public ExecutionOrchestrator(
        Interfaces.IConfigToFrameManager configToFrameManager,
        Interfaces.IDataOutputManager dataOutputManager,
        Interfaces.ILoggingManager loggingManager)
    {
        _configToFrameManager = configToFrameManager;
        _dataOutputManager = dataOutputManager;
        _loggingManager = loggingManager;
    }

    /// <summary>
    /// コンストラクタ（Phase 継続実行モード対応、完全依存性注入）
    /// Phase 2-2: IOptions<DataProcessingConfig>依存を削除
    /// </summary>
    public ExecutionOrchestrator(
        Interfaces.ITimerService timerService,
        Interfaces.IConfigToFrameManager configToFrameManager,
        Interfaces.IDataOutputManager dataOutputManager,
        Interfaces.ILoggingManager loggingManager)
    {
        _timerService = timerService;
        _configToFrameManager = configToFrameManager;
        _dataOutputManager = dataOutputManager;
        _loggingManager = loggingManager;
    }

    /// <summary>
    /// コンストラクタ（Phase 4-1: ParallelExecutionController統合、完全依存性注入）
    /// </summary>
    public ExecutionOrchestrator(
        Interfaces.ITimerService timerService,
        Interfaces.IConfigToFrameManager configToFrameManager,
        Interfaces.IDataOutputManager dataOutputManager,
        Interfaces.ILoggingManager loggingManager,
        Interfaces.IParallelExecutionController parallelController)
    {
        _timerService = timerService;
        _configToFrameManager = configToFrameManager;
        _dataOutputManager = dataOutputManager;
        _loggingManager = loggingManager;
        _parallelController = parallelController;
    }

    // Phase 2-2: GetMonitoringInterval()メソッド削除
    // MonitoringIntervalMsは各PlcConfigurationから直接取得するため不要

    /// <summary>
    /// 継続データサイクル実行（Phase 1 Step 1-2 TDDサイクル2）
    /// Phase 継続実行モード: PlcConfiguration追加
    /// </summary>
    public async Task RunContinuousDataCycleAsync(
        List<PlcConfiguration> plcConfigs,
        List<Interfaces.IPlcCommunicationManager> plcManagers,
        CancellationToken cancellationToken,
        IProgress<ProgressInfo>? progress = null) // Phase 4-2: 進捗報告パラメータ追加
    {
        await (_loggingManager?.LogDebug($"[DEBUG] RunContinuousDataCycleAsync called - plcConfigs={plcConfigs?.Count ?? 0}, plcManagers={plcManagers?.Count ?? 0}") ?? Task.CompletedTask);
        Console.WriteLine($"[DEBUG] RunContinuousDataCycleAsync called - plcConfigs={plcConfigs?.Count ?? 0}, plcManagers={plcManagers?.Count ?? 0}");

        if (_timerService == null)
        {
            await (_loggingManager?.LogError(null, "[ERROR] TimerService is null") ?? Task.CompletedTask);
            Console.WriteLine("[ERROR] TimerService is null");
            throw new InvalidOperationException("TimerServiceが設定されていません");
        }

        // Phase 2-2: Excel設定から直接MonitoringIntervalMsを取得
        var interval = plcConfigs.Count > 0
            ? TimeSpan.FromMilliseconds(plcConfigs[0].MonitoringIntervalMs)
            : TimeSpan.FromMilliseconds(1000);  // デフォルト1秒
        await (_loggingManager?.LogInfo($"[INFO] Starting timer with interval: {interval.TotalMilliseconds}ms") ?? Task.CompletedTask);
        Console.WriteLine($"[INFO] Starting timer with interval: {interval.TotalMilliseconds}ms");

        // Phase 4-2: 開始時の進捗報告
        progress?.Report(new ProgressInfo(
            "Continuous Data Cycle",
            0.0,
            $"Starting continuous cycle with interval {interval.TotalMilliseconds}ms",
            TimeSpan.Zero));

        await _timerService.StartPeriodicExecution(
            async () =>
            {
                // 各サイクル実行時の進捗報告
                progress?.Report(new ProgressInfo(
                    "Executing Cycle",
                    0.5,
                    $"Executing cycle for {plcConfigs.Count} PLC(s)",
                    TimeSpan.Zero));

                // Phase 4-2 Fix: 型変換アダプター実装
                // ParallelProgressInfo → ProgressInfo 変換
                IProgress<ParallelProgressInfo>? parallelProgress = null;
                if (progress != null)
                {
                    parallelProgress = new Progress<ParallelProgressInfo>(info =>
                    {
                        // 各PLCの進捗の平均を計算
                        var overallProgress = info.PlcProgresses.Count > 0
                            ? info.PlcProgresses.Values.Average()
                            : 0.0;

                        // ParallelProgressInfoをProgressInfoに変換して報告
                        progress.Report(new ProgressInfo(
                            info.CurrentStep,
                            overallProgress,
                            $"{info.CurrentStep} - {info.PlcProgresses.Count} PLCs",
                            info.ElapsedTime));
                    });
                }

                await ExecuteMultiPlcCycleAsync_Internal(plcConfigs, plcManagers, cancellationToken, parallelProgress);

                // サイクル完了時の進捗報告
                progress?.Report(new ProgressInfo(
                    "Cycle Complete",
                    1.0,
                    $"Cycle completed for {plcConfigs.Count} PLC(s)",
                    TimeSpan.Zero));
            },
            interval,
            cancellationToken);
    }

    /// <summary>
    /// 継続データサイクル実行（後方互換性維持用オーバーロード）
    /// </summary>
    public async Task RunContinuousDataCycleAsync(
        List<PlcConfiguration> plcConfigs,
        List<Interfaces.IPlcCommunicationManager> plcManagers,
        CancellationToken cancellationToken)
    {
        await RunContinuousDataCycleAsync(plcConfigs, plcManagers, cancellationToken, progress: null);
    }

    /// <summary>
    /// テスト用: ExecuteMultiPlcCycleAsync_Internal() の公開ラッパー
    /// Phase 継続実行モード Phase 1-1 Green
    /// </summary>
    /// <remarks>
    /// 本番コード: internal または削除予定
    /// テスト目的: 周期実行ロジックの単体テスト
    /// </remarks>
    internal async Task ExecuteSingleCycleAsync(
        List<PlcConfiguration> plcConfigs,
        List<Interfaces.IPlcCommunicationManager> plcManagers,
        CancellationToken cancellationToken,
        IProgress<ParallelProgressInfo>? progress = null) // Phase 4-2: 進捗報告パラメータ追加
    {
        await ExecuteMultiPlcCycleAsync_Internal(plcConfigs, plcManagers, cancellationToken, progress);
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
        CancellationToken cancellationToken,
        IProgress<ParallelProgressInfo>? progress = null) // Phase 4-2: 進捗報告パラメータ追加
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

        // Phase 4-2: 開始時の進捗報告
        if (progress != null)
        {
            var initialPlcProgresses = new Dictionary<string, double>();
            for (int i = 0; i < plcManagers.Count; i++)
            {
                initialPlcProgresses[$"PLC{i + 1}"] = 0.0;
            }
            var initialProgress = new ParallelProgressInfo(
                "Multi-PLC Cycle - Starting",
                initialPlcProgresses,
                TimeSpan.Zero);
            progress.Report(initialProgress);
        }

        // Phase 4-1: ParallelExecutionController使用時は並行実行
        if (_parallelController != null)
        {
            await (_loggingManager?.LogDebug("[DEBUG] Using ParallelExecutionController for concurrent execution") ?? Task.CompletedTask);
            Console.WriteLine("[DEBUG] Using ParallelExecutionController for concurrent execution");

            // PlcConfiguration と IPlcCommunicationManager のペアを作成
            var plcPairs = plcConfigs.Zip(plcManagers, (config, manager) => new { Config = config, Manager = manager }).ToList();

            // 並行実行
            var parallelResult = await _parallelController.ExecuteParallelPlcOperationsAsync(
                plcPairs,
                async (pair, ct) => await ExecuteSinglePlcCycleAsync((dynamic)pair, ct),
                cancellationToken);

            await (_loggingManager?.LogInfo($"[INFO] Parallel execution completed - Success: {parallelResult.SuccessfulPlcCount}/{parallelResult.TotalPlcCount}") ?? Task.CompletedTask);
            Console.WriteLine($"[INFO] Parallel execution completed - Success: {parallelResult.SuccessfulPlcCount}/{parallelResult.TotalPlcCount}");

            // Phase 4-2: 完了時の進捗報告
            if (progress != null)
            {
                var completedPlcProgresses = new Dictionary<string, double>();
                for (int i = 0; i < plcManagers.Count; i++)
                {
                    completedPlcProgresses[$"PLC{i + 1}"] = 1.0;
                }
                var completionProgress = new ParallelProgressInfo(
                    "Multi-PLC Cycle - Completed",
                    completedPlcProgresses,
                    TimeSpan.Zero);
                progress.Report(completionProgress);
            }
        }
        else
        {
            // Phase 1-2 Green: 順次実行（後方互換性維持）
            await (_loggingManager?.LogDebug("[DEBUG] Using sequential execution (ParallelExecutionController not available)") ?? Task.CompletedTask);
            Console.WriteLine("[DEBUG] Using sequential execution (ParallelExecutionController not available)");

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

                    await ExecuteSinglePlcCycleInternalAsync(i, manager, config, cancellationToken);
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
    }

    /// <summary>
    /// 並行実行用: 単一PLCサイクル実行
    /// Phase 4-1: ParallelExecutionController統合
    /// </summary>
    private async Task<CycleExecutionResult> ExecuteSinglePlcCycleAsync(
        dynamic plcPair,
        CancellationToken cancellationToken)
    {
        var config = (PlcConfiguration)plcPair.Config;
        var manager = (Interfaces.IPlcCommunicationManager)plcPair.Manager;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await ExecuteSinglePlcCycleInternalAsync(-1, manager, config, cancellationToken);

            return new CycleExecutionResult
            {
                IsSuccess = true,
                CompletedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            await (_loggingManager?.LogError(ex, $"[ERROR] PLC cycle failed - IP:{config.IpAddress}:{config.Port}") ?? Task.CompletedTask);
            return new CycleExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = $"PLC {config.IpAddress}:{config.Port} - {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 内部用: 単一PLCサイクルの実際の処理
    /// Phase 4-1: 共通処理を抽出
    /// </summary>
    private async Task ExecuteSinglePlcCycleInternalAsync(
        int plcIndex,
        Interfaces.IPlcCommunicationManager manager,
        PlcConfiguration config,
        CancellationToken cancellationToken)
    {
        // Step2: フレーム構築（Phase 1-3実装）
        await (_loggingManager?.LogDebug($"[DEBUG] Step2: Building frame for PLC {config.IpAddress}:{config.Port}") ?? Task.CompletedTask);
        var frame = _configToFrameManager!.BuildReadRandomFrameFromConfig(config);
        await (_loggingManager?.LogDebug($"[DEBUG] Step2: Frame built successfully, size={frame.Length} bytes") ?? Task.CompletedTask);

        // Step3-6: 完全サイクル実行
        // Phase 3-4: 拡張メソッド使用（重複コード削減）
        var connectionConfig = config.ToConnectionConfig();
        var timeoutConfig = config.ToTimeoutConfig();

        // Phase12恒久対策: ReadRandomRequestInfo生成（ReadRandom(0x0403)専用）
        var readRandomRequestInfo = new ReadRandomRequestInfo
        {
            DeviceSpecifications = config.Devices?.ToList() ?? new List<DeviceSpecification>(),
            FrameType = config.FrameVersion == "4E" ? FrameType.Frame4E : FrameType.Frame3E,
            RequestedAt = DateTime.UtcNow
        };

        // デバッグログ追加（Phase12実機確認用）
        if (plcIndex >= 0)
        {
            Console.WriteLine($"[DEBUG] ReadRandomRequestInfo created for PLC #{plcIndex+1}:");
        }
        else
        {
            Console.WriteLine($"[DEBUG] ReadRandomRequestInfo created:");
        }
        Console.WriteLine($"[DEBUG]   DeviceSpecifications.Count: {readRandomRequestInfo.DeviceSpecifications.Count}");
        if (readRandomRequestInfo.DeviceSpecifications.Count > 0)
        {
            Console.WriteLine($"[DEBUG]   First device: {readRandomRequestInfo.DeviceSpecifications[0].DeviceType}{readRandomRequestInfo.DeviceSpecifications[0].DeviceNumber}");
        }

        await (_loggingManager?.LogDebug($"[DEBUG] Step3-6: Starting full cycle for PLC {config.IpAddress}:{config.Port}") ?? Task.CompletedTask);
        var result = await manager.ExecuteFullCycleAsync(
            connectionConfig,
            timeoutConfig,
            frame,
            readRandomRequestInfo,
            cancellationToken);

        if (result.IsSuccess)
        {
            await (_loggingManager?.LogDebug($"[DEBUG] Step3-6: Full cycle completed successfully for PLC {config.IpAddress}:{config.Port}") ?? Task.CompletedTask);
        }
        else
        {
            await (_loggingManager?.LogError(null, $"[ERROR] Step3-6: Full cycle failed for PLC {config.IpAddress}:{config.Port} - {result.ErrorMessage}") ?? Task.CompletedTask);
        }

        // Phase 5.0: 代替プロトコル情報のログ出力
        // Phase 5.0-Refactor: ErrorMessages.cs使用（マジックストリング削除）
        if (result.IsSuccess && result.ConnectResult != null)
        {
            if (result.ConnectResult.IsFallbackConnection)
            {
                await (_loggingManager?.LogInfo(
                    Constants.ErrorMessages.FallbackConnectionSummary(
                        plcIndex >= 0 ? plcIndex + 1 : 0,
                        result.ConnectResult.UsedProtocol,
                        result.ConnectResult.FallbackErrorDetails ?? "不明"))
                    ?? Task.CompletedTask);
            }
            else
            {
                await (_loggingManager?.LogDebug(
                    Constants.ErrorMessages.InitialProtocolConnectionSummary(
                        plcIndex >= 0 ? plcIndex + 1 : 0,
                        result.ConnectResult.UsedProtocol))
                    ?? Task.CompletedTask);
            }
        }

        // Step7: データ出力（Phase 1-4実装）
        if (result.IsSuccess && result.ProcessedData != null)
        {
            // Phase 2-4: Excel設定のSavePathを使用
            var outputDirectory = string.IsNullOrWhiteSpace(config.SavePath)
                ? "./output"
                : config.SavePath;

            // ディレクトリが存在しない場合は作成
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // TODO: Phase 1-4 Refactor - deviceConfigをPlcConfiguration.Devicesから構築
            var deviceConfig = new Dictionary<string, DeviceEntryInfo>();  // 仮実装

            _dataOutputManager?.OutputToJson(
                result.ProcessedData,
                outputDirectory,
                config.IpAddress,
                config.Port,
                config.PlcModel,  // Phase 2-3: PlcModelをJSON出力に追加
                deviceConfig);
        }
    }
}
