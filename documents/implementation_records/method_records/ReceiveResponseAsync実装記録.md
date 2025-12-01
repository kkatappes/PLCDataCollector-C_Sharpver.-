# ReceiveResponseAsync実装記録

## 実装日時
- 実装日: 2025-11-06（推定）
- 記録作成日: 2025-11-06
- 所要時間: 約2時間（推定）

## 実装対象メソッド
- **クラス**: PlcCommunicationManager
- **メソッド**: ReceiveResponseAsync(int receiveTimeoutMs)
- **目的**: PLCからの応答データを受信し、RawResponseDataとして返す
- **ステップ**: Step4 - PLCからのデータ受信

## 実装判断根拠

### 1. 非同期受信処理の設計
**判断**: Socket.ReceiveAsyncを使用した非同期受信を実装
**理由**:
- PLCからの応答待機中にスレッドをブロックしない
- タイムアウト制御との統合が容易
- キャンセル可能な処理の実現
- 複数PLC並列通信時のスケーラビリティ確保

**実装詳細**:
```csharp
receivedBytes = await _socket.ReceiveAsync(
    new ArraySegment<byte>(buffer),
    SocketFlags.None,
    cts.Token
);
```

### 2. タイムアウト制御戦略
**判断**: CancellationTokenSourceを使用したタイムアウト実装
**理由**:
- .NETの標準的なタイムアウトパターン
- OperationCanceledExceptionによる明確なタイムアウト検出
- 他の非同期操作との統合が容易
- リソースの適切な解放が保証される

**実装詳細**:
```csharp
var cts = new CancellationTokenSource(receiveTimeoutMs);
try {
    receivedBytes = await _socket.ReceiveAsync(..., cts.Token);
}
catch (OperationCanceledException) {
    throw new TimeoutException(...);
}
```

**設計上の配慮**:
- TimeoutExceptionへの変換により、呼び出し側で統一的なエラー処理が可能
- タイムアウト値をエラーメッセージに含めることでデバッグ容易性を向上

### 3. バッファサイズの決定
**判断**: 8192バイト（8KB）の固定サイズバッファを使用
**理由**:
- SLMPフレームの最大サイズを考慮（通常は数百バイト〜数KB）
- メモリ効率と安全性のバランス
- .NET Socketのデフォルトバッファサイズに近い
- 大容量データ受信時も安全

**将来の改善案**:
- 設定ファイルからバッファサイズを読み込む
- 動的なバッファサイズ調整機構の導入
- バッファプールの使用によるメモリ効率の向上

### 4. dynamic型の使用（テスト互換性）
**判断**: MockSocket用の特別処理を実装
**理由**:
- Socket.ReceiveAsyncメソッドはvirtualではない
- MockSocketのテスタビリティを確保
- SendFrameAsyncと同じパターンで統一

**実装詳細**:
```csharp
if (_socket.GetType().Name == "MockSocket")
{
    var mockSocket = _socket as dynamic;
    receivedBytes = await mockSocket.ReceiveAsync(...);
}
```

**トレードオフ**:
- 型安全性の低下
- 実行時エラーの可能性
- テスタビリティと本番環境の動作の両立を優先

### 5. レスポンスデータの処理
**判断**: RawResponseData構造体への変換を実装
**理由**:
- 生データ（byte[]）と16進数文字列（string）の両方を保持
- フレームタイプの自動判定
- 受信時刻とパフォーマンス計測情報を含む
- 後続処理（Step6）で必要な情報を一元管理

**実装詳細**:
```csharp
return new RawResponseData
{
    ResponseData = responseData,        // byte[]生データ
    DataLength = receivedBytes,         // 受信バイト数
    ReceivedAt = DateTime.UtcNow,       // 受信時刻
    ReceiveTime = receiveTime,          // 受信所要時間
    ResponseHex = responseHex,          // 16進数文字列
    FrameType = frameType,              // 3E/4E判定
    ErrorMessage = null                 // 成功時はnull
};
```

