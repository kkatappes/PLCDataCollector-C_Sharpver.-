using Xunit;
using Andon.Core.Constants;
using Andon.Core.Models;

namespace Andon.Tests.Unit.Core.Models;

/// <summary>
/// DeviceSpecificationクラスのテスト
/// 移行計画書 Phase1 対応
/// conmoni_testの実データ基準
/// </summary>
public class DeviceSpecificationTests
{
    #region コンストラクタテスト

    [Fact]
    public void Constructor_D100_CreatesValidInstance()
    {
        // Arrange & Act
        var device = new DeviceSpecification(DeviceCode.D, 100);

        // Assert
        Assert.Equal(DeviceCode.D, device.Code);
        Assert.Equal(100, device.DeviceNumber);
        Assert.False(device.IsHexAddress); // Dデバイスは10進
    }

    [Fact]
    public void Constructor_W機器_AutoDetectsHexAddress()
    {
        // Arrange & Act
        var device = new DeviceSpecification(DeviceCode.W, 0x11AA);

        // Assert
        Assert.Equal(DeviceCode.W, device.Code);
        Assert.Equal(0x11AA, device.DeviceNumber);
        Assert.True(device.IsHexAddress); // Wデバイスは16進（自動判定）
    }

    [Fact]
    public void Constructor_ExplicitHexFlag_OverridesAutoDetection()
    {
        // Arrange & Act - Dデバイスを強制的に16進扱い
        var device = new DeviceSpecification(DeviceCode.D, 100, isHexAddress: true);

        // Assert
        Assert.Equal(DeviceCode.D, device.Code);
        Assert.Equal(100, device.DeviceNumber);
        Assert.True(device.IsHexAddress); // 明示的にtrueを指定
    }

    #endregion

    #region FromHexString() テスト

    [Fact]
    public void FromHexString_W11AA_CreatesCorrectInstance()
    {
        // Arrange & Act
        var device = DeviceSpecification.FromHexString(DeviceCode.W, "11AA");

        // Assert
        Assert.Equal(DeviceCode.W, device.Code);
        Assert.Equal(0x11AA, device.DeviceNumber); // 16進→10進変換: 4522
        Assert.True(device.IsHexAddress);
    }

