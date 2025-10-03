using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using SlmpClient.Core;

namespace andon.Tests.Core
{
    /// <summary>
    /// UnifiedLogWriter のテストクラス
    /// TDD Red-Green-Refactor サイクルに基づく実装
    /// </summary>
    public class UnifiedLogWriterTests : IDisposable
    {
        private readonly Mock<ILogger<UnifiedLogWriter>> _mockLogger;
        private readonly string _testLogFilePath;

        public UnifiedLogWriterTests()
        {
            _mockLogger = new Mock<ILogger<UnifiedLogWriter>>();
            _testLogFilePath = Path.Combine(Path.GetTempPath(), $"test_unified_log_{Guid.NewGuid()}.log");
        }

        /// <summary>
        /// テスト用のクリーンアップ
        /// </summary>
        public void Dispose()
        {
            if (File.Exists(_testLogFilePath))
            {
                File.Delete(_testLogFilePath);
            }
        }

        [Fact]
        public async Task WriteSessionStartEntry_ShouldCreateValidJsonEntry()
        {
            // Arrange
            var logWriter = new UnifiedLogWriter(_mockLogger.Object, _testLogFilePath);
            var sessionInfo = new SessionStartInfo
            {
                SessionId = "session_test_001",
                ProcessId = 12345,
                ApplicationName = "Test Andon SLMP Client",
                Version = "1.0.0-test",
                Environment = "Test"
            };

            var configDetails = new ConfigurationDetails
            {
                ConfigFile = "test_appsettings.json",
                ConnectionTarget = "192.168.1.100:8192",
                SlmpSettings = "Port:8192, Binary, Version4E, UDP, RxTimeout:3000ms",
                ContinuityMode = "ReturnDefaultAndContinue",
                RawDataLogging = "有効",
                LogOutputPath = "logs/test_rawdata_analysis.log"
            };

            // Act
            await logWriter.WriteSessionStartAsync(sessionInfo, configDetails);

            // Assert
            Assert.True(File.Exists(_testLogFilePath));
            var logContent = await File.ReadAllTextAsync(_testLogFilePath);

            Assert.Contains("SESSION_START", logContent);
            Assert.Contains("session_test_001", logContent);
            Assert.Contains("Test Andon SLMP Client", logContent);
            Assert.Contains("192.168.1.100:8192", logContent);
        }

        [Fact]
        public async Task WriteCycleStartEntry_ShouldCreateValidJsonEntry()
        {
            // Arrange
            var logWriter = new UnifiedLogWriter(_mockLogger.Object, _testLogFilePath);
            var cycleInfo = new CycleStartInfo
            {
                SessionId = "session_test_001",
                CycleNumber = 1,
                StartMessage = "--- サイクル 1 ---",
                IntervalFromPrevious = 1000.0
            };

            // Act
            await logWriter.WriteCycleStartAsync(cycleInfo);

            // Assert
            Assert.True(File.Exists(_testLogFilePath));
            var logContent = await File.ReadAllTextAsync(_testLogFilePath);

            Assert.Contains("CYCLE_START", logContent);
            Assert.Contains("session_test_001", logContent);
            Assert.Contains("--- サイクル 1 ---", logContent);
        }

        [Fact]
        public async Task WriteCommunicationEntry_ShouldIncludeRawDataAnalysis()
        {
            // Arrange
            var logWriter = new UnifiedLogWriter(_mockLogger.Object, _testLogFilePath);

            var communicationInfo = new CommunicationInfo
            {
                SessionId = "session_test_001",
                CycleNumber = 1,
                PhaseInfo = new PhaseInfo
                {
                    Phase = "BitDeviceRead",
                    StatusMessage = "センサー状態読み取り中...",
                    DeviceAddress = "M100"
                },
                CommunicationDetails = new CommunicationDetails
                {
                    OperationType = "BitDeviceRead",
                    DeviceCode = "M",
                    DeviceNumber = 100,
                    DeviceAddress = "M100",
                    Values = new object[] { false, false, false, false, false, false, false, false },
                    ResponseTimeMs = 15.5,
                    Success = true
                }
            };

            var rawDataAnalysis = new RawDataAnalysis
            {
                RequestFrameHex = "5400000000FF03000C001400010400000000010001000064000000",
                ResponseFrameHex = "D4000000000300020000000000000000",
                HexDump = "00000000: D4 00 00 00 00 03 00 02  00 00 00 00 00 00 00 00 |................|",
                FrameAnalysis = new FrameAnalysis
                {
                    SubHeader = "0x00D4",
                    SubHeaderDescription = "4Eフレーム",
                    EndCode = "0x0000",
                    EndCodeDescription = "正常終了"
                }
            };

            // Act
            await logWriter.WriteCommunicationAsync(communicationInfo, rawDataAnalysis);

            // Assert
            Assert.True(File.Exists(_testLogFilePath));
            var logContent = await File.ReadAllTextAsync(_testLogFilePath);

            Assert.Contains("CYCLE_COMMUNICATION", logContent);
            Assert.Contains("RequestFrameHex", logContent);
            Assert.Contains("ResponseFrameHex", logContent);
            Assert.Contains("4Eフレーム", logContent);
        }

