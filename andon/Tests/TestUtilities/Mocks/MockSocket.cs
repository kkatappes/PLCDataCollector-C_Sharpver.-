using System.Net;
using System.Net.Sockets;

namespace Andon.Tests.TestUtilities.Mocks;

/// <summary>
/// Socketモッククラス
/// テストでのネットワーク通信をシミュレートします
/// </summary>
public class MockSocket : Socket
{
    private readonly List<byte[]> _sentData = new();
    private readonly Queue<byte[]> _receiveQueue = new();

    /// <summary>
    /// 送信されたデータのリスト
    /// </summary>
    public IReadOnlyList<byte[]> SentData => _sentData.AsReadOnly();

    /// <summary>
    /// 送信回数
    /// </summary>
    public int SendCallCount => _sentData.Count;

    /// <summary>
    /// 最後に送信されたデータ
    /// </summary>
    public byte[]? LastSentData => _sentData.LastOrDefault();
    /// <summary>
    /// Dispose()が呼ばれたかどうか
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Shutdown()が呼ばれたかどうか
    /// </summary>
    public bool ShutdownCalled { get; private set; }

    /// <summary>
    /// Close()が呼ばれたかどうか
    /// </summary>
    public bool CloseCalled { get; private set; }

    /// <summary>
    /// 接続状態のセットアップ用
    /// </summary>
    private bool _connected = false;

    /// <summary>
    /// 受信時の遅延時間（ミリ秒）- TC122用
    /// </summary>
    public int ReceiveDelayMs { get; set; } = 0;

    /// <summary>
    /// 接続状態をセットアップ
    /// </summary>
    public void SetupConnected(bool connected)
    {
        _connected = connected;

        // 基底クラスの接続状態もシミュレート
        if (connected)
        {
            try
            {
                // 実際の接続をシミュレート（ローカルホストに疑似接続）
                // これにより基底クラスのConnectedプロパティもtrueになる
                var localEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
                this.Bind(localEndPoint);
            }
            catch
            {
                // 接続シミュレーション失敗時も_connectedは設定済み
            }
        }
    }

    /// <summary>
    /// 接続状態
    /// </summary>
    public new bool Connected => _connected && !IsDisposed;

    private readonly ProtocolType _protocolType;
    private readonly SocketType _socketType;

    /// <summary>
    /// コンストラクタ（UDP用）
    /// </summary>
    public MockSocket() : base(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
    {
        _protocolType = ProtocolType.Udp;
        _socketType = SocketType.Dgram;
    }

    /// <summary>
    /// コンストラクタ（TCP/UDP選択可能）
    /// </summary>
    public MockSocket(bool useTcp) : base(
        AddressFamily.InterNetwork,
        useTcp ? SocketType.Stream : SocketType.Dgram,
        useTcp ? ProtocolType.Tcp : ProtocolType.Udp)
    {
        _protocolType = useTcp ? ProtocolType.Tcp : ProtocolType.Udp;
        _socketType = useTcp ? SocketType.Stream : SocketType.Dgram;
    }

    /// <summary>
    /// 受信データをキューに追加（テスト用）
    /// </summary>
    public void EnqueueReceiveData(byte[] data)
    {
        _receiveQueue.Enqueue(data);
    }

    /// <summary>
    /// 受信データキューのコピーを取得（複数サイクルテスト用）
    /// </summary>
    /// <returns>受信データキューのコピー</returns>
    public List<byte[]> GetReceiveQueueSnapshot()
    {
        return _receiveQueue.ToList();
    }

    /// <summary>
    /// 受信データを設定（TC025用）
    /// </summary>
    /// <param name="data">設定する受信データ</param>
    public void SetReceiveData(byte[] data)
    {
        _receiveQueue.Clear();
        _receiveQueue.Enqueue(data);
    }

    /// <summary>
    /// 送信データをクリア（テスト用）
    /// </summary>
    public void ClearSentData()
    {
        _sentData.Clear();
    }

    /// <summary>
    /// 非同期送信（ArraySegment版、モック実装）
    /// </summary>
    public new Task<int> SendAsync(ArraySegment<byte> buffer, SocketFlags socketFlags)
    {
        // TC123: 送信エラーシミュレーション
        if (_sendFailureException != null)
        {
            return Task.FromException<int>(_sendFailureException);
        }

        // ArraySegmentの範囲のバイトをコピー
        var data = new byte[buffer.Count];
        Array.Copy(buffer.Array!, buffer.Offset, data, 0, buffer.Count);
        _sentData.Add(data);
        return Task.FromResult(buffer.Count);
    }

    /// <summary>
    /// 非同期送信（byte配列版、モック実装）
    /// </summary>
    public Task<int> SendAsync(byte[] buffer, SocketFlags socketFlags)
    {
        // TC123: 送信エラーシミュレーション
        if (_sendFailureException != null)
        {
            return Task.FromException<int>(_sendFailureException);
        }

        _sentData.Add(buffer.ToArray());
        return Task.FromResult(buffer.Length);
    }

    /// <summary>
    /// 非同期送信（ReadOnlyMemory版、モック実装）
    /// </summary>
    public new ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken = default)
    {
        // TC123: 送信エラーシミュレーション
        if (_sendFailureException != null)
        {
            return ValueTask.FromException<int>(_sendFailureException);
        }

        var data = buffer.ToArray();
        _sentData.Add(data);
        return ValueTask.FromResult(data.Length);
    }

