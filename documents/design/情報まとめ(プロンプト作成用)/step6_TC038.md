# Step6 ParseRawToStructuredData テスト実装用情報（TC038）

## ドキュメント概要

### 目的
このドキュメントは、TC038_ParseRawToStructuredData_4Eフレーム解析テストの実装に必要な情報を集約したものです。
**コード作成時に必要となる技術情報のみ**を記載しており、学習資料や説明的な内容は含みません。

### 情報取得元
本ドキュメントの情報は以下のソースから抽出・統合されています：

#### 設計書（andon/documents/design/）
- `クラス・メソッドリスト.md` - クラス・メソッドの一覧と概要
- `クラス設計.md` - 詳細なクラス設計仕様
- `テスト内容.md` - テストケース仕様
- `プロジェクト構造設計.md` - フォルダ構造・プロジェクト構成
- `依存関係.md` - クラス間の依存関係

#### 実装参考（PySLMPClient）
- `PySLMPClient/pyslmpclient/const.py` - SLMP定数・列挙型定義
- `PySLMPClient/pyslmpclient/__init__.py` - SLMPクライアント実装
- `PySLMPClient/pyslmpclient/util.py` - フレーム作成ユーティリティ
- `PySLMPClient/tests/test_main.py` - テストケース実例

---

## 1. テスト対象メソッド仕様

### ParseRawToStructuredData（Step6-3: 構造化データ解析）
**クラス**: PlcCommunicationManager
**名前空間**: andon.Core.Managers

#### Input
- ProcessedResponseData（DWord結合後処理結果データ）
  - Step6-2（CombineDwordData）で生成されたDWord結合済みデータ
  - BasicProcessedData（ワード・ビット単位データ）
  - CombinedDWordData（32ビットDWordデータ）
- ProcessedDeviceRequestInfo（前処理済みデバイス要求情報）
  - SLMP応答フレーム解析情報
  - デバイス配置情報、フレーム形式情報

#### Output
- StructuredData（最終構造化データオブジェクト）
  - 継承結果:
    - ProcessedResponseData全プロパティ（元データ保持）
  - 拡張結果:
    - StructuredDeviceData: 構造化デバイスデータ（Dictionary<string, StructuredDevice>）
    - ParsedAt: 構造化解析処理時刻（DateTime）
    - StructuredDeviceCount: 構造化済みデバイス数（int）
    - SlmpFrameInfo: SLMPフレーム解析情報（SlmpFrameAnalysis）
  - エラー情報:
    - ParseErrors: 構造化解析固有エラー（List<string>）

#### 機能
- SLMP応答フレーム構造解析（4Eフレーム対応）
- デバイス別構造化データ生成
  - デバイス名、データ型、値、メタデータの統合
  - ビット・ワード・DWordの統一インターフェース
- SLMP終了コード解析・エラー判定
- フレーム整合性検証（ヘッダ、データ長、チェックサム等）

#### データ取得元
- PlcCommunicationManager.CombineDwordData()（DWord結合済みデータ）
- ConfigToFrameManager（フレーム解析情報）

#### 重要な処理内容
**4Eフレーム解析**:
- 4Eフレーム構造（拡張形式）の解析
- ヘッダ情報抽出（サブヘッダ: 54001234000000）
- 終了コード判定
- データ部の構造化処理

**構造化データ生成**:
- デバイス別メタデータ付加
- 統一されたデータアクセスインターフェース
- エラー・警告情報の統合

---

## 2. テストケース仕様（TC038）

### TC038_ParseRawToStructuredData_4Eフレーム解析
**目的**: 4EフレームSLMP応答の構造化解析機能をテスト

#### 前提条件
- CombineDwordDataが成功済み（Step6-2完了）
- ProcessedResponseDataが正常に生成済み
- SLMP 4Eフレーム形式の応答データが処理済み
- ProcessedDeviceRequestInfoにフレーム解析情報が設定済み

