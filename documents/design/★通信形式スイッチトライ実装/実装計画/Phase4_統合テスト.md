# Phase 4: 統合テスト

**最終更新**: 2025-12-03

## 概要

接続処理全体（接続→送信→受信）を統合的にテストし、代替プロトコルでも正常にデータ送受信できることを確認します。

## ✅ 実装状況の確認

**現在の実装状況**: ✅ **Phase 1-4完了（2025-12-05）**

以下のPhaseが完了済みです:
1. **Phase 1**: ✅ 完了（2025-12-03）- ConnectionResponseモデル拡張（UsedProtocol, IsFallbackConnection, FallbackErrorDetails追加）
2. **Phase 2**: ✅ 完了（2025-12-03 17:30）- 代替プロトコル試行ロジック実装（TryConnectWithProtocolAsync, GetProtocolName追加）
3. **Phase 3**: ✅ 完了（2025-12-03）- LoggingManager統合、ログ出力実装
4. **Phase 4**: ✅ 完了（2025-12-05）- 統合テスト実装（6件）、代替プロトコル切り替えの統合動作確認

詳細な実装状況については、`現在の実装状況.md`および以下の実装結果ドキュメントを参照してください:
- [Phase1_ConnectionResponse拡張_TestResults.md](../実装結果/Phase1_ConnectionResponse拡張_TestResults.md)
- [Phase2_接続ロジック実装_TestResults.md](../実装結果/Phase2_接続ロジック実装_TestResults.md)
- [Phase3_ログ出力実装_TestResults.md](../実装結果/Phase3_ログ出力実装_TestResults.md)
- [Phase4_統合テスト_TestResults.md](../実装結果/Phase4_統合テスト_TestResults.md)

## 前提条件

- ✅ Phase 1完了（ConnectionResponseに新規プロパティ追加済み）→ **完了（2025-12-03）**
- ✅ Phase 2完了（代替プロトコル試行ロジック実装済み）→ **完了（2025-12-03 17:30）**
- ✅ Phase 3完了（ログ出力実装済み）→ **完了（2025-12-03）**

## Phase 3からの引き継ぎ事項

### ✅ Phase 3完了内容（2025-12-03）

**実装済み機能:**
- PlcCommunicationManagerにLoggingManager統合（`_loggingManager`フィールド追加）
- コンストラクタに`ILoggingManager? loggingManager = null`パラメータ追加（null許容・デフォルト引数）
- ConnectAsync()内の4箇所でログ出力:
  1. 接続試行開始（LogInfo）
  2. 初期プロトコル失敗・再試行（LogWarning）
  3. 代替プロトコル接続成功（LogInfo）
  4. 両プロトコル失敗（LogError）

**ErrorMessages.cs新規メソッド（Phase 3追加、ログ出力用）:**
- `ConnectionAttemptStarted(string ipAddress, int port, string protocol)` - 接続試行開始ログメッセージ
- `InitialProtocolFailedRetrying(string failedProtocol, string error, string alternativeProtocol)` - 初期失敗・再試行ログメッセージ
- `FallbackConnectionSucceeded(string protocol, string ipAddress, int port)` - 代替成功ログメッセージ
- `BothProtocolsConnectionFailedDetailed(string ipAddress, int port, string tcpError, string udpError)` - 両失敗詳細ログメッセージ

**テスト結果:**
- Phase 3新規テスト: 3/3成功
- 既存テスト: 42/42成功（影響なし）
- 合計: 45/45成功
- 実行時間: 約1秒
- 詳細: [Phase3_ログ出力実装_TestResults.md](../実装結果/Phase3_ログ出力実装_TestResults.md)

**Phase 4での対応が必要な点:**
1. **統合テストでのLoggingManager設定**:
   - Phase 3でコンストラクタにLoggingManagerパラメータが追加された
   - 統合テストでもLoggingManagerを注入する必要がある（または省略でnull許容動作確認）
   - ログ出力が統合動作でも正常に機能することを確認

2. **統合テストでのログ検証追加**:
   - 接続→送信→受信の統合フローで適切なログが出力されることを確認
   - 特に代替プロトコル成功時のログシーケンス検証
   - 両プロトコル失敗時のエラーログ検証

3. **テスト環境の構築**:
   - Phase 3と同様に`Mock<ILoggingManager>`を使用してログ出力検証
   - 統合テストではログとデータ送受信の両方を検証
   - 既存の統合テストへの影響を確認（コンストラクタ変更による影響）

4. **Phase 3での設計判断の継承**:
   - null許容設計（ILoggingManager?）の継続
   - ErrorMessages.csでのメッセージ統一管理の活用
   - 既存テストへの影響最小化の方針継続

