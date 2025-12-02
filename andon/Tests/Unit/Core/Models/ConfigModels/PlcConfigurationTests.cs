using Xunit;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Models;
using Andon.Core.Constants;

namespace Andon.Tests.Unit.Core.Models.ConfigModels;

/// <summary>
/// PlcConfigurationクラスの単体テスト
/// </summary>
public class PlcConfigurationTests
{
    #region プロパティ初期化テスト

    [Fact]
    public void Constructor_デフォルト値が正しく設定される()
    {
        // Act
        var config = new PlcConfiguration();

        // Assert
        Assert.NotNull(config.IpAddress);
        Assert.Equal(string.Empty, config.IpAddress);
        Assert.Equal(0, config.Port);
        Assert.Equal(DefaultValues.MonitoringIntervalMs, config.MonitoringIntervalMs); // 既定値1000msを期待
        Assert.NotNull(config.PlcModel);
        Assert.Equal(string.Empty, config.PlcModel);
        Assert.NotNull(config.SavePath);
        Assert.Equal(string.Empty, config.SavePath);
        Assert.NotNull(config.SourceExcelFile);
        Assert.Equal(string.Empty, config.SourceExcelFile);
        Assert.NotNull(config.Devices);
        Assert.Empty(config.Devices);
    }

    [Fact]
    public void IpAddress_設定と取得が正しく動作する()
    {
        // Arrange
        var config = new PlcConfiguration();
        var expected = "172.30.40.15";

        // Act
        config.IpAddress = expected;

        // Assert
        Assert.Equal(expected, config.IpAddress);
    }

    [Fact]
    public void Port_設定と取得が正しく動作する()
    {
        // Arrange
        var config = new PlcConfiguration();
        var expected = 8192;

        // Act
        config.Port = expected;

        // Assert
        Assert.Equal(expected, config.Port);
    }

    [Fact]
    public void MonitoringIntervalMs_設定と取得が正しく動作する()
    {
        // Arrange
        var config = new PlcConfiguration();
        var expected = 1000;

        // Act
        config.MonitoringIntervalMs = expected;

        // Assert
        Assert.Equal(expected, config.MonitoringIntervalMs);
    }

    [Fact]
    public void PlcModel_設定と取得が正しく動作する()
    {
        // Arrange
        var config = new PlcConfiguration();
        var expected = "ライン1-炉A";

        // Act
        config.PlcModel = expected;

        // Assert
        Assert.Equal(expected, config.PlcModel);
    }

    [Fact]
    public void SavePath_設定と取得が正しく動作する()
    {
        // Arrange
        var config = new PlcConfiguration();
        var expected = @"C:\data\output";

        // Act
        config.SavePath = expected;

        // Assert
        Assert.Equal(expected, config.SavePath);
    }

    [Fact]
    public void SourceExcelFile_設定と取得が正しく動作する()
    {
        // Arrange
        var config = new PlcConfiguration();
        var expected = @"C:\config\plc_config.xlsx";

        // Act
        config.SourceExcelFile = expected;

        // Assert
        Assert.Equal(expected, config.SourceExcelFile);
    }

    [Fact]
    public void Devices_設定と取得が正しく動作する()
    {
        // Arrange
        var config = new PlcConfiguration();
        var device = new DeviceSpecification(DeviceCode.D, 60000);
        var expected = new List<DeviceSpecification> { device };

        // Act
        config.Devices = expected;

        // Assert
        Assert.Single(config.Devices);
        Assert.Equal(device, config.Devices[0]);
    }

    #endregion

    #region ConfigurationName計算プロパティテスト

    [Fact]
    public void ConfigurationName_SourceExcelFileが空の場合_空文字を返す()
    {
        // Arrange
        var config = new PlcConfiguration
        {
            SourceExcelFile = string.Empty
        };

        // Act
        var result = config.ConfigurationName;

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ConfigurationName_拡張子なしファイル名を返す()
    {
        // Arrange
        var config = new PlcConfiguration
        {
            SourceExcelFile = @"C:\config\plc_config.xlsx"
        };

        // Act
        var result = config.ConfigurationName;

        // Assert
        Assert.Equal("plc_config", result);
    }

    [Fact]
    public void ConfigurationName_パスなしファイル名でも正しく動作する()
    {
        // Arrange
        var config = new PlcConfiguration
        {
            SourceExcelFile = "test_plc.xlsx"
        };

        // Act
        var result = config.ConfigurationName;

        // Assert
        Assert.Equal("test_plc", result);
    }

    [Fact]
    public void ConfigurationName_複数ドットを含むファイル名でも正しく動作する()
    {
        // Arrange
        var config = new PlcConfiguration
        {
            SourceExcelFile = "plc.config.v1.0.xlsx"
        };

        // Act
        var result = config.ConfigurationName;

        // Assert
        Assert.Equal("plc.config.v1.0", result);
    }

    #endregion

    #region 統合テスト

    [Fact]
    public void 全プロパティ設定_正しく動作する()
    {
        // Arrange & Act
        var config = new PlcConfiguration
        {
            IpAddress = "172.30.40.15",
            Port = 8192,
            MonitoringIntervalMs = 1000,
            PlcModel = "ライン1-炉A",
            SavePath = @"C:\data\output",
            SourceExcelFile = @"C:\config\plc_config.xlsx",
            Devices = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 60000),
                new DeviceSpecification(DeviceCode.M, 32)
            }
        };

        // Assert
        Assert.Equal("172.30.40.15", config.IpAddress);
        Assert.Equal(8192, config.Port);
        Assert.Equal(1000, config.MonitoringIntervalMs);
        Assert.Equal("ライン1-炉A", config.PlcModel);
        Assert.Equal(@"C:\data\output", config.SavePath);
        Assert.Equal(@"C:\config\plc_config.xlsx", config.SourceExcelFile);
        Assert.Equal("plc_config", config.ConfigurationName);
        Assert.Equal(2, config.Devices.Count);
    }

    #endregion

    #region EffectivePlcName Tests (Phase6)

    [Fact]
    public void EffectivePlcName_PlcNameが設定されている場合_PlcNameを返す()
    {
        // Arrange
        var config = new PlcConfiguration
        {
            PlcName = "ライン1-炉A",
            PlcId = "192.168.1.10_8192",
            IpAddress = "192.168.1.10",
            Port = 8192
        };

        // Act & Assert
        Assert.Equal("ライン1-炉A", config.EffectivePlcName);
    }

    [Fact]
    public void EffectivePlcName_PlcNameが空の場合_PlcIdを返す()
    {
        // Arrange
        var config = new PlcConfiguration
        {
            PlcName = "",
            PlcId = "192.168.1.10_8192",
            IpAddress = "192.168.1.10",
            Port = 8192
        };

        // Act & Assert
        Assert.Equal("192.168.1.10_8192", config.EffectivePlcName);
    }

    [Fact]
    public void EffectivePlcName_PlcNameがnullの場合_PlcIdを返す()
    {
        // Arrange
        var config = new PlcConfiguration
        {
            PlcName = null,
            PlcId = "192.168.1.10_8192",
            IpAddress = "192.168.1.10",
            Port = 8192
        };

        // Act & Assert
        Assert.Equal("192.168.1.10_8192", config.EffectivePlcName);
    }

    #endregion
}
