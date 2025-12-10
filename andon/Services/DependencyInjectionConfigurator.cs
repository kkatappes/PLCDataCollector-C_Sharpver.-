using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Andon.Core.Interfaces;
using Andon.Core.Controllers;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Infrastructure.Configuration;

namespace Andon.Services;

/// <summary>
/// DIコンテナ設定
/// </summary>

public static class DependencyInjectionConfigurator
{
    public static void Configure(IServiceCollection services, IConfiguration configuration)
    {
        // Logging設定
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Phase 2-1完了: LoggingConfigはハードコード化されたため、DI登録不要
        // Phase 2-2完了: DataProcessingConfig(MonitoringIntervalMs)は各PlcConfigurationから取得するため、DI登録不要

        // Singleton登録 - アプリケーション全体で1つのインスタンスを共有
        services.AddSingleton<IApplicationController, ApplicationController>();
        services.AddSingleton<ILoggingManager, LoggingManager>();
        services.AddSingleton<IErrorHandler, ErrorHandler>();

        // Part8追加: Phase3実装クラス(Singleton)
        // Phase4整理: AsyncExceptionHandler, CancellationCoordinator, ResourceSemaphoreManagerは
        // テスト専用クラスとして維持（Phase5以降の本格統合時に再登録予定）
        services.AddSingleton<GracefulShutdownHandler>();
        services.AddSingleton<IConfigurationWatcher, ConfigurationWatcher>();

        // MultiConfig関連 - Singleton登録(重要:設定を複数のクラスで共有するため)
        services.AddSingleton<MultiPlcConfigManager>();

        // Transient登録 - 要求ごとに新しいインスタンスを生成
        // Phase 4-1: ParallelExecutionController統合 (5パラメータコンストラクタ使用)
        services.AddTransient<IExecutionOrchestrator>(provider =>
        {
            var timerService = provider.GetRequiredService<ITimerService>();
            var configToFrameManager = provider.GetRequiredService<IConfigToFrameManager>();
            var dataOutputManager = provider.GetRequiredService<IDataOutputManager>();
            var loggingManager = provider.GetRequiredService<ILoggingManager>();
            var parallelController = provider.GetRequiredService<IParallelExecutionController>();

            return new ExecutionOrchestrator(
                timerService,
                configToFrameManager,
                dataOutputManager,
                loggingManager,
                parallelController);
        });
        // PlcCommunicationManagerは設定ファイルから動的に生成されるため、ここでは登録しない
        services.AddTransient<IConfigToFrameManager, ConfigToFrameManager>(); // インターフェースあり
        services.AddTransient<IDataOutputManager, DataOutputManager>();
        services.AddTransient<ITimerService, TimerService>();

        // Part8追加: Phase3実装クラス(Transient)
        services.AddTransient<IProgressReporter<ProgressInfo>, ProgressReporter<ProgressInfo>>();
        services.AddTransient<IParallelExecutionController, ParallelExecutionController>();

        // Phase2 Step2-7: ConfigurationLoaderExcelの登録(Singleton)
        services.AddSingleton(provider =>
        {
            var baseDirectory = AppContext.BaseDirectory;
            var configManager = provider.GetRequiredService<MultiPlcConfigManager>();
            return new ConfigurationLoaderExcel(baseDirectory, configManager);
        });
    }
}
