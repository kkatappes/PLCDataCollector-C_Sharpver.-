namespace Andon.Core.Constants;

/// <summary>
/// エラーメッセージ統一管理
/// </summary>
public static class ErrorMessages
{
    // PLC接続関連
    public const string NotConnected = "PLC未接続状態です。先にConnectAsync()を実行してください。";
    public const string AlreadyConnected = "既にPLCに接続されています。";
    public const string ConnectionFailed = "PLCへの接続に失敗しました。";
    public const string ConnectionTimeout = "PLC接続がタイムアウトしました。";

    // フレーム送受信関連
    public const string SendTimeout = "フレーム送信がタイムアウトしました。";
    public const string ReceiveTimeout = "フレーム受信がタイムアウトしました。";
    public const string InvalidFrame = "不正なSLMPフレーム形式です。";
    public const string SendFailed = "フレーム送信に失敗しました。";
    public const string ReceiveFailed = "フレーム受信に失敗しました。";

    // ソケット関連
    public const string SocketError = "ソケットエラーが発生しました。";
    public const string SocketDisposed = "ソケットは既に破棄されています。";

    // TC025用 ReceiveResponseAsync 専用メッセージ
    public const string NotConnectedForReceive = "未接続状態です。受信前にConnectAsync()を実行してください";
    public const string ReceiveTimeoutWithMs = "受信タイムアウト（タイムアウト時間: {0}ms）";
    public const string ReceiveError = "受信エラー: {0}";

    // データ処理エラー（TC029用）
    public const string InvalidRawDataFormat = "受信データの形式が不正です。";
    public const string DataLengthMismatch = "データ長が期待値と一致しません。期待: {0}バイト、実際: {1}バイト";
    public const string DeviceNumberOutOfRange = "デバイス番号が範囲外です: {0}";
    public const string InvalidEndCode = "SLMP終了コードが正常終了以外です: {0}";

    // 前処理情報エラー
    public const string ProcessedDeviceRequestInfoNull = "前処理情報（ProcessedDeviceRequestInfo）がnullです。";
    public const string DeviceTypeInfoMissing = "デバイス型情報が不足しています: {0}";

    // 変換エラー
    public const string HexConversionFailed = "16進数変換に失敗しました: {0}";
    public const string UnsupportedDataType = "サポートされていないデータ型です: {0}";
}
