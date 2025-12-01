# PlcCommunicationManager テスト設計 Part2 (Step6関連)

## テスト方針
- **Step3-6ブロック（PlcCommunicationManager）を優先**して実装・テスト
- **TDD手法**を使用してテスト駆動開発を実施
- **DWord結合処理**と**構造化データ変換**の詳細テスト

## 対応する動作フロー
- **Step6**: 受信データ解析(DWordの場合は事前に結合処理ステップを入れる)

---

## 2. PlcCommunicationManager テスト設計（続き）

### 2.5 ProcessReceivedRawData メソッド（Step6-1）
**目的**: 受信データ基本後処理機能をテスト

#### 正常系テスト
- **TC029_ProcessReceivedRawData_基本後処理成功**
  - 入力:
    - Step4で受信した生データ(16進数)
    - ProcessedDeviceRequestInfo（送信時の前処理情報）
  - 期待出力: BasicProcessedResponseData（基本後処理結果オブジェクト）
    - 基本結果: 元生データ、基本処理済みデータ（デバイス名キー構造）、処理時刻
    - エラー情報: エラーフラグ、エラー・警告メッセージリスト
    - 統計情報: 処理デバイス数
  - 処理内容:
    - 生データの16進数パース
    - デバイス別データ抽出
    - 基本的な型変換（ビット/ワード）
    - エラー検証・記録

- **TC030_ProcessReceivedRawData_混合データ型基本処理**
  - 入力:
    - 混合データ型の受信生データ（ビット・ワード混在）
    - ProcessedDeviceRequestInfo（混合型要求情報）
  - 期待出力: 基本処理済みデータ（型別最適化済み、DWord結合前状態）

#### 異常系テスト
- **TC031_ProcessReceivedRawData_不正生データ**
  - 入力: 不正なフォーマットの受信データ
  - 期待出力: DataProcessingException

### 2.6 CombineDwordData メソッド（Step6-2）
**目的**: DWord結合処理機能をテスト

#### 正常系テスト
- **TC032_CombineDwordData_DWord結合処理成功**
  - 入力:
    - BasicProcessedResponseData（基本後処理結果）
    - ProcessedDeviceRequestInfo（DWord分割情報）
  - 期待出力: ProcessedResponseData（最終処理結果オブジェクト）
    - 基本結果: 元生データ、処理済みデータ（DWord結合済み）、DWord結合フラグ、処理時刻
    - エラー情報: エラーフラグ、エラー・警告メッセージリスト
    - 統計情報: 処理デバイス数、DWord結合数（自動計算）
  - 処理内容:
    - DWord分割要否判定
    - Low/Highワードの結合処理（16bit×2→32bit）
    - 結合結果の検証
    - DWord結合統計の計算
  - 動作フロー成功条件: DWordの情報を読み取る場合はLow/High結合→構造化データ変換のステップが正しく動作すること

- **TC033_CombineDwordData_結合不要データ処理**
  - 入力:
    - BasicProcessedResponseData（Word型・Bit型のみのデータ）
    - ProcessedDeviceRequestInfo（DWord分割なし情報）
  - 期待出力: ProcessedResponseData（DWord結合フラグ=false、結合処理スキップ）

- **TC034_CombineDwordData_混合データ型結合処理**
  - 入力:
    - BasicProcessedResponseData（ビット・ワード・DWord対象混在）
    - ProcessedDeviceRequestInfo（部分的DWord分割情報）
  - 期待出力: 必要なデバイスのみDWord結合済み最終データ

#### 異常系テスト
- **TC035_CombineDwordData_基本後処理未実行**
  - 入力: null または未処理のBasicProcessedResponseData
  - 期待出力: InvalidOperationException

- **TC036_CombineDwordData_DWord結合エラー**
  - 入力: 不正なLow/Highワードペアを含むBasicProcessedResponseData
  - 期待出力: DataProcessingException

