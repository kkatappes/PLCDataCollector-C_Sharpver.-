# Phase 4: 統合テスト

**最終更新**: 2025-11-28

## 概要

接続処理全体（接続→送信→受信）を統合的にテストし、代替プロトコルでも正常にデータ送受信できることを確認します。

## 前提条件

- Phase 1完了（ConnectionResponseに新規プロパティ追加済み）
- Phase 2完了（代替プロトコル試行ロジック実装済み）
- Phase 3完了（ログ出力実装済み）

## TDDサイクル

### Step 4-Red: 失敗する統合テストを作成

**作業内容:**
1. `Step3_6_IntegrationTests.cs`に統合テストを追加（初期状態では失敗）:

```csharp
[Fact]
public async Task Integration_TCPからUDPへの自動切り替え_正常にデータ送受信()
{
    // Arrange: TCP接続不可、UDP接続可能な環境を模擬
    var mockTcpSocket = CreateFailingTcpSocket();
    var mockUdpSocket = CreateSuccessfulUdpSocket();

    // テスト用のPLC要求フレーム（ReadRandom）
    var testFrame = CreateTestReadRandomFrame();

    // Act: 接続→データ送信→データ受信の一連の流れ
    var connectResult = await _manager.ConnectAsync();
    var sendResult = await _manager.SendFrameAsync(testFrame);
    var receiveResult = await _manager.ReceiveResponseAsync();

    // Assert
    Assert.True(connectResult.IsFallbackConnection);
    Assert.Equal("UDP", connectResult.UsedProtocol);
    Assert.True(sendResult.Success);  // ← 統合動作確認（初期は失敗）
    Assert.NotNull(receiveResult.Data);
    Assert.True(receiveResult.Success);
}

[Fact]
public async Task Integration_UDPからTCPへの自動切り替え_正常にデータ送受信()
{
    // Arrange: UDP接続不可、TCP接続可能な環境を模擬
    var mockUdpSocket = CreateFailingUdpSocket();
    var mockTcpSocket = CreateSuccessfulTcpSocket();

    var testFrame = CreateTestReadRandomFrame();

    // Act
    var connectResult = await _manager.ConnectAsync();
    var sendResult = await _manager.SendFrameAsync(testFrame);
    var receiveResult = await _manager.ReceiveResponseAsync();

    // Assert
    Assert.True(connectResult.IsFallbackConnection);
    Assert.Equal("TCP", connectResult.UsedProtocol);
    Assert.True(sendResult.Success);
    Assert.NotNull(receiveResult.Data);
    Assert.True(receiveResult.Success);
}

[Fact]
public async Task Integration_両プロトコル失敗_適切なエラーハンドリング()
{
    // Arrange: TCP/UDP両方失敗する環境を模擬
    var mockTcpSocket = CreateFailingTcpSocket();
    var mockUdpSocket = CreateFailingUdpSocket();

    // Act
    var connectResult = await _manager.ConnectAsync();

    // Assert
    Assert.False(connectResult.Status == ConnectionStatus.Connected);
    Assert.Contains("TCP", connectResult.ErrorMessage);
    Assert.Contains("UDP", connectResult.ErrorMessage);

    // 送信は試行されない（接続失敗のため）
    // 例外が発生せず、適切にエラーが返却されることを確認
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
    var mockTcpSocket = CreateSuccessfulTcpSocket();
    var testFrame = CreateTestReadRandomFrame();

    // Act
    var connectResult = await _manager.ConnectAsync();
    var sendResult = await _manager.SendFrameAsync(testFrame);
    var receiveResult = await _manager.ReceiveResponseAsync();

    // Assert
    Assert.False(connectResult.IsFallbackConnection);  // 初期プロトコルで成功
    Assert.Equal("TCP", connectResult.UsedProtocol);
    Assert.True(sendResult.Success);
    Assert.NotNull(receiveResult.Data);
}

[Fact]
public async Task Integration_複数回の送受信_代替プロトコルで安定動作()
{
    // Arrange: TCP失敗、UDP成功
    var mockTcpSocket = CreateFailingTcpSocket();
    var mockUdpSocket = CreateSuccessfulUdpSocket();
    var testFrame = CreateTestReadRandomFrame();

    // Act: 接続後、複数回の送受信
    var connectResult = await _manager.ConnectAsync();

    var sendResult1 = await _manager.SendFrameAsync(testFrame);
    var receiveResult1 = await _manager.ReceiveResponseAsync();

    var sendResult2 = await _manager.SendFrameAsync(testFrame);
    var receiveResult2 = await _manager.ReceiveResponseAsync();

    // Assert
    Assert.Equal("UDP", connectResult.UsedProtocol);

    // 両方の送受信が成功
    Assert.True(sendResult1.Success);
    Assert.NotNull(receiveResult1.Data);
    Assert.True(sendResult2.Success);
    Assert.NotNull(receiveResult2.Data);
}

[Fact]
public async Task Integration_タイムアウト発生_適切なエラー処理()
{
    // Arrange: 接続は成功するがタイムアウトするソケット
    var mockSocket = CreateTimeoutSocket();
    var testFrame = CreateTestReadRandomFrame();

    // Act
    var connectResult = await _manager.ConnectAsync();

    // Assert: 接続は成功
    Assert.Equal(ConnectionStatus.Connected, connectResult.Status);

    // Act: 送信時にタイムアウト
    await Assert.ThrowsAsync<TimeoutException>(async () =>
    {
        await _manager.SendFrameAsync(testFrame);
    });
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
| TC_P4_001 | Integration_TCPからUDPへの自動切り替え_正常にデータ送受信 | Assert失敗 | テスト成功 | TCP→UDP切替での送受信動作 |
| TC_P4_002 | Integration_UDPからTCPへの自動切り替え_正常にデータ送受信 | Assert失敗 | テスト成功 | UDP→TCP切替での送受信動作 |
| TC_P4_003 | Integration_両プロトコル失敗_適切なエラーハンドリング | Assert失敗 | テスト成功 | 完全失敗時の統合動作 |
| TC_P4_004 | Integration_初期プロトコル成功_通常フロー | - | テスト成功 | 初期プロトコル成功時の統合動作 |
| TC_P4_005 | Integration_複数回の送受信_代替プロトコルで安定動作 | - | テスト成功 | 代替プロトコルでの連続送受信 |
| TC_P4_006 | Integration_タイムアウト発生_適切なエラー処理 | - | テスト成功 | タイムアウト時のエラー処理 |

## 実装後の確認事項

### 必須確認項目

1. ✅ 全統合テストがGreen状態
2. ✅ TCP→UDP切替での送受信動作確認
3. ✅ UDP→TCP切替での送受信動作確認
4. ✅ 両プロトコル失敗時のエラーハンドリング確認
5. ✅ 代替プロトコルでの連続送受信動作確認

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

Phase 4完了後、**Phase 5: 実機検証とドキュメント更新**に進みます。

Phase 5では、実機PLC環境でのテストシナリオ作成・検証を実施し、ドキュメントを整備します。
