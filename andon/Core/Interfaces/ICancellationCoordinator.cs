namespace Andon.Core.Interfaces;

/// <summary>
/// キャンセレーション制御インターフェース
/// </summary>
/// <summary>
/// キャンセレーション制御インターフェース
/// </summary>
public interface ICancellationCoordinator
{
    /// <summary>
    /// 階層キャンセレーショントークン作成
    /// </summary>
    /// <param name="parentToken">親トークン</param>
    /// <param name="timeout">タイムアウト時間（オプション）</param>
    /// <returns>子トークンソース</returns>
    CancellationTokenSource CreateHierarchicalToken(
        CancellationToken parentToken,
        TimeSpan? timeout = null);

    /// <summary>
    /// キャンセル時コールバック登録
    /// </summary>
    /// <param name="token">対象トークン</param>
    /// <param name="callback">キャンセル時実行処理</param>
    /// <param name="callbackName">コールバック名称</param>
    /// <returns>登録ハンドル</returns>
    CancellationTokenRegistration RegisterCancellationCallback(
        CancellationToken token,
        Func<Task> callback,
        string callbackName);
}
