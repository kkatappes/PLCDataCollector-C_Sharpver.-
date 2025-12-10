# Phase 3: ログ出力実装

**最終更新**: 2025-12-03

## 概要

接続試行の各ステップでログ出力を追加し、トラブルシューティングを容易にします。

## ✅ 実装状況の確認

**現在の実装状況**: ✅ **Phase 3完了（2025-12-03）**

以下のPhaseが完了済みです:
1. **Phase 1**: ✅ 完了（2025-12-03）- ConnectionResponseモデル拡張（UsedProtocol, IsFallbackConnection, FallbackErrorDetails追加）
2. **Phase 2**: ✅ 完了（2025-12-03 17:30）- 代替プロトコル試行ロジック実装（TryConnectWithProtocolAsync, GetProtocolName追加）
3. **Phase 3**: ✅ 完了（2025-12-03）- LoggingManager統合、ログ出力実装

詳細な実装状況については、`現在の実装状況.md`および以下の実装結果ドキュメントを参照してください:
- [Phase1_ConnectionResponse拡張_TestResults.md](../実装結果/Phase1_ConnectionResponse拡張_TestResults.md)
- [Phase2_接続ロジック実装_TestResults.md](../実装結果/Phase2_接続ロジック実装_TestResults.md)
- [Phase3_ログ出力実装_TestResults.md](../実装結果/Phase3_ログ出力実装_TestResults.md)

## 前提条件

- ✅ Phase 1完了（ConnectionResponseに新規プロパティ追加済み）→ **完了（2025-12-03）**
- ✅ Phase 2完了（代替プロトコル試行ロジック実装済み）→ **完了（2025-12-03 17:30）**

## Phase 2からの引き継ぎ事項

### ✅ Phase 2完了内容（2025-12-03 17:30）

**実装済みメソッド:**
- `ConnectAsync()` - 代替プロトコル試行ロジック完成
- `TryConnectWithProtocolAsync(bool, int)` - 指定プロトコルでの接続試行（戻り値: 4タプル `(bool success, Socket? socket, string? error, Exception? exception)`）
- `GetProtocolName(bool)` - プロトコル名取得（"TCP"/"UDP"）

**エラーハンドリング完成:**
- TimeoutException vs SocketException の正確な判定
- ConnectionStatus.Timeout vs Failed の適切な設定
- ErrorDetails.AdditionalInfo 条件付きフィールド（TimeoutMs/SocketErrorCode）

**ErrorMessages.cs 既存実装:**
- `BothProtocolsConnectionFailed(string tcpError, string udpError)` - 両プロトコル失敗メッセージ生成（短い形式）
- `InitialProtocolFailed(string protocol, string error)` - 初期プロトコル失敗メッセージ生成（短い形式）

**Phase 3での対応が必要な点:**
1. **ログ出力の実装**:
   - Phase 2では Console.WriteLine() で仮実装済み
   - Phase 3で LoggingManager 統合が必要
   - 接続試行開始、プロトコル切替、失敗時のログ出力

2. **ErrorMessages.cs への追加**:
   - Phase 2実装のメソッドは**エラー詳細記録用（短い形式）**
   - Phase 3では**ログ出力用（詳細形式、IPアドレス/ポート番号含む）**のメソッドを追加
   - 命名は既存メソッドと区別すること（例: `ConnectionAttemptStarted`, `InitialProtocolFailedRetrying` など）

**テスト結果:**
- Phase 2新規テスト: 6/6成功
- 既存テスト: 799/799成功
- 実行時間: 42秒
- 詳細: [Phase2_接続ロジック実装_TestResults.md](../実装結果/Phase2_接続ロジック実装_TestResults.md)

## TDDサイクル

### Step 3-Red: 失敗するテストを作成

**作業内容:**
1. `PlcCommunicationManagerTests.cs`にログ検証テストを追加:

