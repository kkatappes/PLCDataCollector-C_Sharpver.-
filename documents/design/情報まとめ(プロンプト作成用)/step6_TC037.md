# Step6 ParseRawToStructuredData テスト実装用情報（TC037）

## ドキュメント概要

### 目的
このドキュメントは、TC037_ParseRawToStructuredData_3Eフレーム解析テストの実装に必要な情報を集約したものです。
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
- SLMP応答フレーム構造解析（3E/4Eフレーム対応）
- デバイス別構造化データ生成
  - デバイス名、データ型、値、メタデータの統合
  - ビット・ワード・DWordの統一インターフェース
- SLMP終了コード解析・エラー判定
- フレーム整合性検証（ヘッダ、データ長、チェックサム等）

#### データ取得元
- PlcCommunicationManager.CombineDwordData()（DWord結合済みデータ）
- ConfigToFrameManager（フレーム解析情報）

#### 重要な処理内容
**3Eフレーム解析**:
- 3Eフレーム構造（簡易形式）の解析
- ヘッダ情報抽出、終了コード判定
- データ部の構造化処理

**構造化データ生成**:
- デバイス別メタデータ付加
- 統一されたデータアクセスインターフェース
- エラー・警告情報の統合

---

## 2. テストケース仕様（TC037）

### TC037_ParseRawToStructuredData_3Eフレーム解析
**目的**: 3EフレームSLMP応答の構造化解析機能をテスト

#### 前提条件
- CombineDwordDataが成功済み（Step6-2完了）
- ProcessedResponseDataが正常に生成済み
- SLMP 3Eフレーム形式の応答データが処理済み
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
- SlmpFrameFormat: "3E" （3Eフレーム形式）
- ResponseFrameStructure:
  - FrameType: "3E_Response"
  - SubHeader: "D000"
  - EndCode: "0000" （正常終了）
  - DataLength: 実際のデータ長
- DeviceLayoutInfo: デバイス配置・順序情報

#### 期待出力
- StructuredData（最終構造化データ）
  - ProcessedResponseData全プロパティ（継承）
  - StructuredDeviceData:
    - "M000" → StructuredDevice { Name="M000", Type="Bit", Value=true, Address=0x90000000, ... }
    - "D100" → StructuredDevice { Name="D100", Type="Word", Value=0x1234, Address=0xA8000064, ... }
    - "D000_DWord" → StructuredDevice { Name="D000", Type="DWord", Value=0x56781234, Address=0xA8000000, ... }
  - ParsedAt: 構造化解析完了時刻
  - StructuredDeviceCount: 構造化済みデバイス総数
  - SlmpFrameInfo:
    - FrameType: "3E_Response"
    - EndCode: 0x0000
    - EndCodeDescription: "正常終了"
    - DataLength: 実際のバイト長
    - SubHeader: "D000"
  - ParseErrors: [] (空リスト、エラーなし)

#### 検証項目
1. **3Eフレーム解析精度**:
   - サブヘッダ（"D000"）の正確な抽出
   - 終了コード（"0000"）の正確な解析
   - データ長の整合性確認

2. **構造化データ生成**:
   - StructuredDevice オブジェクトの正確な生成
   - デバイス名、データ型、値の正確なマッピング
   - メタデータ（アドレス、サイズ等）の正確な付加

3. **統一インターフェース**:
   - ビット・ワード・DWordの統一アクセス
   - データ型透過的な値取得
   - エラー・警告情報の統合表示

4. **フレーム整合性検証**:
   - ヘッダ情報の妥当性確認
   - データ長の計算値vs実際値の整合性
   - 終了コードによるエラー判定

5. **エラーハンドリング**:
   - 不正なフレーム構造の検出
   - データ欠損・不整合の検出
   - エラー情報の適切な記録

6. **統計情報の正確性**:
   - StructuredDeviceCount = 全デバイス数（ビット+ワード+DWord）
   - 処理時刻の記録精度

---

## 3. 3Eフレーム構造解析詳細

### 3Eフレーム/ASCIIフォーマット応答構造
**実機テスト設定**: 一部PLCで使用される簡易形式

