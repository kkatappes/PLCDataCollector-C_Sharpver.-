# TC018_ConnectAsync_UDP接続成功テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC018_ConnectAsync_UDP接続成功テストケースを、TDD手法に従って実装してください。

---

## 実装概要

### 目的
PlcCommunicationManager.ConnectAsync()メソッドのUDP接続機能を検証します。
UDP疎通確認方法（TDD・オフライン環境対応）を含みます。

### 実装対象
- **テストファイル**: `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`
- **テスト名前空間**: `andon.Tests.Unit.Core.Managers`
- **テストメソッド名**: `TC018_ConnectAsync_UDP接続成功`

---

## 前提条件の確認

実装開始前に以下を確認してください：

1. **依存ファイルの存在確認**
   - `Core/Managers/PlcCommunicationManager.cs` (TCP実装済み)
   - `Core/Interfaces/IPlcCommunicationManager.cs`
   - `Core/Models/ConnectionResponse.cs`
   - `Core/Models/ConnectionConfig.cs`
   - `Core/Models/TimeoutConfig.cs`
   - `Core/Models/ConnectionStatus.cs`（列挙型）

2. **前提テスト確認**
   - TC017_ConnectAsync_TCP接続成功が実装済み・テストパス済みであること

3. **開発手法ドキュメント確認**
   - `C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.md`を参照

不足しているファイルがあれば報告してください。

---

## 実装手順（TDD Red-Green-Refactor）

### Phase 1: Red（テスト失敗）

#### Step 1-1: テストケース実装

**TC018: UDP接続成功テスト**

**Arrange（準備）**:
- ConnectionConfigを作成
  - IpAddress = "192.168.1.10"
  - Port = 5000
  - **UseTcp = false（UDP接続モード）**
  - **ConnectionType = "UDP"**
  - IsBinary = false
  - FrameVersion = FrameVersion.Frame4E
- TimeoutConfigを作成
  - ConnectTimeoutMs = 5000
  - SendTimeoutMs = 3000
  - ReceiveTimeoutMs = 3000
- **疎通確認用模擬フレーム準備**
  - M000-M999読み込みフレーム: "54001234000000010401006400000090E8030000"
  - または D000-D999読み込みフレーム: "54001234000000010400A800000090E8030000"
- MockUdpServerを作成（UDP疎通確認対応）
- PlcCommunicationManagerインスタンス作成

**Act（実行）**:
- `var result = await manager.ConnectAsync(connectionConfig, timeoutConfig);`

**Assert（検証）**:
- `result != null`
- `result.Status == ConnectionStatus.Connected`（接続成功）
- `result.Socket != null`（Socketインスタンス取得）
- **`result.Socket.Connected == false`（UDP接続の正常動作、重要）**
- `result.Socket.ProtocolType == ProtocolType.Udp`（UDPプロトコル）
- `result.RemoteEndPoint != null`
- `result.RemoteEndPoint.ToString() == "192.168.1.10:5000"`
- `result.ConnectedAt != default(DateTime)`
- `result.ConnectionTime.TotalMilliseconds > 0 && result.ConnectionTime.TotalMilliseconds < timeoutConfig.ConnectTimeoutMs`（疎通確認含む）
- `result.ErrorMessage == null`

#### Step 1-2: テスト実行（Red確認）
```bash
dotnet test --filter "FullyQualifiedName~TC018"
```

期待結果: テスト失敗（UDP接続処理が未実装のため）

---

### Phase 2: Green（最小実装）

#### Step 2-1: ConnectAsync UDP対応実装

**実装箇所**: `Core/Managers/PlcCommunicationManager.cs`

