using System.Net.Sockets;
using System.Text;
using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Tests.TestUtilities.Mocks;

namespace Andon.Tests.Unit.Core.Managers;

/// <summary>
/// TC025単体テスト専用クラス
/// </summary>
public class TC025_Test
{
    /// <summary>
    /// TC025: ReceiveResponseAsync_正常受信 ★重要テスト
    /// PLCからのSLMPレスポンス受信機能の正常動作を検証する重要テスト
    /// 19時deadline対応の最小成功基準に含まれる必須テスト
    /// </summary>
    [Fact]
    public async Task TC025_ReceiveResponseAsync_正常受信()
    {
        // Arrange（準備）
        // TimeoutConfig準備
        var timeoutConfig = new TimeoutConfig
        {
            ReceiveTimeoutMs = 3000
        };

        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.1.100",
            Port = 5007,
            UseTcp = true
        };

        // MockPlcServer準備（4Eフレーム応答 - memo.mdの実データ使用）
        var mockPlcServer = new MockPlcServer();
        mockPlcServer.SetResponse4EFrame("D4000000"); // 4Eフレーム識別ヘッダー (実データ)
        mockPlcServer.SetM000ToM999ReadResponse(); // M000-M999読み込み応答データ (111バイト)

        // Debug: MockPlcServerのデータ確認
        var serverResponseData = mockPlcServer.GetResponseData();
        Assert.NotNull(serverResponseData);
        Assert.True(serverResponseData.Length > 0, $"MockPlcServer response data should not be empty. Length: {serverResponseData.Length}");

        // MockSocket設定
        var mockSocket = new MockSocket();
        mockSocket.SetupConnected(true);
        mockPlcServer.ConfigureMockSocket(mockSocket);

        // Debug: MockSocketの状態確認
        Assert.True(mockSocket.Connected, "MockSocket should be connected");
        Assert.False(mockSocket.IsDisposed, "MockSocket should not be disposed");

        // PlcCommunicationManager接続済み状態をシミュレート
        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig);

        // プライベートフィールドを設定するためリフレクションを使用
        var socketField = typeof(PlcCommunicationManager).GetField("_socket",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(socketField); // リフレクション取得確認
        socketField.SetValue(manager, mockSocket);

        // Debug: 設定後の状態確認
        var setSocket = socketField.GetValue(manager) as MockSocket;
        Assert.NotNull(setSocket); // ソケット設定確認
        Assert.True(setSocket.Connected, "Set socket should be connected");

        // Debug: MockSocketのReceiveQueue状態確認（リフレクション使用）
        var receiveQueueField = typeof(MockSocket).GetField("_receiveQueue",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var receiveQueue = receiveQueueField?.GetValue(mockSocket) as Queue<byte[]>;
        Assert.NotNull(receiveQueue);
        Assert.True(receiveQueue.Count > 0, $"MockSocket receive queue should have data. Count: {receiveQueue.Count}");

        // Act（実行）
        var result = await manager.ReceiveResponseAsync(timeoutConfig.ReceiveTimeoutMs);

        // Assert（検証）
        // 受信データ検証
        Assert.NotNull(result.ResponseData);
        Assert.True(result.ResponseData.Length > 0);
        Assert.Equal(result.DataLength, result.ResponseData.Length);

        // 時刻・時間検証
        Assert.NotNull(result.ReceivedAt);
        Assert.True(result.ReceivedAt.Value <= DateTime.UtcNow);
        Assert.NotNull(result.ReceiveTime);
        Assert.True(result.ReceiveTime.Value.TotalMilliseconds > 0);
        Assert.True(result.ReceiveTime.Value.TotalMilliseconds < timeoutConfig.ReceiveTimeoutMs);

        // 16進数文字列検証（4Eフレーム確認 - 実データに合わせて修正）
        Assert.NotNull(result.ResponseHex);
        Assert.False(string.IsNullOrEmpty(result.ResponseHex));
        Assert.StartsWith("D4000000", result.ResponseHex); // 4Eフレーム識別 (memo.md実データ)

        // フレームタイプ検証
        Assert.Equal(Andon.Core.Models.FrameType.Frame4E, result.FrameType);

        // エラー情報検証（成功時はnull）
        Assert.Null(result.ErrorMessage);
    }
}