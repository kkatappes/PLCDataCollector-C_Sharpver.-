# TC020_ConnectAsync_接続拒否テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC020_ConnectAsync_接続拒否テストケースを、TDD手法に従って実装してください。

---

## 実装概要

### 目的
ConnectAsync()メソッドが、接続拒否される場合に適切に例外処理を行うことを検証します。

### 実装対象
- **テストファイル**: `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`
- **テスト名前空間**: `andon.Tests.Unit.Core.Managers`
- **テストメソッド名**: `TC020_ConnectAsync_接続拒否`

---

## 前提条件

実装開始前に以下を確認してください：

1. **依存ファイルの存在確認**
   - `Core/Managers/PlcCommunicationManager.cs` (TCP/UDP実装済み、タイムアウト処理済み)
   - `Core/Interfaces/IPlcCommunicationManager.cs`
   - `Core/Models/ConnectionResponse.cs`
   - `Core/Constants/ErrorMessages.cs`

2. **前提テスト確認**
   - TC017_ConnectAsync_TCP接続成功が実装済み・テストパス済みであること
   - TC018_ConnectAsync_UDP接続成功が実装済み・テストパス済みであること
   - TC019_ConnectAsync_接続タイムアウトが実装済み・テストパス済みであること

不足しているファイルがあれば報告してください。

---

## 実装手順（TDD Red-Green-Refactor）

### Phase 1: Red（テスト失敗）

#### Step 1-1: テストケース実装

**TC020: 接続拒否テスト**

**Arrange（準備）**:
- ConnectionConfigを作成
  - IpAddress = "192.168.1.10"
  - **Port = 9999（接続拒否するポート）**
  - UseTcp = true
  - ConnectionType = "TCP"
- TimeoutConfigを作成
  - ConnectTimeoutMs = 5000
  - SendTimeoutMs = 3000
  - ReceiveTimeoutMs = 3000
- PlcCommunicationManagerインスタンス作成

**Act & Assert（実行・検証）**:
```csharp
// SocketException がスローされることを確認
var exception = await Assert.ThrowsAsync<SocketException>(async () =>
    await manager.ConnectAsync(config, timeout));

// 例外メッセージ確認
Assert.Contains("接続拒否", exception.Message);
Assert.Contains("192.168.1.10:9999", exception.Message);
```

**追加検証項目**:
- リソースクリーンアップ確認（ソケットDispose済み）
- 接続状態確認（NotConnected または Failed）
- SocketException.SocketErrorCode確認（ConnectionRefused）

#### Step 1-2: テスト実行（Red確認）
```bash
dotnet test --filter "FullyQualifiedName~TC020"
```

期待結果: テスト失敗（接続拒否例外が適切にハンドリングされていない）

---

### Phase 2: Green（最小実装）

#### Step 2-1: ConnectAsync 接続拒否処理実装

**実装箇所**: `Core/Managers/PlcCommunicationManager.cs`

**接続拒否処理実装**:
```csharp
public async Task<ConnectionResponse> ConnectAsync(ConnectionConfig config, TimeoutConfig timeout)
{
    var startTime = DateTime.Now;
    Socket socket = null;
    CancellationTokenSource cts = null;

    try
    {
        // 既存チェック・入力検証省略...

        // Socket作成
        if (config.UseTcp)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        else
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        // タイムアウト設定
        socket.SendTimeout = timeout.SendTimeoutMs;
        socket.ReceiveTimeout = timeout.ReceiveTimeoutMs;

        // エンドポイント作成
        IPAddress ipAddress = IPAddress.Parse(config.IpAddress);
        IPEndPoint endPoint = new IPEndPoint(ipAddress, config.Port);

        // タイムアウト制御付き接続実行
        cts = new CancellationTokenSource(timeout.ConnectTimeoutMs);

        if (config.UseTcp)
        {
            try
            {
                await socket.ConnectAsync(endPoint, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // TC019: タイムアウト
                socket?.Dispose();
                throw new TimeoutException(
                    string.Format(ErrorMessages.ConnectionTimeout,
                        config.IpAddress,
                        config.Port,
                        timeout.ConnectTimeoutMs));
            }
            catch (SocketException ex)
            {
                // TC020: 接続拒否
                socket?.Dispose();

                // エラーコード判定
                if (ex.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    throw new SocketException(
                        string.Format(ErrorMessages.ConnectionRefused,
                            config.IpAddress,
                            config.Port));
                }
                else
                {
                    // その他のソケットエラー
                    throw new SocketException(
                        string.Format(ErrorMessages.ConnectionFailed,
                            config.IpAddress,
                            config.Port,
                            ex.Message));
                }
            }
        }
        else
        {
            // UDP処理（既存実装）
            socket.Connect(endPoint);
            // UDP疎通確認...
        }

        // 接続成功処理...
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
    finally
    {
        cts?.Dispose();
    }
}
```

**必要なErrorMessages定数追加**:
```csharp
// Core/Constants/ErrorMessages.cs
public const string ConnectionRefused = "接続拒否: {0}:{1}";
public const string ConnectionFailed = "接続失敗: {0}:{1} - {2}";
```

