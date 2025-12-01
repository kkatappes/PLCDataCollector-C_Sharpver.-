using System;
using System.Text;
using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;

namespace Andon.Tests.Unit.Core.Managers;

/// <summary>
/// PlcCommunicationManager ASCII フレームパース機能テスト
/// TDD Phase 1: Red - テスト先行実装
/// TC037_ASCII, TC038_ASCII 対応
/// </summary>
public class PlcCommunicationManagerTests_AsciiParsing
{
    #region TC037_ASCII: 3E ASCIIフレーム解析テスト

    /// <summary>
    /// TC037_3E_ASCII: 3E ASCIIフレーム ビットデータ解析成功
    ///
    /// 入力フレーム: "D00000FF03FF000004000000C800"
    /// フレーム構造:
    ///   [0-1]   "D0"      サブヘッダ（3E ASCII応答）
    ///   [2-3]   "00"      ネットワーク番号
    ///   [4-5]   "FF"      PC番号
    ///   [6-9]   "03FF"    I/O番号
    ///   [10-11] "00"      局番
    ///   [12-15] "0004"    データ長（4バイト）
    ///   [16-19] "0000"    終了コード
    ///   [20-]   "C800"    デバイスデータ
    ///
    /// 期待出力:
    ///   - EndCode: 0x0000
    ///   - DeviceDataAscii: "C800"
    ///   - ビット値: M100=0, M101=0, M102=0, M103=1, M104=0, M105=0, M106=1, M107=1
    /// </summary>
    [Fact]
    public void TC037_3E_ASCII_ビットデータ解析成功()
    {
        // Arrange: 3E ASCIIフレームデータ作成
        string asciiFrame = "D000FF03FF0000040000C800";
        byte[] rawData = Encoding.ASCII.GetBytes(asciiFrame);

        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.3.250",
            Port = 5007,
            UseTcp = false,
            IsBinary = false,
            FrameVersion = FrameVersion.Frame3E
        };

        var manager = new PlcCommunicationManager(connectionConfig, new TimeoutConfig());

        // Act: Parse3EFrameAscii 呼び出し
        // Note: プライベートメソッドのため、リフレクションで呼び出し
        var method = typeof(PlcCommunicationManager).GetMethod(
            "Parse3EFrameAscii",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        var result = method?.Invoke(manager, new object[] { rawData });

        // Assert: 戻り値を検証
        Assert.NotNull(result);

        // タプル型の戻り値を取得（Item1=EndCode, Item2=DeviceDataAscii）
        var resultType = result.GetType();
        var endCode = (ushort)resultType.GetField("Item1")?.GetValue(result)!;
        var deviceDataAscii = (string)resultType.GetField("Item2")?.GetValue(result)!;

        Assert.Equal((ushort)0x0000, endCode);
        Assert.Equal("C800", deviceDataAscii);
    }

