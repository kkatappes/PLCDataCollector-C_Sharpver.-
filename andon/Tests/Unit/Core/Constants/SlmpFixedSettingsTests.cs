using Andon.Core.Constants;
using Xunit;

namespace Andon.Tests.Unit.Core.Constants;

/// <summary>
/// SlmpFixedSettingsクラスのテスト（Phase1）
/// TDD Red-Green-Refactorサイクルに従って実装
/// memo.md送信フレーム仕様に準拠
/// </summary>
public class SlmpFixedSettingsTests
{
    #region 固定値確認テスト

    [Fact]
    public void FrameVersion_Is4E()
    {
        // Assert
        Assert.Equal("4E", SlmpFixedSettings.FrameVersion);
    }

    [Fact]
    public void Protocol_IsUDP()
    {
        // Assert
        Assert.Equal("UDP", SlmpFixedSettings.Protocol);
    }

    [Fact]
    public void NetworkNumber_Is0x00()
    {
        // Assert
        Assert.Equal(0x00, SlmpFixedSettings.NetworkNumber);
    }

    [Fact]
    public void StationNumber_Is0xFF()
    {
        // Assert
        Assert.Equal(0xFF, SlmpFixedSettings.StationNumber);
    }

    [Fact]
    public void IoNumber_Is0x03FF()
    {
        // Assert
        Assert.Equal(0x03FF, SlmpFixedSettings.IoNumber);
    }

    [Fact]
    public void MultiDropStation_Is0x00()
    {
        // Assert
        Assert.Equal(0x00, SlmpFixedSettings.MultiDropStation);
    }

    [Fact]
    public void MonitorTimer_Is0x0020()
    {
        // Assert: 0x0020 = 32 = 8秒
        Assert.Equal(0x0020, SlmpFixedSettings.MonitorTimer);
    }

    [Fact]
    public void ReceiveTimeoutMs_Is500()
    {
        // Assert
        Assert.Equal(500, SlmpFixedSettings.ReceiveTimeoutMs);
    }

    [Fact]
    public void Command_Is0x0403()
    {
        // Assert: ReadRandom
        Assert.Equal(0x0403, SlmpFixedSettings.Command);
    }

    [Fact]
    public void SubCommand_Is0x0000()
    {
        // Assert
        Assert.Equal(0x0000, SlmpFixedSettings.SubCommand);
    }

    [Fact]
    public void SubHeader_4E_IsCorrect()
    {
        // Assert
        Assert.Equal(new byte[] { 0x54, 0x00 }, SlmpFixedSettings.SubHeader_4E);
    }

    [Fact]
    public void Serial_IsZero()
    {
        // Assert
        Assert.Equal(new byte[] { 0x00, 0x00 }, SlmpFixedSettings.Serial);
    }

    [Fact]
    public void Reserved_IsZero()
    {
        // Assert
        Assert.Equal(new byte[] { 0x00, 0x00 }, SlmpFixedSettings.Reserved);
    }

    #endregion

    #region フレームヘッダ構築テスト

    [Fact]
    public void BuildFrameHeader_ReturnsCorrectLength()
    {
        // Arrange
        int dataLength = 72;

        // Act
        var header = SlmpFixedSettings.BuildFrameHeader(dataLength);

        // Assert: 4Eフレームヘッダは19バイト
        Assert.Equal(19, header.Length);
    }

    [Fact]
    public void BuildFrameHeader_SubHeaderIsCorrect()
    {
        // Arrange
        int dataLength = 72;

        // Act
        var header = SlmpFixedSettings.BuildFrameHeader(dataLength);

        // Assert: サブヘッダ (0-1)
        Assert.Equal(0x54, header[0]);
        Assert.Equal(0x00, header[1]);
    }

    [Fact]
    public void BuildFrameHeader_SerialAndReservedAreZero()
    {
        // Arrange
        int dataLength = 72;

        // Act
        var header = SlmpFixedSettings.BuildFrameHeader(dataLength);

        // Assert: シリアル (2-3)、予約 (4-5)
        Assert.Equal(0x00, header[2]);
        Assert.Equal(0x00, header[3]);
        Assert.Equal(0x00, header[4]);
        Assert.Equal(0x00, header[5]);
    }