### 2.7 ParseRawToStructuredData メソッド（Step6-3）
**目的**: 構造化データ変換機能をテスト

#### 正常系テスト

- **TC037_ParseRawToStructuredData_3Eフレーム解析（Binary/ASCII）**
  - 入力: ProcessedResponseData（DWord結合済み処理データ、3Eフレーム）
  - 期待出力: StructuredData（SLMP構造化解析結果オブジェクト）
    - 基本構造化データ: SLMPヘッダー（全標準情報）、終了コード、デバイスデータ、受信時刻、エラーフラグ
    - 解析詳細情報: 解析手順記録、解釈情報、処理時間、デバイス解釈、ステータス判定
    - エラー詳細情報: 詳細エラーコード、エラー説明、影響デバイス（エラー時のみ）

  **3E Binary詳細仕様:**

  **入力フレーム（13バイト）:**
  ```
  D0 00 00 FF FF 03 00 04 00 00 00 C8 00
  ```

  **フレーム構造:**
  ```
  [0-1]  D0 00      サブヘッダ（3E Binary応答）
  [2]    00         ネットワーク番号
  [3]    FF         PC番号
  [4-5]  FF 03      I/O番号（LE: 0x03FF）
  [6]    00         局番
  [7-8]  04 00      データ長（LE: 4バイト）
  [9-10] 00 00      終了コード
  [11-12] C8 00     デバイスデータ（2バイト）
  ```

  **解析処理の流れ:**
  1. フレームタイプ判定: frame[0] == 0xD0 && frame[1] == 0x00 → 3E Binary
  2. ヘッダー解析（オフセット2-10、9バイト）:
     - NetworkNumber = frame[2] = 0x00
     - PcNumber = frame[3] = 0xFF
     - IoNumber = BitConverter.ToUInt16(frame, 4) = 0x03FF
     - StationNumber = frame[6] = 0x00
     - DataLength = BitConverter.ToUInt16(frame, 7) = 0x0004
     - EndCode = BitConverter.ToUInt16(frame, 9) = 0x0000
  3. デバイスデータ抽出（オフセット11-、2バイト）:
     - DeviceDataOffset = 11
     - DeviceDataLength = DataLength - 2 = 2
     - DeviceData = frame[11..13] = [0xC8, 0x00]
  4. ビットデータ変換（BCD形式）:
     - 0xC8 → BCDデコード → [0, 0, 0, 1, 0, 0, 1, 1]
     - M100=0, M101=0, M102=0, M103=1, M104=0, M105=0, M106=1, M107=1

  **期待出力（Binary）:**
  ```csharp
  {
    "FrameType": "3E",
    "IsBinary": true,
    "SubHeader": 0xD000,
    "NetworkNumber": 0x00,
    "PcNumber": 0xFF,
    "IoNumber": 0x03FF,
    "StationNumber": 0x00,
    "DataLength": 4,
    "EndCode": 0x0000,
    "DeviceDataOffset": 11,
    "DeviceDataLength": 2,
    "DeviceData": [0xC8, 0x00],
    "BitValues": [0, 0, 0, 1, 0, 0, 1, 1]  // M100-M107
  }
  ```

  **検証項目（Binary）:**
  - 3Eフレーム構造の正確な解析（11バイトヘッダー）
  - シーケンス番号・予約フィールドが存在しない
  - デバイスデータ開始位置の正確性（オフセット11）
  - ビットデータのBCDデコード（0xC8 → [0,0,0,1,0,0,1,1]）

  **3E ASCII詳細仕様:**

  **入力フレーム（文字列）:**
  ```
  "D00000FF03FF000004000000C800"
  ```

  **フレーム構造:**
  ```
  [0-1]   "D0"      サブヘッダ（3E ASCII応答）
  [2-3]   "00"      ネットワーク番号
  [4-5]   "FF"      PC番号
  [6-9]   "03FF"    I/O番号
  [10-11] "00"      局番
  [12-15] "0004"    データ長（4バイト）
  [16-19] "0000"    終了コード
  [20-]   "C800"    デバイスデータ
  ```

  **解析処理の流れ:**
  1. フレームタイプ判定: frame.Substring(0, 2) == "D0" → 3E ASCII
  2. ヘッダー解析（文字2-19、18文字）:
     - NetworkNumber = Convert.ToInt32(frame.Substring(2, 2), 16) = 0x00
     - PcNumber = Convert.ToInt32(frame.Substring(4, 2), 16) = 0xFF
     - IoNumber = Convert.ToInt32(frame.Substring(6, 4), 16) = 0x03FF
     - StationNumber = Convert.ToInt32(frame.Substring(10, 2), 16) = 0x00
     - DataLength = Convert.ToInt32(frame.Substring(12, 4), 16) = 4
     - EndCode = Convert.ToInt32(frame.Substring(16, 4), 16) = 0x0000
  3. デバイスデータ抽出（文字20-、4文字）:
     - DeviceDataOffset = 20
     - DeviceData = frame.Substring(20) = "C800"
  4. ビットデータ変換（16進数ASCII → バイナリ）:
     - "C800" → [0xC8, 0x00] → BCDデコード → [0, 0, 0, 1, 0, 0, 1, 1]

  **期待出力（ASCII）:**
  ```csharp
  {
    "FrameType": "3E",
    "IsBinary": false,
    "SubHeader": "D000",
    "NetworkNumber": 0x00,
    "PcNumber": 0xFF,
    "IoNumber": 0x03FF,
    "StationNumber": 0x00,
    "DataLength": 4,
    "EndCode": 0x0000,
    "DeviceDataOffset": 20,
    "DeviceData": "C800",
    "BitValues": [0, 0, 0, 1, 0, 0, 1, 1]
  }
  ```

