# ConnectAsync UDP対応実装記録

## メソッド情報
- **メソッド名**: `ConnectAsync()`
- **クラス**: `PlcCommunicationManager`
- **ファイル**: `Core/Managers/PlcCommunicationManager.cs`
- **実装日**: 2025-11-06
- **実装者**: Claude Code (AI Assistant)

## 実装概要
既存のTCP接続処理に加えて、UDP接続処理と疎通確認機能を実装しました。
UDP接続はコネクションレス型通信のため、実際の通信可能性を確認するための疎通確認処理（模擬フレーム送受信）を含みます。

## 実装方法の選択理由

### 1. TCP/UDP分岐の実装

#### 検討した方法

**方法A: ConnectAsync()内でif分岐（採用）**
```csharp
public async Task<ConnectionResponse> ConnectAsync()
{
    if (_connectionConfig.UseTcp)
    {
        // TCP接続処理
    }
    else
    {
        // UDP接続処理（疎通確認含む）
    }
}
```

**メリット**:
- シンプルで理解しやすい
- 共通処理（タイムアウト設定、ConnectionResponse作成）の重複なし
- 既存のTCP実装への影響を最小化

**デメリット**:
- メソッドが長くなる（211行）
- TCP/UDP処理が1つのメソッドに混在

**方法B: ConnectTcpAsync() / ConnectUdpAsync()に分離**
```csharp
public async Task<ConnectionResponse> ConnectAsync()
{
    if (_connectionConfig.UseTcp)
        return await ConnectTcpAsync();
    else
        return await ConnectUdpAsync();
}
```

**メリット**:
- メソッドが短く、責任が明確
- TCP/UDP処理が分離され、個別にテスト可能

**デメリット**:
- 共通処理（タイムアウト設定、ConnectionResponse作成）の重複
- メソッド数が増加

**方法C: ストラテジーパターン（IConnectionStrategy）**
```csharp
interface IConnectionStrategy
{
    Task<ConnectionResponse> ConnectAsync();
}

class TcpConnectionStrategy : IConnectionStrategy { }
class UdpConnectionStrategy : IConnectionStrategy { }
```

**メリット**:
- 高い拡張性（新しいプロトコル追加が容易）
- SOLID原則に準拠

**デメリット**:
- 現時点では過剰設計
- クラス数が大幅に増加
- 実装の複雑度が上がる

#### 採用理由
**方法A（if分岐）を採用**

**根拠**:
1. **シンプルさ**: 現時点ではTCP/UDPの2つのみで、複雑なパターンは不要
2. **共通処理の一元管理**: タイムアウト設定、ConnectionResponse作成などの共通処理を重複なく記述できる
3. **保守性**: 1つのメソッドで完結するため、変更箇所が明確
4. **既存コードへの影響**: TC017（TCP接続テスト）への影響を最小化

**トレードオフの受け入れ**:
- メソッドの長さは許容範囲（211行）
- コメントとロジック分離により可読性を維持

### 2. UDP疎通確認の実装方法

#### 検討した方法

**方法A: SLMP READコマンド送受信（採用）**
- 模擬フレーム: "54001234000000010401006400000090E8030000"（M000-M999読み込み）
- 送信 → 応答受信（ConnectTimeoutMs以内）

**メリット**:
- 実際のPLC通信と同じ動作
- 通信可能性を正確に確認
- SLMP規格準拠

**デメリット**:
- 接続処理が重い（フレーム送受信の時間）
- PLCの状態に依存する可能性

**方法B: PINGコマンド送信**
- ICMPエコー要求/応答

**メリット**:
- 軽量で高速
- PLC機器の状態に依存しない

**デメリット**:
- SLMP通信の疎通確認にはならない
- PLC側でPINGが無効化されている可能性
- Socket APIがICMPをサポートしない場合がある

**方法C: 疎通確認なし（送信先設定のみ）**
- Socket.Connect()のみ実行

**メリット**:
- 最も高速
- シンプル

**デメリット**:
- 実際の通信可能性を確認できない
- エラー検出が遅れる（初回送信時に失敗）

#### 採用理由
**方法A（SLMP READコマンド送受信）を採用**

**根拠**:
1. **実際の通信確認**: SLMP通信が可能かを正確に確認
2. **早期エラー検出**: 接続時点で通信不可を検出
3. **SLMP規格準拠**: 三菱電機PLCの標準プロトコルに従う
4. **本番環境との整合性**: 実際のPLC通信と同じ動作

