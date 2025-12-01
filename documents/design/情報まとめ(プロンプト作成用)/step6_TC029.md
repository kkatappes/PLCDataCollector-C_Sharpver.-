# Step6 ProcessReceivedRawData テスト実装用情報（TC029）

## ドキュメント概要

### 目的
このドキュメントは、TC029_ProcessReceivedRawData_基本後処理成功テストの実装に必要な情報を集約したものです。
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

### ProcessReceivedRawData（Step6-1: 受信データ基本後処理）
**クラス**: PlcCommunicationManager
**名前空間**: andon.Core.Managers

#### Input
- 受信生データ（16進数文字列形式）
  - Step4（ReceiveResponseAsync）で受信したPLCからの生データ
  - 単一データ型例: D000-D999（ワード型のみ）またはM000-M999（ビット型のみ）
- ProcessedDeviceRequestInfo（前処理済みデバイス要求情報）
  - ConfigToFrameManager.SplitDwordToWord()で生成された前処理情報
  - 単一データ型情報（ビットのみ/ワードのみ）

#### Output
- BasicProcessedResponseData（基本後処理結果オブジェクト）
  - 基本結果:
    - OriginalRawData: 元生データ（16進数文字列）
    - BasicProcessedData: 基本処理済みデータ（デバイス名キー構造、Dictionary<string, object>）
    - ProcessedAt: 処理時刻（DateTime）
  - エラー情報:
    - HasErrors: エラーフラグ（bool）
    - Errors: エラーメッセージリスト（List<string>）
    - Warnings: 警告メッセージリスト（List<string>）
  - 統計情報:
    - ProcessedDeviceCount: 処理デバイス数（int）

#### 機能
- 受信生データの16進数パース（16進数文字列 → バイナリデータ）
- デバイス別データ抽出（SLMPフレーム構造解析）
- 基本的な型変換（単一データ型での処理）
  - ビット型: 1ビット単位でのbool値変換
  - ワード型: 16ビット単位でのushort/int値変換
- エラー検証・記録（データ長整合性、フォーマット正常性）

#### データ取得元
- PlcCommunicationManager.ReceiveResponseAsync()（受信生データ）
- ConfigToFrameManager.SplitDwordToWord()（デバイス型情報）

#### 重要な処理内容
**DWord結合前状態の保持**:
- このメソッドではDWord結合処理は実行しない
- 16ビットワード単位での処理済みデータを生成
- DWord結合はStep6-2（CombineDwordData）で実行

**単一データ型での基本処理**:
- ビット型のみまたはワード型のみの処理
- 基本的なパース・変換機能の検証
- エラーハンドリングの基本動作確認

---

## 2. テストケース仕様（TC029）

### TC029_ProcessReceivedRawData_基本後処理成功
**目的**: 単一データ型（ワード型のみ）の受信データ基本処理機能をテスト

#### 前提条件
- ConnectAsyncが成功済み（接続状態: Connected）
- SendFrameAsync, ReceiveResponseAsyncが成功済み
- 受信生データが有効なSLMP応答形式
- ProcessedDeviceRequestInfo（前処理情報）が準備済み

#### 入力データ
**単一データ型の受信生データ（ワード型のみ）**:
- D000-D099（ワード型、100点）の応答データ
  - データ長: 200バイト（100ワード × 2バイト）
  - ASCII形式では400文字（200バイト × 2）
  - 例（先頭16バイト）: "34127856AB03EF1290AB5634CD12EF90"
  - 解釈: D000=0x1234, D001=0x5678, D002=0x03AB, D003=0x12EF...

**ProcessedDeviceRequestInfo（単一型要求情報）**:
- DDeviceRange: "D000-D099"
- DataTypeInfo:
  - "D000-D099" → "Word"
- SplitRanges: {} (空、Word型のためDWord分割なし)

#### 期待出力
- BasicProcessedResponseData（基本処理済みデータ）
  - OriginalRawData: 元生データ（入力データそのもの）
  - BasicProcessedData:
    - ワード型デバイス（D000-D099）:
      - "D000" → 0x1234（4660）
      - "D001" → 0x5678（22136）
      - "D002" → 0x03AB（939）
      - "D003" → 0x12EF（4847）
      - ...
      - "D099" → ushort値
  - ProcessedAt: 処理完了時刻
  - HasErrors: false（正常処理時）
  - ProcessedDeviceCount: 100（D000-D099の100点）

#### 検証項目
1. **データパース精度**:
   - 16進数文字列 → バイナリデータの変換精度
   - データ長計算の正確性（ワード型: 点数×2）

2. **ワード型処理の正確性**:
   - 16ビット単位でのushort値変換
   - エンディアン処理（リトルエンディアン）の正確性

3. **デバイス名マッピング**:
   - デバイスコード + 開始番号 → デバイス名の正確な生成
   - "D000", "D001", ..., "D099"