```csharp
[Fact]
public async Task ConnectAsync_初期プロトコル成功_接続開始ログのみ出力()
{
    // Arrange: ログ出力をキャプチャするモック
    var mockLogger = new Mock<ILoggingManager>();
    var manager = CreateManagerWithMockLogger(mockLogger.Object);

    // Act
    var result = await manager.ConnectAsync();

    // Assert
    mockLogger.Verify(x => x.LogInfo(
        It.Is<string>(s => s.Contains("PLC接続試行開始"))),
        Times.Once);  // ← 現在の実装ではログ出力なし（Red状態）

    // 初期成功時は警告・エラーログなし
    mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
    mockLogger.Verify(x => x.LogError(It.IsAny<string>()), Times.Never);
}

[Fact]
public async Task ConnectAsync_代替プロトコル成功_警告ログと成功ログ出力()
{
    // Arrange: TCP失敗→UDP成功のモック
    var mockLogger = new Mock<ILoggingManager>();
    var manager = CreateManagerWithFailingTcpAndSuccessfulUdp(mockLogger.Object);

    // Act
    var result = await manager.ConnectAsync();

    // Assert
    mockLogger.Verify(x => x.LogWarning(
        It.Is<string>(s => s.Contains("TCP接続失敗") && s.Contains("UDP再試行"))),
        Times.Once);  // ← 現在の実装ではログ出力なし
    mockLogger.Verify(x => x.LogInfo(
        It.Is<string>(s => s.Contains("代替プロトコル(UDP)で接続成功"))),
        Times.Once);
}

[Fact]
public async Task ConnectAsync_両プロトコル失敗_詳細エラーログ出力()
{
    // Arrange: 両プロトコル失敗のモック
    var mockLogger = new Mock<ILoggingManager>();
    var manager = CreateManagerWithBothProtocolsFailing(mockLogger.Object);

    // Act
    var result = await manager.ConnectAsync();

    // Assert
    mockLogger.Verify(x => x.LogError(
        It.Is<string>(s => s.Contains("TCP/UDP両プロトコルで接続失敗"))),
        Times.Once);  // ← 現在の実装ではログ出力なし

    // エラー詳細に両プロトコルのエラーが含まれる
    mockLogger.Verify(x => x.LogError(
        It.Is<string>(s => s.Contains("TCP") && s.Contains("UDP"))),
        Times.Once);
}
```

2. テスト実行 → **失敗（Red状態）**を確認

**期待される結果:**
- `ConnectAsync_初期プロトコル成功_接続開始ログのみ出力()`: ❌ Verify失敗（ログ出力なし）
- `ConnectAsync_代替プロトコル成功_警告ログと成功ログ出力()`: ❌ Verify失敗（ログ出力なし）
- `ConnectAsync_両プロトコル失敗_詳細エラーログ出力()`: ❌ Verify失敗（ログ出力なし）

---

### Step 3-Green: テストを通す最小実装

**作業内容:**
1. `ConnectAsync()`内の仮実装（Console.WriteLine）をLoggingManagerに置き換え:

**Phase 2での実装（Console.WriteLine仮実装）:**
```csharp
// Phase 2で既に実装されている箇所（仮ログ出力）
Console.WriteLine($"[INFO] PLC接続試行: {initialProtocolName}");
Console.WriteLine($"[WARN] {initialProtocolName}接続失敗、{alternativeProtocolName}で再試行");
Console.WriteLine($"[INFO] 代替プロトコル({alternativeProtocolName})で接続成功");
Console.WriteLine($"[ERROR] 両プロトコルで接続失敗");
```

