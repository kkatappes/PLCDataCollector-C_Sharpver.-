using Xunit;
using Microsoft.Extensions.Configuration;
using Andon.Infrastructure.Configuration;
using Andon.Core.Models;
using Andon.Core.Constants;

namespace Andon.Tests.Unit.Infrastructure.Configuration;

/// <summary>
/// ConfigurationLoaderのテスト
/// Phase6 ステップ19: 設定ファイルのバリデーションテスト
/// </summary>
public class ConfigurationLoaderTests
{
    /// <summary>
    /// TC_Step19_001: Devicesリストの読み込みテスト（正常系）
    /// </summary>
    [Fact]
    public void TC_Step19_001_LoadPlcConnectionConfig_NewFormat_ReturnsCorrectConfig()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["PlcCommunication:Connection:FrameVersion"] = "4E",
            ["PlcCommunication:Timeouts:ReceiveTimeoutMs"] = "8000",
            ["PlcCommunication:TargetDevices:Devices:0:DeviceType"] = "D",
            ["PlcCommunication:TargetDevices:Devices:0:DeviceNumber"] = "100",
            ["PlcCommunication:TargetDevices:Devices:0:Description"] = "生産数カウンタ",
            ["PlcCommunication:TargetDevices:Devices:1:DeviceType"] = "M",
            ["PlcCommunication:TargetDevices:Devices:1:DeviceNumber"] = "200",
            ["PlcCommunication:TargetDevices:Devices:1:Description"] = "運転状態フラグ"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        var loader = new ConfigurationLoader(configuration);

        // Act
        var config = loader.LoadPlcConnectionConfig();

        // Assert
        Assert.Equal("4E", config.FrameType);
        Assert.Equal((ushort)32, config.Timeout); // 8000ms / 250 = 32
        Assert.Equal(2, config.Devices.Count);

        // デバイス1: D100
        Assert.Equal("D", config.Devices[0].DeviceType);
        Assert.Equal(100, config.Devices[0].DeviceNumber);
        Assert.False(config.Devices[0].IsHexAddress);