## TDDサイクル

### Step 4-Red: 失敗する統合テストを作成

**作業内容:**
1. `Step3_6_IntegrationTests.cs`に統合テストを追加（初期状態では失敗）:

```csharp
[Fact]
public async Task Integration_TCPからUDPへの自動切り替え_正常にデータ送受信()
{
    // Arrange: TCP接続不可、UDP接続可能な環境を模擬
    var mockLogger = new Mock<ILoggingManager>();  // Phase 3追加: ログ検証用
    var mockTcpSocket = CreateFailingTcpSocket();
    var mockUdpSocket = CreateSuccessfulUdpSocket();
    var manager = CreateManagerWithLogger(mockLogger.Object);  // LoggingManager注入

    // テスト用のPLC要求フレーム（ReadRandom）
    var testFrame = CreateTestReadRandomFrame();

    // Act: 接続→データ送信→データ受信の一連の流れ
    var connectResult = await manager.ConnectAsync();
    var sendResult = await manager.SendFrameAsync(testFrame);
    var receiveResult = await manager.ReceiveResponseAsync();

    // Assert: 接続・送受信の検証
    Assert.True(connectResult.IsFallbackConnection);
    Assert.Equal("UDP", connectResult.UsedProtocol);
    Assert.True(sendResult.Success);  // ← 統合動作確認（初期は失敗）
    Assert.NotNull(receiveResult.Data);
    Assert.True(receiveResult.Success);

    // Assert: ログ出力の検証（Phase 3追加）
    mockLogger.Verify(x => x.LogInfo(
        It.Is<string>(s => s.Contains("PLC接続試行開始"))), Times.Once);
    mockLogger.Verify(x => x.LogWarning(
        It.Is<string>(s => s.Contains("TCP接続失敗") && s.Contains("UDP再試行"))), Times.Once);
    mockLogger.Verify(x => x.LogInfo(
        It.Is<string>(s => s.Contains("代替プロトコル(UDP)で接続成功"))), Times.Once);
}

[Fact]
public async Task Integration_UDPからTCPへの自動切り替え_正常にデータ送受信()
{
    // Arrange: UDP接続不可、TCP接続可能な環境を模擬
    var mockLogger = new Mock<ILoggingManager>();  // Phase 3追加: ログ検証用
    var mockUdpSocket = CreateFailingUdpSocket();
    var mockTcpSocket = CreateSuccessfulTcpSocket();
    var manager = CreateManagerWithLogger(mockLogger.Object);  // LoggingManager注入

    var testFrame = CreateTestReadRandomFrame();

    // Act
    var connectResult = await manager.ConnectAsync();
    var sendResult = await manager.SendFrameAsync(testFrame);
    var receiveResult = await manager.ReceiveResponseAsync();

    // Assert: 接続・送受信の検証
    Assert.True(connectResult.IsFallbackConnection);
    Assert.Equal("TCP", connectResult.UsedProtocol);
    Assert.True(sendResult.Success);
    Assert.NotNull(receiveResult.Data);
    Assert.True(receiveResult.Success);

    // Assert: ログ出力の検証（Phase 3追加）
    mockLogger.Verify(x => x.LogInfo(
        It.Is<string>(s => s.Contains("PLC接続試行開始"))), Times.Once);
    mockLogger.Verify(x => x.LogWarning(
        It.Is<string>(s => s.Contains("UDP接続失敗") && s.Contains("TCP再試行"))), Times.Once);
    mockLogger.Verify(x => x.LogInfo(
        It.Is<string>(s => s.Contains("代替プロトコル(TCP)で接続成功"))), Times.Once);
}

[Fact]
public async Task Integration_両プロトコル失敗_適切なエラーハンドリング()
{
    // Arrange: TCP/UDP両方失敗する環境を模擬
    var mockLogger = new Mock<ILoggingManager>();  // Phase 3追加: ログ検証用
    var mockTcpSocket = CreateFailingTcpSocket();
    var mockUdpSocket = CreateFailingUdpSocket();
    var manager = CreateManagerWithLogger(mockLogger.Object);  // LoggingManager注入

    // Act
    var connectResult = await manager.ConnectAsync();

    // Assert: 接続失敗の検証
    Assert.False(connectResult.Status == ConnectionStatus.Connected);
    Assert.Contains("TCP", connectResult.ErrorMessage);
    Assert.Contains("UDP", connectResult.ErrorMessage);

    // 送信は試行されない（接続失敗のため）
    // 例外が発生せず、適切にエラーが返却されることを確認

    // Assert: ログ出力の検証（Phase 3追加）
    mockLogger.Verify(x => x.LogInfo(
        It.Is<string>(s => s.Contains("PLC接続試行開始"))), Times.Once);
    mockLogger.Verify(x => x.LogWarning(
        It.Is<string>(s => s.Contains("接続失敗") && s.Contains("再試行"))), Times.Once);
    mockLogger.Verify(x => x.LogError(null,
        It.Is<string>(s => s.Contains("TCP/UDP両プロトコルで接続失敗"))), Times.Once);
}
```

