# TC025 ReceiveResponseAsync 正常受信テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC025（ReceiveResponseAsync_正常受信）のテストケースを、TDD手法に従って実装してください。

---

## TC025: ReceiveResponseAsync_正常受信 ★重要

### 目的
PLCからのSLMPレスポンス受信機能の正常動作を検証する重要テスト
19時deadline対応の最小成功基準に含まれる必須テスト

### 前提条件
- PlcCommunicationManagerが接続済み状態（ConnectAsync完了）
- SendFrameAsync完了状態（READコマンド送信済み）
- MockPlcServerが4Eフレーム形式で応答準備完了
- TimeoutConfig設定済み（ReceiveTimeoutMs: 3000）

### TDD実装手順

#### Arrange（準備）
```csharp
// TimeoutConfig準備
var timeoutConfig = new TimeoutConfig
{
    ReceiveTimeoutMs = 3000
};

// MockPlcServer準備（4Eフレーム応答）
var mockPlcServer = new MockPlcServer();
mockPlcServer.SetResponse4EFrame("D4001234"); // 4Eフレーム識別ヘッダー
mockPlcServer.SetM000ToM999ReadResponse(); // M000-M999読み込み応答データ

// PlcCommunicationManager接続済み状態
var manager = new PlcCommunicationManager(mockSocket, logger, errorHandler);
await manager.ConnectAsync(connectionConfig, timeoutConfig);
await manager.SendFrameAsync(readCommand);
```

#### Act（実行）
```csharp
var result = await manager.ReceiveResponseAsync(timeoutConfig.ReceiveTimeoutMs);
```

#### Assert（検証）
```csharp
// 受信データ検証
Assert.NotNull(result.ResponseData);
Assert.True(result.ResponseData.Length > 0);
Assert.Equal(result.DataLength, result.ResponseData.Length);

// 時刻・時間検証
Assert.NotNull(result.ReceivedAt);
Assert.True(result.ReceivedAt.Value <= DateTime.UtcNow);
Assert.NotNull(result.ReceiveTime);
Assert.True(result.ReceiveTime.Value.TotalMilliseconds > 0);
Assert.True(result.ReceiveTime.Value.TotalMilliseconds < timeoutConfig.ReceiveTimeoutMs);

// 16進数文字列検証（4Eフレーム確認）
Assert.NotNull(result.ResponseHex);
Assert.False(string.IsNullOrEmpty(result.ResponseHex));
Assert.True(result.ResponseHex.StartsWith("D4001234")); // 4Eフレーム識別

// フレームタイプ検証
Assert.Equal(FrameType.Frame4E, result.FrameType);

// エラー情報検証（成功時はnull）
Assert.Null(result.ErrorMessage);
```

### メソッド実装仕様

#### ReceiveResponseAsync実装
```csharp
public async Task<RawResponseData> ReceiveResponseAsync(int receiveTimeoutMs)
{
    // 1. 接続状態チェック
    if (_socket == null || !_socket.Connected)
    {
        throw new InvalidOperationException(ErrorMessages.NotConnectedForReceive);
    }

    // 2. 受信バッファ準備
    byte[] buffer = new byte[8192];
    var startTime = DateTime.UtcNow;

    // 3. タイムアウト制御付き受信
    var cts = new CancellationTokenSource(receiveTimeoutMs);

    try
    {
        int receivedBytes = await _socket.ReceiveAsync(
            new ArraySegment<byte>(buffer),
            SocketFlags.None,
            cts.Token
        );

        var receiveTime = DateTime.UtcNow - startTime;

        // 4. 受信データ処理
        byte[] responseData = new byte[receivedBytes];
        Array.Copy(buffer, responseData, receivedBytes);

        // 5. 16進数文字列変換
        string responseHex = Convert.ToHexString(responseData);

        // 6. フレームタイプ判定
        FrameType frameType = responseHex.StartsWith("D4001234")
            ? FrameType.Frame4E
            : FrameType.Frame3E;

        return new RawResponseData
        {
            ResponseData = responseData,
            DataLength = receivedBytes,
            ReceivedAt = DateTime.UtcNow,
            ReceiveTime = receiveTime,
            ResponseHex = responseHex,
            FrameType = frameType,
            ErrorMessage = null
        };
    }
    catch (OperationCanceledException)
    {
        throw new TimeoutException(
            string.Format(ErrorMessages.ReceiveTimeout, receiveTimeoutMs));
    }
    catch (SocketException ex)
    {
        throw new SocketException(
            string.Format(ErrorMessages.ReceiveError, ex.Message));
    }
}
```

