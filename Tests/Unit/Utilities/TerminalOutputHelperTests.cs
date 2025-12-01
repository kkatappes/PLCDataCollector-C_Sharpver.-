using Xunit;
using Andon.Utilities;
using System;

namespace Andon.Tests.Unit.Utilities
{
    /// <summary>
    /// TerminalOutputHelperのテスト
    /// </summary>
    public class TerminalOutputHelperTests
    {
        [Fact]
        [Trait("Phase", "Part8")]
        public void Parse4EFrame_実機データ_正しく解析できること()
        {
            // Arrange - 実機データ: D4 00 04 00 00 00 00 FF FF 03 00 04 00 00 00 21 05
            byte[] testData = new byte[]
            {
                0xD4, 0x00, // サブヘッダ (4E Binary)
                0x04, 0x00, // シーケンス番号 = 4
                0x00, 0x00, // 予約
                0x00,       // ネットワーク番号
                0xFF,       // PC番号
                0xFF, 0x03, // I/O番号 (LE: 0x03FF)
                0x00,       // 局番
                0x04, 0x00, // データ長 = 4バイト
                0x00, 0x00, // 終了コード = 0x0000 (正常終了)
                0x21, 0x05  // デバイスデータ = 0x0521 (LE) = 1313
            };

            // Act
            var (endCode, deviceData) = TerminalOutputHelper.Parse4EFrame(testData);

            // Assert
            Assert.Equal(0x0000, endCode); // 終了コードは 0x0000 (正常終了)
            Assert.Equal(2, deviceData.Length); // デバイスデータは2バイト
            Assert.Equal(0x21, deviceData[0]); // デバイスデータ下位バイト
            Assert.Equal(0x05, deviceData[1]); // デバイスデータ上位バイト

            // デバイス値をリトルエンディアンで変換して確認
            ushort deviceValue = (ushort)(deviceData[0] | (deviceData[1] << 8));
            Assert.Equal(0x0521, deviceValue); // 1313 (decimal)
        }

        [Fact]
        [Trait("Phase", "Part8")]
        public void Parse4EFrame_最小長15バイト未満_例外スロー()
        {
            // Arrange - 14バイトのデータ
            byte[] testData = new byte[14];

            // Act & Assert
            var ex = Assert.Throws<FormatException>(() =>
                TerminalOutputHelper.Parse4EFrame(testData));

            Assert.Contains("最小15バイト必要", ex.Message);
        }

        [Fact]
        [Trait("Phase", "Part8")]
        public void Parse3EFrame_正常データ_正しく解析できること()
        {
            // Arrange - 3E Binary応答フレームの例
            byte[] testData = new byte[]
            {
                0xD0, 0x00, // サブヘッダ (3E Binary)
                0x00,       // ネットワーク番号
                0xFF,       // PC番号
                0xFF, 0x03, // I/O番号 (LE: 0x03FF)
                0x00,       // 局番
                0x04, 0x00, // データ長 = 4バイト
                0x00, 0x00, // 終了コード = 0x0000 (正常終了)
                0xAB, 0xCD  // デバイスデータ
            };

            // Act
            var (endCode, deviceData) = TerminalOutputHelper.Parse3EFrame(testData);

            // Assert
            Assert.Equal(0x0000, endCode);
            Assert.Equal(2, deviceData.Length);
            Assert.Equal(0xAB, deviceData[0]);
            Assert.Equal(0xCD, deviceData[1]);
        }

        [Fact]
        [Trait("Phase", "Part8")]
        public void DetermineFrameType_4EBinary_正しく判定できること()
        {
            // Arrange
            byte[] testData = new byte[] { 0xD4, 0x00 };

            // Act
            string frameType = TerminalOutputHelper.DetermineFrameType(testData);

            // Assert
            Assert.Equal("4Eフレーム (Binary)", frameType);
        }

        [Fact]
        [Trait("Phase", "Part8")]
        public void DetermineFrameType_3EBinary_正しく判定できること()
        {
            // Arrange
            byte[] testData = new byte[] { 0xD0, 0x00 };

            // Act
            string frameType = TerminalOutputHelper.DetermineFrameType(testData);

            // Assert
            Assert.Equal("3Eフレーム (Binary)", frameType);
        }
    }
}