2. テスト実行 → **失敗（Red状態）**を確認

**期待される結果:**
- `Integration_TCPからUDPへの自動切り替え_正常にデータ送受信()`: ❌ Assert失敗（統合動作未確認）
- `Integration_UDPからTCPへの自動切り替え_正常にデータ送受信()`: ❌ Assert失敗（統合動作未確認）
- `Integration_両プロトコル失敗_適切なエラーハンドリング()`: ❌ Assert失敗（エラーハンドリング未確認）

---

### Step 4-Green: テストを通す実装

**作業内容:**
1. 既存の`SendFrameAsync()`, `ReceiveResponseAsync()`が代替プロトコルで接続したソケットでも正常動作することを確認

```csharp
// PlcCommunicationManager.cs の SendFrameAsync() と ReceiveResponseAsync() の確認

// SendFrameAsync() - ソケットがTCP/UDPどちらでも動作するか確認
public async Task<SendResult> SendFrameAsync(byte[] frame)
{
    // _socket は ConnectAsync() で設定されたもの（TCP/UDP両対応）
    // 実装の確認と必要に応じた修正
}

// ReceiveResponseAsync() - ソケットがTCP/UDPどちらでも動作するか確認
public async Task<ReceiveResult> ReceiveResponseAsync()
{
    // _socket は ConnectAsync() で設定されたもの（TCP/UDP両対応）
    // 実装の確認と必要に応じた修正
}
```

2. 必要に応じて修正:
   - TCP/UDP共通のソケット操作であることを確認
   - 両プロトコルで動作することをテスト

3. テスト実行 → **全テスト成功（Green状態）**

**期待される結果:**
- `Integration_TCPからUDPへの自動切り替え_正常にデータ送受信()`: ✅ 成功
- `Integration_UDPからTCPへの自動切り替え_正常にデータ送受信()`: ✅ 成功
- `Integration_両プロトコル失敗_適切なエラーハンドリング()`: ✅ 成功

---

### Step 4-Refactor: 統合テストの改善

**作業内容:**
1. テストケースの追加（より詳細なシナリオ）:

```csharp
[Fact]
public async Task Integration_初期プロトコル成功_通常フロー()
{
    // Arrange: 初期TCP接続が成功する環境
    var mockLogger = new Mock<ILoggingManager>();  // Phase 3追加: ログ検証用
    var mockTcpSocket = CreateSuccessfulTcpSocket();
    var manager = CreateManagerWithLogger(mockLogger.Object);  // LoggingManager注入
    var testFrame = CreateTestReadRandomFrame();

    // Act
    var connectResult = await manager.ConnectAsync();
    var sendResult = await manager.SendFrameAsync(testFrame);
    var receiveResult = await manager.ReceiveResponseAsync();

    // Assert: 接続・送受信の検証
    Assert.False(connectResult.IsFallbackConnection);  // 初期プロトコルで成功
    Assert.Equal("TCP", connectResult.UsedProtocol);
    Assert.True(sendResult.Success);
    Assert.NotNull(receiveResult.Data);

    // Assert: ログ出力の検証（Phase 3追加）
    mockLogger.Verify(x => x.LogInfo(
        It.Is<string>(s => s.Contains("PLC接続試行開始"))), Times.Once);
    // 初期プロトコル成功時は警告・エラーログなし
    mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
    mockLogger.Verify(x => x.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
}

[Fact]
public async Task Integration_複数回の送受信_代替プロトコルで安定動作()
{
    // Arrange: TCP失敗、UDP成功
    var mockLogger = new Mock<ILoggingManager>();  // Phase 3追加: ログ検証用
    var mockTcpSocket = CreateFailingTcpSocket();
    var mockUdpSocket = CreateSuccessfulUdpSocket();
    var manager = CreateManagerWithLogger(mockLogger.Object);  // LoggingManager注入
    var testFrame = CreateTestReadRandomFrame();

    // Act: 接続後、複数回の送受信
    var connectResult = await manager.ConnectAsync();

    var sendResult1 = await manager.SendFrameAsync(testFrame);
    var receiveResult1 = await manager.ReceiveResponseAsync();

    var sendResult2 = await manager.SendFrameAsync(testFrame);
    var receiveResult2 = await manager.ReceiveResponseAsync();

    // Assert: 接続・送受信の検証
    Assert.Equal("UDP", connectResult.UsedProtocol);

    // 両方の送受信が成功
    Assert.True(sendResult1.Success);
    Assert.NotNull(receiveResult1.Data);
    Assert.True(sendResult2.Success);
    Assert.NotNull(receiveResult2.Data);

    // Assert: ログ出力の検証（Phase 3追加）
    mockLogger.Verify(x => x.LogInfo(
        It.Is<string>(s => s.Contains("代替プロトコル(UDP)で接続成功"))), Times.Once);
}

[Fact]
public async Task Integration_タイムアウト発生_適切なエラー処理()
{
    // Arrange: 接続は成功するがタイムアウトするソケット
    var mockLogger = new Mock<ILoggingManager>();  // Phase 3追加: ログ検証用
    var mockSocket = CreateTimeoutSocket();
    var manager = CreateManagerWithLogger(mockLogger.Object);  // LoggingManager注入
    var testFrame = CreateTestReadRandomFrame();

    // Act
    var connectResult = await manager.ConnectAsync();

    // Assert: 接続は成功
    Assert.Equal(ConnectionStatus.Connected, connectResult.Status);

    // Act: 送信時にタイムアウト
    await Assert.ThrowsAsync<TimeoutException>(async () =>
    {
        await manager.SendFrameAsync(testFrame);
    });

    // Assert: ログ出力の検証（Phase 3追加）
    // 接続成功時のログ出力確認
    mockLogger.Verify(x => x.LogInfo(
        It.Is<string>(s => s.Contains("PLC接続試行開始"))), Times.Once);
}
```

