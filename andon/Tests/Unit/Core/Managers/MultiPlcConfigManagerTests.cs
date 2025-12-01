using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Andon.Core.Managers;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Models;
using Andon.Core.Constants;

namespace Andon.Tests.Unit.Core.Managers;

/// <summary>
/// MultiPlcConfigManagerの単体テスト（Phase5）
/// </summary>
public class MultiPlcConfigManagerTests
{
    private readonly ILogger<MultiPlcConfigManager> _logger;

    public MultiPlcConfigManagerTests()
    {
        _logger = NullLogger<MultiPlcConfigManager>.Instance;
    }

    /// <summary>
    /// テスト用PlcConfiguration生成ヘルパー
    /// </summary>
    private PlcConfiguration CreateTestConfig(string name, int deviceCount = 5)
    {
        var devices = new List<DeviceSpecification>();
        for (int i = 0; i < deviceCount; i++)
        {
            devices.Add(new DeviceSpecification(
                DeviceCode.D,
                i * 10,
                isHexAddress: false)
            {
                ItemName = $"TestDevice{i}",
                DeviceType = "D",
                Digits = 1,
                Unit = "word"
            });
        }

        return new PlcConfiguration
        {
            SourceExcelFile = $"C:\\test\\{name}.xlsx",
            IpAddress = "192.168.1.100",
            Port = 5000,
            MonitoringIntervalMs = 1000,
            PlcModel = "TestPLC",
            SavePath = "C:\\test\\output",
            Devices = devices
        };
    }

    #region 設定追加テスト

