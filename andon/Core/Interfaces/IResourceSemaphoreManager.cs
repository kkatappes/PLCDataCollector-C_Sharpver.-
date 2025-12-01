using Andon.Core.Models;

namespace Andon.Core.Interfaces;

/// <summary>
/// 共有リソース排他制御インターフェース
/// </summary>
public interface IResourceSemaphoreManager
{
    /// <summary>
    /// ログファイル書き込み用セマフォ（同時アクセス数：1）
    /// </summary>
    SemaphoreSlim LogFileSemaphore { get; }

    /// <summary>
    /// 設定ファイル読み込み用セマフォ（同時アクセス数：3）
    /// </summary>
    SemaphoreSlim ConfigFileSemaphore { get; }

    /// <summary>
    /// データ出力ファイル用セマフォ（同時アクセス数：2）
    /// </summary>
    SemaphoreSlim OutputFileSemaphore { get; }

    /// <summary>
    /// セマフォ制御付き実行
    /// </summary>
    /// <typeparam name="T">戻り値の型</typeparam>
    /// <param name="semaphore">対象セマフォ</param>
    /// <param name="operation">実行対象処理</param>
    /// <param name="cancellationToken">キャンセル制御</param>
    /// <param name="timeout">セマフォ取得タイムアウト（デフォルト30秒）</param>
    /// <returns>実行結果</returns>
    Task<T> ExecuteWithSemaphoreAsync<T>(
        SemaphoreSlim semaphore,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null);

    /// <summary>
    /// リソース種別セマフォ取得
    /// </summary>
    /// <param name="resourceType">リソース種別</param>
    /// <returns>対応するセマフォ</returns>
    SemaphoreSlim GetResourceSemaphore(ResourceType resourceType);
}
