using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using SlmpClient.Core;

namespace andon.Tests.Core
{
    /// <summary>
    /// SessionManager のテストクラス
    /// TDD Red-Green-Refactor サイクルに基づく実装
    /// </summary>
    public class SessionManagerTests
    {
        private readonly Mock<ILogger<SessionManager>> _mockLogger;

        public SessionManagerTests()
        {
            _mockLogger = new Mock<ILogger<SessionManager>>();
        }

        [Fact]
        public void GenerateSessionId_ShouldReturnUniqueId()
        {
            // Arrange
            var sessionManager = new SessionManager(_mockLogger.Object);

            // Act
            var sessionId1 = sessionManager.GenerateSessionId();
            var sessionId2 = sessionManager.GenerateSessionId();

            // Assert
            Assert.NotEmpty(sessionId1);
            Assert.NotEmpty(sessionId2);
            Assert.NotEqual(sessionId1, sessionId2);
            Assert.StartsWith("session_", sessionId1);
            Assert.StartsWith("session_", sessionId2);
        }

        [Fact]
        public void GetCurrentSessionId_ShouldReturnCurrentSession()
        {
            // Arrange
            var sessionManager = new SessionManager(_mockLogger.Object);

            // Act
            var sessionId = sessionManager.GenerateSessionId();
            var currentSessionId = sessionManager.GetCurrentSessionId();

            // Assert
            Assert.Equal(sessionId, currentSessionId);
        }

        [Fact]
        public void GetSessionStartTime_ShouldReturnSessionStartTime()
        {
            // Arrange
            var sessionManager = new SessionManager(_mockLogger.Object);
            var beforeStartTime = DateTime.Now;

            // Act
            sessionManager.GenerateSessionId();
            var sessionStartTime = sessionManager.GetSessionStartTime();
            var afterStartTime = DateTime.Now;

            // Assert
            Assert.True(sessionStartTime >= beforeStartTime);
            Assert.True(sessionStartTime <= afterStartTime);
        }

        [Fact]
        public void GetSessionDuration_ShouldReturnCorrectDuration()
        {
            // Arrange
            var sessionManager = new SessionManager(_mockLogger.Object);

            // Act
            sessionManager.GenerateSessionId();
            var beforeDuration = sessionManager.GetSessionDuration();

            // 少し待機
            System.Threading.Thread.Sleep(10);

            var afterDuration = sessionManager.GetSessionDuration();

            // Assert
            Assert.True(afterDuration > beforeDuration);
            Assert.True(afterDuration.TotalMilliseconds >= 10);
        }

        [Fact]
        public void GetSessionInfo_ShouldReturnCompleteSessionInfo()
        {
            // Arrange
            var sessionManager = new SessionManager(_mockLogger.Object);

            // Act
            var sessionId = sessionManager.GenerateSessionId();
            var sessionInfo = sessionManager.GetSessionInfo();

            // Assert
            Assert.Equal(sessionId, sessionInfo.SessionId);
            Assert.True(sessionInfo.StartTime <= DateTime.Now);
            Assert.True(sessionInfo.Duration >= TimeSpan.Zero);
            Assert.True(sessionInfo.ProcessId > 0);
        }

        [Fact]
        public void StartNewSession_ShouldCreateNewSession()
        {
            // Arrange
            var sessionManager = new SessionManager(_mockLogger.Object);
            var firstSessionId = sessionManager.GenerateSessionId();

            // Act
            var newSessionId = sessionManager.StartNewSession();

            // Assert
            Assert.NotEqual(firstSessionId, newSessionId);
            Assert.Equal(newSessionId, sessionManager.GetCurrentSessionId());
            Assert.StartsWith("session_", newSessionId);
        }

        [Fact]
        public void EndCurrentSession_ShouldReturnSessionSummary()
        {
            // Arrange
            var sessionManager = new SessionManager(_mockLogger.Object);
            var sessionId = sessionManager.GenerateSessionId();

            // 少し待機してセッション期間を作る
            System.Threading.Thread.Sleep(5);

            // Act
            var sessionSummary = sessionManager.EndCurrentSession("正常終了", "テスト完了");

            // Assert
            Assert.Equal(sessionId, sessionSummary.SessionId);
            Assert.Equal("正常終了", sessionSummary.FinalStatus);
            Assert.Equal("テスト完了", sessionSummary.ExitReason);
            Assert.Contains("00:00:00", sessionSummary.Duration);
            Assert.Equal(0, sessionSummary.TotalLogEntries);
        }

        [Fact]
        public void IncrementLogEntryCount_ShouldTrackLogEntries()
        {
            // Arrange
            var sessionManager = new SessionManager(_mockLogger.Object);
            sessionManager.GenerateSessionId();

            // Act
            sessionManager.IncrementLogEntryCount();
            sessionManager.IncrementLogEntryCount();
            sessionManager.IncrementLogEntryCount();

            var sessionInfo = sessionManager.GetSessionInfo();

            // Assert
            Assert.Equal(3, sessionInfo.LogEntryCount);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new SessionManager(null!));
        }

        [Fact]
        public void GetCurrentSessionId_ShouldReturnNull_WhenNoSessionStarted()
        {
            // Arrange
            var sessionManager = new SessionManager(_mockLogger.Object);

            // Act
            var currentSessionId = sessionManager.GetCurrentSessionId();

            // Assert
            Assert.Null(currentSessionId);
        }

        [Fact]
        public void GetSessionStartTime_ShouldThrowInvalidOperationException_WhenNoSessionStarted()
        {
            // Arrange
            var sessionManager = new SessionManager(_mockLogger.Object);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                sessionManager.GetSessionStartTime());
        }
    }
}