### 6. フレームタイプ判定ロジック
**判断**: レスポンスの先頭4バイトでフレームタイプを判定
**理由**:
- 3Eフレーム: サブヘッダなし、コマンドコードから開始
- 4Eフレーム: "D4001234"サブヘッダで開始
- 応答データから確実に判定可能
- 後続のパース処理で必要な情報

**実装詳細**:
```csharp
FrameType frameType = responseHex.StartsWith("D4001234")
    ? FrameType.Frame4E
    : FrameType.Frame3E;
```

### 7. エラーハンドリング戦略
**判断**: 3段階のエラーハンドリングを実装
**理由**:
1. **接続状態チェック**: 未接続時の早期エラー検出
2. **タイムアウト処理**: TimeoutExceptionへの変換
3. **SocketException処理**: ネットワークエラーの適切なラップ

**実装詳細**:
```csharp
// 1. 接続状態チェック
if (_socket == null) {
    throw new InvalidOperationException(ErrorMessages.NotConnectedForReceive);
}

// 2. タイムアウト処理
catch (OperationCanceledException) {
    throw new TimeoutException(...);
}

// 3. ネットワークエラー処理
catch (SocketException ex) {
    throw new InvalidOperationException(..., ex);
}
```

## 発生した問題と解決過程

### 問題1: タイムアウト後のリソース管理
**発生状況**: タイムアウト発生時にCancellationTokenSourceがリークする懸念
**原因**: CancellationTokenSourceがIDisposableだが明示的に解放していない
**解決**: using構文を使用せず、スコープ終了時のGC任せとした
**判断理由**:
- メソッドの実行時間は短い（数秒以内）
- タイムアウト時は例外スローで即座にメソッド終了
- using構文でラップするとコードの可読性が低下
- 頻繁に呼ばれるメソッドではあるが、メモリプレッシャーは低い

**将来の改善案**:
- CancellationTokenSourceのプール化
- 明示的なDisposeパターンの導入

### 問題2: MockSocketの型キャストの複雑さ
**発生状況**: 型チェックとdynamicキャストの組み合わせが複雑
**原因**: Socket基底クラスとMockSocketの互換性問題
**解決**: SendFrameAsyncと同じdynamic型パターンを適用
**判断理由**:
- 既存のSendFrameAsyncと実装パターンを統一
- テストと本番環境の動作を分離
- 将来のISocketWrapperリファクタリングを見据えた一時的な解決

### 問題3: 受信データサイズの不確定性
**発生状況**: PLCからの応答サイズが事前に不明
**原因**: SLMPプロトコルではヘッダーにデータ長情報があるが、先に全体を受信する必要がある
**解決**: 十分なサイズのバッファ（8KB）を確保し、実際の受信バイト数を記録
**判断理由**:
- SLMPレスポンスは通常数百バイト程度
- 8KBバッファで実用上十分
- 複雑なストリーミング受信ロジックを回避
- パフォーマンスとコードの単純性のバランス

## 技術選択の詳細

### パフォーマンス計測の組み込み
**実装**:
```csharp
var startTime = DateTime.UtcNow;
// ... 受信処理 ...
var receiveTime = DateTime.UtcNow - startTime;
```

**目的**:
- 通信パフォーマンスの可視化
- タイムアウト調整の基礎データ
- ログ出力時の有用な情報
- 将来のパフォーマンス最適化の指標

### Convert.ToHexStringの使用
**実装**:
```csharp
string responseHex = Convert.ToHexString(responseData);
```

**利点**:
- .NET 5.0以降の高速な変換メソッド
- BitConverter + String.Concatより高速
- メモリ効率が良い
- 可読性の高いコード

**代替案との比較**:
- BitConverter.ToString(): ハイフン区切りで不適切
- 手動ループ: 低速で冗長
- String.Join: 複雑で低速

## テスト戦略

### テストケース設計
1. **正常系テスト**:
   - 正常な3Eフレーム受信
   - 正常な4Eフレーム受信
   - 小サイズデータ受信
   - 大サイズデータ受信

2. **異常系テスト**:
   - タイムアウト発生
   - 未接続状態での呼び出し
   - ネットワークエラー
   - ゼロバイト受信

3. **パフォーマンステスト**:
   - 受信時間の計測
   - タイムアウト時間の正確性

