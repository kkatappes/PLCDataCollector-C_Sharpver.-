# Integration Step3to5 TCP完全サイクル テスト実装用情報（TC115）

## ドキュメント概要

### 目的
このドキュメントは、TC115_Step3to5_TCP完全サイクル正常動作テストの実装に必要な情報を集約したものです。
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
- `step3_TC017.md` - TCP接続テスト（Step3）
- `step3_TC021.md` - 送信テスト（Step4）
- `step3_TC025.md` - 受信テスト（Step4）
- `step3_TC027.md` - 切断テスト（Step5）

---

## 1. テスト対象機能仕様

### Step3to5サイクル（TCP接続→送受信→切断）
**統合テスト対象**: PlcCommunicationManager（Step3-5）
**名前空間**: andon.Core.Managers

#### Step3to5サイクル構成
```
Step3: PLC接続処理（ConnectAsync - TCP）
  ↓
Step4: PLCリクエスト送信（SendFrameAsync）
  ↓
Step4: PLCデータ受信（ReceiveResponseAsync）
  ↓
Step5: PLC切断処理（DisconnectAsync）
```

#### 統合Input
- ConnectionConfig（IpAddress="192.168.1.10", Port=5000, UseTcp=true, IsBinary=false, FrameVersion="4E"）
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
- TCP接続→送受信→切断の連続実行
- 統計情報の累積
- リソース管理（ソケット）

---

## 2. テストケース仕様（TC115）

### TC115_Step3to5_TCP完全サイクル正常動作
**目的**: TCP接続でのStep3-5完全サイクル実行を統合テスト

#### 前提条件
- ConfigToFrameManagerによる設定読み込み完了
- BuildFramesによるSLMPフレーム生成完了
- PLCシミュレータまたはモックTCPサーバーが稼働中
- ネットワーク到達可能

#### 入力データ
**ConnectionConfig（TCP接続設定）**:
```csharp
var connectionConfig = new ConnectionConfig
{
    IpAddress = "192.168.1.10",
    Port = 5000,
    UseTcp = true,
    ConnectionType = "TCP",
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
Assert.Equal(ProtocolType.Tcp, connectionResponse.Socket.ProtocolType);
Assert.NotNull(connectionResponse.RemoteEndPoint);
Assert.NotNull(connectionResponse.ConnectedAt);
Assert.NotNull(connectionResponse.ConnectionTime);
```

**PLCからの生データ（2つ）**:
```csharp
Assert.NotNull(mDeviceResponse);
Assert.NotNull(dDeviceResponse);
Assert.Equal(250, mDeviceResponse.Length);   // 125バイト × 2（ASCII）
Assert.Equal(4000, dDeviceResponse.Length);  // 2000バイト × 2（ASCII）
```

**ConnectionStats（統計情報）**:
```csharp
Assert.NotNull(connectionStats);
Assert.True(connectionStats.TotalResponseTime > TimeSpan.Zero);
Assert.Equal(0, connectionStats.TotalErrors);
Assert.Equal(1.0, connectionStats.SuccessRate);
```

#### 動作フロー成功条件
1. **Step3（TCP接続）成功**:
   - ConnectionResponse.Status == ConnectionStatus.Connected
   - Socket.Connected == true
   - Socket.ProtocolType == ProtocolType.Tcp
   - RemoteEndPoint == "192.168.1.10:5000"

2. **Step4（送信）成功**:
   - SendFrameAsync完了（例外なし）
   - 2つのフレーム送信成功

3. **Step4（受信）成功**:
   - ReceiveResponseAsync完了（例外なし）
   - 2つの応答データ受信成功
   - データ長が期待値と一致

4. **Step5（切断）成功**:
   - DisconnectAsync完了（例外なし）
   - Socket.Connected == false
   - 統計情報記録済み

---

## 3. Step別詳細仕様

### Step3: TCP接続処理（ConnectAsync）

#### 詳細情報
**参照**: step3_TC017.md（TCP接続成功テスト）

#### Input
- ConnectionConfig（IpAddress="192.168.1.10", Port=5000, UseTcp=true）
- TimeoutConfig（ConnectTimeoutMs=5000）

#### Output
- ConnectionResponse:
  - Status = ConnectionStatus.Connected
  - Socket != null（TCP用ソケット）
  - Socket.ProtocolType = ProtocolType.Tcp
  - Socket.SendTimeout = 3000ms
  - Socket.ReceiveTimeout = 3000ms
  - RemoteEndPoint = "192.168.1.10:5000"
  - ConnectedAt != null
  - ConnectionTime > TimeSpan.Zero

