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

    // Phase 2-Refactor: 接続プロトコル関連エラーメッセージ生成（短い形式、エラー詳細記録用）
    /// <summary>
    /// 両プロトコル接続失敗時のエラーメッセージを生成（短い形式）
    /// </summary>
    /// <param name="tcpError">TCPエラー詳細</param>
    /// <param name="udpError">UDPエラー詳細</param>
    /// <returns>エラーメッセージ</returns>
    public static string BothProtocolsConnectionFailed(string tcpError, string udpError)
    {
        return $"TCP/UDP両プロトコルで接続失敗\n- TCP: {tcpError}\n- UDP: {udpError}";
    }

    /// <summary>
    /// 初期プロトコル失敗時のエラーメッセージを生成（短い形式）
    /// </summary>
    /// <param name="protocol">プロトコル名（"TCP"/"UDP"）</param>
    /// <param name="error">エラー詳細</param>
    /// <returns>エラーメッセージ</returns>
    public static string InitialProtocolFailed(string protocol, string error)
    {
        return $"初期プロトコル({protocol})失敗: {error}";
    }

    // Phase 3: ログ出力用メッセージ生成（詳細形式、IPアドレス/ポート番号含む）
    /// <summary>
    /// 接続試行開始ログメッセージを生成
    /// </summary>
    /// <param name="ipAddress">IPアドレス</param>
    /// <param name="port">ポート番号</param>
    /// <param name="protocol">プロトコル名（"TCP"/"UDP"）</param>
    /// <returns>ログメッセージ</returns>
    public static string ConnectionAttemptStarted(string ipAddress, int port, string protocol)
    {
        return $"PLC接続試行開始: {ipAddress}:{port}, プロトコル: {protocol}";
    }

    /// <summary>
    /// 初期プロトコル失敗・代替プロトコル再試行ログメッセージを生成
    /// </summary>
    /// <param name="failedProtocol">失敗したプロトコル名</param>
    /// <param name="error">エラー詳細</param>
    /// <param name="alternativeProtocol">代替プロトコル名</param>
    /// <returns>ログメッセージ</returns>
    public static string InitialProtocolFailedRetrying(string failedProtocol, string error, string alternativeProtocol)
    {
        return $"{failedProtocol}接続失敗: {error}. 代替プロトコル({alternativeProtocol})で再試行します。";
    }

    /// <summary>
    /// 代替プロトコル接続成功ログメッセージを生成
    /// </summary>
    /// <param name="protocol">成功したプロトコル名</param>
    /// <param name="ipAddress">IPアドレス</param>
    /// <param name="port">ポート番号</param>
    /// <returns>ログメッセージ</returns>
    public static string FallbackConnectionSucceeded(string protocol, string ipAddress, int port)
    {
        return $"代替プロトコル({protocol})で接続成功: {ipAddress}:{port}";
    }

    /// <summary>
    /// 両プロトコル失敗詳細ログメッセージを生成（ログ出力用）
    /// </summary>
    /// <param name="ipAddress">IPアドレス</param>
    /// <param name="port">ポート番号</param>
    /// <param name="tcpError">TCPエラー詳細</param>
    /// <param name="udpError">UDPエラー詳細</param>
    /// <returns>ログメッセージ</returns>
    public static string BothProtocolsConnectionFailedDetailed(string ipAddress, int port, string tcpError, string udpError)
    {
        return $"PLC接続失敗: {ipAddress}:{port}. TCP/UDP両プロトコルで接続に失敗しました。\n" +
               $"  - TCP接続エラー: {tcpError}\n" +
               $"  - UDP接続エラー: {udpError}";
    }

    // Phase 5.0-Refactor: 代替プロトコル接続のサマリーログメッセージ生成
    /// <summary>
    /// 代替プロトコル接続成功のサマリーログメッセージ（ExecutionOrchestrator用）
    /// </summary>
    /// <param name="plcIndex">PLC番号</param>
    /// <param name="protocol">使用されたプロトコル名</param>
    /// <param name="fallbackReason">代替プロトコル使用理由（初期プロトコル失敗理由）</param>
    /// <returns>ログメッセージ</returns>
    public static string FallbackConnectionSummary(int plcIndex, string protocol, string fallbackReason)
    {
        return $"[INFO] PLC #{plcIndex} は代替プロトコル({protocol})で接続されました。" +
               $" 初期プロトコル失敗理由: {fallbackReason}";
    }

    /// <summary>
    /// 初期プロトコル接続成功のサマリーログメッセージ（ExecutionOrchestrator用）
    /// </summary>
    /// <param name="plcIndex">PLC番号</param>
    /// <param name="protocol">使用されたプロトコル名</param>
    /// <returns>ログメッセージ</returns>
    public static string InitialProtocolConnectionSummary(int plcIndex, string protocol)
    {
        return $"[DEBUG] PLC #{plcIndex} は初期プロトコル({protocol})で接続されました。";
    }
}
