using Xunit;
using Andon.Utilities;
using Andon.Core.Constants;
using Andon.Core.Models;
using System;
using System.Collections.Generic;

namespace Andon.Tests.Unit.Utilities;

/// <summary>
/// SlmpDataParserのテスト（Phase5 Step14）
/// </summary>
public class SlmpDataParserTests
{
    [Fact]
    public void ParseReadRandomResponse_4EFrame_ValidResponse_ReturnsCorrectData()
    {
        // Arrange
        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.M, 0),
            new DeviceSpecification(DeviceCode.M, 16),
            new DeviceSpecification(DeviceCode.D, 100)
        };

        // 4Eフレーム応答（15バイトヘッダ + 6バイトデータ = 21バイト）
        byte[] responseFrame = new byte[]
        {
            // サブヘッダ2バイト
            0xD4, 0x00,
            // シーケンス番号2バイト
            0x00, 0x00,
            // 予約2バイト
            0x00, 0x00,
            // ネットワーク番号1バイト
            0x00,
            // PC番号1バイト
            0xFF,
            // I/O番号2バイト（LE: 0x03FF）
            0xFF, 0x03,
            // マルチドロップ局番1バイト
            0x00,
            // データ長2バイト（LE: 8バイト = エンドコード2 + データ6）
            0x08, 0x00,
            // エンドコード2バイト（正常）
            0x00, 0x00,
            // デバイスデータ6バイト（3ワード × 2バイト）
            0x01, 0x00,  // M0 = 0x0001
            0x02, 0x00,  // M16 = 0x0002
            0x64, 0x00   // D100 = 0x0064 = 100
        };

        // Act
        var result = SlmpDataParser.ParseReadRandomResponse(responseFrame, devices);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("M0", result["M0"].DeviceName);
        Assert.Equal(1u, result["M0"].Value);
        Assert.Equal("M16", result["M16"].DeviceName);
        Assert.Equal(2u, result["M16"].Value);
        Assert.Equal("D100", result["D100"].DeviceName);
        Assert.Equal(100u, result["D100"].Value);
    }

    [Fact]
    public void ParseReadRandomResponse_3EFrame_ValidResponse_ReturnsCorrectData()
    {
        // Arrange
        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100),
            new DeviceSpecification(DeviceCode.D, 200)
        };

        // 3Eフレーム応答（11バイトヘッダ + 4バイトデータ = 15バイト）
        byte[] responseFrame = new byte[]
        {
            // サブヘッダ2バイト
            0xD0, 0x00,
            // ネットワーク番号1バイト
            0x00,
            // PC番号1バイト
            0xFF,
            // I/O番号2バイト（LE: 0x03FF）
            0xFF, 0x03,
            // マルチドロップ局番1バイト
            0x00,
            // データ長2バイト（LE: 6バイト = エンドコード2 + データ4）
            0x06, 0x00,
            // エンドコード2バイト（正常）
            0x00, 0x00,
            // デバイスデータ4バイト（2ワード × 2バイト）
            0x64, 0x00,  // D100 = 0x0064 = 100
            0xC8, 0x00   // D200 = 0x00C8 = 200
        };

        // Act
        var result = SlmpDataParser.ParseReadRandomResponse(responseFrame, devices);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(100u, result["D100"].Value);
        Assert.Equal(200u, result["D200"].Value);
    }

    [Fact]
    public void ParseReadRandomResponse_HexAddressDevice_ReturnsCorrectKey()
    {
        // Arrange
        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.W, 0x11AA, isHexAddress: true)
        };

        byte[] responseFrame = new byte[]
        {
            0xD4, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF,
            0xFF, 0x03, 0x00, 0x04, 0x00, 0x00, 0x00,
            0x99, 0x26  // W0x11AA = 0x2699 = 9881
        };

        // Act
        var result = SlmpDataParser.ParseReadRandomResponse(responseFrame, devices);

        // Assert
        Assert.Single(result);
        Assert.True(result.ContainsKey("W0x11AA"));
        Assert.Equal(9881u, result["W0x11AA"].Value);
    }

    [Fact]
    public void ParseReadRandomResponse_ErrorEndCode_ThrowsException()
    {
        // Arrange
        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100)
        };

        // エラーレスポンス（エンドコード = 0xC051 = デバイス範囲エラー）
        byte[] responseFrame = new byte[]
        {
            0xD0, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00,
            0x02, 0x00,  // データ長: 2バイト（エンドコードのみ）
            0x51, 0xC0   // エンドコード: 0xC051（エラー）
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => SlmpDataParser.ParseReadRandomResponse(responseFrame, devices)
        );
        Assert.Contains("エラー応答", ex.Message);
        Assert.Contains("C051", ex.Message);
    }

    [Fact]
    public void ParseReadRandomResponse_EmptyFrame_ThrowsException()
    {
        // Arrange
        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100)
        };
        byte[] emptyFrame = Array.Empty<byte>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(
            () => SlmpDataParser.ParseReadRandomResponse(emptyFrame, devices)
        );
        Assert.Contains("レスポンスフレームが空", ex.Message);
    }

    [Fact]
    public void ParseReadRandomResponse_InvalidSubHeader_ThrowsException()
    {
        // Arrange
        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100)
        };

        // 不正なサブヘッダ
        byte[] invalidFrame = new byte[]
        {
            0xFF, 0xFF,  // 不正なサブヘッダ
            0x00, 0x00, 0x00, 0x00, 0x00, 0xFF,
            0xFF, 0x03, 0x00, 0x04, 0x00, 0x00, 0x00
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => SlmpDataParser.ParseReadRandomResponse(invalidFrame, devices)
        );
        Assert.Contains("未対応のフレームタイプ", ex.Message);
    }

    [Fact]
    public void ParseReadRandomResponse_MemoMdRealDataSimplified_ReturnsCorrectCount()
    {
        // Arrange: memo.mdの実データを簡易版でテスト（詳細検証は統合テストで実施）
        var devices = new List<DeviceSpecification>();
        for (int i = 0; i < 10; i++)
        {
            devices.Add(new DeviceSpecification(DeviceCode.D, i));
        }

        // 4Eフレーム（15バイトヘッダ + 2バイトエンドコード + 20バイトデータ）
        byte[] responseFrame = new byte[]
        {
            // ヘッダ（15バイト）
            0xD4, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF,
            0xFF, 0x03, 0x00, 0x16, 0x00,  // データ長: 22バイト (エンドコード2 + データ20)
            // エンドコード（2バイト）
            0x00, 0x00,
            // デバイスデータ（10ワード = 20バイト）
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF
        };

        // Act
        var result = SlmpDataParser.ParseReadRandomResponse(responseFrame, devices);

        // Assert
        Assert.Equal(10, result.Count);
        Assert.All(result.Values, data => Assert.Equal(0xFFFFu, data.Value));
        Assert.True(result.ContainsKey("D0"));
        Assert.True(result.ContainsKey("D9"));
    }

    [Fact]
    public void ParseReadRandomResponse_InsufficientDataSize_ThrowsException()
    {
        // Arrange
        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100),
            new DeviceSpecification(DeviceCode.D, 200)
        };

        // データ部が1ワード分不足（2バイト不足）
        byte[] responseFrame = new byte[]
        {
            0xD4, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF,
            0xFF, 0x03, 0x00, 0x04, 0x00, 0x00, 0x00,
            0x64, 0x00   // 1ワードのみ（2ワード必要）
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => SlmpDataParser.ParseReadRandomResponse(responseFrame, devices)
        );
        Assert.Contains("サイズが不足", ex.Message);
    }
}