    [Fact]
    public void BuildFrameHeader_NetworkAndStationAreCorrect()
    {
        // Arrange
        int dataLength = 72;

        // Act
        var header = SlmpFixedSettings.BuildFrameHeader(dataLength);

        // Assert: ネットワーク番号 (6)、局番 (7)
        Assert.Equal(0x00, header[6]);
        Assert.Equal(0xFF, header[7]);
    }

    [Fact]
    public void BuildFrameHeader_IoNumberIsLittleEndian()
    {
        // Arrange
        int dataLength = 72;

        // Act
        var header = SlmpFixedSettings.BuildFrameHeader(dataLength);

        // Assert: I/O番号 (8-9) = 0x03FF (リトルエンディアン)
        Assert.Equal(0xFF, header[8]);  // 下位バイト
        Assert.Equal(0x03, header[9]);  // 上位バイト
    }

    [Fact]
    public void BuildFrameHeader_MultiDropStationIsCorrect()
    {
        // Arrange
        int dataLength = 72;

        // Act
        var header = SlmpFixedSettings.BuildFrameHeader(dataLength);

        // Assert: マルチドロップ (10)
        Assert.Equal(0x00, header[10]);
    }

    [Fact]
    public void BuildFrameHeader_DataLengthIsLittleEndian()
    {
        // Arrange
        int dataLength = 72; // 0x0048

        // Act
        var header = SlmpFixedSettings.BuildFrameHeader(dataLength);

        // Assert: データ長 (11-12) リトルエンディアン
        Assert.Equal(0x48, header[11]); // 下位バイト
        Assert.Equal(0x00, header[12]); // 上位バイト
    }

    [Fact]
    public void BuildFrameHeader_MonitorTimerIsLittleEndian()
    {
        // Arrange
        int dataLength = 72;

        // Act
        var header = SlmpFixedSettings.BuildFrameHeader(dataLength);

        // Assert: 監視タイマ (13-14) = 0x0020 (リトルエンディアン)
        Assert.Equal(0x20, header[13]); // 下位バイト
        Assert.Equal(0x00, header[14]); // 上位バイト
    }

    [Fact]
    public void BuildFrameHeader_CommandIsLittleEndian()
    {
        // Arrange
        int dataLength = 72;

        // Act
        var header = SlmpFixedSettings.BuildFrameHeader(dataLength);

        // Assert: コマンド (15-16) = 0x0403 (リトルエンディアン)
        Assert.Equal(0x03, header[15]); // 下位バイト
        Assert.Equal(0x04, header[16]); // 上位バイト
    }

    [Fact]
    public void BuildFrameHeader_SubCommandIsZero()
    {
        // Arrange
        int dataLength = 72;

        // Act
        var header = SlmpFixedSettings.BuildFrameHeader(dataLength);

        // Assert: サブコマンド (17-18) = 0x0000
        Assert.Equal(0x00, header[17]);
        Assert.Equal(0x00, header[18]);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(72)]
    [InlineData(255)]
    [InlineData(1000)]
    public void BuildFrameHeader_VariousDataLengths_ReturnsCorrectDataLength(int dataLength)
    {
        // Act
        var header = SlmpFixedSettings.BuildFrameHeader(dataLength);

        // Assert: データ長フィールドが正しく設定されていること
        ushort actualDataLength = (ushort)(header[11] | (header[12] << 8));
        Assert.Equal((ushort)dataLength, actualDataLength);
    }

    [Fact]
    public void BuildFrameHeader_CompleteFrameStructure_MatchesMemoSpecification()
    {
        // Arrange: memo.mdの送信フレーム例（データ長=72バイト）
        int dataLength = 72;
        var expectedHeader = new byte[]
        {
            // サブヘッダ (0-1)
            0x54, 0x00,
            // シリアル (2-3)、予約 (4-5)
            0x00, 0x00, 0x00, 0x00,
            // ネットワーク番号 (6)
            0x00,
            // 局番 (7)
            0xFF,
            // I/O番号 (8-9)
            0xFF, 0x03,
            // マルチドロップ (10)
            0x00,
            // データ長 (11-12)
            0x48, 0x00,
            // 監視タイマ (13-14)
            0x20, 0x00,
            // コマンド (15-16)
            0x03, 0x04,
            // サブコマンド (17-18)
            0x00, 0x00
        };

        // Act
        var actualHeader = SlmpFixedSettings.BuildFrameHeader(dataLength);

        // Assert: memo.md送信フレームと完全一致
        Assert.Equal(expectedHeader, actualHeader);
    }

    #endregion
}