    [Fact]
    public void FromHexString_EmptyString_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            DeviceSpecification.FromHexString(DeviceCode.W, ""));

        Assert.Contains("16進文字列が空", ex.Message);
    }

    [Fact]
    public void FromHexString_InvalidFormat_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            DeviceSpecification.FromHexString(DeviceCode.W, "GHIJ"));

        Assert.Contains("不正な16進文字列", ex.Message);
    }

    [Fact]
    public void FromHexString_LowerCase_WorksCorrectly()
    {
        // Arrange & Act
        var device = DeviceSpecification.FromHexString(DeviceCode.W, "11aa");

        // Assert
        Assert.Equal(0x11AA, device.DeviceNumber); // 大文字小文字関係なく変換
    }

    #endregion

    #region ToDeviceNumberBytes() テスト（conmoni_test実データ基準）

    [Fact]
    public void ToDeviceNumberBytes_D100_ReturnsCorrectLittleEndian()
    {
        // Arrange
        var device = new DeviceSpecification(DeviceCode.D, 100);

        // Act
        var bytes = device.ToDeviceNumberBytes();

        // Assert
        Assert.Equal(3, bytes.Length);
        Assert.Equal(0x64, bytes[0]);   // 100 = 0x64（下位）
        Assert.Equal(0x00, bytes[1]);   // 中位
        Assert.Equal(0x00, bytes[2]);   // 上位
    }

    [Fact]
    public void ToDeviceNumberBytes_D61000_ReturnsCorrectLittleEndian()
    {
        // Arrange - conmoni_test実データ: D61000 (0xEE48)
        var device = new DeviceSpecification(DeviceCode.D, 61000);

        // Act
        var bytes = device.ToDeviceNumberBytes();

        // Assert
        Assert.Equal(3, bytes.Length);
        Assert.Equal(0x48, bytes[0]);   // 0xEE48の下位バイト
        Assert.Equal(0xEE, bytes[1]);   // 0xEE48の上位バイト
        Assert.Equal(0x00, bytes[2]);   // 上位バイト（3バイト目）
    }

    [Fact]
    public void ToDeviceNumberBytes_W11AA_ReturnsCorrectLittleEndian()
    {
        // Arrange - conmoni_test実データ: W0x11AA
        var device = DeviceSpecification.FromHexString(DeviceCode.W, "11AA");

        // Act
        var bytes = device.ToDeviceNumberBytes();

        // Assert
        Assert.Equal(3, bytes.Length);
        Assert.Equal(0xAA, bytes[0]);   // 0x11AAの下位バイト
        Assert.Equal(0x11, bytes[1]);   // 0x11AAの上位バイト
        Assert.Equal(0x00, bytes[2]);   // 上位バイト（3バイト目）
    }

    [Fact]
    public void ToDeviceNumberBytes_MaxValue_ReturnsCorrect3Bytes()
    {
        // Arrange - 3バイト最大値: 0xFFFFFF (16777215)
        var device = new DeviceSpecification(DeviceCode.D, 0xFFFFFF);

        // Act
        var bytes = device.ToDeviceNumberBytes();

        // Assert
        Assert.Equal(3, bytes.Length);
        Assert.Equal(0xFF, bytes[0]);   // 下位バイト
        Assert.Equal(0xFF, bytes[1]);   // 中位バイト
        Assert.Equal(0xFF, bytes[2]);   // 上位バイト
    }

    #endregion

    #region ToDeviceSpecificationBytes() テスト（SLMP仕様書準拠）

    [Fact]
    public void ToDeviceSpecificationBytes_D100_Returns4BytesWithDeviceCode()
    {
        // Arrange
        var device = new DeviceSpecification(DeviceCode.D, 100);

        // Act
        var bytes = device.ToDeviceSpecificationBytes();

        // Assert
        Assert.Equal(4, bytes.Length);
        Assert.Equal(0x64, bytes[0]);   // デバイス番号下位
        Assert.Equal(0x00, bytes[1]);   // デバイス番号中位
        Assert.Equal(0x00, bytes[2]);   // デバイス番号上位
        Assert.Equal(0xA8, bytes[3]);   // Dデバイスコード
    }

    [Fact]
    public void ToDeviceSpecificationBytes_D61000_MatchesConmoniTestData()
    {
        // Arrange - conmoni_test実データ: [0x48, 0xEE, 0x00, 0xA8]
        var device = new DeviceSpecification(DeviceCode.D, 61000);

        // Act
        var bytes = device.ToDeviceSpecificationBytes();

        // Assert - conmoni_testとの完全一致検証
        Assert.Equal(4, bytes.Length);
        Assert.Equal(0x48, bytes[0]);   // 72
        Assert.Equal(0xEE, bytes[1]);   // 238
        Assert.Equal(0x00, bytes[2]);   // 0
        Assert.Equal(0xA8, bytes[3]);   // 168 (Dデバイスコード)
    }

    [Fact]
    public void ToDeviceSpecificationBytes_M機器_UsesCorrectDeviceCode()
    {
        // Arrange - conmoni_test実データ: M機器（デバイスコード0x90）
        var device = new DeviceSpecification(DeviceCode.M, 57010);

        // Act
        var bytes = device.ToDeviceSpecificationBytes();

        // Assert
        Assert.Equal(4, bytes.Length);
        Assert.Equal(0xB2, bytes[0]);   // 57010 = 0xDEB2の下位
        Assert.Equal(0xDE, bytes[1]);   // 中位
        Assert.Equal(0x00, bytes[2]);   // 上位
        Assert.Equal(0x90, bytes[3]);   // Mデバイスコード
    }

    #endregion

    #region ToString() テスト

    [Fact]
    public void ToString_D100_ReturnsDecimalFormat()
    {
        // Arrange
        var device = new DeviceSpecification(DeviceCode.D, 100);

        // Act
        var result = device.ToString();

        // Assert
        Assert.Equal("D100", result);
    }

    [Fact]
    public void ToString_W11AA_ReturnsHexFormat()
    {
        // Arrange
        var device = DeviceSpecification.FromHexString(DeviceCode.W, "11AA");

        // Act
        var result = device.ToString();

        // Assert
        Assert.Equal("W0x11AA", result);
    }

    [Fact]
    public void ToString_M機器_ReturnsDecimalFormat()
    {
        // Arrange
        var device = new DeviceSpecification(DeviceCode.M, 200);

        // Act
        var result = device.ToString();

        // Assert
        Assert.Equal("M200", result);
    }

    #endregion

    #region Equals() & GetHashCode() テスト

    [Fact]
    public void Equals_SameDeviceCodeAndNumber_ReturnsTrue()
    {
        // Arrange
        var device1 = new DeviceSpecification(DeviceCode.D, 100);
        var device2 = new DeviceSpecification(DeviceCode.D, 100);

        // Act & Assert
        Assert.Equal(device1, device2);
        Assert.True(device1.Equals(device2));
    }

    [Fact]
    public void Equals_DifferentDeviceNumber_ReturnsFalse()
    {
        // Arrange
        var device1 = new DeviceSpecification(DeviceCode.D, 100);
        var device2 = new DeviceSpecification(DeviceCode.D, 101);

        // Act & Assert
        Assert.NotEqual(device1, device2);
    }

    [Fact]
    public void GetHashCode_SameDevice_ReturnsSameHashCode()
    {
        // Arrange
        var device1 = new DeviceSpecification(DeviceCode.D, 100);
        var device2 = new DeviceSpecification(DeviceCode.D, 100);

        // Act & Assert
        Assert.Equal(device1.GetHashCode(), device2.GetHashCode());
    }

    #endregion

    #region ValidateForReadRandom() テスト

    [Fact]
    public void ValidateForReadRandom_SupportedDevice_DoesNotThrow()
    {
        // Arrange
        var device = new DeviceSpecification(DeviceCode.D, 100);

        // Act & Assert - 例外がスローされないことを確認
        device.ValidateForReadRandom();
    }

    [Fact]
    public void ValidateForReadRandom_RestrictedDevice_ThrowsInvalidOperationException()
    {
        // Arrange - タイマ接点（ReadRandom非対応）
        var device = new DeviceSpecification(DeviceCode.TS, 0);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            device.ValidateForReadRandom());

        Assert.Contains("ReadRandomコマンドで指定できません", ex.Message);
        Assert.Contains("TS0", ex.Message); // デバイス名が含まれる
    }

    #endregion

    #region ValidateDeviceNumberRange() テスト

    [Fact]
    public void ValidateDeviceNumberRange_ValidRange_DoesNotThrow()
    {
        // Arrange
        var device1 = new DeviceSpecification(DeviceCode.D, 0);          // 最小値
        var device2 = new DeviceSpecification(DeviceCode.D, 0xFFFFFF);   // 最大値

        // Act & Assert
        device1.ValidateDeviceNumberRange();
        device2.ValidateDeviceNumberRange();
    }

    [Fact]
    public void ValidateDeviceNumberRange_NegativeNumber_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var device = new DeviceSpecification(DeviceCode.D, -1);

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            device.ValidateDeviceNumberRange());

        Assert.Contains("範囲外", ex.Message);
    }

    [Fact]
    public void ValidateDeviceNumberRange_ExceedsMax_ThrowsArgumentOutOfRangeException()
    {
        // Arrange - 3バイト最大値を超える
        var device = new DeviceSpecification(DeviceCode.D, 0x1000000); // 16777216

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            device.ValidateDeviceNumberRange());

        Assert.Contains("範囲外", ex.Message);
    }

    #endregion

    #region 統合テスト（conmoni_test実データ基準）

    [Fact]
    public void IntegrationTest_ConmoniTestD61000_CompleteFlow()
    {
        // Arrange - conmoni_testの実データを再現
        var device = new DeviceSpecification(DeviceCode.D, 61000);

        // Act
        var deviceBytes = device.ToDeviceSpecificationBytes();
        var displayName = device.ToString();

        // Assert - conmoni_testとの完全一致
        Assert.Equal("D61000", displayName);
        Assert.Equal(new byte[] { 0x48, 0xEE, 0x00, 0xA8 }, deviceBytes);

        // ReadRandom対応確認
        device.ValidateForReadRandom();  // 例外がスローされない

        // 範囲検証
        device.ValidateDeviceNumberRange();  // 例外がスローされない
    }

    [Fact]
    public void IntegrationTest_ConmoniTestW11AA_CompleteFlow()
    {
        // Arrange - conmoni_testの16進デバイスを再現
        var device = DeviceSpecification.FromHexString(DeviceCode.W, "11AA");

        // Act
        var deviceBytes = device.ToDeviceSpecificationBytes();
        var displayName = device.ToString();

        // Assert
        Assert.Equal("W0x11AA", displayName);
        Assert.Equal(new byte[] { 0xAA, 0x11, 0x00, 0xB4 }, deviceBytes);

        // ReadRandom対応確認
        device.ValidateForReadRandom();

        // 範囲検証
        device.ValidateDeviceNumberRange();
    }

    [Fact]
    public void IntegrationTest_MultipleDevices_CreatesList()
    {
        // Arrange - conmoni_testの複数デバイス指定を再現
        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 61000),  // D61000
            new DeviceSpecification(DeviceCode.D, 61003),  // D61003
            new DeviceSpecification(DeviceCode.M, 57010),  // M57010
        };

        // Act
        var allValid = devices.All(d =>
        {
            try
            {
                d.ValidateForReadRandom();
                d.ValidateDeviceNumberRange();
                return true;
            }
            catch
            {
                return false;
            }
        });

        // Assert
        Assert.True(allValid);
        Assert.Equal(3, devices.Count);
    }

    #endregion
}