- **TC038_ParseRawToStructuredData_4Eフレーム解析（Binary/ASCII）**
  - 入力: ProcessedResponseData（DWord結合済み処理データ、4Eフレーム）
  - 期待出力: 4Eフレーム形式の構造化データ

  **4E Binary詳細仕様（実機データ使用）:**

  **実例入力フレーム（113バイト）:**
  ```
  D4 00 00 00 00 00 00 FF FF 03 00 62 00 00 00 00 00
  FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF
  07 19 FF FF FF FF FF FF FF FF FF FF FF FF FF FF
  FF FF 00 10 00 08 00 01 00 10 00 10 00 08 20 00
  10 00 08 00 02 00 00 00 00 00 00 00 00 00 00 00
  00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
  00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
  ```

  **フレーム構造:**
  ```
  [0-1]  D4 00      サブヘッダ（4E Binary応答）
  [2-3]  00 00      シリアル番号
  [4-5]  00 00      予約
  [6]    00         ネットワーク番号
  [7]    FF         PC番号
  [8-9]  FF 03      I/O番号（LE: 0x03FF）
  [10]   00         局番
  [11-12] 62 00     データ長（LE: 98バイト）
  [13-14] 00 00     監視タイマ（応答は0固定）
  [15-16] 00 00     終了コード（正常終了）
  [17-]  [96バイト] デバイスデータ
  ```

  **解析処理の流れ:**
  1. フレームタイプ判定: frame[0] == 0xD4 && frame[1] == 0x00 → 4E Binary
  2. シリアル・予約フィールド処理（オフセット2-5、4バイト）:
     - SerialNumber = BitConverter.ToUInt16(frame, 2) = 0x0000
     - Reserved = BitConverter.ToUInt16(frame, 4) = 0x0000
  3. ヘッダー解析（オフセット6-16、11バイト）:
     - NetworkNumber = frame[6] = 0x00
     - PcNumber = frame[7] = 0xFF
     - IoNumber = BitConverter.ToUInt16(frame, 8) = 0x03FF
     - StationNumber = frame[10] = 0x00
     - DataLength = BitConverter.ToUInt16(frame, 11) = 98
     - EndCode = BitConverter.ToUInt16(frame, 15) = 0x0000 ※オフセット15-16が重要
  4. デバイスデータ抽出（オフセット17-、96バイト）:
     - DeviceDataOffset = 17
     - DeviceDataLength = DataLength - 2 = 96
     - DeviceData = frame[17..113]
  5. ワードデータ変換（リトルエンディアン）:
     - ワード数 = 96 / 2 = 48
     - WordData[8] = BitConverter.ToUInt16(DeviceData, 16) = 0x1907 (6407)
     - WordData[23] = 0x0020 (32)
     - WordData[24] = 0x0010 (16)
     - WordData[25] = 0x0008 (8)
     - WordData[26] = 0x0002 (2)

  **期待出力（Binary）:**
  ```csharp
  {
    "FrameType": "4E",
    "IsBinary": true,
    "SubHeader": 0xD400,
    "SerialNumber": 0x0000,
    "NetworkNumber": 0x00,
    "PcNumber": 0xFF,
    "IoNumber": 0x03FF,
    "StationNumber": 0x00,
    "DataLength": 98,
    "EndCode": 0x0000,
    "DeviceDataOffset": 17,
    "DeviceDataLength": 96,
    "DeviceData": [96バイトのバイト配列],
    "WordData": [
      0xFFFF, 0xFFFF, ... (8ワード),
      0x1907,  // Word 8: 6407
      0xFFFF, ... (7ワード),
      0xFFFF, 0x1000, 0x0800, 0x0100, 0x1000, 0x1000, 0x0800,
      0x0020,  // Word 23: 32
      0x0010,  // Word 24: 16
      0x0008,  // Word 25: 8
      0x0002,  // Word 26: 2
      0x0000, ... (21ワード)
    ]
  }
  ```

  **検証項目（Binary、最重要）:**
  - ✓ サブヘッダが `D4 00` （4E応答フレーム）
  - ✓ シリアルフィールド（2バイト）と予約フィールド（2バイト）が存在
  - ✓ ヘッダー長が17バイト
  - ✓ **終了コード抽出位置がオフセット15-16（11-12ではない！）**
  - ✓ デバイスデータ開始位置がオフセット17（13ではない！）
  - ✓ データ長フィールド = 98バイト（0x62）
  - ✓ 実際のデータ長 = 終了コード（2バイト） + デバイスデータ（96バイト） = 98バイト
  - ✓ リトルエンディアン変換が正しい
  - ✓ 特徴的な値の検出: Word 8=6407, Word 23=32, Word 24=16, Word 25=8, Word 26=2

  **不合格条件（即座に調査が必要）:**
  - ❌ 終了コード抽出位置が誤っている（オフセット11-12を使用している）
  - ❌ デバイスデータ開始位置が誤っている（オフセット13を使用している）
  - ❌ シリアル・予約フィールドが考慮されていない
  - ❌ ワード値がリトルエンディアンで正しく変換されていない
  - ❌ 特徴的な値が抽出できていない

  **4E ASCII詳細仕様:**

  **想定入力フレーム:**
  ```
  "D40000000000FFFF03000062000000FFFF..."
  ```

  **フレーム構造:**
  ```
  [0-1]   "D4"              サブヘッダ（4E ASCII応答）
  [2-3]   "00"              予約1
  [4-7]   "0000"            シリアル
  [8-11]  "0000"            予約2
  [12-13] "00"              ネットワーク番号
  [14-15] "FF"              PC番号
  [16-19] "03FF"            I/O番号
  [20-21] "00"              局番
  [22-25] "0062"            データ長（98バイト）
  [26-29] "0000"            終了コード
  [30-]   デバイスデータ（16進数ASCII文字列、192文字）
  ```

  **解析処理の流れ:**
  1. フレームタイプ判定: frame.Substring(0, 2) == "D4" → 4E ASCII
  2. シリアル・予約フィールド処理（文字2-11、10文字）:
     - Reserved1 = frame.Substring(2, 2) = "00"
     - SerialNumber = Convert.ToInt32(frame.Substring(4, 4), 16) = 0x0000
     - Reserved2 = frame.Substring(8, 4) = "0000"
  3. ヘッダー解析（文字12-29、18文字）:
     - NetworkNumber = Convert.ToInt32(frame.Substring(12, 2), 16) = 0x00
     - PcNumber = Convert.ToInt32(frame.Substring(14, 2), 16) = 0xFF
     - IoNumber = Convert.ToInt32(frame.Substring(16, 4), 16) = 0x03FF
     - StationNumber = Convert.ToInt32(frame.Substring(20, 2), 16) = 0x00
     - DataLength = Convert.ToInt32(frame.Substring(22, 4), 16) = 98
     - EndCode = Convert.ToInt32(frame.Substring(26, 4), 16) = 0x0000
  4. デバイスデータ抽出（文字30-、192文字）:
     - DeviceDataOffset = 30
     - DeviceDataLength = (DataLength - 4) * 2 = 188（ただし実際は192）
     - DeviceData = frame.Substring(30)
  5. ワードデータ変換（ASCII16進数 → ushort）:
     - 4文字ずつ処理: "FFFF" → 0xFFFF
     - "1907" → 0x1907

  **期待出力（ASCII）:**
  ```csharp
  {
    "FrameType": "4E",
    "IsBinary": false,
    "SubHeader": "D400",
    "SerialNumber": 0x0000,
    "NetworkNumber": 0x00,
    "PcNumber": 0xFF,
    "IoNumber": 0x03FF,
    "StationNumber": 0x00,
    "DataLength": 98,
    "EndCode": 0x0000,
    "DeviceDataOffset": 30,
    "DeviceDataLength": 192,
    "DeviceData": "FFFF...1907FFFF...",
    "WordData": [48ワードの値配列]
  }
  ```

