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

    // Phase 2-2: GetMonitoringInterval()メソッド削除
    // MonitoringIntervalMsは各PlcConfigurationから直接取得するため不要

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

        // Phase 2-2: Excel設定から直接MonitoringIntervalMsを取得
        var interval = plcConfigs.Count > 0
            ? TimeSpan.FromMilliseconds(plcConfigs[0].MonitoringIntervalMs)
            : TimeSpan.FromMilliseconds(1000);  // デフォルト1秒
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
                Console.WriteLine($"[DEBUG] ReadRandomRequestInfo created:");
                Console.WriteLine($"[DEBUG]   DeviceSpecifications.Count: {readRandomRequestInfo.DeviceSpecifications.Count}");
                if (readRandomRequestInfo.DeviceSpecifications.Count > 0)
                {
                    Console.WriteLine($"[DEBUG]   First device: {readRandomRequestInfo.DeviceSpecifications[0].DeviceType}{readRandomRequestInfo.DeviceSpecifications[0].DeviceNumber}");
                }

                await (_loggingManager?.LogDebug($"[DEBUG] Step3-6: Starting full cycle for PLC #{i+1}") ?? Task.CompletedTask);
                var result = await manager.ExecuteFullCycleAsync(
                    connectionConfig,
                    timeoutConfig,
                    frame,
                    readRandomRequestInfo,
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
