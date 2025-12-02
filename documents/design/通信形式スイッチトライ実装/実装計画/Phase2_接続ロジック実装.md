# Phase 2: 接続ロジック実装（代替プロトコル試行）

**最終更新**: 2025-11-28

## 概要

PlcCommunicationManager.ConnectAsync()メソッドに代替プロトコル試行ロジックを実装します。初期プロトコルでの接続が失敗した場合、自動的に代替プロトコルで再試行します。

## 前提条件

- Phase 1完了（ConnectionResponseに新規プロパティ追加済み）

## TDDサイクル

### Step 2-Red: 失敗するテストを作成

**作業内容:**
1. `PlcCommunicationManagerTests.cs`に以下のテストケースを追加（全て失敗する状態）:

```csharp
[Fact]
public async Task ConnectAsync_初期TCP成功_TCPで接続しIsFallbackConnectionがFalse()
{
    // Arrange: TCP接続が成功するモック設定
    var mockSocket = CreateMockTcpSocket(success: true);

    // Act
    var result = await _manager.ConnectAsync();

    // Assert
    Assert.Equal(ConnectionStatus.Connected, result.Status);
    Assert.Equal("TCP", result.UsedProtocol);  // ← 現在の実装では設定されない
    Assert.False(result.IsFallbackConnection);  // ← 現在の実装では設定されない
}

[Fact]
public async Task ConnectAsync_TCP失敗UDP成功_UDPで接続しIsFallbackConnectionがTrue()
{
    // Arrange: TCP失敗、UDP成功するモック設定
    var mockTcpSocket = CreateMockTcpSocket(success: false);
    var mockUdpSocket = CreateMockUdpSocket(success: true);

    // Act
    var result = await _manager.ConnectAsync();

    // Assert
    Assert.Equal(ConnectionStatus.Connected, result.Status);
    Assert.Equal("UDP", result.UsedProtocol);  // ← 現在の実装では代替試行なし
    Assert.True(result.IsFallbackConnection);   // ← 現在の実装では代替試行なし
    Assert.NotNull(result.FallbackErrorDetails); // ← TCP失敗のエラー詳細
}

[Fact]
public async Task ConnectAsync_両プロトコル失敗_失敗ステータスと詳細エラー()
{
    // Arrange: TCP/UDP両方失敗するモック設定
    var mockTcpSocket = CreateMockTcpSocket(success: false);
    var mockUdpSocket = CreateMockUdpSocket(success: false);

    // Act
    var result = await _manager.ConnectAsync();

    // Assert
    Assert.NotEqual(ConnectionStatus.Connected, result.Status);
    Assert.Contains("TCP", result.ErrorMessage);  // ← 両エラーの詳細が必要
    Assert.Contains("UDP", result.ErrorMessage);  // ← 両エラーの詳細が必要
}
```

2. テスト実行 → **失敗（Red状態）**を確認

**期待される結果:**
- `ConnectAsync_初期TCP成功_TCPで接続しIsFallbackConnectionがFalse()`: ❌ Assert失敗（UsedProtocolがnull）
- `ConnectAsync_TCP失敗UDP成功_UDPで接続しIsFallbackConnectionがTrue()`: ❌ 例外スロー（代替試行なし）
- `ConnectAsync_両プロトコル失敗_失敗ステータスと詳細エラー()`: ❌ Assert失敗（エラー詳細不足）

---

### Step 2-Green: テストを通す最小実装

**作業内容:**
1. `PlcCommunicationManager.cs`に内部ヘルパーメソッドを追加（privateメソッド）:

