using System;

namespace SlmpClient.Core
{
    /// <summary>
    /// ログエントリファクトリー実装
    /// SOLID原則: Single Responsibility Principle適用
    /// ログエントリ作成処理のみに特化
    /// </summary>
    public class LogEntryFactory : ILogEntryFactory
    {
        /// <summary>セッション開始エントリを作成</summary>
        public object CreateSessionStartEntry(SessionStartInfo sessionInfo, ConfigurationDetails configDetails)
        {
            return new
            {
                EntryType = "SESSION_START",
                Timestamp = DateTime.Now,
                SessionId = sessionInfo.SessionId,
                SessionInfo = new
                {
                    ProcessId = sessionInfo.ProcessId,
                    ApplicationName = sessionInfo.ApplicationName,
                    Version = sessionInfo.Version,
                    Environment = sessionInfo.Environment
                },
                ConfigurationDetails = new
                {
                    ConfigFile = configDetails.ConfigFile,
                    ConnectionTarget = configDetails.ConnectionTarget,
                    SlmpSettings = configDetails.SlmpSettings,
                    ContinuityMode = configDetails.ContinuityMode,
                    RawDataLogging = configDetails.RawDataLogging,
                    LogOutputPath = configDetails.LogOutputPath
                }
            };
        }

        /// <summary>サイクル開始エントリを作成</summary>
        public object CreateCycleStartEntry(CycleStartInfo cycleInfo)
        {
            return new
            {
                EntryType = "CYCLE_START",
                Timestamp = DateTime.Now,
                SessionId = cycleInfo.SessionId,
                CycleNumber = cycleInfo.CycleNumber,
                CycleInfo = new
                {
                    StartMessage = cycleInfo.StartMessage,
                    IntervalFromPrevious = cycleInfo.IntervalFromPrevious
                }
            };
        }

        /// <summary>通信実行詳細エントリを作成</summary>
        public object CreateCommunicationEntry(CommunicationInfo communicationInfo, RawDataAnalysis rawDataAnalysis)
        {
            return new
            {
                EntryType = "CYCLE_COMMUNICATION",
                Timestamp = DateTime.Now,
                SessionId = communicationInfo.SessionId,
                CycleNumber = communicationInfo.CycleNumber,
                PhaseInfo = new
                {
                    Phase = communicationInfo.PhaseInfo.Phase,
                    StatusMessage = communicationInfo.PhaseInfo.StatusMessage,
                    DeviceAddress = communicationInfo.PhaseInfo.DeviceAddress
                },
                CommunicationDetails = new
                {
                    OperationType = communicationInfo.CommunicationDetails.OperationType,
                    DeviceCode = communicationInfo.CommunicationDetails.DeviceCode,
                    DeviceNumber = communicationInfo.CommunicationDetails.DeviceNumber,
                    DeviceAddress = communicationInfo.CommunicationDetails.DeviceAddress,
                    Values = communicationInfo.CommunicationDetails.Values,
                    ResponseTimeMs = communicationInfo.CommunicationDetails.ResponseTimeMs,
                    Success = communicationInfo.CommunicationDetails.Success,
                    DeviceValues = communicationInfo.CommunicationDetails.DeviceValues,
                    BatchReadEfficiency = communicationInfo.CommunicationDetails.BatchReadEfficiency
                },
                RawDataAnalysis = new
                {
                    RequestFrameHex = rawDataAnalysis.RequestFrameHex,
                    ResponseFrameHex = rawDataAnalysis.ResponseFrameHex,
                    HexDump = rawDataAnalysis.HexDump,
                    FrameAnalysis = new
                    {
                        SubHeader = rawDataAnalysis.FrameAnalysis.SubHeader,
                        SubHeaderDescription = rawDataAnalysis.FrameAnalysis.SubHeaderDescription,
                        EndCode = rawDataAnalysis.FrameAnalysis.EndCode,
                        EndCodeDescription = rawDataAnalysis.FrameAnalysis.EndCodeDescription
                    }
                }
            };
        }

