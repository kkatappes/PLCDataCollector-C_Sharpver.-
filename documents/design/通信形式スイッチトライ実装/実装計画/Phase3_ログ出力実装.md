# Phase 3: ログ出力実装

**最終更新**: 2025-11-28

## 概要

接続試行の各ステップでログ出力を追加し、トラブルシューティングを容易にします。

## 前提条件

- Phase 1完了（ConnectionResponseに新規プロパティ追加済み）
- Phase 2完了（代替プロトコル試行ロジック実装済み）

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
1. `ConnectAsync()`内にログ出力を追加:

```csharp
public async Task<ConnectionResponse> ConnectAsync()
{
    var startTime = DateTime.UtcNow;
    var initialProtocol = _connectionConfig.UseTcp;

    // 接続開始ログ
    _loggingManager.LogInfo(
        $"PLC接続試行開始: {_connectionConfig.IpAddress}:{_connectionConfig.Port}, " +
        $"プロトコル: {(initialProtocol ? "TCP" : "UDP")}");

    // 1. 初期プロトコルで試行
    var (success, socket, error) = await TryConnectWithProtocolAsync(
        initialProtocol,
        _timeoutConfig.ConnectTimeoutMs);

    if (success)
    {
        return new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            Socket = socket,
            UsedProtocol = initialProtocol ? "TCP" : "UDP",
            IsFallbackConnection = false,
            ConnectedAt = DateTime.UtcNow,
            ConnectionTime = (DateTime.UtcNow - startTime).TotalMilliseconds
        };
    }

    // 初期プロトコル失敗時の警告ログ
    _loggingManager.LogWarning(
        $"{(initialProtocol ? "TCP" : "UDP")}接続失敗: {error}. " +
        $"代替プロトコル({GetAlternativeProtocol(initialProtocol)})で再試行します。");

    // 2. 代替プロトコルで試行
    var alternativeProtocol = !initialProtocol;
    var (altSuccess, altSocket, altError) = await TryConnectWithProtocolAsync(
        alternativeProtocol,
        _timeoutConfig.ConnectTimeoutMs);

    if (altSuccess)
    {
        // 代替プロトコル成功ログ
        _loggingManager.LogInfo(
            $"代替プロトコル({GetAlternativeProtocol(initialProtocol)})で接続成功: " +
            $"{_connectionConfig.IpAddress}:{_connectionConfig.Port}");

        return new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            Socket = altSocket,
            UsedProtocol = alternativeProtocol ? "TCP" : "UDP",
            IsFallbackConnection = true,
            FallbackErrorDetails = $"初期プロトコル({(initialProtocol ? "TCP" : "UDP")})失敗: {error}",
            ConnectedAt = DateTime.UtcNow,
            ConnectionTime = (DateTime.UtcNow - startTime).TotalMilliseconds
        };
    }

    // 両プロトコル失敗時のエラーログ
    _loggingManager.LogError(
        $"PLC接続失敗: {_connectionConfig.IpAddress}:{_connectionConfig.Port}. " +
        $"TCP/UDP両プロトコルで接続に失敗しました。\n" +
        $"  - TCP接続エラー: {(initialProtocol ? error : altError)}\n" +
        $"  - UDP接続エラー: {(!initialProtocol ? error : altError)}");

    // 3. 両プロトコル失敗
    return new ConnectionResponse
    {
        Status = ConnectionStatus.Failed,
        ErrorMessage = $"TCP/UDP両プロトコルで接続失敗\n- TCP: {(initialProtocol ? error : altError)}\n- UDP: {(!initialProtocol ? error : altError)}"
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
// ErrorMessages.cs に追加
public static class ErrorMessages
{
    // ログメッセージ
    public static string ConnectionAttemptStarted(string ipAddress, int port, string protocol)
    {
        return $"PLC接続試行開始: {ipAddress}:{port}, プロトコル: {protocol}";
    }

    public static string InitialProtocolFailedRetrying(string failedProtocol, string error, string alternativeProtocol)
    {
        return $"{failedProtocol}接続失敗: {error}. 代替プロトコル({alternativeProtocol})で再試行します。";
    }

    public static string FallbackConnectionSucceeded(string protocol, string ipAddress, int port)
    {
        return $"代替プロトコル({protocol})で接続成功: {ipAddress}:{port}";
    }

    public static string BothProtocolsConnectionFailedDetailed(string ipAddress, int port, string tcpError, string udpError)
    {
        return $"PLC接続失敗: {ipAddress}:{port}. TCP/UDP両プロトコルで接続に失敗しました。\n" +
               $"  - TCP接続エラー: {tcpError}\n" +
               $"  - UDP接続エラー: {udpError}";
    }
}
```

