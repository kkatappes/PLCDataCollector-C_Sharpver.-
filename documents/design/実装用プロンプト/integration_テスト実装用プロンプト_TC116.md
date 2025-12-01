# TC116 Step3to5 UDP完全サイクル統合テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC116（Step3to5_UDP完全サイクル正常動作）の統合テストケースを、TDD手法に従って実装してください。

---

## TC116: Step3to5_UDP完全サイクル正常動作

### 目的
UDP接続でのStep3-5完全サイクル実行の統合テスト
19時deadline対応のPhase 2: 連続動作確認の重要テスト

### テスト対象サイクル
```
Step3: PLC接続処理（ConnectAsync - UDP）
  ↓
Step4: PLCリクエスト送信（SendFrameAsync）× 2回
  ↓
Step4: PLCデータ受信（ReceiveResponseAsync）× 2回
  ↓
Step5: PLC切断処理（DisconnectAsync）
```

### 前提条件
- PlcCommunicationManagerクラス実装済み
- TCP版統合テスト（TC115）完了済み
- MockUdpServerが4Eフレーム対応で稼働可能
- 単体テスト（TC018, TC021, TC025, TC027）完了済み

### TDD実装手順

#### Arrange（準備）
```csharp
// 1. UDP接続設定準備
var connectionConfig = new ConnectionConfig
{
    IpAddress = "192.168.1.10",
    Port = 5000,
    UseTcp = false,                    // UDP使用
    ConnectionType = "UDP",
    IsBinary = false,
    FrameVersion = FrameVersion.Frame4E
};

// 2. タイムアウト設定準備
var timeoutConfig = new TimeoutConfig
{
    ConnectTimeoutMs = 5000,
    SendTimeoutMs = 3000,
    ReceiveTimeoutMs = 3000
};

// 3. SLMPフレーム準備（M機器用、D機器用）
var slmpFrames = new List<string>
{
    "54001234000000010401006400000090E8030000",  // M機器用フレーム
    "54001234000000010400A800000090E8030000"   // D機器用フレーム
};

// 4. MockUdpServer準備
var mockUdpServer = new MockUdpServer("192.168.1.10", 5000);
mockUdpServer.Start();

// M機器応答データ設定（125バイト → 250文字）
mockUdpServer.SetResponse(slmpFrames[0], CreateMDeviceResponse());

// D機器応答データ設定（2000バイト → 4000文字）
mockUdpServer.SetResponse(slmpFrames[1], CreateDDeviceResponse());

// 5. PlcCommunicationManager初期化
var plcCommManager = new PlcCommunicationManager(
    loggingManager,
    errorHandler,
    resourceManager
);
```

#### Act（実行）- Step3to5サイクル実行
```csharp
// Step3: UDP接続
var connectionResponse = await plcCommManager.ConnectAsync(
    connectionConfig,
    timeoutConfig
);

// Step4: M機器フレーム送信
await plcCommManager.SendFrameAsync(slmpFrames[0]);

// Step4: M機器応答受信
var mDeviceResponse = await plcCommManager.ReceiveResponseAsync(
    timeoutConfig.ReceiveTimeoutMs
);

// Step4: D機器フレーム送信
await plcCommManager.SendFrameAsync(slmpFrames[1]);

// Step4: D機器応答受信
var dDeviceResponse = await plcCommManager.ReceiveResponseAsync(
    timeoutConfig.ReceiveTimeoutMs
);

// Step5: UDP切断
await plcCommManager.DisconnectAsync();

// 統計情報取得
var connectionStats = plcCommManager.GetConnectionStats();
```

#### Assert（検証）

##### 1. Step3（UDP接続）検証
```csharp
// UDP接続成功検証
Assert.Equal(ConnectionStatus.Connected, connectionResponse.Status);
Assert.NotNull(connectionResponse.Socket);
Assert.True(connectionResponse.Socket.Connected);
Assert.Equal(ProtocolType.Udp, connectionResponse.Socket.ProtocolType);

// UDP固有検証
Assert.Equal(SocketType.Dgram, connectionResponse.Socket.SocketType);
Assert.Equal(AddressFamily.InterNetwork, connectionResponse.Socket.AddressFamily);

// ソケットタイムアウト設定検証
Assert.Equal(timeoutConfig.SendTimeoutMs, connectionResponse.Socket.SendTimeout);
Assert.Equal(timeoutConfig.ReceiveTimeoutMs, connectionResponse.Socket.ReceiveTimeout);

// エンドポイント検証
Assert.NotNull(connectionResponse.RemoteEndPoint);
Assert.Equal("192.168.1.10:5000", connectionResponse.RemoteEndPoint.ToString());

// 時刻・時間検証
Assert.NotNull(connectionResponse.ConnectedAt);
Assert.True(connectionResponse.ConnectedAt.Value <= DateTime.UtcNow);
Assert.NotNull(connectionResponse.ConnectionTime);
Assert.True(connectionResponse.ConnectionTime.Value > TimeSpan.Zero);
Assert.True(connectionResponse.ConnectionTime.Value.TotalMilliseconds < timeoutConfig.ConnectTimeoutMs);

// UDP特性検証: TCPより接続時間が短い
Assert.True(connectionResponse.ConnectionTime.Value.TotalMilliseconds < 100);

// エラー情報検証
Assert.Null(connectionResponse.ErrorMessage);
```

