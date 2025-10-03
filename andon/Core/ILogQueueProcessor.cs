using System;
using System.Threading.Tasks;

namespace SlmpClient.Core
{
    /// <summary>
    /// ログキュープロセッサーインターフェース
    /// SOLID原則: Single Responsibility Principle適用
    /// キューイング処理のみに特化
    /// </summary>
    public interface ILogQueueProcessor : IAsyncDisposable, IDisposable
    {
        /// <summary>ログエントリをキューに追加</summary>
        Task EnqueueLogEntryAsync(object logEntry);

        /// <summary>キュープロセッサーを開始</summary>
        void StartProcessing();

        /// <summary>キュープロセッサーを停止</summary>
        Task StopProcessingAsync();
    }
}