**Phase 3での実装（LoggingManager統合）:**
```csharp
public async Task<ConnectionResponse> ConnectAsync()
{
    var startTime = DateTime.UtcNow;
    var initialProtocol = _connectionConfig.UseTcp;
    var initialProtocolName = GetProtocolName(initialProtocol);  // Phase 2実装済み

    // 接続開始ログ（Console.WriteLine → LoggingManager）
    await (_loggingManager?.LogInfo(
        $"PLC接続試行開始: {_connectionConfig.IpAddress}:{_connectionConfig.Port}, " +
        $"プロトコル: {initialProtocolName}") ?? Task.CompletedTask);

    // 1. 初期プロトコルで試行
    var (success, socket, error, exception) = await TryConnectWithProtocolAsync(
        initialProtocol,
        _timeoutConfig.ConnectTimeoutMs);  // Phase 2実装済み、4タプル返却

    if (success)
    {
        return new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            Socket = socket,
            UsedProtocol = initialProtocolName,  // GetProtocolName()使用
            IsFallbackConnection = false,
            ConnectedAt = DateTime.UtcNow,
            ConnectionTime = (DateTime.UtcNow - startTime).TotalMilliseconds
        };
    }

    // 初期プロトコル失敗時の警告ログ（Console.WriteLine → LoggingManager）
    var alternativeProtocolName = GetProtocolName(!initialProtocol);
    await (_loggingManager?.LogWarning(
        $"{initialProtocolName}接続失敗: {error}. " +
        $"代替プロトコル({alternativeProtocolName})で再試行します。") ?? Task.CompletedTask);

    // 2. 代替プロトコルで試行
    var alternativeProtocol = !initialProtocol;
    var (altSuccess, altSocket, altError, altException) = await TryConnectWithProtocolAsync(
        alternativeProtocol,
        _timeoutConfig.ConnectTimeoutMs);

    if (altSuccess)
    {
        // 代替プロトコル成功ログ（Console.WriteLine → LoggingManager）
        await (_loggingManager?.LogInfo(
            $"代替プロトコル({alternativeProtocolName})で接続成功: " +
            $"{_connectionConfig.IpAddress}:{_connectionConfig.Port}") ?? Task.CompletedTask);

        return new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            Socket = altSocket,
            UsedProtocol = alternativeProtocolName,
            IsFallbackConnection = true,
            FallbackErrorDetails = ErrorMessages.InitialProtocolFailed(initialProtocolName, error),  // Phase 2実装済み
            ConnectedAt = DateTime.UtcNow,
            ConnectionTime = (DateTime.UtcNow - startTime).TotalMilliseconds
        };
    }

    // 両プロトコル失敗時のエラーログ（Console.WriteLine → LoggingManager）
    var tcpError = initialProtocol ? error : altError;
    var udpError = initialProtocol ? altError : error;
    await (_loggingManager?.LogError(null,
        $"PLC接続失敗: {_connectionConfig.IpAddress}:{_connectionConfig.Port}. " +
        $"TCP/UDP両プロトコルで接続に失敗しました。\n" +
        $"  - TCP接続エラー: {tcpError}\n" +
        $"  - UDP接続エラー: {udpError}") ?? Task.CompletedTask);

    // 3. 両プロトコル失敗（Phase 2実装済み）
    // エラータイプ判定（TimeoutException vs SocketException）
    var isTimeout = exception is TimeoutException || altException is TimeoutException;
    var isRefused = (exception is SocketException se && se.SocketErrorCode == SocketError.ConnectionRefused) ||
                    (altException is SocketException ase && ase.SocketErrorCode == SocketError.ConnectionRefused);

    return new ConnectionResponse
    {
        Status = isTimeout ? ConnectionStatus.Timeout : ConnectionStatus.Failed,
        ErrorMessage = ErrorMessages.BothProtocolsConnectionFailed(tcpError, udpError),  // Phase 2実装済み
        UsedProtocol = null,
        IsFallbackConnection = false
    };
}
```

2. テスト実行 → **全テスト成功（Green状態）**を確認

**期待される結果:**
- `ConnectAsync_初期プロトコル成功_接続開始ログのみ出力()`: ✅ 成功
- `ConnectAsync_代替プロトコル成功_警告ログと成功ログ出力()`: ✅ 成功
- `ConnectAsync_両プロトコル失敗_詳細エラーログ出力()`: ✅ 成功

---

### Step 3-Refactor: コード改善

**作業内容:**
1. ログメッセージを`ErrorMessages.cs`に集約:

```csharp
// ErrorMessages.cs に追加（Phase 2実装からの続き）
public static class ErrorMessages
{
    // ===== Phase 2実装済み（エラー詳細記録用、短い形式）=====
    // public static string InitialProtocolFailed(string protocol, string error)
    // public static string BothProtocolsConnectionFailed(string tcpError, string udpError)

    // ===== Phase 3追加（ログ出力用、詳細形式、IPアドレス/ポート番号含む）=====

    /// <summary>
    /// 接続試行開始ログメッセージ
    /// </summary>
    public static string ConnectionAttemptStarted(string ipAddress, int port, string protocol)
    {
        return $"PLC接続試行開始: {ipAddress}:{port}, プロトコル: {protocol}";
    }

    /// <summary>
    /// 初期プロトコル失敗・代替プロトコル再試行ログメッセージ
    /// </summary>
    public static string InitialProtocolFailedRetrying(string failedProtocol, string error, string alternativeProtocol)
    {
        return $"{failedProtocol}接続失敗: {error}. 代替プロトコル({alternativeProtocol})で再試行します。";
    }

    /// <summary>
    /// 代替プロトコル接続成功ログメッセージ
    /// </summary>
    public static string FallbackConnectionSucceeded(string protocol, string ipAddress, int port)
    {
        return $"代替プロトコル({protocol})で接続成功: {ipAddress}:{port}";
    }

    /// <summary>
    /// 両プロトコル失敗詳細ログメッセージ（ログ出力用）
    /// </summary>
    public static string BothProtocolsConnectionFailedDetailed(string ipAddress, int port, string tcpError, string udpError)
    {
        return $"PLC接続失敗: {ipAddress}:{port}. TCP/UDP両プロトコルで接続に失敗しました。\n" +
               $"  - TCP接続エラー: {tcpError}\n" +
               $"  - UDP接続エラー: {udpError}";
    }
}
```

**Phase 2とPhase 3のメソッド使い分け:**

| 用途 | Phase 2実装（短い形式） | Phase 3実装（詳細形式） |
|------|----------------------|----------------------|
| エラー詳細記録（ConnectionResponse.ErrorMessage） | `BothProtocolsConnectionFailed(tcpError, udpError)` | - |
| エラー詳細記録（ConnectionResponse.FallbackErrorDetails） | `InitialProtocolFailed(protocol, error)` | - |
| ログ出力（接続開始） | - | `ConnectionAttemptStarted(ipAddress, port, protocol)` |
| ログ出力（初期失敗・再試行） | - | `InitialProtocolFailedRetrying(protocol, error, alternative)` |
| ログ出力（代替成功） | - | `FallbackConnectionSucceeded(protocol, ipAddress, port)` |
| ログ出力（両失敗） | - | `BothProtocolsConnectionFailedDetailed(ipAddress, port, tcpError, udpError)` |

2. ログ出力処理を内部メソッドに分離（可読性向上）:

```csharp
/// <summary>
/// 接続試行開始ログを出力
/// </summary>
private async Task LogConnectionAttemptAsync(string protocol)
{
    var message = ErrorMessages.ConnectionAttemptStarted(
        _connectionConfig.IpAddress,
        _connectionConfig.Port,
        protocol);
    await (_loggingManager?.LogInfo(message) ?? Task.CompletedTask);
}

/// <summary>
/// 初期プロトコル失敗・代替プロトコル再試行ログを出力
/// </summary>
private async Task LogInitialProtocolFailedAsync(string failedProtocol, string error, string alternativeProtocol)
{
    var message = ErrorMessages.InitialProtocolFailedRetrying(
        failedProtocol,
        error,
        alternativeProtocol);
    await (_loggingManager?.LogWarning(message) ?? Task.CompletedTask);
}

/// <summary>
/// 代替プロトコル接続成功ログを出力
/// </summary>
private async Task LogFallbackConnectionSucceededAsync(string protocol)
{
    var message = ErrorMessages.FallbackConnectionSucceeded(
        protocol,
        _connectionConfig.IpAddress,
        _connectionConfig.Port);
    await (_loggingManager?.LogInfo(message) ?? Task.CompletedTask);
}

/// <summary>
/// 両プロトコル失敗ログを出力
/// </summary>
private async Task LogBothProtocolsFailedAsync(bool initialWasTcp, string initialError, string alternativeError)
{
    var tcpError = initialWasTcp ? initialError : alternativeError;
    var udpError = initialWasTcp ? alternativeError : initialError;

    var message = ErrorMessages.BothProtocolsConnectionFailedDetailed(
        _connectionConfig.IpAddress,
        _connectionConfig.Port,
        tcpError,
        udpError);
    await (_loggingManager?.LogError(null, message) ?? Task.CompletedTask);
}
```

**注意**: LoggingManagerのメソッドは非同期（Task返却）のため、内部メソッドもasyncにする必要があります。また、null安全のため`?? Task.CompletedTask`パターンを使用します。

