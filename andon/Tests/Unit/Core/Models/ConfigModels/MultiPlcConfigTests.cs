using Xunit;
using Andon.Core.Models.ConfigModels;

namespace Andon.Tests.Unit.Core.Models.ConfigModels;

/// <summary>
/// MultiPlcConfig系モデルのテスト（Phase6-2）
/// </summary>
public class MultiPlcConfigTests
{
    [Fact]
    public void TC_MultiPlcConfig_001_デフォルト値で生成できる()
    {
        // Arrange & Act
        var config = new MultiPlcConfig();

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.PlcConnections);
        Assert.Empty(config.PlcConnections);
        Assert.NotNull(config.ParallelConfig);
    }

    [Fact]
    public void TC_MultiPlcConfig_002_PlcConnectionsリストに追加できる()
    {
        // Arrange
        var config = new MultiPlcConfig();
        var plcConfig = new PlcConnectionConfig
        {
            PlcId = "PLC_001",
            PlcName = "ライン1",
            IPAddress = "172.30.40.10",
            Port = 8192
        };

        // Act
        config.PlcConnections.Add(plcConfig);

        // Assert
        Assert.Single(config.PlcConnections);
        Assert.Equal("PLC_001", config.PlcConnections[0].PlcId);
    }

    [Fact]
    public void TC_ParallelProcessingConfig_001_デフォルト値検証()
    {
        // Arrange & Act
        var config = new ParallelProcessingConfig();

        // Assert
        Assert.True(config.EnableParallel); // デフォルト: 有効
        Assert.Equal(0, config.MaxDegreeOfParallelism); // デフォルト: 制限なし
        Assert.Equal(30000, config.OverallTimeoutMs); // デフォルト: 30秒
    }

    [Fact]
    public void TC_ParallelProcessingConfig_002_値を設定できる()
    {
        // Arrange & Act
        var config = new ParallelProcessingConfig
        {
            EnableParallel = false,
            MaxDegreeOfParallelism = 5,
            OverallTimeoutMs = 10000
        };

        // Assert
        Assert.False(config.EnableParallel);
        Assert.Equal(5, config.MaxDegreeOfParallelism);
        Assert.Equal(10000, config.OverallTimeoutMs);
    }

    [Fact]
    public void TC_PlcConnectionConfig_001_デフォルト値検証()
    {
        // Arrange & Act
        var config = new PlcConnectionConfig();

        // Assert
        Assert.Equal("127.0.0.1", config.IPAddress);
        Assert.Equal(8192, config.Port);
        Assert.Equal("UDP", config.ConnectionMethod);
        Assert.Equal("3E", config.FrameVersion);
        Assert.Equal(8000, config.Timeout);
        Assert.Equal("PLC_001", config.PlcId);
        Assert.Equal("ライン1_設備A", config.PlcName);
        Assert.Equal(5, config.Priority);
        Assert.NotNull(config.Devices);
        Assert.Empty(config.Devices);
    }

    [Fact]
    public void TC_PlcConnectionConfig_002_新規追加プロパティを設定できる()
    {
        // Arrange & Act
        var config = new PlcConnectionConfig
        {
            PlcId = "PLC_LINE1_A",
            PlcName = "ライン1設備A",
            Priority = 10
        };

        // Assert
        Assert.Equal("PLC_LINE1_A", config.PlcId);
        Assert.Equal("ライン1設備A", config.PlcName);
        Assert.Equal(10, config.Priority);
    }

    [Fact]
    public void TC_PlcConnectionConfig_003_Devicesリストに追加できる()
    {
        // Arrange
        var config = new PlcConnectionConfig();
        var device = new DeviceEntry
        {
            DeviceType = "D",
            DeviceNumber = 100
        };

        // Act
        config.Devices.Add(device);

        // Assert
        Assert.Single(config.Devices);
        Assert.Equal("D", config.Devices[0].DeviceType);
        Assert.Equal(100, config.Devices[0].DeviceNumber);
    }

    [Fact]
    public void TC_DeviceEntry_001_プロパティを設定できる()
    {
        // Arrange & Act
        var device = new DeviceEntry
        {
            DeviceType = "M",
            DeviceNumber = 16
        };

        // Assert
        Assert.Equal("M", device.DeviceType);
        Assert.Equal(16, device.DeviceNumber);
    }
}