    /// <summary>
    /// TC037_3E_ASCII: 3E ASCIIフレーム ヘッダー解析成功
    ///
    /// 目的: ヘッダー情報が正しく抽出されることを確認
    /// </summary>
    [Fact]
    public void TC037_3E_ASCII_ヘッダー解析成功()
    {
        // Arrange
        string asciiFrame = "D000FF03FF0000040000C800";
        byte[] rawData = Encoding.ASCII.GetBytes(asciiFrame);

        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007,
                IsBinary = false,
                FrameVersion = FrameVersion.Frame3E
            },
            new TimeoutConfig()
        );

        // Act: Parse3EFrameAscii 呼び出し
        var method = typeof(PlcCommunicationManager).GetMethod(
            "Parse3EFrameAscii",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        var result = method?.Invoke(manager, new object[] { rawData });

        // Assert: ヘッダー情報の検証
        Assert.NotNull(result);

        var resultType = result.GetType();
        var endCode = (ushort)resultType.GetField("Item1")?.GetValue(result)!;
        var deviceDataAscii = (string)resultType.GetField("Item2")?.GetValue(result)!;

        // 終了コードが正常
        Assert.Equal((ushort)0x0000, endCode);

        // デバイスデータ開始位置が正しい（文字20-）
        Assert.NotEmpty(deviceDataAscii);
        Assert.Equal(4, deviceDataAscii.Length); // "C800" = 4文字
    }

    /// <summary>
    /// TC037_3E_ASCII: 不完全フレームエラー
    ///
    /// 目的: フレーム長が不足している場合にエラーが発生することを確認
    /// </summary>
    [Fact]
    public void TC037_3E_ASCII_不完全フレームエラー()
    {
        // Arrange: 不完全なフレーム（ヘッダーのみ、データ部なし）
        string incompleteFrame = "D00000FF03FF000004"; // 18文字（20文字未満）
        byte[] rawData = Encoding.ASCII.GetBytes(incompleteFrame);

        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007,
                IsBinary = false,
                FrameVersion = FrameVersion.Frame3E
            },
            new TimeoutConfig()
        );

        // Act & Assert: 例外が発生することを確認
        var method = typeof(PlcCommunicationManager).GetMethod(
            "Parse3EFrameAscii",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
            method?.Invoke(manager, new object[] { rawData })
        );

        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    /// <summary>
    /// TC037_3E_ASCII: エラー終了コード処理
    ///
    /// 目的: エラー終了コード（0xC059など）が正しく抽出されることを確認
    /// </summary>
    [Fact]
    public void TC037_3E_ASCII_エラー終了コード処理()
    {
        // Arrange: エラー終了コードを含むフレーム
        string errorFrame = "D000FF03FF000002C059"; // データ長=0x0002、終了コード=0xC059
        byte[] rawData = Encoding.ASCII.GetBytes(errorFrame);

        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007,
                IsBinary = false,
                FrameVersion = FrameVersion.Frame3E
            },
            new TimeoutConfig()
        );

        // Act
        var method = typeof(PlcCommunicationManager).GetMethod(
            "Parse3EFrameAscii",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        var result = method?.Invoke(manager, new object[] { rawData });

        // Assert: エラーコードが正しく抽出される
        Assert.NotNull(result);

        var resultType = result.GetType();
        var endCode = (ushort)resultType.GetField("Item1")?.GetValue(result)!;

        Assert.Equal((ushort)0xC059, endCode);
    }

    #endregion

    #region TC038_ASCII: 4E ASCIIフレーム解析テスト

    /// <summary>
    /// TC038_4E_ASCII: 4E ASCIIフレーム 基本解析成功
    ///
    /// 入力フレーム: "D40000000000FFFF03000062000000FFFF..."
    /// フレーム構造:
    ///   [0-1]   "D4"              サブヘッダ（4E ASCII応答）
    ///   [2-3]   "00"              予約1
    ///   [4-7]   "0000"            シリアル
    ///   [8-11]  "0000"            予約2
    ///   [12-13] "00"              ネットワーク番号
    ///   [14-15] "FF"              PC番号
    ///   [16-19] "03FF"            I/O番号
    ///   [20-21] "00"              局番
    ///   [22-25] "0062"            データ長（98バイト）
    ///   [26-29] "0000"            終了コード
    ///   [30-]   デバイスデータ（192文字 = 96バイト × 2）
    ///
    /// 期待出力:
    ///   - EndCode: 0x0000
    ///   - DeviceDataAscii: 192文字のデータ
    /// </summary>
    [Fact]
    public void TC038_4E_ASCII_基本解析成功()
    {
        // Arrange: 4E ASCIIフレーム作成
        // ヘッダー（30文字）
        // [0-1]D4 [2-3]00 [4-7]0000 [8-11]0000 [12-13]00 [14-15]FF [16-19]03FF [20-21]00 [22-25]0062 [26-29]0000
        string header = "D4000000000000FF03FF0000620000";

        // デバイスデータ（簡略化: 96バイト = 192文字）
        string deviceData = new string('F', 192); // 0xFF × 96バイト相当

        string asciiFrame = header + deviceData;
        byte[] rawData = Encoding.ASCII.GetBytes(asciiFrame);

        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007,
                IsBinary = false,
                FrameVersion = FrameVersion.Frame4E
            },
            new TimeoutConfig()
        );

        // Act: Parse4EFrameAscii 呼び出し
        var method = typeof(PlcCommunicationManager).GetMethod(
            "Parse4EFrameAscii",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        var result = method?.Invoke(manager, new object[] { rawData });

        // Assert
        Assert.NotNull(result);

        var resultType = result.GetType();
        var endCode = (ushort)resultType.GetField("Item1")?.GetValue(result)!;
        var deviceDataAscii = (string)resultType.GetField("Item2")?.GetValue(result)!;

        Assert.Equal((ushort)0x0000, endCode);
        Assert.Equal(192, deviceDataAscii.Length); // 96バイト × 2文字
    }

    /// <summary>
    /// TC038_4E_ASCII: 4E ASCIIフレーム ヘッダーサイズ検証
    ///
    /// 目的: 4Eフレームのヘッダーが30文字であることを確認
    /// </summary>
    [Fact]
    public void TC038_4E_ASCII_ヘッダーサイズ検証()
    {
        // Arrange
        string header = "D4000000000000FF03FF0000040000";
        string deviceData = "C800"; // 最小データ（2バイト = 4文字）
        string asciiFrame = header + deviceData;
        byte[] rawData = Encoding.ASCII.GetBytes(asciiFrame);

        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007,
                IsBinary = false,
                FrameVersion = FrameVersion.Frame4E
            },
            new TimeoutConfig()
        );

        // Act
        var method = typeof(PlcCommunicationManager).GetMethod(
            "Parse4EFrameAscii",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        var result = method?.Invoke(manager, new object[] { rawData });

        // Assert: デバイスデータ開始位置が30文字目（インデックス30）
        Assert.NotNull(result);

        var resultType = result.GetType();
        var deviceDataAscii = (string)resultType.GetField("Item2")?.GetValue(result)!;

        Assert.Equal("C800", deviceDataAscii);
    }

    /// <summary>
    /// TC038_4E_ASCII: 不完全フレームエラー
    ///
    /// 目的: フレーム長が30文字未満の場合にエラーが発生することを確認
    /// </summary>
    [Fact]
    public void TC038_4E_ASCII_不完全フレームエラー()
    {
        // Arrange: 不完全なフレーム（ヘッダー途中まで）
        string incompleteFrame = "D4000000000000FF03FF0000"; // 26文字（30文字未満）
        byte[] rawData = Encoding.ASCII.GetBytes(incompleteFrame);

        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007,
                IsBinary = false,
                FrameVersion = FrameVersion.Frame4E
            },
            new TimeoutConfig()
        );

        // Act & Assert
        var method = typeof(PlcCommunicationManager).GetMethod(
            "Parse4EFrameAscii",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
            method?.Invoke(manager, new object[] { rawData })
        );

        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    /// <summary>
    /// TC038_4E_ASCII: データ長整合性検証
    ///
    /// 目的: データ長フィールドとデバイスデータの実長が整合していることを確認
    /// </summary>
    [Fact]
    public void TC038_4E_ASCII_データ長整合性検証()
    {
        // Arrange: データ長 = 0x0002（2バイト = 終了コード2バイトのみ、実データ0バイト）
        string header = "D4000000000000FF03FF0000020000"; // データ長 = 2
        string deviceData = ""; // データなし（データ長2 - 終了コード2 = 0）
        string asciiFrame = header + deviceData;
        byte[] rawData = Encoding.ASCII.GetBytes(asciiFrame);

        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007,
                IsBinary = false,
                FrameVersion = FrameVersion.Frame4E
            },
            new TimeoutConfig()
        );

        // Act
        var method = typeof(PlcCommunicationManager).GetMethod(
            "Parse4EFrameAscii",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        var result = method?.Invoke(manager, new object[] { rawData });

        // Assert: デバイスデータが空
        Assert.NotNull(result);

        var resultType = result.GetType();
        var deviceDataAscii = (string)resultType.GetField("Item2")?.GetValue(result)!;

        Assert.Empty(deviceDataAscii);
    }

    #endregion
}