- **TC039_ParseRawToStructuredData_DWord結合済みデータ解析**
  - 入力: ProcessedResponseData（DWord結合済み、IsDwordCombined=true）
  - 期待出力: DWord結合されたデバイス情報を含む構造化データ

#### 異常系テスト

- **TC040_ParseRawToStructuredData_異常系全般**

  **TC040-1: DWord結合未実行**
  - 入力: 未処理のProcessedResponseData（CombineDwordData未実行）
  - 期待出力: InvalidOperationException

  **TC040-2: 不正フレームタイプ**
  - 目的: 不正なフレームタイプの検出
  - 入力: サブヘッダが `0xAA00` など不正な値
  - 期待出力: `ArgumentException("Unknown frame type")`

  **TC040-3: 不完全フレーム**
  - 目的: 不完全フレームの検出
  - 入力: 4Eフレームだがデータが10バイトしかない
  - 期待出力: `ArgumentException("Incomplete frame data")`

- **TC041_ParseRawToStructuredData_エラー終了コード**
  - 目的: エラー終了コードの検出
  - 入力: 終了コードが `0xC059` (不正コマンド)
  - 期待出力:
  ```csharp
  {
    "EndCode": 0xC059,
    "IsError": true,
    "ErrorMessage": "Invalid command",
    "DeviceData": null
  }
  ```

