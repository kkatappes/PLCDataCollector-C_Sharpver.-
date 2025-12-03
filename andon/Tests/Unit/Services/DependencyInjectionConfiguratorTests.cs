using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Andon.Core.Interfaces;
using Andon.Core.Controllers;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Services;
using Andon.Infrastructure.Configuration;

namespace Andon.Tests.Unit.Services;

/// <summary>
/// DependencyInjectionConfigurator単体テスト
/// </summary>
public class DependencyInjectionConfiguratorTests
{
    /// <summary>
    /// mockConfigurationのGetSectionをセットアップするヘルパーメソッド
    /// </summary>
    private Mock<IConfiguration> CreateMockConfiguration()
    {
        var mockConfiguration = new Mock<IConfiguration>();

        // PlcCommunicationセクションのモック
        var mockPlcSection = new Mock<IConfigurationSection>();
        mockPlcSection.Setup(x => x.Path).Returns("PlcCommunication");
        mockPlcSection.Setup(x => x.Key).Returns("PlcCommunication");
        mockPlcSection.Setup(x => x.Value).Returns((string?)null);

        // Phase 2-1完了: LoggingConfigセクションのモックは不要（ハードコード化されたため）

        // GetSectionのセットアップ
        mockConfiguration.Setup(x => x.GetSection("PlcCommunication")).Returns(mockPlcSection.Object);

        return mockConfiguration;
    }

    [Fact]
    public void Configure_必要なサービスをすべて登録する()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConfiguration = CreateMockConfiguration();

        // Act
        DependencyInjectionConfigurator.Configure(services, mockConfiguration.Object);
        using var provider = services.BuildServiceProvider();

        // Assert - Singleton登録確認
        var controller1 = provider.GetService<IApplicationController>();
        var controller2 = provider.GetService<IApplicationController>();
        Assert.NotNull(controller1);
        Assert.Same(controller1, controller2); // Singletonは同一インスタンス

        var logger1 = provider.GetService<ILoggingManager>();
        var logger2 = provider.GetService<ILoggingManager>();
        Assert.NotNull(logger1);
        Assert.Same(logger1, logger2);

        // Assert - Transient登録確認
        var orchestrator1 = provider.GetService<IExecutionOrchestrator>();
        var orchestrator2 = provider.GetService<IExecutionOrchestrator>();
        Assert.NotNull(orchestrator1);
        Assert.NotSame(orchestrator1, orchestrator2); // Transientは異なるインスタンス

        // PlcCommunicationManagerは手動で設定が必要なため、ここではスキップ

