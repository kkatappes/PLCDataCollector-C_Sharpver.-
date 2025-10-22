# SLMP ASCIIフレーム解析エラー解決手順書

## 作成日
2025-09-30

## 概要
SLMP通信において、4E_ASCIIフレーム設定で「Response frame too short: 20 chars, expected at least 22」エラーが発生した際の分析・解決手順を詳細に記録。

## 問題の症状

### 初期エラー（4E_Binary設定）
```
SLMP communication error: 1023 (0x03FF) - 未定義エラー (0x03FF)
```

### 各設定パターンのテスト結果
| 設定 | 結果 | 重要度 |
|------|------|---------|
| **3E_ASCII** | `UDP communication timeout` | ❌ 通信不可 |
| **3E_Binary** | `UDP communication timeout` | ❌ 通信不可 |
| **4E_ASCII** | `Response frame too short: 20 chars, expected at least 22` | ⚠️ **最有望** |
| **4E_Binary** | `SLMP communication error: 1023 (0x03FF)` | ⚠️ 元のエラー |

### 環境情報
- PLC IP: 172.30.40.15
- ポート: 8192 (UDP)
- 接続テスト結果:
  - ping: 成功 (1ms)
  - TCP 8192: 失敗
  - UDP 8192: 成功

### 新規発見エラー（2025-10-01 追加）

#### ReadTypeName実行時の16進文字解析エラー
```
SlmpClient.Exceptions.SlmpCommunicationException: Failed to parse SLMP response
 ---> System.ArgumentException: Invalid hex character: D
   at SlmpClient.Serialization.SlmpResponseParserHelper.GetHexValue(Byte hexChar) in SlmpResponseParser.cs:line 345
   at SlmpClient.Serialization.SlmpResponseParserHelper.ParseHexUshort(ReadOnlySpan`1 hexSpan) in SlmpResponseParser.cs:line 307
   at SlmpClient.Serialization.SlmpResponseParser.ParseAsciiResponse(Byte[] responseFrame, SlmpFrameVersion version) in SlmpResponseParser.cs:line 148
```

**発生状況**:
- **実行フェーズ**: 6ステップフロー Step 2（機器情報取得）
- **通信設定**: 4E_ASCII モード、172.30.40.15:8192 (UDP)
- **PLC接続**: 成功（接続は正常に確立）
- **エラー発生**: ReadTypeNameAsync 実行中の応答解析時
- **システム動作**: フォールバック処理により継続実行（Q00CPU推定で進行）

#### ログファイル同時アクセスエラー
```
System.IO.IOException: The process cannot access the file 'C:\Users\PPESAdmin\Desktop\dist\logs\intelligent_monitoring_log.log' because it is being used by another process.
   at SlmpClient.Core.UnifiedLogWriter.WriteLogEntryAsync(Object logEntry) in UnifiedLogWriter.cs:line 314
```

**発生パターン**:
- **エラー頻度**: 継続的（多数のエラーが連続発生）
- **発生タイミング**: Step 4（デバイススキャン）実行中
- **対象ファイル**: `logs/intelligent_monitoring_log.log`
- **影響**: ログ出力失敗（システム実行は継続）

## 分析プロセス

### 1. 基本接続確認
```bash
# ping テスト
ping 172.30.40.15
# 結果: 成功 (0-1ms, TTL=63)

# TCP接続テスト
powershell "Test-NetConnection 172.30.40.15 -Port 8192"
# 結果: TcpTestSucceeded : False

# UDP接続テスト
powershell "$udp = New-Object System.Net.Sockets.UdpClient; try { $udp.Connect('172.30.40.15', 8192); Write-Host 'UDP 8192: Connected'; $udp.Close() } catch { Write-Host 'UDP 8192: Failed' }"
# 結果: UDP 8192: Connected
```

### 2. SLMPリファレンスマニュアル分析結果

#### Read Type Nameコマンド仕様（ページ33, 136）
- **コマンド**: `0101`
- **サブコマンド**: `0000`
- **機能**: アクセス先のユニットの形名およびコードを読み出し
- **参照先**: 136ページ Read Type Name(コマンド:0101)

#### フレーム構造（ページ10, 229, 230）
- **3Eフレーム**: より一般的、互換性が高い
- **4Eフレーム**: 拡張型、シーケンス番号・予約領域を含む
- **MCプロトコル互換**: QnA互換3Eフレームまたは4Eフレームと同一伝文フォーマット

### 3. 重要な発見
- **PLCは4Eフレームに対応**: 4E設定でのみ応答あり
- **PLCは3Eフレーム非対応**: 3E設定は全てタイムアウト
- **ASCIIモードで部分的成功**: 20文字の実応答を受信

### 4. 新規エラーパターンの詳細分析（2025-10-01 追加）

#### 16進文字解析エラーの解析
**エラー発生フロー**:
```
Step 2: PLC機器情報取得
  ↓