---

## 2.8 オフセット計算まとめ

**重要: フレームタイプ別のオフセット**
- **3E Binary**: ヘッダー11バイト（サブヘッダ2 + フィールド9）
- **3E ASCII**: ヘッダー20文字（サブヘッダ2 + フィールド18）
- **4E Binary**: ヘッダー17バイト（サブヘッダ2 + シリアル・予約4 + フィールド11）
- **4E ASCII**: ヘッダー30文字（サブヘッダ2 + 予約・シリアル10 + フィールド18）

**データ長の解釈:**
- **Binary**: 終了コード（2バイト）を含む
- **ASCII**: 終了コード（4文字）を含む
- 実データ長 = データ長フィールド値 - 終了コード長

**エンディアン処理:**
- **Binary**: リトルエンディアン（BitConverter.ToUInt16）
- **ASCII**: 16進数文字列（既にビッグエンディアン表示）

---

### データ変換ユーティリティメソッド

#### ParseBinaryBitData
```csharp
public static bool[] ParseBinaryBitData(byte[] binaryData, int count)
{
    // BCDデコード: [0x12] → [1, 2]
    var decoded = DecodeBcd(binaryData);
    var result = decoded.Select(b => b == 1).ToArray();

    // 奇数個の場合、最後の余分なビットを削除
    if (count % 2 == 1 && result.Length > count)
    {
        return result.Take(count).ToArray();
    }

    return result;
}
```