        // Assert - TimerService登録確認
        var timerService = provider.GetService<ITimerService>();
        Assert.NotNull(timerService);
    }

    [Fact]
    public void Configure_MultiConfig関連サービスが登録される()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConfiguration = CreateMockConfiguration();

        // Act
        DependencyInjectionConfigurator.Configure(services, mockConfiguration.Object);
        using var provider = services.BuildServiceProvider();

        // Assert
        var multiConfigManager = provider.GetService<MultiPlcConfigManager>();
        Assert.NotNull(multiConfigManager);

        // var multiCoordinator = provider.GetService<MultiPlcCoordinator>();  // MultiPlcCoordinator削除により一時コメントアウト（JSON設定廃止）
        // Assert.NotNull(multiCoordinator);
    }

    [Fact]
    public void Configure_全インターフェースが解決可能()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConfiguration = CreateMockConfiguration();

        // Act
        DependencyInjectionConfigurator.Configure(services, mockConfiguration.Object);
        using var provider = services.BuildServiceProvider();

        // Assert - 全インターフェース・クラスが解決できることを確認
        Assert.NotNull(provider.GetService<IApplicationController>());
        Assert.NotNull(provider.GetService<IExecutionOrchestrator>());
        // PlcCommunicationManagerは手動で設定が必要なため、ここではスキップ
        Assert.NotNull(provider.GetService<IConfigToFrameManager>()); // インターフェース経由で解決
        Assert.NotNull(provider.GetService<IDataOutputManager>());
        Assert.NotNull(provider.GetService<ILoggingManager>());
        Assert.NotNull(provider.GetService<IErrorHandler>()); // Part8修正: インターフェース経由で解決
        Assert.NotNull(provider.GetService<ITimerService>());
        Assert.NotNull(provider.GetService<MultiPlcConfigManager>());
        // Assert.NotNull(provider.GetService<MultiPlcCoordinator>());  // MultiPlcCoordinator削除により一時コメントアウト（JSON設定廃止）
    }

    [Fact]
    public void Configure_ConfigurationLoaderExcelが登録される()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConfiguration = CreateMockConfiguration();

        // Act
        DependencyInjectionConfigurator.Configure(services, mockConfiguration.Object);
        using var provider = services.BuildServiceProvider();

        // Assert
        var loader = provider.GetService<ConfigurationLoaderExcel>();
        Assert.NotNull(loader);
    }

    // ===== Phase 3 Part8: 新規追加テスト =====

    [Fact]
    [Trait("Category", "DI")]
    [Trait("Phase", "Part8")]
    public void Configure_Phase3Part1クラスがすべて登録される()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConfiguration = CreateMockConfiguration();

        // Act
        DependencyInjectionConfigurator.Configure(services, mockConfiguration.Object);
        using var provider = services.BuildServiceProvider();

        // Assert - Part1 Singleton登録確認
        var asyncHandler1 = provider.GetService<AsyncExceptionHandler>();
        var asyncHandler2 = provider.GetService<AsyncExceptionHandler>();
        Assert.NotNull(asyncHandler1);
        Assert.Same(asyncHandler1, asyncHandler2); // Singletonは同一インスタンス

        var cancellationCoord1 = provider.GetService<CancellationCoordinator>();
        var cancellationCoord2 = provider.GetService<CancellationCoordinator>();
        Assert.NotNull(cancellationCoord1);
        Assert.Same(cancellationCoord1, cancellationCoord2);

        var semaphoreManager1 = provider.GetService<ResourceSemaphoreManager>();
        var semaphoreManager2 = provider.GetService<ResourceSemaphoreManager>();
        Assert.NotNull(semaphoreManager1);
        Assert.Same(semaphoreManager1, semaphoreManager2);
    }

    [Fact]
    [Trait("Category", "DI")]
    [Trait("Phase", "Part8")]
    public void Configure_Phase3Part2Part3クラスがすべて登録される()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConfiguration = CreateMockConfiguration();

        // Act
        DependencyInjectionConfigurator.Configure(services, mockConfiguration.Object);
        using var provider = services.BuildServiceProvider();

        // Assert - Part2/3 Transient登録確認
        var reporter1 = provider.GetService<IProgressReporter<ProgressInfo>>();
        var reporter2 = provider.GetService<IProgressReporter<ProgressInfo>>();
        Assert.NotNull(reporter1);
        Assert.NotSame(reporter1, reporter2); // Transientは異なるインスタンス

        var controller1 = provider.GetService<IParallelExecutionController>();
        var controller2 = provider.GetService<IParallelExecutionController>();
        Assert.NotNull(controller1);
        Assert.NotSame(controller1, controller2);
    }

    [Fact]
    [Trait("Category", "DI")]
    [Trait("Phase", "Part8")]
    public void Configure_GracefulShutdownHandlerが登録される()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConfiguration = CreateMockConfiguration();

        // Act
        DependencyInjectionConfigurator.Configure(services, mockConfiguration.Object);
        using var provider = services.BuildServiceProvider();

        // Assert - Singleton確認
        var handler1 = provider.GetService<GracefulShutdownHandler>();
        var handler2 = provider.GetService<GracefulShutdownHandler>();
        Assert.NotNull(handler1);
        Assert.Same(handler1, handler2);
    }

    [Fact]
    [Trait("Category", "DI")]
    [Trait("Phase", "Part8")]
    public void Configure_ConfigurationWatcherが登録される()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConfiguration = CreateMockConfiguration();

        // Act
        DependencyInjectionConfigurator.Configure(services, mockConfiguration.Object);
        using var provider = services.BuildServiceProvider();

        // Assert - Singleton確認
        var watcher1 = provider.GetService<IConfigurationWatcher>();
        var watcher2 = provider.GetService<IConfigurationWatcher>();
        Assert.NotNull(watcher1);
        Assert.Same(watcher1, watcher2);
    }

    // Phase 2-1完了: LoggingConfigはハードコード化されたため、このテストは不要
    // [Fact]
    // [Trait("Category", "DI")]
    // [Trait("Phase", "Part8")]
    // public void Configure_LoggingConfigが登録される()
    // {
    //     // Arrange
    //     var services = new ServiceCollection();
    //     var mockConfiguration = CreateMockConfiguration();
    //
    //     // Act
    //     DependencyInjectionConfigurator.Configure(services, mockConfiguration.Object);
    //     using var provider = services.BuildServiceProvider();
    //
    //     // Assert
    //     var config = provider.GetService<IOptions<LoggingConfig>>();
    //     Assert.NotNull(config);
    //     Assert.NotNull(config.Value);
    //     Assert.Equal("Information", config.Value.LogLevel);
    //     Assert.True(config.Value.EnableFileOutput);
    //     Assert.Equal("logs/andon.log", config.Value.LogFilePath);
    //     Assert.Equal(10, config.Value.MaxLogFileSizeMb);
    // }

    [Fact]
    [Trait("Category", "DI")]
    [Trait("Phase", "Part8")]
    public void Configure_LoggingManagerがOptions経由で解決できる()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConfiguration = CreateMockConfiguration();

        // Act
        DependencyInjectionConfigurator.Configure(services, mockConfiguration.Object);
        using var provider = services.BuildServiceProvider();

        // Assert
        var loggingManager = provider.GetService<ILoggingManager>();
        Assert.NotNull(loggingManager);
    }
}