#### 入力データ
**ProcessedResponseData（DWord結合済みデータ）**:
- BasicProcessedData（ワード・ビット）:
  - "M000" → true, "M001" → false, ... （ビット型）
  - "D100" → 0x1234, "D101" → 0x5678, ... （ワード型）
- CombinedDWordData（DWord結合済み）:
  - "D000_DWord" → 0x56781234 （32ビット）
  - "D001_DWord" → 0xDEF09ABC （32ビット）

**ProcessedDeviceRequestInfo（フレーム解析情報）**:
- SlmpFrameFormat: "4E" （4Eフレーム形式）
- ResponseFrameHeader: "54001234000000" （4Eフレームサブヘッダ）
- DeviceConfiguration（デバイス配置情報）:
  - M機器: M000-M999, ビット型, 1000点
  - D機器: D000-D999, ワード/DWord型, 1000点

#### 期待出力
- StructuredData（構造化データ成功オブジェクト）
  - ParsedAt != null
  - StructuredDeviceCount > 0
  - SlmpFrameInfo != null
    - FrameVersion = "4E"
    - SubHeader = "54001234000000"
    - EndCode = 0x0000（正常終了）
    - DataLength > 0
  - StructuredDeviceData（構造化済みデバイス辞書）:
    - "M000" → { DeviceName="M000", DataType=Bit, Value=true, Metadata={...} }
    - "M001" → { DeviceName="M001", DataType=Bit, Value=false, Metadata={...} }
    - "D100" → { DeviceName="D100", DataType=Word, Value=0x1234, Metadata={...} }
    - "D000_DWord" → { DeviceName="D000", DataType=DWord, Value=0x56781234, Metadata={...} }
  - ParseErrors.Count == 0（エラーなし）

#### 動作フロー成功条件
1. **フレーム形式判定**: SlmpFrameFormat == "4E" を確認
2. **4Eヘッダ解析**: サブヘッダ "54001234000000" を正確に抽出
3. **終了コード確認**: EndCode == 0x0000（正常終了）を検証
4. **データ長検証**: DataLengthが期待値と一致することを確認
5. **デバイス別構造化**:
   - ビット型デバイス（M機器）をStructuredDevice形式に変換
   - ワード型デバイス（D機器）をStructuredDevice形式に変換
   - DWord型デバイス（D_DWord）をStructuredDevice形式に変換
6. **メタデータ付加**: 各デバイスにメタデータ（タイムスタンプ、データソース等）を付加
7. **統計情報更新**: StructuredDeviceCountを正確に計算
8. **エラー検証**: ParseErrorsが空リストであることを確認

---

## 3. SLMP 4Eフレーム詳細

### 4Eフレーム構造（拡張形式）

#### 送信フレーム構造
**M000-M999読み込みフレーム（4Eフレーム/ASCII）**:
```
フレーム: "54001234000000010401006400000090E8030000"

構成詳細:
  - サブヘッダ: "54001234000000" (4Eフレーム識別、7バイト)
    - 固定値: 54H 00H 12H 34H 00H 00H 00H 00H (バイナリ)
  - READコマンド: "0104" (0x0104, デバイス一括読み出し)
  - サブコマンド: "0100" (0x0001, ビット単位読み出し)
  - デバイスコード: "6400" (0x0064, M機器=90H、リトルエンディアン)
  - 開始番号: "00000090" (0x00000000, M000=0、リトルエンディアン24ビット)
  - デバイス点数: "E8030000" (0x000003E8=1000点、リトルエンディアン)
```

**D000-D999読み込みフレーム（4Eフレーム/ASCII）**:
```
フレーム: "54001234000000010400A800000090E8030000"

構成詳細:
  - サブヘッダ: "54001234000000" (4Eフレーム識別)
  - READコマンド: "0104" (0x0104)
  - サブコマンド: "0000" (0x0000, ワード単位読み出し)
  - デバイスコード: "A800" (0x00A8, D機器=A8H、リトルエンディアン)
  - 開始番号: "00000090" (0x00000000, D000=0)
  - デバイス点数: "E8030000" (0x000003E8=1000点)
```