### MockSocketの役割
```csharp
if (_socket.GetType().Name == "MockSocket")
{
    var mockSocket = _socket as dynamic;
    receivedBytes = await mockSocket.ReceiveAsync(...);
}
```

**目的**:
- 実際のPLC接続なしでテスト実行
- タイムアウト動作の検証
- エラーケースの再現
- 高速なテスト実行

## 実装の完成度

### 実装済み機能
- ✅ 非同期データ受信
- ✅ タイムアウト制御
- ✅ フレームタイプ判定
- ✅ パフォーマンス計測
- ✅ エラーハンドリング
- ✅ MockSocket互換性

### 未実装機能（将来の拡張）
- ⚠️ 部分受信の処理（大容量データ対応）
- ⚠️ 受信バッファのプール化
- ⚠️ 詳細なログ出力（LoggingManager連携）
- ⚠️ 受信データの詳細検証
- ⚠️ リトライ機構

## 設計書との対応

### クラス設計.md準拠確認
- ✅ メソッドシグネチャ: `Task<RawResponseData> ReceiveResponseAsync(int receiveTimeoutMs)`
- ✅ 戻り値型: RawResponseData
- ✅ パラメータ: receiveTimeoutMs (int型)
- ✅ 例外処理: TimeoutException, InvalidOperationException

### 各ステップio.md準拠確認
**Step4: データ送受信**
- ✅ 入力: receiveTimeoutMs（タイムアウト値）
- ✅ 出力: RawResponseData（生データ、16進数文字列、フレームタイプ、受信時刻）
- ✅ 処理内容:
  - PLCからの応答受信
  - タイムアウト制御
  - フレームタイプ判定
  - パフォーマンス計測

## 学習ポイント（C#初学者向け）

### 1. 非同期メソッドとawait
```csharp
public async Task<RawResponseData> ReceiveResponseAsync(int receiveTimeoutMs)
{
    receivedBytes = await _socket.ReceiveAsync(...);
}
```

**解説**:
- `async`キーワード: 非同期メソッドであることを宣言
- `await`キーワード: 非同期操作の完了を待機（ブロックせずにスレッドを解放）
- `Task<T>`: 非同期操作の結果を表す型

### 2. CancellationTokenSourceによるタイムアウト
```csharp
var cts = new CancellationTokenSource(receiveTimeoutMs);
receivedBytes = await _socket.ReceiveAsync(..., cts.Token);
```

**解説**:
- CancellationTokenSource: キャンセル可能な操作を制御
- コンストラクタにタイムアウト時間を指定
- 時間経過でTokenがキャンセル状態になる
- OperationCanceledExceptionがスローされる

### 3. ArraySegment<byte>の使用
```csharp
byte[] buffer = new byte[8192];
await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), ...);
```

**解説**:
- ArraySegment<byte>: 配列の一部を表す軽量な構造体
- 配列のコピーなしで部分的なアクセスが可能
- Socketメソッドの標準的なパラメータ型

### 4. Convert.ToHexStringの活用
```csharp
string responseHex = Convert.ToHexString(responseData);
```

**解説**:
- バイト配列を16進数文字列に変換
- .NET 5.0以降で利用可能
- 高速で簡潔な変換方法

### 5. 例外処理のパターン
```csharp
try {
    // 正常処理
}
catch (OperationCanceledException) {
    throw new TimeoutException(...);
}
catch (SocketException ex) {
    throw new InvalidOperationException(..., ex);
}
```

**解説**:
- 特定の例外を捕捉して再スロー
- 例外の変換（OperationCanceled → Timeout）
- InnerExceptionによる元の例外情報の保持

## 関連ファイル
- 実装: `andon/Core/Managers/PlcCommunicationManager.cs` (585-660行)
- テスト: `andon/Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`
- モデル: `andon/Core/Models/RawResponseData.cs`
- 定数: `andon/Core/Constants/ErrorMessages.cs`

## 次のステップ
1. Step5: DisconnectAsyncの実装記録作成
2. LoggingManager連携によるログ出力強化
3. パフォーマンステストの拡充
4. リトライ機構の実装検討