4. **エラーハンドリング**:
   - データ長整合性の確認（期待長 vs 実際長）
   - フォーマット正常性の確認
   - 範囲内デバイス番号の確認

5. **統計情報の正確性**:
   - ProcessedDeviceCount = 100（全デバイス数）
   - エラー・警告カウントの正確性

---

## 3. SLMP応答フレーム詳細

### 4Eフレーム/ASCIIフォーマット応答構造
**実機テスト設定**: Q00UDPCPUとの通信で使用

#### D000-D099ワード読み出し応答例
```
バイナリ形式（参考）:
D4 00 12 34 00 00 00 00  [ネットワーク情報7バイト]  [データ長4バイト]  00 00  [デバイスデータ200バイト]

ASCII形式（実送信）:
"D40012340000000100FFFFFFFF03FF000000C800000000" + [デバイスデータ400文字]

フィールド詳細:
- サブヘッダ: D4001234000000
- ネットワーク番号: 01
- 要求元局番: 00
- 要求元プロセッサ番号: FFFF (16進数表現)
- 要求元マルチドロップ: FF
- 要求先プロセッサ番号: 03FF
- 予備: 00
- データ長: 000000C8 (200バイト、リトルエンディアン)
- 終了コード: 0000 (正常終了)
- デバイスデータ: 100ワード × 2バイト = 200バイト = 400文字（ASCII）
```

#### エンディアン処理
**リトルエンディアン形式**:
- ワード値0x1234 → バイナリ: 34 12 → ASCII: "3412"
- ワード値0x5678 → バイナリ: 78 56 → ASCII: "7856"

**処理手順**:
1. ASCII文字列 → バイナリバイト配列変換
2. リトルエンディアンでの数値変換
3. デバイス名とのマッピング

---

## 4. 依存クラス・設定

### ProcessedDeviceRequestInfo（デバイス要求前処理情報）
**取得元**: ConfigToFrameManager.SplitDwordToWord()

```csharp
public class ProcessedDeviceRequestInfo
{
    // デバイス範囲情報
    public string DDeviceRange { get; set; }        // "D000-D099"

    // データ型情報（Dictionary<デバイス範囲, データ型>）
    public Dictionary<string, string> DataTypeInfo { get; set; }

    // DWord分割情報（Dictionary<元範囲, 分割後範囲リスト>）
    public Dictionary<string, List<string>> SplitRanges { get; set; }

    // 最適化範囲情報
    public Dictionary<string, string> OptimizedRanges { get; set; }

    // メソッド
    public void AddDataTypeInfo(string range, string dataType);
    public string GetDataType(string range);
    public List<string> GetAllDeviceTypes();
}
```

