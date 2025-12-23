using Andon.Core.Interfaces;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Infrastructure.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Andon.Core.Controllers;

/// <summary>
/// アプリケーション全体制御・継続実行モード
/// Phase3 Part7: ConfigurationWatcher統合
/// Phase 継続実行モード: PlcConfiguration参照保持（オプション3実装）
/// </summary>
public class ApplicationController : IApplicationController
{
    private readonly MultiPlcConfigManager _configManager;
    private readonly IExecutionOrchestrator _orchestrator;
    private readonly ILoggingManager _loggingManager;
    private readonly IConfigurationWatcher? _configurationWatcher;
    private readonly ConfigurationLoaderExcel? _configLoader; // Phase2 Step2-7追加
    private List<IPlcCommunicationManager>? _plcManagers;
    private List<PlcConfiguration>? _plcConfigs;
    private string _configDirectory = AppContext.BaseDirectory;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public ApplicationController(
        MultiPlcConfigManager configManager,
        IExecutionOrchestrator orchestrator,
        ILoggingManager loggingManager,
        IConfigurationWatcher? configurationWatcher = null,
        ConfigurationLoaderExcel? configLoader = null) // Phase2 Step2-7追加
    {
        _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _loggingManager = loggingManager ?? throw new ArgumentNullException(nameof(loggingManager));
        _configurationWatcher = configurationWatcher;
        _configLoader = configLoader; // Phase2 Step2-7追加

        // ConfigurationWatcherのイベントハンドラ登録
        if (_configurationWatcher != null)
        {
            _configurationWatcher.OnConfigurationChanged += HandleConfigurationChanged;
        }
    }

    /// <summary>
    /// テスト用: PlcManagersリストを取得
    /// Phase 2 TDDサイクル1
    /// </summary>
    internal List<IPlcCommunicationManager> GetPlcManagers() => _plcManagers ?? new List<IPlcCommunicationManager>();

    /// <summary>
    /// Step1初期化実行
    /// Phase 継続実行モード: PlcConfiguration保存追加
    /// </summary>
    public async Task<InitializationResult> ExecuteStep1InitializationAsync(
        string configDirectory = "./config/",
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _loggingManager.LogInfo("Starting Step1 initialization");

            // Phase2 Step2-7: ConfigurationLoaderExcelが注入されている場合、自動的に設定を読み込む
            if (_configLoader != null)
            {
                try
                {
                    _configLoader.LoadAllPlcConnectionConfigs();
                    await _loggingManager.LogInfo($"Loaded configuration files from {AppContext.BaseDirectory}");
                }
                catch (ArgumentException ex) when (ex.Message.Contains("設定ファイル(.xlsx)が見つかりません"))
                {
                    await _loggingManager.LogWarning($"No Excel configuration files found: {ex.Message}");
                    // 設定が0件でも続行（テスト環境等）
                }
            }

            var configs = _configManager.GetAllConfigurations();
            _plcConfigs = configs.ToList(); // Phase 継続実行モード: 設定情報を保持
            _plcManagers = new List<IPlcCommunicationManager>();

            // Phase 2 TDDサイクル1 Green: PlcCommunicationManager を設定ごとに初期化
            // Phase 3-4: 拡張メソッド使用（重複コード削減）
            // Phase 5.0: LoggingManager注入（本番統合対応）
            foreach (var config in configs)
            {
                var connectionConfig = config.ToConnectionConfig();
                var timeoutConfig = config.ToTimeoutConfig();

                var manager = new PlcCommunicationManager(
                    connectionConfig,
                    timeoutConfig,
                    bitExpansionSettings: null,
                    connectionResponse: null,
                    socketFactory: null,
                    loggingManager: _loggingManager);  // Phase 5.0: LoggingManager注入

                _plcManagers.Add(manager);
            }

            await _loggingManager.LogInfo("Step1 initialization completed");

            return new InitializationResult
            {
                Success = true,
                PlcCount = configs.Count
            };
        }
        catch (Exception ex)
        {
            await _loggingManager.LogError(ex, "Step1 initialization failed");
            return new InitializationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// 継続データサイクル開始
    /// Phase 継続実行モード: PlcConfiguration渡し追加
    /// Phase 4-2: ProgressReporter統合（進捗報告機能追加）
    /// </summary>
    public async Task StartContinuousDataCycleAsync(
        InitializationResult initResult,
        CancellationToken cancellationToken)
    {
        await _loggingManager.LogDebug($"[DEBUG] StartContinuousDataCycleAsync called - initResult.Success={initResult.Success}, _plcManagers={_plcManagers?.Count ?? 0}, _plcConfigs={_plcConfigs?.Count ?? 0}");
        Console.WriteLine($"[DEBUG] StartContinuousDataCycleAsync called - initResult.Success={initResult.Success}, _plcManagers={_plcManagers?.Count ?? 0}, _plcConfigs={_plcConfigs?.Count ?? 0}");

        if (!initResult.Success || _plcManagers == null || _plcConfigs == null)
        {
            await _loggingManager.LogError(null, $"[ERROR] Cannot start cycle: initResult.Success={initResult.Success}, _plcManagers={((_plcManagers == null) ? "null" : _plcManagers.Count.ToString())}, _plcConfigs={((_plcConfigs == null) ? "null" : _plcConfigs.Count.ToString())}");
            Console.WriteLine($"[ERROR] Cannot start cycle: initResult.Success={initResult.Success}, _plcManagers={((_plcManagers == null) ? "null" : _plcManagers.Count.ToString())}, _plcConfigs={((_plcConfigs == null) ? "null" : _plcConfigs.Count.ToString())}");
            return;
        }

        await _loggingManager.LogInfo("Starting continuous data cycle");

        // Phase 4-2 Green: 進捗報告統合
        var progressReporter = new Services.ProgressReporter<ProgressInfo>(_loggingManager);
        var progress = new Progress<ProgressInfo>(progressReporter.Report);

        await _orchestrator.RunContinuousDataCycleAsync(_plcConfigs, _plcManagers, cancellationToken, progress);
    }

    /// <summary>
    /// アプリケーション開始（Step1初期化と継続実行を統合）
    /// Phase3 Part7: ConfigurationWatcher監視開始追加
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var initResult = await ExecuteStep1InitializationAsync(cancellationToken: cancellationToken);

        // ConfigurationWatcher監視開始
        if (_configurationWatcher != null)
        {
            _configurationWatcher.StartWatchingExcel(_configDirectory);
            await _loggingManager.LogInfo($"Started monitoring configuration directory: {_configDirectory}");
        }

        await StartContinuousDataCycleAsync(initResult, cancellationToken);
    }

