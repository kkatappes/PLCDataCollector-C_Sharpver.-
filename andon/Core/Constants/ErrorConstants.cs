namespace Andon.Core.Constants;

/// <summary>
/// エラーコード定数
/// </summary>
public static class ErrorConstants
{
    /// <summary>
    /// エラータイプ: 検証エラー（不正なIPアドレス、不正なポート等）
    /// </summary>
    public const string ValidationError = "Validation";

    /// <summary>
    /// エラータイプ: 接続タイムアウト
    /// </summary>
    public const string TimeoutError = "Timeout";

    /// <summary>
    /// エラータイプ: 接続拒否
    /// </summary>
    public const string RefusedError = "Refused";

    /// <summary>
    /// エラータイプ: ネットワークエラー
    /// </summary>
    public const string NetworkError = "Network";

    /// <summary>
    /// エラータイプ: 送信エラー
    /// </summary>
    public const string SendError = "SendError";

    /// <summary>
    /// エラータイプ: 受信エラー
    /// </summary>
    public const string ReceiveError = "ReceiveError";

    /// <summary>
    /// エラータイプ: 受信タイムアウト
    /// </summary>
    public const string ReceiveTimeoutError = "ReceiveTimeout";
}

/// <summary>
/// エラーカテゴリ列挙型
/// </summary>
public enum ErrorCategory
{
    /// <summary>
    /// 不明なエラー
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// タイムアウトエラー
    /// </summary>
    Timeout = 1,

    /// <summary>
    /// 接続エラー（SocketException等）
    /// </summary>
    Connection = 2,

    /// <summary>
    /// 設定エラー（設定ファイル読み込みエラー等）
    /// </summary>
    Configuration = 3,

    /// <summary>
    /// データ処理エラー
    /// </summary>
    DataProcessing = 4,

    /// <summary>
    /// ネットワークエラー
    /// </summary>
    Network = 5,

    /// <summary>
    /// 検証エラー
    /// </summary>
    Validation = 6
}