##### 2. Step4（送受信）検証
```csharp
// 受信成功検証
Assert.NotNull(mDeviceResponse);
Assert.NotNull(dDeviceResponse);
Assert.NotEmpty(mDeviceResponse);
Assert.NotEmpty(dDeviceResponse);

// 受信データ長検証（UDP特有：パディング考慮）
Assert.True(mDeviceResponse.Length >= 250);   // 最低250文字以上
Assert.True(dDeviceResponse.Length >= 4000);  // 最低4000文字以上

// パディング検証（もしパディングが存在する場合）
if (mDeviceResponse.Length > 250)
{
    var padding = mDeviceResponse.Substring(250);
    Assert.Matches("^[0]+$", padding);  // パディングは0のみ
}

if (dDeviceResponse.Length > 4000)
{
    var padding = dDeviceResponse.Substring(4000);
    Assert.Matches("^[0]+$", padding);  // パディングは0のみ
}

// 受信データ形式検証
Assert.Matches("^[0-9A-Fa-f]+$", mDeviceResponse);  // 16進数文字列
Assert.Matches("^[0-9A-Fa-f]+$", dDeviceResponse);  // 16進数文字列

// 実データ部分検証（先頭250文字、4000文字のみ）
var mActualData = mDeviceResponse.Substring(0, Math.Min(250, mDeviceResponse.Length));
var dActualData = dDeviceResponse.Substring(0, Math.Min(4000, dDeviceResponse.Length));
Assert.Equal(250, mActualData.Length);
Assert.Equal(4000, dActualData.Length);
```

##### 3. Step5（UDP切断）検証
```csharp
// UDP切断成功検証
Assert.False(connectionResponse.Socket.Connected);

// ソケット状態検証（破棄済み確認）
Assert.Throws<ObjectDisposedException>(() =>
{
    var _ = connectionResponse.Socket.RemoteEndPoint;
});
```

##### 4. 統計情報検証
```csharp
// 統計情報累積検証
Assert.NotNull(connectionStats);
Assert.True(connectionStats.TotalResponseTime > TimeSpan.Zero);
Assert.Equal(0, connectionStats.TotalErrors);
Assert.Equal(0, connectionStats.TotalRetries);
Assert.Equal(1.0, connectionStats.SuccessRate);

// 応答時間内訳検証
var expectedTotalTime = connectionResponse.ConnectionTime.Value;
Assert.True(connectionStats.TotalResponseTime >= expectedTotalTime);

// UDP特性: TCP版（TC115）より短い処理時間
Assert.True(connectionStats.TotalResponseTime.TotalMilliseconds < 600); // UDP上限
```

### UDP vs TCP の重要な違い

#### UDP（本テスト対象）
- **接続**: コネクションレス（疎通確認のみ）
- **ソケットタイプ**: SocketType.Dgram
- **プロトコル**: ProtocolType.Udp
- **信頼性**: 低（再送なし、順序保証なし）
- **接続時間**: 極めて短い（10-50ms）
- **パケット境界**: 保証される（1パケット = 1送信単位）
- **データ長**: パディングの可能性あり

#### TCP（TC115で検証済み）
- **接続**: 3ウェイハンドシェイク
- **ソケットタイプ**: SocketType.Stream
- **プロトコル**: ProtocolType.Tcp
- **信頼性**: 高（再送制御、順序保証）
- **接続時間**: 比較的長い（50-150ms）
- **ストリーム**: 境界なし、連続データ
- **データ長**: 正確（パディングなし）

### 必要なテストユーティリティ