#### Step 2-2: テスト再実行（Green確認）
```bash
dotnet test --filter "FullyQualifiedName~TC020"
```

期待結果: すべてのテストがパス

---

### Phase 3: Refactor（リファクタリング）

#### Step 3-1: コード品質向上
- ログ出力追加（LoggingManager連携）
  - 接続開始ログ: IP・ポート情報
  - エラーログ: SocketException詳細、SocketErrorCode、スタックトレース
- エラーハンドリング強化
  - SocketErrorCodeによる詳細な分類
  - 適切なエラーメッセージ生成
- ドキュメントコメント追加

#### Step 3-2: テスト再実行（Green維持確認）
```bash
dotnet test --filter "FullyQualifiedName~TC020"
```

期待結果: すべてのテストがパス（リファクタリング後も）

---

## 技術仕様詳細

### 接続拒否の発生条件

**接続拒否が発生する状況**:
- 指定されたポートでサービスがリスニングしていない
- ファイアウォールによって接続がブロックされている
- ホストは到達可能だがポートが閉じている

**SocketException.SocketErrorCode**:
- `SocketError.ConnectionRefused`: 接続拒否
- `SocketError.NetworkUnreachable`: ネットワーク到達不可能
- `SocketError.HostUnreachable`: ホスト到達不可能
- `SocketError.TimedOut`: タイムアウト（TC019とは異なる低レベルタイムアウト）

### 接続拒否検出メカニズム

**SocketExceptionによる検出**:
```csharp
try
{
    await socket.ConnectAsync(endPoint, cts.Token);
}
catch (SocketException ex)
{
    if (ex.SocketErrorCode == SocketError.ConnectionRefused)
    {
        // TC020: 接続拒否
        throw new SocketException("接続拒否: ...");
    }
    else
    {
        // その他のソケットエラー
        throw;
    }
}
```

**エラーコード別処理**:
| SocketErrorCode | 意味 | 処理 |
|-----------------|------|------|
| ConnectionRefused | 接続拒否 | TC020で検証、専用メッセージ |
| NetworkUnreachable | ネットワーク不通 | 一般的なエラーメッセージ |
| HostUnreachable | ホスト不通 | 一般的なエラーメッセージ |
| TimedOut | タイムアウト | TC019と区別 |

### リソースクリーンアップ

**重要**: 接続拒否発生時も確実にリソースを解放

```csharp
try
{
    // 接続処理
}
catch (SocketException ex)
{
    socket?.Dispose(); // ソケット解放
    throw; // 例外再スロー
}
finally
{
    cts?.Dispose(); // CancellationTokenSource解放
}
```

### 例外処理フロー

```
ConnectAsync実行
    ↓
接続試行
    ↓
ポートが閉じている
    ↓
SocketException発生（ConnectionRefused）
    ↓
SocketErrorCode判定
    ↓
適切なエラーメッセージ生成・スロー
    ↓
リソースクリーンアップ（finally）
    ↓
呼び出し元に例外伝播
```

### エラーメッセージ詳細

**SocketException メッセージ形式**:
```
"接続拒否: 192.168.1.10:9999"
```

**含むべき情報**:
- 接続先IPアドレス
- 接続拒否されたポート番号
- エラー種別（接続拒否）

### モック・スタブ実装

**MockSocket（接続拒否シミュレーション）**:
- ConnectAsync実行時にSocketException（ConnectionRefused）スロー
- SocketErrorCode = SocketError.ConnectionRefused設定

**MockRefusedServer（オプション）**:
- 指定ポートで接続拒否をシミュレート
- RST（リセット）パケット返却

---

## 実装記録・ドキュメント作成要件

### 必須作業項目

#### 1. 進捗記録開始
**ファイル**: `documents/implementation_records/progress_notes/2025-11-06_TC020実装.md`
- 実装開始時刻
- 目標（TC020テスト実装完了）
- 実装方針（接続拒否処理）

#### 2. 実装記録作成
**ファイル**: `documents/implementation_records/method_records/ConnectAsync_接続拒否実装記録.md`
- 実装判断根拠
  - なぜSocketErrorCodeで分類したか
  - 他のエラーハンドリング方法との比較
  - 技術選択の根拠とトレードオフ
- 発生した問題と解決過程
  - エラーコード判定の正確性
  - リソースリーク対策

#### 3. テスト結果保存
**ファイル**: `documents/implementation_records/execution_logs/TC020_テスト結果.log`
- 単体テスト結果（成功/失敗、実行時間）
- Red-Green-Refactorの各フェーズ結果
- SocketErrorCode検証結果
- エラーログとデバッグ情報

---

## 完了条件

以下すべてが満たされた時点で実装完了とする：

- [ ] TC020テストがパス
- [ ] SocketException適切にスロー
- [ ] 例外メッセージが正確
- [ ] SocketErrorCode判定が正確
- [ ] リソースクリーンアップ確認
- [ ] TC017, TC018, TC019も引き続きパス（回帰テスト）
- [ ] リファクタリング完了
- [ ] 進捗記録作成完了
- [ ] 実装記録作成完了