    /// <summary>
    /// アプリケーション停止とリソース解放
    /// Phase3 Part7: ConfigurationWatcher監視停止追加
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _loggingManager.LogInfo("Stopping application");

        // ConfigurationWatcher監視停止
        if (_configurationWatcher != null)
        {
            _configurationWatcher.StopWatching();
            await _loggingManager.LogInfo("Stopped configuration monitoring");
        }

        // Phase 4-4 Green: PLCマネージャーのリソース解放
        if (_plcManagers != null && _plcManagers.Count > 0)
        {
            await _loggingManager.LogInfo($"Releasing {_plcManagers.Count} PLC manager(s)...");

            foreach (var manager in _plcManagers)
            {
                try
                {
                    // IDisposableを実装している場合はDispose
                    if (manager is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    await _loggingManager.LogError(ex, "Failed to dispose PLC manager");
                }
            }

            _plcManagers.Clear();
            _plcManagers = null;

            await _loggingManager.LogInfo("All PLC managers released");
        }

        await _loggingManager.LogInfo("Application stopped successfully");
    }

    /// <summary>
    /// 設定ファイル変更イベントハンドラー
    /// Phase3 Part7実装
    /// </summary>
    private async void HandleConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        try
        {
            await _loggingManager.LogInfo($"Configuration file changed: {e.FilePath}");

            // Phase 4-3 Green (Option B): 全設定を再読み込み
            // ExecuteStep1InitializationAsyncが内部で以下を実行:
            // 1. ConfigurationLoaderExcel.LoadAllPlcConnectionConfigs()
            // 2. MultiPlcConfigManagerへの設定反映
            // 3. PlcCommunicationManager再初期化
            await ExecuteStep1InitializationAsync(_configDirectory, CancellationToken.None);

            await _loggingManager.LogInfo("Configuration reloaded successfully after file change");
        }
        catch (Exception ex)
        {
            await _loggingManager.LogError(ex, "Failed to handle configuration change");
        }
    }
}
