using System;
using System.Threading.Tasks;

namespace SlmpClient.Core
{
    /// <summary>
    /// SLMP生データ記録インターフェース
    /// SOLID原則: Interface Segregation Principle適用
    /// SLMP生データ記録機能のみに特化
    /// </summary>
    public interface ISlmpRawDataRecorder : IAsyncDisposable, IDisposable
    {
        /// <summary>SLMPフレームを16進ダンプとして記録</summary>
        Task RecordSlmpFrameAsync(SlmpFrameData frameData);

        /// <summary>SLMPバッチ操作効率を記録</summary>
        Task RecordBatchOperationAsync(SlmpBatchOperationData batchData);

        /// <summary>SLMP通信統計を記録</summary>
        Task RecordCommunicationStatisticsAsync(string sessionId, SlmpCommunicationStatistics statistics);
    }

    /// <summary>
    /// SLMP通信統計データ
    /// </summary>
    public class SlmpCommunicationStatistics
    {
        /// <summary>総通信回数</summary>
        public int TotalCommunications { get; set; }

        /// <summary>成功回数</summary>
        public int SuccessfulCommunications { get; set; }

        /// <summary>失敗回数</summary>
        public int FailedCommunications { get; set; }

        /// <summary>平均応答時間</summary>
        public double AverageResponseTime { get; set; }

        /// <summary>最小応答時間</summary>
        public double MinResponseTime { get; set; }

        /// <summary>最大応答時間</summary>
        public double MaxResponseTime { get; set; }

        /// <summary>成功率</summary>
        public double SuccessRate { get; set; }

        /// <summary>記録期間</summary>
        public TimeSpan RecordingPeriod { get; set; }
    }
}