2. ログ出力処理を内部メソッドに分離（可読性向上）:

```csharp
private void LogConnectionAttempt(string protocol)
{
    var message = ErrorMessages.ConnectionAttemptStarted(
        _connectionConfig.IpAddress,
        _connectionConfig.Port,
        protocol);
    _loggingManager.LogInfo(message);
}

private void LogInitialProtocolFailed(string failedProtocol, string error, string alternativeProtocol)
{
    var message = ErrorMessages.InitialProtocolFailedRetrying(
        failedProtocol,
        error,
        alternativeProtocol);
    _loggingManager.LogWarning(message);
}

private void LogFallbackConnectionSucceeded(string protocol)
{
    var message = ErrorMessages.FallbackConnectionSucceeded(
        protocol,
        _connectionConfig.IpAddress,
        _connectionConfig.Port);
    _loggingManager.LogInfo(message);
}

private void LogBothProtocolsFailed(bool initialWasTcp, string initialError, string alternativeError)
{
    var tcpError = initialWasTcp ? initialError : alternativeError;
    var udpError = initialWasTcp ? alternativeError : initialError;

    var message = ErrorMessages.BothProtocolsConnectionFailedDetailed(
        _connectionConfig.IpAddress,
        _connectionConfig.Port,
        tcpError,
        udpError);
    _loggingManager.LogError(message);
}
```

3. ConnectAsync()内でログメソッドを使用:

```csharp
public async Task<ConnectionResponse> ConnectAsync()
{
    var startTime = DateTime.UtcNow;
    var initialProtocol = _connectionConfig.UseTcp;
    var initialProtocolName = GetProtocolName(initialProtocol);

    // 接続開始ログ
    LogConnectionAttempt(initialProtocolName);

    // 1. 初期プロトコルで試行
    var (success, socket, error) = await TryConnectWithProtocolAsync(
        initialProtocol,
        _timeoutConfig.ConnectTimeoutMs);

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

    // 初期プロトコル失敗時の警告ログ
    var alternativeProtocolName = GetAlternativeProtocol(initialProtocol);
    LogInitialProtocolFailed(initialProtocolName, error, alternativeProtocolName);

    // 2. 代替プロトコルで試行
    var alternativeProtocol = !initialProtocol;
    var (altSuccess, altSocket, altError) = await TryConnectWithProtocolAsync(
        alternativeProtocol,
        _timeoutConfig.ConnectTimeoutMs);

    if (altSuccess)
    {
        // 代替プロトコル成功ログ
        LogFallbackConnectionSucceeded(alternativeProtocolName);

        return new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            Socket = altSocket,
            UsedProtocol = alternativeProtocolName,
            IsFallbackConnection = true,
            FallbackErrorDetails = ErrorMessages.InitialProtocolFailed(initialProtocolName, error),
            ConnectedAt = DateTime.UtcNow,
            ConnectionTime = (DateTime.UtcNow - startTime).TotalMilliseconds
        };
    }

    // 両プロトコル失敗時のエラーログ
    LogBothProtocolsFailed(initialProtocol, error, altError);

    // 3. 両プロトコル失敗
    return new ConnectionResponse
    {
        Status = ConnectionStatus.Failed,
        ErrorMessage = ErrorMessages.BothProtocolsConnectionFailed(
            initialProtocol ? error : altError,
            !initialProtocol ? error : altError)
    };
}
```

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

## 次のステップ

Phase 3完了後、**Phase 4: 統合テスト**に進みます。

Phase 4では、接続処理全体（接続→送信→受信）を統合的にテストし、代替プロトコルでも正常にデータ送受信できることを確認します。