### 必要なクラス・列挙型

#### RawResponseData（データ転送オブジェクト）
```csharp
public class RawResponseData
{
    public byte[] ResponseData { get; set; }      // 受信した生バイトデータ
    public int DataLength { get; set; }           // 受信データサイズ
    public DateTime? ReceivedAt { get; set; }     // 受信完了時刻
    public TimeSpan? ReceiveTime { get; set; }    // 受信処理時間
    public string? ResponseHex { get; set; }      // 16進数文字列表現
    public FrameType FrameType { get; set; }      // フレームタイプ（4E/3E）
    public string? ErrorMessage { get; set; }     // エラーメッセージ（失敗時のみ）
}
```

#### FrameType（フレーム種別列挙型）
```csharp
public enum FrameType
{
    Frame3E,     // 3Eフレーム
    Frame4E,     // 4Eフレーム（TC025で期待される形式）
    Unknown      // 不明なフレーム
}
```

#### ErrorMessages（エラーメッセージ定数）
```csharp
public static class ErrorMessages
{
    public const string ReceiveTimeout = "受信タイムアウト（タイムアウト時間: {0}ms）";
    public const string ReceiveError = "受信エラー: {0}";
    public const string NotConnectedForReceive = "未接続状態です。受信前にConnectAsync()を実行してください";
}
```

### テストユーティリティ

#### MockPlcServer（4Eフレーム応答対応）
```csharp
public class MockPlcServer
{
    public void SetResponse4EFrame(string frameHeader)
    {
        // 4Eフレーム形式の応答データ設定
        // frameHeader: "D4001234"
    }

    public void SetM000ToM999ReadResponse()
    {
        // M000-M999読み込み応答データ（1000点分）を設定
    }
}
```

### 期待される動作フロー
1. **接続状態チェック**: 接続済み状態であることを確認
2. **受信バッファ準備**: byte[]配列初期化（8192バイト）
3. **受信実行**: Socket.ReceiveAsync実行、ReceiveTimeoutMs適用
4. **データ受信**: 0より大きいサイズのデータ受信完了
5. **時間計測**: 受信処理時間を正確に記録
6. **データ変換**: バイトデータを16進数文字列に変換
7. **フレーム判定**: 先頭バイトから4E/3Eフレーム判定
8. **受信時刻記録**: ReceivedAt設定
9. **戻り値生成**: RawResponseData生成・返却

### 4Eフレーム受信データ構造
**標準4Eレスポンスフレーム**:
1. フレーム識別: "D4001234"（8バイト、16進数）
2. データ長: レスポンスデータサイズ（4バイト）
3. 終了コード: "0000"（正常終了、4バイト）
4. データ部: 実際のPLCデータ（可変長）

### 完了条件

- [ ] TC025（ReceiveResponseAsync_正常受信）テストがパス
- [ ] RawResponseDataの全プロパティが正しく設定されることを確認
- [ ] 4Eフレームタイプの正確な判定確認
- [ ] 受信時間が正確に計測されることを確認
- [ ] 16進数文字列変換が正確に実行されることを確認
- [ ] タイムアウト制御が適切に機能することを確認
- [ ] チェックリストの該当項目にチェック

### 重要ポイント
- **19時deadline最小成功基準**: TC025は★重要テストで必須
- **実機環境対応**: 実際のPLC Q00UDPCPU環境でも動作する設計
- **TDD準拠**: Red→Green→Refactorサイクルを遵守
- **エラーハンドリング**: 適切な例外処理とメッセージ設定

---

以上の指示に従って実装してください。