#### 3E応答フレーム構成
```
応答フレーム（ASCII形式）:
[サブヘッダ] + [終了コード] + [デバイスデータ]

各フィールド:
- サブヘッダ: "D000" (3E応答フレーム識別、2バイト)
- 終了コード: "0000" (正常終了) / エラーコード（2バイト）
- デバイスデータ: 実際のデバイス値（可変長）
```

#### 3E M000-M999ビット読み出し応答例
```
ASCII形式（実送信）:
"D0000000" + [デバイスデータ250文字]

フィールド詳細:
- サブヘッダ: D000 (3E応答識別)
- 終了コード: 0000 (正常終了)
- デバイスデータ: 1000ビット = 125バイト = 250文字（ASCII）

データ部例:
"000102030405060708090A0B0C0D0E0F..." (250文字)
解釈: M000=0, M001=0, M002=0, M003=0, M004=1, M005=0, M006=1, M007=0...
```

#### 3E D000-D099ワード読み出し応答例
```
ASCII形式（実送信）:
"D0000000" + [デバイスデータ200文字]

フィールド詳細:
- サブヘッダ: D000 (3E応答識別)
- 終了コード: 0000 (正常終了)
- デバイスデータ: 100ワード × 2バイト = 200バイト = 400文字（ASCII）

データ部例:
"34127856AB03EF12..." (400文字)
解釈: D000=0x1234, D001=0x5678, D002=0x03AB, D003=0x12EF...
```

### 終了コード解析
**正常終了**:
- "0000": 正常終了

**エラーコード例**:
- "C059": 指定デバイス範囲外
- "C05C": デバイス指定エラー
- "C061": データ長エラー

---

## 4. 依存クラス・設定

### ProcessedResponseData（入力データ型）
**取得元**: CombineDwordData（Step6-2）

```csharp
public class ProcessedResponseData : BasicProcessedResponseData
{
    // 基本データ（継承）
    public Dictionary<string, object> BasicProcessedData { get; set; }

    // DWord結合データ
    public Dictionary<string, uint> CombinedDWordData { get; set; }
    public DateTime CombinedAt { get; set; }
    public int CombinedDeviceCount { get; set; }

    // メソッド
    public object GetDeviceValue(string deviceName);
    public uint GetCombinedDWordValue(string dwordDeviceName);
}
```

### StructuredData（出力データ型）
**本メソッドの出力データ型**

```csharp
public class StructuredData : ProcessedResponseData
{
    // 構造化データ
    public Dictionary<string, StructuredDevice> StructuredDeviceData { get; set; }
    public DateTime ParsedAt { get; set; }
    public int StructuredDeviceCount { get; set; }

    // SLMP解析情報
    public SlmpFrameAnalysis SlmpFrameInfo { get; set; }

    // 構造化解析固有エラー
    public List<string> ParseErrors { get; set; }

    // メソッド
    public void AddStructuredDevice(string deviceName, StructuredDevice device);
    public StructuredDevice GetStructuredDevice(string deviceName);
    public List<StructuredDevice> GetDevicesByType(string deviceType);
    public void AddParseError(string errorMessage);
    public void SetSlmpFrameInfo(SlmpFrameAnalysis frameInfo);
}
```

### StructuredDevice（構造化デバイス情報）
**デバイス別構造化データ**

```csharp
public class StructuredDevice
{
    // 基本情報
    public string Name { get; set; }                    // デバイス名 ("M000", "D100", "D000_DWord")
    public string Type { get; set; }                    // データ型 ("Bit", "Word", "DWord")
    public object Value { get; set; }                   // デバイス値
    public DateTime UpdatedAt { get; set; }             // 更新時刻

    // メタデータ
    public uint Address { get; set; }                   // デバイスアドレス
    public int Size { get; set; }                       // データサイズ（ビット/バイト）
    public string Description { get; set; }             // デバイス説明

    // SLMP情報
    public byte DeviceCode { get; set; }                // SLMPデバイスコード
    public int DeviceNumber { get; set; }               // デバイス番号

    // 品質情報
    public bool IsValid { get; set; }                   // データ有効性
    public string QualityInfo { get; set; }             // 品質情報
}
```

