# TC017_ConnectAsync_TCP接続成功テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC017_ConnectAsync_TCP接続成功テストケースを、TDD手法に従って実装してください。

---

## 実装概要

### 目的
PlcCommunicationManager.ConnectAsync()メソッドのテストケースTC017を実装します。
このテストは、PLCへのTCP接続機能が正常に動作することを検証します。

### 実装対象
- **テストファイル**: `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`
- **テスト名前空間**: `andon.Tests.Unit.Core.Managers`
- **テストメソッド名**: `TC017_ConnectAsync_TCP接続成功`

---

## 前提条件の確認

実装開始前に以下を確認してください：

1. **依存ファイルの存在確認**
   - `Core/Managers/PlcCommunicationManager.cs` (空実装可)
   - `Core/Interfaces/IPlcCommunicationManager.cs`
   - `Core/Models/ConnectionResponse.cs`
   - `Core/Models/ConnectionConfig.cs`
   - `Core/Models/TimeoutConfig.cs`
   - `Core/Models/ConnectionStatus.cs`（列挙型）

2. **テストユーティリティの確認**
   - `Tests/TestUtilities/Mocks/` 配下のモッククラス
   - `Tests/TestUtilities/Stubs/` 配下のスタブクラス

3. **開発手法ドキュメント確認**
   - `C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.md`を参照

不足しているファイルがあれば報告してください。

---

## 実装手順（TDD Red-Green-Refactor）

### Phase 1: Red（テスト失敗）

#### Step 1-1: テストファイル作成
```
ファイル: Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs
名前空間: andon.Tests.Unit.Core.Managers
```

#### Step 1-2: テストケース実装

**TC017: TCP接続成功テスト**

**Arrange（準備）**:
- ConnectionConfigを作成
  - IpAddress = "192.168.1.10"
  - Port = 5000
  - UseTcp = true
  - ConnectionType = ConnectionType.Ethernet
  - IsBinary = false
  - FrameVersion = FrameVersion.Frame4E
- TimeoutConfigを作成
  - ConnectTimeoutMs = 5000
  - SendTimeoutMs = 3000
  - ReceiveTimeoutMs = 3000
- MockSocketFactoryを作成（TCP接続成功をシミュレート）
- PlcCommunicationManagerインスタンス作成（モック注入）

**Act（実行）**:
- `var result = await manager.ConnectAsync(connectionConfig, timeoutConfig);`

**Assert（検証）**:
- `result != null`
- `result.Status == ConnectionStatus.Connected`（接続成功）
- `result.Socket != null`（Socketインスタンス取得）
- `result.RemoteEndPoint != null`
- `result.RemoteEndPoint.ToString() == "192.168.1.10:5000"`（正確なエンドポイント記録）
- `result.ConnectedAt != default(DateTime)`（接続完了時刻記録済み）
- `result.ConnectionTime.TotalMilliseconds > 0 && result.ConnectionTime.TotalMilliseconds < timeoutConfig.ConnectTimeoutMs`（接続処理時間が妥当範囲）
- `result.ErrorMessage == null`（成功時はエラーメッセージなし）

#### Step 1-3: テスト実行（Red確認）
```bash
dotnet test --filter "FullyQualifiedName~TC017"
```

期待結果: テスト失敗（ConnectAsyncが未実装のため）

---

### Phase 2: Green（最小実装）

#### Step 2-1: ConnectAsync最小実装

**実装箇所**: `Core/Managers/PlcCommunicationManager.cs`

