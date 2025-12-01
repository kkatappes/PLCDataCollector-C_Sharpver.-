using Xunit;
using Andon.Core.Models;
using Andon.Core.Constants;

namespace Andon.Tests.Unit.Core.Models;

/// <summary>
/// DeviceDataクラスのテスト（Phase5 Step14-A）
/// </summary>
public class DeviceDataTests
{
    [Fact]
    public void FromDeviceSpecification_WordDevice_CreatesCorrectDeviceData()
    {
        // Arrange
        var device = new DeviceSpecification(DeviceCode.D, 100);
        ushort value = 1234;

        // Act
        var result = DeviceData.FromDeviceSpecification(device, value);

        // Assert
        Assert.Equal("D100", result.DeviceName);
        Assert.Equal(DeviceCode.D, result.Code);
        Assert.Equal(100, result.Address);
        Assert.Equal((uint)1234, result.Value);
        Assert.False(result.IsDWord);
        Assert.False(result.IsHexAddress);
    }

    [Fact]
    public void FromDeviceSpecification_HexAddressDevice_CreatesCorrectDeviceData()
    {
        // Arrange
        var device = new DeviceSpecification(DeviceCode.W, 0x11AA, isHexAddress: true);
        ushort value = 9999;

        // Act
        var result = DeviceData.FromDeviceSpecification(device, value);

        // Assert
        Assert.Equal("W0x11AA", result.DeviceName);
        Assert.Equal(DeviceCode.W, result.Code);
        Assert.Equal(0x11AA, result.Address);
        Assert.Equal((uint)9999, result.Value);
        Assert.False(result.IsDWord);
        Assert.True(result.IsHexAddress);
    }

    [Fact]
    public void FromDWordDevice_TwoWords_CreatesCombined32BitValue()
    {
        // Arrange
        var device = new DeviceSpecification(DeviceCode.D, 200);
        ushort lowerWord = 0x1234;  // 下位ワード
        ushort upperWord = 0x5678;  // 上位ワード

        // Act
        var result = DeviceData.FromDWordDevice(device, lowerWord, upperWord);

        // Assert
        Assert.Equal("D200", result.DeviceName);
        Assert.Equal(DeviceCode.D, result.Code);
        Assert.Equal(200, result.Address);
        Assert.Equal(0x56781234u, result.Value);  // 上位ワード << 16 | 下位ワード
        Assert.True(result.IsDWord);
        Assert.False(result.IsHexAddress);
    }

    [Fact]
    public void FromDWordDevice_MaxValues_HandlesCorrectly()
    {
        // Arrange
        var device = new DeviceSpecification(DeviceCode.D, 0);
        ushort lowerWord = 0xFFFF;
        ushort upperWord = 0xFFFF;

        // Act
        var result = DeviceData.FromDWordDevice(device, lowerWord, upperWord);

        // Assert
        Assert.Equal(0xFFFFFFFFu, result.Value);
        Assert.True(result.IsDWord);
    }

    [Fact]
    public void FromDeviceSpecification_BitDevice_CreatesCorrectDeviceData()
    {
        // Arrange
        var device = new DeviceSpecification(DeviceCode.M, 16);
        ushort value = 0x0001;  // ビットデバイスもワード単位で読み取る

        // Act
        var result = DeviceData.FromDeviceSpecification(device, value);

        // Assert
        Assert.Equal("M16", result.DeviceName);
        Assert.Equal(DeviceCode.M, result.Code);
        Assert.Equal(16, result.Address);
        Assert.Equal((uint)1, result.Value);
        Assert.False(result.IsDWord);
    }
}