**トレードオフの受け入れ**:
- 接続処理の重さは許容範囲（ConnectTimeoutMs内）
- 疎通確認失敗時の明確なエラーメッセージ

### 3. 模擬フレームの選定

#### 検討したフレーム

**フレームA: M000-M999読み込み（採用）**
- フレーム: "54001234000000010401006400000090E8030000"
- デバイス: M機器（内部リレー）
- 読み込み点数: 1000点（ビット単位）

**メリット**:
- 最も基本的なデバイスアクセス
- 全てのPLCで対応
- 軽量（ビット単位読み込み）

**デメリット**:
- M機器が存在しない場合（理論上あり得ない）

**フレームB: D000-D999読み込み**
- フレーム: "54001234000000010400A800000090E8030000"
- デバイス: D機器（データレジスタ）
- 読み込み点数: 1000点（ワード単位）

**メリット**:
- データレジスタは必ず使用される
- ワード単位でより実用的

**デメリット**:
- M機器より重い（ワード単位）

#### 採用理由
**フレームA（M000-M999読み込み）を採用**

**根拠**:
1. **普遍性**: 全てのPLCで必ずM機器は存在
2. **軽量**: ビット単位読み込みで最小限のデータ転送
3. **基本動作確認**: 最も基本的なREADコマンドで疎通確認

**補足**:
- プロンプトでもM000-M999フレームが推奨されていた
- D000-D999も代替案として記載されていたが、M機器を優先

### 4. エラーハンドリングの実装

#### エラー種別と例外クラス

**TimeoutException: UDP疎通確認タイムアウト**
```csharp
throw new TimeoutException(
    $"UDP疎通確認タイムアウト: {_connectionConfig.IpAddress}:{_connectionConfig.Port}（タイムアウト時間: {_timeoutConfig.ConnectTimeoutMs}ms）");
```

**採用理由**:
- 標準の例外クラスを使用（保守性）
- タイムアウトであることが明確
- 詳細なエラーメッセージ（IP・ポート・タイムアウト時間）

**InvalidOperationException: UDP接続失敗、応答なし**
```csharp
throw new InvalidOperationException(
    $"UDP接続失敗: {_connectionConfig.IpAddress}:{_connectionConfig.Port} - {ex.Message}",
    ex);
```

**採用理由**:
- 操作が無効であることを表現（疎通確認失敗）
- 内部例外として元のSocketExceptionを保持（デバッグ容易）

#### 検討した他の方法

**SocketExceptionをそのまま使用**
```csharp
throw new SocketException($"UDP接続失敗: ...");
```

**不採用理由**:
- SocketExceptionのコンストラクタはint errorCodeを期待
- 文字列メッセージを直接渡せない（コンパイルエラー）

**カスタム例外クラス作成**
```csharp
public class UdpConnectionException : Exception { }
```

**不採用理由**:
- 現時点では過剰設計
- 標準例外クラスで十分

### 5. ConvertHexStringToBytes()実装

#### 実装方法

**実装コード**:
```csharp
private byte[] ConvertHexStringToBytes(string hexString)
{
    if (string.IsNullOrEmpty(hexString))
    {
        throw new ArgumentException("16進数文字列が空です。", nameof(hexString));
    }

    if (hexString.Length % 2 != 0)
    {
        throw new ArgumentException("16進数文字列の長さが偶数ではありません。", nameof(hexString));
    }

    byte[] bytes = new byte[hexString.Length / 2];
    for (int i = 0; i < bytes.Length; i++)
    {
        bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
    }
    return bytes;
}
```

**実装の特徴**:
1. **入力検証**:
   - null/空文字列チェック
   - 奇数長チェック（16進数は2文字で1バイト）

2. **変換処理**:
   - 2文字ずつSubstringで切り出し
   - Convert.ToByte()で16進数変換

3. **エラーハンドリング**:
   - ArgumentExceptionで入力エラーを通知
   - 詳細なエラーメッセージ

#### 検討した他の方法

**方法A: Span<T>使用（高性能版）**
```csharp
private byte[] ConvertHexStringToBytes(string hexString)
{
    Span<char> span = hexString.AsSpan();
    byte[] bytes = new byte[hexString.Length / 2];
    for (int i = 0; i < bytes.Length; i++)
    {
        bytes[i] = Convert.ToByte(span.Slice(i * 2, 2).ToString(), 16);
    }
    return bytes;
}
```

