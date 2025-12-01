using Andon.Core.Interfaces;
using Andon.Core.Managers;
using Andon.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Andon.Tests.Unit.Services;

/// <summary>
/// MultiConfigDIIntegration単体テスト
/// </summary>
public class MultiConfigDIIntegrationTests
{
    private readonly IMultiConfigDIIntegration _integration;
    private readonly Mock<ILogger<MultiPlcConfigManager>> _mockLogger;

    public MultiConfigDIIntegrationTests()
    {
        _integration = new MultiConfigDIIntegration();
        _mockLogger = new Mock<ILogger<MultiPlcConfigManager>>();
    }

    #region TC191_RegisterMultiConfigServices

    /// <summary>
    /// TC191_001: 正常系 - 複数設定対応サービス登録が成功する
    /// </summary>
    [Fact]
    public void TC191_001_RegisterMultiConfigServices_正常系_サービス登録成功()
    {
        // Arrange
        var services = new ServiceCollection();
        var configManager = new MultiPlcConfigManager(_mockLogger.Object);

        // Act
        var result = _integration.RegisterMultiConfigServices(services, configManager);

        // Assert
        Assert.NotNull(result);
        Assert.Same(services, result); // 同じIServiceCollectionが返される
        Assert.True(services.Count > 0); // 何らかのサービスが登録されている
    }

    /// <summary>
    /// TC191_002: 異常系 - services引数がnullの場合にArgumentNullExceptionがスローされる
    /// </summary>
    [Fact]
    public void TC191_002_RegisterMultiConfigServices_異常系_ServicesNull()
    {
        // Arrange
        IServiceCollection services = null!;
        var configManager = new MultiPlcConfigManager(_mockLogger.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _integration.RegisterMultiConfigServices(services, configManager));
    }

    /// <summary>
    /// TC191_003: 異常系 - configManager引数がnullの場合にArgumentNullExceptionがスローされる
    /// </summary>
    [Fact]
    public void TC191_003_RegisterMultiConfigServices_異常系_ConfigManagerNull()
    {
        // Arrange
        var services = new ServiceCollection();
        MultiPlcConfigManager configManager = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _integration.RegisterMultiConfigServices(services, configManager));
    }

    /// <summary>
    /// TC191_004: 境界値テスト - 空のServiceCollectionに対して登録が成功する
    /// </summary>
    [Fact]
    public void TC191_004_RegisterMultiConfigServices_境界値_空のServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var configManager = new MultiPlcConfigManager(_mockLogger.Object);

        // Act
        var result = _integration.RegisterMultiConfigServices(services, configManager);

        // Assert
        Assert.NotNull(result);
        Assert.True(services.Count > 0);
    }

    #endregion

    #region TC192_CreateConfigSpecificProvider

    /// <summary>
    /// TC192_001: 正常系 - 設定別サービスプロバイダ作成が成功する
    /// </summary>
    [Fact]
    public void TC192_001_CreateConfigSpecificProvider_正常系_プロバイダ作成成功()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<MultiPlcConfigManager>(sp => new MultiPlcConfigManager(_mockLogger.Object));
        var serviceProvider = services.BuildServiceProvider();
        var configName = "PLC1_settings.json";

        // Act
        var result = _integration.CreateConfigSpecificProvider(serviceProvider, configName);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IServiceProvider>(result);
    }

    /// <summary>
    /// TC192_002: 異常系 - serviceProvider引数がnullの場合にArgumentNullExceptionがスローされる
    /// </summary>
    [Fact]
    public void TC192_002_CreateConfigSpecificProvider_異常系_ServiceProviderNull()
    {
        // Arrange
        IServiceProvider serviceProvider = null!;
        var configName = "PLC1_settings.json";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _integration.CreateConfigSpecificProvider(serviceProvider, configName));
    }

    /// <summary>
    /// TC192_003: 異常系 - configName引数がnullの場合にArgumentNullExceptionがスローされる
    /// </summary>
    [Fact]
    public void TC192_003_CreateConfigSpecificProvider_異常系_ConfigNameNull()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        string configName = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _integration.CreateConfigSpecificProvider(serviceProvider, configName));
    }

    /// <summary>
    /// TC192_004: 異常系 - configName引数が空文字列の場合にArgumentExceptionがスローされる
    /// </summary>
    [Fact]
    public void TC192_004_CreateConfigSpecificProvider_異常系_ConfigName空文字列()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var configName = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _integration.CreateConfigSpecificProvider(serviceProvider, configName));
    }

    /// <summary>
    /// TC192_005: 異常系 - configName引数がホワイトスペースの場合にArgumentExceptionがスローされる
    /// </summary>
    [Fact]
    public void TC192_005_CreateConfigSpecificProvider_異常系_ConfigNameホワイトスペース()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var configName = "   ";

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _integration.CreateConfigSpecificProvider(serviceProvider, configName));
    }

    /// <summary>
    /// TC192_006: 正常系 - 複数の設定名で異なるプロバイダが作成される
    /// </summary>
    [Fact]
    public void TC192_006_CreateConfigSpecificProvider_正常系_複数設定名で異なるプロバイダ()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<MultiPlcConfigManager>(sp => new MultiPlcConfigManager(_mockLogger.Object));
        var serviceProvider = services.BuildServiceProvider();
        var configName1 = "PLC1_settings.json";
        var configName2 = "PLC2_settings.json";

        // Act
        var result1 = _integration.CreateConfigSpecificProvider(serviceProvider, configName1);
        var result2 = _integration.CreateConfigSpecificProvider(serviceProvider, configName2);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotSame(result1, result2); // 異なるインスタンス
    }

    #endregion
}
