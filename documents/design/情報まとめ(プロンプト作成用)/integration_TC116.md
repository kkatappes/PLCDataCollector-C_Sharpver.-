# Integration Step3to5 UDP完全サイクル テスト実装用情報（TC116）

## ドキュメント概要

### 目的
このドキュメントは、TC116_Step3to5_UDP完全サイクル正常動作テストの実装に必要な情報を集約したものです。
**コード作成時に必要となる技術情報のみ**を記載しており、学習資料や説明的な内容は含みません。

### 情報取得元
本ドキュメントの情報は以下のソースから抽出・統合されています：

#### 設計書（andon/documents/design/）
- `クラス・メソッドリスト.md` - クラス・メソッドの一覧と概要
- `クラス設計.md` - 詳細なクラス設計仕様
- `テスト内容.md` - テストケース仕様
- `プロジェクト構造設計.md` - フォルダ構造・プロジェクト構成
- `依存関係.md` - クラス間の依存関係
- `各ステップio.md` - 各ステップのInput/Output詳細

#### 既存テストケース実装情報
- `step3_TC018.md` - UDP接続テスト（Step3）
- `step3_TC021.md` - 送信テスト（Step4）
- `step3_TC025.md` - 受信テスト（Step4）
- `step3_TC027.md` - 切断テスト（Step5）
- `integration_TC115.md` - TCP完全サイクルテスト（対比用）

---

## 1. テスト対象機能仕様

### Step3to5サイクル（UDP接続→送受信→切断）
**統合テスト対象**: PlcCommunicationManager（Step3-5）
**名前空間**: andon.Core.Managers

#### Step3to5サイクル構成
```
Step3: PLC接続処理（ConnectAsync - UDP）
  ↓
Step4: PLCリクエスト送信（SendFrameAsync）
  ↓
Step4: PLCデータ受信（ReceiveResponseAsync）
  ↓
Step5: PLC切断処理（DisconnectAsync）
```

#### 統合Input
- ConnectionConfig（IpAddress="192.168.1.10", Port=5000, UseTcp=false, IsBinary=false, FrameVersion="4E"）
- TimeoutConfig（ConnectTimeoutMs=5000, SendTimeoutMs=3000, ReceiveTimeoutMs=3000）
- SLMPフレーム（2つ）:
  - M機器用: "54001234000000010401006400000090E8030000"
  - D機器用: "54001234000000010400A800000090E8030000"

#### 統合Output
- ConnectionResponse（接続結果）
- PLCからの生データ（2つ）:
  - M機器応答: 125バイト（ASCII形式250文字）
  - D機器応答: 2000バイト（ASCII形式4000文字）
- ConnectionStats（接続統計情報）:
  - TotalResponseTime（合計応答時間）
  - TotalErrors（0）
  - SuccessRate（1.0）

#### 機能
- UDP接続→送受信→切断の連続実行
- 統計情報の累積
- リソース管理（ソケット）

---

## 2. テストケース仕様（TC116）

### TC116_Step3to5_UDP完全サイクル正常動作
**目的**: UDP接続でのStep3-5完全サイクル実行を統合テスト

#### 前提条件
- ConfigToFrameManagerによる設定読み込み完了
- BuildFramesによるSLMPフレーム生成完了
- PLCシミュレータまたはモックUDPサーバーが稼働中
- ネットワーク到達可能

#### 入力データ
**ConnectionConfig（UDP接続設定）**:
```csharp
var connectionConfig = new ConnectionConfig
{
    IpAddress = "192.168.1.10",
    Port = 5000,
    UseTcp = false,
    ConnectionType = "UDP",
    IsBinary = false,
    FrameVersion = FrameVersion.Frame4E
};
```

**TimeoutConfig（タイムアウト設定）**:
```csharp
var timeoutConfig = new TimeoutConfig
{
    ConnectTimeoutMs = 5000,
    SendTimeoutMs = 3000,
    ReceiveTimeoutMs = 3000
};
```

**SLMPフレーム（2つ）**:
```csharp
var slmpFrames = new List<string>
{
    "54001234000000010401006400000090E8030000",  // M機器用
    "54001234000000010400A800000090E8030000"   // D機器用
};
```

#### 期待出力
**ConnectionResponse（接続成功）**:
```csharp
Assert.Equal(ConnectionStatus.Connected, connectionResponse.Status);
Assert.NotNull(connectionResponse.Socket);
Assert.True(connectionResponse.Socket.Connected);
Assert.Equal(ProtocolType.Udp, connectionResponse.Socket.ProtocolType);
Assert.NotNull(connectionResponse.RemoteEndPoint);
Assert.NotNull(connectionResponse.ConnectedAt);
Assert.NotNull(connectionResponse.ConnectionTime);
```

