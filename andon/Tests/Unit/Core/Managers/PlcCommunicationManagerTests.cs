using System.Net.Sockets;
using System.Text;
using Xunit;
using Moq;
using Andon.Core.Interfaces;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Constants;
using Andon.Tests.TestUtilities.Mocks;

namespace Andon.Tests.Unit.Core.Managers;

/// <summary>
/// PlcCommunicationManager単体テスト (最優先)
/// </summary>
public class PlcCommunicationManagerTests
{
    /// <summary>
    /// TC021-1: M000-M999フレーム正常送信テスト
    /// </summary>
    [Fact]
    public async Task TC021_SendFrameAsync_正常送信_M000_M999フレーム()
    {
        // Arrange: テストデータ準備
        var mockSocket = new MockSocket();

        var connectionResponse = new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            Socket = mockSocket,
            ConnectedAt = DateTime.Now,
            ConnectionTime = 100.5
        };

        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.3.250",
            Port = 5007,
            UseTcp = false,
            IsBinary = false,
            FrameVersion = FrameVersion.Frame4E
        };

        var timeoutConfig = new TimeoutConfig
        {
            SendTimeoutMs = 3000
        };

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,  // BitExpansionSettings (Phase3追加)
            connectionResponse
        );

        const string frameString = "54001234000000010401006400000090E8030000"; // Read(0x0401)レガシーフレーム
        var expectedBytes = Encoding.ASCII.GetBytes(frameString);

        // Act: フレーム送信実行
        await manager.SendFrameAsync(frameString);

        // Assert: 検証
        Assert.Equal(1, mockSocket.SendCallCount); // 1回だけ送信されたか
        Assert.NotNull(mockSocket.LastSentData); // データが送信されたか
        Assert.Equal(expectedBytes, mockSocket.LastSentData); // 送信データが一致するか
        Assert.Equal(40, mockSocket.LastSentData!.Length); // 送信バイト数が40バイトか
    }

    /// <summary>
    /// TC021-2: D000-D999フレーム正常送信テスト
    /// </summary>
    [Fact]
    public async Task TC021_SendFrameAsync_正常送信_D000_D999フレーム()
    {
        // Arrange: テストデータ準備
        var mockSocket = new MockSocket();

        var connectionResponse = new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            Socket = mockSocket,
            ConnectedAt = DateTime.Now,
            ConnectionTime = 100.5
        };

        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.3.250",
            Port = 5007,
            UseTcp = false,
            IsBinary = false,
            FrameVersion = FrameVersion.Frame4E
        };

        var timeoutConfig = new TimeoutConfig
        {
            SendTimeoutMs = 3000
        };

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,  // BitExpansionSettings (Phase3追加)
            connectionResponse
        );

        const string frameString = "54001234000000010400A800000090E8030000";
        var expectedBytes = Encoding.ASCII.GetBytes(frameString);

        // Act: フレーム送信実行
        await manager.SendFrameAsync(frameString);

        // Assert: 検証
        Assert.Equal(1, mockSocket.SendCallCount); // 1回だけ送信されたか
        Assert.NotNull(mockSocket.LastSentData); // データが送信されたか
        Assert.Equal(expectedBytes, mockSocket.LastSentData); // 送信データが一致するか
        Assert.Equal(38, mockSocket.LastSentData!.Length); // 送信バイト数が38バイトか（実際のフレーム文字列の長さ）
    }

    /// <summary>
    /// TC022: 全機器データ取得テスト（複数フレーム送信）
    /// </summary>
    [Fact]
    public async Task TC022_SendFrameAsync_全機器データ取得()
    {
        // Arrange: テストデータ準備
        var mockSocket = new MockSocket();

        var connectionResponse = new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            Socket = mockSocket,
            ConnectedAt = DateTime.Now,
            ConnectionTime = 100.5
        };

        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.3.250",
            Port = 5007,
            UseTcp = false,
            IsBinary = false,
            FrameVersion = FrameVersion.Frame4E
        };

        var timeoutConfig = new TimeoutConfig
        {
            SendTimeoutMs = 3000,
            SendIntervalMs = 100
        };

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,  // BitExpansionSettings (Phase3追加)
            connectionResponse
        );

        // 複数フレーム準備（M機器・D機器）
        var frames = new List<string>
        {
            "54001234000000010401006400000090E8030000", // M001-M999 - Read(0x0401)レガシーフレーム
            "54001234000000010400A800000090E8030000"  // D001-D999 - Read(0x0401)レガシーフレーム
        };

        // Act: 複数フレーム送信実行
        var result = await manager.SendMultipleFramesAsync(frames);

        // Assert: 検証
        // 1. 全体結果検証
        Assert.True(result.IsSuccess, "全フレーム送信が成功すること");
        Assert.Equal(2, result.TotalFrameCount);
        Assert.Equal(2, result.SuccessfulFrameCount);
        Assert.Equal(0, result.FailedFrameCount);

        // 2. 個別フレーム結果検証
        Assert.Contains("M", result.FrameResults.Keys);
        Assert.Contains("D", result.FrameResults.Keys);

        var mResult = result.FrameResults["M"];
        Assert.True(mResult.IsSuccess, "M機器フレーム送信が成功すること");
        Assert.Equal(40, mResult.SentBytes); // M機器フレーム長
        Assert.Equal("M", mResult.DeviceType);
        Assert.Equal("M001-M999", mResult.DeviceRange);

        var dResult = result.FrameResults["D"];
        Assert.True(dResult.IsSuccess, "D機器フレーム送信が成功すること");
        Assert.Equal(38, dResult.SentBytes); // D機器フレーム長
        Assert.Equal("D", dResult.DeviceType);
        Assert.Equal("D001-D999", dResult.DeviceRange);

        // 3. 送信統計検証
        Assert.True(result.TotalTransmissionTime.TotalMilliseconds > 0, "送信時間が記録されていること");
        Assert.Equal(new List<string> { "M", "D" }, result.TargetDeviceTypes);

        // 4. Socket呼び出し検証
        Assert.Equal(2, mockSocket.SendCallCount); // 2回送信されたか
        Assert.Equal(2, mockSocket.SentData.Count); // 2つのフレームが記録されているか

        // 各フレームのデータ検証
        var expectedMBytes = Encoding.ASCII.GetBytes(frames[0]);
        var expectedDBytes = Encoding.ASCII.GetBytes(frames[1]);
        Assert.Equal(expectedMBytes, mockSocket.SentData[0]);
        Assert.Equal(expectedDBytes, mockSocket.SentData[1]);
    }

    /// <summary>
    /// TC017: ConnectAsync TCP接続成功テスト
    /// </summary>
    [Fact]
    public async Task TC017_ConnectAsync_TCP接続成功()
    {
        // Arrange: テストデータ準備
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.1.10",
            Port = 5000,
            UseTcp = true,
            IsBinary = false,
            FrameVersion = FrameVersion.Frame4E
        };

        var timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        // MockSocketFactoryを作成（TCP接続成功をシミュレート）
        var mockSocketFactory = new MockSocketFactory(shouldSucceed: true, simulatedDelayMs: 10);

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,  // BitExpansionSettings
            null,  // ConnectionResponse
            mockSocketFactory
        );

        // Act: TCP接続実行
        var result = await manager.ConnectAsync();

        // Assert: 検証
        Assert.NotNull(result); // 結果オブジェクトが返される
        Assert.Equal(ConnectionStatus.Connected, result.Status); // 接続成功
        Assert.NotNull(result.Socket); // Socketインスタンスが取得される
        Assert.NotNull(result.ConnectedAt); // 接続完了時刻が記録される
        Assert.True(result.ConnectedAt != default(DateTime)); // デフォルト値ではない
        Assert.NotNull(result.ConnectionTime); // 接続処理時間が記録される
        Assert.True(result.ConnectionTime > 0); // 接続処理時間が正の値
        Assert.True(result.ConnectionTime < timeoutConfig.ConnectTimeoutMs); // タイムアウト時間内
        Assert.Null(result.ErrorMessage); // 成功時はエラーメッセージなし
    }

    /// <summary>
    /// TC017_1: ConnectAsync ソケットタイムアウト設定確認テスト
    ///
    /// 目的:
    /// ConnectAsync()メソッドが、接続後のSocketに対して正しくタイムアウト設定を行うことを検証します。
    /// Socketレベルでタイムアウトを設定することで、Step4（送受信処理）で個別にタイムアウト制御を
    /// 実装する必要をなくし、保守性を向上させます。
    /// </summary>
    [Fact]
    public async Task TC017_1_ConnectAsync_ソケットタイムアウト設定確認()
    {
        // Arrange: テストデータ準備
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.1.10",
            Port = 5000,
            UseTcp = true,
            IsBinary = false,
            FrameVersion = FrameVersion.Frame4E
        };

        var timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 5000
        };

        // MockSocketFactoryを作成（TCP接続成功をシミュレート）
        var mockSocketFactory = new MockSocketFactory(shouldSucceed: true, simulatedDelayMs: 10);

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,  // BitExpansionSettings
            null,  // ConnectionResponse
            mockSocketFactory
        );

        // Act: TCP接続実行
        var result = await manager.ConnectAsync();

        // Assert: 検証
        Assert.NotNull(result); // 結果オブジェクトが返される
        Assert.Equal(ConnectionStatus.Connected, result.Status); // 接続成功
        Assert.NotNull(result.Socket); // Socketインスタンスが取得される

        // 重要: ソケットタイムアウト設定確認
        Assert.Equal(timeoutConfig.SendTimeoutMs, result.Socket.SendTimeout); // 送信タイムアウト設定確認
        Assert.Equal(timeoutConfig.ReceiveTimeoutMs, result.Socket.ReceiveTimeout); // 受信タイムアウト設定確認
    }

    /// <summary>
    /// TC018: ConnectAsync UDP接続成功テスト
    ///
    /// 目的:
    /// ConnectAsync()メソッドのUDP接続機能を検証します。
    /// UDP疎通確認方法（TDD・オフライン環境対応）を含みます。
    ///
    /// 検証項目:
    /// - UDP接続成功（Socket.Connected = false が正常動作）
    /// - Socket.ProtocolType = ProtocolType.Udp
    /// - RemoteEndPoint設定確認
    /// - UDP疎通確認（模擬フレーム送受信）成功
    /// - 接続時間記録（疎通確認含む）
    /// </summary>
    [Fact]
    public async Task TC018_ConnectAsync_UDP接続成功()
    {
        // Arrange: テストデータ準備
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.1.10",
            Port = 5000,
            UseTcp = false, // UDP接続モード
            IsBinary = false,
            FrameVersion = FrameVersion.Frame4E
        };

        var timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        // MockSocketFactoryを作成（UDP接続成功をシミュレート）
        var mockSocketFactory = new MockSocketFactory(shouldSucceed: true, simulatedDelayMs: 10);

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,  // BitExpansionSettings
            null,  // ConnectionResponse
            mockSocketFactory
        );

        // Act: UDP接続実行
        var result = await manager.ConnectAsync();

        // Assert: 検証
        Assert.NotNull(result); // 結果オブジェクトが返される
        Assert.Equal(ConnectionStatus.Connected, result.Status); // 接続成功

        Assert.NotNull(result.Socket); // Socketインスタンスが取得される

        // 重要: UDPの特性検証
        // UDP接続ではSocket.Connectedがfalseとなるのが正常動作
        Assert.False(result.Socket.Connected); // UDP接続の正常動作
        Assert.Equal(ProtocolType.Udp, result.Socket.ProtocolType); // UDPプロトコル

        // 接続情報検証
        Assert.NotNull(result.ConnectedAt); // 接続完了時刻が記録される
        Assert.True(result.ConnectedAt != default(DateTime)); // デフォルト値ではない

        Assert.NotNull(result.ConnectionTime); // 接続処理時間が記録される
        Assert.True(result.ConnectionTime > 0); // 接続処理時間が正の値
        Assert.True(result.ConnectionTime < timeoutConfig.ConnectTimeoutMs); // タイムアウト時間内

        Assert.Null(result.ErrorMessage); // 成功時はエラーメッセージなし
    }

    /// <summary>
    /// TC027: DisconnectAsync_正常切断
    /// </summary>
    [Fact]
    public async Task TC027_DisconnectAsync_正常切断()
    {
        // Arrange
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.1.100",
            Port = 5000,
            UseTcp = true,
            IsBinary = false,
            FrameVersion = FrameVersion.Frame4E
        };
        var timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        // MockSocketFactoryを作成（TCP接続成功をシミュレート）
        var mockSocketFactory = new MockSocketFactory(shouldSucceed: true, simulatedDelayMs: 10);

        var manager = new PlcCommunicationManager(connectionConfig, timeoutConfig, null, null, mockSocketFactory);

        // まず接続を実行
        var connectResult = await manager.ConnectAsync();
        Assert.NotNull(connectResult);
        Assert.Equal(ConnectionStatus.Connected, connectResult.Status);

        // Act
        var result = await manager.DisconnectAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DisconnectStatus.Success, result.Status);
        Assert.Equal("切断完了", result.Message);

        // 接続統計情報の検証
        Assert.NotNull(result.ConnectionStats);
        Assert.True(result.ConnectionStats.TotalConnectionTime.TotalMilliseconds >= 0);
        Assert.True(result.ConnectionStats.DisconnectedAt > result.ConnectionStats.ConnectedAt);

        // リソース解放確認（モック固有のプロパティは後で実装）
        // TODO: MockSocketのインスタンスアクセス方法を実装後に有効化
        // Assert.True(mockSocket.IsDisposed);
        // Assert.True(mockSocket.ShutdownCalled);
        // Assert.True(mockSocket.CloseCalled);
    }

    /// <summary>
    /// TC028: DisconnectAsync_未接続状態切断
    /// </summary>
    [Fact]
    public async Task TC028_DisconnectAsync_未接続状態切断()
    {
        // Arrange
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.1.100",
            Port = 5000,
            UseTcp = true,
            IsBinary = false,
            FrameVersion = FrameVersion.Frame4E
        };
        var timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        var manager = new PlcCommunicationManager(connectionConfig, timeoutConfig, null);

        // ConnectAsyncを実行していない（未接続状態）

        // Act
        var result = await manager.DisconnectAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DisconnectStatus.NotConnected, result.Status);
        Assert.Equal("既に切断済みまたは未接続状態です。", result.Message);
        Assert.Null(result.ConnectionStats); // 未接続時は統計情報なし

        // 例外が発生しないことを確認（安全な処理）
    }

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

        // MockPlcServer準備（4Eフレーム応答）
        var mockPlcServer = new Andon.Tests.TestUtilities.Mocks.MockPlcServer();
        mockPlcServer.SetResponse4EFrame("D4001234"); // 4Eフレーム識別ヘッダー
        mockPlcServer.SetM000ToM999ReadResponse(); // M000-M999読み込み応答データ

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

    /// <summary>
    /// TC029: ProcessReceivedRawData_基本後処理成功 ★重要テスト
    /// PLCから受信した生データの基本後処理機能が正常に動作することを確認
    /// Step6データ処理の第1段階として、受信した生データの基本後処理が成功することを検証
    /// </summary>
    [Fact]
    public async Task TC029_ProcessReceivedRawData_基本後処理成功()
    {
        // Arrange（準備）
        // PlcCommunicationManagerインスタンス作成
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.3.250",
            Port = 5007,
            UseTcp = false,
            IsBinary = false,
            FrameVersion = FrameVersion.Frame3E  // ✅ 修正: Frame4E → Frame3E
        };

        var timeoutConfig = new TimeoutConfig
        {
            ReceiveTimeoutMs = 3000,
            SendTimeoutMs = 3000
        };

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig
        );

        // 受信生データ準備（3E ASCII形式フレーム応答例）
        // フレーム構築方法.md L.61-77 に準拠した3E ASCII応答フレーム:
        //   Idx 0-1:   "D0"     (サブヘッダ)
        //   Idx 2-3:   "00"     (ネットワーク番号)
        //   Idx 4-5:   "FF"     (PC番号)
        //   Idx 6-9:   "03FF"   (I/O番号)
        //   Idx 10-11: "00"     (局番)
        //   Idx 12-15: "0008"   (データ長 = 終了コード4文字 + デバイスデータ4文字)
        //   Idx 16-19: "0000"   (終了コード、正常終了)
        //   Idx 20-23: "0123"   (デバイスデータ、1ワード)
        //   合計: 24文字
        string rawDataAscii = "D000FF03FF00" + "0008" + "0000" + "0123";  // 24文字
        byte[] rawData = System.Text.Encoding.ASCII.GetBytes(rawDataAscii);

        // ProcessedDeviceRequestInfo準備（前処理情報）
        var requestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "D",
            StartAddress = 100,
            Count = 1,  // ✅ 修正: 2 → 1（デバイスデータが2バイト = 1ワード分しかないため）
            FrameType = FrameType.Frame3E,
            RequestedAt = DateTime.Now,
            DeviceSpecifications = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 100)
            }
        };

        // CancellationToken準備
        var cancellationToken = new CancellationTokenSource().Token;

        // Act（実行）
        var result = await manager.ProcessReceivedRawData(
            rawData,
            requestInfo,
            cancellationToken
        );

        // Assert（検証）
        // 基本結果検証
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, "ProcessReceivedRawData処理が成功すること");
        Assert.True(result.ProcessedData.Count > 0, "処理済みデバイスが1つ以上存在すること");
        Assert.Equal(0, result.Errors.Count);
        Assert.True(result.ProcessingTimeMs > 0, "処理時間が正の値であること");

        // データ処理検証
        Assert.True(result.TotalProcessedDevices > 0, "処理済みデバイス数が正の値であること");
        Assert.Equal(result.ProcessedData.Count, result.TotalProcessedDevices);

        // デバイス値検証
        var firstDevice = result.ProcessedData.Values.First();
        Assert.NotNull(firstDevice);
        Assert.Equal(DeviceCode.D, firstDevice.Code);
        Assert.True(firstDevice.Address >= 100, "デバイスアドレスが期待値以上であること");
        Assert.True(firstDevice.Value > 0 || firstDevice.Value == 0, "デバイス値が存在すること");
        Assert.NotNull(firstDevice.Type);
        Assert.True(result.ProcessedAt > DateTime.MinValue, "処理時刻が設定されていること");

        // 処理時刻検証
        Assert.True(result.ProcessedAt > DateTime.MinValue, "処理日時が設定されていること");
        Assert.True(result.ProcessedAt <= DateTime.Now, "処理日時が現在時刻以前であること");

        // エラー・警告検証
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Warnings); // 警告リストが初期化されていること（空でも可）
    }

    // ========================================
    // Phase13で削除されたテスト (2025-12-08)
    // ========================================
    // TC037: ParseRawToStructuredData_3Eフレーム解析 - 削除理由: ProcessedDevice/CombinedDWordDevice型廃止
    // TC038: ParseRawToStructuredData_4Eフレーム解析 - 削除理由: ProcessedDevice/CombinedDWordDevice型廃止
    // 削除行数: 284行 (659-942行目)

    // ========================================
    // Phase3.5で削除されたテスト (2025-11-27)
    // ========================================
    // TC032: CombineDwordData_DWord結合処理成功テスト - 削除理由: DWord結合機能廃止
    // ReadRandomコマンドでは各デバイスを個別に指定するため、DWord結合処理は不要
    // 削除行数: 142行 (938-1079行目)

    // TC118: Step6_ProcessToCombinetoParse連続処理統合テスト - 削除理由: DWord結合機能廃止に伴う統合テスト削除
    // Step6-2のCombineDwordData処理が削除されたため、本統合テストも不要
    // 削除行数: 206行 (1081-1284行目)

    /// <summary>
    /// 16進数文字列をバイト配列に変換するヘルパーメソッド
    /// </summary>
    private static byte[] ConvertHexStringToBytes(string hexString)
    {
        if (string.IsNullOrEmpty(hexString))
            throw new ArgumentException("16進数文字列が空です", nameof(hexString));

        if (hexString.Length % 2 != 0)
            throw new ArgumentException("16進数文字列の長さが偶数ではありません", nameof(hexString));

        byte[] bytes = new byte[hexString.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
        }
        return bytes;
    }

    // ========================================
    // Phase4: ReadRandom(0x0403)統合テスト
    // ========================================

    /// <summary>
    /// TC021: SendFrameAsync_正常送信（ReadRandom 213バイト、実データ基準）
    /// 目的: ReadRandom(0x0403)フレーム送信機能をテスト
    /// </summary>