```csharp
/// <summary>
/// 代替プロトコルの名称を取得
/// </summary>
/// <param name="useTcp">現在のプロトコルがTCPかどうか</param>
/// <returns>代替プロトコル名（"TCP"または"UDP"）</returns>
private string GetAlternativeProtocol(bool useTcp)
{
    return useTcp ? "UDP" : "TCP";
}

/// <summary>
/// 指定されたプロトコルで接続を試行
/// </summary>
/// <param name="useTcp">TCPを使用するかどうか</param>
/// <param name="timeoutMs">接続タイムアウト（ミリ秒）</param>
/// <returns>接続結果（成功/失敗、ソケット、エラー詳細）</returns>
private async Task<(bool success, Socket? socket, string? error)>
    TryConnectWithProtocolAsync(bool useTcp, int timeoutMs)
{
    try
    {
        // 指定プロトコルでの接続試行ロジック
        // （既存のConnectAsyncのロジックを流用）

        Socket socket;
        if (useTcp)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // TCP接続処理
        }
        else
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // UDP接続処理
        }

        // タイムアウト設定
        socket.SendTimeout = timeoutMs;
        socket.ReceiveTimeout = timeoutMs;

        // 接続試行
        var endpoint = new IPEndPoint(IPAddress.Parse(_connectionConfig.IpAddress), _connectionConfig.Port);
        await socket.ConnectAsync(endpoint).WaitAsync(TimeSpan.FromMilliseconds(timeoutMs));

        return (true, socket, null);
    }
    catch (Exception ex)
    {
        return (false, null, ex.Message);
    }
}
```

2. `ConnectAsync()`メソッドを拡張:

```csharp
public async Task<ConnectionResponse> ConnectAsync()
{
    var startTime = DateTime.UtcNow;
    var initialProtocol = _connectionConfig.UseTcp;

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

    // 2. 代替プロトコルで試行
    var alternativeProtocol = !initialProtocol;
    var (altSuccess, altSocket, altError) = await TryConnectWithProtocolAsync(
        alternativeProtocol,
        _timeoutConfig.ConnectTimeoutMs);

    if (altSuccess)
    {
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

    // 3. 両プロトコル失敗
    return new ConnectionResponse
    {
        Status = ConnectionStatus.Failed,
        ErrorMessage = $"TCP/UDP両プロトコルで接続失敗\n- TCP: {(initialProtocol ? error : altError)}\n- UDP: {(!initialProtocol ? error : altError)}"
    };
}
```

3. テスト実行 → **全テスト成功（Green状態）**を確認

**期待される結果:**
- `ConnectAsync_初期TCP成功_TCPで接続しIsFallbackConnectionがFalse()`: ✅ 成功
- `ConnectAsync_TCP失敗UDP成功_UDPで接続しIsFallbackConnectionがTrue()`: ✅ 成功
- `ConnectAsync_両プロトコル失敗_失敗ステータスと詳細エラー()`: ✅ 成功

---

### Step 2-Refactor: コード改善

**作業内容:**
1. 重複コードの削除（プロトコル判定処理を共通化）:

```csharp
private string GetProtocolName(bool useTcp)
{
    return useTcp ? "TCP" : "UDP";
}

// ConnectAsync()内で使用
UsedProtocol = GetProtocolName(initialProtocol),
FallbackErrorDetails = $"初期プロトコル({GetProtocolName(initialProtocol)})失敗: {error}",
```

2. マジックナンバーの定数化:

```csharp
// SlmpConstants.csまたはPlcCommunicationManager内に定数を定義
private const string PROTOCOL_TCP = "TCP";
private const string PROTOCOL_UDP = "UDP";
```

3. エラーメッセージの統一（ErrorMessages.csへの移動）:

```csharp
// ErrorMessages.cs
public static class ErrorMessages
{
    public static string BothProtocolsConnectionFailed(string tcpError, string udpError)
    {
        return $"TCP/UDP両プロトコルで接続失敗\n- TCP: {tcpError}\n- UDP: {udpError}";
    }

    public static string InitialProtocolFailed(string protocol, string error)
    {
        return $"初期プロトコル({protocol})失敗: {error}";
    }
}

// ConnectAsync()内で使用
ErrorMessage = ErrorMessages.BothProtocolsConnectionFailed(
    initialProtocol ? error : altError,
    !initialProtocol ? error : altError)
```

4. テスト実行 → **全テスト成功を維持**

**期待される結果:**
- 全テストが引き続き成功
- コードの可読性・保守性が向上

---

## テストケース一覧

