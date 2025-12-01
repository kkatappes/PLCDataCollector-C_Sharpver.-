# Step6 CombineDwordData テスト実装用情報（TC032）

## ドキュメント概要

### 目的
このドキュメントは、TC032_CombineDwordData_DWord結合処理成功テストの実装に必要な情報を集約したものです。
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

### CombineDwordData（Step6-2: DWord結合処理）
**クラス**: PlcCommunicationManager
**名前空間**: andon.Core.Managers

#### Input
- BasicProcessedResponseData（基本後処理結果データ）
  - Step6-1（ProcessReceivedRawData）で生成された基本処理済みデータ
  - DWord結合対象のワード型デバイスを含む
- ProcessedDeviceRequestInfo（前処理済みデバイス要求情報）
  - ConfigToFrameManager.SplitDwordToWord()で生成された分割情報
  - DWord分割範囲情報（SplitRanges）を含む

#### Output
- ProcessedResponseData（DWord結合後処理結果オブジェクト）
  - 継承結果:
    - BasicProcessedResponseData全プロパティ（元データ保持）
  - 拡張結果:
    - CombinedDWordData: DWord結合済みデータ（Dictionary<string, uint>）
    - CombinedAt: DWord結合処理時刻（DateTime）
    - CombinedDeviceCount: 結合済みDWordデバイス数（int）
  - エラー情報:
    - DWordCombineErrors: DWord結合固有エラー（List<string>）

#### 機能
- DWord結合対象デバイスの特定（SplitRanges参照）
- 16ビットワード → 32ビットDWordの結合処理
  - 下位ワード（Low）+ 上位ワード（High）→ DWord値
  - リトルエンディアン形式での結合
- 結合不要デバイスの処理スキップ
- エラー検証・記録（ワードペア不整合、結合失敗）

#### データ取得元
- PlcCommunicationManager.ProcessReceivedRawData()（基本処理済みデータ）
- ConfigToFrameManager.SplitDwordToWord()（DWord分割情報）

#### 重要な処理内容
**DWord結合ロジック**:
- DWord分割情報に基づく結合対象特定
- ワードペア（Low/High）の正確なマッピング
- 32ビット値の正確な計算: `(High << 16) | Low`

**結合対象判定**:
- SplitRanges内に存在するデバイス範囲のみ結合
- 結合不要デバイスは元のワード値を保持
- 欠損ワードの検出とエラー処理

---

## 2. テストケース仕様（TC032）

### TC032_CombineDwordData_DWord結合処理成功
**目的**: DWord分割されたワード型デバイスの結合処理機能をテスト

#### 前提条件
- ProcessReceivedRawDataが成功済み（Step6-1完了）
- BasicProcessedResponseDataが正常に生成済み
- ProcessedDeviceRequestInfoにDWord分割情報が設定済み
- DWord結合対象ワード型デバイスが基本処理済みデータに存在

#### 入力データ
**BasicProcessedResponseData（基本処理済みデータ）**:
- 元の32ビットDWordデバイス（D000-D009）を16ビット単位に分割した20個のワードデバイス:
  - "D000" → 0x1234 (Low)  ← D000 DWordの下位
  - "D001" → 0x5678 (High) ← D000 DWordの上位
  - "D002" → 0x9ABC (Low)  ← D001 DWordの下位
  - "D003" → 0xDEF0 (High) ← D001 DWordの上位
  - ...
  - "D018" → 0x1122 (Low)  ← D009 DWordの下位
  - "D019" → 0x3344 (High) ← D009 DWordの上位

**ProcessedDeviceRequestInfo（DWord分割情報）**:
- SplitRanges（DWord分割情報）:
  - "D000_DWord" → ["D000", "D001"] （D000 DWord → D000(Low), D001(High)）
  - "D001_DWord" → ["D002", "D003"] （D001 DWord → D002(Low), D003(High)）
  - "D002_DWord" → ["D004", "D005"] （D002 DWord → D004(Low), D005(High)）
  - ...
  - "D009_DWord" → ["D018", "D019"] （D009 DWord → D018(Low), D019(High)）

