# TC021_SendFrameAsync実装記録 - 既存実装分析

## 実装対象メソッド
- **クラス**: `PlcCommunicationManager`
- **メソッド**: `SendFrameAsync(string frameHexString)`
- **ファイル**: `andon/Core/Managers/PlcCommunicationManager.cs:273-327`
- **実装日**: 既存実装（2025年11月6日分析）

## 実装判断根拠の分析

### 1. メソッドシグネチャ設計判断

**選択された設計:**
```csharp
public async Task SendFrameAsync(string frameHexString)
```

**判断根拠:**
- **非同期設計**: PLCとの通信は時間がかかる処理のため、UIブロッキングを避けるためTask非同期パターンを採用
- **文字列パラメータ**: SLMP ASCII形式フレームはHEX文字列として扱うのが自然
- **戻り値なし**: 送信専用メソッドのため、void（Task）が適切

**他の検討案との比較:**
- ❌ `byte[]`パラメータ: HEXフレーム文字列からのバイト変換が呼び出し側の責任になる
- ❌ 同期メソッド: PLC通信は本質的に非同期であり、同期実装はブロッキングリスクが高い
- ❌ 戻り値あり: 送信成功/失敗は例外で表現する方が.NETの慣例に合致

### 2. 入力検証戦略

**実装内容:**
```csharp
if (string.IsNullOrEmpty(frameHexString))
{
    throw new ArgumentException(Constants.ErrorMessages.InvalidFrame, nameof(frameHexString));
}
```

**技術選択の根拠:**
- **Fail Fast原則**: 不正入力は可能な限り早期に検出して例外を投げる
- **ArgumentException使用**: .NET標準の引数例外型を使用
- **エラーメッセージ統一**: Constants.ErrorMessagesでメッセージを統一管理

**検討された他の方法:**
- ❌ 戻り値でエラー通知: C#では例外処理が標準的
- ❌ HEX形式妥当性チェック: 後段でバイト変換時にエラー検出されるため冗長
- ❌ ログ出力のみ: サイレント失敗は問題の特定を困難にする

### 3. 接続状態確認方式

**実装内容:**
```csharp
if (_connectionResponse?.Status != ConnectionStatus.Connected || _connectionResponse?.Socket == null)
{
    throw new InvalidOperationException(Constants.ErrorMessages.NotConnected);
}
```

**技術選択の根拠:**
- **二重チェック**: Status確認とSocket実体確認の両方を実施
- **Null安全**: null条件演算子（?.）で安全にアクセス
- **InvalidOperationException**: 状態不正時の標準例外型

**設計上の利点:**
- 接続していないのに送信を試行する論理エラーを防止
- デバッグ時の問題切り分けが容易
- PLC接続管理の責任分離（ConnectAsync → SendFrameAsync）

### 4. バイト変換方式の選択

**実装内容:**
```csharp
byte[] frameBytes = System.Text.Encoding.ASCII.GetBytes(frameHexString);
```

**技術選択の根拠:**
- **ASCII符号化**: SLMP ASCII形式フレームに適合
- **システム標準API**: .NET組み込みのEncoding.ASCIIを使用
- **単純変換**: HEX文字列 → ASCIIバイト配列の直接変換

**なぜ他の方式を選ばなかったか:**
- ❌ UTF-8: SLMPプロトコルはASCII前提
- ❌ 16進数パース: フレームは既にASCII文字として送信する必要がある
- ❌ カスタム変換: 標準APIで十分、保守性重視

### 5. Socket送信実装の工夫

**実装内容:**
```csharp
dynamic socket = _connectionResponse.Socket;
var segment = new ArraySegment<byte>(frameBytes);
int bytesSent = await socket.SendAsync(segment, System.Net.Sockets.SocketFlags.None);
```

**高度な技術選択:**
- **dynamic型使用**: MockSocketとの互換性確保
- **ArraySegment使用**: メモリ効率的なバイト配列操作
- **SocketFlags.None**: 標準的な送信フラグ

**dynamic型採用の背景:**
- 本番環境: `Socket.SendAsync`メソッド呼び出し
- テスト環境: `MockSocket.SendAsync`メソッド呼び出し
- コンパイル時バインディングでは型の違いによるエラーが発生
- 実行時バインディングにより両方の環境で動作