#### 応答フレーム構造（4Eフレーム/ASCII）
**正常応答（M機器、125バイトデータ）**:
```
応答フレーム構造:
  - サブヘッダ: "54001234000000" (4Eフレーム識別、7バイト)
  - 終了コード: "0000" (0x0000, 正常終了)
  - データ部: <125バイト分のビットデータ> (1000ビット ÷ 8 = 125バイト)
  - ASCII形式: 各バイトが2文字のASCII16進数に変換（合計250文字）

検証項目:
  - サブヘッダ == "54001234000000"
  - 終了コード == 0x0000
  - データ長 == 250文字（125バイト × 2）
```

**正常応答（D機器、2000バイトデータ）**:
```
応答フレーム構造:
  - サブヘッダ: "54001234000000"
  - 終了コード: "0000" (0x0000, 正常終了)
  - データ部: <2000バイト分のワードデータ> (1000ワード × 2バイト = 2000バイト)
  - ASCII形式: 各バイトが2文字のASCII16進数に変換（合計4000文字）

検証項目:
  - サブヘッダ == "54001234000000"
  - 終了コード == 0x0000
  - データ長 == 4000文字（2000バイト × 2）
```

### 4Eフレーム vs 3Eフレームの違い

| 項目 | 3Eフレーム（簡易形式） | 4Eフレーム（拡張形式） |
|------|----------------------|---------------------|
| サブヘッダ | "5000" (2バイト) | "54001234000000" (7バイト) |
| 用途 | 基本的な通信 | 拡張機能・複雑な通信 |
| ヘッダサイズ | 小さい | 大きい |
| 対応機器 | 基本PLCシリーズ | 拡張PLCシリーズ（Q00UDPCPU等） |
| 実装難易度 | 低 | 中 |

**重要**: TC038では4Eフレームの正確な解析を検証する

---

## 4. 依存クラス・設定

### ProcessedResponseData（DWord結合後処理結果）
**取得元**: PlcCommunicationManager.CombineDwordData()

```csharp
public class ProcessedResponseData
{
    // 基本後処理結果（継承）
    public Dictionary<string, object> BasicProcessedData { get; set; }
    public string RawDataHex { get; set; }
    public DateTime ProcessedAt { get; set; }

    // DWord結合結果
    public Dictionary<string, uint> CombinedDWordData { get; set; }
    public bool IsDwordCombined { get; set; }
    public int DWordCombineCount { get; set; }

    // エラー情報
    public bool HasError { get; set; }
    public List<string> Errors { get; set; }
    public List<string> Warnings { get; set; }

    // 統計情報
    public int ProcessedDeviceCount { get; set; }
}
```

### StructuredData（最終構造化データ）
**本メソッドの出力データ型**

```csharp
public class StructuredData : ProcessedResponseData
{
    // 構造化結果
    public Dictionary<string, StructuredDevice> StructuredDeviceData { get; set; }
    public DateTime ParsedAt { get; set; }
    public int StructuredDeviceCount { get; set; }

    // SLMP解析情報
    public SlmpFrameAnalysis SlmpFrameInfo { get; set; }

    // エラー情報
    public List<string> ParseErrors { get; set; }
}
```

### StructuredDevice（構造化デバイス情報）
```csharp
public class StructuredDevice
{
    public string DeviceName { get; set; }        // 例: "M000", "D100", "D000_DWord"
    public DeviceDataType DataType { get; set; }  // Bit, Word, DWord
    public object Value { get; set; }             // 実際の値（型に応じて変換）
    public Dictionary<string, object> Metadata { get; set; } // メタデータ
}
```