#### 期待出力
- ProcessedResponseData（DWord結合後処理結果）
  - BasicProcessedResponseData全プロパティ（継承）
  - CombinedDWordData:
    - "D000_DWord" → 0x56781234 (0x5678 << 16 | 0x1234)
    - "D001_DWord" → 0xDEF09ABC (0xDEF0 << 16 | 0x9ABC)
    - "D002_DWord" → 結合済みDWord値
    - ...
    - "D009_DWord" → 0x33441122 (0x3344 << 16 | 0x1122)
  - CombinedAt: DWord結合処理完了時刻
  - CombinedDeviceCount: 10（D000_DWord～D009_DWordの10個）
  - DWordCombineErrors: [] (空リスト、エラーなし)

#### 検証項目
1. **DWord結合精度**:
   - 32ビット計算の正確性: `(High << 16) | Low`
   - リトルエンディアン処理の正確性
   - 数値オーバーフロー回避

2. **ワードペアマッピング**:
   - SplitRanges参照によるペア特定の正確性
   - Low/Highワードの正確な対応付け
   - 欠損ワード検出（片方のワードが存在しない場合）

3. **デバイス名生成**:
   - 元のDWordデバイス名の正確な復元
   - "D000_DWord", "D001_DWord", ... の命名規則準拠

4. **結合判定ロジック**:
   - SplitRanges内デバイスのみ結合処理
   - 結合対象外デバイスの処理スキップ
   - 混在データ型での適切な処理分岐

5. **エラーハンドリング**:
   - ワードペア不整合の検出
   - 無効なDWord分割情報の検出
   - エラー情報の適切な記録

6. **統計情報の正確性**:
   - CombinedDeviceCount = 10（結合済みDWord数）
   - 処理時刻の記録精度

---

## 3. DWord結合ロジック詳細

### 32ビットDWord結合計算
**計算式**:
```csharp
uint dwordValue = (uint)((highWord << 16) | lowWord);
```

**結合例**:
```
元データ:
- D000 (Low): 0x1234
- D001 (High): 0x5678

結合計算:
1. High << 16: 0x5678 << 16 = 0x56780000
2. Low: 0x1234
3. OR演算: 0x56780000 | 0x1234 = 0x56781234

結果: D000_DWord = 0x56781234 (1450744372 decimal)
```

### エンディアン処理
**リトルエンディアン準拠**:
- PLCからの受信データはリトルエンディアン
- ワード内バイト順序: 下位バイト → 上位バイト
- DWord結合時も同様の順序を維持

### 分割情報マッピング
**SplitRanges構造例**:
```csharp
SplitRanges = {
    "D000_DWord" => ["D000", "D001"],  // 元D000 → D000(Low), D001(High)
    "D001_DWord" => ["D002", "D003"],  // 元D001 → D002(Low), D003(High)
    "D002_DWord" => ["D004", "D005"],  // 元D002 → D004(Low), D005(High)
}
```

---

## 4. 依存クラス・設定

### BasicProcessedResponseData（入力データ型）
**取得元**: ProcessReceivedRawData（Step6-1）

```csharp
public class BasicProcessedResponseData
{
    // 基本結果
    public string OriginalRawData { get; set; }                 // 元生データ
    public Dictionary<string, object> BasicProcessedData { get; set; }  // 処理済みデータ
    public DateTime ProcessedAt { get; set; }                    // 処理時刻

    // エラー情報
    public bool HasErrors { get; set; }                          // エラーフラグ
    public List<string> Errors { get; set; }                     // エラーメッセージ
    public List<string> Warnings { get; set; }                   // 警告メッセージ

    // 統計情報
    public int ProcessedDeviceCount { get; set; }                // 処理デバイス数

    // メソッド
    public object GetDeviceValue(string deviceName);
}
```

