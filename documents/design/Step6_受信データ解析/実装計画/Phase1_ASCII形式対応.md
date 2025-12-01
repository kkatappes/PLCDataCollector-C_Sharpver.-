# Phase 1: ASCII形式対応 実装仕様書

作成日: 2025-01-17
優先度: 🔴 最優先
対象プロジェクト: andon (C#)
参照元: 受信データ解析_実装方針決定.md

---

## 1. Phase 1 概要

### 1.1 目標

PySLMPClient方式の完全実装により、3E/4E × Binary/ASCII 全組み合わせのフレーム形式に対応する。

### 1.2 実装範囲

- **1-1**: フレーム自動判定機能
- **1-2**: データオフセット決定（標準仕様準拠）
- **1-3**: ASCII形式パーサー実装

### 1.3 予想工数

**合計**: 8-11時間
- フレーム自動判定: 2-3時間
- ASCII形式パーサー（3E）: 3-4時間
- ASCII形式パーサー（4E）: 3-4時間

---

## 2. 実装詳細

### 2.1 フレーム自動判定機能

#### 2.1.1 目的

応答フレームの先頭バイトを判定し、3E/4E × Binary/ASCII の4パターンを自動識別する。

#### 2.1.2 実装仕様

**判定ロジック**:

```csharp
/// <summary>
/// 応答フレームタイプの自動判定（Binary/ASCII、3E/4E）
/// PySLMPClient方式を採用
/// </summary>
/// <param name="rawData">受信したバイト配列</param>
/// <returns>検出されたフレームタイプ</returns>
/// <exception cref="ArgumentException">データ長が不足している場合</exception>
/// <exception cref="FormatException">不明なフレーム形式の場合</exception>
private FrameType DetectResponseFrameType(byte[] rawData)
{
    // 最小データ長チェック
    if (rawData == null || rawData.Length < 2)
    {
        throw new ArgumentException(
            $"Data too short for frame detection. Length: {rawData?.Length ?? 0}");
    }

    // ASCII判定（先頭が 'D' = 0x44）
    if (rawData[0] == 0x44) // 'D'
    {
        _logger.LogDebug("ASCII frame detected");

        return rawData[1] switch
        {
            0x30 => FrameType.Frame3E_ASCII,  // "D0"
            0x34 => FrameType.Frame4E_ASCII,  // "D4"
            _ => throw new FormatException(
                $"Invalid ASCII response subheader: D{(char)rawData[1]}")
        };
    }

    // Binary判定（サブヘッダ 0xD0 0x00 / 0xD4 0x00）
    _logger.LogDebug("Binary frame detected");

    return (rawData[0], rawData[1]) switch
    {
        (0xD0, 0x00) => FrameType.Frame3E_Binary,
        (0xD4, 0x00) => FrameType.Frame4E_Binary,
        _ => throw new FormatException(
            $"Unknown response subheader: 0x{rawData[0]:X2} 0x{rawData[1]:X2}")
    };
}
```

#### 2.1.3 FrameType列挙型の拡張

既存の列挙型にASCII形式を追加：

```csharp
/// <summary>
/// SLMPフレームタイプ（Binary/ASCII対応）
/// </summary>
public enum FrameType
{
    /// <summary>3Eフレーム Binary形式</summary>
    Frame3E_Binary = 0,

    /// <summary>4Eフレーム Binary形式</summary>
    Frame4E_Binary = 1,

    /// <summary>3Eフレーム ASCII形式</summary>
    Frame3E_ASCII = 2,

    /// <summary>4Eフレーム ASCII形式</summary>
    Frame4E_ASCII = 3
}
```

#### 2.1.4 実装要件チェックリスト

- 🔲 DetectResponseFrameType()メソッド実装
- 🔲 FrameType列挙型にASCII追加
- 🔲 引数検証（null、長さ不足）
- 🔲 ログ出力（検出成功・失敗）
- 🔲 単体テストケース作成
  - 3E Binary: 0xD0 0x00
  - 4E Binary: 0xD4 0x00
  - 3E ASCII: "D0"
  - 4E ASCII: "D4"
  - 不正データ: 例外スロー確認

---

### 2.2 データオフセット決定（標準仕様準拠）

#### 2.2.1 目的

フレームタイプに応じて、デバイスデータの開始位置を正確に決定する。

#### 2.2.2 実装仕様

**オフセット計算ロジック**:

```csharp
/// <summary>
/// フレームタイプに応じたデバイスデータ開始位置を取得
/// SLMP標準仕様に準拠
/// </summary>
/// <param name="frameType">フレームタイプ</param>
/// <returns>デバイスデータ開始位置（バイト単位またはASCII文字位置）</returns>
/// <exception cref="NotSupportedException">未対応のフレームタイプ</exception>
private int GetDeviceDataOffset(FrameType frameType) => frameType switch
{
    // Binary形式（バイト単位）
    FrameType.Frame3E_Binary => 11,   // 標準仕様
    FrameType.Frame4E_Binary => 15,   // 標準仕様（実機確認済み）

    // ASCII形式（文字位置）
    FrameType.Frame3E_ASCII => 20,    // 標準仕様（20文字目～）
    FrameType.Frame4E_ASCII => 30,    // 標準仕様（30文字目～）

    // 未対応
    _ => throw new NotSupportedException(
        $"Unsupported frame type for offset calculation: {frameType}")
};
```

#### 2.2.3 オフセット根拠

| フレーム形式 | オフセット | 根拠 |
|------------|-----------|------|
| **3E Binary** | 11バイト目 | サブヘッダ(2) + ネットワーク情報(7) + 終了コード(2) = 11 |
| **4E Binary** | 15バイト目 | サブヘッダ(2) + シリアル(2) + 予約(2) + ネットワーク情報(7) + 終了コード(2) = 15 |
| **3E ASCII** | 20文字目 | サブヘッダ(2) + ネットワーク情報(14) + 終了コード(4) = 20 |
| **4E ASCII** | 30文字目 | サブヘッダ(2) + 予約1(2) + シリアル(4) + 予約2(4) + ネットワーク情報(14) + 終了コード(4) = 30 |

**重要**:
- 実機データでは4E Binary応答の15バイト目から正しくデバイスデータが開始していることを確認済み
- 4E ASCII形式はPySLMPClientの実装に準拠（予約1→シーケンス→予約2の順）

#### 2.2.4 ConMoniの13バイトオフセットについて（非採用）

**⚠️ 重要**: ConMoniのコードでは `resRaw[13+2*x:15+2*x]` と13バイト目から読み取っているが、これは以下の理由により**andonでは採用しない**:

1. **実機データ検証結果**: 4E応答フレームの標準オフセットは15バイト目であることを確認済み（memo.md参照）
   ```
   Idx 13-14: 00 00  ← 終了コード（データ領域ではない）
   Idx 15~:   FF FF  ← デバイスデータ開始位置（標準仕様）
   ```

2. **標準仕様との不一致**: SLMP仕様書では4E Binaryのデバイスデータ開始位置は15バイト目

3. **終了コード位置**: 13-14バイト目は終了コード領域であり、データ領域ではない

**採用方針**: 標準仕様準拠の15バイト目オフセットを使用

#### 2.2.5 実装要件チェックリスト

- ✅ Binary形式は既に実装済み（3E: 11, 4E: 15）
- 🔲 ASCII形式オフセットの追加
- 🔲 境界チェック強化（データ長 >= オフセット）
- 🔲 単体テストケース作成
  - 各フレームタイプで正しいオフセット返却
  - 未対応フレームタイプで例外スロー

---

### 2.3 ASCII形式パーサー実装

#### 2.3.1 目的

3E/4E ASCII応答フレームを解析し、終了コードとデバイスデータを抽出する。

#### 2.3.2 3E ASCII形式パーサー

**フレーム構造**:
```
D0              ← サブヘッダ（2文字）
00              ← ネットワーク番号（2文字）
FF              ← PC番号（2文字）
03FF            ← I/O番号（4文字）
00              ← 局番（2文字）
0010            ← データ長（4文字）
0000            ← 終了コード（4文字）
FFFF0000...     ← デバイスデータ（可変長）
```

**実装コード**:

```csharp
/// <summary>
/// 3E ASCII応答フレームの解析
/// </summary>
/// <param name="rawData">受信したバイト配列（ASCII文字列）</param>
/// <returns>終了コードとデバイスデータのタプル</returns>
/// <exception cref="FormatException">フレーム形式エラー</exception>
private (ushort EndCode, byte[] DeviceData) Parse3EFrameStructureAscii(byte[] rawData)
{
    try
    {
        // ASCII文字列に変換
        string response = Encoding.ASCII.GetString(rawData);

        _logger.LogDebug($"Parsing 3E ASCII frame: {response.Substring(0, Math.Min(50, response.Length))}...");

        // "D0" で開始確認
        if (!response.StartsWith("D0"))
        {
            throw new FormatException($"Invalid 3E ASCII response start: {response.Substring(0, 2)}");
        }

        // 最小フレーム長チェック（20文字以上）
        if (response.Length < 20)
        {
            throw new FormatException($"3E ASCII frame too short: {response.Length} characters");
        }

        // ヘッダ解析（2文字目～19文字目）
        // D0 + 00 + FF + 03FF + 00 + 0010 + 0000 = 20文字
        string hexNetworkNum = response.Substring(2, 2);
        string hexPcNum = response.Substring(4, 2);
        string hexIoNum = response.Substring(6, 4);
        string hexDropNum = response.Substring(10, 2);
        string hexDataLength = response.Substring(12, 4);
        string hexEndCode = response.Substring(16, 4);

        // 終了コード抽出
        ushort endCode = Convert.ToUInt16(hexEndCode, 16);

        _logger.LogDebug($"3E ASCII - EndCode: 0x{endCode:X4}, DataLength: {hexDataLength}");

        // データ部抽出（20文字目以降）
        string dataHex = response.Substring(20);

        // HEX文字列 → バイト配列変換（4文字 = 1ワード = 2バイト）
        int wordCount = dataHex.Length / 4;
        byte[] deviceData = new byte[wordCount * 2];

        for (int i = 0; i < wordCount; i++)
        {
            if (i * 4 + 4 > dataHex.Length)
                break;

            string wordHex = dataHex.Substring(i * 4, 4);

            // ASCIIはビッグエンディアン表記（上位2桁、下位2桁）
            // バイト配列はリトルエンディアンで格納
            deviceData[i * 2] = Convert.ToByte(wordHex.Substring(2, 2), 16);     // 下位バイト
            deviceData[i * 2 + 1] = Convert.ToByte(wordHex.Substring(0, 2), 16); // 上位バイト
        }

        _logger.LogDebug($"3E ASCII - Extracted {wordCount} words ({deviceData.Length} bytes)");

        return (endCode, deviceData);
    }
    catch (Exception ex) when (ex is not FormatException)
    {
        _logger.LogError(ex, "Failed to parse 3E ASCII frame");
        throw new FormatException($"3E ASCII frame parse error: {ex.Message}", ex);
    }
}
```

#### 2.3.3 4E ASCII形式パーサー

**フレーム構造**（PySLMPClient方式に準拠）:
```
D4              ← サブヘッダ（2文字）
00              ← 予約1（2文字）
0001            ← シーケンス番号（4文字）  ← PySLMPClientの実装順序
0000            ← 予約2（4文字）
00              ← ネットワーク番号（2文字）
FF              ← PC番号（2文字）
03FF            ← I/O番号（4文字）
00              ← 局番（2文字）
0010            ← データ長（4文字）
0000            ← 終了コード（4文字）
FFFF0000...     ← デバイスデータ（可変長）
```

**注意**: 一部の実装では予約フィールドの順序が異なる場合があるが、本実装ではPySLMPClientの実績ある実装順序を採用する。

**実装コード**:

```csharp
/// <summary>
/// 4E ASCII応答フレームの解析
/// PySLMPClient方式に準拠（予約1-シーケンス-予約2の順）
/// </summary>
/// <param name="rawData">受信したバイト配列（ASCII文字列）</param>
/// <returns>終了コードとデバイスデータのタプル</returns>
/// <exception cref="FormatException">フレーム形式エラー</exception>
private (ushort EndCode, byte[] DeviceData) Parse4EFrameStructureAscii(byte[] rawData)
{
    try
    {
        // ASCII文字列に変換
        string response = Encoding.ASCII.GetString(rawData);

        _logger.LogDebug($"Parsing 4E ASCII frame: {response.Substring(0, Math.Min(50, response.Length))}...");

        // "D4" で開始確認
        if (!response.StartsWith("D4"))
        {
            throw new FormatException($"Invalid 4E ASCII response start: {response.Substring(0, 2)}");
        }

        // 最小フレーム長チェック（30文字以上）
        if (response.Length < 30)
        {
            throw new FormatException($"4E ASCII frame too short: {response.Length} characters");
        }

        // ヘッダ解析（2文字目～29文字目）
        // D4 + 00(予約1) + 0001(シーケンス) + 0000(予約2) + 00 + FF + 03FF + 00 + 0010 + 0000 = 30文字
        string hexReserved1 = response.Substring(2, 2);   // 予約1（位置2-3）
        string hexSerial = response.Substring(4, 4);      // シーケンス番号（位置4-7）
        string hexReserved2 = response.Substring(8, 4);   // 予約2（位置8-11）
        string hexNetworkNum = response.Substring(12, 2);
        string hexPcNum = response.Substring(14, 2);
        string hexIoNum = response.Substring(16, 4);
        string hexDropNum = response.Substring(20, 2);
        string hexDataLength = response.Substring(22, 4);
        string hexEndCode = response.Substring(26, 4);

        // シーケンス番号と終了コード抽出
        ushort sequenceNumber = Convert.ToUInt16(hexSerial, 16);
        ushort endCode = Convert.ToUInt16(hexEndCode, 16);

        _logger.LogDebug($"4E ASCII - Serial: {sequenceNumber}, EndCode: 0x{endCode:X4}, DataLength: {hexDataLength}");

        // データ部抽出（30文字目以降）
        string dataHex = response.Substring(30);

        // HEX文字列 → バイト配列変換（4文字 = 1ワード = 2バイト）
        int wordCount = dataHex.Length / 4;
        byte[] deviceData = new byte[wordCount * 2];

        for (int i = 0; i < wordCount; i++)
        {
            if (i * 4 + 4 > dataHex.Length)
                break;

            string wordHex = dataHex.Substring(i * 4, 4);

            // ASCIIはビッグエンディアン表記
            // バイト配列はリトルエンディアンで格納
            deviceData[i * 2] = Convert.ToByte(wordHex.Substring(2, 2), 16);     // 下位バイト
            deviceData[i * 2 + 1] = Convert.ToByte(wordHex.Substring(0, 2), 16); // 上位バイト
        }

        _logger.LogDebug($"4E ASCII - Extracted {wordCount} words ({deviceData.Length} bytes)");

        return (endCode, deviceData);
    }
    catch (Exception ex) when (ex is not FormatException)
    {
        _logger.LogError(ex, "Failed to parse 4E ASCII frame");
        throw new FormatException($"4E ASCII frame parse error: {ex.Message}", ex);
    }
}
```

#### 2.3.4 既存パーサーとの統合

ProcessReceivedRawData()メソッドでの動的分岐:

```csharp
// Step-4 フレーム構造解析（動的分岐）
(ushort endCode, byte[] deviceData) frameData;

switch (detectedFrameType)
{
    case FrameType.Frame3E_Binary:
        frameData = Parse3EFrameStructure(rawData);
        break;

    case FrameType.Frame4E_Binary:
        frameData = Parse4EFrameStructure(rawData);
        break;

    case FrameType.Frame3E_ASCII:
        frameData = Parse3EFrameStructureAscii(rawData);
        break;

    case FrameType.Frame4E_ASCII:
        frameData = Parse4EFrameStructureAscii(rawData);
        break;

    default:
        throw new NotSupportedException($"Frame type {detectedFrameType} is not supported");
}
```

#### 2.3.5 実装要件チェックリスト

- 🔲 Parse3EFrameStructureAscii()メソッド実装
- 🔲 Parse4EFrameStructureAscii()メソッド実装
- 🔲 HEX文字列→バイト配列変換の正確性確認
- 🔲 エンディアン変換の正確性確認（ASCII: Big Endian表記 → Little Endian格納）
- 🔲 エラーハンドリング強化
- 🔲 ログ出力（解析開始・成功・失敗）
- 🔲 単体テストケース作成
  - 正常系: 標準的な応答フレーム
  - 異常系: 不正な開始文字、長さ不足、HEX変換エラー

---

## 3. テスト計画

### 3.1 単体テスト

#### 3.1.1 DetectResponseFrameType() テスト

**テストケース**:

| No | 入力データ | 期待結果 | 説明 |
|----|----------|---------|------|
| 1 | `[0xD0, 0x00, ...]` | Frame3E_Binary | 3E Binary検出 |
| 2 | `[0xD4, 0x00, ...]` | Frame4E_Binary | 4E Binary検出 |
| 3 | `"D0..."` (ASCII) | Frame3E_ASCII | 3E ASCII検出 |
| 4 | `"D4..."` (ASCII) | Frame4E_ASCII | 4E ASCII検出 |
| 5 | `[0x00, 0x00, ...]` | FormatException | 不明なサブヘッダ |
| 6 | `null` | ArgumentException | null入力 |
| 7 | `[0xD0]` | ArgumentException | 長さ不足 |

#### 3.1.2 GetDeviceDataOffset() テスト

**テストケース**:

| No | フレームタイプ | 期待オフセット | 説明 |
|----|-------------|--------------|------|
| 1 | Frame3E_Binary | 11 | 標準仕様 |
| 2 | Frame4E_Binary | 15 | 標準仕様（実機確認済み） |
| 3 | Frame3E_ASCII | 20 | 標準仕様 |
| 4 | Frame4E_ASCII | 30 | 標準仕様 |

#### 3.1.3 Parse3EFrameStructureAscii() テスト

**テストデータ**:
```
D000FF03FF000010000000010002
```

**解析期待値**:
- 終了コード: 0x0000
- デバイスデータ: `[0x01, 0x00, 0x02, 0x00]` (ワード値: 1, 2)

#### 3.1.4 Parse4EFrameStructureAscii() テスト

**テストデータ**:
```
D400000100000FF03FF000010000000FFFF0000
```

**フレーム構造の説明**:
```
D4      : サブヘッダ
00      : 予約1
0001    : シーケンス番号
0000    : 予約2
00      : ネットワーク番号
FF      : PC番号
03FF    : I/O番号
00      : 局番
0010    : データ長
0000    : 終了コード
FFFF0000: デバイスデータ
```

**解析期待値**:
- シーケンス番号: 0x0001
- 終了コード: 0x0000
- デバイスデータ: `[0xFF, 0xFF, 0x00, 0x00]` (ワード値: 0xFFFF, 0x0000)

### 3.2 統合テスト

#### 3.2.1 実機データ再生テスト

**準備**:
1. memo.mdから4E Binary実機データを抽出
2. 手動で4E ASCII形式のテストデータを作成
3. ProcessReceivedRawData()で両方を処理

**検証項目**:
- ✅ 4E Binary: 既存の実機データで正常動作
- 🔲 4E ASCII: 同等の内容で正常動作
- 🔲 デバイス値が両形式で一致

#### 3.2.3 実機データとの照合（Phase1完了後に実施）

**検証用実機データ**: memo.md の 4E Binary 応答フレーム

```
応答フレーム (hex):
D4 00 00 00 00 00 00 FF FF 03 00 62 00 00 00 FF FF ...
```

**検証項目**:
- Idx 0-1: サブヘッダ 0xD4 0x00 → `FrameType.Frame4E_Binary` 判定成功
- Idx 13-14: 終了コード 0x00 0x00 → 正常終了確認
- Idx 15~: デバイスデータ → `GetDeviceDataOffset()` が15を返すこと
- 抽出されたデバイス値が既存実装と一致すること

#### 3.2.2 エラーケーステスト

**テストケース**:
- 不完全フレーム（途中で切れたデータ）
- 不正な終了コード（0x0000以外）
- HEX文字列に非16進文字が含まれる
- サブヘッダ不正（0xD0/0xD4/'D'以外）
- データ長不足（最小フレーム長未満）

---

## 4. 実装手順

### 4.1 推奨実装順序

1. **FrameType列挙型の拡張**（10分）
2. **DetectResponseFrameType()実装**（1時間）
3. **単体テスト（フレーム判定）**（30分）
4. **GetDeviceDataOffset()拡張**（30分）
5. **Parse3EFrameStructureAscii()実装**（2時間）
6. **単体テスト（3E ASCII）**（1時間）
7. **Parse4EFrameStructureAscii()実装**（2時間）
8. **単体テスト（4E ASCII）**（1時間）
9. **統合テスト（全形式）**（1-2時間）

**合計**: 8-11時間

### 4.2 実装時の注意点

#### 4.2.1 エンディアン変換の注意

ASCII形式では16進文字列がビッグエンディアン表記（上位桁が先）:
```
"FFFF" → 0xFF 0xFF (バイト配列: [0xFF, 0xFF])
"0001" → 0x00 0x01 (バイト配列: [0x01, 0x00]) ※リトルエンディアン格納
```

変換時は下位2桁を先にバイト配列に格納:
```csharp
deviceData[i * 2] = Convert.ToByte(wordHex.Substring(2, 2), 16);     // 下位
deviceData[i * 2 + 1] = Convert.ToByte(wordHex.Substring(0, 2), 16); // 上位
```

#### 4.2.2 文字列長の検証

ASCII形式では文字列長が重要:
- 3E: 最低20文字
- 4E: 最低30文字
- データ部: 4文字単位（1ワード = 4文字）

境界チェックを忘れずに実装。

#### 4.2.3 ログ出力の充実

デバッグ性向上のため、以下をログ出力:
- 検出されたフレーム形式
- 解析開始時の先頭50文字
- 抽出された終了コード・データ長
- 変換されたワード数・バイト数

---

## 5. Phase 1 完了基準

### 5.1 機能要件

- ✅ 3E/4E × Binary/ASCII 全4パターンの自動判定
- ✅ 各形式の正確なデータオフセット決定
- ✅ ASCII形式の正確な解析とバイト配列変換
- ✅ 既存Binary形式パーサーとの統合

### 5.2 品質要件

- ✅ 全単体テストがパス
- ✅ 統合テストで実機データと同等の結果
- ✅ エラーケースで適切な例外スロー
- ✅ ログ出力でデバッグ可能な情報量

### 5.3 ドキュメント要件

- ✅ コード内コメント（各メソッド、複雑なロジック）
- ✅ テスト結果レポート
- ✅ 実装記録の作成（documents/implementation_records/）

---

## 6. Phase 1 後の次ステップ

Phase 1完了後は以下に進む:

1. **Phase 2: ビット展開機能** → ConMoni互換のビット展開実装
2. **実機テスト** → ASCII形式での実機通信確認（環境があれば）
3. **設定ファイル拡張** → appsettings.jsonにASCII設定追加

---

**文書作成者**: Claude Code
**参照元**: 受信データ解析_実装方針決定.md
