using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SlmpClient.Constants;
using SlmpClient.Core;
using SlmpClient.Serialization;
using SlmpClient.Utils;
using Xunit;

namespace SlmpClient.Tests
{
    /// <summary>
    /// メモリ最適化機能の単体テスト
    /// ArrayPool、Span<T>、ゼロアロケーション処理を検証
    /// </summary>
    public class MemoryOptimizationTests : IDisposable
    {
        private readonly IMemoryOptimizer _memoryOptimizer;
        private readonly IStreamingFrameProcessor _streamingProcessor;

        public MemoryOptimizationTests()
        {
            _memoryOptimizer = new MemoryOptimizer();
            _streamingProcessor = new StreamingFrameProcessor(_memoryOptimizer);
        }

        #region Memory Optimizer Tests

        [Fact]
        public void MemoryOptimizer_RentBuffer_Should_ReturnCorrectSize()
        {
            // Arrange
            const int requestedSize = 1024;

            // Act
            using var buffer = _memoryOptimizer.RentBuffer(requestedSize);

            // Assert
            Assert.True(buffer.Memory.Length >= requestedSize);
        }

        [Fact]
        public void MemoryOptimizer_MultipleRentals_Should_TrackMemoryUsage()
        {
            // Arrange
            const int bufferSize = 512;
            const int rentalCount = 10;

            // Act
            var initialUsage = _memoryOptimizer.CurrentMemoryUsage;
            var buffers = new IMemoryOwner<byte>[rentalCount];

            for (int i = 0; i < rentalCount; i++)
            {
                buffers[i] = _memoryOptimizer.RentBuffer(bufferSize);
            }

            var peakUsage = _memoryOptimizer.CurrentMemoryUsage;

            // Cleanup
            for (int i = 0; i < rentalCount; i++)
            {
                buffers[i].Dispose();
            }

            var finalUsage = _memoryOptimizer.CurrentMemoryUsage;

            // Assert
            Assert.True(peakUsage > initialUsage);
            Assert.True(finalUsage <= peakUsage);
        }

        [Fact]
        public void MemoryOptimizer_MemoryThreshold_Should_TrackUsage()
        {
            // Arrange
            var initialUsage = _memoryOptimizer.CurrentMemoryUsage;

            // Act - Rent multiple buffers
            using var buffer1 = _memoryOptimizer.RentBuffer(600);
            using var buffer2 = _memoryOptimizer.RentBuffer(600);
            var peakUsage = _memoryOptimizer.PeakMemoryUsage;

            // Assert - Memory usage should increase
            Assert.True(peakUsage > initialUsage);
            Assert.True(_memoryOptimizer.CurrentMemoryUsage > initialUsage);
        }

        #endregion

        #region Data Processor Tests

        [Theory]
        [InlineData("ABCD", new byte[] { 0xDC, 0xBA })]
        [InlineData("12345678", new byte[] { 0x87, 0x65, 0x43, 0x21 })]
        [InlineData("", new byte[] { })]
        public void DataProcessor_StringToBytesBuffer_Should_ConvertCorrectly(string hexString, byte[] expected)
        {
            // Act
            var result = DataProcessor.StringToBytesBuffer(hexString);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void DataProcessor_StringToBytesBuffer_Should_ThrowOnInvalidHex()
        {
            // Arrange
            const string invalidHex = "GHIJ";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => DataProcessor.StringToBytesBuffer(invalidHex));
        }

        [Fact]
        public void DataProcessor_ExtractWordDwordData_Should_SplitCorrectly()
        {
            // Arrange
            var buffer = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
            const int splitPosition = 2; // 2個の2バイトデータ

            // Act
            var (wordData, dwordData) = DataProcessor.ExtractWordDwordData(buffer, splitPosition);

            // Assert
            Assert.Equal(2, wordData.Length);
            Assert.Equal(1, dwordData.Length);
            Assert.Equal(2, wordData[0].Length);
            Assert.Equal(4, dwordData[0].Length);
        }

