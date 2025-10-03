using System;

namespace SlmpClient.Core
{
    /// <summary>
    /// パフォーマンス監視インターフェース
    /// SOLID原則: インターフェース分離原則適用
    /// </summary>
    public interface IPerformanceMonitor
    {
        /// <summary>現在の統計情報を取得</summary>
        PerformanceStatistics GetCurrentStatistics();

        /// <summary>パフォーマンスアラートをチェック</summary>
        void CheckPerformanceAlerts();
    }
}