3. ConnectAsync()内でログメソッドを使用（Refactor版）:

```csharp
public async Task<ConnectionResponse> ConnectAsync()
{
    var startTime = DateTime.UtcNow;
    var initialProtocol = _connectionConfig.UseTcp;
    var initialProtocolName = GetProtocolName(initialProtocol);  // Phase 2実装済み

    // 接続開始ログ（メソッド分離）
    await LogConnectionAttemptAsync(initialProtocolName);

    // 1. 初期プロトコルで試行
    var (success, socket, error, exception) = await TryConnectWithProtocolAsync(
        initialProtocol,
        _timeoutConfig.ConnectTimeoutMs);  // Phase 2実装済み、4タプル返却

    if (success)
    {
        return new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            Socket = socket,
            UsedProtocol = initialProtocolName,
            IsFallbackConnection = false,
            ConnectedAt = DateTime.UtcNow,
            ConnectionTime = (DateTime.UtcNow - startTime).TotalMilliseconds
        };
    }

    // 初期プロトコル失敗時の警告ログ（メソッド分離）
    var alternativeProtocolName = GetProtocolName(!initialProtocol);  // Phase 2実装済み
    await LogInitialProtocolFailedAsync(initialProtocolName, error, alternativeProtocolName);

    // 2. 代替プロトコルで試行
    var alternativeProtocol = !initialProtocol;
    var (altSuccess, altSocket, altError, altException) = await TryConnectWithProtocolAsync(
        alternativeProtocol,
        _timeoutConfig.ConnectTimeoutMs);

    if (altSuccess)
    {
        // 代替プロトコル成功ログ（メソッド分離）
        await LogFallbackConnectionSucceededAsync(alternativeProtocolName);

        return new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            Socket = altSocket,
            UsedProtocol = alternativeProtocolName,
            IsFallbackConnection = true,
            FallbackErrorDetails = ErrorMessages.InitialProtocolFailed(initialProtocolName, error),  // Phase 2実装済み
            ConnectedAt = DateTime.UtcNow,
            ConnectionTime = (DateTime.UtcNow - startTime).TotalMilliseconds
        };
    }

    // 両プロトコル失敗時のエラーログ（メソッド分離）
    await LogBothProtocolsFailedAsync(initialProtocol, error, altError);

    // 3. 両プロトコル失敗（Phase 2実装済み）
    // エラータイプ判定（TimeoutException vs SocketException）
    var isTimeout = exception is TimeoutException || altException is TimeoutException;

    var tcpError = initialProtocol ? error : altError;
    var udpError = initialProtocol ? altError : error;

    return new ConnectionResponse
    {
        Status = isTimeout ? ConnectionStatus.Timeout : ConnectionStatus.Failed,
        ErrorMessage = ErrorMessages.BothProtocolsConnectionFailed(tcpError, udpError),  // Phase 2実装済み
        UsedProtocol = null,
        IsFallbackConnection = false
    };
}
```

**Refactor後の改善点:**
- ログ出力処理を専用メソッドに分離し、可読性向上
- ErrorMessages.csでメッセージを統一管理
- Phase 2実装済みメソッド（GetProtocolName, TryConnectWithProtocolAsync）を活用
- 非同期処理の適切なawait使用

4. テスト実行 → **全テスト成功を維持**

**期待される結果:**
- 全テストが引き続き成功
- コードの可読性・保守性が向上
- ログメッセージが統一管理される

---

## ログ出力仕様

### 接続試行開始時
```
[INFO] PLC接続試行開始: 192.168.1.100:5000, プロトコル: TCP
```

### 初期プロトコル失敗時
```
[WARN] TCP接続失敗: Connection timeout. 代替プロトコル(UDP)で再試行します。
```

### 代替プロトコル成功時
```
[INFO] 代替プロトコル(UDP)で接続成功: 192.168.1.100:5000
```

### 両プロトコル失敗時
```
[ERROR] PLC接続失敗: 192.168.1.100:5000. TCP/UDP両プロトコルで接続に失敗しました。
  - TCP接続エラー: Connection timeout
  - UDP接続エラー: Network unreachable
```

## テストケース一覧