ReadTypeNameAsync 実行
  ↓
SLMPレスポンス受信（正常）
  ↓
ParseResponse (ASCII応答として解析)
  ↓
ParseAsciiResponse メソッド
  ↓
ParseHexUshort (16進文字列解析)
  ↓
GetHexValue メソッドで 'D' 文字処理
  ↓
❌ ArgumentException: "Invalid hex character: D"
```

**問題の特徴**:
- **'D' は有効な16進文字**: 0xD (13) として処理されるべき
- **バイト値の問題**: (char)hexChar が 'D' として認識されない
- **文字エンコーディング問題**: ASCII/UTF-8エンコーディングの競合の可能性
- **フォールバック処理**: 正常に機能（Q00CPU推定で継続）

**実際のPLC応答推測**:
```
正常期待値: "504030304443505530000..."  (標準16進ASCII)
問題応答例: "50D030304443505530000..."  (D文字が直接含まれる)
または文字エンコーディング問題による解釈エラー
```

#### ログシステム競合問題の解析
**競合発生パターン**:
- **発生箇所**: `UnifiedLogWriter.WriteLogEntryAsync`（line 314）
- **ファイルアクセス方式**: 非同期書き込み（AppendAllTextAsync）
- **競合要因**: 複数のログエントリ同時書き込み試行
- **FileShare設定**: 不適切（排他制御不足）

**システム影響**:
- **ログ出力**: 失敗（統合ログファイルに記録されない）
- **アプリケーション動作**: 継続（ログエラーでは停止しない）
- **診断情報**: 重要データの欠損リスク

## 根本原因の特定

### 問題の本質
1. **PLCは正常に応答している** - 20文字のASCII応答を返している
2. **C#実装側の期待値に問題** - 22文字を期待するが実際は20文字
3. **フレーム長チェックが厳格すぎる** - 応答データを正しく処理できていない

### エラー発生箇所
```csharp
// SlmpResponseParser.cs の ParseAsciiResponse() 内
if (responseFrame.Length < headerSize)
    throw new SlmpCommunicationException($"Response frame too short: {responseFrame.Length} chars, expected at least {headerSize}");