### 6. 送信結果検証の実装

**実装内容:**
```csharp
if (bytesSent != frameBytes.Length)
{
    var errorMsg = $"送信バイト数不一致: 期待={frameBytes.Length}, 実際={bytesSent}";
    throw new InvalidOperationException(errorMsg);
}
```

**実装理念:**
- **データ整合性確保**: 部分送信を確実に検出
- **詳細エラー情報**: 期待値vs実際値の明確な表示
- **堅牢性重視**: ネットワーク不安定時の不完全送信対策

### 7. エラーハンドリング戦略

**実装内容:**
```csharp
catch (System.Net.Sockets.SocketException ex)
{
    var errorMsg = $"{Constants.ErrorMessages.SendFailed}: {ex.Message}";
    throw new InvalidOperationException(errorMsg, ex);
}
catch (Exception ex) when (ex is not InvalidOperationException && ex is not ArgumentException)
{
    var errorMsg = $"{Constants.ErrorMessages.SendFailed}: {ex.Message}";
    throw new InvalidOperationException(errorMsg, ex);
}
```

**例外設計パターン:**
- **SocketException個別処理**: ネットワーク固有エラーの専用ハンドリング
- **既知例外の再スロー**: ArgumentException, InvalidOperationExceptionはそのまま通す
- **未知例外のラップ**: 予期しない例外をInvalidOperationExceptionでラップ
- **内部例外保持**: ex引数で元例外のスタックトレース保持

**技術的利点:**
- 呼び出し側で統一的な例外処理が可能
- デバッグ時の原因特定が容易
- ログ出力時の詳細情報保持

### 8. パフォーマンス測定の組み込み

**実装内容:**
```csharp
var startTime = DateTime.Now;
// ... 送信処理 ...
var elapsedTime = (DateTime.Now - startTime).TotalMilliseconds;
```

**実装目的:**
- 送信パフォーマンスの可視化
- ネットワーク遅延の測定
- 将来のログ出力・監視機能への準備

**技術選択:**
- `DateTime.Now`: 簡潔で理解しやすい
- `TotalMilliseconds`: ミリ秒精度で実用的
- ローカル変数: メモリ効率的

## トレードオフ分析

### 採用した設計の利点
✅ **型安全性**: dynamic使用だが、コンパイル時チェックとのバランス
✅ **テスタビリティ**: MockSocketとの完全互換性
✅ **保守性**: 標準的な.NET慣例に準拠
✅ **堅牢性**: 複数レイヤーでのエラー検出

### 採用した設計の欠点
⚠️ **dynamic型リスク**: 実行時エラーの可能性
⚠️ **パフォーマンス**: dynamic呼び出しのオーバーヘッド
⚠️ **型検証**: IntelliSenseが効かない

### 総合評価
**excellent（非常に良い）**
- SLMP通信要件を満たしている
- テスト可能性を重視した設計
- エラーハンドリングが堅牢
- .NET標準慣例に準拠

## 実装パターンの学習価値

### 1. dynamic型を活用したテスト互換性
MockとProduction環境で異なる型を統一的に扱う高度なテクニック

### 2. 多層防御によるエラー検証
- 入力検証 → 状態確認 → 送信結果確認 → 例外ハンドリング

### 3. .NET非同期プログラミングのベストプラクティス
Task/async/awaitパターンの適切な実装

### 4. 責任分離設計
接続管理（ConnectAsync）と送信処理（SendFrameAsync）の明確な分離

## 今後の拡張可能性

### ログ出力機能追加
```csharp
// [TODO: ログ出力] 送信開始
// LoggingManager: $"フレーム送信開始: {frameString}, 送信先: {endpoint}"
```

### リトライ機能追加
部分送信発生時の自動リトライメカニズム

### 送信統計収集
送信回数、成功率、平均レスポンス時間の統計

## 実装品質評価: A+

**理由:**
- 要件を完全に満たしている
- テスト駆動開発に対応した設計
- エラー処理が包括的
- 保守性・拡張性を考慮した実装
- .NET標準慣例への準拠

この実装は、PLC通信における信頼性とテスタビリティを両立した優れた設計例である。