### SlmpFrameAnalysis（SLMPフレーム解析情報）
```csharp
public class SlmpFrameAnalysis
{
    public string FrameVersion { get; set; }      // "3E" または "4E"
    public string SubHeader { get; set; }         // 4E: "54001234000000", 3E: "5000"
    public ushort EndCode { get; set; }           // 0x0000: 正常、その他: エラー
    public int DataLength { get; set; }           // データ部の長さ（バイト）
    public bool IsSuccess { get; set; }           // 終了コードが正常かどうか
    public string ErrorDescription { get; set; }  // エラー時の説明（正常時null）
}
```

### DeviceDataType（デバイスデータ型列挙型）
```csharp
public enum DeviceDataType
{
    Bit,     // ビット型（M機器等）
    Word,    // ワード型（16ビット、D機器等）
    DWord    // ダブルワード型（32ビット、DWord結合済み）
}
```

---

## 5. テスト実装方針（TDD）

### 開発手法
- C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.mdに記載のTDD手法を使用

### テストファイル配置
- **ファイル名**: PlcCommunicationManagerTests.cs
- **配置先**: Tests/Unit/Core/Managers/
- **名前空間**: andon.Tests.Unit.Core.Managers

### テスト実装順序
1. **TC037_ParseRawToStructuredData_3Eフレーム解析**（前提、完了済み）
2. **TC038_ParseRawToStructuredData_4Eフレーム解析**（本テスト、最優先）
3. TC039_ParseRawToStructuredData_DWord結合済みデータ解析
4. TC040-TC041: 異常系テスト

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockProcessedResponseData**: DWord結合済み処理結果データモック
  - BasicProcessedData、CombinedDWordDataを含む
  - IsDwordCombined = true
- **MockProcessedDeviceRequestInfo**: フレーム解析情報モック
  - SlmpFrameFormat = "4E"
  - ResponseFrameHeader = "54001234000000"
  - DeviceConfiguration設定済み

#### 使用するスタブ
- **ProcessedResponseDataStubs**: DWord結合済みデータスタブ
- **StructuredDataValidator**: 構造化データ検証ヘルパー
- **SlmpFrameAnalysisValidator**: SLMPフレーム解析情報検証ヘルパー

---

## 6. テストケース実装構造

### Arrange（準備）
1. **ProcessedResponseData準備**（DWord結合済み）:
   - BasicProcessedData = { "M000": true, "M001": false, "D100": 0x1234, ... }
   - CombinedDWordData = { "D000_DWord": 0x56781234, "D001_DWord": 0xDEF09ABC, ... }
   - IsDwordCombined = true
   - DWordCombineCount = 2

2. **ProcessedDeviceRequestInfo準備**:
   - SlmpFrameFormat = "4E"
   - ResponseFrameHeader = "54001234000000"
   - DeviceConfiguration:
     - M機器: M000-M999, Bit型
     - D機器: D000-D999, Word/DWord型

3. **PlcCommunicationManagerインスタンス作成**:
   - DWord結合処理完了状態で初期化

### Act（実行）
1. ParseRawToStructuredData実行:
   ```csharp
   var result = await plcCommManager.ParseRawToStructuredData(
       processedData,
       deviceRequestInfo
   );
   ```

### Assert（検証）
1. **構造化成功検証**:
   - `result != null`
   - `result.ParsedAt != null`
   - `result.StructuredDeviceCount > 0`

2. **SLMPフレーム解析情報検証**:
   - `result.SlmpFrameInfo != null`
   - `result.SlmpFrameInfo.FrameVersion == "4E"`
   - `result.SlmpFrameInfo.SubHeader == "54001234000000"`
   - `result.SlmpFrameInfo.EndCode == 0x0000`
   - `result.SlmpFrameInfo.IsSuccess == true`
   - `result.SlmpFrameInfo.ErrorDescription == null`

