using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SlmpClient.Core
{
    /// <summary>
    /// セッション管理クラス - セッションID生成、期間追跡、統計情報管理
    /// TDD Red-Green-Refactor サイクルで実装
    /// </summary>
    public class SessionManager
    {
        private readonly ILogger<SessionManager> _logger;
        private string? _currentSessionId;
        private DateTime _sessionStartTime;
        private int _logEntryCount;
        private readonly int _processId;

        public SessionManager(ILogger<SessionManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _processId = Environment.ProcessId;
        }

        /// <summary>
        /// 新しいセッションIDを生成
        /// </summary>
        public string GenerateSessionId()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var milliseconds = DateTime.Now.Millisecond.ToString("D3");
            var random = new Random().Next(1000, 9999);
            _currentSessionId = $"session_{timestamp}_{milliseconds}_{random}";
            _sessionStartTime = DateTime.Now;
            _logEntryCount = 0;

            _logger.LogInformation("新しいセッションを開始しました: {SessionId}", _currentSessionId);
            return _currentSessionId;
        }

        /// <summary>
        /// 現在のセッションIDを取得
        /// </summary>
        public string? GetCurrentSessionId()
        {
            return _currentSessionId;
        }

        /// <summary>
        /// セッション開始時刻を取得
        /// </summary>
        public DateTime GetSessionStartTime()
        {
            if (string.IsNullOrEmpty(_currentSessionId))
            {
                throw new InvalidOperationException("セッションが開始されていません");
            }
            return _sessionStartTime;
        }

        /// <summary>
        /// セッション継続時間を取得
        /// </summary>
        public TimeSpan GetSessionDuration()
        {
            if (string.IsNullOrEmpty(_currentSessionId))
            {
                throw new InvalidOperationException("セッションが開始されていません");
            }
            return DateTime.Now - _sessionStartTime;
        }

        /// <summary>
        /// 完全なセッション情報を取得
        /// </summary>
        public SessionInfo GetSessionInfo()
        {
            if (string.IsNullOrEmpty(_currentSessionId))
            {
                throw new InvalidOperationException("セッションが開始されていません");
            }

            return new SessionInfo
            {
                SessionId = _currentSessionId,
                StartTime = _sessionStartTime,
                Duration = GetSessionDuration(),
                ProcessId = _processId,
                LogEntryCount = _logEntryCount
            };
        }

        /// <summary>
        /// 新しいセッションを開始
        /// </summary>
        public string StartNewSession()
        {
            if (!string.IsNullOrEmpty(_currentSessionId))
            {
                _logger.LogInformation("既存のセッション {OldSessionId} を終了して新しいセッションを開始します", _currentSessionId);
            }

            return GenerateSessionId();
        }

        /// <summary>
        /// 現在のセッションを終了
        /// </summary>
        public SessionSummary EndCurrentSession(string finalStatus, string finalMessage)
        {
            if (string.IsNullOrEmpty(_currentSessionId))
            {
                throw new InvalidOperationException("セッションが開始されていません");
            }

            var duration = GetSessionDuration();
            var formattedDuration = FormatDuration(duration);
            var summary = new SessionSummary
            {
                SessionId = _currentSessionId,
                Duration = formattedDuration,
                FinalStatus = finalStatus,
                ExitReason = finalMessage,
                TotalLogEntries = _logEntryCount
            };

            _logger.LogInformation("セッション {SessionId} を終了しました。継続時間: {Duration}, ログエントリ数: {LogEntryCount}",
                _currentSessionId, formattedDuration, _logEntryCount);

            // セッション状態をリセット
            _currentSessionId = null;
            _logEntryCount = 0;

            return summary;
        }

        /// <summary>
        /// ログエントリ数をインクリメント
        /// </summary>
        public void IncrementLogEntryCount()
        {
            _logEntryCount++;
        }

        /// <summary>
        /// 継続時間を人間が読みやすい形式にフォーマット
        /// </summary>
        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
            {
                return $"{(int)duration.TotalDays:D2}:{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}.{duration.Milliseconds:D3}";
            }
            else
            {
                return $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}.{duration.Milliseconds:D3}";
            }
        }
    }

    /// <summary>
    /// セッション情報
    /// </summary>
    public class SessionInfo
    {
        public string SessionId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int ProcessId { get; set; }
        public int LogEntryCount { get; set; }
    }

}