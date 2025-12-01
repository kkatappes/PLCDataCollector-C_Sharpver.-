# TC029: ProcessReceivedRawData_基本後処理成功 テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC029_ProcessReceivedRawData_基本後処理成功テストケースを、TDD手法に従って実装してください。

---

## 🎯 テスト目的
PlcCommunicationManager.ProcessReceivedRawData メソッドの基本後処理機能が正常に動作することを確認

## 実装概要

### 目的
PlcCommunicationManager.ProcessReceivedRawData()メソッドのテストケースTC029を実装します。
このテストは、PLCから受信した生データの基本後処理機能が正常に動作することを検証します。

### 実装対象
- **テストファイル**: `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`
- **テスト名前空間**: `andon.Tests.Unit.Core.Managers`
- **テストメソッド名**: `TC029_ProcessReceivedRawData_基本後処理成功`

---

## 前提条件の確認

実装開始前に以下を確認してください：

1. **依存ファイルの存在確認**
   - `Core/Managers/PlcCommunicationManager.cs` (空実装可)
   - `Core/Interfaces/IPlcCommunicationManager.cs`
   - `Core/Models/BasicProcessedResponseData.cs`
   - `Core/Models/ProcessedDeviceRequestInfo.cs`
   - `Core/Models/ProcessedDevice.cs`

2. **テストユーティリティの確認**
   - `Tests/TestUtilities/Mocks/` 配下のモッククラス
   - `Tests/TestUtilities/Stubs/` 配下のスタブクラス
   - `Tests/TestUtilities/TestData/` 配下のテストデータ

3. **開発手法ドキュメント確認**
   - `C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.md`を参照

不足しているファイルがあれば報告してください。

---

## ⭐ 重要度: 高（★マーク付きテスト）
Step6データ処理の第1段階として、受信した生データの基本後処理が成功することを検証

---

## 実装手順（TDD Red-Green-Refactor）

### Phase 1: Red（テスト失敗）

#### Step 1-1: テストファイル準備
```
ファイル: Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs
名前空間: andon.Tests.Unit.Core.Managers
```

#### Step 1-2: テストケース実装

**Arrange（準備）**:
- MockLoggingManager、MockErrorHandler、MockResourceManager作成
- PlcCommunicationManagerインスタンス作成（モック注入）
- 受信生データ準備（3Eフレーム応答例）
- ProcessedDeviceRequestInfo準備（前処理情報）
- CancellationToken準備

**Act（実行）**:
```csharp
var result = await plcManager.ProcessReceivedRawData(
    rawData,
    requestInfo,
    cancellationToken
);
```

**Assert（検証）**:
- result != null
- result.IsSuccess == true
- result.ProcessedDevices.Count > 0
- result.Errors.Count == 0
- result.ProcessingTimeMs > 0

#### Step 1-3: テスト実行（Red確認）
```bash
dotnet test --filter "FullyQualifiedName~TC029"
```

期待結果: テスト失敗（ProcessReceivedRawDataが未実装のため）

---

### Phase 2: Green（最小実装）

#### Step 2-1: ProcessReceivedRawData最小実装

**実装箇所**: `Core/Managers/PlcCommunicationManager.cs`

**最小実装要件**:
```csharp
public async Task<BasicProcessedResponseData> ProcessReceivedRawData(
    byte[] rawData,
    ProcessedDeviceRequestInfo processedRequestInfo,
    CancellationToken cancellationToken = default)
{
    // 1. 入力検証
    if (rawData == null || rawData.Length == 0)
        throw new ArgumentException("受信データが空です");

    if (processedRequestInfo == null)
        throw new ArgumentException("処理済み要求情報がnullです");

    // 2. 基本処理済みデータオブジェクト作成
    var result = new BasicProcessedResponseData
    {
        IsSuccess = true,
        ProcessedDevices = new List<ProcessedDevice>(),
        Errors = new List<string>(),
        ProcessingTimeMs = 50
    };

    // 3. 生データ解析（最小実装）
    // ここで実際のSLMPフレーム解析を行う
    // 現在は成功データを返すのみ

    return result;
}
```

#### Step 2-2: テスト再実行（Green確認）
```bash
dotnet test --filter "FullyQualifiedName~TC029"
```

期待結果: テストがパス

---

### Phase 3: Refactor（リファクタリング）

#### Step 3-1: 完全実装
- SLMPフレーム解析の実装
- デバイス値抽出の実装
- エラーハンドリングの強化
- ログ出力の追加
- パフォーマンス最適化

#### Step 3-2: テスト再実行（Green維持確認）
```bash
dotnet test --filter "FullyQualifiedName~TC029"
```

期待結果: すべてのテストがパス（リファクタリング後も）