**不採用理由**:
- 現時点では性能要求がない
- Substring版で十分（可読性優先）

**方法B: BitConverter.ToString()の逆変換**
```csharp
// BitConverter.ToString()の出力: "54-00-12-34"
// 逆変換が必要
```

**不採用理由**:
- BitConverterは逆変換メソッドを提供していない
- 独自実装が必要

## 技術選択のトレードオフ

### TCP/UDP分岐実装
| 側面 | トレードオフ |
|------|------------|
| 可読性 | △ メソッドが長い vs ○ 処理が1箇所に集約 |
| 保守性 | ○ 変更箇所が明確 vs △ TCP/UDP処理が混在 |
| 拡張性 | △ 新プロトコル追加時は修正必要 vs ○ 現時点で十分 |
| パフォーマンス | ○ 影響なし |

**結論**: 現時点の要求に対して最適なバランス

### UDP疎通確認実装
| 側面 | トレードオフ |
|------|------------|
| 信頼性 | ○ 実際の通信可能性を確認 vs △ PLC状態に依存 |
| パフォーマンス | △ 接続処理が重い vs ○ 早期エラー検出 |
| SLMP準拠 | ○ 規格準拠 vs △ 実装の複雑度 |

**結論**: 信頼性とSLMP準拠を優先

### エラーハンドリング
| 側面 | トレードオフ |
|------|------------|
| 例外種別 | ○ 標準例外クラス使用 vs △ カスタム例外なし |
| エラーメッセージ | ○ 詳細な情報 vs △ 多言語対応未実装 |
| デバッグ容易性 | ○ 内部例外保持 vs ○ スタックトレース維持 |

**結論**: 標準例外クラスで十分、将来的にカスタム例外追加可能

## 実装の制約と前提条件

### 制約
1. **SLMP規格**: 三菱電機PLC向けSLMPプロトコルに準拠
2. **.NET 9.0**: Socket APIの非同期メソッド使用
3. **テスト環境**: MockSocketFactory使用による疎通確認シミュレーション

### 前提条件
1. **ConnectionConfig.UseTcp**: TCP/UDPの選択フラグが正しく設定されている
2. **TimeoutConfig**: ConnectTimeoutMsが適切な値（>0）
3. **PLC応答**: UDP疎通確認時にPLCが正常に応答する（本番環境）

### 今後の課題
1. **ログ出力**: LoggingManager連携（TODOコメント実装済み）
2. **多言語対応**: エラーメッセージの国際化
3. **疎通確認最適化**: タイムアウト時間の調整、フレーム選定の見直し

## パフォーマンス考慮事項

### 接続処理時間
- **TCP接続**: ConnectTimeoutMs以内（通常10-100ms）
- **UDP接続**: ConnectTimeoutMs以内（疎通確認含む、通常50-200ms）

### メモリ使用量
- **フレームバイト配列**: 40バイト（M000-M999読み込み）
- **受信バッファ**: 1024バイト
- **影響**: 極小（毎回接続時に確保・解放）

### 最適化の余地
1. **受信バッファサイズ**: 1024バイト → 実際の応答サイズ（通常200バイト程度）に縮小可能
2. **疎通確認フレーム**: M000-M999（1000点）→ M000-M099（100点）に縮小可能

## テスト観点

### 単体テスト（TC018）
- ✅ UDP接続成功
- ✅ Socket.Connected = false（UDP正常動作）
- ✅ Socket.ProtocolType = ProtocolType.Udp
- ✅ RemoteEndPoint設定確認
- ✅ 接続時間記録

### 回帰テスト（TC017）
- ✅ TCP接続成功（既存機能維持）
- ✅ TCP/UDP分岐による影響なし

### 今後追加すべきテスト
1. **TC019**: UDP疎通確認タイムアウト
2. **TC020**: UDP疎通確認失敗（応答なし）
3. **統合テスト**: 実際のPLC機器との疎通確認

## 参考資料
- SLMP規格書: 三菱電機 MELSEC通信プロトコル仕様
- .NET Socket API: Microsoft Docs
- UDP/IPプロトコル: RFC 768

## 更新履歴
| 日付 | 内容 | 担当者 |
|------|------|--------|
| 2025-11-06 | UDP接続処理実装、疎通確認追加 | Claude Code |
| 2025-11-06 | MockSocket TCP/UDP対応修正 | Claude Code |
| 2025-11-06 | ConvertHexStringToBytes実装 | Claude Code |