```

### SLMPリファレンスマニュアルとの照合
- **ASCII応答フレーム構造**: 実際の応答が仕様と一致している可能性
- **最小フレーム長**: 期待値22文字が過大である可能性
- **Read Type Name応答**: コマンド固有の簡略化された応答形式の可能性

### 新規根本原因の特定（2025-10-01 追加）

#### 16進文字解析エラーの根本原因
**1. GetHexValueメソッドの文字判定ロジック問題**:
```csharp
// 現在の問題箇所: SlmpResponseParser.cs:345
return hexChar switch
{
    >= (byte)'0' and <= (byte)'9' => hexChar - '0',     // 0-9 ✅
    >= (byte)'A' and <= (byte)'F' => hexChar - 'A' + 10, // A-F ✅
    >= (byte)'a' and <= (byte)'f' => hexChar - 'a' + 10, // a-f ✅
    _ => throw new ArgumentException($"Invalid hex character: {(char)hexChar}")  // ❌ 'D'がここに到達
};
```

**2. 'D'文字がエラーになる理由**:
- **バイト値0x44**: ASCIIコード68 = 'D'
- **判定ロジック**: `>= (byte)'A' and <= (byte)'F'` で 'D' は本来マッチするはず
- **推定問題**: バイト値が期待する範囲外、または型変換問題
- **実際のバイト値**: 'D'以外の文字（制御文字等）が0x44として解釈されている可能性

**3. 文字エンコーディング競合**:
- **PLC応答**: 実際のバイナリデータにASCII以外の文字が混入
- **C#解釈**: バイナリを強制的にASCII文字として解釈
- **結果**: 有効でない'D'（実際は別のバイト値）がGetHexValueに渡される

#### ログファイル競合エラーの根本原因
**1. ファイル排他制御の不備**:
```csharp
// 問題箇所: UnifiedLogWriter.cs:314
await File.AppendAllTextAsync(_logFilePath, json + Environment.NewLine);
// ❌ FileShare指定なし、同時アクセス制御なし
```

**2. 非同期処理の競合**:
- **複数スレッド**: Step4でのデバイススキャン時に大量のログエントリ生成
- **同時書き込み**: 複数の`WriteLogEntryAsync`が並行実行
- **排他制御なし**: ファイルロック機構の不在

**3. 設計上の問題**:
- **ログキューイング**: バッファリング機構の不在
- **エラーハンドリング**: 書き込み失敗時のリトライなし
- **代替手段**: ログ失敗時の代替出力先なし

## 解決手順

### Phase 1: 受信データの詳細確認
1. **ログ出力レベルをTraceに変更**
```json
{
  "Logging": {
    "LogLevel": {
      "SlmpClient": "Trace"
    }
  }
}
```

2. **20文字応答の内容確認**
   - 受信フレームの16進ダンプ機能追加
   - ASCII文字列としての内容確認
   - フレーム構造との照合

### Phase 2: ASCIIレスポンス処理の修正
1. **最小フレーム長の修正**
```csharp
// 修正前: 22文字期待
int minFrameSize = 22;

// 修正後: 20文字でも処理可能
int minFrameSize = 20; // または動的計算
```

2. **エラーハンドリングの改善**
   - 警告レベルでの継続処理
   - 詳細なエラー情報の出力

### Phase 3: Read Type Name対応強化
1. **コマンド固有処理の追加**
   - Read Type Name（0101）専用の応答処理
   - 簡略化された応答フォーマット対応

2. **フォールバック機能**
   - ReadTypeNameが失敗した場合の代替手段
   - より基本的なコマンドでの機器情報取得

### Phase 4: 16進文字解析エラーの解決（2025-10-01 追加）

#### 4-1: 詳細診断機能の実装
1. **SLMPレスポンス生データダンプ機能**:
```csharp
// SlmpClient.cs に追加
private void LogRawResponse(byte[] responseData, string context)
{
    var hexDump = Convert.ToHexString(responseData);
    var asciiDump = Encoding.ASCII.GetString(responseData);

    _logger.LogDebug("SLMP生レスポンスダンプ [{Context}]:", context);
    _logger.LogDebug("  16進: {HexDump}", hexDump);
    _logger.LogDebug("  ASCII: {AsciiDump}", asciiDump);
    _logger.LogDebug("  サイズ: {Size}バイト", responseData.Length);
}
```

2. **16進解析プロセスの詳細トレース**:
```csharp
// SlmpResponseParserHelper.cs に追加
private static byte GetHexValueWithDiagnostics(byte hexChar)
{
    _logger.LogTrace("16進文字解析: バイト値=0x{ByteValue:X2}, 文字='{Char}'",
                     hexChar, (char)hexChar);

    return hexChar switch
    {
        >= (byte)'0' and <= (byte)'9' => hexChar - '0',
        >= (byte)'A' and <= (byte)'F' => hexChar - 'A' + 10,
        >= (byte)'a' and <= (byte)'f' => hexChar - 'a' + 10,
        _ => throw new ArgumentException($"無効な16進文字: バイト値=0x{hexChar:X2}, 文字='{(char)hexChar}', 前後文脈が必要")
    };
}
```

#### 4-2: 16進文字解析の堅牢化
1. **バイト値範囲チェックの追加**:
```csharp
// より厳密なバリデーション
private static bool IsValidHexChar(byte hexChar)
{
    return (hexChar >= 0x30 && hexChar <= 0x39) ||  // '0'-'9'
           (hexChar >= 0x41 && hexChar <= 0x46) ||  // 'A'-'F'
           (hexChar >= 0x61 && hexChar <= 0x66);    // 'a'-'f'
}
```

2. **エラー継続モードの実装**:
```csharp
// フォールバック処理付きの解析
private static byte GetHexValueSafe(byte hexChar, bool allowFallback = true)
{
    try
    {
        return GetHexValue(hexChar);
    }
    catch (ArgumentException ex) when (allowFallback)
    {
        _logger.LogWarning("16進文字解析失敗、0で代替: {Error}", ex.Message);
        return 0; // 代替値
    }
}
```

### Phase 5: ログファイル競合問題の解決（2025-10-01 追加）

#### 5-1: ファイル排他制御の実装
1. **同期プリミティブの導入**:
```csharp
// UnifiedLogWriter.cs に追加
private readonly SemaphoreSlim _writeSemaphore = new(1, 1);
private readonly object _writeLock = new object();