---

## 📋 テスト仕様

### テスト対象メソッド
```csharp
Task<BasicProcessedResponseData> ProcessReceivedRawData(
    byte[] rawData,
    ProcessedDeviceRequestInfo processedRequestInfo,
    CancellationToken cancellationToken = default
)
```

### 成功条件
1. **生データ解析成功**: 受信した生データが正しく解析される
2. **BasicProcessedResponseData生成**: 基本処理済みデータオブジェクトが生成される
3. **デバイス値抽出**: 各デバイスの値が正しく抽出される
4. **エラー情報記録**: エラーが発生しない場合、エラー情報は空

### テストデータ
```csharp
// 想定生データ（3Eフレーム応答例）
byte[] rawData = {
    0x44, 0x30, 0x30, 0x30,  // ヘッダー
    0x30, 0x30,              // 終了コード（正常）
    0x01, 0x23, 0x45, 0x67   // デバイスデータ（例：D100の値など）
};

// リクエスト情報
ProcessedDeviceRequestInfo requestInfo = new ProcessedDeviceRequestInfo
{
    DeviceType = "D",
    StartAddress = 100,
    Count = 2,
    FrameType = "3E"
};
```

## 🧪 テスト実装パターン

### 1. Arrange（準備）
```csharp
// PlcCommunicationManagerインスタンス作成
// テスト用の生データとリクエスト情報準備
// CancellationToken準備
```

### 2. Act（実行）
```csharp
var result = await plcManager.ProcessReceivedRawData(
    rawData,
    requestInfo,
    cancellationToken
);
```

### 3. Assert（検証）
```csharp
// result != null
// result.IsSuccess == true
// result.ProcessedDevices.Count > 0
// result.Errors.Count == 0
// result.ProcessingTimeMs > 0
```

## 📊 検証項目詳細

### 基本機能検証
- [ ] メソッド呼び出し成功
- [ ] BasicProcessedResponseData オブジェクト生成
- [ ] IsSuccess プロパティが true
- [ ] ProcessingTimeMs が適切な値

### データ処理検証
- [ ] デバイス値の正確な抽出
- [ ] デバイス型情報の保持
- [ ] アドレス情報の正確性
- [ ] データ型変換の正確性

### エラーハンドリング検証
- [ ] エラー情報が空であること
- [ ] 警告情報の適切な記録
- [ ] 統計情報の更新

---

## 技術仕様詳細

### SLMPフレーム構造（3Eフレーム）

#### 応答フレーム構成
```
応答フレーム（バイナリ形式）:
[サブヘッダ4バイト] + [ネットワーク情報7バイト] + [データ長2バイト] + [終了コード2バイト] + [デバイスデータ]

各フィールド:
- サブヘッダ: 0x44, 0x30, 0x30, 0x30 (3E応答フレーム識別)
- ネットワーク情報: 要求元ネットワーク番号等（7バイト）
- データ長: データ部バイト長（2バイト、リトルエンディアン）
- 終了コード: 0x00, 0x00 (正常終了) / エラーコード（2バイト）
- デバイスデータ: 実際のデバイス値（可変長）
```

#### データ変換アルゴリズム
```csharp
// 16進数文字列からバイト配列への変換
public byte[] HexStringToBytes(string hexString)
{
    var bytes = new byte[hexString.Length / 2];
    for (int i = 0; i < bytes.Length; i++)
    {
        bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
    }
    return bytes;
}

// リトルエンディアンでのワード値変換
public ushort BytesToWord(byte[] bytes, int offset)
{
    return (ushort)(bytes[offset] | (bytes[offset + 1] << 8));
}

// ビット値変換（1バイト = 8ビット）
public bool[] BytesToBits(byte[] bytes)
{
    var bits = new bool[bytes.Length * 8];
    for (int i = 0; i < bytes.Length; i++)
    {
        for (int j = 0; j < 8; j++)
        {
            bits[i * 8 + j] = (bytes[i] & (1 << j)) != 0;
        }
    }
    return bits;
}
```

### データモデル詳細

#### BasicProcessedResponseData構造
```csharp
public class BasicProcessedResponseData
{
    // 基本結果
    public bool IsSuccess { get; set; }
    public List<ProcessedDevice> ProcessedDevices { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
    public long ProcessingTimeMs { get; set; }

    // エラー情報
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    // 統計情報
    public int ProcessedDeviceCount { get; set; }
    public int TotalDataSizeBytes { get; set; }

    // メソッド
    public void AddProcessedDevice(string deviceName, object value, string dataType);
    public void AddError(string errorMessage);
    public void AddWarning(string warningMessage);
}
```