2. モックの共通化・テストユーティリティ化:

```csharp
// Tests/TestUtilities/Mocks/SocketMockFactory.cs
public static class SocketMockFactory
{
    public static Socket CreateSuccessfulTcpSocket()
    {
        // TCP接続成功のモックソケット作成
    }

    public static Socket CreateFailingTcpSocket()
    {
        // TCP接続失敗のモックソケット作成
    }

    public static Socket CreateSuccessfulUdpSocket()
    {
        // UDP接続成功のモックソケット作成
    }

    public static Socket CreateFailingUdpSocket()
    {
        // UDP接続失敗のモックソケット作成
    }

    public static Socket CreateTimeoutSocket()
    {
        // タイムアウト発生のモックソケット作成
    }
}

// Tests/TestUtilities/TestData/SlmpFrameSamples/TestFrameFactory.cs
public static class TestFrameFactory
{
    public static byte[] CreateTestReadRandomFrame()
    {
        // テスト用ReadRandomフレーム作成
    }

    public static byte[] CreateTestReadRandomResponse()
    {
        // テスト用ReadRandomレスポンス作成
    }
}
```

3. テスト実行 → **全テスト成功を維持**

**期待される結果:**
- 全テストが引き続き成功
- テストコードの可読性・保守性が向上
- モック/テストデータが再利用可能になる

---

## テストケース一覧

| テストID | テスト名 | Red状態 | Green状態 | 検証内容 |
|---------|---------|---------|----------|---------|
| TC_P4_001 | Integration_TCPからUDPへの自動切り替え_正常にデータ送受信 | Assert失敗 | テスト成功 | TCP→UDP切替での送受信動作、ログ出力検証 |
| TC_P4_002 | Integration_UDPからTCPへの自動切り替え_正常にデータ送受信 | Assert失敗 | テスト成功 | UDP→TCP切替での送受信動作、ログ出力検証 |
| TC_P4_003 | Integration_両プロトコル失敗_適切なエラーハンドリング | Assert失敗 | テスト成功 | 完全失敗時の統合動作、エラーログ検証 |
| TC_P4_004 | Integration_初期プロトコル成功_通常フロー | - | テスト成功 | 初期プロトコル成功時の統合動作、ログ出力検証 |
| TC_P4_005 | Integration_複数回の送受信_代替プロトコルで安定動作 | - | テスト成功 | 代替プロトコルでの連続送受信、ログ出力検証 |
| TC_P4_006 | Integration_タイムアウト発生_適切なエラー処理 | - | テスト成功 | タイムアウト時のエラー処理、ログ出力検証 |

**Phase 3追加の検証項目:**
- 各テストケースで`Mock<ILoggingManager>`を使用したログ出力検証を追加
- 接続試行開始、代替プロトコル切替、成功/失敗時のログレベル（INFO/WARNING/ERROR）を確認
- ログメッセージ内容の検証（プロトコル名、IPアドレス、エラー詳細）