    /// <summary>
    /// 非同期受信（ArraySegment版、モック実装）
    /// CancellationToken版にリダイレクト
    /// </summary>
    public new Task<int> ReceiveAsync(ArraySegment<byte> buffer, SocketFlags socketFlags)
    {
        return ReceiveAsync(buffer, socketFlags, CancellationToken.None);
    }

    /// <summary>
    /// 非同期受信（byte配列版、モック実装）
    /// </summary>
    public Task<int> ReceiveAsync(byte[] buffer, SocketFlags socketFlags)
    {
        if (_receiveQueue.Count == 0)
        {
            return Task.FromResult(0);
        }

        var data = _receiveQueue.Dequeue();
        Array.Copy(data, buffer, Math.Min(data.Length, buffer.Length));
        return Task.FromResult(data.Length);
    }

    /// <summary>
    /// 非同期受信（Memory版、モック実装）
    /// </summary>
    public new ValueTask<int> ReceiveAsync(Memory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken = default)
    {
        if (_receiveQueue.Count == 0)
        {
            return ValueTask.FromResult(0);
        }

        var data = _receiveQueue.Dequeue();
        var length = Math.Min(data.Length, buffer.Length);
        data.AsSpan(0, length).CopyTo(buffer.Span);
        return ValueTask.FromResult(length);
    }