#### MockUdpServer（UDP対応PLCシミュレータ）
```csharp
public class MockUdpServer
{
    private UdpClient _udpClient;
    private IPEndPoint _endPoint;
    private Dictionary<string, string> _responseMap;

    public MockUdpServer(string ipAddress, int port)
    {
        _endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        _responseMap = new Dictionary<string, string>();
    }

    public void Start()
    {
        _udpClient = new UdpClient(_endPoint);
        // バックグラウンドでリクエスト受信・応答送信処理
    }

    public void SetResponse(string requestFrame, string responseData)
    {
        _responseMap[requestFrame] = responseData;
    }

    public void Stop()
    {
        _udpClient?.Close();
    }
}
```

#### 応答データ作成ヘルパー
```csharp
private string CreateMDeviceResponse()
{
    // M機器応答: 125バイト（250文字の16進数）
    // 4Eフレーム + 正常終了コード + M000-M999データ
    return "D4001234" + "0000" + new string('0', 242); // 合計250文字
}

private string CreateDDeviceResponse()
{
    // D機器応答: 2000バイト（4000文字の16進数）
    // 4Eフレーム + 正常終了コード + D000-D999データ
    return "D4001234" + "0000" + new string('1', 3992); // 合計4000文字
}
```

### UDP固有の検証ポイント

#### 1. コネクションレス特性
```csharp
// UDP接続は疎通確認のみ（実際の接続確立なし）
Assert.Equal(SocketType.Dgram, socket.SocketType);
Assert.Equal(ProtocolType.Udp, socket.ProtocolType);

// Connect呼び出しは即座に完了
Assert.True(connectionTime.TotalMilliseconds < 100);
```

#### 2. パケット境界保証
```csharp
// UDP: 1パケット = 1送信単位
// ReceiveFrom一度の呼び出しでパケット全体を受信
var receivedData = await ReceiveResponseAsync(timeoutConfig);
Assert.True(receivedData.Length >= expectedLength);
```

#### 3. パディング処理
```csharp
// ネットワーク層の最小パケットサイズ調整でパディングされる可能性
if (receivedData.Length > expectedLength)
{
    // パディング部分を検証（通常は0x00）
    var padding = receivedData.Substring(expectedLength);
    Assert.Matches("^[0]+$", padding);
}
```

### 処理時間目安（UDP特性）
- Step3（UDP接続）: 10-50ms（疎通確認のみ）
- Step4（送信）: 5-30ms × 2回 = 10-60ms
- Step4（受信）: 50-200ms × 2回 = 100-400ms
- Step5（UDP切断）: 5-20ms（ソケットクローズのみ）
- **合計**: 125-530ms（通常200-300ms）

### ログ出力例
```
[Information] Step3開始: UDP接続処理 - 192.168.1.10:5000
[Debug] UDP接続設定: SendTimeout=3000ms, ReceiveTimeout=3000ms
[Debug] UDP接続: コネクションレス疎通確認
[Information] Step3完了: UDP接続成功, 接続時間=15ms
[Information] Step4開始: M機器フレーム送信（UDP）
[Information] Step4完了: M機器送信成功
[Information] Step4開始: M機器応答受信（UDP）
[Debug] UDP受信: データ長=250文字, パディングなし
[Information] Step4完了: M機器受信成功, データ長=250文字
[Information] Step4開始: D機器フレーム送信（UDP）
[Information] Step4完了: D機器送信成功
[Information] Step4開始: D機器応答受信（UDP）
[Debug] UDP受信: データ長=4008文字, パディング=8文字
[Information] Step4完了: D機器受信成功, 実データ=4000文字
[Information] Step5開始: UDP切断処理
[Information] Step5完了: UDP切断成功
[Information] Step3to5サイクル完了: UDP, 合計時間=250ms, 成功率=100%
```

### 完了条件

- [ ] TC116（Step3to5_UDP完全サイクル正常動作）テストがパス
- [ ] UDP接続の特性（コネクションレス、短時間接続）が正しく検証されることを確認
- [ ] UDP送受信のパケット境界保証が正しく動作することを確認
- [ ] パディング処理が適切に対応されることを確認
- [ ] 統計情報がTCP版（TC115）と適切に比較できることを確認
- [ ] TCP版との処理時間差異が確認できることを確認
- [ ] ログ出力がUDP特性を含んで適切に記録されることを確認
- [ ] チェックリストの該当項目にチェック

### 重要ポイント
- **統合テスト**: Step3-5を連続実行する統合テスト
- **UDP特性**: コネクションレス、パケット境界、パディングの考慮
- **TCP比較**: TCP版（TC115）との動作・性能比較
- **Phase2必須**: 19時deadline対応のPhase2連続動作確認テスト
- **TDD準拠**: Red→Green→Refactorサイクルを遵守

---

以上の指示に従って実装してください。