### ProcessedResponseData（出力データ型）
**本メソッドの出力データ型**

```csharp
public class ProcessedResponseData : BasicProcessedResponseData
{
    // DWord結合結果
    public Dictionary<string, uint> CombinedDWordData { get; set; }      // DWord結合済みデータ
    public DateTime CombinedAt { get; set; }                             // 結合処理時刻
    public int CombinedDeviceCount { get; set; }                         // 結合済みDWordデバイス数

    // DWord結合固有エラー
    public List<string> DWordCombineErrors { get; set; }                 // 結合固有エラー

    // メソッド
    public void MergeFromBasicData(BasicProcessedResponseData basicData);
    public uint GetCombinedDWordValue(string dwordDeviceName);
    public List<string> GetCombinedDWordDevices();
    public void AddCombinedDWord(string deviceName, uint value);
    public void AddDWordCombineError(string errorMessage);
}
```

### ProcessedDeviceRequestInfo（DWord分割情報）
**取得元**: ConfigToFrameManager.SplitDwordToWord()

```csharp
public class ProcessedDeviceRequestInfo
{
    // DWord分割情報（Dictionary<元DWordデバイス名, 分割後ワードデバイス名リスト>）
    public Dictionary<string, List<string>> SplitRanges { get; set; }

    // データ型情報
    public Dictionary<string, string> DataTypeInfo { get; set; }

    // メソッド
    public List<string> GetSplitWordsForDWord(string dwordDeviceName);
    public bool IsDWordSplitTarget(string deviceName);
    public string GetOriginalDWordName(string wordDeviceName);
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
2. **TC032_CombineDwordData_DWord結合処理成功**（本テスト、最優先）
3. 異常系・拡張テスト（次フェーズ）
   - TC033: 結合不要データ処理
   - TC035: 基本後処理未実行エラー
   - TC036: DWord結合エラー

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockPlcCommunicationManager**: PlcCommunicationManager全体のモック
- **MockConfigToFrameManager**: DWord分割情報生成のモック

#### 使用するスタブ
- **BasicProcessedResponseDataStubs**: 基本処理済みデータのスタブ（DWord結合対象ワード含む）
- **ProcessedDeviceRequestInfoStubs**: DWord分割情報のスタブ
- **ProcessedResponseDataValidator**: DWord結合結果検証用ヘルパー

---

## 6. テストケース実装構造

### Arrange（準備）
1. **BasicProcessedResponseData準備**:
   - D000-D019ワード型デバイス（20個）
   - 各ワードに期待値設定（DWord結合テスト用の値）

2. **ProcessedDeviceRequestInfo準備**:
   - SplitRanges設定:
     - "D000_DWord" → ["D000", "D001"]
     - "D001_DWord" → ["D002", "D003"]
     - ...
     - "D009_DWord" → ["D018", "D019"]

3. **期待DWord結果準備**:
   - 各DWordデバイスの期待値計算
   - 結合後の32ビット値

4. **PlcCommunicationManagerインスタンス作成**:
   - 必要な依存関係設定

### Act（実行）
1. CombineDwordData実行:
   ```csharp
   var result = await plcCommManager.CombineDwordData(
       basicProcessedData,           // 基本処理済みデータ
       processedDeviceRequestInfo    // DWord分割情報
   );
   ```

### Assert（検証）
1. **継承プロパティ検証**:
   - `result.OriginalRawData == basicProcessedData.OriginalRawData`
   - `result.BasicProcessedData`が適切に継承
   - `result.ProcessedAt == basicProcessedData.ProcessedAt`

2. **DWord結合結果検証**:
   - `result.CombinedDWordData["D000_DWord"] == 0x56781234`
   - `result.CombinedDWordData["D001_DWord"] == 0xDEF09ABC`
   - 全DWordデバイス（10個）の値確認

3. **統計情報検証**:
   - `result.CombinedDeviceCount == 10`（DWord結合済み数）
   - `result.CombinedAt`が実行時刻に近い

4. **エラー情報検証**:
   - `result.DWordCombineErrors.Count == 0`（エラーなし）
   - `result.HasErrors == false`（全体エラーなし）

5. **結合精度検証**:
   - 32ビット計算の正確性確認
   - リトルエンディアン処理の正確性確認

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

### CombineDwordData スロー例外
- **DataProcessingException**: DWord結合処理エラー
  - ワードペア不整合（Low/Highの片方が欠損）
  - 無効な分割情報
  - 数値変換エラー
- **ArgumentException**: 不正な引数
  - BasicProcessedResponseDataがnull
  - ProcessedDeviceRequestInfoがnull
  - SplitRangesが空または無効
- **InvalidOperationException**: 無効な操作
  - 基本処理未実行状態
  - 結合対象ワードデバイス不足

### エラーメッセージ統一
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    // DWord結合エラー
    public const string BasicProcessingNotCompleted = "基本後処理が完了していません。";
    public const string DWordSplitInfoMissing = "DWord分割情報が不足しています: {0}";
    public const string WordPairIncomplete = "ワードペアが不完全です。Low: {0}, High: {1}";
    public const string DWordCombineFailed = "DWord結合に失敗しました: {0}";

    // 引数エラー
    public const string BasicProcessedDataNull = "基本処理済みデータ（BasicProcessedResponseData）がnullです。";
    public const string SplitRangesEmpty = "DWord分割情報（SplitRanges）が空です。";
}
```