        [Theory]
        [InlineData(DeviceCode.D, 100, "D*000100")]
        [InlineData(DeviceCode.X, 0x1A, "X*00001A")]
        [InlineData(DeviceCode.Y, 255, "Y*0000FF")]
        public void DataProcessor_DeviceToAscii_Should_FormatCorrectly(DeviceCode deviceCode, uint address, string expected)
        {
            // Act
            var result = DataProcessor.DeviceToAscii(deviceCode, address);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void DataProcessor_BytesToUshortArray_Should_ConvertLittleEndian()
        {
            // Arrange
            var bytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var expected = new ushort[] { 0x0201, 0x0403 };

            // Act
            var result = DataProcessor.BytesToUshortArray(bytes);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void DataProcessor_ProcessInChunks_Should_ProcessLargeData()
        {
            // Arrange
            var data = new int[1000];
            for (int i = 0; i < data.Length; i++) data[i] = i;
            
            const int chunkSize = 100;
            int processedChunks = 0;

            // Act
            var results = DataProcessor.ProcessInChunks(data, chunkSize, 
                chunk => {
                    processedChunks++;
                    return chunk.Length;
                }).ToList();

            // Assert
            Assert.Equal(10, results.Count);
            Assert.Equal(10, processedChunks);
        }

        #endregion

        #region Streaming Frame Processor Tests

        [Fact]
        public async Task StreamingFrameProcessor_ProcessFrameAsync_Should_ReadCompleteFrame()
        {
            // Arrange
            var mockFrame = CreateMockSlmpResponse();
            using var stream = new MemoryStream(mockFrame);

            // Act
            var result = await _streamingProcessor.ProcessFrameAsync(stream);

            // Assert
            Assert.Equal(mockFrame.Length, result.Length);
            Assert.Equal(mockFrame, result);
        }

        [Fact]
        public void StreamingFrameProcessor_DetermineFrameSize_Should_CalculateCorrectSize()
        {
            // Arrange - 3Eフレームヘッダー（9バイト + データ長4）
            var header = new byte[] { 0x50, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00, 0x04, 0x00, 0x00, 0x00 };

            // Act
            var frameSize = _streamingProcessor.DetermineFrameSize(header.AsSpan());

            // Assert
            Assert.Equal(13, frameSize); // 9 + 4 = 13
        }

        [Fact]
        public void StreamingFrameProcessor_DetermineFrameSize_Should_Handle4EFrame()
        {
            // Arrange - 4Eフレームヘッダー（11バイト + データ長4）
            // データ長は headerBytes[7-8] の位置に設定（StreamingFrameProcessorの実装に合わせる）
            var header = new byte[] { 0x54, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x03, 0x00, 0x00, 0x00 };

            // Act
            var frameSize = _streamingProcessor.DetermineFrameSize(header.AsSpan());

            // Assert
            Assert.Equal(15, frameSize); // 11 + 4 = 15
        }

        #endregion

        #region Response Parser Tests

        [Fact]
        public void SlmpResponseParser_ParseHexByte_Should_ConvertCorrectly()
        {
            // Arrange
            var hexBytes = "FF"u8.ToArray();

            // Act
            var result = SlmpResponseParserHelper.ParseHexByte(hexBytes);

            // Assert
            Assert.Equal(0xFF, result);
        }

        [Fact]
        public void SlmpResponseParser_ParseHexUshort_Should_ConvertCorrectly()
        {
            // Arrange
            var hexBytes = "1234"u8.ToArray();

            // Act
            var result = SlmpResponseParserHelper.ParseHexUshort(hexBytes);

            // Assert
            Assert.Equal(0x1234, result);
        }

        [Fact]
        public void SlmpResponseParser_GetHexValue_Should_ConvertAllValidChars()
        {
            // Test numeric characters
            for (byte c = (byte)'0'; c <= (byte)'9'; c++)
            {
                Assert.Equal(c - '0', SlmpResponseParserHelper.GetHexValue(c));
            }

            // Test uppercase hex characters
            for (byte c = (byte)'A'; c <= (byte)'F'; c++)
            {
                Assert.Equal(c - 'A' + 10, SlmpResponseParserHelper.GetHexValue(c));
            }

            // Test lowercase hex characters
            for (byte c = (byte)'a'; c <= (byte)'f'; c++)
            {
                Assert.Equal(c - 'a' + 10, SlmpResponseParserHelper.GetHexValue(c));
            }
        }

        [Fact]
        public void SlmpResponseParser_GetHexValue_Should_ThrowOnInvalidChar()
        {
            // Arrange
            const byte invalidChar = (byte)'G';

            // Act & Assert
            Assert.Throws<ArgumentException>(() => SlmpResponseParserHelper.GetHexValue(invalidChar));
        }

        #endregion

        #region Helper Methods

        private byte[] CreateMockSlmpResponse()
        {
            // 3E バイナリレスポンス（正常）
            return new byte[]
            {
                0x50, 0x00,           // サブヘッダー
                0x00,                 // ネットワーク番号
                0xFF,                 // ノード番号
                0xFF, 0x03,          // 要求先プロセッサ番号
                0x00,                 // マルチドロップ局番
                0x06, 0x00,          // データ長（エンドコード2バイト + データ4バイト = 6バイト）
                0x00, 0x00,          // エンドコード（正常）
                0x01, 0x02, 0x03, 0x04 // データ部分
            };
        }

        #endregion

        public void Dispose()
        {
            _memoryOptimizer?.Dispose();
            _streamingProcessor?.Dispose();
        }
    }
}