    [Fact]
    public void AddConfiguration_1件追加_成功()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);
        var config = CreateTestConfig("config1");

        // Act
        manager.AddConfiguration(config);

        // Assert
        Assert.Equal(1, manager.GetConfigurationCount());
    }

    [Fact]
    public void AddConfiguration_複数件追加_成功()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);
        var config1 = CreateTestConfig("config1");
        var config2 = CreateTestConfig("config2");
        var config3 = CreateTestConfig("config3");

        // Act
        manager.AddConfiguration(config1);
        manager.AddConfiguration(config2);
        manager.AddConfiguration(config3);

        // Assert
        Assert.Equal(3, manager.GetConfigurationCount());
    }

    [Fact]
    public void AddConfiguration_重複追加_上書き成功()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);
        var config1 = CreateTestConfig("config1");

        // Act
        manager.AddConfiguration(config1);
        manager.AddConfiguration(config1); // 重複追加

        // Assert
        Assert.Equal(1, manager.GetConfigurationCount()); // 上書きなので1件のまま
    }

    [Fact]
    public void AddConfiguration_Null_ArgumentNullException()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            manager.AddConfiguration(null!));
    }

    [Fact]
    public void AddConfigurations_複数設定一括追加_成功()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);
        var configs = new[]
        {
            CreateTestConfig("config1"),
            CreateTestConfig("config2"),
            CreateTestConfig("config3")
        };

        // Act
        manager.AddConfigurations(configs);

        // Assert
        Assert.Equal(3, manager.GetConfigurationCount());
    }

    [Fact]
    public void AddConfigurations_Null_ArgumentNullException()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            manager.AddConfigurations(null!));
    }

    #endregion

    #region 設定取得テスト

    [Fact]
    public void GetConfiguration_名前で取得_成功()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);
        var config = CreateTestConfig("test_config");
        manager.AddConfiguration(config);

        // Act
        var retrieved = manager.GetConfiguration("test_config");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(config.ConfigurationName, retrieved.ConfigurationName);
        Assert.Equal(config.IpAddress, retrieved.IpAddress);
    }

    [Fact]
    public void GetConfiguration_存在しない名前_KeyNotFoundException()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() =>
            manager.GetConfiguration("not_exist"));
    }

    [Fact]
    public void GetConfiguration_空文字列_ArgumentException()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            manager.GetConfiguration(""));
    }

    [Fact]
    public void GetConfiguration_Null_ArgumentException()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            manager.GetConfiguration(null!));
    }

    [Fact]
    public void HasConfiguration_存在する設定_True()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);
        var config = CreateTestConfig("test_config");
        manager.AddConfiguration(config);

        // Act & Assert
        Assert.True(manager.HasConfiguration("test_config"));
    }

    [Fact]
    public void HasConfiguration_存在しない設定_False()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);

        // Act & Assert
        Assert.False(manager.HasConfiguration("not_exist"));
    }

    [Fact]
    public void HasConfiguration_空文字列_False()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);

        // Act & Assert
        Assert.False(manager.HasConfiguration(""));
    }

    [Fact]
    public void HasConfiguration_Null_False()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);

        // Act & Assert
        Assert.False(manager.HasConfiguration(null!));
    }

    #endregion

    #region 全設定取得テスト

    [Fact]
    public void GetAllConfigurations_全設定取得_成功()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);
        manager.AddConfiguration(CreateTestConfig("config1"));
        manager.AddConfiguration(CreateTestConfig("config2"));
        manager.AddConfiguration(CreateTestConfig("config3"));

        // Act
        var allConfigs = manager.GetAllConfigurations();

        // Assert
        Assert.Equal(3, allConfigs.Count);
    }

    [Fact]
    public void GetAllConfigurations_空_空リスト()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);

        // Act
        var allConfigs = manager.GetAllConfigurations();

        // Assert
        Assert.Empty(allConfigs);
    }

    [Fact]
    public void GetAllConfigurationNames_全設定名取得_成功()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);
        manager.AddConfiguration(CreateTestConfig("config1"));
        manager.AddConfiguration(CreateTestConfig("config2"));
        manager.AddConfiguration(CreateTestConfig("config3"));

        // Act
        var allNames = manager.GetAllConfigurationNames();

        // Assert
        Assert.Equal(3, allNames.Count);
        Assert.Contains("config1", allNames);
        Assert.Contains("config2", allNames);
        Assert.Contains("config3", allNames);
    }

    [Fact]
    public void GetAllConfigurationNames_空_空リスト()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);

        // Act
        var allNames = manager.GetAllConfigurationNames();

        // Assert
        Assert.Empty(allNames);
    }

    [Fact]
    public void GetConfigurationCount_設定数取得_成功()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);
        manager.AddConfiguration(CreateTestConfig("config1"));
        manager.AddConfiguration(CreateTestConfig("config2"));

        // Act
        var count = manager.GetConfigurationCount();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public void GetConfigurationCount_空_0()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);

        // Act
        var count = manager.GetConfigurationCount();

        // Assert
        Assert.Equal(0, count);
    }

    #endregion

    #region 設定削除テスト

    [Fact]
    public void RemoveConfiguration_1件削除_成功()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);
        manager.AddConfiguration(CreateTestConfig("config1"));
        manager.AddConfiguration(CreateTestConfig("config2"));

        // Act
        bool removed = manager.RemoveConfiguration("config1");

        // Assert
        Assert.True(removed);
        Assert.Equal(1, manager.GetConfigurationCount());
        Assert.False(manager.HasConfiguration("config1"));
    }

    [Fact]
    public void RemoveConfiguration_存在しない設定_False()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);

        // Act
        bool removed = manager.RemoveConfiguration("not_exist");

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public void RemoveConfiguration_空文字列_False()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);

        // Act
        bool removed = manager.RemoveConfiguration("");

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public void Clear_全削除_成功()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);
        manager.AddConfiguration(CreateTestConfig("config1"));
        manager.AddConfiguration(CreateTestConfig("config2"));
        manager.AddConfiguration(CreateTestConfig("config3"));

        // Act
        manager.Clear();

        // Assert
        Assert.Equal(0, manager.GetConfigurationCount());
    }

    #endregion

    #region 統計情報取得テスト

    [Fact]
    public void GetStatistics_統計情報取得_成功()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);
        var config1 = CreateTestConfig("config1", deviceCount: 10);
        var config2 = CreateTestConfig("config2", deviceCount: 20);
        manager.AddConfiguration(config1);
        manager.AddConfiguration(config2);

        // Act
        var stats = manager.GetStatistics();

        // Assert
        Assert.Equal(2, stats.TotalConfigurations);
        Assert.Equal(30, stats.TotalDevices);
        Assert.Equal(2, stats.ConfigurationDetails.Count);
    }

    [Fact]
    public void GetStatistics_空_全てゼロ()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);

        // Act
        var stats = manager.GetStatistics();

        // Assert
        Assert.Equal(0, stats.TotalConfigurations);
        Assert.Equal(0, stats.TotalDevices);
        Assert.Empty(stats.ConfigurationDetails);
    }

    [Fact]
    public void GetStatistics_ConfigDetail内容確認_成功()
    {
        // Arrange
        var manager = new MultiPlcConfigManager(_logger);
        var config = CreateTestConfig("test_config", deviceCount: 5);
        manager.AddConfiguration(config);

        // Act
        var stats = manager.GetStatistics();

        // Assert
        var detail = stats.ConfigurationDetails[0];
        Assert.Equal("test_config", detail.Name);
        Assert.Equal("192.168.1.100", detail.IpAddress);
        Assert.Equal(5000, detail.Port);
        Assert.Equal(5, detail.DeviceCount);
        Assert.Equal("TestPLC", detail.PlcModel);
    }

    #endregion
}
