using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SlmpClient.Core;
using Xunit;

namespace andon.Tests.Core
{
    /// <summary>
    /// SLMP生データ記録システム TDD実装テスト
    /// t-wadaの手法に基づくテストファースト開発
    /// RED-GREEN-REFACTORサイクルでSLMP生データ記録機能を実装
    /// </summary>
    public class SlmpRawDataRecorderTDDTests : IDisposable
    {
        private readonly string _testLogPath;
        private readonly Mock<ILogger<SlmpRawDataRecorder>> _mockLogger;

        public SlmpRawDataRecorderTDDTests()
        {
            _testLogPath = Path.Combine(Path.GetTempPath(), $"test_slmp_rawdata_{Guid.NewGuid()}.log");
            _mockLogger = new Mock<ILogger<SlmpRawDataRecorder>>();
        }

        /// <summary>
        /// RED: SLMP 16進ダンプ記録機能のテスト（失敗させる）
        /// </summary>
        [Fact]
        public async Task RecordSlmpFrameAsync_ShouldRecordHexDump_WithFrameAnalysis()
        {
            // Arrange
            var recorder = new SlmpRawDataRecorder(_mockLogger.Object, _testLogPath);
            var frameData = new SlmpFrameData
            {
                RequestFrame = new byte[] { 0x50, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00 },
                ResponseFrame = new byte[] { 0xD0, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00 },
                DeviceAddress = "D100",
                OperationType = "DeviceRead",
                ResponseTimeMs = 15.5,
                Success = true
            };

            // Act
            await recorder.RecordSlmpFrameAsync(frameData);

            // GREEN: キューイングシステムによる非同期書き込み完了を待機
            await Task.Delay(100); // ファイル書き込み完了まで少し待機

            // Assert - SLMP 16進ダンプ出力を確認
            Assert.True(File.Exists(_testLogPath), "SLMP生データログファイルが作成されていません");
            var logContent = await File.ReadAllTextAsync(_testLogPath);
            Assert.Contains("\"RequestFrameHex\": \"50 00 00 FF FF 03 00\"", logContent);
            Assert.Contains("\"ResponseFrameHex\": \"D0 00 00 FF FF 03 00\"", logContent);
            Assert.Contains("\"DeviceAddress\": \"D100\"", logContent);
            Assert.Contains("\"ResponseTimeMs\": 15.5", logContent);
        }

        /// <summary>
        /// RED: SLMP詳細フレーム解析機能のテスト（失敗させる）
        /// </summary>
        [Fact]
        public async Task RecordSlmpFrameAsync_ShouldAnalyzeFrame_WithDetailedAnalysis()
        {
            // Arrange
            var recorder = new SlmpRawDataRecorder(_mockLogger.Object, _testLogPath);
            var frameData = new SlmpFrameData
            {
                RequestFrame = new byte[] { 0x50, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00, 0x0C, 0x00, 0x01, 0x04, 0x00, 0x00, 0x64, 0x00, 0x01 },
                ResponseFrame = new byte[] { 0xD0, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00, 0x02, 0x00, 0x00, 0x00, 0x39, 0x30 },
                DeviceAddress = "D100",
                OperationType = "DeviceRead",
                ResponseTimeMs = 12.8,
                Success = true,
                ReadValue = 12345
            };

            // Act
            await recorder.RecordSlmpFrameAsync(frameData);

            // GREEN: キューイングシステムによる非同期書き込み完了を待機
            await Task.Delay(100);

            // Assert - 詳細解析結果を確認
            Assert.True(File.Exists(_testLogPath));
            var logContent = await File.ReadAllTextAsync(_testLogPath);
            Assert.Contains("\"DetailedFrameAnalysis\"", logContent);
            Assert.Contains("\"SubHeader\"", logContent);
            Assert.Contains("\"EndCode\"", logContent);
            Assert.Contains("\"DataTypeAnalysis\"", logContent);
        }

        /// <summary>
        /// RED: バッチ読み取り効率記録機能のテスト（失敗させる）
        /// </summary>
        [Fact]
        public async Task RecordBatchOperationAsync_ShouldRecordBatchEfficiency_WithMultipleDevices()
        {
            // Arrange
            var recorder = new SlmpRawDataRecorder(_mockLogger.Object, _testLogPath);
            var batchData = new SlmpBatchOperationData
            {
                OperationType = "BatchRead",
                DeviceAddresses = new[] { "D100", "D101", "D102", "D103", "D104" },
                TotalResponseTimeMs = 25.2,
                IndividualResponseTimes = new[] { 5.1, 4.8, 5.3, 4.9, 5.1 },
                BatchEfficiency = "5 devices in 25.2ms vs 5 individual operations (estimated 75ms)",
                PerformanceBenefit = "66.4% time reduction",
                Success = true
            };

            // Act
            await recorder.RecordBatchOperationAsync(batchData);

            // GREEN: キューイングシステムによる非同期書き込み完了を待機
            await Task.Delay(100);

            // Assert - バッチ効率記録を確認
            Assert.True(File.Exists(_testLogPath));
            var logContent = await File.ReadAllTextAsync(_testLogPath);
            Assert.Contains("\"OperationType\": \"BatchRead\"", logContent);
            Assert.Contains("\"BatchEfficiency\"", logContent);
            Assert.Contains("\"PerformanceBenefit\": \"66.4% time reduction\"", logContent);
            Assert.Contains("\"TotalResponseTimeMs\": 25.2", logContent);
        }

        public void Dispose()
        {
            if (File.Exists(_testLogPath))
            {
                try
                {
                    File.Delete(_testLogPath);
                }
                catch
                {
                    // テスト後のクリーンアップ失敗は無視
                }
            }
        }
    }
}