3. **構造化デバイスデータ検証**:
   - `result.StructuredDeviceData != null`
   - `result.StructuredDeviceData.Count == result.StructuredDeviceCount`
   - ビット型デバイス検証:
     - `result.StructuredDeviceData["M000"].DeviceName == "M000"`
     - `result.StructuredDeviceData["M000"].DataType == DeviceDataType.Bit`
     - `result.StructuredDeviceData["M000"].Value == true`
   - ワード型デバイス検証:
     - `result.StructuredDeviceData["D100"].DeviceName == "D100"`
     - `result.StructuredDeviceData["D100"].DataType == DeviceDataType.Word`
     - `result.StructuredDeviceData["D100"].Value == 0x1234`
   - DWord型デバイス検証:
     - `result.StructuredDeviceData["D000_DWord"].DeviceName == "D000"`
     - `result.StructuredDeviceData["D000_DWord"].DataType == DeviceDataType.DWord`
     - `result.StructuredDeviceData["D000_DWord"].Value == 0x56781234`

4. **エラー情報検証**:
   - `result.ParseErrors.Count == 0`

5. **継承データ保持検証**:
   - `result.RawDataHex == processedData.RawDataHex`
   - `result.BasicProcessedData == processedData.BasicProcessedData`
   - `result.CombinedDWordData == processedData.CombinedDWordData`

---

## 7. DIコンテナ設定

### サービスライフタイム
- **PlcCommunicationManager**: Transient（PLC別インスタンス）
- **ConfigToFrameManager**: Transient（設定別インスタンス）
- **LoggingManager**: Singleton（共有リソース）
- **ErrorHandler**: Singleton（共有リソース）

### インターフェース登録
```csharp
services.AddTransient<IPlcCommunicationManager, PlcCommunicationManager>();
services.AddTransient<IConfigToFrameManager, ConfigToFrameManager>();
services.AddSingleton<ILoggingManager, LoggingManager>();
services.AddSingleton<IErrorHandler, ErrorHandler>();
```

---

## 8. エラーハンドリング

### ParseRawToStructuredData スロー例外（TC038では発生しない）
- **InvalidOperationException**: DWord結合未実行状態での呼び出し
  - メッセージ: "DWord結合処理が未実行です。CombineDwordData()を先に実行してください"
- **DataProcessingException**: フレーム解析失敗
  - メッセージ: "4Eフレーム解析に失敗しました: {詳細}"
- **ArgumentNullException**: null入力
  - メッセージ: "ProcessedResponseDataまたはProcessedDeviceRequestInfoがnullです"

### エラー終了コード処理（TC041で検証）
- **EndCode != 0x0000**: SLMP応答エラー
  - IsSuccess = false
  - ErrorDescription = エラーコードに応じた説明
  - ParseErrorsにエラー詳細を記録
  - 例外はスローせず、エラー情報を含むStructuredDataを返却