---

## 実装時の注意点

### TDD手法厳守
- 必ずテストを先に書く（Red）
- 最小実装でテストをパスさせる（Green）
- リファクタリングで品質向上（Refactor）
- 各フェーズでテスト実行を確認

### 接続拒否処理固有の注意点
- SocketErrorCodeによる正確なエラー分類
- 接続拒否とその他のソケットエラーの区別
- 接続拒否時のソケットリソース解放
- タイムアウト（TC019）との区別

### 記録の重要性
- エラー分類方法の選択理由を記録
- SocketErrorCode判定ロジックを記録
- テスト結果は詳細データも含めて保存

### 文字化け対策
- 日本語ファイル名の新規作成時は`.txt`経由で作成
- 作成後は必ずReadツールで確認
- 文字化け発見時は早期に対処

---

## 参考情報

### 設計書参照先
- `documents/design/クラス設計.md`
- `documents/design/テスト内容.md`（TC020詳細: 338-348行）
- `documents/design/エラーハンドリング.md`

### 開発手法
- `documents/development_methodology/development-methodology.md`

### 接続拒否ポートの選定
- **9999**: 一般的に使用されていないポート番号
- **注意**: 実環境で使用されていないポートを選択
- **代替案**: 1～1023以外で、組織で使用されていないポート

### SocketErrorCodeの種類

**主要なSocketErrorCode**:
```csharp
public enum SocketError
{
    Success = 0,
    OperationAborted = 995,
    IOPending = 997,
    Interrupted = 10004,
    AccessDenied = 10013,
    Fault = 10014,
    InvalidArgument = 10022,
    TooManyOpenSockets = 10024,
    WouldBlock = 10035,
    InProgress = 10036,
    AlreadyInProgress = 10037,
    NotSocket = 10038,
    DestinationAddressRequired = 10039,
    MessageSize = 10040,
    ProtocolType = 10041,
    ProtocolOption = 10042,
    ProtocolNotSupported = 10043,
    SocketNotSupported = 10044,
    OperationNotSupported = 10045,
    ProtocolFamilyNotSupported = 10046,
    AddressFamilyNotSupported = 10047,
    AddressAlreadyInUse = 10048,
    AddressNotAvailable = 10049,
    NetworkDown = 10050,
    NetworkUnreachable = 10051,
    NetworkReset = 10052,
    ConnectionAborted = 10053,
    ConnectionReset = 10054,
    NoBufferSpaceAvailable = 10055,
    IsConnected = 10056,
    NotConnected = 10057,
    Shutdown = 10058,
    TimedOut = 10060,
    ConnectionRefused = 10061, // TC020で検証
    HostDown = 10064,
    HostUnreachable = 10065,
    ProcessLimit = 10067,
    SystemNotReady = 10091,
    VersionNotSupported = 10092,
    NotInitialized = 10093,
    Disconnecting = 10101,
    TypeNotFound = 10109,
    HostNotFound = 11001,
    TryAgain = 11002,
    NoRecovery = 11003,
    NoData = 11004
}
```

### 関連テストケース
- TC017: TCP接続成功（正常系）
- TC018: UDP接続成功（正常系）
- TC019: 接続タイムアウト（前の異常系テスト）
- TC020_1～TC020_5: その他の異常系テスト（次の実装）

---

## エラーハンドリング設計

### ErrorMessages定数
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    // TC020で使用
    public const string ConnectionRefused = "接続拒否: {0}:{1}";
    public const string ConnectionFailed = "接続失敗: {0}:{1} - {2}";

    // TC019で使用
    public const string ConnectionTimeout = "接続タイムアウト: {0}:{1}（タイムアウト時間: {2}ms）";

    // 共通接続関連
    public const string AlreadyConnected = "既に接続済みです。再接続する場合は先にDisconnectAsync()を実行してください";
    public const string ConfigOrTimeoutNull = "ConnectionConfigまたはTimeoutConfigがnullです。";
}
```

### エラー分類（ErrorHandler連携）
**TC020で発生する例外の分類**:
- **ErrorCategory**: CommunicationError
- **Severity**: Error
- **ShouldRetry**: false（接続拒否はリトライしても成功しない）
- **ErrorAction**: Abort（処理中断）

---

## ログ出力要件

### LoggingManager連携

**接続開始ログ**:
```
[Information] TCP接続開始: 192.168.1.10:9999, タイムアウト=5000ms
```

**エラーログ**:
```
[Error] 接続拒否: 192.168.1.10:9999
  Exception: SocketException
  SocketErrorCode: ConnectionRefused (10061)
  StackTrace: ...
```

### ログレベル
- **Information**: 接続開始
- **Error**: 接続拒否発生

---

以上の指示に従って、TC020_ConnectAsync_接続拒否テストの実装を開始してください。

不明点や不足情報があれば、実装前に質問してください。