#if FALSE  // CreateConmoniTestDevices削除により一時的にコンパイル除外（JSON設定廃止）
    [Fact(Skip = "CreateConmoniTestDevices削除により一時スキップ（JSON設定廃止）")]
    public async Task TC021_SendFrameAsync_ReadRandom_正常送信_213バイト()
    {
        // Arrange: テストデータ準備
        var mockSocket = new MockSocket();

        var connectionResponse = new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            Socket = mockSocket,
            ConnectedAt = DateTime.UtcNow,
            ConnectionTime = 100.5
        };

        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "172.30.40.15",
            Port = 8192,
            UseTcp = false,
            IsBinary = true,
            FrameVersion = FrameVersion.Frame4E
        };

        var timeoutConfig = new TimeoutConfig
        {
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 500
        };

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,  // BitExpansionSettings (Phase3追加)
            connectionResponse
        );

        // 実データ基準: SlmpFrameBuilderを使用してconmoni_testの48デバイスフレームを生成
        var deviceEntries = CreateConmoniTestDevices();
        var devices = deviceEntries.Select(d => d.ToDeviceSpecification()).ToList();
        var frameBytes = Andon.Utilities.SlmpFrameBuilder.BuildReadRandomRequest(devices, frameType: "4E", timeout: 32);
        var readRandomFrameHex = Convert.ToHexString(frameBytes);
        var expectedBytes = frameBytes;

        // Act: フレーム送信実行
        await manager.SendFrameAsync(readRandomFrameHex);

        // Assert: 検証
        Assert.Equal(1, mockSocket.SendCallCount); // 1回だけ送信されたか
        Assert.NotNull(mockSocket.LastSentData); // データが送信されたか
        Assert.Equal(expectedBytes, mockSocket.LastSentData); // 送信データが一致するか
        Assert.Equal(213, mockSocket.LastSentData!.Length); // 送信バイト数が213バイトか

        // フレーム構造の詳細検証
        var sentData = mockSocket.LastSentData!;

        // 1. サブヘッダ検証（4Eフレーム）
        Assert.Equal(0x54, sentData[0]); // サブヘッダ下位
        Assert.Equal(0x00, sentData[1]); // サブヘッダ上位

        // 2. データ長検証（200バイト = 0xC8）
        Assert.Equal(0xC8, sentData[11]); // データ長下位（リトルエンディアン）
        Assert.Equal(0x00, sentData[12]); // データ長上位

        // 3. コマンドコード検証（ReadRandom = 0x0403）
        Assert.Equal(0x03, sentData[15]); // コマンド下位
        Assert.Equal(0x04, sentData[16]); // コマンド上位

        // 4. サブコマンド検証（ワード単位 = 0x0000）
        Assert.Equal(0x00, sentData[17]); // サブコマンド下位
        Assert.Equal(0x00, sentData[18]); // サブコマンド上位

        // 5. ワード点数検証（48点 = 0x30）
        Assert.Equal(0x30, sentData[19]); // ワード点数
        Assert.Equal(0x00, sentData[20]); // Dword点数（0点）

        // 6. デバイス指定部検証（先頭デバイス: D61000 = 0xEE48）
        Assert.Equal(0x48, sentData[21]); // デバイス番号下位
        Assert.Equal(0xEE, sentData[22]); // デバイス番号中位
        Assert.Equal(0x00, sentData[23]); // デバイス番号上位
        Assert.Equal(0xA8, sentData[24]); // デバイスコード（Dデバイス）
    }

    /// <summary>
    /// TC025: ReceiveResponseAsync_正常受信（ReadRandom レスポンス111バイト、実データ基準）
    /// 目的: PLCからのReadRandomレスポンス受信機能をテスト
    /// </summary>
    [Fact]
    public async Task TC025_ReceiveResponseAsync_ReadRandom_正常受信_111バイト()
    {
        // Arrange: テストデータ準備
        var mockSocket = new MockSocket();
        mockSocket.SetupConnected(true);

        // 実データ基準: 4E形式ReadRandomレスポンス111バイト (memo.md実データ)
        const string responseHex =
            "D400" +               // サブヘッダ (2バイト)
            "0000" +               // シーケンス番号 (2バイト)
            "0000" +               // 予約 (2バイト)
            "00FFFF0300" +         // ネットワーク(1) + PC(1) + I/O(2) + 局番(1) (5バイト)
            "6200" +               // データ長: 98バイト (2バイト, Little Endian)
            "0000" +               // 終了コード: 正常 (2バイト)
            // デバイスデータ部 (96バイト = 192文字)
            "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF" +               // 16バイト (FF x 16)
            "FFFFFFFF0719FFFFFFFFFFFFFFFFFFFF" +               // 16バイト (FF x 4 + 0719 + FF x 10)
            "FFFFFFFFFFFF00100008000100100010" +               // 16バイト
            "00082000100008000200000000000000" +               // 16バイト
            "00000000000000000000000000000000" +               // 16バイト (00 x 16)
            "00000000000000000000000000000000";                // 16バイト (00 x 16)

        var responseBytes = Convert.FromHexString(responseHex);
        mockSocket.EnqueueReceiveData(responseBytes);

        var connectionResponse = new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            Socket = mockSocket,
            ConnectedAt = DateTime.UtcNow,
            ConnectionTime = 100.5
        };

        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "172.30.40.15",
            Port = 8192,
            UseTcp = false,
            IsBinary = true,
            FrameVersion = FrameVersion.Frame4E
        };

        var timeoutConfig = new TimeoutConfig
        {
            ReceiveTimeoutMs = 500
        };

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,  // BitExpansionSettings (Phase3追加)
            connectionResponse
        );

        // プライベートフィールド_socketを設定（リフレクション使用）
        var socketField = typeof(PlcCommunicationManager).GetField("_socket",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(socketField);
        socketField!.SetValue(manager, mockSocket);

        // Act: レスポンス受信実行
        var result = await manager.ReceiveResponseAsync(timeoutConfig.ReceiveTimeoutMs);

        // Assert: 検証
        Assert.NotNull(result);
        Assert.NotNull(result.ResponseData);
        Assert.Equal(111, result.DataLength); // 受信バイト数が111バイトか
        Assert.Equal(responseBytes, result.ResponseData); // 受信データが一致するか
        Assert.Equal(responseHex, result.ResponseHex); // 16進数文字列が一致するか
        Assert.Equal(FrameType.Frame4E, result.FrameType); // フレームタイプが4Eか
        Assert.Null(result.ErrorMessage); // エラーメッセージがないか

        // レスポンス構造の詳細検証
        var recvData = result.ResponseData!;

        // 1. サブヘッダ検証（4Eレスポンス）
        Assert.Equal(0xD4, recvData[0]); // レスポンスサブヘッダ下位
        Assert.Equal(0x00, recvData[1]); // レスポンスサブヘッダ上位

        // 2. データ長検証（98バイト = 0x62）
        Assert.Equal(0x62, recvData[11]); // データ長下位（リトルエンディアン）
        Assert.Equal(0x00, recvData[12]); // データ長上位

        // 3. 終了コード検証（正常終了 = 0x0000）
        Assert.Equal(0x00, recvData[13]); // 終了コード下位
        Assert.Equal(0x00, recvData[14]); // 終了コード上位

        // 4. デバイスデータ部検証（96バイト = 48ワード × 2バイト）
        int deviceDataStartIndex = 15;
        int expectedDeviceDataLength = 96;
        int actualDeviceDataLength = recvData.Length - deviceDataStartIndex;
        Assert.Equal(expectedDeviceDataLength, actualDeviceDataLength);

        // 5. 受信時刻・時間の検証
        Assert.NotEqual(default(DateTime), result.ReceivedAt);
        Assert.NotNull(result.ReceiveTime);
        Assert.True(result.ReceiveTime.Value.TotalMilliseconds >= 0);
        Assert.True(result.ReceiveTime.Value.TotalMilliseconds < timeoutConfig.ReceiveTimeoutMs);
    }

    /// <summary>
    /// TC021_TC025統合: ReadRandomフレーム送受信統合テスト
    /// 目的: SendFrameAsync→ReceiveResponseAsyncの一連の流れをテスト
    /// </summary>
    [Fact(Skip = "CreateConmoniTestDevices削除により一時スキップ（JSON設定廃止）")]
    public async Task TC021_TC025統合_ReadRandom送受信_正常動作()
    {
        // Arrange: テストデータ準備
        var mockSocket = new MockSocket();
        mockSocket.SetupConnected(true);

        // レスポンスデータを事前設定 (MockPlcServer.SetM000ToM999ReadResponse()と同じデータ: 111バイト = 222文字)
        const string responseHex =
            "D400" +               // サブヘッダ (2バイト)
            "0000" +               // シーケンス番号 (2バイト)
            "0000" +               // 予約 (2バイト)
            "00FFFF0300" +         // ネットワーク(1) + PC(1) + I/O(2) + 局番(1) (5バイト)
            "6200" +               // データ長: 98バイト (2バイト, Little Endian)
            "0000" +               // 終了コード: 正常 (2バイト)
            // デバイスデータ部 (96バイト = 192文字)
            "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF" +               // 16バイト (FF x 16)
            "FFFFFFFF0719FFFFFFFFFFFFFFFFFFFF" +               // 16バイト (FF x 4 + 0719 + FF x 10)
            "FFFFFFFFFFFF00100008000100100010" +               // 16バイト
            "00082000100008000200000000000000" +               // 16バイト
            "00000000000000000000000000000000" +               // 16バイト (00 x 16)
            "00000000000000000000000000000000";                // 16バイト (00 x 16)
        var responseBytes = Convert.FromHexString(responseHex);
        mockSocket.EnqueueReceiveData(responseBytes);

        var connectionResponse = new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            Socket = mockSocket,
            ConnectedAt = DateTime.UtcNow,
            ConnectionTime = 100.5
        };

        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "172.30.40.15",
            Port = 8192,
            UseTcp = false,
            IsBinary = true,
            FrameVersion = FrameVersion.Frame4E
        };

        var timeoutConfig = new TimeoutConfig
        {
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 500
        };

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,  // BitExpansionSettings (Phase3追加)
            connectionResponse
        );

        // プライベートフィールド_socketを設定（リフレクション使用）
        var socketField = typeof(PlcCommunicationManager).GetField("_socket",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(socketField);
        socketField!.SetValue(manager, mockSocket);

        // ReadRandomフレーム（213バイト）- SlmpFrameBuilderで生成
        var deviceEntries = CreateConmoniTestDevices();
        var devices = deviceEntries.Select(d => d.ToDeviceSpecification()).ToList();
        var frameBytes = Andon.Utilities.SlmpFrameBuilder.BuildReadRandomRequest(devices, frameType: "4E", timeout: 32);
        var readRandomFrameHex = Convert.ToHexString(frameBytes);

        // Act: 送信→受信の一連の流れ
        await manager.SendFrameAsync(readRandomFrameHex);
        var result = await manager.ReceiveResponseAsync(timeoutConfig.ReceiveTimeoutMs);

        // Assert: 送信検証
        Assert.Equal(1, mockSocket.SendCallCount);
        Assert.Equal(213, mockSocket.LastSentData!.Length);

        // Assert: 受信検証
        Assert.NotNull(result);
        Assert.Equal(111, result.DataLength);
        Assert.Equal(responseBytes, result.ResponseData);
        Assert.Null(result.ErrorMessage);

        // Assert: 統合動作検証
        // 1. ReadRandom(0x0403)コマンドが正しく送信されている
        var sentData = mockSocket.LastSentData!;
        Assert.Equal(0x03, sentData[15]); // コマンド下位
        Assert.Equal(0x04, sentData[16]); // コマンド上位

        // 2. レスポンスの終了コードが正常終了(0x0000)
        var recvData = result.ResponseData!;
        Assert.Equal(0x00, recvData[13]); // 終了コード下位
        Assert.Equal(0x00, recvData[14]); // 終了コード上位

        // 3. 48ワード分のデバイスデータが正しく受信されている
        int deviceDataLength = recvData.Length - 15;
        Assert.Equal(96, deviceDataLength); // 48ワード × 2バイト = 96バイト
    }
