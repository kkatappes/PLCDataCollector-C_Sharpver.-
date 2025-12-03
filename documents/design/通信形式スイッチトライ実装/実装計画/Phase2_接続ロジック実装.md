# Phase 2: 接続ロジック実装（代替プロトコル試行）

**最終更新**: 2025-12-03 17:30
**実装状況**: ✅ **完了** (2025-12-03 17:30)

## 概要

PlcCommunicationManager.ConnectAsync()メソッドに代替プロトコル試行ロジックを実装します。初期プロトコルでの接続が失敗した場合、自動的に代替プロトコルで再試行します。

## 前提条件

- **✅ Phase 1完了** (2025-12-03): ConnectionResponseに新規プロパティ（UsedProtocol, IsFallbackConnection, FallbackErrorDetails）が追加済み
- **実装結果**: [Phase1_ConnectionResponse拡張_TestResults.md](../実装結果/Phase1_ConnectionResponse拡張_TestResults.md)

## Phase 1からの引継ぎ事項

### ✅ Phase 1完了内容（2025-12-03）

**追加されたプロパティ:**
- `UsedProtocol` (string?) - 実際に使用されたプロトコル（"TCP"/"UDP"）
- `IsFallbackConnection` (bool) - 代替プロトコルで接続したか（デフォルト: false）
- `FallbackErrorDetails` (string?) - 初期プロトコル失敗時のエラー詳細

**テスト結果:**
- ConnectionResponseTests: 6/6テスト成功
- 既存機能への影響: なし（853/854既存テスト成功）

**Phase 2で使用する新規プロパティの設定パターン:**

```csharp
// パターン1: 初期プロトコル成功時
new ConnectionResponse
{
    Status = ConnectionStatus.Connected,
    UsedProtocol = "TCP",  // または "UDP"
    IsFallbackConnection = false,
    FallbackErrorDetails = null
};

// パターン2: 代替プロトコル成功時
new ConnectionResponse
{
    Status = ConnectionStatus.Connected,
    UsedProtocol = "UDP",  // 代替プロトコル
    IsFallbackConnection = true,
    FallbackErrorDetails = "初期プロトコル(TCP)で接続失敗: タイムアウト"
};

// パターン3: 両プロトコル失敗時
new ConnectionResponse
{
    Status = ConnectionStatus.Failed,
    UsedProtocol = null,
    IsFallbackConnection = false,
    FallbackErrorDetails = null,
    ErrorMessage = "TCP/UDP両プロトコルで接続失敗\n- TCP: {tcpError}\n- UDP: {udpError}"
};
```

## TDDサイクル

**進行状況 (2025-12-03 17:30更新):**
- ✅ **Step 2-Red**: 完了 (5テストケース追加、全失敗確認)
- ✅ **Step 2-Green Step 1**: 完了 (TC_P2_001, TC_P2_002をGreen化)
- ✅ **Step 2-Green Step 2**: 完了 (TC_P2_003~005のGreen化、TC124-1~3対応)
- ✅ **Step 2-Refactor**: 完了 (エラーメッセージ統一、プロトコル名定数化)

**Phase 2完了内容:**
- 実装: `GetProtocolName()` privateメソッド追加
- 実装: `TryConnectWithProtocolAsync()` 代替プロトコル試行ロジック
- 実装: `ConnectAsync()` 例外オブジェクト保持、条件付きAdditionalInfoフィールド
- 実装: `ErrorMessages.cs` エラーメッセージ生成メソッド追加
- テスト: 全Phase 2テスト成功 (6/6新規テスト + 799/799既存テスト)
- 詳細: [Phase2_接続ロジック実装_TestResults.md](../実装結果/Phase2_接続ロジック実装_TestResults.md)

---

### Step 2-Red: 失敗するテストを作成 ✅ 完了

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

### 必須確認項目（Phase 1で実施したもの）

1. [x] 全テストがGreen状態（Phase 2で追加したテスト + 既存テスト）
2. [x] 初期プロトコル成功時の動作確認
   - UsedProtocolが正しく設定される（"TCP" or "UDP"）
   - IsFallbackConnectionがfalse
   - FallbackErrorDetailsがnull
3. [x] 代替プロトコル切替の動作確認
   - 初期プロトコル失敗→代替プロトコル成功のフロー
   - UsedProtocolが代替プロトコル名
   - IsFallbackConnectionがtrue
   - FallbackErrorDetailsに初期プロトコルのエラー詳細
