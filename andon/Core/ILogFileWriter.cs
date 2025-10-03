using System.Threading.Tasks;

namespace SlmpClient.Core
{
    /// <summary>
    /// ログファイルライターインターフェース
    /// SOLID原則: Single Responsibility Principle適用
    /// ファイル書き込み処理のみに特化
    /// </summary>
    public interface ILogFileWriter
    {
        /// <summary>ログエントリをファイルに書き込み</summary>
        Task WriteLogEntryAsync(object logEntry);

        /// <summary>ログファイルパスを取得</summary>
        string LogFilePath { get; }
    }
}