        [Fact]
        public async Task WriteErrorEntry_ShouldIncludeDetailedErrorInfo()
        {
            // Arrange
            var logWriter = new UnifiedLogWriter(_mockLogger.Object, _testLogFilePath);

            var errorInfo = new ErrorInfo
            {
                SessionId = "session_test_001",
                CycleNumber = 5,
                ErrorType = "CommunicationTimeout",
                ErrorMessage = "SocketException - 接続がタイムアウトしました",
                DeviceAddress = "M100",
                OperationType = "BitDeviceRead",
                AttemptCount = 3,
                ResponseTimeMs = 3000.0,
                ContinuityAction = "デフォルト値で継続中",
                EstimatedCause = "一時的なネットワーク遅延"
            };

            var recoveryInfo = new RecoveryInfo
            {
                AutoRecoveryEnabled = true,
                RecoveryStatus = "自動回復試行中...",
                DefaultValueReturned = new object[] { false, false, false, false, false, false, false, false }
            };

            // Act
            await logWriter.WriteErrorAsync(errorInfo, recoveryInfo);

            // Assert
            Assert.True(File.Exists(_testLogFilePath));
            var logContent = await File.ReadAllTextAsync(_testLogFilePath);

            Assert.Contains("ERROR_OCCURRED", logContent);
            Assert.Contains("CommunicationTimeout", logContent);
            Assert.Contains("自動回復試行中", logContent);
        }

        [Fact]
        public async Task WriteStatisticsEntry_ShouldIncludePerformanceMetrics()
        {
            // Arrange
            var logWriter = new UnifiedLogWriter(_mockLogger.Object, _testLogFilePath);

            var statisticsInfo = new StatisticsInfo
            {
                SessionId = "session_test_001",
                StatisticsType = "SESSION_SUMMARY",
                ExecutedCycles = 10,
                TotalCommunications = 20,
                SuccessfulCommunications = 18,
                FailedCommunications = 2,
                SuccessRate = "90%",
                AverageResponseTime = 25.5,
                MinResponseTime = 5.2,
                MaxResponseTime = 150.3
            };

            // Act
            await logWriter.WriteStatisticsAsync(statisticsInfo);

            // Assert
            Assert.True(File.Exists(_testLogFilePath));
            var logContent = await File.ReadAllTextAsync(_testLogFilePath);

            Assert.Contains("STATISTICS", logContent);
            Assert.Contains("SESSION_SUMMARY", logContent);
            Assert.Contains("90%", logContent);
        }

        [Fact]
        public async Task WriteSessionEndEntry_ShouldCreateValidJsonEntry()
        {
            // Arrange
            var logWriter = new UnifiedLogWriter(_mockLogger.Object, _testLogFilePath);

            var sessionSummary = new SessionSummary
            {
                SessionId = "session_test_001",
                Duration = "00:05:30.123",
                FinalStatus = "正常終了",
                ExitReason = "ユーザー停止要求 (Ctrl+C)",
                TotalLogEntries = 50,
                FinalMessage = "テストセッション終了"
            };

            // Act
            await logWriter.WriteSessionEndAsync(sessionSummary);

            // Assert
            Assert.True(File.Exists(_testLogFilePath));
            var logContent = await File.ReadAllTextAsync(_testLogFilePath);

            Assert.Contains("SESSION_END", logContent);
            Assert.Contains("正常終了", logContent);
            Assert.Contains("00:05:30.123", logContent);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new UnifiedLogWriter(null!, _testLogFilePath));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentException_WhenLogFilePathIsNullOrEmpty()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new UnifiedLogWriter(_mockLogger.Object, null!));

            Assert.Throws<ArgumentException>(() =>
                new UnifiedLogWriter(_mockLogger.Object, ""));
        }
    }

}