#### ProcessedDevice構造
```csharp
public class ProcessedDevice
{
    public string DeviceType { get; set; }      // "D", "M", "X", "Y"
    public int Address { get; set; }            // デバイス番号
    public object Value { get; set; }           // デバイス値
    public string DataType { get; set; }        // "Word", "Bit", "DWord"
    public DateTime ProcessedAt { get; set; }   // 処理時刻
    public string DeviceName { get; set; }      // "D100", "M000"等
}
```

---

## エラーハンドリング詳細

### スロー例外
- **DataProcessingException**: データ処理エラー
  - 不正なSLMPフレーム形式
  - データ長不整合（期待長 vs 実際長）
  - 範囲外デバイス番号
- **FormatException**: フォーマット異常
  - 16進数変換失敗
  - 不正な終了コード
- **ArgumentException**: 不正な引数
  - ProcessedDeviceRequestInfoがnull
  - 受信生データが空またはnull
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
    public const string InvalidEndCode = "SLMP終了コードが正常終了以外です: {0}";

    // 前処理情報エラー
    public const string ProcessedDeviceRequestInfoNull = "前処理情報（ProcessedDeviceRequestInfo）がnullです。";
    public const string DeviceTypeInfoMissing = "デバイス型情報が不足しています: {0}";

    // 変換エラー
    public const string HexConversionFailed = "16進数変換に失敗しました: {0}";
    public const string UnsupportedDataType = "サポートされていないデータ型です: {0}";
}
```

## 🔧 モック・依存関係

### 必要なモック
```csharp
// ILoggingManager - ログ出力用
Mock<ILoggingManager> mockLogging;

// IErrorHandler - エラー処理用
Mock<IErrorHandler> mockErrorHandler;

// IResourceManager - リソース管理用
Mock<IResourceManager> mockResourceManager;
```

### 設定値
```csharp
// タイムアウト設定
ProcessingTimeout = 5000ms

// ログレベル
LogLevel = Information

// メモリ制限
MaxProcessingMemoryMb = 100
```

## 📈 成功基準

### 機能的成功基準
1. **正常完了**: メソッドが例外なく完了
2. **データ抽出**: 全デバイス値が正確に抽出
3. **オブジェクト生成**: BasicProcessedResponseData が正しく生成
4. **処理時間**: 適切な処理時間での完了（< 100ms）

### 非機能的成功基準
1. **メモリ使用量**: 処理中のメモリ使用量が閾値内
2. **ログ出力**: 適切なログレベルでの情報出力
3. **リソース管理**: 処理後のリソース適切解放

## 🚨 注意事項

### 実装時の注意
- **非同期処理**: await/async パターンの正確な実装
- **CancellationToken**: キャンセル処理の適切な実装
- **例外処理**: 予期しない例外の適切なハンドリング
- **リソース管理**: using文やDisposeパターンの活用

### テスト実行時の注意
- **実行順序**: 他のテストとの依存関係なし
- **クリーンアップ**: テスト後のリソース解放
- **並行実行**: このテストは単独実行可能

## 📋 チェックリスト

### 実装前チェック
- [ ] テスト対象メソッドの仕様理解
- [ ] 必要なモックオブジェクトの準備
- [ ] テストデータの準備
- [ ] 依存関係の確認

### 実装後チェック
- [ ] すべてのAssertが成功
- [ ] 実行時間が適切（< 1秒）
- [ ] メモリリークなし
- [ ] ログ出力確認

### Phase 1基本動作確認での位置づけ
- **Step6データ処理系（4テスト中の1番目）**
- **推定実行時間**: 12-18分
- **★重要度**: 高（最小成功基準に含まれる）
- **後続テスト**: TC032（DWord結合）→ TC037（構造化）

---

## 実装記録・ドキュメント作成要件

### 必須作業項目

#### 1. 進捗記録開始
**ファイル**: `documents/implementation_records/progress_notes/2025-11-06_TC029実装.md`
- 実装開始時刻
- 目標（TC029テスト実装完了）
- 実装方針
- 進捗状況のリアルタイム更新

#### 2. 実装記録作成
**ファイル**: `documents/implementation_records/method_records/ProcessReceivedRawData実装記録.md`
- 実装判断根拠
  - なぜこの実装方法を選択したか
  - 検討した他の方法との比較
  - 技術選択の根拠とトレードオフ
- 発生した問題と解決過程
- SLMPフレーム解析アルゴリズムの選択理由

#### 3. テスト結果保存
**ファイル**: `documents/implementation_records/execution_logs/TC029_テスト結果.log`
- 単体テスト結果（成功/失敗、実行時間、カバレッジ）
- Red-Green-Refactorの各フェーズ結果
- パフォーマンステスト結果（実行時間、メモリ使用量）
- エラーログとデバッグ情報

---

## 完了条件

以下すべてが満たされた時点で実装完了とする：

### 機能的完了条件
- [ ] TC029テストがパス
- [ ] ProcessReceivedRawData本体実装完了
- [ ] SLMPフレーム解析機能の完全実装
- [ ] BasicProcessedResponseData生成機能の実装
- [ ] デバイス値抽出機能の実装

### 非機能的完了条件
- [ ] エラーハンドリング完了（5種類の例外対応）
- [ ] ログ出力機能完了（4レベル対応）
- [ ] パフォーマンス要件満足（< 100ms）
- [ ] メモリ使用量要件満足（< 100MB）

### ドキュメント完了条件
- [ ] 進捗記録作成完了
- [ ] 実装記録作成完了
- [ ] テスト結果ログ保存完了
- [ ] C:\Users\1010821\Desktop\python\andon\documents\design\チェックリスト\step6_test実施リスト.mdの該当項目にチェック

### 品質保証完了条件
- [ ] リファクタリング完了（コード品質向上）
- [ ] テスト再実行でGreen維持確認
- [ ] 他のTC（TC032, TC037）との整合性確認
- [ ] コードレビュー実施（自己レビューまたはペアレビュー）

---

## ログ出力要件

### LoggingManager連携
- **処理開始ログ**: 受信データ長、デバイス情報、処理開始時刻
- **処理完了ログ**: 処理デバイス数、所要時間、成功/失敗
- **エラーログ**: 例外詳細、スタックトレース、発生コンテキスト
- **デバッグログ**: フレーム解析詳細、データ変換統計、パフォーマンス情報

### ログレベル
- **Information**: 処理開始・完了
- **Debug**: フレーム解析詳細、データ変換統計
- **Warning**: データ形式自動修正、軽微な異常
- **Error**: 例外発生時、処理失敗時

### ログ出力例
```csharp
_logger.LogInformation("ProcessReceivedRawData開始: データ長={DataLength}バイト", rawData.Length);
_logger.LogDebug("SLMPフレーム解析開始: フレーム形式={FrameType}", "3E");
_logger.LogInformation("ProcessReceivedRawData完了: 処理デバイス数={DeviceCount}, 所要時間={ElapsedMs}ms",
    result.ProcessedDeviceCount, result.ProcessingTimeMs);