        /// <summary>エラー発生エントリを作成</summary>
        public object CreateErrorEntry(ErrorInfo errorInfo, RecoveryInfo recoveryInfo)
        {
            return new
            {
                EntryType = "ERROR_OCCURRED",
                Timestamp = DateTime.Now,
                SessionId = errorInfo.SessionId,
                CycleNumber = errorInfo.CycleNumber,
                ErrorDetails = new
                {
                    ErrorType = errorInfo.ErrorType,
                    ErrorMessage = errorInfo.ErrorMessage,
                    DeviceAddress = errorInfo.DeviceAddress,
                    OperationType = errorInfo.OperationType,
                    AttemptCount = errorInfo.AttemptCount,
                    ResponseTimeMs = errorInfo.ResponseTimeMs,
                    ContinuityAction = errorInfo.ContinuityAction,
                    EstimatedCause = errorInfo.EstimatedCause
                },
                RecoveryInfo = new
                {
                    AutoRecoveryEnabled = recoveryInfo.AutoRecoveryEnabled,
                    RecoveryStatus = recoveryInfo.RecoveryStatus,
                    DefaultValueReturned = recoveryInfo.DefaultValueReturned
                }
            };
        }

        /// <summary>統計情報エントリを作成</summary>
        public object CreateStatisticsEntry(StatisticsInfo statisticsInfo)
        {
            return new
            {
                EntryType = "STATISTICS",
                Timestamp = DateTime.Now,
                SessionId = statisticsInfo.SessionId,
                StatisticsType = statisticsInfo.StatisticsType,
                StatisticsInfo = new
                {
                    ExecutedCycles = statisticsInfo.ExecutedCycles,
                    TotalCommunications = statisticsInfo.TotalCommunications,
                    SuccessfulCommunications = statisticsInfo.SuccessfulCommunications,
                    FailedCommunications = statisticsInfo.FailedCommunications,
                    SuccessRate = statisticsInfo.SuccessRate,
                    AverageResponseTime = statisticsInfo.AverageResponseTime,
                    MinResponseTime = statisticsInfo.MinResponseTime,
                    MaxResponseTime = statisticsInfo.MaxResponseTime
                }
            };
        }

        /// <summary>パフォーマンスメトリクスエントリを作成</summary>
        public object CreatePerformanceMetricsEntry(PerformanceMetricsInfo metricsInfo)
        {
            return new
            {
                EntryType = "PERFORMANCE_METRICS",
                Timestamp = DateTime.Now,
                SessionId = metricsInfo.SessionId,
                PerformanceData = new
                {
                    NetworkQuality = new
                    {
                        AverageLatency = metricsInfo.NetworkQuality.AverageLatency,
                        PacketLoss = metricsInfo.NetworkQuality.PacketLoss,
                        ConnectionStability = metricsInfo.NetworkQuality.ConnectionStability
                    },
                    SlmpPerformance = new
                    {
                        AverageResponseTime = metricsInfo.SlmpPerformance.AverageResponseTime,
                        MaxResponseTime = metricsInfo.SlmpPerformance.MaxResponseTime,
                        MinResponseTime = metricsInfo.SlmpPerformance.MinResponseTime,
                        SuccessRate = metricsInfo.SlmpPerformance.SuccessRate,
                        TotalOperations = metricsInfo.SlmpPerformance.TotalOperations
                    },
                    SystemResource = new
                    {
                        CpuUsage = metricsInfo.SystemResource.CpuUsage,
                        MemoryUsage = metricsInfo.SystemResource.MemoryUsage,
                        ThreadCount = metricsInfo.SystemResource.ThreadCount
                    }
                }
            };
        }

        /// <summary>セッション終了エントリを作成</summary>
        public object CreateSessionEndEntry(SessionSummary sessionSummary)
        {
            return new
            {
                EntryType = "SESSION_END",
                Timestamp = DateTime.Now,
                SessionId = sessionSummary.SessionId,
                SessionSummary = new
                {
                    Duration = sessionSummary.Duration,
                    FinalStatus = sessionSummary.FinalStatus,
                    ExitReason = sessionSummary.ExitReason,
                    TotalLogEntries = sessionSummary.TotalLogEntries,
                    FinalMessage = sessionSummary.FinalMessage
                }
            };
        }

        /// <summary>システムイベントエントリを作成</summary>
        public object CreateSystemEventEntry(string sessionId, string eventType, string message, object? additionalData = null)
        {
            return new
            {
                EntryType = "SYSTEM_EVENT",
                Timestamp = DateTime.Now,
                SessionId = sessionId,
                EventType = eventType,
                Message = message,
                AdditionalData = additionalData
            };
        }

        /// <summary>エラー発生エントリを作成（簡略版）</summary>
        public object CreateErrorEntry(string sessionId, string errorType, string errorMessage, string deviceAddress)
        {
            return new
            {
                EntryType = "ERROR_OCCURRED",
                Timestamp = DateTime.Now,
                SessionId = sessionId,
                ErrorType = errorType,
                ErrorMessage = errorMessage,
                DeviceAddress = deviceAddress
            };
        }
    }
}