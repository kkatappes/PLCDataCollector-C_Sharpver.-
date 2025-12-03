using Xunit;
using Andon.Core.Models.ConfigModels;

namespace Andon.Tests.Unit.Core.Models.ConfigModels;

/// <summary>
/// ConfigurationExtensions拡張メソッドのテスト
/// Phase 3-4: ConnectionConfig/TimeoutConfig生成処理の重複解消
/// </summary>
public class ConfigurationExtensionsTests
{
    [Fact]
    public void ToConnectionConfig_PlcConfiguration_正常変換()
    {
        // Arrange
        var plcConfig = new PlcConfiguration
        {
            IpAddress = "192.168.1.100",
            Port = 5000,
            ConnectionMethod = "TCP",
            IsBinary = true
        };

        // Act
        var result = plcConfig.ToConnectionConfig();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("192.168.1.100", result.IpAddress);
        Assert.Equal(5000, result.Port);
        Assert.True(result.UseTcp); // "TCP" → true
        Assert.True(result.IsBinary);
    }

    [Fact]
    public void ToConnectionConfig_PlcConfiguration_UDP変換()
    {
        // Arrange
        var plcConfig = new PlcConfiguration
        {
            IpAddress = "192.168.1.200",
            Port = 6000,
            ConnectionMethod = "UDP",
            IsBinary = false
        };

        // Act
        var result = plcConfig.ToConnectionConfig();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("192.168.1.200", result.IpAddress);
        Assert.Equal(6000, result.Port);
        Assert.False(result.UseTcp); // "UDP" → false
        Assert.False(result.IsBinary);
    }

    [Fact]
    public void ToTimeoutConfig_PlcConfiguration_正常変換()
    {
        // Arrange
        var plcConfig = new PlcConfiguration
        {
            Timeout = 3000
        };

        // Act
        var result = plcConfig.ToTimeoutConfig();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3000, result.ConnectTimeoutMs);
        Assert.Equal(3000, result.SendTimeoutMs);
        Assert.Equal(3000, result.ReceiveTimeoutMs);
    }

    [Fact]
    public void ToTimeoutConfig_PlcConfiguration_異なる値()
    {
        // Arrange
        var plcConfig = new PlcConfiguration
        {
            Timeout = 5000
        };

        // Act
        var result = plcConfig.ToTimeoutConfig();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5000, result.ConnectTimeoutMs);
        Assert.Equal(5000, result.SendTimeoutMs);
        Assert.Equal(5000, result.ReceiveTimeoutMs);
    }
}
