using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Andon.Tests.TestUtilities.Mocks;

/// <summary>
/// TC116 Step3to5 UDP完全サイクル統合テスト用MockUDPサーバー
/// UDPプロトコルでの4Eフレーム形式レスポンス送信をサポート
/// </summary>
public class MockUdpServer : IDisposable
{
    private readonly string _ipAddress;
    private readonly int _port;
    private UdpClient? _udpClient;
    private IPEndPoint? _endPoint;
    private readonly Dictionary<string, string> _responseMap;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task? _listenerTask;
    private bool _isRunning = false;

    public MockUdpServer(string ipAddress, int port)
    {
        _ipAddress = ipAddress;
        _port = port;
        _responseMap = new Dictionary<string, string>();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// MockUDPサーバーを開始
    /// </summary>
    public void Start()
    {
        if (_isRunning)
            return;

        _endPoint = new IPEndPoint(IPAddress.Parse(_ipAddress), _port);
        _udpClient = new UdpClient(_endPoint);
        _isRunning = true;

        // バックグラウンドでリクエスト受信・応答送信処理を開始
        _listenerTask = Task.Run(async () => await ListenForRequests(_cancellationTokenSource.Token));
    }

    /// <summary>
    /// MockUDPサーバーを停止
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
            return;

        _cancellationTokenSource.Cancel();
        _listenerTask?.Wait(TimeSpan.FromSeconds(1));
        _udpClient?.Close();
        _udpClient?.Dispose();
        _isRunning = false;
    }

    /// <summary>
    /// 特定のSLMPフレームに対する応答データを設定
    /// </summary>
    /// <param name="requestFrame">リクエストフレーム（16進数文字列）</param>
    /// <param name="responseData">応答データ（16進数文字列）</param>
    public void SetResponse(string requestFrame, string responseData)
    {
        _responseMap[requestFrame] = responseData;
    }

    /// <summary>
    /// M機器応答データ（125バイト → 250文字）を作成
    /// 4Eフレーム（Binary形式）の正しい構造に従う
    /// </summary>
    /// <returns>M機器応答データ（16進数文字列）</returns>
    public static string CreateMDeviceResponse()
    {
        var responseBuilder = new StringBuilder();

        // 4Eフレーム（Binary形式）応答構造
        // サブヘッダ（2バイト = 4文字）
        responseBuilder.Append("D400");

        // シーケンス番号（2バイト = 4文字）
        responseBuilder.Append("1234");

        // 予約（2バイト = 4文字）
        responseBuilder.Append("0000");

        // ネットワーク番号（1バイト = 2文字）
        responseBuilder.Append("00");

        // PC番号（1バイト = 2文字）
        responseBuilder.Append("FF");

        // I/O番号（2バイト = 4文字、リトルエンディアン）
        responseBuilder.Append("FF03");

        // 局番（1バイト = 2文字）
        responseBuilder.Append("00");

        // データ長（2バイト = 4文字、リトルエンディアン）
        // データ長 = 終了コード(2バイト) + データ部(110バイト) = 112バイト = 0x0070
        responseBuilder.Append("7000");

        // 終了コード（2バイト = 4文字）
        responseBuilder.Append("0000");

        // ヘッダー合計: 4+4+4+2+2+4+2+4+4 = 30文字（15バイト）
        // データ部: 250 - 30 = 220文字（110バイト）

        // M000-M999データ（110バイト = 220文字）
        responseBuilder.Append(new string('0', 220));

        return responseBuilder.ToString(); // 合計250文字（125バイト）
    }

    /// <summary>
    /// D機器応答データ（2000バイト → 4000文字）を作成
    /// 4Eフレーム（Binary形式）の正しい構造に従う
    /// </summary>
    /// <returns>D機器応答データ（16進数文字列）</returns>
    public static string CreateDDeviceResponse()
    {
        var responseBuilder = new StringBuilder();

        // 4Eフレーム（Binary形式）応答構造
        // サブヘッダ（2バイト = 4文字）
        responseBuilder.Append("D400");

        // シーケンス番号（2バイト = 4文字）
        responseBuilder.Append("1234");

        // 予約（2バイト = 4文字）
        responseBuilder.Append("0000");

        // ネットワーク番号（1バイト = 2文字）
        responseBuilder.Append("00");

        // PC番号（1バイト = 2文字）
        responseBuilder.Append("FF");

        // I/O番号（2バイト = 4文字、リトルエンディアン）
        responseBuilder.Append("FF03");

        // 局番（1バイト = 2文字）
        responseBuilder.Append("00");

        // データ長（2バイト = 4文字、リトルエンディアン）
        // データ長 = 終了コード(2バイト) + データ部(1985バイト) = 1987バイト = 0x07C3
        responseBuilder.Append("C307");

        // 終了コード（2バイト = 4文字）
        responseBuilder.Append("0000");

        // ヘッダー合計: 4+4+4+2+2+4+2+4+4 = 30文字（15バイト）
        // データ部: 4000 - 30 = 3970文字（1985バイト）

        // D000-D999データ（1985バイト = 3970文字）
        responseBuilder.Append(new string('1', 3970));

        return responseBuilder.ToString(); // 合計4000文字（2000バイト）
    }

    /// <summary>
    /// UDP リクエストを待機し、適切な応答を送信
    /// </summary>
    private async Task ListenForRequests(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _udpClient != null)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync();
                    var receivedData = result.Buffer;
                    var clientEndPoint = result.RemoteEndPoint;

                    // 受信データを16進数文字列に変換
                    var receivedHex = Convert.ToHexString(receivedData);

                    // マッピングから対応する応答を検索
                    if (_responseMap.TryGetValue(receivedHex, out var responseHex))
                    {
                        // 16進数文字列をバイト配列に変換
                        var responseBytes = ConvertHexStringToBytes(responseHex);

                        // クライアントに応答を送信
                        await _udpClient.SendAsync(responseBytes, clientEndPoint);
                    }
                    else
                    {
                        // デフォルト応答（エラーレスポンス）
                        var errorResponse = "D4001234" + "FFFF"; // エラーコード
                        var errorBytes = ConvertHexStringToBytes(errorResponse);
                        await _udpClient.SendAsync(errorBytes, clientEndPoint);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // UdpClientが破棄された場合は正常終了
                    break;
                }
                catch (SocketException)
                {
                    // ソケットエラーは無視して継続
                    continue;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // キャンセルされた場合は正常終了
        }
    }

    /// <summary>
    /// 16進数文字列をバイト配列に変換
    /// </summary>
    /// <param name="hexString">16進数文字列</param>
    /// <returns>バイト配列</returns>
    private static byte[] ConvertHexStringToBytes(string hexString)
    {
        if (string.IsNullOrEmpty(hexString))
            return Array.Empty<byte>();

        // 文字数が奇数の場合は先頭に0を追加
        if (hexString.Length % 2 != 0)
            hexString = "0" + hexString;

        byte[] bytes = new byte[hexString.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
        }

        return bytes;
    }

    /// <summary>
    /// リソースを解放
    /// </summary>
    public void Dispose()
    {
        Stop();
        _udpClient?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}