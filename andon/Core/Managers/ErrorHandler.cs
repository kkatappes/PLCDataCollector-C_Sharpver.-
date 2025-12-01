namespace Andon.Core.Managers;

/// <summary>
/// エラーハンドリング
/// </summary>
public class ErrorHandler : Andon.Core.Interfaces.IErrorHandler
{
    /// <summary>
    /// 例外からエラーカテゴリを判定する
    /// </summary>
    /// <param name="ex">判定対象の例外</param>
    /// <returns>エラーカテゴリ</returns>
    public Andon.Core.Constants.ErrorCategory DetermineErrorCategory(Exception ex)
    {
        return ex switch
        {
            TimeoutException => Andon.Core.Constants.ErrorCategory.Timeout,
            System.Net.Sockets.SocketException => Andon.Core.Constants.ErrorCategory.Connection,
            Andon.Core.Exceptions.MultiConfigLoadException => Andon.Core.Constants.ErrorCategory.Configuration,
            InvalidOperationException => Andon.Core.Constants.ErrorCategory.DataProcessing,
            ArgumentNullException => Andon.Core.Constants.ErrorCategory.Validation,
            ArgumentException => Andon.Core.Constants.ErrorCategory.Validation,
            System.IO.IOException => Andon.Core.Constants.ErrorCategory.Network,
            _ => Andon.Core.Constants.ErrorCategory.Unknown
        };
    }

    /// <summary>
    /// エラーカテゴリに応じたリトライ可否を判定する
    /// </summary>
    /// <param name="category">エラーカテゴリ</param>
    /// <returns>リトライ可能な場合はtrue</returns>
    public bool ShouldRetry(Andon.Core.Constants.ErrorCategory category)
    {
        return category switch
        {
            Andon.Core.Constants.ErrorCategory.Timeout => true,
            Andon.Core.Constants.ErrorCategory.Connection => true,
            Andon.Core.Constants.ErrorCategory.Network => true,
            Andon.Core.Constants.ErrorCategory.Configuration => false,
            Andon.Core.Constants.ErrorCategory.Validation => false,
            Andon.Core.Constants.ErrorCategory.DataProcessing => false,
            Andon.Core.Constants.ErrorCategory.Unknown => false,
            _ => false
        };
    }

    /// <summary>
    /// エラーカテゴリに応じたリトライ回数を取得する
    /// </summary>
    /// <param name="category">エラーカテゴリ</param>
    /// <returns>最大リトライ回数</returns>
    public int GetMaxRetryCount(Andon.Core.Constants.ErrorCategory category)
    {
        return category switch
        {
            Andon.Core.Constants.ErrorCategory.Timeout => 3,
            Andon.Core.Constants.ErrorCategory.Connection => 3,
            Andon.Core.Constants.ErrorCategory.Network => 2,
            _ => 0
        };
    }

    /// <summary>
    /// エラーカテゴリに応じたリトライ遅延時間（ミリ秒）を取得する
    /// </summary>
    /// <param name="category">エラーカテゴリ</param>
    /// <returns>リトライ遅延時間（ミリ秒）</returns>
    public int GetRetryDelayMs(Andon.Core.Constants.ErrorCategory category)
    {
        return category switch
        {
            Andon.Core.Constants.ErrorCategory.Timeout => 1000,
            Andon.Core.Constants.ErrorCategory.Connection => 2000,
            Andon.Core.Constants.ErrorCategory.Network => 1500,
            _ => 0
        };
    }
}