**PLCからの生データ（2つ）**:
```csharp
Assert.NotNull(mDeviceResponse);
Assert.NotNull(dDeviceResponse);
Assert.True(mDeviceResponse.Length >= 250);   // UDP: パディング可能性
Assert.True(dDeviceResponse.Length >= 4000);  // UDP: パディング可能性
```

**ConnectionStats（統計情報）**:
```csharp
Assert.NotNull(connectionStats);
Assert.True(connectionStats.TotalResponseTime > TimeSpan.Zero);
Assert.Equal(0, connectionStats.TotalErrors);
Assert.Equal(1.0, connectionStats.SuccessRate);
```

#### 動作フロー成功条件
1. **Step3（UDP接続）成功**:
   - ConnectionResponse.Status == ConnectionStatus.Connected
   - Socket.Connected == true
   - Socket.ProtocolType == ProtocolType.Udp
   - RemoteEndPoint == "192.168.1.10:5000"

2. **Step4（送信）成功**:
   - SendFrameAsync完了（例外なし）
   - 2つのフレーム送信成功

3. **Step4（受信）成功**:
   - ReceiveResponseAsync完了（例外なし）
   - 2つの応答データ受信成功
   - データ長が期待値以上（UDP特性考慮）

4. **Step5（切断）成功**:
   - DisconnectAsync完了（例外なし）
   - Socket.Connected == false
   - 統計情報記録済み

---

## 3. Step別詳細仕様

### Step3: UDP接続処理（ConnectAsync）

#### 詳細情報
**参照**: step3_TC018.md（UDP接続成功テスト）

#### Input
- ConnectionConfig（IpAddress="192.168.1.10", Port=5000, UseTcp=false）
- TimeoutConfig（ConnectTimeoutMs=5000）

#### Output
- ConnectionResponse:
  - Status = ConnectionStatus.Connected
  - Socket != null（UDP用ソケット）
  - Socket.ProtocolType = ProtocolType.Udp
  - Socket.SocketType = SocketType.Dgram
  - Socket.SendTimeout = 3000ms
  - Socket.ReceiveTimeout = 3000ms
  - RemoteEndPoint = "192.168.1.10:5000"
  - ConnectedAt != null
  - ConnectionTime > TimeSpan.Zero

#### 検証ポイント（UDP固有）
- UDP接続成功（コネクションレス疎通確認）
- ソケットタイプがDatagramである
- ソケットタイムアウト設定済み
- 接続時間が ConnectTimeoutMs 以内
- エンドポイント情報正確に記録

#### UDP接続の特性
- **疎通確認のみ**: 実際の3ウェイハンドシェイクは発生しない
- **Connect呼び出し**: デフォルトの送信先アドレスを設定するのみ
- **即座に完了**: TCPと比較して接続時間が極めて短い（通常10-50ms）

---

### Step4: PLCリクエスト送信（SendFrameAsync）

#### 詳細情報
**参照**: step3_TC021.md（正常送信テスト）

#### Input
- SLMPフレーム（2つ）:
  - M機器用: "54001234000000010401006400000090E8030000"
  - D機器用: "54001234000000010400A800000090E8030000"

#### Output
- Task（送信完了）

#### 検証ポイント（UDP固有）
- 2つのフレーム連続送信成功
- 送信完了（例外なし）
- **UDP特性**: 送信成功でも到達保証なし（テストではモックで保証）

---

### Step4: PLCデータ受信（ReceiveResponseAsync）

#### 詳細情報
**参照**: step3_TC025.md（正常受信テスト）

#### Input
- TimeoutConfig（ReceiveTimeoutMs=3000）

#### Output
- PLCからの生データ（16進数文字列）:
  - M機器応答: 125バイト、ASCII形式250文字（+ パディング可能性）
  - D機器応答: 2000バイト、ASCII形式4000文字（+ パディング可能性）

#### 検証ポイント（UDP固有）
- 2つの応答データ受信成功
- 受信データ長が期待値以上（パディング考慮）
- 受信時間が ReceiveTimeoutMs 以内
- **UDP特性**: データ欠損・重複の可能性（テストではモックで保証）

#### UDP受信の特性
- **パケット単位**: 一度のRecvFrom呼び出しで1パケット全体を受信
- **パディング**: ネットワーク層での最小パケットサイズ調整でパディングされる可能性
- **順序非保証**: 本来順序保証がないが、テストでは送信順に受信することを期待

