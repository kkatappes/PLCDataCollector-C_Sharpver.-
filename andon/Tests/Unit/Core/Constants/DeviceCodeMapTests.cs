using Andon.Core.Constants;
using Xunit;

namespace Andon.Tests.Unit.Core.Constants;

/// <summary>
/// DeviceCodeMapクラスのテスト（Phase1）
/// TDD Red-Green-Refactorサイクルに従って実装
/// </summary>
public class DeviceCodeMapTests
{
    #region デバイスコード取得テスト

    [Theory]
    [InlineData("SM", 0x91)]   // 特殊リレー
    [InlineData("M", 0x90)]    // 内部リレー
    [InlineData("L", 0x92)]    // ラッチリレー
    [InlineData("F", 0x93)]    // アナンシエータ
    [InlineData("V", 0x94)]    // エッジリレー
    [InlineData("B", 0xA0)]    // リンクリレー
    [InlineData("SB", 0xA1)]   // リンク特殊リレー
    [InlineData("X", 0x9C)]    // 入力
    [InlineData("Y", 0x9D)]    // 出力
    [InlineData("DX", 0xA2)]   // ダイレクト入力
    [InlineData("DY", 0xA3)]   // ダイレクト出力
    [InlineData("TS", 0xC1)]   // タイマ接点
    [InlineData("TC", 0xC0)]   // タイマコイル
    [InlineData("STS", 0xC7)]  // 積算タイマ接点
    [InlineData("STC", 0xC6)]  // 積算タイマコイル
    [InlineData("CS", 0xC4)]   // カウンタ接点
    [InlineData("CC", 0xC3)]   // カウンタコイル
    [InlineData("SD", 0xA9)]   // 特殊レジスタ
    [InlineData("D", 0xA8)]    // データレジスタ
    [InlineData("W", 0xB4)]    // リンクレジスタ
    [InlineData("SW", 0xB5)]   // リンク特殊レジスタ
    [InlineData("TN", 0xC2)]   // タイマ現在値
    [InlineData("STN", 0xC8)]  // 積算タイマ現在値
    [InlineData("CN", 0xC5)]   // カウンタ現在値
    [InlineData("Z", 0xCC)]    // インデックスレジスタ
    [InlineData("R", 0xAF)]    // ファイルレジスタ
    [InlineData("ZR", 0xB0)]   // ファイルレジスタ（拡張）
    public void GetDeviceCode_ValidDeviceType_ReturnsCorrectCode(string deviceType, byte expectedCode)
    {
        // Act
        var result = DeviceCodeMap.GetDeviceCode(deviceType);

        // Assert
        Assert.Equal(expectedCode, result);
    }

    [Theory]
    [InlineData("sm")]  // 小文字
    [InlineData("SM")]  // 大文字
    [InlineData("Sm")]  // 混在
    public void GetDeviceCode_CaseInsensitive_ReturnsCorrectCode(string deviceType)
    {
        // Act
        var result = DeviceCodeMap.GetDeviceCode(deviceType);

        // Assert
        Assert.Equal(0x91, result); // SM = 0x91
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("")]
    [InlineData("XYZ")]
    public void GetDeviceCode_InvalidDeviceType_ThrowsArgumentException(string deviceType)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => DeviceCodeMap.GetDeviceCode(deviceType));
        Assert.Contains("未対応のデバイスタイプ", exception.Message);
    }

    #endregion

    #region 16進デバイス判定テスト

    [Theory]
    [InlineData("X", true)]    // 16進デバイス
    [InlineData("Y", true)]
    [InlineData("B", true)]
    [InlineData("SB", true)]
    [InlineData("DX", true)]
    [InlineData("DY", true)]
    [InlineData("M", false)]   // 10進デバイス
    [InlineData("D", false)]
    [InlineData("W", false)]
    public void IsHexDevice_VariousDevices_ReturnsCorrectResult(string deviceType, bool expected)
    {
        // Act
        var result = DeviceCodeMap.IsHexDevice(deviceType);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region ビットデバイス判定テスト

    [Theory]
    [InlineData("SM", true)]   // ビットデバイス
    [InlineData("M", true)]
    [InlineData("L", true)]
    [InlineData("F", true)]
    [InlineData("V", true)]
    [InlineData("B", true)]
    [InlineData("X", true)]
    [InlineData("Y", true)]
    [InlineData("TS", true)]
    [InlineData("TC", true)]
    [InlineData("CS", true)]
    [InlineData("CC", true)]
    [InlineData("D", false)]   // ワードデバイス
    [InlineData("W", false)]
    [InlineData("SD", false)]
    [InlineData("TN", false)]
    [InlineData("CN", false)]
    public void IsBitDevice_VariousDevices_ReturnsCorrectResult(string deviceType, bool expected)
    {
        // Act
        var result = DeviceCodeMap.IsBitDevice(deviceType);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region デバイスタイプ検証テスト

    [Theory]
    [InlineData("M", true)]
    [InlineData("D", true)]
    [InlineData("X", true)]
    [InlineData("INVALID", false)]
    [InlineData("", false)]
    public void IsValidDeviceType_VariousInputs_ReturnsCorrectResult(string deviceType, bool expected)
    {
        // Act
        var result = DeviceCodeMap.IsValidDeviceType(deviceType);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region 全24種類デバイス網羅テスト

    [Fact]
    public void GetDeviceCode_All24DeviceTypes_Supported()
    {
        // Arrange: 24種類全てのデバイスタイプ
        var allDeviceTypes = new[]
        {
            // ビットデバイス（10進） - 11種類
            "SM", "M", "L", "F", "V", "TS", "TC", "STS", "STC", "CS", "CC",
            // ビットデバイス（16進） - 6種類
            "X", "Y", "B", "SB", "DX", "DY",
            // ワードデバイス（10進） - 10種類
            "SD", "D", "W", "SW", "TN", "STN", "CN", "Z", "R", "ZR"
        };

        // Act & Assert: 全てのデバイスタイプで例外が発生しないこと
        foreach (var deviceType in allDeviceTypes)
        {
            var code = DeviceCodeMap.GetDeviceCode(deviceType);
            Assert.True(code > 0, $"デバイス {deviceType} のコードが不正: {code}");
        }

        // 合計27種類であることを確認（Phase1設計書では24種類と記載されているが、実際は27種類）
        Assert.Equal(27, allDeviceTypes.Length);
    }

    #endregion
}
