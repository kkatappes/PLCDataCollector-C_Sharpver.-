# TC019_ConnectAsync_接続タイムアウトテスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC019_ConnectAsync_接続タイムアウトテストケースを、TDD手法に従って実装してください。

---

## 実装概要

### 目的
ConnectAsync()メソッドが、到達不可能なIPアドレスに対して適切にタイムアウト処理を行うことを検証します。

### 実装対象
- **テストファイル**: `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`
- **テスト名前空間**: `andon.Tests.Unit.Core.Managers`
- **テストメソッド名**: `TC019_ConnectAsync_接続タイムアウト`

---

## 前提条件

実装開始前に以下を確認してください：

1. **依存ファイルの存在確認**
   - `Core/Managers/PlcCommunicationManager.cs` (TCP/UDP実装済み)
   - `Core/Interfaces/IPlcCommunicationManager.cs`
   - `Core/Models/ConnectionResponse.cs`
   - `Core/Constants/ErrorMessages.cs`

2. **前提テスト確認**
   - TC017_ConnectAsync_TCP接続成功が実装済み・テストパス済みであること
   - TC018_ConnectAsync_UDP接続成功が実装済み・テストパス済みであること

不足しているファイルがあれば報告してください。

---

## 実装手順（TDD Red-Green-Refactor）

### Phase 1: Red（テスト失敗）

#### Step 1-1: テストケース実装

**TC019: 接続タイムアウトテスト**

**Arrange（準備）**:
- ConnectionConfigを作成
  - **IpAddress = "192.168.100.200"（到達不可能なIPアドレス）**
  - Port = 5000
  - UseTcp = true
  - ConnectionType = "TCP"
- TimeoutConfigを作成
  - **ConnectTimeoutMs = 1000（短いタイムアウト設定）**
  - SendTimeoutMs = 3000
  - ReceiveTimeoutMs = 3000
- PlcCommunicationManagerインスタンス作成

**Act & Assert（実行・検証）**:
```csharp
// TimeoutExceptionがスローされることを確認
var exception = await Assert.ThrowsAsync<TimeoutException>(async () =>
    await manager.ConnectAsync(config, timeout));

// 例外メッセージ確認
Assert.Contains("接続タイムアウト", exception.Message);
Assert.Contains("192.168.100.200:5000", exception.Message);
Assert.Contains("1000ms", exception.Message);
```

**追加検証項目**:
- タイムアウト時間の精度確認（処理時間 ≈ ConnectTimeoutMs、誤差±200ms）
- リソースクリーンアップ確認（ソケットDispose済み）
- 接続状態確認（NotConnected または Failed）

#### Step 1-2: テスト実行（Red確認）
```bash
dotnet test --filter "FullyQualifiedName~TC019"
```

期待結果: テスト失敗（タイムアウト処理が適切に実装されていない、または例外がスローされない）

---

### Phase 2: Green（最小実装）

#### Step 2-1: ConnectAsync タイムアウト処理実装

**実装箇所**: `Core/Managers/PlcCommunicationManager.cs`