---

### Step5: PLC切断処理（DisconnectAsync）

#### 詳細情報
**参照**: step3_TC027.md（正常切断テスト）

#### Input
- 接続統計情報（ConnectionTime等）

#### Output
- 切断完了ステータス
- ConnectionStats（接続統計情報）

#### 検証ポイント（UDP固有）
- UDP接続切断成功（Socket.Connected == false）
- 統計情報正確に記録
- ソケットリソース正常解放
- **UDP特性**: 明示的な切断プロトコル不要（ソケットクローズのみ）

---

## 4. 統合テスト実装構造

### Arrange（準備）

#### 1. 設定準備
```csharp
// ConnectionConfig準備（UDP）
var connectionConfig = new ConnectionConfig
{
    IpAddress = "192.168.1.10",
    Port = 5000,
    UseTcp = false,
    ConnectionType = "UDP",
    IsBinary = false,
    FrameVersion = FrameVersion.Frame4E
};

// TimeoutConfig準備
var timeoutConfig = new TimeoutConfig
{
    ConnectTimeoutMs = 5000,
    SendTimeoutMs = 3000,
    ReceiveTimeoutMs = 3000
};
```

#### 2. SLMPフレーム準備
```csharp
var slmpFrames = new List<string>
{
    "54001234000000010401006400000090E8030000",  // M機器用
    "54001234000000010400A800000090E8030000"   // D機器用
};
```

#### 3. MockUdpServer準備
```csharp
var mockUdpServer = new MockUdpServer("192.168.1.10", 5000);
mockUdpServer.Start();

// M機器応答データ設定
mockUdpServer.SetResponse(slmpFrames[0], CreateMDeviceResponse());

// D機器応答データ設定
mockUdpServer.SetResponse(slmpFrames[1], CreateDDeviceResponse());
```

#### 4. PlcCommunicationManager初期化
```csharp
var plcCommManager = new PlcCommunicationManager(
    loggingManager,
    errorHandler,
    resourceManager
);
```

---

### Act（実行）

#### Step3to5サイクル実行
```csharp
// Step3: UDP接続
var connectionResponse = await plcCommManager.ConnectAsync(
    connectionConfig,
    timeoutConfig
);

// Step4: 送信（M機器用フレーム）
await plcCommManager.SendFrameAsync(slmpFrames[0]);

// Step4: 受信（M機器応答）
var mDeviceResponse = await plcCommManager.ReceiveResponseAsync(timeoutConfig);

// Step4: 送信（D機器用フレーム）
await plcCommManager.SendFrameAsync(slmpFrames[1]);

// Step4: 受信（D機器応答）
var dDeviceResponse = await plcCommManager.ReceiveResponseAsync(timeoutConfig);

// Step5: UDP切断
await plcCommManager.DisconnectAsync();

// 統計情報取得
var connectionStats = plcCommManager.GetConnectionStats();
```

---

### Assert（検証）

#### 1. Step3（UDP接続）検証
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

#### 2. Step4（送受信）検証
```csharp
// 受信成功検証
Assert.NotNull(mDeviceResponse);
Assert.NotNull(dDeviceResponse);
Assert.NotEmpty(mDeviceResponse);
Assert.NotEmpty(dDeviceResponse);

// 受信データ長検証（UDP特有：パディング考慮）
Assert.True(mDeviceResponse.Length >= 250);   // 最低250文字以上
Assert.True(dDeviceResponse.Length >= 4000);  // 最低4000文字以上

// 厳密なデータ長検証（パディングなしの場合）
if (mDeviceResponse.Length > 250)
{
    // パディング検出: 末尾の余分な文字を確認
    var padding = mDeviceResponse.Substring(250);
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

#### 3. Step5（UDP切断）検証
```csharp
// UDP切断成功検証
Assert.False(connectionResponse.Socket.Connected);

// ソケット状態検証
Assert.Throws<ObjectDisposedException>(() =>
{
    var _ = connectionResponse.Socket.RemoteEndPoint;
});
```

#### 4. 統計情報検証
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
```

---

## 5. TCP vs UDP の違い（重要）

### UDP（本テスト対象）
- **接続**: コネクションレス（疎通確認のみ）
- **ソケットタイプ**: SocketType.Dgram
- **プロトコル**: ProtocolType.Udp
- **信頼性**: 低（再送なし、順序保証なし）
- **コネクション**: コネクションレス型
- **切断**: 明示的な切断不要（ソケットクローズのみ）
- **接続時間**: 極めて短い（10-50ms）
- **パケット単位**: 一度の受信で1パケット全体
- **パディング**: ネットワーク層でパディングされる可能性