#### ParseBinaryWordData
```csharp
public static ushort[] ParseBinaryWordData(byte[] binaryData)
{
    var result = new ushort[binaryData.Length / 2];
    Buffer.BlockCopy(binaryData, 0, result, 0, binaryData.Length);
    return result;
}
```

#### ParseAsciiWordData
```csharp
public static ushort[] ParseAsciiWordData(string asciiData)
{
    var result = new ushort[asciiData.Length / 4];
    for (int i = 0; i < asciiData.Length; i += 4)
    {
        result[i / 4] = Convert.ToUInt16(asciiData.Substring(i, 4), 16);
    }
    return result;
}
```

---

### パフォーマンステスト

#### TC042_Parse_LargeFrame_Performance（大量データ処理）

**目的**: 大量データフレームの処理性能
**入力**: 1000ワード（2000バイト）のデバイスデータを含むフレーム
**期待出力**: パース処理時間 < 10ms

---

## Step6詳細フロー（動作フロー対応）

### Step6-1: ProcessReceivedRawData（基本後処理）
1. 生データ(16進数)の検証
2. SLMPヘッダー情報抽出
3. 終了コード確認
4. デバイスデータ部抽出
5. デバイス別データ分離
6. 基本的な型変換（ビット/ワード）
7. BasicProcessedResponseData返却

### Step6-2: CombineDwordData（DWord結合処理）
1. DWord分割情報確認
2. DWord結合要否判定
3. Low/Highワードペア抽出
4. 16bit×2→32bit結合処理
5. エンディアン処理
6. 結合結果検証
7. ProcessedResponseData返却

### Step6-3: ParseRawToStructuredData（構造化変換）
1. ProcessedResponseData検証
2. SLMPヘッダー詳細解析
3. 終了コード解釈
4. デバイスデータ構造化
5. デバイス解釈情報追加
6. ステータス判定
7. StructuredData返却

---

## データ破損時の対応方針（継続実行モード）

### 検出・対応フロー
1. ParseRawToStructuredDataで壊れたデータを検出
2. エラーログ出力（詳細なエラー情報記録）
3. 次サイクル継続判定
4. 正常データのみ処理、異常データは破棄
5. アプリケーション停止せず次サイクルで回復

### 成功条件
- データ破損等の一時的な異常でもアプリケーション停止せず次サイクルで回復すること

---

## 重要な統合テストケース

### TC143_10 Step3-6 M100～M107ビット読み出し2パターン統合テスト（オフライン）

**目的**: 実際のPLC機器無しで、M100～M107ビット読み出しの2パターン（3E/4E × バイナリ）について、Step3-6の完全な通信フローが正しく動作することを検証

#### 想定するPLC設備状態
```
M100: OFF (0)
M101: OFF (0)
M102: OFF (0)
M103: ON  (1)
M104: OFF (0)
M105: OFF (0)
M106: ON  (1)
M107: ON  (1)

ビット配列: 0 0 0 1 0 0 1 1
16進数: 0xC8 (1バイト目), 0x00 (2バイト目、未使用ビット)
```

