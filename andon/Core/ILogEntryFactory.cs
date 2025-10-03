using System;

namespace SlmpClient.Core
{
    /// <summary>
    /// ログエントリファクトリーインターフェース
    /// SOLID原則: Single Responsibility Principle適用
    /// ログエントリ作成のみに特化
    /// </summary>
    public interface ILogEntryFactory
    {
        /// <summary>セッション開始エントリを作成</summary>
        object CreateSessionStartEntry(SessionStartInfo sessionInfo, ConfigurationDetails configDetails);

        /// <summary>サイクル開始エントリを作成</summary>
        object CreateCycleStartEntry(CycleStartInfo cycleInfo);

        /// <summary>通信実行詳細エントリを作成</summary>
        object CreateCommunicationEntry(CommunicationInfo communicationInfo, RawDataAnalysis rawDataAnalysis);

        /// <summary>エラー発生エントリを作成</summary>
        object CreateErrorEntry(ErrorInfo errorInfo, RecoveryInfo recoveryInfo);

        /// <summary>統計情報エントリを作成</summary>
        object CreateStatisticsEntry(StatisticsInfo statisticsInfo);

        /// <summary>パフォーマンスメトリクスエントリを作成</summary>
        object CreatePerformanceMetricsEntry(PerformanceMetricsInfo metricsInfo);

        /// <summary>セッション終了エントリを作成</summary>
        object CreateSessionEndEntry(SessionSummary sessionSummary);

        /// <summary>システムイベントエントリを作成</summary>
        object CreateSystemEventEntry(string sessionId, string eventType, string message, object? additionalData = null);

        /// <summary>エラー発生エントリを作成（簡略版）</summary>
        object CreateErrorEntry(string sessionId, string errorType, string errorMessage, string deviceAddress);
    }
}