| テストID | テスト名 | Red状態 | Green状態 | 検証内容 |
|---------|---------|---------|----------|---------|
| TC_P3_001 | ConnectAsync_初期プロトコル成功_接続開始ログのみ出力 | Verify失敗 | テスト成功 | 初期成功時のログ出力 |
| TC_P3_002 | ConnectAsync_代替プロトコル成功_警告ログと成功ログ出力 | Verify失敗 | テスト成功 | 代替成功時のログ出力 |
| TC_P3_003 | ConnectAsync_両プロトコル失敗_詳細エラーログ出力 | Verify失敗 | テスト成功 | 失敗時のログ出力 |

## 実装後の確認事項

### 必須確認項目

1. ✅ 全テストがGreen状態
2. ✅ 初期成功時のログ出力確認
3. ✅ 代替成功時のログ出力確認（警告+情報）
4. ✅ 両失敗時のログ出力確認（エラー）
5. ✅ ログメッセージの統一管理（ErrorMessages.cs）

### ログレベルの適切性

- **INFO**: 接続試行開始、代替プロトコル成功
- **WARN**: 初期プロトコル失敗（代替を試行する）
- **ERROR**: 両プロトコル失敗（最終的な接続失敗）

## 想定工数

| ステップ | 作業内容 | 想定工数 | 備考 |
|---------|---------|---------|------|
| **Red** | ログ出力検証テスト作成（失敗状態） | 0.5h | Verify失敗確認 |
| **Green** | ログ出力実装 | 0.3h | テスト成功確認 |
| **Refactor** | ログメッセージ集約・メソッド分離 | 0.2h | テスト成功維持 |
| **合計** | | **1h** | |

## Phase 2完了後の実装準備状況

### ✅ Phase 3実装のための準備完了事項

1. **ConnectAsync()の代替プロトコル試行ロジック**: ✅ 完了
   - 初期プロトコル試行 → 失敗時に代替プロトコル試行のフロー実装済み
   - Console.WriteLine()での仮ログ出力実装済み

2. **必要なヘルパーメソッド**: ✅ 完了
   - `GetProtocolName(bool)` - プロトコル名取得実装済み
   - `TryConnectWithProtocolAsync(bool, int)` - 接続試行実装済み

3. **エラーメッセージ管理基盤**: ✅ 完了
   - `ErrorMessages.cs` にエラー詳細記録用メソッド実装済み
   - Phase 3でログ出力用メソッドを追加する基盤が整っている

4. **既存テスト**: ✅ 全成功（799/801）
   - Phase 2実装による既存機能への影響なし確認済み

### Phase 3実装時の注意点

1. **仮ログ出力の置き換え**:
   - Phase 2で実装した`Console.WriteLine()`を`LoggingManager`に置き換える
   - 非同期処理（Task返却）に注意

2. **ErrorMessages.csへの追加**:
   - Phase 2実装メソッド（短い形式）と命名を区別する
   - ログ出力用メソッドはIPアドレス・ポート番号を含む詳細形式

3. **テスト戦略**:
   - ログ出力のモック（Mock<ILoggingManager>）を使用
   - Verify()でログメソッド呼び出しを検証
   - 既存テスト（799件）への影響確認

## 次のステップ

Phase 3完了後、**Phase 4: 統合テスト**に進みます。

Phase 4では、接続処理全体（接続→送信→受信）を統合的にテストし、代替プロトコルでも正常にデータ送受信できることを確認します。

## 関連ドキュメント

- [Phase0_概要と前提条件.md](Phase0_概要と前提条件.md) - 全体概要と仕様
- [Phase1_ConnectionResponse拡張.md](Phase1_ConnectionResponse拡張.md) - Phase 1実装計画
- [Phase2_接続ロジック実装.md](Phase2_接続ロジック実装.md) - Phase 2実装計画
- [Phase1_ConnectionResponse拡張_TestResults.md](../実装結果/Phase1_ConnectionResponse拡張_TestResults.md) - Phase 1実装結果
- [Phase2_接続ロジック実装_TestResults.md](../実装結果/Phase2_接続ロジック実装_TestResults.md) - Phase 2実装結果
- [現在の実装状況.md](現在の実装状況.md) - 最新の実装状況