**UDP接続実装要件**:
```csharp
public async Task<ConnectionResponse> ConnectAsync(ConnectionConfig config, TimeoutConfig timeout)
{
    var startTime = DateTime.Now;

    try
    {
        // 1. 既存接続チェック
        if (_connectionResponse?.Status == ConnectionStatus.Connected)
        {
            throw new InvalidOperationException(ErrorMessages.AlreadyConnected);
        }

        // 2. 入力検証
        if (config == null || timeout == null)
        {
            throw new ArgumentNullException(ErrorMessages.ConfigOrTimeoutNull);
        }

        // 3. Socket作成（TCP/UDP分岐）
        Socket socket;
        if (config.UseTcp)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        else
        {
            // UDP用ソケット作成
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        // 4. タイムアウト設定
        socket.SendTimeout = timeout.SendTimeoutMs;
        socket.ReceiveTimeout = timeout.ReceiveTimeoutMs;

        // 5. 接続実行
        IPAddress ipAddress = IPAddress.Parse(config.IpAddress);
        IPEndPoint endPoint = new IPEndPoint(ipAddress, config.Port);

        if (config.UseTcp)
        {
            // TCP接続（TC017実装済み）
            var cts = new CancellationTokenSource(timeout.ConnectTimeoutMs);
            await socket.ConnectAsync(endPoint, cts.Token);
        }
        else
        {
            // UDP接続（送信先設定のみ）
            socket.Connect(endPoint);

            // UDP疎通確認（模擬フレーム送受信）
            var verificationFrame = "54001234000000010401006400000090E8030000"; // M000-M999読み込み
            byte[] frameBytes = ConvertHexStringToBytes(verificationFrame);

            // フレーム送信
            int sentBytes = socket.Send(frameBytes);

            // 応答受信（ConnectTimeoutMs内）
            byte[] receiveBuffer = new byte[1024];
            var cts = new CancellationTokenSource(timeout.ConnectTimeoutMs);
            int receivedBytes = await socket.ReceiveAsync(
                new ArraySegment<byte>(receiveBuffer),
                SocketFlags.None,
                cts.Token);
        }

        // 6. ConnectionResponse作成
        var connectionTime = DateTime.Now - startTime;
        var response = new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            Socket = socket,
            RemoteEndPoint = socket.RemoteEndPoint,
            ConnectedAt = DateTime.Now,
            ConnectionTime = connectionTime,
            ErrorMessage = null
        };

        _connectionResponse = response;
        _socket = socket;

        return response;
    }
    catch (OperationCanceledException)
    {
        // UDP疎通確認タイムアウト
        var connectionTime = DateTime.Now - startTime;
        throw new TimeoutException($"UDP疎通確認タイムアウト: {config.IpAddress}:{config.Port}（タイムアウト時間: {timeout.ConnectTimeoutMs}ms）");
    }
    catch (SocketException ex)
    {
        var connectionTime = DateTime.Now - startTime;
        throw new SocketException($"UDP接続失敗: {config.IpAddress}:{config.Port} - {ex.Message}");
    }
}
```

**必要なヘルパーメソッド**:
```csharp
private byte[] ConvertHexStringToBytes(string hexString)
{
    byte[] bytes = new byte[hexString.Length / 2];
    for (int i = 0; i < bytes.Length; i++)
    {
        bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
    }
    return bytes;
}
```

#### Step 2-2: テスト再実行（Green確認）
```bash
dotnet test --filter "FullyQualifiedName~TC018"
```

期待結果: すべてのテストがパス

---

### Phase 3: Refactor（リファクタリング）

#### Step 3-1: コード品質向上
- ログ出力追加（LoggingManager連携）
  - UDP接続開始ログ: IP・ポート情報、UDP疎通確認開始
  - 疎通確認ログ: 模擬フレーム送信、応答受信確認
  - 接続完了ログ: 接続時間（疎通確認含む）、エンドポイント情報
  - エラーログ: 例外詳細、スタックトレース
- エラーハンドリング強化
- ドキュメントコメント追加

#### Step 3-2: テスト再実行（Green維持確認）
```bash
dotnet test --filter "FullyQualifiedName~TC018"
```

期待結果: すべてのテストがパス（リファクタリング後も）

---

## 技術仕様詳細

