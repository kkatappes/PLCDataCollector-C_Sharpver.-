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
    /// UnifiedLogWriter TDD実装テスト
    /// t-wadaの手法に基づくテストファースト開発
    /// RED-GREEN-REFACTORサイクルで7種類エントリタイプを実装
    /// </summary>
    public class UnifiedLogWriterTDDTests : IDisposable
    {
        private readonly string _testLogPath;
        private readonly Mock<ILogger<UnifiedLogWriter>> _mockLogger;

        public UnifiedLogWriterTDDTests()
        {
            _testLogPath = Path.Combine(Path.GetTempPath(), $"test_unified_log_{Guid.NewGuid()}.log");
            _mockLogger = new Mock<ILogger<UnifiedLogWriter>>();
        }

        /// <summary>
        /// RED: SESSION_START エントリタイプのテスト（失敗させる）
        /// </summary>
        [Fact]
        public async Task WriteSessionStartAsync_ShouldWriteSessionStartEntry_WithRequiredFields()
        {
            // Arrange
            var writer = new UnifiedLogWriter(_mockLogger.Object, _testLogPath);
            var sessionInfo = new SessionStartInfo
            {
                SessionId = "test_session_001",
                ProcessId = 12345,
                ApplicationName = "Test Application",
                Version = "1.0.0",
                Environment = "Test"
            };
            var configDetails = new ConfigurationDetails
            {
                ConfigFile = "test_appsettings.json",
                ConnectionTarget = "192.168.1.100:5007",
                SlmpSettings = "4E Binary",
                ContinuityMode = "ReturnDefaultAndContinue",
                RawDataLogging = "Enabled",
                LogOutputPath = _testLogPath
            };

            // Act
            await writer.WriteSessionStartAsync(sessionInfo, configDetails);

            // GREEN: キューイングシステムによる非同期書き込み完了を待機
            await Task.Delay(100); // ファイル書き込み完了まで少し待機

            // Assert - ファイルが作成され、SESSION_STARTエントリが書き込まれていることを確認
            Assert.True(File.Exists(_testLogPath), "ログファイルが作成されていません");
            var logContent = await File.ReadAllTextAsync(_testLogPath);
            Assert.Contains("\"EntryType\": \"SESSION_START\"", logContent);
            Assert.Contains("\"SessionId\": \"test_session_001\"", logContent);
            Assert.Contains("\"ProcessId\": 12345", logContent);
            Assert.Contains("\"ApplicationName\": \"Test Application\"", logContent);
        }

        /// <summary>
        /// RED: CYCLE_START エントリタイプのテスト（失敗させる）
        /// </summary>
        [Fact]
        public async Task WriteCycleStartAsync_ShouldWriteCycleStartEntry_WithCycleInfo()
        {
            // Arrange
            var writer = new UnifiedLogWriter(_mockLogger.Object, _testLogPath);
            var cycleInfo = new CycleStartInfo
            {
                SessionId = "test_session_001",
                CycleNumber = 1,
                StartMessage = "PLC接続確立 - サイクル開始",
                IntervalFromPrevious = 5.2
            };

            // Act
            await writer.WriteCycleStartAsync(cycleInfo);

            // GREEN: キューイングシステムによる非同期書き込み完了を待機
            await Task.Delay(100); // ファイル書き込み完了まで少し待機

            // Assert
            Assert.True(File.Exists(_testLogPath));
            var logContent = await File.ReadAllTextAsync(_testLogPath);
            Assert.Contains("\"EntryType\": \"CYCLE_START\"", logContent);
            Assert.Contains("\"SessionId\": \"test_session_001\"", logContent);
            Assert.Contains("\"CycleNumber\": 1", logContent);
            Assert.Contains("\"StartMessage\": \"PLC接続確立 - サイクル開始\"", logContent);
        }

        /// <summary>
        /// RED: CYCLE_COMMUNICATION エントリタイプのテスト（生データ含む）
        /// </summary>
        [Fact]
        public async Task WriteCommunicationAsync_ShouldWriteCommunicationEntry_WithRawData()
        {
            // Arrange
            var writer = new UnifiedLogWriter(_mockLogger.Object, _testLogPath);
            var commInfo = new CommunicationInfo
            {
                SessionId = "test_session_001",
                CycleNumber = 1,
                PhaseInfo = new PhaseInfo
                {
                    Phase = "Step4_DeviceScan",
                    StatusMessage = "デバイススキャン実行中",
                    DeviceAddress = "D100"
                },
                CommunicationDetails = new CommunicationDetails
                {
                    OperationType = "DeviceScan",
                    DeviceCode = "D",
                    DeviceNumber = 100,
                    DeviceAddress = "D100",
                    ResponseTimeMs = 15.5,
                    Success = true,
                    Values = new object[] { 12345 }
                }
            };

            var rawDataAnalysis = new RawDataAnalysis
            {
                RequestFrameHex = "5000 00FF FF03 00",
                ResponseFrameHex = "D000 00FF FF03 00",
                HexDump = "Request: 50 00 00 FF FF 03 00\nResponse: D0 00 00 FF FF 03 00",
                RequestHexDump = "50 00 00 FF FF 03 00",
                DetailedDataAnalysis = "D100: 値=12345, データ型=Word",
                DetailedFrameAnalysis = "4Eフレーム, バイナリ通信"
            };

            // Act
            await writer.WriteCommunicationAsync(commInfo, rawDataAnalysis);

            // GREEN: キューイングシステムによる非同期書き込み完了を待機
            await Task.Delay(100); // ファイル書き込み完了まで少し待機

            // Assert - SLMP 16進ダンプ出力を確認
            Assert.True(File.Exists(_testLogPath));
            var logContent = await File.ReadAllTextAsync(_testLogPath);
            Assert.Contains("\"EntryType\": \"CYCLE_COMMUNICATION\"", logContent);
            Assert.Contains("\"RequestFrameHex\": \"5000 00FF FF03 00\"", logContent);
            Assert.Contains("\"ResponseFrameHex\": \"D000 00FF FF03 00\"", logContent);
            Assert.Contains("\"HexDump\"", logContent);
        }

        /// <summary>
        /// RED: ERROR_OCCURRED エントリタイプのテスト
        /// </summary>
        [Fact]
        public async Task WriteErrorAsync_ShouldWriteErrorEntry_WithErrorDetails()
        {
            // Arrange
            var writer = new UnifiedLogWriter(_mockLogger.Object, _testLogPath);
            var errorInfo = new ErrorInfo
            {
                SessionId = "test_session_001",
                CycleNumber = 1,
                ErrorType = "CommunicationError",
                ErrorMessage = "SLMP応答解析エラー: 無効な16進文字",
                DeviceAddress = "D100",
                OperationType = "DeviceRead",
                AttemptCount = 2,
                ResponseTimeMs = 15.5
            };

            var recoveryInfo = new RecoveryInfo
            {
                AutoRecoveryEnabled = true,
                RecoveryStatus = "Success",
                DefaultValueReturned = new object[] { 0 }
            };

            // Act
            await writer.WriteErrorAsync(errorInfo, recoveryInfo);

            // GREEN: キューイングシステムによる非同期書き込み完了を待機
            await Task.Delay(100); // ファイル書き込み完了まで少し待機

            // Assert
            Assert.True(File.Exists(_testLogPath));
            var logContent = await File.ReadAllTextAsync(_testLogPath);
            Assert.Contains("\"EntryType\": \"ERROR_OCCURRED\"", logContent);
            Assert.Contains("\"ErrorType\": \"CommunicationError\"", logContent);
            Assert.Contains("\"AutoRecoveryEnabled\": true", logContent);
            Assert.Contains("\"RecoveryStatus\": \"Success\"", logContent);
        }

        /// <summary>
        /// RED: STATISTICS エントリタイプのテスト
        /// </summary>
        [Fact]
        public async Task WriteStatisticsAsync_ShouldWriteStatisticsEntry_WithMetrics()
        {
            // Arrange
            var writer = new UnifiedLogWriter(_mockLogger.Object, _testLogPath);
            var statisticsInfo = new StatisticsInfo
            {
                SessionId = "test_session_001",
                StatisticsType = "CycleStatistics",
                ExecutedCycles = 15,
                TotalCommunications = 150,
                SuccessfulCommunications = 147,
                FailedCommunications = 3,
                SuccessRate = "98.0%",
                AverageResponseTime = 12.5,
                MinResponseTime = 8.2,
                MaxResponseTime = 25.1
            };

            // Act
            await writer.WriteStatisticsAsync(statisticsInfo);

            // GREEN: キューイングシステムによる非同期書き込み完了を待機
            await Task.Delay(100); // ファイル書き込み完了まで少し待機

            // Assert
            Assert.True(File.Exists(_testLogPath));
            var logContent = await File.ReadAllTextAsync(_testLogPath);
            Assert.Contains("\"EntryType\": \"STATISTICS\"", logContent);
            Assert.Contains("\"TotalCommunications\": 150", logContent);
            Assert.Contains("\"SuccessRate\": \"98.0%\"", logContent);
            Assert.Contains("\"AverageResponseTime\": 12.5", logContent);
        }

        /// <summary>
        /// RED: PERFORMANCE_METRICS エントリタイプのテスト
        /// </summary>
        [Fact]
        public async Task WritePerformanceMetricsAsync_ShouldWriteMetricsEntry_WithSystemMetrics()
        {
            // Arrange
            var writer = new UnifiedLogWriter(_mockLogger.Object, _testLogPath);
            var metricsInfo = new PerformanceMetricsInfo
            {
                SessionId = "test_session_001",
                NetworkQuality = new NetworkQualityData
                {
                    AverageLatency = 15.5,
                    PacketLoss = 0.1,
                    ConnectionStability = "Excellent"
                },
                SlmpPerformance = new SlmpPerformanceData
                {
                    AverageResponseTime = 12.5,
                    MaxResponseTime = 25.0,
                    MinResponseTime = 8.0,
                    SuccessRate = 98.5,
                    TotalOperations = 150
                },
                SystemResource = new SystemResourceData
                {
                    CpuUsage = 15.5,
                    MemoryUsage = 125.8,
                    ThreadCount = 12
                }
            };

            // Act
            await writer.WritePerformanceMetricsAsync(metricsInfo);

            // GREEN: キューイングシステムによる非同期書き込み完了を待機
            await Task.Delay(100); // ファイル書き込み完了まで少し待機

            // Assert
            Assert.True(File.Exists(_testLogPath));
            var logContent = await File.ReadAllTextAsync(_testLogPath);
            Assert.Contains("\"EntryType\": \"PERFORMANCE_METRICS\"", logContent);
            Assert.Contains("\"AverageLatency\": 15.5", logContent);
            Assert.Contains("\"CpuUsage\": 15.5", logContent);
            Assert.Contains("\"TotalOperations\": 150", logContent);
        }

        /// <summary>
        /// RED: SESSION_END エントリタイプのテスト
        /// </summary>
        [Fact]
        public async Task WriteSessionEndAsync_ShouldWriteSessionEndEntry_WithSummary()
        {
            // Arrange
            var writer = new UnifiedLogWriter(_mockLogger.Object, _testLogPath);
            var sessionSummary = new SessionSummary
            {
                SessionId = "test_session_001",
                Duration = "00:15:30",
                FinalStatus = "Success",
                ExitReason = "UserTermination",
                TotalLogEntries = 150,
                FinalMessage = "セッション正常終了"
            };

            // Act
            await writer.WriteSessionEndAsync(sessionSummary);

            // GREEN: キューイングシステムによる非同期書き込み完了を待機
            await Task.Delay(100); // ファイル書き込み完了まで少し待機

            // Assert
            Assert.True(File.Exists(_testLogPath));
            var logContent = await File.ReadAllTextAsync(_testLogPath);
            Assert.Contains("\"EntryType\": \"SESSION_END\"", logContent);
            Assert.Contains("\"Duration\": \"00:15:30\"", logContent);
            Assert.Contains("\"FinalStatus\": \"Success\"", logContent);
            Assert.Contains("\"ExitReason\": \"UserTermination\"", logContent);
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