### SlmpFrameAnalysis（SLMPフレーム解析情報）
**フレーム解析結果**

```csharp
public class SlmpFrameAnalysis
{
    // フレーム基本情報
    public string FrameType { get; set; }               // "3E_Response", "4E_Response"
    public string SubHeader { get; set; }               // サブヘッダ
    public ushort EndCode { get; set; }                 // 終了コード
    public string EndCodeDescription { get; set; }      // 終了コード説明
    public int DataLength { get; set; }                 // データ長

    // 解析統計
    public DateTime AnalyzedAt { get; set; }            // 解析時刻
    public bool IsValidFrame { get; set; }              // フレーム妥当性
    public List<string> ValidationWarnings { get; set; } // 検証警告
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
1. **TC029_ProcessReceivedRawData_基本後処理成功**（前提テスト）
2. **TC032_CombineDwordData_DWord結合処理成功**（前提テスト）
3. **TC037_ParseRawToStructuredData_3Eフレーム解析**（本テスト、最優先）
4. 拡張・異常系テスト（次フェーズ）
   - TC038: 4Eフレーム解析
   - TC040: DWord結合未実行エラー
   - TC041: エラー終了コード処理

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockPlcCommunicationManager**: PlcCommunicationManager全体のモック
- **MockConfigToFrameManager**: フレーム解析情報生成のモック

#### 使用するスタブ
- **ProcessedResponseDataStubs**: DWord結合済みデータのスタブ
- **Slmp3EFrameStubs**: 3Eフレーム応答データのスタブ
- **StructuredDataValidator**: 構造化データ検証用ヘルパー

---

## 6. テストケース実装構造

### Arrange（準備）
1. **ProcessedResponseData準備**:
   - BasicProcessedData: ビット・ワード単位データ
   - CombinedDWordData: DWord結合済みデータ

2. **ProcessedDeviceRequestInfo準備**:
   - SlmpFrameFormat: "3E"
   - ResponseFrameStructure: 3Eフレーム情報
   - DeviceLayoutInfo: デバイス配置情報

3. **期待構造化結果準備**:
   - 各デバイスのStructuredDevice期待値
   - SlmpFrameAnalysis期待値

4. **PlcCommunicationManagerインスタンス作成**:
   - 必要な依存関係設定

### Act（実行）
1. ParseRawToStructuredData実行:
   ```csharp
   var result = await plcCommManager.ParseRawToStructuredData(
       processedResponseData,        // DWord結合済みデータ
       processedDeviceRequestInfo    // フレーム解析情報
   );
   ```

### Assert（検証）
1. **継承プロパティ検証**:
   - `result.BasicProcessedData`が適切に継承
   - `result.CombinedDWordData`が適切に継承
   - `result.CombinedAt`等が適切に保持

2. **構造化データ検証**:
   - `result.StructuredDeviceData["M000"]`がStructuredDevice型
   - `result.StructuredDeviceData["M000"].Value == true`
   - `result.StructuredDeviceData["D100"].Value == 0x1234`
   - `result.StructuredDeviceData["D000_DWord"].Value == 0x56781234`

3. **SLMPフレーム情報検証**:
   - `result.SlmpFrameInfo.FrameType == "3E_Response"`
   - `result.SlmpFrameInfo.EndCode == 0x0000`
   - `result.SlmpFrameInfo.EndCodeDescription == "正常終了"`

4. **統計情報検証**:
   - `result.StructuredDeviceCount`が全デバイス数と一致
   - `result.ParsedAt`が実行時刻に近い

5. **エラー情報検証**:
   - `result.ParseErrors.Count == 0`（エラーなし）
   - `result.SlmpFrameInfo.IsValidFrame == true`

6. **メタデータ検証**:
   - StructuredDeviceのAddress、Size、DeviceCode等の正確性
   - UpdatedAt、QualityInfo等の適切な設定

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

### ParseRawToStructuredData スロー例外
- **SlmpFrameException**: SLMPフレーム解析エラー
  - 不正なフレーム構造
  - 終了コードエラー
  - ヘッダ情報不整合
- **DataStructuringException**: データ構造化エラー
  - デバイス情報不足
  - メタデータ生成失敗
  - 構造化処理失敗
- **ArgumentException**: 不正な引数
  - ProcessedResponseDataがnull
  - フレーム解析情報不足
- **InvalidOperationException**: 無効な操作
  - DWord結合未完了状態
  - 前段処理未実行

### エラーメッセージ統一
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    // フレーム解析エラー
    public const string InvalidSlmpFrame = "SLMPフレーム構造が不正です: {0}";
    public const string SlmpEndCodeError = "SLMP終了コードがエラーを示しています: {0} - {1}";
    public const string FrameDataLengthMismatch = "フレームデータ長が不整合です。期待: {0}, 実際: {1}";

    // 構造化エラー
    public const string DeviceStructuringFailed = "デバイス構造化に失敗しました: {0}";
    public const string MetadataGenerationFailed = "メタデータ生成に失敗しました: {0}";
    public const string DWordCombiningNotCompleted = "DWord結合処理が完了していません。";

    // 引数エラー
    public const string ProcessedResponseDataNull = "DWord結合済みデータ（ProcessedResponseData）がnullです。";
    public const string FrameAnalysisInfoMissing = "フレーム解析情報が不足しています。";
}
```