**最小実装要件**:
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

        // 3. Socket作成
        Socket socket;
        if (config.UseTcp)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        else
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        // 4. タイムアウト設定
        socket.SendTimeout = timeout.SendTimeoutMs;
        socket.ReceiveTimeout = timeout.ReceiveTimeoutMs;

        // 5. 接続実行（タイムアウト付き）
        var connectTask = socket.ConnectAsync(config.IpAddress, config.Port);
        if (await Task.WhenAny(connectTask, Task.Delay(timeout.ConnectTimeoutMs)) == connectTask)
        {
            await connectTask; // 例外がある場合はここでスロー
        }
        else
        {
            socket.Dispose();
            throw new TimeoutException($"接続タイムアウト: {config.IpAddress}:{config.Port}（タイムアウト時間: {timeout.ConnectTimeoutMs}ms）");
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
    catch (SocketException ex)
    {
        var connectionTime = DateTime.Now - startTime;
        return new ConnectionResponse
        {
            Status = ConnectionStatus.Failed,
            Socket = null,
            RemoteEndPoint = null,
            ConnectedAt = default(DateTime),
            ConnectionTime = connectionTime,
            ErrorMessage = $"接続失敗: {ex.Message}"
        };
    }
}
```

**必要なフィールド**:
```csharp
private ConnectionResponse? _connectionResponse;
private Socket? _socket;
```

#### Step 2-2: テスト再実行（Green確認）
```bash
dotnet test --filter "FullyQualifiedName~TC017"
```

期待結果: すべてのテストがパス

---

### Phase 3: Refactor（リファクタリング）

#### Step 3-1: コード品質向上
- ログ出力追加（LoggingManager連携）
  - 接続開始ログ: IP・ポート情報、プロトコル種別
  - 接続完了ログ: 接続時間、エンドポイント情報
  - エラーログ: 例外詳細、スタックトレース
- エラーハンドリング強化
- ドキュメントコメント追加

#### Step 3-2: テスト再実行（Green維持確認）
```bash
dotnet test --filter "FullyQualifiedName~TC017"
```

期待結果: すべてのテストがパス（リファクタリング後も）

---

## 技術仕様詳細

### TCP接続処理

**接続手順**:
1. Socket作成（TCP: SocketType.Stream, ProtocolType.Tcp）
2. タイムアウト設定（SendTimeout, ReceiveTimeout）
3. ConnectAsync実行（非同期接続）
4. タイムアウト監視（Task.WhenAny使用）
5. 接続完了確認（RemoteEndPoint設定確認）

**検証項目**:
- Socket.Connected状態がtrue
- RemoteEndPointが設定済み
- 接続時間が妥当範囲内（0ms < time < ConnectTimeoutMs）

### ConnectionResponseオブジェクト

**成功時のプロパティ**:
```csharp
{
    Status = ConnectionStatus.Connected,
    Socket = <接続済みSocketインスタンス>,
    RemoteEndPoint = <IPEndPoint: "192.168.1.10:5000">,
    ConnectedAt = <接続完了時刻>,
    ConnectionTime = <接続処理時間>,
    ErrorMessage = null
}
```

### エラーハンドリング

**スロー例外**:
- `TimeoutException`: 接続タイムアウト（ConnectTimeoutMs超過）
- `SocketException`: ソケットエラー（ネットワーク不通、接続拒否等）
- `InvalidOperationException`: 既に接続済み状態での再接続試行
- `ArgumentNullException`: config/timeoutがnull

**エラーメッセージ定数**（Core/Constants/ErrorMessages.cs）:
```csharp
public const string AlreadyConnected = "既に接続済みです。再接続する場合は先にDisconnectAsync()を実行してください。";
public const string ConfigOrTimeoutNull = "ConnectionConfigまたはTimeoutConfigがnullです。";
public const string ConnectionTimeout = "接続タイムアウト: {0}:{1}（タイムアウト時間: {2}ms）";
public const string ConnectionFailed = "接続失敗: {0}:{1}";
```

### モック・スタブ実装

**MockSocketFactory**:
- TCP接続成功をシミュレート
- RemoteEndPoint設定
- Connected状態をtrue

**MockPlcCommunicationManager**（必要に応じて）:
- ConnectAsync結果をモック

---

## 実装記録・ドキュメント作成要件

### 必須作業項目

#### 1. 進捗記録開始
**ファイル**: `documents/implementation_records/progress_notes/2025-11-06_TC017実装.md`
- 実装開始時刻
- 目標（TC017テスト実装完了）
- 実装方針

#### 2. 実装記録作成
**ファイル**: `documents/implementation_records/method_records/ConnectAsync実装記録.md`
- 実装判断根拠
  - なぜこの実装方法を選択したか
  - 検討した他の方法との比較
  - 技術選択の根拠とトレードオフ
- 発生した問題と解決過程

#### 3. テスト結果保存
**ファイル**: `documents/implementation_records/execution_logs/TC017_テスト結果.log`
- 単体テスト結果（成功/失敗、実行時間）
- Red-Green-Refactorの各フェーズ結果
- エラーログとデバッグ情報

---

## 完了条件

以下すべてが満たされた時点で実装完了とする：

- [ ] TC017テストがパス
- [ ] ConnectAsync本体実装完了（TCP接続対応）
- [ ] リファクタリング完了（ログ出力、エラーハンドリング等）
- [ ] テスト再実行でGreen維持確認
- [ ] 進捗記録作成完了
- [ ] 実装記録作成完了
- [ ] C:\Users\1010821\Desktop\python\andon\documents\design\チェックリスト\step3to6_test実施リスト.mdの該当項目にチェック

---

## 実装時の注意点

### TDD手法厳守
- 必ずテストを先に書く（Red）
- 最小実装でテストをパスさせる（Green）
- リファクタリングで品質向上（Refactor）
- 各フェーズでテスト実行を確認

### 記録の重要性
- 実装判断の根拠を詳細に記録
- テスト結果は数値データも含めて保存

### 文字化け対策
- 日本語ファイル名の新規作成時は`.txt`経由で作成
- 作成後は必ずReadツールで確認
- 文字化け発見時は早期に対処

---

## 参考情報

### 設計書参照先
- `documents/design/クラス設計.md`
- `documents/design/テスト内容.md`（TC017詳細: 282-295行）
- `documents/design/エラーハンドリング.md`

### 開発手法
- `documents/development_methodology/development-methodology.md`

### 関連テストケース
- TC017_1: ソケットタイムアウト設定確認（連続実施推奨）
- TC018: UDP接続成功

---

以上の指示に従って、TC017_ConnectAsync_TCP接続成功テストの実装を開始してください。

不明点や不足情報があれば、実装前に質問してください。
