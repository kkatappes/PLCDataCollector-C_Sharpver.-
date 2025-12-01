using Andon.Core.Models.ConfigModels;
using Andon.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Andon.Tests.Unit.Services;

/// <summary>
/// OptionsConfiguratorのユニットテスト
/// </summary>
public class OptionsConfiguratorTests
{
    /// <summary>
    /// ConfigureOptions - 正常系: 全Options設定が正しく登録される
    /// </summary>
    [Fact]
    public void ConfigureOptions_ValidConfiguration_RegistersAllOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionConfig:IpAddress"] = "192.168.1.1",
                ["ConnectionConfig:Port"] = "5000",
                ["TimeoutConfig:ConnectTimeoutMs"] = "5000",
                ["TimeoutConfig:SendTimeoutMs"] = "3000",
                ["TimeoutConfig:ReceiveTimeoutMs"] = "5000",
                ["SystemResourcesConfig:MaxConcurrentConnections"] = "10",
                ["LoggingConfig:LogLevel"] = "Information"
            })
            .Build();

        var configurator = new OptionsConfigurator();

        // Act
        var result = configurator.ConfigureOptions(services, configuration);

        // Assert
        Assert.NotNull(result);
        Assert.Same(services, result);

        // サービスプロバイダで実際にOptions取得可能か確認
        var provider = services.BuildServiceProvider();
        var connectionConfig = provider.GetService<IOptions<ConnectionConfig>>();
        var timeoutConfig = provider.GetService<IOptions<TimeoutConfig>>();
        var systemResourcesConfig = provider.GetService<IOptions<SystemResourcesConfig>>();
        var loggingConfig = provider.GetService<IOptions<LoggingConfig>>();

        Assert.NotNull(connectionConfig);
        Assert.NotNull(timeoutConfig);
        Assert.NotNull(systemResourcesConfig);
        Assert.NotNull(loggingConfig);
    }

    /// <summary>
    /// ConfigureOptions - 異常系: IServiceCollectionがnullの場合は例外
    /// </summary>
    [Fact]
    public void ConfigureOptions_NullServiceCollection_ThrowsArgumentNullException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var configurator = new OptionsConfigurator();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            configurator.ConfigureOptions(null!, configuration));
    }

    /// <summary>
    /// ConfigureOptions - 異常系: IConfigurationがnullの場合は例外
    /// </summary>
    [Fact]
    public void ConfigureOptions_NullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = new OptionsConfigurator();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            configurator.ConfigureOptions(services, null!));
    }

    /// <summary>
    /// ConfigureOptions - 正常系: ConnectionConfig値が正しくバインドされる
    /// </summary>
    [Fact]
    public void ConfigureOptions_ConnectionConfig_BindsValuesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionConfig:IpAddress"] = "10.0.0.1",
                ["ConnectionConfig:Port"] = "8080"
            })
            .Build();

        var configurator = new OptionsConfigurator();

        // Act
        configurator.ConfigureOptions(services, configuration);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ConnectionConfig>>();

        Assert.Equal("10.0.0.1", options.Value.IpAddress);
        Assert.Equal(8080, options.Value.Port);
    }

    /// <summary>
    /// ValidateOptions - 正常系: バリデーション設定が正しく登録される
    /// </summary>
    [Fact]
    public void ValidateOptions_ValidServiceCollection_RegistersValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionConfig:IpAddress"] = "192.168.1.1",
                ["ConnectionConfig:Port"] = "5000"
            })
            .Build();

        var configurator = new OptionsConfigurator();
        configurator.ConfigureOptions(services, configuration);

        // Act
        var result = configurator.ValidateOptions(services);

        // Assert
        Assert.NotNull(result);
        Assert.Same(services, result);

        // バリデーション設定が登録されていることを確認
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<ConnectionConfig>>();
        Assert.NotNull(options);
    }

    /// <summary>
    /// ValidateOptions - 異常系: IServiceCollectionがnullの場合は例外
    /// </summary>
    [Fact]
    public void ValidateOptions_NullServiceCollection_ThrowsArgumentNullException()
    {
        // Arrange
        var configurator = new OptionsConfigurator();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            configurator.ValidateOptions(null!));
    }

    /// <summary>
    /// ValidateOptions - 正常系: 無効な設定値でバリデーションエラーが発生
    /// </summary>
    [Fact]
    public void ValidateOptions_InvalidConfiguration_ThrowsOptionsValidationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionConfig:IpAddress"] = "", // 空文字（無効）
                ["ConnectionConfig:Port"] = "-1" // 負の値（無効）
            })
            .Build();

        var configurator = new OptionsConfigurator();
        configurator.ConfigureOptions(services, configuration);
        configurator.ValidateOptions(services);

        // Act & Assert
        var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<ConnectionConfig>>().Value);
    }

    /// <summary>
    /// ConfigureOptions - 正常系: TimeoutConfig値が正しくバインドされる
    /// </summary>
    [Fact]
    public void ConfigureOptions_TimeoutConfig_BindsValuesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TimeoutConfig:ConnectTimeoutMs"] = "10000",
                ["TimeoutConfig:SendTimeoutMs"] = "3000",
                ["TimeoutConfig:ReceiveTimeoutMs"] = "5000"
            })
            .Build();

        var configurator = new OptionsConfigurator();

        // Act
        configurator.ConfigureOptions(services, configuration);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<TimeoutConfig>>();

        Assert.Equal(10000, options.Value.ConnectTimeoutMs);
        Assert.Equal(3000, options.Value.SendTimeoutMs);
        Assert.Equal(5000, options.Value.ReceiveTimeoutMs);
    }

    /// <summary>
    /// ConfigureOptions - 正常系: SystemResourcesConfig値が正しくバインドされる
    /// </summary>
    [Fact]
    public void ConfigureOptions_SystemResourcesConfig_BindsValuesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SystemResourcesConfig:MaxConcurrentConnections"] = "20"
            })
            .Build();

        var configurator = new OptionsConfigurator();

        // Act
        configurator.ConfigureOptions(services, configuration);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SystemResourcesConfig>>();

        Assert.Equal(20, options.Value.MaxConcurrentConnections);
    }

    /// <summary>
    /// ConfigureOptions - 正常系: LoggingConfig値が正しくバインドされる
    /// </summary>
    [Fact]
    public void ConfigureOptions_LoggingConfig_BindsValuesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LoggingConfig:LogLevel"] = "Debug"
            })
            .Build();

        var configurator = new OptionsConfigurator();

        // Act
        configurator.ConfigureOptions(services, configuration);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LoggingConfig>>();

        Assert.Equal("Debug", options.Value.LogLevel);
    }
}