| テストID | テスト名 | Red状態 | Green状態 | 検証内容 |
|---------|---------|---------|----------|---------|
| TC_P2_001 | ConnectAsync_初期TCP成功_TCPで接続しIsFallbackConnectionがFalse | Assert失敗 | テスト成功 | 初期プロトコル成功時の動作 |
| TC_P2_002 | ConnectAsync_初期UDP成功_UDPで接続しIsFallbackConnectionがFalse | Assert失敗 | テスト成功 | UDP初期プロトコル成功時の動作 |
| TC_P2_003 | ConnectAsync_TCP失敗UDP成功_UDPで接続しIsFallbackConnectionがTrue | Assert失敗 | テスト成功 | TCP→UDP代替プロトコル切替 |
| TC_P2_004 | ConnectAsync_UDP失敗TCP成功_TCPで接続しIsFallbackConnectionがTrue | Assert失敗 | テスト成功 | UDP→TCP代替プロトコル切替 |
| TC_P2_005 | ConnectAsync_両プロトコル失敗_失敗ステータスと詳細エラー | Assert失敗 | テスト成功 | 両プロトコル失敗時のエラー情報 |
| TC_P2_006 | ConnectAsync_タイムアウト処理_指定時間内に完了 | Assert失敗 | テスト成功 | タイムアウト設定の適用 |

## 追加のテストケース（Refactor後）

```csharp
[Fact]
public async Task ConnectAsync_初期UDP成功_UDPで接続しIsFallbackConnectionがFalse()
{
    // Arrange: UDP接続が成功するモック設定
    var mockSocket = CreateMockUdpSocket(success: true);

    // Act
    var result = await _manager.ConnectAsync();

    // Assert
    Assert.Equal(ConnectionStatus.Connected, result.Status);
    Assert.Equal("UDP", result.UsedProtocol);
    Assert.False(result.IsFallbackConnection);
}

[Fact]
public async Task ConnectAsync_UDP失敗TCP成功_TCPで接続しIsFallbackConnectionがTrue()
{
    // Arrange: UDP失敗、TCP成功するモック設定
    var mockUdpSocket = CreateMockUdpSocket(success: false);
    var mockTcpSocket = CreateMockTcpSocket(success: true);

    // Act
    var result = await _manager.ConnectAsync();

    // Assert
    Assert.Equal(ConnectionStatus.Connected, result.Status);
    Assert.Equal("TCP", result.UsedProtocol);
    Assert.True(result.IsFallbackConnection);
    Assert.NotNull(result.FallbackErrorDetails);
}

[Fact]
public async Task ConnectAsync_タイムアウト処理_指定時間内に完了()
{
    // Arrange: タイムアウト設定5000ms、接続処理3000ms
    var mockSocket = CreateMockTcpSocketWithDelay(delayMs: 3000);

    // Act
    var startTime = DateTime.UtcNow;
    var result = await _manager.ConnectAsync();
    var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

    // Assert
    Assert.True(elapsed < 10000); // 最大10秒以内（初期5秒+代替5秒）
}
```

## 実装後の確認事項

### 必須確認項目

1. ✅ 全テストがGreen状態
2. ✅ 初期プロトコル成功時の動作確認
3. ✅ 代替プロトコル切替の動作確認
4. ✅ 両プロトコル失敗時のエラー処理確認
5. ✅ タイムアウト設定の適用確認

### 既存コードへの影響確認

1. **ConnectAsync()の呼び出し元**:
   - ExecutionOrchestrator - ConnectionResponseの新規プロパティ（UsedProtocol等）は使用可能だが必須ではない
   - その他の呼び出し元 - 既存動作に影響なし

2. **例外処理への影響**:
   - 現在は例外をスローしていた箇所が、ConnectionResponseで失敗を返すように変更
   - 呼び出し元での例外ハンドリングが不要になる（ConnectionResponse.Statusで判定）

## 想定工数

| ステップ | 作業内容 | 想定工数 | 備考 |
|---------|---------|---------|------|
| **Red** | ConnectAsyncテスト作成（失敗状態） | 1h | Assert失敗確認 |
| **Green** | 代替プロトコル試行ロジック実装 | 1.5h | テスト成功確認 |
| **Refactor** | 重複コード削除・メッセージ統一 | 0.5h | テスト成功維持 |
| **合計** | | **3h** | |

## 次のステップ

Phase 2完了後、**Phase 3: ログ出力実装**に進みます。

Phase 3では、接続試行の各ステップでログ出力を追加し、トラブルシューティングを容易にします。