```

---

## 実装時の注意点

### TDD手法厳守
- 必ずテストを先に書く（Red）
- 最小実装でテストをパスさせる（Green）
- リファクタリングで品質向上（Refactor）
- 各フェーズでテスト実行を確認

### データ処理の注意
- **エンディアン処理**: 三菱PLCはリトルエンディアン
- **データ長計算**: ビット型（点数÷8切り上げ）、ワード型（点数×2）
- **型安全性**: 不正なキャストを避ける
- **メモリ管理**: 大きなバイト配列の適切な処理

### 記録の重要性
- 実装判断の根拠を詳細に記録
- テスト結果は数値データも含めて保存
- 発生した問題と解決過程を詳細記録

### 文字化け対策
- 日本語ファイル名の新規作成時は`.txt`経由で作成
- 作成後は必ずReadツールで確認
- 文字化け発見時は早期に対処

---

## 参考情報

### 設計書参照先
- `documents/design/クラス設計.md` - PlcCommunicationManager詳細仕様
- `documents/design/テスト内容.md` - TC029詳細要件
- `documents/design/エラーハンドリング.md` - 例外処理方針
- `documents/design/ログ機能設計.md` - ログ出力仕様

### 開発手法
- `documents/development_methodology/development-methodology.md` - TDD実装手順

### SLMP仕様書
- `pdf2img/sh080931q.pdf` - SLMP通信プロトコル仕様
- デバイスコード表: page_36.png
- 3Eフレーム構造: page_42-45.png

### PySLMPClient実装参照
- `PySLMPClient/pyslmpclient/const.py` - デバイスコード定義
- `PySLMPClient/pyslmpclient/__init__.py` - フレーム解析ロジック
- `PySLMPClient/pyslmpclient/util.py` - データ変換ユーティリティ
- `PySLMPClient/tests/test_main.py` - テストケース実例

### テストデータサンプル
**配置先**: Tests/TestUtilities/TestData/SlmpResponseSamples/
- BasicProcessing_4E_Response.bin: 4Eフレーム応答サンプル
- DeviceData_Mixed.txt: 混合デバイスデータサンプル
- ErrorResponse_Samples.bin: エラー応答サンプル

---

以上の指示に従って、TC029_ProcessReceivedRawData_基本後処理成功テストの実装を開始してください。

不明点や不足情報があれば、実装前に質問してください。