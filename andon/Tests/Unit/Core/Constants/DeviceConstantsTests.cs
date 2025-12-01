using Xunit;
using Andon.Core.Constants;

namespace Andon.Tests.Unit.Core.Constants;

/// <summary>
/// DeviceCode列挙型とDeviceCodeExtensionsのテスト
/// 移行計画書 Phase1 対応
/// </summary>
public class DeviceConstantsTests
{
    #region DeviceCode列挙型の基本テスト

    [Theory]
    [InlineData(DeviceCode.SM, 0x91)]
    [InlineData(DeviceCode.X, 0x9C)]
    [InlineData(DeviceCode.Y, 0x9D)]
    [InlineData(DeviceCode.M, 0x90)]
    [InlineData(DeviceCode.D, 0xA8)]
    [InlineData(DeviceCode.W, 0xB4)]
    [InlineData(DeviceCode.ZR, 0xB0)]
    public void DeviceCode_EnumValue_MatchesSLMPSpecification(DeviceCode code, byte expectedValue)
    {
        // Arrange & Act
        byte actualValue = (byte)code;

        // Assert
        Assert.Equal(expectedValue, actualValue);
    }

    #endregion

    #region IsHexAddress() テスト

    [Theory]
    [InlineData(DeviceCode.X, true)]
    [InlineData(DeviceCode.Y, true)]
    [InlineData(DeviceCode.W, true)]
    [InlineData(DeviceCode.B, true)]
    [InlineData(DeviceCode.ZR, true)]
    public void IsHexAddress_HexDevices_ReturnsTrue(DeviceCode code, bool expected)
    {
        // Act
        var result = code.IsHexAddress();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(DeviceCode.D, false)]
    [InlineData(DeviceCode.M, false)]
    [InlineData(DeviceCode.SM, false)]
    [InlineData(DeviceCode.R, false)]
    [InlineData(DeviceCode.TN, false)]
    public void IsHexAddress_DecimalDevices_ReturnsFalse(DeviceCode code, bool expected)
    {
        // Act
        var result = code.IsHexAddress();

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region IsBitDevice() テスト

    [Theory]
    [InlineData(DeviceCode.SM, true)]
    [InlineData(DeviceCode.X, true)]
    [InlineData(DeviceCode.Y, true)]
    [InlineData(DeviceCode.M, true)]
    [InlineData(DeviceCode.L, true)]
    [InlineData(DeviceCode.F, true)]
    [InlineData(DeviceCode.B, true)]
    [InlineData(DeviceCode.TS, true)]
    [InlineData(DeviceCode.TC, true)]
    [InlineData(DeviceCode.CS, true)]
    [InlineData(DeviceCode.CC, true)]
    public void IsBitDevice_BitDevices_ReturnsTrue(DeviceCode code, bool expected)
    {
        // Act
        var result = code.IsBitDevice();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(DeviceCode.D, false)]
    [InlineData(DeviceCode.W, false)]
    [InlineData(DeviceCode.R, false)]
    [InlineData(DeviceCode.ZR, false)]
    [InlineData(DeviceCode.TN, false)]
    [InlineData(DeviceCode.CN, false)]
    public void IsBitDevice_WordDevices_ReturnsFalse(DeviceCode code, bool expected)
    {
        // Act
        var result = code.IsBitDevice();

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region IsReadRandomSupported() テスト（SLMP仕様書 page_64.png準拠）

    [Theory]
    [InlineData(DeviceCode.D, true)]
    [InlineData(DeviceCode.M, true)]
    [InlineData(DeviceCode.W, true)]
    [InlineData(DeviceCode.X, true)]
    [InlineData(DeviceCode.Y, true)]
    [InlineData(DeviceCode.TN, true)]  // タイマ現在値はOK
    [InlineData(DeviceCode.CN, true)]  // カウンタ現在値はOK
    public void IsReadRandomSupported_SupportedDevices_ReturnsTrue(DeviceCode code, bool expected)
    {
        // Act
        var result = code.IsReadRandomSupported();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(DeviceCode.TS, false)]  // タイマ接点は非対応
    [InlineData(DeviceCode.TC, false)]  // タイマコイルは非対応
    [InlineData(DeviceCode.CS, false)]  // カウンタ接点は非対応
    [InlineData(DeviceCode.CC, false)]  // カウンタコイルは非対応
    public void IsReadRandomSupported_RestrictedDevices_ReturnsFalse(DeviceCode code, bool expected)
    {
        // Act
        var result = code.IsReadRandomSupported();

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region 複合条件テスト

    [Fact]
    public void DeviceCodeExtensions_ConmoniTestDevices_ValidConfiguration()
    {
        // Arrange - conmoni_testで使用されるデバイスコード検証
        var dDevice = DeviceCode.D;    // D61000等
        var mDevice = DeviceCode.M;    // M機器

        // Act & Assert - D機器
        Assert.False(dDevice.IsHexAddress());      // 10進表記
        Assert.False(dDevice.IsBitDevice());       // ワード型
        Assert.True(dDevice.IsReadRandomSupported());  // ReadRandom対応

        // Act & Assert - M機器
        Assert.False(mDevice.IsHexAddress());      // 10進表記
        Assert.True(mDevice.IsBitDevice());        // ビット型
        Assert.True(mDevice.IsReadRandomSupported());  // ReadRandom対応
    }

    [Fact]
    public void DeviceCodeExtensions_W機器_HexAddressAndWordDevice()
    {
        // Arrange - W機器（16進表記のワード型）
        var wDevice = DeviceCode.W;

        // Act & Assert
        Assert.True(wDevice.IsHexAddress());       // 16進表記
        Assert.False(wDevice.IsBitDevice());       // ワード型（ビット型ではない）
        Assert.True(wDevice.IsReadRandomSupported());  // ReadRandom対応
    }

    [Fact]
    public void DeviceCodeExtensions_X機器_HexAddressAndBitDevice()
    {
        // Arrange - X機器（16進表記のビット型）
        var xDevice = DeviceCode.X;

        // Act & Assert
        Assert.True(xDevice.IsHexAddress());       // 16進表記
        Assert.True(xDevice.IsBitDevice());        // ビット型
        Assert.True(xDevice.IsReadRandomSupported());  // ReadRandom対応
    }

    #endregion

    #region エラーケーステスト

    [Fact]
    public void DeviceCodeExtensions_TimerContactDevice_NotSupportedForReadRandom()
    {
        // Arrange - タイマ接点（ReadRandom制約対象）
        var tsDevice = DeviceCode.TS;

        // Act & Assert
        Assert.False(tsDevice.IsHexAddress());     // 10進表記
        Assert.True(tsDevice.IsBitDevice());       // ビット型
        Assert.False(tsDevice.IsReadRandomSupported());  // ReadRandom非対応（重要）
    }

    [Fact]
    public void DeviceCodeExtensions_TimerCurrentValue_SupportedForReadRandom()
    {
        // Arrange - タイマ現在値（ReadRandom対応）
        var tnDevice = DeviceCode.TN;

        // Act & Assert
        Assert.False(tnDevice.IsHexAddress());     // 10進表記
        Assert.False(tnDevice.IsBitDevice());      // ワード型
        Assert.True(tnDevice.IsReadRandomSupported());   // ReadRandom対応（重要）
    }

    #endregion
}
