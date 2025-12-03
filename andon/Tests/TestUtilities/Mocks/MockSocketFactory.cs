using System.Net.Sockets;
using Andon.Core.Interfaces;

namespace Andon.Tests.TestUtilities.Mocks;

/// <summary>
/// SocketFactoryモッククラス
/// テスト時にSocket接続をシミュレートします
/// </summary>
public class MockSocketFactory : ISocketFactory
{
    private readonly bool _shouldSucceed;
    private readonly int _simulatedDelayMs;
    private readonly MockSocket? _preconfiguredSocket;

    // Phase 2-Green Step 2: プロトコルごとの成功/失敗制御
    private readonly bool? _tcpShouldSucceed;
    private readonly bool? _udpShouldSucceed;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="shouldSucceed">接続成功をシミュレートするか</param>
    /// <param name="simulatedDelayMs">シミュレートする接続遅延（ミリ秒）</param>
    public MockSocketFactory(bool shouldSucceed = true, int simulatedDelayMs = 10)
    {
        _shouldSucceed = shouldSucceed;
        _simulatedDelayMs = simulatedDelayMs;
        _preconfiguredSocket = null;
        _tcpShouldSucceed = null;
        _udpShouldSucceed = null;
    }

    /// <summary>
    /// コンストラクタ（事前設定済みMockSocket用）
    /// </summary>
    /// <param name="preconfiguredSocket">事前設定済みMockSocket</param>
    /// <param name="shouldSucceed">接続成功をシミュレートするか</param>
    /// <param name="simulatedDelayMs">シミュレートする接続遅延（ミリ秒）</param>
    public MockSocketFactory(MockSocket preconfiguredSocket, bool shouldSucceed = true, int simulatedDelayMs = 10)
    {
        _shouldSucceed = shouldSucceed;
        _simulatedDelayMs = simulatedDelayMs;
        _preconfiguredSocket = preconfiguredSocket;
        _tcpShouldSucceed = null;
        _udpShouldSucceed = null;
    }

    /// <summary>
    /// コンストラクタ（プロトコルごとの成功/失敗制御用）
    /// Phase 2-Green Step 2: TC_P2_003, TC_P2_004用
    /// </summary>
    /// <param name="tcpShouldSucceed">TCP接続成功をシミュレートするか</param>
    /// <param name="udpShouldSucceed">UDP接続成功をシミュレートするか</param>
    /// <param name="simulatedDelayMs">シミュレートする接続遅延（ミリ秒）</param>
    public MockSocketFactory(bool tcpShouldSucceed, bool udpShouldSucceed, int simulatedDelayMs = 10)
    {
        _shouldSucceed = true; // デフォルト値（使用されない）
        _simulatedDelayMs = simulatedDelayMs;
        _preconfiguredSocket = null;
        _tcpShouldSucceed = tcpShouldSucceed;
        _udpShouldSucceed = udpShouldSucceed;
    }

    /// <summary>
    /// Socket作成
    /// </summary>
    public Socket CreateSocket(bool useTcp)
    {
        // SetMockSocketで設定されたMockSocketがある場合、それを優先的に返す
        if (_configuredMockSocket != null)
        {
            return _configuredMockSocket;
        }

        // 事前設定済みMockSocketがある場合、応答データをコピーして新しいMockSocketを作成
        if (_preconfiguredSocket != null)
        {
            var newSocket = new MockSocket(useTcp);

            // 接続状態をコピー
            newSocket.SetupConnected(_preconfiguredSocket.Connected);

            // 応答データキューをコピー
            var responseDataSnapshot = _preconfiguredSocket.GetReceiveQueueSnapshot();
            foreach (var data in responseDataSnapshot)
            {
                newSocket.EnqueueReceiveData(data);
            }

            // TC122: 受信遅延時間をコピー
            newSocket.ReceiveDelayMs = _preconfiguredSocket.ReceiveDelayMs;

            // TC124: エラーシミュレーション設定をコピー
            var connectionError = _preconfiguredSocket.GetConnectionError();
            if (connectionError != null)
            {
                newSocket.SetupConnectionFailure(connectionError);
            }

            var sendError = _preconfiguredSocket.GetSendError();
            if (sendError != null)
            {
                newSocket.SetupSendFailure(sendError);
            }

            var receiveError = _preconfiguredSocket.GetReceiveError();
            if (receiveError != null)
            {
                newSocket.SetupReceiveFailure(receiveError);
            }

            return newSocket;
        }

        // テスト用のMockSocketを返す（TCP/UDP対応）
        return new MockSocket(useTcp);
    }

    /// <summary>
    /// Socket接続（非同期）
    /// </summary>
    public async Task<bool> ConnectAsync(Socket socket, string ipAddress, int port, int timeoutMs)
    {
        // 接続処理をシミュレート
        await Task.Delay(_simulatedDelayMs);

        // MockSocketに設定された接続失敗例外をチェック
        if (socket is MockSocket mockSocket && mockSocket.HasConnectionFailure())
        {
            throw mockSocket.GetConnectionFailureException()!;
        }

        // Phase 2-Green Step 2: プロトコルごとの成功/失敗制御
        if (_tcpShouldSucceed.HasValue || _udpShouldSucceed.HasValue)
        {
            // プロトコルを判定
            bool isTcp = socket.ProtocolType == ProtocolType.Tcp;

            // プロトコルに応じて成功/失敗を返す
            if (isTcp)
            {
                return _tcpShouldSucceed ?? _shouldSucceed;
            }
            else
            {
                return _udpShouldSucceed ?? _shouldSucceed;
            }
        }

        if (!_shouldSucceed)
        {
            return false; // 接続失敗
        }

        // 接続成功時の処理（MockSocketの状態を設定）
        // 実際のSocketでは自動的に設定されるが、MockSocketでは手動設定が必要
        return true;
    }

    private MockSocket? _configuredMockSocket;

    /// <summary>
    /// モックソケットを設定（TC123テスト用）
    /// </summary>
    /// <param name="mockSocket">設定するモックソケット</param>
    public void SetMockSocket(MockSocket mockSocket)
    {
        _configuredMockSocket = mockSocket;
    }

    /// <summary>
    /// 設定されたモックソケットを取得（TC123テスト用）
    /// </summary>
    /// <returns>設定されたモックソケット、またはnull</returns>
    public MockSocket? GetConfiguredMockSocket()
    {
        return _configuredMockSocket;
    }
}