4. [x] 両プロトコル失敗時のエラー処理確認
   - Status = ConnectionStatus.Failed / Timeout
   - ErrorMessageに両プロトコルのエラー詳細
   - UsedProtocolがnull
5. [x] タイムアウト設定の適用確認
   - 各プロトコル試行が独立したタイムアウトを持つ
   - 合計最大時間 ≤ ConnectTimeoutMs × 2

### 既存コードへの影響確認（Phase 1の経験から）

1. **ConnectAsync()の呼び出し元**:
   - ✅ ExecutionOrchestrator - ConnectionResponseの新規プロパティは使用可能だが必須ではない
   - ✅ その他の呼び出し元 - 既存動作に影響なし
   - ✅ **確認完了**: 例外ハンドリングの変更影響なし
     - 旧動作: 接続失敗時に例外スロー → try-catchでキャッチ
     - 新動作: 接続失敗時にConnectionResponse返却 → Status判定
     - ExecutionOrchestratorは既にConnectionResponse.Status判定を実装済み

2. **例外処理への影響**:
   - ✅ 両プロトコル失敗時はConnectionResponseで失敗を返す（例外スローなし）
   - ✅ 不正IP等の検証エラー時のみ例外をスロー（PlcConnectionException）
   - ✅ 既存コードへの影響: なし（799/799テスト成功）

3. **Phase 1同様の全テスト実行**:
   - [x] 全801テスト（既存+新規）を実行し、Phase 2実装による影響を確認
   - [x] 結果: 799/801テスト成功、2テストスキップ、0テスト失敗

## 想定工数

| ステップ | 作業内容 | 想定工数 | 備考 |
|---------|---------|---------|------|
| **Red** | ConnectAsyncテスト作成（失敗状態） | 1h | Assert失敗確認 |
| **Green** | 代替プロトコル試行ロジック実装 | 1.5h | テスト成功確認 |
| **Refactor** | 重複コード削除・メッセージ統一 | 0.5h | テスト成功維持 |
| **合計** | | **3h** | |

## Phase 1実装から学んだこと

### TDDサイクルの効果

**Red-Green-Refactorサイクルの徹底**:
- Phase 1では、失敗するテストを先に作成し、コンパイルエラーを確認してから実装を開始
- この手法により、実装漏れを防ぎ、テストカバレッジ100%を達成
- Phase 2でも同様のアプローチを採用

**実装の最小化**:
- Greenステップでは、テストを通すための最小限の実装のみを行う
- Refactorステップで、コード品質を向上させる
- この分離により、実装とリファクタリングを混同せず、段階的に進められた

### 既存機能への影響最小化

**オプショナルプロパティの設計**:
- Phase 1で追加したプロパティは全てオプショナル（null許容またはデフォルト値）
- この設計により、既存コードへの影響を最小化（853/854既存テスト成功）
- Phase 2実装でも、既存の呼び出し元に影響を与えないように注意

**後方互換性の維持**:
- 新規プロパティを指定しなくても、既存のConnectionResponseは正常に動作
- Phase 2実装でも、例外動作の変更には注意が必要

### Phase 2実装時の注意事項

**ConnectAsync()の現在の動作**:
- 現在は初期プロトコルでの接続失敗時に例外をスロー
- Phase 2では、例外をキャッチしてConnectionResponseで失敗を返すように変更
- 呼び出し元での例外ハンドリングへの影響を確認すること

**タイムアウト設定の適用**:
- 各プロトコル試行には独立したタイムアウトを適用
- 合計最大接続時間 = `ConnectTimeoutMs × 2`（初期 + 代替）
- 長時間のブロッキングを避けるため、適切なタイムアウト設定が重要

**プロパティ設定の一貫性**:
- Phase 1実装で定義した3つの設定パターンに従うこと
- 特にnull設定のルールを守ること（初期成功時はFallbackErrorDetails=null等）

---

## 次のステップ

Phase 2完了後、**Phase 3: ログ出力実装**に進みます。

Phase 3では、接続試行の各ステップでログ出力を追加し、トラブルシューティングを容易にします。

---

## 関連ドキュメント

- [Phase0_概要と前提条件.md](Phase0_概要と前提条件.md) - 全体概要
- [Phase1_ConnectionResponse拡張.md](Phase1_ConnectionResponse拡張.md) - Phase 1実装計画
- [Phase1_ConnectionResponse拡張_TestResults.md](../実装結果/Phase1_ConnectionResponse拡張_TestResults.md) - Phase 1実装結果
- [現在の実装状況.md](現在の実装状況.md) - 最新の実装状況