### UDP接続処理

**UDP接続の特性**:
- **コネクションレス型通信**: TCPのような接続確立手順（3ウェイハンドシェイク）なし
- **Socket.Connect()の意味**: 送信先アドレス・ポートの設定のみ（実際の接続確立なし）
- **Socket.Connected**: UDP接続ではfalseが正常動作（重要）
- **疎通確認の必要性**: 実際の通信可能性を確認するため、模擬フレーム送受信実施

**接続手順**:
1. Socket作成（UDP: SocketType.Dgram, ProtocolType.Udp）
2. タイムアウト設定（SendTimeout, ReceiveTimeout）
3. Connect実行（送信先設定のみ）
4. 疎通確認（模擬フレーム送受信）
5. 応答受信確認（ConnectTimeoutMs以内）

**検証項目**:
- Socket.Connected状態がfalse（UDP正常動作）
- RemoteEndPointが設定済み
- 疎通確認成功（応答受信完了）
- 接続時間が妥当範囲内（疎通確認含む）

### UDP疎通確認方法（TDD・オフライン環境対応）

**テスト実施方法**:
- PLCシミュレータまたはネットワークモックを使用
- 模擬送信フレーム（M000-M999またはD000-D999読み込み）送信
- 模擬応答データ: モックがConnectTimeoutMs内に正常応答を返却
- **検証項目**: Socket.Connected状態（false）、RemoteEndPoint設定、応答受信完了の確認
- **重要**: 実際のPLC機器不要、完全オフライン環境でのテスト実施

**疎通確認用模擬フレーム**:
- M000-M999読み込みフレーム: "54001234000000010401006400000090E8030000"
  - 構成: サブヘッダ(54001234000000) + READコマンド(0104) + サブコマンド(0100:ビット単位) + デバイスコード(6400:M機器) + 開始番号(00000090:M000) + デバイス点数(E8030000:1000点)
- D000-D999読み込みフレーム: "54001234000000010400A800000090E8030000"
  - 構成: サブヘッダ(54001234000000) + READコマンド(0104) + サブコマンド(0000:ワード単位) + デバイスコード(A800:D機器) + 開始番号(00000090:D000) + デバイス点数(E8030000:1000点)

### ConnectionResponseオブジェクト

**UDP接続成功時のプロパティ**:
```csharp
{
    Status = ConnectionStatus.Connected,
    Socket = <接続済みUDPソケットインスタンス>,
    RemoteEndPoint = <IPEndPoint: "192.168.1.10:5000">,
    ConnectedAt = <接続完了時刻>,
    ConnectionTime = <接続処理時間（疎通確認含む）>,
    ErrorMessage = null
}
```

**TCP接続との違い**:
- Socket.Connected = false（UDP正常動作）
- Socket.ProtocolType = ProtocolType.Udp
- 疎通確認処理を含む

### エラーハンドリング

**スロー例外**:
- `TimeoutException`: UDP疎通確認タイムアウト（ConnectTimeoutMs超過）
- `SocketException`: ネットワークエラー
- `ArgumentException`: 不正なIPアドレス・ポート番号
- `InvalidOperationException`: 既に接続済み状態での再接続試行
- `ArgumentNullException`: config/timeoutがnull

**エラーメッセージ定数**（Core/Constants/ErrorMessages.cs）:
```csharp
// UDP接続関連
public const string UdpVerificationTimeout = "UDP疎通確認タイムアウト: {0}:{1}（タイムアウト時間: {2}ms）";
public const string UdpConnectionFailed = "UDP接続失敗: {0}:{1} - {2}";

// 共通接続関連
public const string AlreadyConnected = "既に接続済みです。再接続する場合は先にDisconnectAsync()を実行してください";
public const string ConfigOrTimeoutNull = "ConnectionConfigまたはTimeoutConfigがnullです。";
```

### モック・スタブ実装

**MockSocket（UDP対応）**:
- ProtocolType.Udp返却
- Connected = false返却（UDP正常動作）
- RemoteEndPoint設定
- Send(), ReceiveAsync()実装