#### 検証ポイント
- TCP接続成功
- ソケットタイムアウト設定済み
- 接続時間が ConnectTimeoutMs 以内
- エンドポイント情報正確に記録

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

#### 検証ポイント
- 2つのフレーム連続送信成功
- 送信完了（例外なし）

---

### Step4: PLCデータ受信（ReceiveResponseAsync）

#### 詳細情報
**参照**: step3_TC025.md（正常受信テスト）

#### Input
- TimeoutConfig（ReceiveTimeoutMs=3000）

#### Output
- PLCからの生データ（16進数文字列）:
  - M機器応答: 125バイト、ASCII形式250文字
  - D機器応答: 2000バイト、ASCII形式4000文字

#### 検証ポイント
- 2つの応答データ受信成功
- 受信データ長が期待値と一致
- 受信時間が ReceiveTimeoutMs 以内

---

### Step5: PLC切断処理（DisconnectAsync）

#### 詳細情報
**参照**: step3_TC027.md（正常切断テスト）

#### Input
- 接続統計情報（ConnectionTime等）

#### Output
- 切断完了ステータス
- ConnectionStats（接続統計情報）

#### 検証ポイント
- TCP接続切断成功（Socket.Connected == false）
- 統計情報正確に記録
- ソケットリソース正常解放

---

## 4. 統合テスト実装構造

### Arrange（準備）

#### 1. 設定準備
```csharp
// ConnectionConfig準備（TCP）
var connectionConfig = new ConnectionConfig
{
    IpAddress = "192.168.1.10",
    Port = 5000,
    UseTcp = true,
    ConnectionType = "TCP",
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

#### 3. MockTcpServer準備
```csharp
var mockTcpServer = new MockTcpServer("192.168.1.10", 5000);
mockTcpServer.Start();

// M機器応答データ設定
mockTcpServer.SetResponse(slmpFrames[0], CreateMDeviceResponse());

// D機器応答データ設定
mockTcpServer.SetResponse(slmpFrames[1], CreateDDeviceResponse());
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
// Step3: TCP接続
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

// Step5: TCP切断
await plcCommManager.DisconnectAsync();

// 統計情報取得
var connectionStats = plcCommManager.GetConnectionStats();
```

---

### Assert（検証）

#### 1. Step3（TCP接続）検証
```csharp
// TCP接続成功検証
Assert.Equal(ConnectionStatus.Connected, connectionResponse.Status);
Assert.NotNull(connectionResponse.Socket);
Assert.True(connectionResponse.Socket.Connected);
Assert.Equal(ProtocolType.Tcp, connectionResponse.Socket.ProtocolType);

// TCP固有検証
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

// 受信データ長検証（TCP特有：正確なデータ長）
Assert.Equal(250, mDeviceResponse.Length);   // 125バイト × 2（ASCII）
Assert.Equal(4000, dDeviceResponse.Length);  // 2000バイト × 2（ASCII）

// 受信データ形式検証
Assert.Matches("^[0-9A-Fa-f]+$", mDeviceResponse);  // 16進数文字列
Assert.Matches("^[0-9A-Fa-f]+$", dDeviceResponse);  // 16進数文字列
```

#### 3. Step5（TCP切断）検証
```csharp
// TCP切断成功検証
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

## 5. TCP vs UDP の違い

### TCP（本テスト対象）
- **接続**: 3ウェイハンドシェイク（SYN → SYN-ACK → ACK）
- **ソケットタイプ**: SocketType.Stream
- **プロトコル**: ProtocolType.Tcp
- **信頼性**: 高（再送制御、順序保証）
- **コネクション**: 接続指向型
- **切断**: 明示的な切断処理（FIN → FIN-ACK → ACK）

### UDP（TC116で検証）
- **接続**: コネクションレス（疎通確認のみ）
- **ソケットタイプ**: SocketType.Dgram
- **プロトコル**: ProtocolType.Udp
- **信頼性**: 低（再送なし、順序保証なし）
- **コネクション**: コネクションレス型
- **切断**: 明示的な切断不要

---

## 6. エラーハンドリング

### Step3to5サイクルエラー処理（TC115では発生しない）

#### Step3エラー時
- ConnectionResponse.Status != Connected
- 以降のStep4-5はスキップ
- エラーログ出力
- ConnectionStats.TotalErrors++

