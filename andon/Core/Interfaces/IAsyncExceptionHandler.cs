using Andon.Core.Models;

namespace Andon.Core.Interfaces;

/// <summary>
/// 階層的例外ハンドリングインターフェース
/// </summary>
public interface IAsyncExceptionHandler
{
    /// <summary>
    /// 重要処理用例外ハンドリング
    /// </summary>
    /// <typeparam name="T">戻り値の型</typeparam>
    /// <param name="operation">実行対象の重要処理</param>
    /// <param name="operationName">処理名称（エラーログ識別用）</param>
    /// <param name="cancellationToken">キャンセル制御</param>
    /// <returns>実行結果オブジェクト</returns>
    Task<AsyncOperationResult<T>> HandleCriticalOperationAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 一般処理用一括例外ハンドリング
    /// </summary>
    /// <param name="operations">実行対象の一般処理群</param>
    /// <param name="groupName">処理グループ名称</param>
    /// <param name="cancellationToken">キャンセル制御</param>
    /// <returns>一括実行結果オブジェクト</returns>
    Task<GeneralOperationResult> HandleGeneralOperationsAsync(
        IEnumerable<Func<Task>> operations,
        string groupName,
        CancellationToken cancellationToken = default);
}