### TCP（TC115で検証済み）
- **接続**: 3ウェイハンドシェイク（SYN → SYN-ACK → ACK）
- **ソケットタイプ**: SocketType.Stream
- **プロトコル**: ProtocolType.Tcp
- **信頼性**: 高（再送制御、順序保証）
- **コネクション**: 接続指向型
- **切断**: 明示的な切断処理（FIN → FIN-ACK → ACK）
- **接続時間**: 比較的長い（50-150ms）
- **ストリーム**: 境界なし、連続データ
- **パディング**: なし（正確なデータ長）

---

## 6. UDP固有の検証ポイント

### 6.1 コネクションレス特性
```csharp
// UDP接続は疎通確認のみ（実際の接続確立なし）
Assert.Equal(SocketType.Dgram, socket.SocketType);
Assert.Equal(ProtocolType.Udp, socket.ProtocolType);

// Connect呼び出しは即座に完了
Assert.True(connectionTime.TotalMilliseconds < 100);
```

### 6.2 パケット境界保証
```csharp
// UDP: 1パケット = 1送信単位
// 送信フレームサイズ: 40文字（20バイト）× 2
// 受信応答サイズ: 250文字（125バイト）+ パディング可能性

// ReceiveFrom一度の呼び出しでパケット全体を受信
var receivedData = await ReceiveResponseAsync(timeoutConfig);
Assert.True(receivedData.Length >= 250);
```

### 6.3 パディング処理
```csharp
// ネットワーク層の最小パケットサイズ調整でパディングされる可能性
// 実データ長: 125バイト（250文字）
// 最小パケットサイズ: 46バイト（Ethernet）→ 影響なし
// ただし、一部実装でパディングが追加される場合あり

if (receivedData.Length > expectedLength)
{
    // パディング部分を検証（通常は0x00）
    var padding = receivedData.Substring(expectedLength);
    Assert.Matches("^[0]+$", padding);
}
```

### 6.4 順序非保証（テストでは保証）
```csharp
// 本来UDPは順序保証がないが、テストではモックで送信順に受信
// M機器 → D機器の順序で送信・受信
await SendFrameAsync(mDeviceFrame);
var mResponse = await ReceiveResponseAsync(timeoutConfig);

await SendFrameAsync(dDeviceFrame);
var dResponse = await ReceiveResponseAsync(timeoutConfig);

// テストでは順序が保証される
Assert.NotNull(mResponse);
Assert.NotNull(dResponse);
```

---

## 7. エラーハンドリング

### Step3to5サイクルエラー処理（TC116では発生しない）

#### Step3エラー時（UDP固有）
- ConnectionResponse.Status != Connected
- **UDP特性**: タイムアウトエラーが主（接続拒否は発生しない）
- 以降のStep4-5はスキップ
- エラーログ出力
- ConnectionStats.TotalErrors++

#### Step4エラー時（UDP固有）
- TimeoutException または SocketException
- **UDP特性**:
  - 送信失敗は稀（バッファフル以外）
  - 受信タイムアウトが主なエラー
  - パケットロス時はタイムアウトで検出
- Step5（切断）実行後、処理終了
- ConnectionStats.TotalErrors++

#### Step5エラー時（UDP固有）
- DisconnectAsync内で例外発生
- **UDP特性**: 切断時のエラーは極めて稀（ソケットクローズのみ）
- エラーログ出力
- リソース強制解放試行

---

## 8. テスト実装方針（TDD）

### 開発手法
- C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.mdに記載のTDD手法を使用

### テストファイル配置
- **ファイル名**: PlcCommunicationManager_IntegrationTests.cs
- **配置先**: Tests/Integration/
- **名前空間**: andon.Tests.Integration

### テスト実装順序
1. TC017, TC021, TC025, TC027: Step3-5の単体テスト（完了済み）
2. TC115: Step3to5 TCP完全サイクル（完了済み）
3. **TC116**: Step3to5 UDP完全サイクル（本テスト）

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockUdpServer**: UDP接続対応PLCシミュレータ
  - SLMPフレーム受信→応答返却機能
  - 4Eフレーム対応
  - パケットロス・遅延シミュレーション機能
  - パディング設定機能

#### 使用するスタブ
- **ConnectionConfigStubs**: UDP接続設定スタブ
- **TimeoutConfigStubs**: タイムアウト設定スタブ
- **SlmpFrameStubs**: SLMPフレームスタブ（M機器用、D機器用）
- **PlcResponseStubs**: PLC応答データスタブ（UDP用、パディング対応）