### エラーメッセージ統一
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    // データ処理関連
    public const string DWordCombineNotExecuted = "DWord結合処理が未実行です。CombineDwordData()を先に実行してください";
    public const string FrameParsingFailed = "{0}フレーム解析に失敗しました: {1}";
    public const string InvalidFrameVersion = "不正なフレームバージョンです: {0}（有効値: 3E, 4E）";

    // 入力検証
    public const string NullProcessedData = "ProcessedResponseDataまたはProcessedDeviceRequestInfoがnullです";
}
```

---

## 9. ログ出力要件

### LoggingManager連携
- **構造化開始ログ**: フレーム形式（3E/4E）、デバイス数
- **構造化完了ログ**: 構造化デバイス数、処理時間、終了コード
- **エラーログ**: 解析失敗詳細、スタックトレース（TC038では発生しない）

### ログレベル
- **Information**: 構造化開始・完了
- **Debug**: フレーム解析詳細、デバイス別データ
- **Error**: 解析失敗時（TC038では発生しない）

### ログ出力例
```
[Information] 4Eフレーム構造化解析開始: デバイス数=1000, フレーム形式=4E
[Debug] 4Eサブヘッダ解析: SubHeader=54001234000000, EndCode=0x0000
[Debug] デバイス別構造化: M000=true (Bit), D100=0x1234 (Word), D000_DWord=0x56781234 (DWord)
[Information] 4Eフレーム構造化解析完了: 構造化デバイス数=1000, 処理時間=15ms
```

---

## 10. テスト実装チェックリスト

### TC038実装前
- [ ] PlcCommunicationManagerクラス作成（TC037で完了）
- [ ] IPlcCommunicationManagerインターフェース作成（TC037で完了）
- [ ] ParseRawToStructuredDataメソッドシグネチャ定義（TC037で完了）
- [ ] StructuredDataモデル作成（TC037で完了）
- [ ] SlmpFrameAnalysisモデル作成（TC037で完了）
- [ ] StructuredDeviceモデル作成（TC037で完了）
- [ ] DeviceDataType列挙型作成（TC037で完了）

### TC038実装中
- [ ] Arrange: ProcessedResponseData準備（DWord結合済み）
- [ ] Arrange: ProcessedDeviceRequestInfo準備（4Eフレーム設定）
- [ ] Act: ParseRawToStructuredData呼び出し（4Eフレーム）
- [ ] Assert: 構造化成功検証
- [ ] Assert: SLMPフレーム解析情報検証（4Eフレーム特有項目）
- [ ] Assert: 構造化デバイスデータ検証（Bit/Word/DWord）
- [ ] Assert: エラー情報検証（空リスト）
- [ ] Assert: 継承データ保持検証

### TC038実装後
- [ ] テスト実行・Red確認
- [ ] ParseRawToStructuredData本体実装（4Eフレーム解析ロジック追加）
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施
- [ ] TC039（DWord結合済みデータ解析）への準備

---

## 11. 参考情報

### 4Eフレーム仕様
- プロトコル: SLMP（Seamless Message Protocol）
- フレーム形式: 4Eフレーム（拡張形式）
- サブヘッダ: 54H 00H 12H 34H 00H 00H 00H 00H（7バイト）
- 用途: 拡張機能を持つPLC（Q00UDPCPU等）との通信

**重要**: TC038ではモックデータを使用、実機PLC不要

### テストデータサンプル
**配置先**: Tests/TestUtilities/TestData/PlcResponseSamples/

- 4E_FrameResponse_M_Success.json: M機器4Eフレーム正常応答サンプル
- 4E_FrameResponse_D_Success.json: D機器4Eフレーム正常応答サンプル
- 4E_StructuredData_Expected.json: 4Eフレーム構造化データ期待値サンプル

---

## 12. PySLMPClient実装参考情報

### 4Eフレーム解析実装（Python実装例）

#### PySLMPClientでの4Eフレーム解析
```python
def parse_4e_frame(response_hex):
    """4Eフレーム応答を解析"""
    # サブヘッダ解析（7バイト）
    sub_header = response_hex[0:14]  # "54001234000000"

    if sub_header != "54001234000000":
        raise ValueError(f"Invalid 4E frame header: {sub_header}")

    # 終了コード解析（2バイト）
    end_code_hex = response_hex[14:18]  # "0000"
    end_code = int(end_code_hex, 16)

    if end_code != 0x0000:
        raise ValueError(f"SLMP error code: 0x{end_code:04X}")

    # データ部解析
    data_part = response_hex[18:]

    return {
        'frame_version': '4E',
        'sub_header': sub_header,
        'end_code': end_code,
        'is_success': end_code == 0x0000,
        'data': data_part
    }
```

#### C#実装例（TC038対応）
```csharp
public async Task<StructuredData> ParseRawToStructuredData(
    ProcessedResponseData processedData,
    ProcessedDeviceRequestInfo deviceRequestInfo)
{
    // DWord結合済み状態チェック
    if (!processedData.IsDwordCombined)
    {
        throw new InvalidOperationException(ErrorMessages.DWordCombineNotExecuted);
    }

    // フレーム形式判定
    var frameVersion = deviceRequestInfo.SlmpFrameFormat;

    if (frameVersion == "4E")
    {
        return await Parse4EFrame(processedData, deviceRequestInfo);
    }
    else if (frameVersion == "3E")
    {
        return await Parse3EFrame(processedData, deviceRequestInfo);
    }
    else
    {
        throw new ArgumentException(
            string.Format(ErrorMessages.InvalidFrameVersion, frameVersion));
    }
}

