namespace SlmpClient.Core
{
    /// <summary>
    /// SLMPエラー統計インターフェース
    /// SOLID原則: インターフェース分離原則適用
    /// </summary>
    public interface ISlmpErrorStatistics
    {
        /// <summary>エラー率（パーセンテージ）</summary>
        double ErrorRate { get; }

        /// <summary>総エラー数</summary>
        int TotalErrors { get; }

        /// <summary>総継続動作数</summary>
        int TotalContinuedOperations { get; }
    }
}