#### パターン1: 3Eフレーム × バイナリ（ビット単位読み出し）

**送信フレーム（23バイト）:**
```
50 00 00 FF FF 03 00 0E 00 00 00 01 04 01 00 9C 00 64 00 00 00 08 00
```

**受信フレーム（15バイト）:**
```
D0 00 00 FF FF 03 00 04 00 00 00 C8 00
```

**解析後データ（成功条件）:**
- status: "SUCCESS"
- end_code: "0x0000"
- device_type: "M"
- start_address: 100
- point_count: 8
- data_format: "bit"
- bit_values: M100=0, M101=0, M102=0, M103=1, M104=0, M105=0, M106=1, M107=1
- raw_data_hex: "C8 00"

#### パターン2: 4Eフレーム × バイナリ（ビット単位読み出し）

**送信フレーム（27バイト）:**
```
54 00 00 00 00 00 00 FF FF 03 00 0E 00 00 00 01 04 01 00 9C 00 64 00 00 00 08 00
```

**受信フレーム（19バイト）:**
```
D4 00 00 00 00 00 00 FF FF 03 00 04 00 00 00 C8 00
```

**解析後データ:** パターン1と同じ構造化データ

#### 成功条件（テスト合格基準）

**1. Step3（接続）の成功条件**
- ConnectionResponse.Status == `Connected`
- Socket接続情報が正しく設定されている
- 例外が発生しない

**2. Step4-1（送信）の成功条件**
- 送信バイト数 == 期待バイト数（3E: 23バイト、4E: 27バイト）
- 送信フレームの16進数ダンプが期待値と一致
- 送信統計が正しく記録される
- 例外が発生しない

**3. Step4-2（受信）の成功条件**
- 受信バイト数 == 期待バイト数（3E: 15バイト、4E: 19バイト）
- 受信データの16進数ダンプが期待値と一致
- 生データ(16進数)として正しく保存される
- 例外が発生しない

**4. Step5（切断）の成功条件**
- 切断処理が正常に完了する
- 接続統計情報が正しく記録される
- リソースが適切に解放される（メモリリークなし）
- 例外が発生しない

**5. Step6（データ解析）の成功条件**
- 終了コードが正常（`0x0000`）
- StructuredData.IsError == `false`
- ビット値が期待通り（M100～M107の各値が正確）
- RawDataHex == `"C8 00"`

**統合成功条件（全パターン共通）:**
- フレーム整合性: 送信フレームと受信フレームが仕様通りの構造
- データ整合性: ビット配列が正しくエンコード・デコードされる
- エラーハンドリング: 異常系でも適切にエラーが検出・報告される
- リソース管理: 全ステップ完了後、リソースが解放されている
- ログ出力: 各ステップで適切なログが出力される

---

### TC143_11 4Eフレーム ランダム読み出し（複数デバイス）実機データ解析テスト

**目的**: 実機PLCから受信した4Eフレーム（ランダム読み出し応答）の構造解析とデータ抽出が正しく動作することを検証

#### 実際に受信した4Eフレーム（113バイト）

**受信フレーム全体:**
```
D4 00 00 00 00 00 00 FF FF 03 00 62 00 00 00 00 00
FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF
07 19 FF FF FF FF FF FF FF FF FF FF FF FF FF FF
FF FF 00 10 00 08 00 01 00 10 00 10 00 08 20 00
10 00 08 00 02 00 00 00 00 00 00 00 00 00 00 00
00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
```

#### フレーム構造解析

**ヘッダー部分（17バイト）:**

