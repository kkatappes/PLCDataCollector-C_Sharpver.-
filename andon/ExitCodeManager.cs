namespace Andon;

/// <summary>
/// 終了コード管理・エラー分類
/// </summary>
public static class ExitCodeManager
{
    /// <summary>
    /// 正常終了
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// 設定エラー
    /// </summary>
    public const int ConfigurationError = 1;

    /// <summary>
    /// 接続エラー
    /// </summary>
    public const int ConnectionError = 2;

    /// <summary>
    /// タイムアウトエラー
    /// </summary>
    public const int TimeoutError = 3;

    /// <summary>
    /// データ処理エラー
    /// </summary>
    public const int DataProcessingError = 4;

    /// <summary>
    /// バリデーションエラー
    /// </summary>
    public const int ValidationError = 5;

    /// <summary>
    /// ネットワークエラー
    /// </summary>
    public const int NetworkError = 6;

    /// <summary>
    /// 不明なエラー
    /// </summary>
    public const int UnknownError = 99;

    /// <summary>
    /// 例外から終了コードを判定する
    /// </summary>
    /// <param name="ex">判定対象の例外</param>
    /// <returns>終了コード</returns>
    public static int FromException(Exception ex)
    {
        return ex switch
        {
            TimeoutException => TimeoutError,
            System.Net.Sockets.SocketException => ConnectionError,
            Core.Exceptions.MultiConfigLoadException => ConfigurationError,
            InvalidOperationException => DataProcessingError,
            ArgumentNullException => ValidationError,
            ArgumentException => ValidationError,
            System.IO.IOException => NetworkError,
            _ => UnknownError
        };
    }
}