---

## 9. ログ出力要件

### LoggingManager連携
- 処理開始ログ: 構造化対象デバイス数、フレーム形式
- 処理完了ログ: 構造化済みデバイス数、所要時間
- エラーログ: フレーム解析失敗詳細、構造化エラー
- デバッグログ: 構造化処理詳細、メタデータ生成

### ログレベル
- **Information**: 処理開始・完了
- **Debug**: 構造化処理詳細、フレーム解析詳細
- **Warning**: フレーム妥当性警告
- **Error**: 解析・構造化失敗時

---

## 10. テスト実装チェックリスト

### TC037実装前
- [ ] PlcCommunicationManagerクラス拡張
- [ ] ParseRawToStructuredDataメソッドシグネチャ定義
- [ ] StructuredDataモデル作成
- [ ] StructuredDeviceモデル作成
- [ ] SlmpFrameAnalysisモデル作成
- [ ] 3Eフレーム解析ロジック設計

### TC037実装中
- [ ] Arrange: ProcessedResponseData準備
- [ ] Arrange: ProcessedDeviceRequestInfo準備（3Eフレーム解析情報）
- [ ] Arrange: 期待構造化結果準備
- [ ] Act: ParseRawToStructuredData呼び出し
- [ ] Assert: 継承プロパティ検証
- [ ] Assert: 構造化データ検証（全デバイス）
- [ ] Assert: SLMPフレーム情報検証
- [ ] Assert: 統計情報検証

### TC037実装後
- [ ] テスト実行・Red確認
- [ ] ParseRawToStructuredData本体実装（最小実装）
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施
- [ ] TC029, TC032との連携確認

---

## 11. 参考情報

### 3Eフレーム解析例
**実際の3E応答解析**:
```
受信フレーム: "D0000000340078......" (3E形式)

解析結果:
- SubHeader: "D000" (3E応答識別)
- EndCode: "0000" (0x0000, 正常終了)
- DataPart: "340078......" (実際のデバイス値)

構造化結果:
- SlmpFrameInfo.FrameType: "3E_Response"
- SlmpFrameInfo.EndCode: 0x0000
- SlmpFrameInfo.IsValidFrame: true
```

### デバイスアドレス計算
**SLMPデバイスアドレス**:
```
M機器: 0x90000000 + デバイス番号
D機器: 0xA8000000 + デバイス番号 × 2

例:
- M000: 0x90000000 + 0 = 0x90000000
- D100: 0xA8000000 + 100×2 = 0xA80000C8
```

### テストデータサンプル
**配置先**: Tests/TestUtilities/TestData/StructuredDataSamples/

- ProcessedResponseData_3EFrame.txt: 3Eフレーム用のDWord結合済みデータ
- Slmp3EFrameInfo.txt: 3Eフレーム解析情報
- ExpectedStructuredData_3EFrame.txt: 期待構造化データ結果

---

以上が TC037_ParseRawToStructuredData_3Eフレーム解析テスト実装に必要な情報です。