---

## 9. 依存クラス・設定

### ConnectionResponse（接続結果）
```csharp
public class ConnectionResponse
{
    public ConnectionStatus Status { get; set; }
    public Socket? Socket { get; set; }
    public EndPoint? RemoteEndPoint { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public TimeSpan? ConnectionTime { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### ConnectionStats（接続統計情報）
```csharp
public class ConnectionStats
{
    public TimeSpan TotalResponseTime { get; set; }
    public int TotalErrors { get; set; }
    public int TotalRetries { get; set; }
    public double SuccessRate { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
}
```

---

## 10. ログ出力要件

### LoggingManager連携
- **UDP接続開始ログ**: 接続先情報
- **UDP接続完了ログ**: 接続時間（コネクションレス疎通確認）
- **送信ログ**: フレーム情報
- **受信ログ**: データ長（パディング含む）
- **UDP切断ログ**: 統計情報

### ログレベル
- **Information**: 各ステップ開始・完了
- **Debug**: 詳細情報（ソケット設定、パディング検出等）

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
[Information] Step3to5サイクル完了: 合計時間=250ms, 成功率=100%
```

---

## 11. テスト実装チェックリスト

### TC116実装前
- [ ] 単体テスト完了確認（TC018, TC021, TC025, TC027）
- [ ] TC115（TCP版）完了確認
- [ ] MockUdpServer作成（4Eフレーム対応、パディング対応）
- [ ] PlcResponseStubs作成（UDP用）

### TC116実装中
- [ ] Arrange: ConnectionConfig準備（UDP設定）
- [ ] Arrange: TimeoutConfig準備
- [ ] Arrange: SLMPフレーム準備（2つ）
- [ ] Arrange: MockUdpServer起動・応答データ設定
- [ ] Act: Step3（ConnectAsync - UDP）実行
- [ ] Act: Step4（SendFrameAsync, ReceiveResponseAsync）実行（2回）
- [ ] Act: Step5（DisconnectAsync）実行
- [ ] Assert: Step3検証（UDP接続成功）
- [ ] Assert: Step4検証（送受信成功、パディング考慮）
- [ ] Assert: Step5検証（UDP切断成功）
- [ ] Assert: 統計情報検証
- [ ] Assert: TCP版（TC115）との比較検証

### TC116実装後
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施
- [ ] TC118（Step6連続処理）への準備

---

## 12. 参考情報

### UDP通信処理時間（目安）
- Step3（UDP接続）: 10-50ms（疎通確認のみ）
- Step4（送信）: 5-30ms × 2回 = 10-60ms
- Step4（受信）: 50-200ms × 2回 = 100-400ms
- Step5（UDP切断）: 5-20ms（ソケットクローズのみ）
- **合計**: 125-530ms（通常200-300ms）

### TCP通信処理時間との比較
- **TCP合計**: 180-700ms（通常300-400ms）
- **UDP合計**: 125-530ms（通常200-300ms）
- **差分**: UDPの方が50-100ms程度短い（接続・切断が軽量）

**重要**: TC116ではモックUDPサーバーを使用、実機PLC不要

---

## 13. TCP版（TC115）との相違点まとめ

### 実装上の相違点
| 項目 | TCP（TC115） | UDP（TC116） |
|------|-------------|-------------|
| **ソケットタイプ** | SocketType.Stream | SocketType.Dgram |
| **プロトコル** | ProtocolType.Tcp | ProtocolType.Udp |
| **接続処理** | 3ウェイハンドシェイク | 疎通確認のみ |
| **接続時間** | 50-150ms | 10-50ms |
| **切断処理** | FINハンドシェイク | ソケットクローズのみ |
| **切断時間** | 10-50ms | 5-20ms |
| **データ境界** | なし（ストリーム） | あり（パケット単位） |
| **受信データ長** | 正確 | パディング可能性 |
| **順序保証** | あり | なし |
| **信頼性** | 高（再送あり） | 低（再送なし） |

### テスト実装の相違点
| 項目 | TCP（TC115） | UDP（TC116） |
|------|-------------|-------------|
| **データ長検証** | `Assert.Equal(250, data.Length)` | `Assert.True(data.Length >= 250)` |
| **接続時間検証** | `< 150ms` | `< 100ms` |
| **パディング検証** | なし | あり |
| **モックサーバー** | MockTcpServer | MockUdpServer |

---

以上が TC116_Step3to5_UDP完全サイクル正常動作テスト実装に必要な情報です。