### BasicProcessedResponseData（基本後処理結果）
**本メソッドの出力データ型**

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
    public void AddProcessedDevice(string deviceName, object value, string dataType);
    public void AddError(string errorMessage);
    public void AddWarning(string warningMessage);
    public object GetDeviceValue(string deviceName);
    public string GetDeviceType(string deviceName);
}
```

### デバイスコード定義（SLMP仕様）
**取得元**: SLMP仕様書（pdf2img/sh080931q.pdf）、PySLMPClient/pyslmpclient/const.py

```csharp
public static class DeviceCode
{
    public const byte D = 0xA8;   // 168 (decimal) - データレジスタ（D機器）
    public const byte M = 0x90;   // 144 (decimal) - 内部リレー（M機器）
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
1. **TC029_ProcessReceivedRawData_基本後処理成功**（本テスト、最優先）
   - 単一データ型（ワード型のみ）の基本処理
2. **TC030_ProcessReceivedRawData_混合データ型基本処理**（拡張テスト）
   - 混合データ型（ビット・ワード混在）の基本処理
3. 異常系テスト（次フェーズ）
   - TC031: 不正生データ処理

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockPlcCommunicationManager**: PlcCommunicationManager全体のモック
- **MockConfigToFrameManager**: 前処理情報生成のモック

#### 使用するスタブ
- **PlcResponseStubs**: SLMP応答データのスタブ（単一型応答データ）
- **ProcessedDeviceRequestInfoStubs**: 前処理情報のスタブ
- **BasicProcessedResponseDataValidator**: 期待出力検証用ヘルパー

---

## 6. テストケース実装構造

### Arrange（準備）
1. **受信生データの準備**:
   - D000-D099ワード型応答データ（200バイト = 400文字ASCII）
   - SLMPヘッダ + ワードデータの完全な応答フレーム

2. **ProcessedDeviceRequestInfo準備**:
   - DDeviceRange = "D000-D099"
   - DataTypeInfo:
     - "D000-D099" → "Word"
   - SplitRanges = {} (空、Word型のためDWord分割なし)

3. **期待出力データの準備**:
   - ワード型期待値: D000～D099の各ushort値

4. **PlcCommunicationManagerインスタンス作成**:
   - モックSocket注入（接続済み状態）
   - 前処理情報設定済み

### Act（実行）
1. ProcessReceivedRawData実行:
   ```csharp
   var result = await plcCommManager.ProcessReceivedRawData(
       receivedRawData,              // 単一型受信生データ
       processedDeviceRequestInfo    // 前処理情報
   );
   ```

### Assert（検証）
1. **基本結果検証**:
   - `result.OriginalRawData == receivedRawData`（元データ保持）
   - `result.ProcessedAt`が現在時刻に近い
   - `result.HasErrors == false`（エラーなし）

2. **処理済みデータ検証**:
   - ワード型データ:
     - `result.BasicProcessedData["D000"]`がushort型
     - `result.BasicProcessedData["D000"] == 0x1234`（期待値と一致）
     - D000～D099全点の値確認

3. **統計情報検証**:
   - `result.ProcessedDeviceCount == 100`（D000-D099の100点）

4. **エラー情報検証**:
   - `result.Errors.Count == 0`（エラーなし）
   - `result.Warnings.Count == 0`（警告なし）

5. **型別処理精度検証**:
   - ワード型: リトルエンディアンでの正確な変換
   - デバイス名マッピングの正確性

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

### ProcessReceivedRawData スロー例外
- **DataProcessingException**: データ処理エラー
  - 不正な16進数形式
  - データ長不整合（期待長 vs 実際長）
  - 範囲外デバイス番号
- **FormatException**: フォーマット異常
  - 16進数変換失敗
  - 不正なSLMP応答形式
- **ArgumentException**: 不正な引数
  - ProcessedDeviceRequestInfoがnull
  - 受信生データが空文字列
- **InvalidOperationException**: 無効な操作
  - 前処理情報未設定
  - デバイス型情報不足

### エラーメッセージ統一
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    // データ処理エラー
    public const string InvalidRawDataFormat = "受信データの形式が不正です。";
    public const string DataLengthMismatch = "データ長が期待値と一致しません。期待: {0}バイト、実際: {1}バイト";
    public const string DeviceNumberOutOfRange = "デバイス番号が範囲外です: {0}";

    // 前処理情報エラー
    public const string ProcessedDeviceRequestInfoNull = "前処理情報（ProcessedDeviceRequestInfo）がnullです。";
    public const string DeviceTypeInfoMissing = "デバイス型情報が不足しています: {0}";

    // 変換エラー
    public const string HexConversionFailed = "16進数変換に失敗しました: {0}";
    public const string UnsupportedDataType = "サポートされていないデータ型です: {0}";
}
```

---

## 9. ログ出力要件

### LoggingManager連携
- 処理開始ログ: 受信データ長、デバイス情報
- 処理完了ログ: 処理デバイス数、所要時間
- エラーログ: 例外詳細、スタックトレース
- デバッグログ: 変換精度情報、処理統計

### ログレベル
- **Information**: 処理開始・完了
- **Debug**: 変換詳細、処理統計
- **Warning**: データ形式自動修正
- **Error**: 例外発生時

---

## 10. テスト実装チェックリスト

### TC029実装前
- [ ] PlcCommunicationManagerクラス作成（空実装）
- [ ] IPlcCommunicationManagerインターフェース作成
- [ ] ProcessReceivedRawDataメソッドシグネチャ定義
- [ ] BasicProcessedResponseDataモデル作成
- [ ] ProcessedDeviceRequestInfoモデル作成
- [ ] MockSocket・MockPlcCommunicationManager作成

### TC029実装中
- [ ] Arrange: 単一型受信データ準備
- [ ] Arrange: ProcessedDeviceRequestInfo準備
- [ ] Arrange: 期待出力データ準備
- [ ] Act: ProcessReceivedRawData呼び出し
- [ ] Assert: 基本結果検証
- [ ] Assert: ワード型データ検証（D000～D099）
- [ ] Assert: 統計情報検証

### TC029実装後
- [ ] テスト実行・Red確認
- [ ] ProcessReceivedRawData本体実装（最小実装）
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施
- [ ] TC030（混合データ型処理）との整合性確認

---

## 11. 参考情報

### SLMP仕様書
- デバイスコード表: SLMP仕様書pdf2img/page_36.png
- 応答フレーム構造: 4Eフレーム/ASCIIフォーマット準拠
- 終了コード: 0x0000（正常終了）、エラーコード各種

### テストデータサンプル
**配置先**: Tests/TestUtilities/TestData/SlmpResponseSamples/

- D000-D099_WordResponse.txt: ワード型応答データサンプル（400文字ASCII）

---

以上が TC029_ProcessReceivedRawData_基本後処理成功テスト実装に必要な情報です。