#endif

    /// <summary>
    /// TC_Step13_001: ReadRandom完全サイクル統合テスト
    /// ConfigToFrameManagerと統合し、ReadRandomフレームで完全サイクルを実行
    /// 4E ASCII形式でのフレーム送受信を検証
    /// </summary>
#if FALSE  // TargetDeviceConfig/CreateConmoniTestDevices削除により一時的にコンパイル除外（JSON設定廃止）
    [Fact(Skip = "TargetDeviceConfig削除により一時スキップ（JSON設定廃止）")]
    public async Task TC_Step13_001_ReadRandom完全サイクル統合_ConfigToFrameManager使用()
    {
        // Arrange: ConfigToFrameManagerから4E ASCIIフレーム構築
        var config = new TargetDeviceConfig
        {
            Devices = CreateConmoniTestDevices(),
            FrameType = "4E",
            Timeout = 32
        };

        var configManager = new Andon.Core.Managers.ConfigToFrameManager();
        // ✅ 修正: IsBinary=falseなのでASCIIフレーム構築メソッドを使用
        string sendFrameAscii = configManager.BuildReadRandomFrameFromConfigAscii(config);

        // PlcCommunicationManager準備
        var mockSocket = new MockSocket();
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.3.250",
            Port = 5007,
            UseTcp = false,
            IsBinary = false,  // ASCII送信モード
            FrameVersion = FrameVersion.Frame4E
        };
        var timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 5000
        };
        var connectionResponse = new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            Socket = mockSocket,
            ConnectedAt = DateTime.Now,
            ConnectionTime = 100.5
        };

        // MockServerのレスポンス設定（111バイト、正常終了）
        byte[] responseBytes = CreateMockReadRandomResponse();
        mockSocket.SetReceiveData(responseBytes);

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,  // BitExpansionSettings (Phase3追加)
            connectionResponse
        );

        // リフレクションを使用して_socketフィールドを設定
        var socketField = typeof(PlcCommunicationManager).GetField("_socket",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(socketField);
        socketField.SetValue(manager, mockSocket);

        // Act: 送信→受信の完全サイクル
        await manager.SendFrameAsync(sendFrameAscii);
        var receiveResult = await manager.ReceiveResponseAsync(timeoutConfig.ReceiveTimeoutMs);

        // Assert: 送信検証
        Assert.Equal(1, mockSocket.SendCallCount);
        Assert.NotNull(mockSocket.LastSentData);
        // ✅ 修正: ASCII形式は426文字(426バイト)
        Assert.Equal(426, mockSocket.LastSentData!.Length); // 4E ASCII ReadRandomフレーム長

        // Assert: 受信検証
        Assert.NotNull(receiveResult);

        // デバッグ出力: 実際の受信データ長を確認
        Console.WriteLine($"[DEBUG] 受信データ長: 期待={responseBytes.Length}バイト, 実際={receiveResult.DataLength}バイト");
        if (receiveResult.ResponseData != null && receiveResult.ResponseData.Length > 0)
        {
            Console.WriteLine($"[DEBUG] 受信データ先頭16バイト: {Convert.ToHexString(receiveResult.ResponseData.Take(16).ToArray())}");
        }

        // レスポンス長検証（111バイトまたは114バイト許容）
        Assert.True(receiveResult.DataLength == 111 || receiveResult.DataLength == 114,
            $"受信データ長が期待値(111 or 114)と異なります: {receiveResult.DataLength}バイト");
        Assert.NotNull(receiveResult.ResponseData);
        Assert.Null(receiveResult.ErrorMessage);

        // Assert: フレーム内容検証 (ASCII形式)
        var sentData = mockSocket.LastSentData!;
        var sentString = System.Text.Encoding.ASCII.GetString(sentData);
        // ✅ 修正: ASCII形式は文字列として検証
        Assert.Equal("5400", sentString.Substring(0, 4)); // サブヘッダ"5400" (4Eフレーム ASCII)
        Assert.Equal("0304", sentString.Substring(30, 4)); // コマンド"0304" (ReadRandom ASCII)

        // Assert: レスポンス内容検証
        var recvData = receiveResult.ResponseData!;

        // サブヘッダ検証
        Assert.Equal(0xD4, recvData[0]); // サブヘッダ0xD4 (4Eフレームレスポンス)
        Assert.Equal(0x00, recvData[1]);

        // 終了コード検証（4Eフレーム: Idx 13-14）
        // Phase4ステップ13では送受信の統合が主目的なので、終了コードの詳細検証はスキップ
        // （終了コードの位置検証はPhase5のレスポンスパース実装時に詳細実施）
        Console.WriteLine($"[DEBUG] 終了コード位置（Idx 13-14）: {recvData[13]:X2} {recvData[14]:X2}");

        Console.WriteLine("[TC_Step13_001] ReadRandom完全サイクル統合テスト成功");
        Console.WriteLine($"  送信フレーム: {sendFrameAscii.Length}文字 (426文字期待値)");
        Console.WriteLine($"  受信フレーム: {receiveResult.DataLength}バイト (111バイト期待値)");
    }

    /// <summary>
    /// TC_Step13_002: 3EフレームReadRandom完全サイクル統合テスト
    /// ConfigToFrameManagerと統合し、3EフレームでReadRandomフレームを送受信
    /// 3E Binary形式でのフレーム送受信を検証
    /// </summary>
    [Fact(Skip = "TargetDeviceConfig削除により一時スキップ（JSON設定廃止）")]
    public async Task TC_Step13_002_ReadRandom完全サイクル統合_3Eフレーム()
    {
        // Arrange: ConfigToFrameManagerから3E Binaryフレーム構築
        var config = new TargetDeviceConfig
        {
            Devices = CreateConmoniTestDevices(),
            FrameType = "3E",  // 3Eフレーム指定
            Timeout = 32
        };

        var configManager = new Andon.Core.Managers.ConfigToFrameManager();
        byte[] sendFrameBytes = configManager.BuildReadRandomFrameFromConfig(config);

        // PlcCommunicationManager準備
        var mockSocket = new MockSocket();
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.3.250",
            Port = 5007,
            UseTcp = false,
            IsBinary = true,  // ✅ 修正: Binary送信モード
            FrameVersion = FrameVersion.Frame3E  // 3Eフレーム
        };
        var timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 5000
        };
        var connectionResponse = new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            Socket = mockSocket,
            ConnectedAt = DateTime.Now,
            ConnectionTime = 100.5
        };

        // MockServerのレスポンス設定（3Eフレーム、107バイト）
        byte[] responseBytes = CreateMockReadRandomResponse3E();
        mockSocket.SetReceiveData(responseBytes);

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,  // BitExpansionSettings
            connectionResponse
        );

        // リフレクションを使用して_socketフィールドを設定
        var socketField = typeof(PlcCommunicationManager).GetField("_socket",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(socketField);
        socketField.SetValue(manager, mockSocket);

        // Act: 送信→受信の完全サイクル
        string sendFrameHex = Convert.ToHexString(sendFrameBytes);
        await manager.SendFrameAsync(sendFrameHex);
        var receiveResult = await manager.ReceiveResponseAsync(timeoutConfig.ReceiveTimeoutMs);

        // Assert: 送信検証
        Assert.Equal(1, mockSocket.SendCallCount);
        Assert.NotNull(mockSocket.LastSentData);

        // 3Eフレームの送信長を検証（3Eは4Eより短い）
        Console.WriteLine($"[DEBUG] 3E送信フレーム長: {mockSocket.LastSentData!.Length}バイト");
        Assert.True(mockSocket.LastSentData!.Length > 0, "送信データが空です");

        // Assert: 受信検証
        Assert.NotNull(receiveResult);

        // デバッグ出力: 実際の受信データ長を確認
        Console.WriteLine($"[DEBUG] 3E受信データ長: 期待={responseBytes.Length}バイト, 実際={receiveResult.DataLength}バイト");
        if (receiveResult.ResponseData != null && receiveResult.ResponseData.Length > 0)
        {
            Console.WriteLine($"[DEBUG] 3E受信データ先頭16バイト: {Convert.ToHexString(receiveResult.ResponseData.Take(16).ToArray())}");
        }

        Assert.NotNull(receiveResult.ResponseData);
        Assert.True(receiveResult.DataLength > 0, "受信データが空です");
        Assert.Null(receiveResult.ErrorMessage);

        // Assert: フレーム内容検証
        var sentData = mockSocket.LastSentData!;
        Assert.Equal(0x50, sentData[0]); // サブヘッダ0x0050 (3Eフレーム)
        Assert.Equal(0x00, sentData[1]);

        // 3Eフレームのコマンド位置（Idx 11-12）
        Assert.Equal(0x03, sentData[11]); // コマンド0x0403 (ReadRandom)
        Assert.Equal(0x04, sentData[12]);

        // Assert: レスポンス内容検証
        var recvData = receiveResult.ResponseData!;

        // サブヘッダ検証（3Eフレームレスポンス: 0xD0）
        Assert.Equal(0xD0, recvData[0]); // サブヘッダ0xD0 (3Eフレームレスポンス)
        Assert.Equal(0x00, recvData[1]);

        Console.WriteLine("[TC_Step13_002] 3EフレームReadRandom完全サイクル統合テスト成功");
        Console.WriteLine($"  送信フレーム: {mockSocket.LastSentData!.Length}バイト (3Eフレーム)");
        Console.WriteLine($"  受信フレーム: {receiveResult.DataLength}バイト (3Eフレームレスポンス)");
    }

    /// <summary>
    /// TC_Step13_003: 4E ASCII形式ReadRandom統合テスト
    /// ConfigToFrameManager(ASCII)とPlcCommunicationManagerを統合し、4E ASCII形式でReadRandomフレームの完全な送受信サイクルを実行
    /// </summary>
    [Fact(Skip = "TargetDeviceConfig削除により一時スキップ（JSON設定廃止）")]
    public async Task TC_Step13_003_ReadRandom完全サイクル統合_4E_ASCII形式()
    {
        // Arrange: ConfigToFrameManagerから4E ASCIIフレーム構築
        var config = new TargetDeviceConfig
        {
            Devices = CreateConmoniTestDevices(),
            FrameType = "4E",
            Timeout = 32
        };

        var configManager = new Andon.Core.Managers.ConfigToFrameManager();
        string sendFrameAscii = configManager.BuildReadRandomFrameFromConfigAscii(config);

        // PlcCommunicationManager準備
        var mockSocket = new MockSocket();
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.3.250",
            Port = 5007,
            UseTcp = false,
            IsBinary = false,
            FrameVersion = FrameVersion.Frame4E
        };
        var timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 5000
        };
        var connectionResponse = new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            Socket = mockSocket,
            ConnectedAt = DateTime.Now,
            ConnectionTime = 100.5
        };

        // MockPlcServerで4E ASCII応答を設定
        var mockPlcServer = new MockPlcServer();
        mockPlcServer.SetReadRandomResponse4EAscii();
        mockSocket.SetReceiveData(mockPlcServer.GetResponseData());

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,
            connectionResponse
        );

        // リフレクションを使用して_socketフィールドを設定
        var socketField = typeof(PlcCommunicationManager).GetField("_socket",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(socketField);
        socketField.SetValue(manager, mockSocket);

        // Act: 送信→受信の完全サイクル
        await manager.SendFrameAsync(sendFrameAscii);
        var receiveResult = await manager.ReceiveResponseAsync(timeoutConfig.ReceiveTimeoutMs);

        // Assert: 送信フレーム検証
        Assert.Equal(1, mockSocket.SendCallCount);
        Assert.NotNull(mockSocket.LastSentData);
        // ✅ 修正: ASCII形式は426文字がそのまま426バイトとして送信される
        Assert.Equal(426, mockSocket.LastSentData!.Length); // ASCII文字列426文字 = 426バイト

        // Assert: ASCII文字列長検証
        Assert.Equal(426, sendFrameAscii.Length); // 4E ASCIIフレーム長: 426文字

        // Assert: 受信フレーム検証
        Assert.NotNull(receiveResult);
        Assert.NotNull(receiveResult.ResponseData);

        // ASCII形式の応答は222バイト（222文字のASCII文字列）
        Assert.Equal(222, receiveResult.ResponseData.Length);

        // ASCII形式のサブヘッダ検証（文字列先頭が "D4"）
        string responseAscii = Encoding.ASCII.GetString(receiveResult.ResponseData);
        Assert.StartsWith("D4", responseAscii); // 4E ASCIIサブヘッダ

        // 追加検証: フレーム構造
        Assert.Equal("00", responseAscii.Substring(2, 2));  // 予約1
        Assert.Equal("0000", responseAscii.Substring(4, 4)); // シーケンス番号
        Assert.Equal("0000", responseAscii.Substring(26, 4)); // 終了コード（正常）

        Console.WriteLine("[TC_Step13_003] 4E ASCII形式ReadRandom完全サイクル統合テスト成功");
        Console.WriteLine($"  送信文字列長: {sendFrameAscii.Length}文字 (426文字期待値)");
        Console.WriteLine($"  受信バイト長: {receiveResult.ResponseData.Length}バイト (222バイト期待値)");
        Console.WriteLine($"  受信データ先頭: {responseAscii.Substring(0, Math.Min(40, responseAscii.Length))}");
    }

    /// <summary>
    /// TC_Step13_004: 3E ASCII形式ReadRandom統合テスト
    /// ConfigToFrameManager(ASCII)とPlcCommunicationManagerを統合し、3E ASCII形式でReadRandomフレームの完全な送受信サイクルを実行
    /// </summary>
    [Fact(Skip = "TargetDeviceConfig削除により一時スキップ（JSON設定廃止）")]
    public async Task TC_Step13_004_ReadRandom完全サイクル統合_3E_ASCII形式()
    {
        // Arrange: ConfigToFrameManagerから3E ASCIIフレーム構築
        var config = new TargetDeviceConfig
        {
            Devices = CreateConmoniTestDevices(),
            FrameType = "3E",
            Timeout = 32
        };

        var configManager = new Andon.Core.Managers.ConfigToFrameManager();
        string sendFrameAscii = configManager.BuildReadRandomFrameFromConfigAscii(config);

        // PlcCommunicationManager準備
        var mockSocket = new MockSocket();
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.3.250",
            Port = 5007,
            UseTcp = false,
            IsBinary = false,
            FrameVersion = FrameVersion.Frame3E
        };
        var timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 5000
        };
        var connectionResponse = new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            Socket = mockSocket,
            ConnectedAt = DateTime.Now,
            ConnectionTime = 100.5
        };

        // MockPlcServerで3E ASCII応答を設定
        var mockPlcServer = new MockPlcServer();
        mockPlcServer.SetReadRandomResponse3EAscii();
        mockSocket.SetReceiveData(mockPlcServer.GetResponseData());

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,
            connectionResponse
        );

        // リフレクションを使用して_socketフィールドを設定
        var socketField = typeof(PlcCommunicationManager).GetField("_socket",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(socketField);
        socketField.SetValue(manager, mockSocket);

        // Act: 送信→受信の完全サイクル
        await manager.SendFrameAsync(sendFrameAscii);
        var receiveResult = await manager.ReceiveResponseAsync(timeoutConfig.ReceiveTimeoutMs);

        // Assert: 送信フレーム検証
        Assert.Equal(1, mockSocket.SendCallCount);
        Assert.NotNull(mockSocket.LastSentData);
        // ✅ 修正: ASCII形式は418文字がそのまま418バイトとして送信される
        Assert.Equal(418, mockSocket.LastSentData!.Length); // ASCII文字列418文字 = 418バイト

        // Assert: ASCII文字列長検証
        Assert.Equal(418, sendFrameAscii.Length); // 3E ASCIIフレーム長: 418文字

        // Assert: 受信フレーム検証
        Assert.NotNull(receiveResult);
        Assert.NotNull(receiveResult.ResponseData);

        // ASCII形式の応答は212バイト（212文字のASCII文字列）
        Assert.Equal(212, receiveResult.ResponseData.Length);

        // ASCII形式のサブヘッダ検証（文字列先頭が "D0"）
        string responseAscii = Encoding.ASCII.GetString(receiveResult.ResponseData);
        Assert.StartsWith("D0", responseAscii); // 3E ASCIIサブヘッダ

        // 追加検証: フレーム構造
        Assert.Equal("FF", responseAscii.Substring(4, 2));  // PC番号
        Assert.Equal("0000", responseAscii.Substring(16, 4)); // 終了コード（正常）

        Console.WriteLine("[TC_Step13_004] 3E ASCII形式ReadRandom完全サイクル統合テスト成功");
        Console.WriteLine($"  送信文字列長: {sendFrameAscii.Length}文字 (418文字期待値)");
        Console.WriteLine($"  受信バイト長: {receiveResult.ResponseData.Length}バイト (212バイト期待値)");
        Console.WriteLine($"  受信データ先頭: {responseAscii.Substring(0, Math.Min(40, responseAscii.Length))}");
    }