        // デバイス2: M200
        Assert.Equal("M", config.Devices[1].DeviceType);
        Assert.Equal(200, config.Devices[1].DeviceNumber);
        Assert.False(config.Devices[1].IsHexAddress);
    }

    /// <summary>
    /// TC_Step19_002: 16進アドレスデバイスの読み込みテスト
    /// </summary>
    [Fact]
    public void TC_Step19_002_LoadPlcConnectionConfig_HexAddressDevice_CorrectlyParsed()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["PlcCommunication:Connection:FrameVersion"] = "3E",
            ["PlcCommunication:Timeouts:ReceiveTimeoutMs"] = "5000",
            ["PlcCommunication:TargetDevices:Devices:0:DeviceType"] = "W",
            ["PlcCommunication:TargetDevices:Devices:0:DeviceNumber"] = "4522",
            ["PlcCommunication:TargetDevices:Devices:0:IsHexAddress"] = "true",
            ["PlcCommunication:TargetDevices:Devices:0:Description"] = "通信ステータス（W0x11AA）"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        var loader = new ConfigurationLoader(configuration);

        // Act
        var config = loader.LoadPlcConnectionConfig();

        // Assert
        Assert.Equal("3E", config.FrameType);
        Assert.Equal((ushort)20, config.Timeout); // 5000ms / 250 = 20
        Assert.Single(config.Devices);

        // デバイス: W4522 (0x11AA)
        Assert.Equal("W", config.Devices[0].DeviceType);
        Assert.Equal(4522, config.Devices[0].DeviceNumber);
        Assert.True(config.Devices[0].IsHexAddress);
    }

    /// <summary>
    /// TC_Step19_003: 空のDevicesリストで例外がスローされること
    /// </summary>
    [Fact]
    public void TC_Step19_003_LoadPlcConnectionConfig_EmptyDevices_ThrowsException()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["PlcCommunication:Connection:FrameVersion"] = "4E",
            ["PlcCommunication:Timeouts:ReceiveTimeoutMs"] = "8000"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        var loader = new ConfigurationLoader(configuration);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => loader.LoadPlcConnectionConfig()
        );
        Assert.Contains("デバイスリストが空です", ex.Message);
    }

    /// <summary>
    /// TC_Step19_004: 256点以上で例外がスローされること
    /// </summary>
    [Fact]
    public void TC_Step19_004_LoadPlcConnectionConfig_TooManyDevices_ThrowsException()
    {
        // Arrange: 256デバイス（上限超過）
        var configData = new Dictionary<string, string>
        {
            ["PlcCommunication:Connection:FrameVersion"] = "4E",
            ["PlcCommunication:Timeouts:ReceiveTimeoutMs"] = "8000"
        };

        // 256デバイスを追加
        for (int i = 0; i < 256; i++)
        {
            configData[$"PlcCommunication:TargetDevices:Devices:{i}:DeviceType"] = "D";
            configData[$"PlcCommunication:TargetDevices:Devices:{i}:DeviceNumber"] = i.ToString();
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        var loader = new ConfigurationLoader(configuration);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => loader.LoadPlcConnectionConfig()
        );
        Assert.Contains("デバイス点数が上限を超えています", ex.Message);
        Assert.Contains("256", ex.Message);
    }

    /// <summary>
    /// TC_Step19_005: 不正なDeviceTypeで例外がスローされること
    /// </summary>
    [Fact]
    public void TC_Step19_005_LoadPlcConnectionConfig_InvalidDeviceType_ThrowsException()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["PlcCommunication:Connection:FrameVersion"] = "4E",
            ["PlcCommunication:Timeouts:ReceiveTimeoutMs"] = "8000",
            ["PlcCommunication:TargetDevices:Devices:0:DeviceType"] = "INVALID",
            ["PlcCommunication:TargetDevices:Devices:0:DeviceNumber"] = "100"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        var loader = new ConfigurationLoader(configuration);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(
            () => loader.LoadPlcConnectionConfig()
        );
        Assert.Contains("不正なDeviceType", ex.Message);
        Assert.Contains("INVALID", ex.Message);
    }

    /// <summary>
    /// TC_Step19_006: 不正なフレームタイプで例外がスローされること
    /// </summary>
    [Fact]
    public void TC_Step19_006_LoadPlcConnectionConfig_InvalidFrameType_ThrowsException()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["PlcCommunication:Connection:FrameVersion"] = "5E", // 不正
            ["PlcCommunication:Timeouts:ReceiveTimeoutMs"] = "8000",
            ["PlcCommunication:TargetDevices:Devices:0:DeviceType"] = "D",
            ["PlcCommunication:TargetDevices:Devices:0:DeviceNumber"] = "100"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        var loader = new ConfigurationLoader(configuration);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => loader.LoadPlcConnectionConfig()
        );
        Assert.Contains("未対応のフレームタイプ", ex.Message);
        Assert.Contains("5E", ex.Message);
    }

    /// <summary>
    /// TC_Step19_007: 混在デバイスタイプの読み込みテスト
    /// </summary>
    [Fact]
    public void TC_Step19_007_LoadPlcConnectionConfig_MixedDeviceTypes_CorrectlyParsed()
    {
        // Arrange: M, D, Wを混在
        var configData = new Dictionary<string, string>
        {
            ["PlcCommunication:Connection:FrameVersion"] = "4E",
            ["PlcCommunication:Timeouts:ReceiveTimeoutMs"] = "8000",
            ["PlcCommunication:TargetDevices:Devices:0:DeviceType"] = "M",
            ["PlcCommunication:TargetDevices:Devices:0:DeviceNumber"] = "0",
            ["PlcCommunication:TargetDevices:Devices:1:DeviceType"] = "D",
            ["PlcCommunication:TargetDevices:Devices:1:DeviceNumber"] = "100",
            ["PlcCommunication:TargetDevices:Devices:2:DeviceType"] = "W",
            ["PlcCommunication:TargetDevices:Devices:2:DeviceNumber"] = "4522",
            ["PlcCommunication:TargetDevices:Devices:2:IsHexAddress"] = "true"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        var loader = new ConfigurationLoader(configuration);

        // Act
        var config = loader.LoadPlcConnectionConfig();

        // Assert
        Assert.Equal(3, config.Devices.Count);
        Assert.Equal("M", config.Devices[0].DeviceType);
        Assert.Equal("D", config.Devices[1].DeviceType);
        Assert.Equal("W", config.Devices[2].DeviceType);
        Assert.True(config.Devices[2].IsHexAddress);
    }

    /// <summary>
    /// TC_Step19_008: デフォルト値のテスト
    /// </summary>
    [Fact]
    public void TC_Step19_008_LoadPlcConnectionConfig_DefaultValues_CorrectlyApplied()
    {
        // Arrange: FrameVersionとTimeoutを省略
        var configData = new Dictionary<string, string>
        {
            ["PlcCommunication:TargetDevices:Devices:0:DeviceType"] = "D",
            ["PlcCommunication:TargetDevices:Devices:0:DeviceNumber"] = "100"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        var loader = new ConfigurationLoader(configuration);

        // Act
        var config = loader.LoadPlcConnectionConfig();

        // Assert
        Assert.Equal("4E", config.FrameType); // デフォルト
        Assert.Equal((ushort)32, config.Timeout); // デフォルト（8000ms / 250）
    }
}