---

## 9. ログ出力要件

### LoggingManager連携
- 処理開始ログ: DWord結合対象数、分割情報
- 処理完了ログ: 結合済みDWord数、所要時間
- エラーログ: 結合失敗詳細、ワードペア情報
- デバッグログ: 結合計算詳細、中間値

### ログレベル
- **Information**: 処理開始・完了
- **Debug**: 結合計算詳細、中間値記録
- **Warning**: ワードペア警告
- **Error**: 結合失敗時

---

## 10. テスト実装チェックリスト

### TC032実装前
- [ ] PlcCommunicationManagerクラス拡張
- [ ] CombineDwordDataメソッドシグネチャ定義
- [ ] ProcessedResponseDataモデル作成
- [ ] BasicProcessedResponseData継承関係設定
- [ ] DWord結合ロジック設計

### TC032実装中
- [ ] Arrange: BasicProcessedResponseData準備
- [ ] Arrange: ProcessedDeviceRequestInfo準備（DWord分割情報）
- [ ] Arrange: 期待DWord結果準備
- [ ] Act: CombineDwordData呼び出し
- [ ] Assert: 継承プロパティ検証
- [ ] Assert: DWord結合結果検証（D000_DWord～D009_DWord）
- [ ] Assert: 統計情報検証

### TC032実装後
- [ ] テスト実行・Red確認
- [ ] CombineDwordData本体実装（最小実装）
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施
- [ ] TC029（基本後処理）との連携確認

---

## 11. 参考情報

### DWord計算例
**実際の計算例（10DWordデバイス）**:
```
D000_DWord: (0x5678 << 16) | 0x1234 = 0x56781234 = 1450744372
D001_DWord: (0xDEF0 << 16) | 0x9ABC = 0xDEF09ABC = 3740340924
D002_DWord: (0x2468 << 16) | 0x1357 = 0x24681357 = 610317143
...
D009_DWord: (0x3344 << 16) | 0x1122 = 0x33441122 = 862253346
```

### テストデータサンプル
**配置先**: Tests/TestUtilities/TestData/DWordCombineSamples/

- BasicProcessedData_D000-D019.txt: DWord結合対象の基本処理済みデータ
- SplitRanges_D000-D009.txt: DWord分割情報サンプル
- ExpectedDWordResults_D000-D009.txt: 期待DWord結合結果

---

以上が TC032_CombineDwordData_DWord結合処理成功テスト実装に必要な情報です。