**MockUdpServer**:
- 指定IPアドレス・ポートで待ち受け
- 模擬フレーム受信時に即座に応答（ConnectTimeoutMs以内）
- 完全オフライン動作

---

## 実装記録・ドキュメント作成要件

### 必須作業項目

#### 1. 進捗記録開始
**ファイル**: `documents/implementation_records/progress_notes/2025-11-06_TC018実装.md`
- 実装開始時刻
- 目標（TC018テスト実装完了）
- 実装方針（UDP接続対応）

#### 2. 実装記録作成
**ファイル**: `documents/implementation_records/method_records/ConnectAsync_UDP実装記録.md`
- 実装判断根拠
  - なぜUDP疎通確認を実装したか
  - TCP実装との比較
  - 技術選択の根拠とトレードオフ
- 発生した問題と解決過程

#### 3. テスト結果保存
**ファイル**: `documents/implementation_records/execution_logs/TC018_テスト結果.log`
- 単体テスト結果（成功/失敗、実行時間）
- Red-Green-Refactorの各フェーズ結果
- 疎通確認動作確認結果
- エラーログとデバッグ情報

---

## 完了条件

以下すべてが満たされた時点で実装完了とする：

- [ ] TC018テストがパス
- [ ] ConnectAsync本体実装完了（UDP接続対応）
- [ ] UDP疎通確認処理実装完了
- [ ] リファクタリング完了（ログ出力、エラーハンドリング等）
- [ ] テスト再実行でGreen維持確認
- [ ] TC017（TCP接続）も引き続きパス（回帰テスト）
- [ ] 進捗記録作成完了
- [ ] 実装記録作成完了

---

## 実装時の注意点

### TDD手法厳守
- 必ずテストを先に書く（Red）
- 最小実装でテストをパスさせる（Green）
- リファクタリングで品質向上（Refactor）
- 各フェーズでテスト実行を確認

### UDP接続特有の注意点
- Socket.Connected = false が正常動作（TCPとは異なる）
- 疎通確認の実装が必須（実際の通信可能性確認）
- モックUdpServerでの応答シミュレーション

### 記録の重要性
- 実装判断の根拠を詳細に記録
- TCP実装との比較を記録
- テスト結果は数値データも含めて保存

### 文字化け対策
- 日本語ファイル名の新規作成時は`.txt`経由で作成
- 作成後は必ずReadツールで確認
- 文字化け発見時は早期に対処

---

## 参考情報

### 設計書参照先
- `documents/design/クラス設計.md`
- `documents/design/テスト内容.md`（TC018詳細: 304-323行）
- `documents/design/エラーハンドリング.md`

### 開発手法
- `documents/development_methodology/development-methodology.md`

### UDP/IP通信仕様
- プロトコル: UDP/IP（User Datagram Protocol / Internet Protocol）
- **コネクションレス型通信**: 接続確立手順なし
- **Socket.Connect()の意味**: 送信先設定のみ、実際の接続なし
- **疎通確認の必要性**: 実際の通信可能性を確認するため
- ポート範囲: 1-65535

### UDP vs TCP比較
| 項目 | TCP | UDP |
|------|-----|-----|
| 接続確立 | 3ウェイハンドシェイク | なし |
| Socket.Connected | true（接続後） | false（常に） |
| 信頼性 | 高（再送制御あり） | 低（再送なし） |
| 速度 | 比較的遅い | 速い |
| 用途 | 確実な通信 | リアルタイム通信 |

**重要**: TC018ではモックUDPサーバーを使用、実機PLC不要

### 関連テストケース
- TC017: TCP接続成功（前提テスト）
- TC019: 接続タイムアウト（UDP疎通確認タイムアウトも含む）

---

以上の指示に従って、TC018_ConnectAsync_UDP接続成功テストの実装を開始してください。

不明点や不足情報があれば、実装前に質問してください。