#### Step4エラー時
- TimeoutException または SocketException
- 送受信失敗
- Step5（切断）実行後、処理終了
- ConnectionStats.TotalErrors++

#### Step5エラー時
- DisconnectAsync内で例外発生
- エラーログ出力
- リソース強制解放試行

---

## 7. テスト実装方針（TDD）

### 開発手法
- C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.mdに記載のTDD手法を使用

### テストファイル配置
- **ファイル名**: PlcCommunicationManager_IntegrationTests.cs
- **配置先**: Tests/Integration/
- **名前空間**: andon.Tests.Integration

### テスト実装順序
1. **TC017, TC021, TC025, TC027**: Step3-5の単体テスト（完了済み）
2. **TC115**: Step3to5 TCP完全サイクル（本テスト）
3. TC116: Step3to5 UDP完全サイクル

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockTcpServer**: TCP接続対応PLCシミュレータ
  - SLMPフレーム受信→応答返却機能
  - 4Eフレーム対応
  - 遅延シミュレーション機能

#### 使用するスタブ
- **ConnectionConfigStubs**: TCP接続設定スタブ
- **TimeoutConfigStubs**: タイムアウト設定スタブ
- **SlmpFrameStubs**: SLMPフレームスタブ（M機器用、D機器用）
- **PlcResponseStubs**: PLC応答データスタブ（TCP用）

---

## 8. 依存クラス・設定

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

## 9. ログ出力要件

### LoggingManager連携
- **TCP接続開始ログ**: 接続先情報
- **TCP接続完了ログ**: 接続時間
- **送信ログ**: フレーム情報
- **受信ログ**: データ長
- **TCP切断ログ**: 統計情報

### ログレベル
- **Information**: 各ステップ開始・完了
- **Debug**: 詳細情報（ソケット設定等）

### ログ出力例
```
[Information] Step3開始: TCP接続処理 - 192.168.1.10:5000
[Debug] TCP接続設定: SendTimeout=3000ms, ReceiveTimeout=3000ms
[Information] Step3完了: TCP接続成功, 接続時間=120ms
[Information] Step4開始: M機器フレーム送信
[Information] Step4完了: M機器送信成功
[Information] Step4開始: M機器応答受信
[Information] Step4完了: M機器受信成功, データ長=250文字
[Information] Step4開始: D機器フレーム送信
[Information] Step4完了: D機器送信成功
[Information] Step4開始: D機器応答受信
[Information] Step4完了: D機器受信成功, データ長=4000文字
[Information] Step5開始: TCP切断処理
[Information] Step5完了: TCP切断成功
[Information] Step3to5サイクル完了: 合計時間=350ms, 成功率=100%
```

---

## 10. テスト実装チェックリスト

### TC115実装前
- [ ] 単体テスト完了確認（TC017, TC021, TC025, TC027）
- [ ] MockTcpServer作成（4Eフレーム対応）
- [ ] PlcResponseStubs作成（TCP用）

### TC115実装中
- [ ] Arrange: ConnectionConfig準備（TCP設定）
- [ ] Arrange: TimeoutConfig準備
- [ ] Arrange: SLMPフレーム準備（2つ）
- [ ] Arrange: MockTcpServer起動・応答データ設定
- [ ] Act: Step3（ConnectAsync - TCP）実行
- [ ] Act: Step4（SendFrameAsync, ReceiveResponseAsync）実行（2回）
- [ ] Act: Step5（DisconnectAsync）実行
- [ ] Assert: Step3検証（TCP接続成功）
- [ ] Assert: Step4検証（送受信成功）
- [ ] Assert: Step5検証（TCP切断成功）
- [ ] Assert: 統計情報検証

### TC115実装後
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施
- [ ] TC116（UDP完全サイクル）への準備

---

## 11. 参考情報

### TCP通信処理時間（目安）
- Step3（TCP接続）: 50-150ms（3ウェイハンドシェイク含む）
- Step4（送信）: 10-50ms × 2回 = 20-100ms
- Step4（受信）: 50-200ms × 2回 = 100-400ms
- Step5（TCP切断）: 10-50ms（FINハンドシェイク含む）
- **合計**: 180-700ms（通常300-400ms）

**重要**: TC115ではモックTCPサーバーを使用、実機PLC不要

---

以上が TC115_Step3to5_TCP完全サイクル正常動作テスト実装に必要な情報です。
