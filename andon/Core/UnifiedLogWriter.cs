using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SlmpClient.Core
{
    /// <summary>
    /// 統合ログライター - 全てのログエントリを単一ファイルに構造化出力
    /// TDD Red-Green-Refactor サイクルで実装
    /// SOLID原則適用: 依存性注入とファサードパターンで責任分離
    /// </summary>
    public class UnifiedLogWriter : IUnifiedLogWriter
    {
        private readonly ILogger<UnifiedLogWriter> _logger;
        private readonly ILogEntryFactory _logEntryFactory;
        private readonly ILogQueueProcessor _queueProcessor;

        /// <summary>
        /// コンストラクタ（SOLID原則: 依存性注入適用）
        /// </summary>
        public UnifiedLogWriter(
            ILogger<UnifiedLogWriter> logger,
            ILogEntryFactory logEntryFactory,
            ILogQueueProcessor queueProcessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logEntryFactory = logEntryFactory ?? throw new ArgumentNullException(nameof(logEntryFactory));
            _queueProcessor = queueProcessor ?? throw new ArgumentNullException(nameof(queueProcessor));

            // キュープロセッサーを開始
            _queueProcessor.StartProcessing();
        }

        /// <summary>
        /// 後方互換性のためのコンストラクタ（従来の使用法をサポート）
        /// </summary>
        public UnifiedLogWriter(ILogger<UnifiedLogWriter> logger, string logFilePath)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrEmpty(logFilePath))
                throw new ArgumentException("Log file path cannot be null or empty", nameof(logFilePath));

            // デフォルト実装を作成（後方互換性のため）
            _logEntryFactory = new LogEntryFactory();
            var fileWriterLogger = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Critical)).CreateLogger<LogFileWriter>();
            var queueProcessorLogger = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Critical)).CreateLogger<LogQueueProcessor>();
            var fileWriter = new LogFileWriter(fileWriterLogger, logFilePath);
            _queueProcessor = new LogQueueProcessor(queueProcessorLogger, fileWriter);

            // キュープロセッサーを開始
            _queueProcessor.StartProcessing();
        }

        /// <summary>
        /// セッション開始エントリを出力
        /// SOLID原則: 依存性注入されたファクトリーを使用
        /// </summary>
        public async Task WriteSessionStartAsync(SessionStartInfo sessionInfo, ConfigurationDetails configDetails)
        {
            var entry = _logEntryFactory.CreateSessionStartEntry(sessionInfo, configDetails);
            await _queueProcessor.EnqueueLogEntryAsync(entry);
        }

        /// <summary>
        /// サイクル開始エントリを出力
        /// SOLID原則: 依存性注入されたファクトリーを使用
        /// </summary>
        public async Task WriteCycleStartAsync(CycleStartInfo cycleInfo)
        {
            var entry = _logEntryFactory.CreateCycleStartEntry(cycleInfo);
            await _queueProcessor.EnqueueLogEntryAsync(entry);
        }

        /// <summary>
        /// 通信実行詳細エントリを出力
        /// SOLID原則: 依存性注入されたファクトリーを使用
        /// </summary>
        public async Task WriteCommunicationAsync(CommunicationInfo communicationInfo, RawDataAnalysis rawDataAnalysis)
        {
            var entry = _logEntryFactory.CreateCommunicationEntry(communicationInfo, rawDataAnalysis);
            await _queueProcessor.EnqueueLogEntryAsync(entry);
        }

        /// <summary>
        /// エラー発生エントリを出力
        /// SOLID原則: 依存性注入されたファクトリーを使用
        /// </summary>
        public async Task WriteErrorAsync(ErrorInfo errorInfo, RecoveryInfo recoveryInfo)
        {
            var entry = _logEntryFactory.CreateErrorEntry(errorInfo, recoveryInfo);
            await _queueProcessor.EnqueueLogEntryAsync(entry);
        }

        /// <summary>
        /// 統計情報エントリを出力
        /// SOLID原則: 依存性注入されたファクトリーを使用
        /// </summary>
        public async Task WriteStatisticsAsync(StatisticsInfo statisticsInfo)
        {
            var entry = _logEntryFactory.CreateStatisticsEntry(statisticsInfo);
            await _queueProcessor.EnqueueLogEntryAsync(entry);
        }

        /// <summary>
        /// パフォーマンスメトリクスエントリを出力
        /// SOLID原則: 依存性注入されたファクトリーを使用
        /// </summary>
        public async Task WritePerformanceMetricsAsync(PerformanceMetricsInfo metricsInfo)
        {
            var entry = _logEntryFactory.CreatePerformanceMetricsEntry(metricsInfo);
            await _queueProcessor.EnqueueLogEntryAsync(entry);
        }

        /// <summary>
        /// セッション終了エントリを出力
        /// SOLID原則: 依存性注入されたファクトリーを使用
        /// </summary>
        public async Task WriteSessionEndAsync(SessionSummary sessionSummary)
        {
            var entry = _logEntryFactory.CreateSessionEndEntry(sessionSummary);
            await _queueProcessor.EnqueueLogEntryAsync(entry);
        }

        /// <summary>
        /// システムイベントエントリを出力
        /// SOLID原則: 依存性注入されたファクトリーを使用
        /// </summary>
        public async Task WriteSystemEventAsync(string sessionId, string eventType, string message, object? additionalData = null)
        {
            var entry = _logEntryFactory.CreateSystemEventEntry(sessionId, eventType, message, additionalData);
            await _queueProcessor.EnqueueLogEntryAsync(entry);
        }

        /// <summary>
        /// エラー発生エントリを出力（4パラメータオーバーロード）
        /// SOLID原則: 依存性注入されたファクトリーを使用
        /// </summary>
        public async Task WriteErrorAsync(string sessionId, string errorType, string errorMessage, string deviceAddress)
        {
            var entry = _logEntryFactory.CreateErrorEntry(sessionId, errorType, errorMessage, deviceAddress);
            await _queueProcessor.EnqueueLogEntryAsync(entry);
        }

        /// <summary>
        /// リソースを非同期的に解放
        /// SOLID原則: 依存性注入されたコンポーネントの解放
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await _queueProcessor.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// リソースを同期的に解放
        /// SOLID原則: 依存性注入されたコンポーネントの解放
        /// </summary>
        public void Dispose()
        {
            _queueProcessor.Dispose();
            GC.SuppressFinalize(this);
        }

    }

    #region Data Models for UnifiedLogWriter

    public class SessionStartInfo
    {
        public string SessionId { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public string ApplicationName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
    }

    public class ConfigurationDetails
    {
        public string ConfigFile { get; set; } = string.Empty;
        public string ConnectionTarget { get; set; } = string.Empty;
        public string SlmpSettings { get; set; } = string.Empty;
        public string ContinuityMode { get; set; } = string.Empty;
        public string RawDataLogging { get; set; } = string.Empty;
        public string LogOutputPath { get; set; } = string.Empty;
    }

    public class CycleStartInfo
    {
        public string SessionId { get; set; } = string.Empty;
        public int CycleNumber { get; set; }
        public string StartMessage { get; set; } = string.Empty;
        public double IntervalFromPrevious { get; set; }
    }

    public class CommunicationInfo
    {
        public string SessionId { get; set; } = string.Empty;
        public int CycleNumber { get; set; }
        public PhaseInfo PhaseInfo { get; set; } = new();
        public CommunicationDetails CommunicationDetails { get; set; } = new();
    }

    public class PhaseInfo
    {
        public string Phase { get; set; } = string.Empty;
        public string StatusMessage { get; set; } = string.Empty;
        public string DeviceAddress { get; set; } = string.Empty;
    }

    public class CommunicationDetails
    {
        public string OperationType { get; set; } = string.Empty;
        public string DeviceCode { get; set; } = string.Empty;
        public uint DeviceNumber { get; set; }
        public string DeviceAddress { get; set; } = string.Empty;
        public object[] Values { get; set; } = Array.Empty<object>();
        public double ResponseTimeMs { get; set; }
        public bool Success { get; set; }

        /// <summary>
        /// 個別デバイス値情報（新機能）
        /// </summary>
        public DetailedDeviceValueInfo[] DeviceValues { get; set; } = Array.Empty<DetailedDeviceValueInfo>();

        /// <summary>
        /// バッチ読み取り効率情報
        /// </summary>
        public string BatchReadEfficiency { get; set; } = string.Empty;
    }

    public class RawDataAnalysis
    {
        public string RequestFrameHex { get; set; } = string.Empty;
        public string ResponseFrameHex { get; set; } = string.Empty;
        public string HexDump { get; set; } = string.Empty;

        // SlmpRawDataAnalyzer統合: 詳細解析機能追加
        public string RequestHexDump { get; set; } = string.Empty;  // 送信データのHexDump
        public string DetailedDataAnalysis { get; set; } = string.Empty;  // データ型別解析結果
        public string DetailedFrameAnalysis { get; set; } = string.Empty; // 詳細フレーム解析結果

        public FrameAnalysis FrameAnalysis { get; set; } = new();
    }

    public class FrameAnalysis
    {
        public string SubHeader { get; set; } = string.Empty;
        public string SubHeaderDescription { get; set; } = string.Empty;
        public string EndCode { get; set; } = string.Empty;
        public string EndCodeDescription { get; set; } = string.Empty;
    }

    public class ErrorInfo
    {
        public string SessionId { get; set; } = string.Empty;
        public int CycleNumber { get; set; }
        public string ErrorType { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string DeviceAddress { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public int AttemptCount { get; set; }
        public double ResponseTimeMs { get; set; }
        public string ContinuityAction { get; set; } = string.Empty;
        public string EstimatedCause { get; set; } = string.Empty;
    }

    public class RecoveryInfo
    {
        public bool AutoRecoveryEnabled { get; set; }
        public string RecoveryStatus { get; set; } = string.Empty;
        public object[] DefaultValueReturned { get; set; } = Array.Empty<object>();
    }

    public class StatisticsInfo
    {
        public string SessionId { get; set; } = string.Empty;
        public string StatisticsType { get; set; } = string.Empty;
        public int ExecutedCycles { get; set; }
        public int TotalCommunications { get; set; }
        public int SuccessfulCommunications { get; set; }
        public int FailedCommunications { get; set; }
        public string SuccessRate { get; set; } = string.Empty;
        public double AverageResponseTime { get; set; }
        public double MinResponseTime { get; set; }
        public double MaxResponseTime { get; set; }
    }

    public class SessionSummary
    {
        public string SessionId { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string FinalStatus { get; set; } = string.Empty;
        public string ExitReason { get; set; } = string.Empty;
        public int TotalLogEntries { get; set; }
        public string FinalMessage { get; set; } = string.Empty;
    }

    public class PerformanceMetricsInfo
    {
        public string SessionId { get; set; } = string.Empty;
        public NetworkQualityData NetworkQuality { get; set; } = new();
        public SlmpPerformanceData SlmpPerformance { get; set; } = new();
        public SystemResourceData SystemResource { get; set; } = new();
    }

    public class NetworkQualityData
    {
        public double AverageLatency { get; set; }
        public double PacketLoss { get; set; }
        public string ConnectionStability { get; set; } = string.Empty;
    }

    public class SlmpPerformanceData
    {
        public double AverageResponseTime { get; set; }
        public double MaxResponseTime { get; set; }
        public double MinResponseTime { get; set; }
        public double SuccessRate { get; set; }
        public int TotalOperations { get; set; }
    }

    public class SystemResourceData
    {
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public int ThreadCount { get; set; }
    }

    /// <summary>
    /// 詳細デバイス値情報（設計仕様：デバイス解釈情報）
    /// </summary>
    public class DetailedDeviceValueInfo
    {
        public string DeviceAddress { get; set; } = string.Empty;
        public object RawValue { get; set; } = 0;
        public string InterpretedValue { get; set; } = string.Empty;
        public string StatusJudgment { get; set; } = string.Empty;
        public string ChangeDetection { get; set; } = string.Empty;
    }

    #endregion
}