using Andon.Core.Constants;

namespace Andon.Core.Interfaces;

/// <summary>
/// エラーハンドリングインターフェース
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    /// 例外からエラーカテゴリを判定する
    /// </summary>
    /// <param name="ex">判定対象の例外</param>
    /// <returns>エラーカテゴリ</returns>
    ErrorCategory DetermineErrorCategory(Exception ex);

    /// <summary>
    /// エラーカテゴリに応じたリトライ可否を判定する
    /// </summary>
    /// <param name="category">エラーカテゴリ</param>
    /// <returns>リトライ可能な場合はtrue</returns>
    bool ShouldRetry(ErrorCategory category);

    /// <summary>
    /// エラーカテゴリに応じたリトライ回数を取得する
    /// </summary>
    /// <param name="category">エラーカテゴリ</param>
    /// <returns>最大リトライ回数</returns>
    int GetMaxRetryCount(ErrorCategory category);

    /// <summary>
    /// エラーカテゴリに応じたリトライ遅延時間（ミリ秒）を取得する
    /// </summary>
    /// <param name="category">エラーカテゴリ</param>
    /// <returns>リトライ遅延時間（ミリ秒）</returns>
    int GetRetryDelayMs(ErrorCategory category);
}
