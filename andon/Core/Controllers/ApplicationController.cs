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
    private string _configDirectory = "./config/";

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
    public List<IPlcCommunicationManager> GetPlcManagers() => _plcManagers ?? new List<IPlcCommunicationManager>();

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
            foreach (var config in configs)
            {
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

                var manager = new PlcCommunicationManager(
                    connectionConfig,
                    timeoutConfig);

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
        await _orchestrator.RunContinuousDataCycleAsync(_plcConfigs, _plcManagers, cancellationToken);
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

        // TODO: Phase 2でリソース解放処理を拡張
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

            // TODO: Phase3 Part7 - 動的再読み込み処理を実装
            // 1. 変更されたExcelファイルの再読み込み
            // 2. MultiPlcConfigManagerへの設定反映
            // 3. PlcCommunicationManager再初期化（通信サイクル考慮）
        }
        catch (Exception ex)
        {
            await _loggingManager.LogError(ex, "Failed to handle configuration change");
        }
    }
}