private async Task<StructuredData> Parse4EFrame(
    ProcessedResponseData processedData,
    ProcessedDeviceRequestInfo deviceRequestInfo)
{
    var structuredData = new StructuredData();

    // ProcessedResponseDataから継承
    structuredData.RawDataHex = processedData.RawDataHex;
    structuredData.BasicProcessedData = processedData.BasicProcessedData;
    structuredData.CombinedDWordData = processedData.CombinedDWordData;

    // 4Eフレーム解析
    var frameAnalysis = new SlmpFrameAnalysis
    {
        FrameVersion = "4E",
        SubHeader = "54001234000000",  // 実際は応答データから抽出
        EndCode = 0x0000,  // 実際は応答データから抽出
        IsSuccess = true
    };

    structuredData.SlmpFrameInfo = frameAnalysis;

    // デバイス別構造化
    structuredData.StructuredDeviceData = new Dictionary<string, StructuredDevice>();

    // ビット型デバイス構造化
    foreach (var kvp in processedData.BasicProcessedData
        .Where(x => x.Key.StartsWith("M")))
    {
        structuredData.StructuredDeviceData[kvp.Key] = new StructuredDevice
        {
            DeviceName = kvp.Key,
            DataType = DeviceDataType.Bit,
            Value = kvp.Value,
            Metadata = new Dictionary<string, object>
            {
                { "Timestamp", DateTime.UtcNow },
                { "DataSource", "PlcCommunicationManager" }
            }
        };
    }

    // ワード型デバイス構造化
    foreach (var kvp in processedData.BasicProcessedData
        .Where(x => x.Key.StartsWith("D") && !x.Key.Contains("_DWord")))
    {
        structuredData.StructuredDeviceData[kvp.Key] = new StructuredDevice
        {
            DeviceName = kvp.Key,
            DataType = DeviceDataType.Word,
            Value = kvp.Value,
            Metadata = new Dictionary<string, object>
            {
                { "Timestamp", DateTime.UtcNow },
                { "DataSource", "PlcCommunicationManager" }
            }
        };
    }

    // DWord型デバイス構造化
    foreach (var kvp in processedData.CombinedDWordData)
    {
        var deviceName = kvp.Key.Replace("_DWord", "");
        structuredData.StructuredDeviceData[kvp.Key] = new StructuredDevice
        {
            DeviceName = deviceName,
            DataType = DeviceDataType.DWord,
            Value = kvp.Value,
            Metadata = new Dictionary<string, object>
            {
                { "Timestamp", DateTime.UtcNow },
                { "DataSource", "PlcCommunicationManager" },
                { "IsCombined", true }
            }
        };
    }

    // 統計情報更新
    structuredData.StructuredDeviceCount = structuredData.StructuredDeviceData.Count;
    structuredData.ParsedAt = DateTime.UtcNow;
    structuredData.ParseErrors = new List<string>();

    return structuredData;
}
```

### 実装時の重要ポイント

1. **4Eサブヘッダ検証**: "54001234000000"（7バイト）を正確に抽出・検証
2. **終了コード判定**: 0x0000（正常）とその他（エラー）の厳密な区別
3. **データ型別処理**: Bit/Word/DWordの統一的な構造化処理
4. **メタデータ付加**: タイムスタンプ、データソース等の自動付加
5. **継承データ保持**: ProcessedResponseDataの全情報を確実に継承

---

以上が TC038_ParseRawToStructuredData_4Eフレーム解析テスト実装に必要な情報です。