## 実装後の確認事項

### 必須確認項目

1. ✅ 全統合テストがGreen状態 - **Phase 4完了（2025-12-05）**
2. ✅ TCP→UDP切替での送受信動作確認 - **Phase 4完了（2025-12-05）**
3. ✅ UDP→TCP切替での送受信動作確認 - **Phase 4完了（2025-12-05）**
4. ✅ 両プロトコル失敗時のエラーハンドリング確認 - **Phase 4完了（2025-12-05）**
5. ✅ 代替プロトコルでの連続送受信動作確認 - **Phase 4完了（2025-12-05）**
6. ✅ ログ出力が統合フローで正常動作することの確認 - **Phase 4完了（2025-12-05、Phase 3追加項目）**
7. ✅ LoggingManager注入の有無による動作確認 - **Phase 4完了（2025-12-05、Phase 3追加項目）**

### パフォーマンス確認

1. **接続時間**:
   - 初期プロトコル成功時: タイムアウト設定内（例: 5秒以内）
   - 代替プロトコル成功時: タイムアウト設定×2以内（例: 10秒以内）

2. **送受信時間**:
   - 代替プロトコルでも初期プロトコルと同等のパフォーマンス

## 想定工数

| ステップ | 作業内容 | 想定工数 | 備考 |
|---------|---------|---------|------|
| **Red** | 統合テスト作成（失敗状態） | 0.5h | Assert失敗確認 |
| **Green** | 統合動作確認・必要に応じて修正 | 1h | テスト成功確認 |
| **Refactor** | テストケース追加・モック共通化 | 0.5h | テスト成功維持 |
| **合計** | | **2h** | |

## 次のステップ

### ✅ Phase 1-4完了済み（2025-12-05）

### 実装順序
1. **Phase 1**: ConnectionResponseモデル拡張 - ✅ **完了（2025-12-03）**
2. **Phase 2**: 接続ロジック実装（代替プロトコル試行）- ✅ **完了（2025-12-03 17:30）**
3. **Phase 3**: ログ出力実装 - ✅ **完了（2025-12-03）**
4. **Phase 4**: 統合テスト（このフェーズ） - ✅ **完了（2025-12-05）**
5. **Phase 5**: 実機検証とドキュメント更新 - **次のフェーズ**

### Phase 4実装完了状況

**✅ 実装完了**:
- Phase 1-3の機能統合動作確認完了
- 統合テスト6件実装・成功（TC_P4_001～TC_P4_006）
- 既存テスト4件への影響なし（全10テスト成功）
- TDDサイクル（Red-Green-Refactor）完全実施
- テスト実行時間589ms（Phase 4要件: 600ms以内）
- 実装結果ドキュメント作成完了

**Phase 4達成事項**:
1. ✅ 接続→送信→受信の統合テスト作成完了（TDD Red-Green-Refactor）
2. ✅ ログ出力が統合フローで正常動作することの確認完了
3. ✅ 代替プロトコルでの送受信安定性確認完了
4. ✅ 既存の統合テストへの影響確認完了（影響なし）
5. ✅ テスト結果ドキュメント作成完了（Phase4_統合テスト_TestResults.md）

**Phase 5へ準備完了**: 実機検証とドキュメント更新の実施が可能です。

## 関連ドキュメント

- [Phase0_概要と前提条件.md](Phase0_概要と前提条件.md) - 全体概要と仕様
- [Phase1_ConnectionResponse拡張.md](Phase1_ConnectionResponse拡張.md) - Phase 1実装計画
- [Phase2_接続ロジック実装.md](Phase2_接続ロジック実装.md) - Phase 2実装計画
- [Phase3_ログ出力実装.md](Phase3_ログ出力実装.md) - Phase 3実装計画
- [Phase5_実機検証とドキュメント.md](Phase5_実機検証とドキュメント.md) - Phase 5実装計画
- [Phase1_ConnectionResponse拡張_TestResults.md](../実装結果/Phase1_ConnectionResponse拡張_TestResults.md) - Phase 1実装結果
- [Phase2_接続ロジック実装_TestResults.md](../実装結果/Phase2_接続ロジック実装_TestResults.md) - Phase 2実装結果
- [Phase3_ログ出力実装_TestResults.md](../実装結果/Phase3_ログ出力実装_TestResults.md) - Phase 3実装結果
- [Phase4_統合テスト_TestResults.md](../実装結果/Phase4_統合テスト_TestResults.md) - Phase 4実装結果
- [現在の実装状況.md](現在の実装状況.md) - 最新の実装状況