    /// <summary>
    /// CancellationTokenをサポートするReceiveAsync（TC025用、TC122用遅延対応）
    /// </summary>
    public new async Task<int> ReceiveAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken)
    {
        // TC123: 受信エラーシミュレーション
        if (_receiveFailureException != null)
        {
            throw _receiveFailureException;
        }

        // キャンセル処理
        if (cancellationToken.IsCancellationRequested)
        {
            return await Task.FromCanceled<int>(cancellationToken);
        }

        // TC122: 受信遅延シミュレーション（ネットワーク遅延を模擬）
        if (ReceiveDelayMs > 0)
        {
            await Task.Delay(ReceiveDelayMs, cancellationToken);
        }

        // データがない場合は0を返す
        if (_receiveQueue.Count == 0)
        {
            return 0;
        }

        var data = _receiveQueue.Dequeue();
        var length = Math.Min(data.Length, buffer.Count);
        Array.Copy(data, 0, buffer.Array!, buffer.Offset, length);

        return length;
    }

    // TODO: ReadOnlyMemory版は一時的にコメントアウト（コンパイルエラー回避）
    /*
    /// <summary>
    /// ValueTask版のReceiveAsyncもオーバーライド (Memory版の既存メソッドをリダイレクト)
    /// </summary>
    public new ValueTask<int> ReceiveAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[DEBUG] MockSocket.ReceiveAsync(ReadOnlyMemory) called - converting to Memory version");
        // 実装は後で修正
        return ValueTask.FromResult(0);
    }
    */

    /// <summary>
    /// ソケットのシャットダウン
    /// </summary>
    public new void Shutdown(SocketShutdown how)
    {
        ShutdownCalled = true;
        try
        {
            base.Shutdown(how);
        }
        catch (SocketException)
        {
            // MockSocketではShutdown例外を無視
        }
    }

    /// <summary>
    /// ソケットのクローズ
    /// </summary>
    public new void Close()
    {
        CloseCalled = true;
        base.Close();
    }

    /// <summary>
    /// リソースの破棄
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }

    // TC123用のエラーシミュレーション機能

    private Exception? _connectionFailureException;
    private Exception? _sendFailureException;
    private Exception? _receiveFailureException;
    private Exception? _dataProcessingFailureException;

    /// <summary>
    /// 接続失敗をセットアップ（TC123 Step3エラー用）
    /// </summary>
    /// <param name="exception">発生させる例外</param>
    public void SetupConnectionFailure(Exception exception)
    {
        _connectionFailureException = exception;
        _connected = false;
    }

    /// <summary>
    /// 接続失敗例外が設定されているかチェック
    /// </summary>
    public bool HasConnectionFailure()
    {
        return _connectionFailureException != null;
    }

    /// <summary>
    /// 接続失敗例外を取得
    /// </summary>
    public Exception? GetConnectionFailureException()
    {
        return _connectionFailureException;
    }

    /// <summary>
    /// 接続成功をセットアップ（TC123用）
    /// </summary>
    public void SetupConnectionSuccess()
    {
        _connectionFailureException = null;
        _connected = true;
    }

    /// <summary>
    /// 送信失敗をセットアップ（TC123 Step4エラー用）
    /// </summary>
    /// <param name="exception">発生させる例外</param>
    public void SetupSendFailure(Exception exception)
    {
        _sendFailureException = exception;
    }

    /// <summary>
    /// 送信成功をセットアップ（TC123用）
    /// </summary>
    public void SetupSendSuccess()
    {
        _sendFailureException = null;
    }

    /// <summary>
    /// 受信失敗をセットアップ（TC123 Step5エラー用）
    /// </summary>
    /// <param name="exception">発生させる例外</param>
    public void SetupReceiveFailure(Exception exception)
    {
        _receiveFailureException = exception;
    }

    /// <summary>
    /// 受信成功をセットアップ（TC123用）
    /// </summary>
    /// <param name="responseData">正常な応答データ</param>
    public void SetupReceiveSuccess(string responseData)
    {
        _receiveFailureException = null;
        var responseBytes = Convert.FromHexString(responseData);
        EnqueueReceiveData(responseBytes);
    }

    /// <summary>
    /// データ処理失敗をセットアップ（TC123 Step6エラー用）
    /// </summary>
    /// <param name="exception">発生させる例外</param>
    public void SetupDataProcessingFailure(Exception exception)
    {
        _dataProcessingFailureException = exception;
    }

    /// <summary>
    /// 接続エラーが設定されているかチェック
    /// </summary>
    /// <returns>接続エラーの例外、またはnull</returns>
    public Exception? GetConnectionError()
    {
        return _connectionFailureException;
    }

    /// <summary>
    /// 送信エラーが設定されているかチェック
    /// </summary>
    /// <returns>送信エラーの例外、またはnull</returns>
    public Exception? GetSendError()
    {
        return _sendFailureException;
    }

    /// <summary>
    /// 受信エラーが設定されているかチェック
    /// </summary>
    /// <returns>受信エラーの例外、またはnull</returns>
    public Exception? GetReceiveError()
    {
        return _receiveFailureException;
    }

    /// <summary>
    /// データ処理エラーが設定されているかチェック
    /// </summary>
    /// <returns>データ処理エラーの例外、またはnull</returns>
    public Exception? GetDataProcessingError()
    {
        return _dataProcessingFailureException;
    }
}