**タイムアウト処理実装**:
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
                // TC019: タイムアウト発生
                socket?.Dispose();
                throw new TimeoutException(
                    string.Format(ErrorMessages.ConnectionTimeout,
                        config.IpAddress,
                        config.Port,
                        timeout.ConnectTimeoutMs));
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
    catch (OperationCanceledException)
    {
        // TC019: タイムアウト例外変換
        socket?.Dispose();
        throw new TimeoutException(
            string.Format(ErrorMessages.ConnectionTimeout,
                config.IpAddress,
                config.Port,
                timeout.ConnectTimeoutMs));
    }
    catch (SocketException ex)
    {
        // その他のソケット例外
        socket?.Dispose();
        throw;
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
public const string ConnectionTimeout = "接続タイムアウト: {0}:{1}（タイムアウト時間: {2}ms）";
```

#### Step 2-2: テスト再実行（Green確認）
```bash
dotnet test --filter "FullyQualifiedName~TC019"
```

期待結果: すべてのテストがパス

---

### Phase 3: Refactor（リファクタリング）

#### Step 3-1: コード品質向上
- ログ出力追加（LoggingManager連携）
  - 接続開始ログ: IP・ポート情報、タイムアウト設定
  - エラーログ: TimeoutException詳細、スタックトレース、経過時間
- エラーハンドリング強化
  - リソースクリーンアップの確実性向上（finallyブロック）
  - 接続状態の適切な更新
- ドキュメントコメント追加

#### Step 3-2: テスト再実行（Green維持確認）
```bash
dotnet test --filter "FullyQualifiedName~TC019"
```

期待結果: すべてのテストがパス（リファクタリング後も）

---

## 技術仕様詳細

### タイムアウト検出メカニズム

**CancellationTokenSourceによるタイムアウト制御**:
```csharp
var cts = new CancellationTokenSource(timeoutConfig.ConnectTimeoutMs);

try
{
    await socket.ConnectAsync(endPoint, cts.Token);
}
catch (OperationCanceledException)
{
    // タイムアウト発生
    throw new TimeoutException(...);
}
```

**タイムアウト時の動作**:
1. **ConnectTimeoutMs経過**: CancellationTokenSourceがキャンセルを通知
2. **OperationCanceledException発生**: Socket.ConnectAsyncがキャンセル
3. **TimeoutExceptionスロー**: ConnectAsync内で例外を変換
4. **リソースクリーンアップ**: finallyブロックでソケットDispose

### タイムアウト時間の精度

**期待動作**:
- 処理時間 ≈ ConnectTimeoutMs（誤差±200ms程度許容）
- 極端に短い・長い時間で完了していないこと

**計測方法**:
```csharp
var startTime = DateTime.Now;
try
{
    await socket.ConnectAsync(endPoint, cts.Token);
}
catch (OperationCanceledException)
{
    var elapsedTime = DateTime.Now - startTime;
    // elapsedTime.TotalMilliseconds ≈ ConnectTimeoutMs
}
```

### リソースクリーンアップ

**重要**: タイムアウト発生時も確実にリソースを解放

```csharp
try
{
    // 接続処理
}
catch (OperationCanceledException)
{
    socket?.Dispose(); // ソケット解放
    throw new TimeoutException(...);
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
ConnectTimeoutMs経過
    ↓
OperationCanceledException発生
    ↓
TimeoutException変換・スロー
    ↓
リソースクリーンアップ（finally）
    ↓
呼び出し元に例外伝播
```

### エラーメッセージ詳細

**TimeoutException メッセージ形式**:
```
"接続タイムアウト: 192.168.100.200:5000（タイムアウト時間: 1000ms）"
```

**含むべき情報**:
- 到達不可能なIPアドレス
- ポート番号
- 設定されたタイムアウト時間

### モック・スタブ実装

**MockSocket（タイムアウトシミュレーション）**:
- ConnectAsync実行時にOperationCanceledExceptionスロー
- 指定時間後にキャンセル通知

**MockUnreachableServer（オプション）**:
- 接続要求に応答しない（タイムアウトまで待機）
- 実際のネットワークタイムアウトをシミュレート

---

## 実装記録・ドキュメント作成要件

### 必須作業項目

#### 1. 進捗記録開始
**ファイル**: `documents/implementation_records/progress_notes/2025-11-06_TC019実装.md`
- 実装開始時刻
- 目標（TC019テスト実装完了）
- 実装方針（タイムアウト処理）

#### 2. 実装記録作成
**ファイル**: `documents/implementation_records/method_records/ConnectAsync_タイムアウト実装記録.md`
- 実装判断根拠
  - なぜCancellationTokenSourceを使用したか
  - 他のタイムアウト実装方法との比較
  - 技術選択の根拠とトレードオフ
- 発生した問題と解決過程
  - タイムアウト精度の問題
  - リソースリーク対策

#### 3. テスト結果保存
**ファイル**: `documents/implementation_records/execution_logs/TC019_テスト結果.log`
- 単体テスト結果（成功/失敗、実行時間）
- Red-Green-Refactorの各フェーズ結果
- タイムアウト時間精度測定結果
- エラーログとデバッグ情報

---

## 完了条件

以下すべてが満たされた時点で実装完了とする：

- [ ] TC019テストがパス
- [ ] TimeoutException適切にスロー
- [ ] 例外メッセージが正確
- [ ] タイムアウト時間精度が妥当範囲内
- [ ] リソースクリーンアップ確認
- [ ] TC017, TC018も引き続きパス（回帰テスト）
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

### タイムアウト処理固有の注意点
- CancellationTokenSourceの確実なDispose
- OperationCanceledExceptionからTimeoutExceptionへの適切な変換
- タイムアウト時のソケットリソース解放
- タイムアウト時間の精度確認

### 記録の重要性
- タイムアウト実装方法の選択理由を記録
- タイムアウト時間精度測定結果を記録
- テスト結果は数値データも含めて保存

### 文字化け対策
- 日本語ファイル名の新規作成時は`.txt`経由で作成
- 作成後は必ずReadツールで確認
- 文字化け発見時は早期に対処

---

## 参考情報

### 設計書参照先
- `documents/design/クラス設計.md`
- `documents/design/テスト内容.md`（TC019詳細: 325-336行）
- `documents/design/エラーハンドリング.md`

### 開発手法
- `documents/development_methodology/development-methodology.md`

### 到達不可能IPアドレスの選定
- **192.168.100.x**: プライベートネットワーク範囲、通常は到達不可能
- **10.255.255.x**: プライベートネットワーク範囲（代替案）
- **注意**: 実環境で使用されていないIPアドレスを選択

### タイムアウト時間の選定
- **1000ms**: 短いタイムアウト、テスト実行時間短縮
- **注意**: 環境によっては実際の接続失敗時間が異なる可能性

### CancellationTokenSourceの使用

**利点**:
- 非同期処理のタイムアウト制御に適している
- Task.WhenAnyよりも軽量で効率的
- .NETの標準的なタイムアウト制御パターン

**注意点**:
- 使用後は必ずDisposeすること
- タイムアウト後の例外処理を適切に行うこと

### 関連テストケース
- TC017: TCP接続成功（正常系）
- TC018: UDP接続成功（正常系）
- TC020: 接続拒否（次の異常系テスト）

---

## エラーハンドリング設計

### ErrorMessages定数
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    // TC019で使用
    public const string ConnectionTimeout = "接続タイムアウト: {0}:{1}（タイムアウト時間: {2}ms）";

    // 共通接続関連
    public const string AlreadyConnected = "既に接続済みです。再接続する場合は先にDisconnectAsync()を実行してください";
    public const string ConfigOrTimeoutNull = "ConnectionConfigまたはTimeoutConfigがnullです。";
}
```

### エラー分類（ErrorHandler連携）
**TC019で発生する例外の分類**:
- **ErrorCategory**: CommunicationError
- **Severity**: Error
- **ShouldRetry**: true（リトライ推奨）
- **ErrorAction**: Abort（処理中断）

---

## ログ出力要件

### LoggingManager連携

**接続開始ログ**:
```
[Information] TCP接続開始: 192.168.100.200:5000, タイムアウト=1000ms
```

**エラーログ**:
```
[Error] 接続タイムアウト: 192.168.100.200:5000（タイムアウト時間: 1000ms）
  Exception: TimeoutException
  経過時間: 1020ms
  StackTrace: ...
```

### ログレベル
- **Information**: 接続開始
- **Error**: タイムアウト発生

---

以上の指示に従って、TC019_ConnectAsync_接続タイムアウトテストの実装を開始してください。

不明点や不足情報があれば、実装前に質問してください。