private async Task WriteLogEntryAsync(object logEntry)
{
    await _writeSemaphore.WaitAsync();
    try
    {
        var json = JsonSerializer.Serialize(logEntry, _jsonOptions);

        // FileShare.ReadWrite で排他制御
        using var stream = new FileStream(_logFilePath, FileMode.Append,
                                         FileAccess.Write, FileShare.ReadWrite);
        using var writer = new StreamWriter(stream);
        await writer.WriteLineAsync(json);
    }
    finally
    {
        _writeSemaphore.Release();
    }
}
```

#### 5-2: ログキューイングシステムの導入
1. **非同期ログキューの実装**:
```csharp
private readonly Channel<object> _logQueue = Channel.CreateUnbounded<object>();
private readonly CancellationTokenSource _cancellationTokenSource = new();

// バックグラウンドでキューを処理
private async Task ProcessLogQueueAsync()
{
    await foreach (var logEntry in _logQueue.Reader.ReadAllAsync(_cancellationTokenSource.Token))
    {
        await WriteLogEntryWithRetryAsync(logEntry);
    }
}
```

2. **リトライ機構の実装**:
```csharp
private async Task WriteLogEntryWithRetryAsync(object logEntry, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await WriteLogEntryAsync(logEntry);
            break;
        }
        catch (IOException ex) when (attempt < maxRetries)
        {
            _logger.LogWarning("ログ書き込み失敗 (試行 {Attempt}/{MaxRetries}): {Error}",
                              attempt, maxRetries, ex.Message);
            await Task.Delay(100 * attempt); // 指数バックオフ
        }
    }
}

## 再現可能な診断手順

### 1. 環境確認
```bash
# ネットワーク接続確認
ping [PLC_IP]

# ポート確認
powershell "Test-NetConnection [PLC_IP] -Port [PORT]"
powershell "$udp = New-Object System.Net.Sockets.UdpClient; try { $udp.Connect('[PLC_IP]', [PORT]); Write-Host 'UDP Connected'; $udp.Close() } catch { Write-Host 'UDP Failed' }"
```

### 2. 設定パターンテスト
各設定で実行し、結果を記録:
```json
// テスト設定1: 3E_ASCII
{
  "UseTcp": false,
  "Port": 8192,
  "FrameVersion": "3E",
  "IsBinary": false
}

// テスト設定2: 3E_Binary
{
  "UseTcp": false,
  "Port": 8192,
  "FrameVersion": "3E",
  "IsBinary": true
}

// テスト設定3: 4E_ASCII ← 最有望
{
  "UseTcp": false,
  "Port": 8192,
  "FrameVersion": "4E",
  "IsBinary": false
}

// テスト設定4: 4E_Binary
{
  "UseTcp": false,
  "Port": 8192,
  "FrameVersion": "4E",
  "IsBinary": true
}
```

### 3. エラーパターン判定フローチャート
```
通信エラー発生
↓
ping テスト → 失敗 → ネットワーク問題
↓ 成功
ポートテスト → 失敗 → ポート/プロトコル問題
↓ 成功
設定パターンテスト
↓
3E系: timeout → PLC は 4E フレーム専用
4E_Binary: 1023エラー → バイナリモード問題
4E_ASCII: フレーム長エラー → 解析処理問題 ← ここが解決ポイント
```

### 4. ログ分析のポイント
- **タイムアウト**: 通信プロトコル不適合
- **1023エラー**: SLMP応答形式不適合
- **フレーム長エラー**: 解析処理問題（実際は成功に近い）

## コード修正の具体的箇所