| オフセット | バイト列 | フィールド | 値 | 説明 |
|-----------|---------|-----------|-----|------|
| 0-1 | `D4 00` | サブヘッダ | 0xD400 | 4E応答フレーム |
| 2-3 | `00 00` | シリアル | 0x0000 | 要求のエコーバック |
| 4-5 | `00 00` | 予約 | 0x0000 | 予約領域 |
| 6 | `00` | ネットワーク番号 | 0 | ローカル |
| 7 | `FF` | PC番号 | 255 | 全局 |
| 8-9 | `FF 03` | I/O番号 | 0x03FF | 1023 (LE) |
| 10 | `00` | 局番 | 0 | マルチドロップなし |
| 11-12 | `62 00` | データ長 | 0x0062 | 98バイト (LE) |
| 13-14 | `00 00` | 監視タイマ | 0x0000 | 応答は0固定 |
| 15-16 | `00 00` | 終了コード | 0x0000 | **正常終了** ✓ |

**デバイスデータ部分（96バイト、オフセット17～112）:**

特徴的な値の抽出（リトルエンディアン）:
- Word 8 (オフセット33-34): `07 19` → 6407 (0x1907)
- Word 23 (オフセット63-64): `20 00` → 32 (0x0020)
- Word 24 (オフセット65-66): `10 00` → 16 (0x0010)
- Word 25 (オフセット67-68): `08 00` → 8 (0x0008)
- Word 26 (オフセット69-70): `02 00` → 2 (0x0002)

#### 検証項目

**1. フレーム構造の正確性:**
- ✓ サブヘッダが `D4 00` （4E応答フレーム）
- ✓ シリアルフィールド（2バイト）と予約フィールド（2バイト）が存在
- ✓ ヘッダー長が17バイト
- ✓ デバイスデータ開始位置がオフセット17

**2. 終了コードの検証:**
- ✓ 終了コード = `0x0000` （正常終了）
- ✓ エラーメッセージ = null

**3. データ長の整合性:**
- ✓ データ長フィールド = 98バイト（0x62）
- ✓ 実際のデータ長 = 終了コード（2バイト） + デバイスデータ（96バイト） = 98バイト

**4. デバイスデータの解析:**
- ✓ デバイスデータ長 = 96バイト
- ✓ ワード数 = 48ワード（96バイト ÷ 2）
- ✓ リトルエンディアン変換が正しい
- ✓ 特徴的な値の検出: Word 8=6407, Word 23=32, Word 24=16, Word 25=8, Word 26=2

**5. エッジケースの処理:**
- ✓ 0xFFFF（最大値）の正しい処理
- ✓ 0x0000（最小値）の正しい処理
- ✓ 混在データ（FF, 有効値, 00）の正しい解析

#### 成功条件（テスト合格基準）

1. **ヘッダー解析成功:**
   - サブヘッダ判定: `4Eフレーム` として認識
   - 終了コード抽出: オフセット15-16から `0x0000` を正しく取得
   - データ長検証: 98バイトを正しく認識

2. **デバイスデータ抽出成功:**
   - デバイスデータ開始位置: オフセット17から正しく抽出
   - デバイスデータ長: 96バイトを正しく取得

3. **ワード値変換成功:**
   - リトルエンディアン変換が全ワードで正しい
   - 特徴的な値（6407, 32, 16, 8, 2）が正確に抽出される

4. **エラーハンドリング:**
   - フレーム長不足時に適切なエラー検出
   - 不正なサブヘッダ時に適切なエラー検出

#### 不合格条件（即座に調査が必要）

- ❌ 終了コード抽出位置が誤っている（オフセット11-12を使用している）
- ❌ デバイスデータ開始位置が誤っている（オフセット13を使用している）
- ❌ シリアル・予約フィールドが考慮されていない
- ❌ ワード値がリトルエンディアンで正しく変換されていない
- ❌ 特徴的な値が抽出できていない

---

## テストデータ
- **PLCシミュレータ**: 実際のPLC無しでの通信テスト
- **ネットワークモック**: 通信エラーシミュレーション用
- **模擬応答データ**: 既知のSLMP応答パターン
- **実機データ**: 実際のPLC通信で取得したデータサンプル