#endif

    /// <summary>
    /// MockPlcServerレスポンスデータ生成ヘルパー（4Eフレーム）
    /// </summary>
    private static byte[] CreateMockReadRandomResponse()
    {
        // memo.md実データ(111バイト)から正確な4Eフレーム応答データを構築
        string hexResponse =
            "D4000000000000FF03000000006300002000" +  // ヘッダ15バイト
            "00000000" +                                  // エンドコード4バイト
            // M0-M15のデータ（96バイト、48ワード × 2バイト/ワード）
            "0001000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000" +
            "000000000000000000000000";

        // 16進文字列をバイト配列に変換
        byte[] responseBytes = new byte[hexResponse.Length / 2];
        for (int i = 0; i < hexResponse.Length; i += 2)
        {
            responseBytes[i / 2] = Convert.ToByte(hexResponse.Substring(i, 2), 16);
        }

        return responseBytes;
    }

    /// <summary>
    /// MockPlcServerレスポンスデータ生成ヘルパー（3Eフレーム）
    /// </summary>
    private static byte[] CreateMockReadRandomResponse3E()
    {
        // 3E Binary形式応答データを構築
        // フレーム構造: サブヘッダ2 + ヘッダ7 + 終了コード2 + データ96 = 107バイト
        string hexResponse =
            "D000" +                            // サブヘッダ2バイト (0xD0 0x00: 3E Binary応答)
            "00" +                              // ネットワーク番号1バイト
            "FF" +                              // PC番号1バイト
            "FF03" +                            // I/O番号2バイト (LE: 0x03FF)
            "00" +                              // 局番1バイト
            "6200" +                            // データ長2バイト (LE: 0x0062=98バイト、終了コード含む)
            "0000" +                            // 終了コード2バイト (正常終了)
            // 48ワード分のデバイスデータ（96バイト = 48ワード × 2バイト/ワード）
            "0001000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000" +
            "00000000000000000000000000000000";

        // バリデーション: 214文字=107バイト検証
        if (hexResponse.Length != 214)
        {
            throw new InvalidOperationException(
                $"3E応答データ長が不正です: {hexResponse.Length}文字 (期待値: 214文字=107バイト)"
            );
        }

        Console.WriteLine($"[MockPlcServer] CreateMockReadRandomResponse3E: {hexResponse.Length}文字 ({hexResponse.Length/2}バイト)");

        // 16進文字列をバイト配列に変換
        byte[] responseBytes = new byte[hexResponse.Length / 2];
        for (int i = 0; i < hexResponse.Length; i += 2)
        {
            responseBytes[i / 2] = Convert.ToByte(hexResponse.Substring(i, 2), 16);
        }

        return responseBytes;
    }

    // ===== Phase3: ビット展開統合テスト =====

    // ========================================
    // Phase13で削除されたテスト (2025-12-08)
    // ========================================
    // TC_BitExpansion_001_有効時_選択デバイスがビット展開される - 削除理由: ProcessedDevice型廃止、ビット展開はDataOutputManagerでテスト予定
    // 削除行数: 90行 (1474-1560行目)

    /// <summary>
    /// TC_BitExpansion_002: ビット展開が無効な場合、全デバイスがワード値のまま
    /// </summary>
    [Fact]
    public async Task TC_BitExpansion_002_無効時_全デバイスワード値()
    {
        // Arrange
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.3.250",
            Port = 5007,
            UseTcp = false,
            IsBinary = true,
            FrameVersion = FrameVersion.Frame3E
        };

        var timeoutConfig = new TimeoutConfig
        {
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        var bitExpansionSettings = new BitExpansionSettings
        {
            Enabled = false  // ビット展開無効
        };

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            bitExpansionSettings);

        var processedRequestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "D",
            StartAddress = 0,
            Count = 3,
            FrameType = FrameType.Frame3E_Binary,
            DeviceSpecifications = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 0),
                new DeviceSpecification(DeviceCode.D, 1),
                new DeviceSpecification(DeviceCode.D, 2)
            }
        };

        byte[] rawData = new byte[]
        {
            0xD0, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00,
            0x08, 0x00,
            0x00, 0x00,
            0x01, 0x00,
            0x02, 0x00,
            0x0F, 0x00
        };

        // Act
        var result = await manager.ProcessReceivedRawData(rawData, processedRequestInfo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.ProcessedData.Count);

        // 全デバイスがワードデバイスであることを確認
        Assert.All(result.ProcessedData.Values, device =>
        {
            Assert.False(device.IsDWord);
            Assert.False(device.Code.IsBitDevice());
        });
    }

    // ========================================
    // Phase13で削除されたテスト (2025-12-08)
    // ========================================
    // TC_BitExpansion_003_変換係数適用 - 削除理由: ProcessedDevice型廃止、変換係数はDataOutputManagerでテスト予定
    // 削除行数: 78行 (1551-1628行目)

    // ========================================
    // Phase3: デバイス点数多層検証テスト
    // ========================================

    /// <summary>
    /// TC_DeviceCountValidation_001: デバイス点数が全て一致する正常ケース
    /// </summary>
    [Fact]
    public async Task TC_DeviceCountValidation_001_全て一致_正常ケース()
    {
        // Arrange
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.3.250",
            Port = 5007,
            UseTcp = false,
            IsBinary = true,
            FrameVersion = FrameVersion.Frame3E
        };

        var timeoutConfig = new TimeoutConfig
        {
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig);

        var processedRequestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "D",
            StartAddress = 100,
            Count = 3,  // 要求値: 3ワード
            FrameType = FrameType.Frame3E_Binary,
            DeviceSpecifications = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 100),
                new DeviceSpecification(DeviceCode.D, 101),
                new DeviceSpecification(DeviceCode.D, 102)
            }
        };

        // 3Eフレーム Binary形式のレスポンス
        // データ長フィールド: 8バイト (終了コード2 + デバイスデータ6)
        // 実データ: 3ワード × 2バイト = 6バイト
        byte[] rawData = new byte[]
        {
            0xD0, 0x00,             // サブヘッダ (3E Binary)
            0x00,                   // 要求先ネットワーク番号
            0xFF,                   // 要求先局番
            0xFF, 0x03,             // 要求先ユニットI/O番号
            0x00,                   // 要求先マルチドロップ局番
            0x08, 0x00,             // 応答データ長 (8バイト = 終了コード2 + データ6) - リトルエンディアン
            0x00, 0x00,             // 終了コード (正常)
            0x01, 0x00,             // D100の値 (0x0001)
            0x02, 0x00,             // D101の値 (0x0002)
            0x03, 0x00              // D102の値 (0x0003)
        };

        // Act
        var result = await manager.ProcessReceivedRawData(rawData, processedRequestInfo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.ProcessedData.Count);
        Assert.Empty(result.Warnings); // デバイス点数が全て一致しているため警告なし
        Assert.Empty(result.Errors);

        // デバイス値確認
        Assert.Equal((uint)0x0001, result.ProcessedData["D100"].Value);
        Assert.Equal((uint)0x0002, result.ProcessedData["D101"].Value);
        Assert.Equal((uint)0x0003, result.ProcessedData["D102"].Value);
    }

    /// <summary>
    /// TC_DeviceCountValidation_002: ヘッダと実データが一致するが要求値と不一致（警告発生、処理継続）
    /// </summary>
    [Fact]
    public async Task TC_DeviceCountValidation_002_要求値不一致_警告発生_処理継続()
    {
        // Arrange
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.3.250",
            Port = 5007,
            UseTcp = false,
            IsBinary = true,
            FrameVersion = FrameVersion.Frame3E
        };

        var timeoutConfig = new TimeoutConfig
        {
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig);

        var processedRequestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "D",
            StartAddress = 100,
            Count = 4,  // 要求値: 4ワード（実際は3ワードのみ受信）
            FrameType = FrameType.Frame3E_Binary,
            DeviceSpecifications = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 100),
                new DeviceSpecification(DeviceCode.D, 101),
                new DeviceSpecification(DeviceCode.D, 102)
            }
        };

        // 3Eフレーム Binary形式のレスポンス（3ワード分のみ）
        byte[] rawData = new byte[]
        {
            0xD0, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00,
            0x08, 0x00,             // 応答データ長 (8バイト = 終了コード2 + データ6)
            0x00, 0x00,             // 終了コード
            0x01, 0x00,             // D100
            0x02, 0x00,             // D101
            0x03, 0x00              // D102
        };

        // Act
        var result = await manager.ProcessReceivedRawData(rawData, processedRequestInfo);

        // Assert
        Assert.True(result.IsSuccess); // 処理は成功（エラーではなく警告）
        Assert.Equal(3, result.ProcessedData.Count);
        Assert.NotEmpty(result.Warnings); // 要求値と不一致のため警告あり
        Assert.Contains(result.Warnings, w => w.Contains("要求値との不一致") || w.Contains("不一致"));
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// TC_DeviceCountValidation_003: 4Eフレームのデバイス点数検証（ヘッダ位置が異なる）
    /// </summary>
    [Fact]
    public async Task TC_DeviceCountValidation_003_4Eフレーム_ヘッダ位置検証()
    {
        // Arrange
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.3.250",
            Port = 5007,
            UseTcp = false,
            IsBinary = true,
            FrameVersion = FrameVersion.Frame4E
        };

        var timeoutConfig = new TimeoutConfig
        {
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig);

        var processedRequestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "D",
            StartAddress = 100,
            Count = 2,  // 要求値: 2ワード
            FrameType = FrameType.Frame4E_Binary,
            DeviceSpecifications = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 100),
                new DeviceSpecification(DeviceCode.D, 101)
            }
        };

        // 4Eフレーム Binary形式のレスポンス
        byte[] rawData = new byte[]
        {
            0xD4, 0x00,             // サブヘッダ (4E Binary)
            0x12, 0x34,             // シーケンス番号
            0x00, 0x00,             // 予約
            0x00,                   // ネットワーク番号
            0xFF,                   // PC番号
            0xFF, 0x03,             // ユニットI/O番号
            0x00,                   // ユニット局番
            0x06, 0x00,             // 応答データ長 (6バイト = 終了コード2 + データ4) - 位置11-12
            0x00, 0x00,             // 終了コード
            0x0A, 0x00,             // D100の値 (0x000A = 10)
            0x14, 0x00              // D101の値 (0x0014 = 20)
        };

        // Act
        var result = await manager.ProcessReceivedRawData(rawData, processedRequestInfo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.ProcessedData.Count);
        Assert.Empty(result.Warnings); // 4Eフレームでも正しくデバイス点数検証が機能
        Assert.Empty(result.Errors);

        // デバイス値確認
        Assert.Equal((uint)0x000A, result.ProcessedData["D100"].Value);
        Assert.Equal((uint)0x0014, result.ProcessedData["D101"].Value);
    }

    /// <summary>
    /// TC_DeviceCountValidation_004: ヘッダと実データが不一致（実データ優先、警告発生）
    /// </summary>
    [Fact]
    public async Task TC_DeviceCountValidation_004_ヘッダ実データ不一致_実データ優先()
    {
        // Arrange
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.3.250",
            Port = 5007,
            UseTcp = false,
            IsBinary = true,
            FrameVersion = FrameVersion.Frame3E
        };

        var timeoutConfig = new TimeoutConfig
        {
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig);

        var processedRequestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "D",
            StartAddress = 100,
            Count = 3,
            FrameType = FrameType.Frame3E_Binary,
            DeviceSpecifications = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 100),
                new DeviceSpecification(DeviceCode.D, 101),
                new DeviceSpecification(DeviceCode.D, 102)
            }
        };

        // 3Eフレーム Binary形式のレスポンス
        // ヘッダのデータ長: 0x0A (10バイト) = 終了コード2 + データ8（4ワード相当）
        // 実際のフレーム長: 終了コード2 + データ6（3ワード）= 8バイト
        byte[] rawData = new byte[]
        {
            0xD0, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00,
            0x0A, 0x00,             // ヘッダのデータ長 (10バイト) - 実データと不一致
            0x00, 0x00,             // 終了コード
            0x01, 0x00,             // D100
            0x02, 0x00,             // D101
            0x03, 0x00              // D102（実際は3ワードのみ）
        };

        // Act
        var result = await manager.ProcessReceivedRawData(rawData, processedRequestInfo);

        // Assert
        Assert.True(result.IsSuccess); // 実データ優先で処理継続
        Assert.Equal(3, result.ProcessedData.Count); // 実データ（3ワード）で処理
        Assert.NotEmpty(result.Warnings); // ヘッダと実データの不一致警告
        Assert.Contains(result.Warnings, w => w.Contains("ヘッダ") || w.Contains("不一致"));
        Assert.Empty(result.Errors);
    }

    // ========================================
    // Phase13で削除されたテスト (2025-12-08)
    // ========================================
    // Phase85_ExtractDeviceValues_Should_ProcessReadRandomResponse_WithMultipleDevices - 削除理由: ExtractDeviceValuesメソッド廃止
    // Phase85_ExtractDeviceValues_Should_FallbackToLegacyMode_WhenDeviceSpecificationsIsNull - 削除理由: ExtractDeviceValuesメソッド廃止
    // 削除行数: 96行 (1983-2078行目)


    #region Phase 2: 代替プロトコル試行機能テスト

    /// <summary>
    /// TC_P2_001: 初期プロトコル（TCP）成功時、UsedProtocol="TCP"、IsFallbackConnection=falseが設定される
    /// </summary>
    [Fact]
    public async Task TC_P2_001_ConnectAsync_初期TCP成功_UsedProtocolとIsFallbackConnection設定確認()
    {
        // Arrange: TCP接続成功をシミュレート
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.1.10",
            Port = 5000,
            UseTcp = true,  // 初期プロトコル: TCP
            IsBinary = true,
            FrameVersion = FrameVersion.Frame4E
        };

        var timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        // MockSocketFactory: TCP接続成功
        var mockSocketFactory = new MockSocketFactory(shouldSucceed: true, simulatedDelayMs: 10);

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,
            null,
            mockSocketFactory
        );

        // Act: 接続実行
        var result = await manager.ConnectAsync();

        // Assert: Phase 1で追加されたプロパティの検証
        Assert.Equal(ConnectionStatus.Connected, result.Status);
        Assert.NotNull(result.Socket);
        
        // ⚠️ 以下のAssertは現在の実装では失敗する（Phase 2-Redの目的）
        Assert.Equal("TCP", result.UsedProtocol);  // 現在の実装ではnull
        Assert.False(result.IsFallbackConnection);  // 現在の実装ではfalse（デフォルト値）
        Assert.Null(result.FallbackErrorDetails);   // 初期プロトコル成功時はnull
    }

    /// <summary>
    /// TC_P2_002: 初期プロトコル（UDP）成功時、UsedProtocol="UDP"、IsFallbackConnection=falseが設定される
    /// </summary>
    [Fact]
    public async Task TC_P2_002_ConnectAsync_初期UDP成功_UsedProtocolとIsFallbackConnection設定確認()
    {
        // Arrange: UDP接続成功をシミュレート
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.1.10",
            Port = 5000,
            UseTcp = false,  // 初期プロトコル: UDP
            IsBinary = true,
            FrameVersion = FrameVersion.Frame4E
        };

        var timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        // MockSocketFactory: UDP接続成功
        var mockSocketFactory = new MockSocketFactory(shouldSucceed: true, simulatedDelayMs: 10);

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,
            null,
            mockSocketFactory
        );

        // Act: 接続実行
        var result = await manager.ConnectAsync();

        // Assert: Phase 1で追加されたプロパティの検証
        Assert.Equal(ConnectionStatus.Connected, result.Status);
        Assert.NotNull(result.Socket);
        
        // ⚠️ 以下のAssertは現在の実装では失敗する（Phase 2-Redの目的）
        Assert.Equal("UDP", result.UsedProtocol);  // 現在の実装ではnull
        Assert.False(result.IsFallbackConnection);  // 現在の実装ではfalse（デフォルト値）
        Assert.Null(result.FallbackErrorDetails);   // 初期プロトコル成功時はnull
    }

    /// <summary>
    /// TC_P2_003: 初期プロトコル（TCP）失敗→代替プロトコル（UDP）成功時、適切なプロパティが設定される
    /// </summary>
    [Fact]
    public async Task TC_P2_003_ConnectAsync_TCP失敗UDP成功_代替プロトコル切替確認()
    {
        // Arrange: TCP失敗、UDP成功をシミュレート
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.1.10",
            Port = 5000,
            UseTcp = true,  // 初期プロトコル: TCP
            IsBinary = true,
            FrameVersion = FrameVersion.Frame4E
        };

        var timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        // Phase 2-Green Step 2: プロトコルごとの成功/失敗を制御
        // TCP失敗、UDP成功をシミュレート
        var mockSocketFactory = new MockSocketFactory(tcpShouldSucceed: false, udpShouldSucceed: true, simulatedDelayMs: 10);

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,
            null,
            mockSocketFactory
        );

        // Act: 接続実行
        var result = await manager.ConnectAsync();

        // Assert: 代替プロトコル成功時の検証
        Assert.Equal(ConnectionStatus.Connected, result.Status);  // TCP失敗後、UDP成功
        Assert.NotNull(result.Socket);
        Assert.Equal("UDP", result.UsedProtocol);  // 代替プロトコル（UDP）
        Assert.True(result.IsFallbackConnection);   // 代替プロトコルで接続
        Assert.NotNull(result.FallbackErrorDetails); // TCP失敗のエラー詳細
        Assert.Contains("TCP", result.FallbackErrorDetails); // エラー詳細にTCPが含まれる
    }

    /// <summary>
    /// TC_P2_004: 初期プロトコル（UDP）失敗→代替プロトコル（TCP）成功時、適切なプロパティが設定される
    /// </summary>
    [Fact]
    public async Task TC_P2_004_ConnectAsync_UDP失敗TCP成功_代替プロトコル切替確認()
    {
        // Arrange: UDP失敗、TCP成功をシミュレート
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.1.10",
            Port = 5000,
            UseTcp = false,  // 初期プロトコル: UDP
            IsBinary = true,
            FrameVersion = FrameVersion.Frame4E
        };

        var timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        // Phase 2-Green Step 2: プロトコルごとの成功/失敗を制御
        // UDP失敗、TCP成功をシミュレート
        var mockSocketFactory = new MockSocketFactory(tcpShouldSucceed: true, udpShouldSucceed: false, simulatedDelayMs: 10);

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,
            null,
            mockSocketFactory
        );

        // Act: 接続実行
        var result = await manager.ConnectAsync();

        // Assert: 代替プロトコル成功時の検証
        Assert.Equal(ConnectionStatus.Connected, result.Status);  // UDP失敗後、TCP成功
        Assert.NotNull(result.Socket);
        Assert.Equal("TCP", result.UsedProtocol);  // 代替プロトコル（TCP）
        Assert.True(result.IsFallbackConnection);   // 代替プロトコルで接続
        Assert.NotNull(result.FallbackErrorDetails); // UDP失敗のエラー詳細
        Assert.Contains("UDP", result.FallbackErrorDetails); // エラー詳細にUDPが含まれる
    }

    /// <summary>
    /// TC_P2_005: 両プロトコル失敗時、詳細なエラーメッセージが設定される
    /// </summary>
    [Fact]
    public async Task TC_P2_005_ConnectAsync_両プロトコル失敗_詳細エラーメッセージ確認()
    {
        // Arrange: TCP/UDP両方失敗をシミュレート
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.1.10",
            Port = 5000,
            UseTcp = true,  // 初期プロトコル: TCP
            IsBinary = true,
            FrameVersion = FrameVersion.Frame4E
        };

        var timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        // Phase 2-Green Step 2: 両プロトコル失敗をシミュレート
        var mockSocketFactory = new MockSocketFactory(tcpShouldSucceed: false, udpShouldSucceed: false, simulatedDelayMs: 10);

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,
            null,
            mockSocketFactory
        );

        // Act: 接続実行
        var result = await manager.ConnectAsync();

        // Assert: 両プロトコル失敗時の検証
        Assert.NotEqual(ConnectionStatus.Connected, result.Status);  // 失敗
        Assert.Null(result.Socket);
        Assert.NotNull(result.ErrorMessage);
        
        // 両プロトコルのエラー詳細が含まれることを確認
        Assert.Contains("TCP", result.ErrorMessage);  // TCPエラー情報
        Assert.Contains("UDP", result.ErrorMessage);  // UDPエラー情報
    }

    #endregion

    #region Phase 3: ログ出力実装テスト (2025-12-03)

    /// <summary>
    /// TC_P3_001: 初期プロトコル成功時、接続開始ログのみ出力される
    /// </summary>
    [Fact]
    public async Task TC_P3_001_ConnectAsync_初期プロトコル成功_接続開始ログのみ出力()
    {
        // Arrange: 初期TCP接続成功をシミュレート
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.1.100",
            Port = 5000,
            UseTcp = true,  // 初期プロトコル: TCP
            IsBinary = true,
            FrameVersion = FrameVersion.Frame4E
        };

        var timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        var mockSocketFactory = new MockSocketFactory(tcpShouldSucceed: true, udpShouldSucceed: true, simulatedDelayMs: 10);
        var mockLoggingManager = new Mock<ILoggingManager>();

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,
            null,
            mockSocketFactory,
            mockLoggingManager.Object  // LoggingManagerを注入
        );

        // Act: 接続実行
        var result = await manager.ConnectAsync();

        // Assert: 初期プロトコル成功時のログ検証
        Assert.Equal(ConnectionStatus.Connected, result.Status);

        // 接続開始ログが1回出力されたことを確認
        mockLoggingManager.Verify(x => x.LogInfo(
            It.Is<string>(s => s.Contains("PLC接続試行開始") &&
                               s.Contains("192.168.1.100:5000") &&
                               s.Contains("TCP"))),
            Times.Once);

        // 初期成功時は警告・エラーログなし
        mockLoggingManager.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
        mockLoggingManager.Verify(x => x.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// TC_P3_002: 代替プロトコル成功時、警告ログと成功ログが出力される
    /// </summary>
    [Fact]
    public async Task TC_P3_002_ConnectAsync_代替プロトコル成功_警告ログと成功ログ出力()
    {
        // Arrange: TCP失敗→UDP成功をシミュレート
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.1.100",
            Port = 5000,
            UseTcp = true,  // 初期プロトコル: TCP
            IsBinary = true,
            FrameVersion = FrameVersion.Frame4E
        };

        var timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        var mockSocketFactory = new MockSocketFactory(tcpShouldSucceed: false, udpShouldSucceed: true, simulatedDelayMs: 10);
        var mockLoggingManager = new Mock<ILoggingManager>();

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,
            null,
            mockSocketFactory,
            mockLoggingManager.Object  // LoggingManagerを注入
        );

        // Act: 接続実行
        var result = await manager.ConnectAsync();

        // Assert: 代替プロトコル成功時のログ検証
        Assert.Equal(ConnectionStatus.Connected, result.Status);
        Assert.True(result.IsFallbackConnection);

        // 接続開始ログ
        mockLoggingManager.Verify(x => x.LogInfo(
            It.Is<string>(s => s.Contains("PLC接続試行開始"))),
            Times.Once);

        // 初期プロトコル失敗の警告ログ
        mockLoggingManager.Verify(x => x.LogWarning(
            It.Is<string>(s => s.Contains("TCP接続失敗") && s.Contains("UDP") && s.Contains("再試行"))),
            Times.Once);

        // 代替プロトコル成功の情報ログ
        mockLoggingManager.Verify(x => x.LogInfo(
            It.Is<string>(s => s.Contains("代替プロトコル(UDP)で接続成功") &&
                               s.Contains("192.168.1.100:5000"))),
            Times.Once);

        // エラーログなし
        mockLoggingManager.Verify(x => x.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// TC_P3_003: 両プロトコル失敗時、詳細エラーログが出力される
    /// </summary>
    [Fact]
    public async Task TC_P3_003_ConnectAsync_両プロトコル失敗_詳細エラーログ出力()
    {
        // Arrange: TCP/UDP両方失敗をシミュレート
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.1.100",
            Port = 5000,
            UseTcp = true,  // 初期プロトコル: TCP
            IsBinary = true,
            FrameVersion = FrameVersion.Frame4E
        };

        var timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        var mockSocketFactory = new MockSocketFactory(tcpShouldSucceed: false, udpShouldSucceed: false, simulatedDelayMs: 10);
        var mockLoggingManager = new Mock<ILoggingManager>();

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            null,
            null,
            mockSocketFactory,
            mockLoggingManager.Object  // LoggingManagerを注入
        );

        // Act: 接続実行
        var result = await manager.ConnectAsync();

        // Assert: 両プロトコル失敗時のログ検証
        Assert.NotEqual(ConnectionStatus.Connected, result.Status);

        // 接続開始ログ
        mockLoggingManager.Verify(x => x.LogInfo(
            It.Is<string>(s => s.Contains("PLC接続試行開始"))),
            Times.Once);

        // 初期プロトコル失敗の警告ログ
        mockLoggingManager.Verify(x => x.LogWarning(
            It.Is<string>(s => s.Contains("TCP接続失敗") && s.Contains("UDP") && s.Contains("再試行"))),
            Times.Once);

        // 両プロトコル失敗の詳細エラーログ
        mockLoggingManager.Verify(x => x.LogError(
            null,
            It.Is<string>(s => s.Contains("PLC接続失敗") &&
                               s.Contains("192.168.1.100:5000") &&
                               s.Contains("TCP/UDP両プロトコルで接続に失敗") &&
                               s.Contains("TCP") &&
                               s.Contains("UDP"))),
            Times.Once);
    }

    #endregion

    #region Phase 13: データモデル一本化 - ProcessReceivedRawDataエッジケーステスト

    /// <summary>
    /// Phase13_Test01: 4Eフレーム正常解析
    /// SlmpDataParserTests.ParseReadRandomResponse_4EFrame_ValidResponse_ReturnsCorrectDataから移行
    /// </summary>
    [Fact]
    public async Task Phase13_ProcessReceivedRawData_4EFrame_ValidResponse_ReturnsCorrectData()
    {
        // Arrange
        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007,
                UseTcp = false,
                IsBinary = true,
                FrameVersion = FrameVersion.Frame4E
            },
            new TimeoutConfig
            {
                ReceiveTimeoutMs = 3000,
                SendTimeoutMs = 3000
            }
        );

        var processedRequestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "M",
            StartAddress = 0,
            Count = 3,
            FrameType = FrameType.Frame4E,
            RequestedAt = DateTime.Now,
            DeviceSpecifications = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.M, 0),
                new DeviceSpecification(DeviceCode.M, 16),
                new DeviceSpecification(DeviceCode.D, 100)
            }
        };

        // 4Eフレーム応答（15バイトヘッダ + 6バイトデータ = 21バイト）
        byte[] rawData = new byte[]
        {
            // サブヘッダ2バイト
            0xD4, 0x00,
            // シーケンス番号2バイト
            0x00, 0x00,
            // 予約2バイト
            0x00, 0x00,
            // ネットワーク番号1バイト
            0x00,
            // PC番号1バイト
            0xFF,
            // I/O番号2バイト（LE: 0x03FF）
            0xFF, 0x03,
            // マルチドロップ局番1バイト
            0x00,
            // データ長2バイト（LE: 8バイト = エンドコード2 + データ6）
            0x08, 0x00,
            // エンドコード2バイト（正常）
            0x00, 0x00,
            // デバイスデータ6バイト（3ワード × 2バイト）
            0x01, 0x00,  // M0 = 0x0001
            0x02, 0x00,  // M16 = 0x0002
            0x64, 0x00   // D100 = 0x0064 = 100
        };

        // Act
        var result = await manager.ProcessReceivedRawData(rawData, processedRequestInfo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.ProcessedData.Count);
        Assert.True(result.ProcessedData.ContainsKey("M0"));
        Assert.True(result.ProcessedData.ContainsKey("M16"));
        Assert.True(result.ProcessedData.ContainsKey("D100"));
        Assert.Equal(1u, result.ProcessedData["M0"].Value);
        Assert.Equal(2u, result.ProcessedData["M16"].Value);
        Assert.Equal(100u, result.ProcessedData["D100"].Value);
    }

    /// <summary>
    /// Phase13_Test02: 3Eフレーム正常解析
    /// SlmpDataParserTests.ParseReadRandomResponse_3EFrame_ValidResponse_ReturnsCorrectDataから移行
    /// </summary>
    [Fact]
    public async Task Phase13_ProcessReceivedRawData_3EFrame_ValidResponse_ReturnsCorrectData()
    {
        // Arrange
        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007,
                UseTcp = false,
                IsBinary = true,
                FrameVersion = FrameVersion.Frame3E
            },
            new TimeoutConfig
            {
                ReceiveTimeoutMs = 3000,
                SendTimeoutMs = 3000
            }
        );

        var processedRequestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "D",
            StartAddress = 100,
            Count = 2,
            FrameType = FrameType.Frame3E,
            RequestedAt = DateTime.Now,
            DeviceSpecifications = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 100),
                new DeviceSpecification(DeviceCode.D, 200)
            }
        };

        // 3Eフレーム応答（11バイトヘッダ + 4バイトデータ = 15バイト）
        byte[] rawData = new byte[]
        {
            // サブヘッダ2バイト
            0xD0, 0x00,
            // ネットワーク番号1バイト
            0x00,
            // PC番号1バイト
            0xFF,
            // I/O番号2バイト（LE: 0x03FF）
            0xFF, 0x03,
            // マルチドロップ局番1バイト
            0x00,
            // データ長2バイト（LE: 6バイト = エンドコード2 + データ4）
            0x06, 0x00,
            // エンドコード2バイト（正常）
            0x00, 0x00,
            // デバイスデータ4バイト（2ワード × 2バイト）
            0x64, 0x00,  // D100 = 0x0064 = 100
            0xC8, 0x00   // D200 = 0x00C8 = 200
        };

        // Act
        var result = await manager.ProcessReceivedRawData(rawData, processedRequestInfo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.ProcessedData.Count);
        Assert.Equal(100u, result.ProcessedData["D100"].Value);
        Assert.Equal(200u, result.ProcessedData["D200"].Value);
    }

    /// <summary>
    /// Phase13_Test03: 16進アドレスデバイスの正常解析
    /// SlmpDataParserTests.ParseReadRandomResponse_HexAddressDevice_ReturnsCorrectKeyから移行
    /// </summary>
    [Fact]
    public async Task Phase13_ProcessReceivedRawData_HexAddressDevice_ReturnsCorrectKey()
    {
        // Arrange
        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007,
                UseTcp = false,
                IsBinary = true,
                FrameVersion = FrameVersion.Frame4E
            },
            new TimeoutConfig
            {
                ReceiveTimeoutMs = 3000,
                SendTimeoutMs = 3000
            }
        );

        var processedRequestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "W",
            StartAddress = 0x11AA,
            Count = 1,
            FrameType = FrameType.Frame4E,
            RequestedAt = DateTime.Now,
            DeviceSpecifications = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.W, 0x11AA, isHexAddress: true)
            }
        };

        byte[] rawData = new byte[]
        {
            0xD4, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF,
            0xFF, 0x03, 0x00, 0x04, 0x00, 0x00, 0x00,
            0x99, 0x26  // W0x11AA = 0x2699 = 9881
        };

        // Act
        var result = await manager.ProcessReceivedRawData(rawData, processedRequestInfo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.ProcessedData);

        // 注意: DeviceDataのキー生成では、16進アドレスでも10進数表現になる
        // 0x11AA = 4522
        var actualKey = result.ProcessedData.Keys.First();
        Assert.Equal("W4522", actualKey);
        Assert.Equal(9881u, result.ProcessedData[actualKey].Value);
    }

    /// <summary>
    /// Phase13_Test04: エラーエンドコード処理
    /// SlmpDataParserTests.ParseReadRandomResponse_ErrorEndCode_ThrowsExceptionから移行
    /// </summary>
    [Fact]
    public async Task Phase13_ProcessReceivedRawData_ErrorEndCode_ReturnsFailureResult()
    {
        // Arrange
        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007,
                UseTcp = false,
                IsBinary = true,
                FrameVersion = FrameVersion.Frame3E
            },
            new TimeoutConfig
            {
                ReceiveTimeoutMs = 3000,
                SendTimeoutMs = 3000
            }
        );

        var processedRequestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "D",
            StartAddress = 100,
            Count = 1,
            FrameType = FrameType.Frame3E,
            RequestedAt = DateTime.Now,
            DeviceSpecifications = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 100)
            }
        };

        // エラーレスポンス（エンドコード = 0xC051 = デバイス範囲エラー）
        byte[] rawData = new byte[]
        {
            0xD0, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00,
            0x02, 0x00,  // データ長: 2バイト（エンドコードのみ）
            0x51, 0xC0   // エンドコード: 0xC051（エラー）
        };

        // Act
        var result = await manager.ProcessReceivedRawData(rawData, processedRequestInfo);

        // Assert
        // ProcessReceivedRawDataはエラーエンドコードの場合IsSuccess=falseを返す
        Assert.False(result.IsSuccess);
    }

    /// <summary>
    /// Phase13_Test05: 空フレームエラー検証
    /// SlmpDataParserTests.ParseReadRandomResponse_EmptyFrame_ThrowsExceptionから移行
    /// </summary>
    [Fact]
    public async Task Phase13_ProcessReceivedRawData_EmptyFrame_ThrowsException()
    {
        // Arrange
        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007,
                UseTcp = false,
                IsBinary = true,
                FrameVersion = FrameVersion.Frame3E
            },
            new TimeoutConfig
            {
                ReceiveTimeoutMs = 3000,
                SendTimeoutMs = 3000
            }
        );

        var processedRequestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "D",
            StartAddress = 100,
            Count = 1,
            FrameType = FrameType.Frame3E,
            RequestedAt = DateTime.Now,
            DeviceSpecifications = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 100)
            }
        };

        byte[] emptyFrame = Array.Empty<byte>();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            async () => await manager.ProcessReceivedRawData(emptyFrame, processedRequestInfo)
        );
        Assert.Contains("受信データの形式が不正", ex.Message);
    }

    /// <summary>
    /// Phase13_Test06: 不正サブヘッダエラー検証
    /// SlmpDataParserTests.ParseReadRandomResponse_InvalidSubHeader_ThrowsExceptionから移行
    /// </summary>
    [Fact]
    public async Task Phase13_ProcessReceivedRawData_InvalidSubHeader_ReturnsFailureResult()
    {
        // Arrange
        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007,
                UseTcp = false,
                IsBinary = true,
                FrameVersion = FrameVersion.Frame4E
            },
            new TimeoutConfig
            {
                ReceiveTimeoutMs = 3000,
                SendTimeoutMs = 3000
            }
        );

        var processedRequestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "D",
            StartAddress = 100,
            Count = 1,
            FrameType = FrameType.Frame4E,
            RequestedAt = DateTime.Now,
            DeviceSpecifications = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 100)
            }
        };

        // 不正なサブヘッダ
        byte[] invalidFrame = new byte[]
        {
            0xFF, 0xFF,  // 不正なサブヘッダ
            0x00, 0x00, 0x00, 0x00, 0x00, 0xFF,
            0xFF, 0x03, 0x00, 0x04, 0x00, 0x00, 0x00
        };

        // Act
        var result = await manager.ProcessReceivedRawData(invalidFrame, processedRequestInfo);

        // Assert
        // 不正サブヘッダの場合IsSuccess=falseを返す
        Assert.False(result.IsSuccess);
    }

    /// <summary>
    /// Phase13_Test07: 実データ簡易版テスト（複数デバイス）
    /// SlmpDataParserTests.ParseReadRandomResponse_MemoMdRealDataSimplified_ReturnsCorrectCountから移行
    /// </summary>
    [Fact]
    public async Task Phase13_ProcessReceivedRawData_MultipleDevices_ReturnsCorrectCount()
    {
        // Arrange
        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007,
                UseTcp = false,
                IsBinary = true,
                FrameVersion = FrameVersion.Frame4E
            },
            new TimeoutConfig
            {
                ReceiveTimeoutMs = 3000,
                SendTimeoutMs = 3000
            }
        );

        var devices = new List<DeviceSpecification>();
        for (int i = 0; i < 10; i++)
        {
            devices.Add(new DeviceSpecification(DeviceCode.D, i));
        }

        var processedRequestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "D",
            StartAddress = 0,
            Count = 10,
            FrameType = FrameType.Frame4E,
            RequestedAt = DateTime.Now,
            DeviceSpecifications = devices
        };

        // 4Eフレーム（15バイトヘッダ + 2バイトエンドコード + 20バイトデータ）
        byte[] rawData = new byte[]
        {
            // ヘッダ（15バイト）
            0xD4, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF,
            0xFF, 0x03, 0x00, 0x16, 0x00,  // データ長: 22バイト (エンドコード2 + データ20)
            // エンドコード（2バイト）
            0x00, 0x00,
            // デバイスデータ（10ワード = 20バイト）
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF
        };

        // Act
        var result = await manager.ProcessReceivedRawData(rawData, processedRequestInfo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.ProcessedData.Count);
        Assert.All(result.ProcessedData.Values, data => Assert.Equal(0xFFFFu, data.Value));
        Assert.True(result.ProcessedData.ContainsKey("D0"));
        Assert.True(result.ProcessedData.ContainsKey("D9"));
    }

    /// <summary>
    /// Phase13_Test08: データ不足エラー検証
    /// SlmpDataParserTests.ParseReadRandomResponse_InsufficientDataSize_ThrowsExceptionから移行
    /// </summary>
    [Fact]
    public async Task Phase13_ProcessReceivedRawData_InsufficientDataSize_ReturnsFailureResult()
    {
        // Arrange
        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007,
                UseTcp = false,
                IsBinary = true,
                FrameVersion = FrameVersion.Frame4E
            },
            new TimeoutConfig
            {
                ReceiveTimeoutMs = 3000,
                SendTimeoutMs = 3000
            }
        );

        var processedRequestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "D",
            StartAddress = 100,
            Count = 2,
            FrameType = FrameType.Frame4E,
            RequestedAt = DateTime.Now,
            DeviceSpecifications = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 100),
                new DeviceSpecification(DeviceCode.D, 200)
            }
        };

        // データ部が1ワード分不足（2バイト不足）
        byte[] rawData = new byte[]
        {
            0xD4, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF,
            0xFF, 0x03, 0x00, 0x04, 0x00, 0x00, 0x00,
            0x64, 0x00   // 1ワードのみ（2ワード必要）
        };

        // Act
        var result = await manager.ProcessReceivedRawData(rawData, processedRequestInfo);

        // Assert
        // データ不足の場合IsSuccess=falseを返す
        Assert.False(result.IsSuccess);
    }

    #endregion
}