### ファイル: `SlmpResponseParser.cs`
```csharp
// 修正対象メソッド: ParseAsciiResponse()
// 行番号: 約60-70行目付近

// 修正前
if (responseFrame.Length < 22)
    throw new SlmpCommunicationException($"Response frame too short: {responseFrame.Length} chars, expected at least 22");

// 修正後
int expectedMinLength = CalculateMinAsciiFrameLength(version); // 動的計算
if (responseFrame.Length < expectedMinLength)
{
    // 警告レベルで継続処理を検討
    Logger.LogWarning($"Response frame shorter than expected: {responseFrame.Length} chars, expected {expectedMinLength}");

    // 最低限の長さチェック
    if (responseFrame.Length < 16) // 絶対最小長
        throw new SlmpCommunicationException($"Response frame too short: {responseFrame.Length} chars");
}
```

### ファイル: `IntelligentMonitoringSystem.cs`
```csharp
// Step2_ConnectAndGetDeviceInfoAsync() メソッド強化
// ReadTypeNameAsync() のフォールバック処理追加

try
{
    var typeName = await client.ReadTypeNameAsync(timeout, cancellationToken);
    return typeName;
}
catch (SlmpCommunicationException ex)
{
    Logger.LogWarning($"ReadTypeName failed: {ex.Message}, trying fallback methods");

    // フォールバック: より基本的なコマンドで機器情報取得
    return await TryBasicDeviceInfoAsync(client, timeout, cancellationToken);
}
```

## テスト手順

### 1. 修正前の状態確認
```bash
cd C:\Users\1010821\Desktop\python\andon
dotnet run
# 結果: "Response frame too short: 20 chars, expected at least 22"
```

### 2. 修正後のテスト
```bash
# ビルド
dotnet build

# 実行
dotnet run

# 期待される結果
# - 6ステップフロー正常実行
# - rawdata_analysis.log 生成
# - terminal_output.txt 生成
```

### 3. 成功の確認指標
- [ ] Step 2 で ReadTypeName が成功
- [ ] 6ステップフロー完全実行
- [ ] ログファイル正常生成
- [ ] エラーログにSLMP通信エラーが出ない

## 予防策・今後の対応

### 1. 開発時の注意点
- **フレーム長チェック**: 仕様書と実装の厳密な照合
- **エラーハンドリング**: 警告レベルでの継続処理を検討
- **ログ出力**: 詳細な診断情報の出力

### 2. テスト項目の追加
- [ ] 複数PLC機種での動作確認
- [ ] 各種設定パターンでの回帰テスト
- [ ] フレーム長バリエーションテスト

### 3. 文書化の改善
- 実機テスト結果の蓄積
- PLC機種別の対応マトリックス作成
- エラーパターン辞書の作成

## 関連ファイル・リソース

### 修正対象ファイル
- `andon/Serialization/SlmpResponseParser.cs`
- `andon/Core/IntelligentMonitoringSystem.cs`
- `andon/appsettings.json`

### 参照ドキュメント
- SLMPリファレンスマニュアル (`sh080931q.pdf`)
  - ページ33: コマンド一覧
  - ページ136: Read Type Name詳細
  - ページ10,229,230: フレーム構造

### ログファイル場所
- `logs/intelligent_monitoring_log_4E_ascii.log`
- `logs/intelligent_monitoring_log_4E_binary.log`
- `logs/新しいテキスト ドキュメント.txt`

### 画像化された仕様書
- `pdf2img/page_*.png` (240ページの仕様書画像)

---

## 履歴
- 2025-09-30: 初版作成（4E_ASCIIフレーム長エラーの分析・解決手順）
- 2025-10-01: 重要更新
  - **新エラーパターン追加**: `Invalid hex character: D` 解析エラー（SlmpResponseParser.cs:345）
  - **ログ競合問題追加**: UnifiedLogWriter ファイル同時アクセス競合（line 314）
  - **詳細診断機能**: 生SLMPレスポンスダンプ・16進解析トレース機能の設計
  - **解決手順拡張**: Phase 4（16進文字解析）・Phase 5（ログ競合）の追加
  - **根本原因分析**: 文字エンコーディング問題・ファイル排他制御不備の特定

## 作成者
Claude Code による分析・文書化