# TC032: CombineDwordData_DWord結合処理成功 テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC032_CombineDwordData_DWord結合処理成功テストケースを、TDD手法に従って実装してください。

---

## 🎯 テスト目的
PlcCommunicationManager.CombineDwordData メソッドのDWord結合処理機能が正常に動作することを確認

## 実装概要

### 目的
PlcCommunicationManager.CombineDwordData()メソッドのテストケースTC032を実装します。
このテストは、基本処理済みデータからDWord結合処理機能が正常に動作することを検証します。

### 実装対象
- **テストファイル**: `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`
- **テスト名前空間**: `andon.Tests.Unit.Core.Managers`
- **テストメソッド名**: `TC032_CombineDwordData_DWord結合処理成功`

---

## 前提条件の確認

実装開始前に以下を確認してください：

1. **依存ファイルの存在確認**
   - `Core/Managers/PlcCommunicationManager.cs` (空実装可)
   - `Core/Interfaces/IPlcCommunicationManager.cs`
   - `Core/Models/ProcessedResponseData.cs`
   - `Core/Models/BasicProcessedResponseData.cs`
   - `Core/Models/CombinedDWordDevice.cs`
   - `Core/Models/DWordCombineInfo.cs`

2. **テストユーティリティの確認**
   - `Tests/TestUtilities/Mocks/` 配下のモッククラス
   - `Tests/TestUtilities/Stubs/` 配下のスタブクラス
   - `Tests/TestUtilities/TestData/` 配下のテストデータ

3. **前提テスト確認**
   - TC029 (ProcessReceivedRawData) が実装済みであること

4. **開発手法ドキュメント確認**
   - `C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.md`を参照

不足しているファイルがあれば報告してください。

---

## ⭐ 重要度: 高（★マーク付きテスト）
Step6データ処理の第2段階として、基本処理済みデータからDWord結合処理が成功することを検証

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
- BasicProcessedResponseData準備（DWord結合対象含む）
- ProcessedDeviceRequestInfo準備（結合設定含む）
- CancellationToken準備

**Act（実行）**:
```csharp
var result = await plcManager.CombineDwordData(
    basicData,
    requestInfo,
    cancellationToken
);
```

**Assert（検証）**:
- result != null
- result.IsSuccess == true
- result.CombinedDWordDevices.Count > 0
- result.BasicProcessedDevices.Count > 0（元データ保持）
- 結合値が正確（0x56781234 など）

#### Step 1-3: テスト実行（Red確認）
```bash
dotnet test --filter "FullyQualifiedName~TC032"
```

期待結果: テスト失敗（CombineDwordDataが未実装のため）

---

### Phase 2: Green（最小実装）

#### Step 2-1: CombineDwordData最小実装

**実装箇所**: `Core/Managers/PlcCommunicationManager.cs`

**最小実装要件**:
```csharp
public async Task<ProcessedResponseData> CombineDwordData(
    BasicProcessedResponseData basicData,
    ProcessedDeviceRequestInfo processedRequestInfo,
    CancellationToken cancellationToken = default)
{
    // 1. 入力検証
    if (basicData == null)
        throw new ArgumentException("基本処理済みデータがnullです");

    if (processedRequestInfo == null)
        throw new ArgumentException("処理済み要求情報がnullです");

    // 2. ProcessedResponseDataオブジェクト作成
    var result = new ProcessedResponseData
    {
        IsSuccess = true,
        BasicProcessedDevices = basicData.ProcessedDevices.ToList(),
        CombinedDWordDevices = new List<CombinedDWordDevice>(),
        ProcessingTimeMs = basicData.ProcessingTimeMs + 25
    };

    // 3. DWord結合処理（最小実装）
    // ここで実際のDWord結合処理を行う
    // 現在は成功データを返すのみ

    return result;
}
```

#### Step 2-2: テスト再実行（Green確認）
```bash
dotnet test --filter "FullyQualifiedName~TC032"
```

期待結果: テストがパス

---

### Phase 3: Refactor（リファクタリング）

#### Step 3-1: 完全実装
- DWord結合アルゴリズムの実装
- ビット演算による正確な結合計算
- エラーハンドリングの強化
- ログ出力の追加
- パフォーマンス最適化

#### Step 3-2: テスト再実行（Green維持確認）
```bash
dotnet test --filter "FullyQualifiedName~TC032"
```

期待結果: すべてのテストがパス（リファクタリング後も）

---

## 📋 テスト仕様

### テスト対象メソッド
```csharp
Task<ProcessedResponseData> CombineDwordData(
    BasicProcessedResponseData basicData,
    ProcessedDeviceRequestInfo processedRequestInfo,
    CancellationToken cancellationToken = default
)
```

### 成功条件
1. **DWord結合実行**: 必要なデバイスのDWord結合が実行される
2. **ProcessedResponseData生成**: 結合処理済みデータオブジェクトが生成される
3. **結合済みデバイス追加**: CombinedDWordDevices に結合結果が追加される
4. **元データ保持**: BasicProcessedDevices の情報も保持される

### テストデータ
```csharp
// 基本処理済みデータ（DWord結合対象を含む）
BasicProcessedResponseData basicData = new BasicProcessedResponseData
{
    ProcessedDevices = new List<ProcessedDevice>
    {
        new ProcessedDevice { DeviceType = "D", Address = 100, Value = 0x1234 }, // 下位ワード
        new ProcessedDevice { DeviceType = "D", Address = 101, Value = 0x5678 }  // 上位ワード
    },
    IsSuccess = true,
    ProcessingTimeMs = 50
};

// リクエスト情報（DWord結合設定を含む）
ProcessedDeviceRequestInfo requestInfo = new ProcessedDeviceRequestInfo
{
    DeviceType = "D",
    StartAddress = 100,
    Count = 2,
    DWordCombineTargets = new List<DWordCombineInfo>
    {
        new DWordCombineInfo
        {
            LowWordAddress = 100,
            HighWordAddress = 101,
            CombinedName = "D100_32bit"
        }
    }
};
```

## 🧪 テスト実装パターン

### 1. Arrange（準備）
```csharp
// PlcCommunicationManagerインスタンス作成
// BasicProcessedResponseData準備（DWord結合対象含む）
// ProcessedDeviceRequestInfo準備（結合設定含む）
// CancellationToken準備
```

### 2. Act（実行）
```csharp
var result = await plcManager.CombineDwordData(
    basicData,
    requestInfo,
    cancellationToken
);
```

### 3. Assert（検証）
```csharp
// result != null
// result.IsSuccess == true
// result.CombinedDWordDevices.Count > 0
// result.BasicProcessedDevices.Count > 0 （元データ保持）
// 結合値が正確（0x56781234 など）
```

## 📊 検証項目詳細

### DWord結合機能検証
- [ ] DWord結合対象の正確な識別
- [ ] 上位・下位ワードの正確な結合
- [ ] 結合結果の正確性（ビット演算）
- [ ] 結合済みデバイス名の設定

### データ保持検証
- [ ] 元の BasicProcessedDevices 情報保持
- [ ] メタデータ（処理時間等）の引き継ぎ
- [ ] エラー・警告情報の引き継ぎ
- [ ] 統計情報の更新

### オブジェクト生成検証
- [ ] ProcessedResponseData オブジェクト生成
- [ ] IsSuccess プロパティが true
- [ ] CombinedDWordDevices の適切な設定
- [ ] ProcessingTimeMs の累積更新

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
// DWord結合処理タイムアウト
CombineProcessingTimeout = 3000ms

// ログレベル
LogLevel = Debug

// メモリ制限
MaxCombineMemoryMb = 50
```

## 📈 成功基準

### 機能的成功基準
1. **正常完了**: メソッドが例外なく完了
2. **DWord結合**: 指定されたデバイスの32bit値への正確な結合
3. **データ保持**: 元データと新規結合データの適切な保持
4. **処理時間**: 適切な処理時間での完了（< 50ms）

### DWord結合計算検証
```csharp
// 例：D100=0x1234, D101=0x5678 の場合
// 結合結果 = (D101 << 16) | D100 = 0x56781234
// 十進値 = 1450744372
```

### 非機能的成功基準
1. **メモリ使用量**: 結合処理中のメモリ使用量が閾値内
2. **ログ出力**: 結合処理の詳細ログ出力
3. **エラーハンドリング**: 結合失敗時の適切なエラー処理

---

## 技術仕様詳細

### DWord結合アルゴリズム

#### 結合計算方式
```csharp
// DWord結合計算（32bit値生成）
// リトルエンディアン: 下位ワード（Low）+ 上位ワード（High）
public uint CombineToUInt32(ushort lowWord, ushort highWord)
{
    return (uint)(lowWord | (highWord << 16));
}

// 結合例：
// Low=0x1234, High=0x5678 → 0x56781234
// 十進値: 4660 + 22136 << 16 → 1450744372
```

#### ビット演算詳細
```csharp
// ステップバイステップ計算例
ushort lowWord = 0x1234;   // 4660 (decimal)
ushort highWord = 0x5678;  // 22136 (decimal)

// Step 1: 上位ワードを16bit左シフト
uint shiftedHigh = (uint)(highWord << 16);
// 0x5678 << 16 = 0x56780000 = 1450713088

// Step 2: 下位ワードとOR演算
uint combined = lowWord | shiftedHigh;
// 0x1234 | 0x56780000 = 0x56781234 = 1450744372
```

### データモデル詳細

#### ProcessedResponseData構造
```csharp
public class ProcessedResponseData
{
    // 基本結果継承
    public bool IsSuccess { get; set; }
    public List<ProcessedDevice> BasicProcessedDevices { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
    public long ProcessingTimeMs { get; set; }

    // DWord結合結果
    public List<CombinedDWordDevice> CombinedDWordDevices { get; set; } = new();

    // エラー・統計情報
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int TotalProcessedDeviceCount { get; set; }

    // メソッド
    public void AddCombinedDevice(CombinedDWordDevice device);
    public CombinedDWordDevice GetCombinedDevice(string deviceName);
}
```

#### CombinedDWordDevice構造
```csharp
public class CombinedDWordDevice
{
    public string DeviceName { get; set; }          // "D100_32bit"
    public uint CombinedValue { get; set; }         // 結合後32bit値
    public int LowWordAddress { get; set; }         // 下位ワードアドレス (D100)
    public int HighWordAddress { get; set; }        // 上位ワードアドレス (D101)
    public ushort LowWordValue { get; set; }        // 下位ワード値
    public ushort HighWordValue { get; set; }       // 上位ワード値
    public DateTime CombinedAt { get; set; }        // 結合処理時刻
    public string DeviceType { get; set; }          // "D", "R" など
}
```

### 結合対象判定ロジック
```csharp
// DWord結合対象の特定
public List<DWordCombineInfo> IdentifyCombineTargets(
    ProcessedDeviceRequestInfo requestInfo,
    List<ProcessedDevice> devices)
{
    var targets = new List<DWordCombineInfo>();

    foreach (var combineInfo in requestInfo.DWordCombineTargets)
    {
        // 下位・上位ワードデバイスの存在確認
        var lowDevice = devices.FirstOrDefault(d =>
            d.DeviceType == combineInfo.DeviceType &&
            d.Address == combineInfo.LowWordAddress);

        var highDevice = devices.FirstOrDefault(d =>
            d.DeviceType == combineInfo.DeviceType &&
            d.Address == combineInfo.HighWordAddress);

        if (lowDevice != null && highDevice != null)
        {
            targets.Add(combineInfo);
        }
    }

    return targets;
}
```

---

## エラーハンドリング詳細

### スロー例外
- **DataProcessingException**: DWord結合処理エラー
  - 対象デバイスが見つからない
  - データ型が不正（Word型以外）
  - 結合計算でオーバーフロー
- **ArgumentException**: 不正な引数
  - BasicProcessedResponseDataがnull
  - ProcessedDeviceRequestInfoがnull
  - DWordCombineTargetsが空
- **InvalidOperationException**: 無効な操作
  - 下位・上位ワードペアが不完全
  - 既に結合済みのデバイス対象
- **OverflowException**: 数値オーバーフロー
  - 32bit範囲を超える結合値

### エラーメッセージ統一
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    // DWord結合エラー
    public const string CombineTargetNotFound = "DWord結合対象デバイスが見つかりません: {0}";
    public const string InvalidWordPair = "不正なワードペアです。Low:{0}, High:{1}";
    public const string CombineOverflow = "DWord結合でオーバーフローが発生しました: {0}";
    public const string UnsupportedDeviceType = "サポートされていないデバイス型です: {0}";

    // データ整合性エラー
    public const string BasicDataNull = "基本処理済みデータがnullです。";
    public const string CombineTargetsEmpty = "DWord結合対象が指定されていません。";
    public const string AlreadyCombined = "既に結合済みのデバイスです: {0}";

    // 処理フローエラー
    public const string InvalidProcessingOrder = "不正な処理順序です。基本処理を先に実行してください。";
}
```

---

## 🚨 注意事項

### DWord結合処理の注意
- **バイト順序**: リトルエンディアン/ビッグエンディアンの考慮
- **オーバーフロー**: 32bit値の範囲チェック
- **対象判定**: 結合対象デバイスの存在確認
- **エラー処理**: 結合失敗時の適切なエラー記録

### テスト実装時の注意
- **非同期処理**: await/async パターンの正確な実装
- **データ変更**: 元データを変更しないことの確認
- **メモリ管理**: 大きなオブジェクトの適切な管理

## 📋 チェックリスト

### 実装前チェック
- [ ] DWord結合アルゴリズムの理解
- [ ] テスト用基本データの準備
- [ ] 結合設定情報の準備
- [ ] 期待結果の事前計算

### 実装後チェック
- [ ] DWord結合結果の正確性確認
- [ ] 元データ保持の確認
- [ ] 実行時間が適切（< 100ms）
- [ ] メモリリークなし

### DWord結合テストケース
```csharp
// テストケース1: 基本的な結合
// D100=0x1234, D101=0x5678 → 0x56781234

// テストケース2: ゼロ値結合
// D200=0x0000, D201=0x1000 → 0x10000000

// テストケース3: 最大値結合
// D300=0xFFFF, D301=0xFFFF → 0xFFFFFFFF
```

### Phase 1基本動作確認での位置づけ
- **Step6データ処理系（4テスト中の2番目）**
- **推定実行時間**: 12-18分
- **★重要度**: 高（最小成功基準に含まれる）
- **前提テスト**: TC023（基本後処理）
- **後続テスト**: TC031（構造化）

### 依存関係
- **TC029成功後に実行**: 基本処理済みデータが必要
- **TC037への入力**: この処理結果が構造化処理の入力となる

---

## 実装記録・ドキュメント作成要件

### 必須作業項目

#### 1. 進捗記録開始
**ファイル**: `documents/implementation_records/progress_notes/2025-11-06_TC032実装.md`
- 実装開始時刻
- 目標（TC032テスト実装完了）
- 実装方針（DWord結合処理アルゴリズム）
- 進捗状況のリアルタイム更新

#### 2. 実装記録作成
**ファイル**: `documents/implementation_records/method_records/CombineDwordData実装記録.md`
- 実装判断根拠
  - なぜこの結合アルゴリズムを選択したか
  - 検討した他の方法との比較（論理演算 vs 算術演算）
  - 技術選択の根拠とトレードオフ（パフォーマンス vs 可読性）
- 発生した問題と解決過程
- DWordビット演算の実装詳細

#### 3. テスト結果保存
**ファイル**: `documents/implementation_records/execution_logs/TC032_テスト結果.log`
- 単体テスト結果（成功/失敗、実行時間、カバレッジ）
- DWord結合計算テスト結果（具体的な計算値検証）
- Red-Green-Refactorの各フェーズ結果
- パフォーマンステスト結果（実行時間、メモリ使用量）
- エラーログとデバッグ情報

---

## 完了条件

以下すべてが満たされた時点で実装完了とする：

### 機能的完了条件
- [ ] TC032テストがパス
- [ ] CombineDwordData本体実装完了
- [ ] DWord結合アルゴリズムの完全実装
- [ ] ProcessedResponseData生成機能の実装
- [ ] 元データ保持機能の実装

### DWord結合計算完了条件
- [ ] ビット演算による正確な結合（OR + シフト演算）
- [ ] リトルエンディアン対応
- [ ] オーバーフロー検出機能
- [ ] 3種類のテストケース検証完了（基本/ゼロ値/最大値）

### 非機能的完了条件
- [ ] エラーハンドリング完了（4種類の例外対応）
- [ ] ログ出力機能完了（4レベル対応）
- [ ] パフォーマンス要件満足（< 50ms）
- [ ] メモリ使用量要件満足（< 50MB）

### ドキュメント完了条件
- [ ] 進捗記録作成完了
- [ ] 実装記録作成完了（DWord結合アルゴリズム詳細含む）
- [ ] テスト結果ログ保存完了
- [ ] C:\Users\1010821\Desktop\python\andon\documents\design\チェックリスト\step6_test実施リスト.mdの該当項目にチェック

### 品質保証完了条件
- [ ] リファクタリング完了（コード品質向上）
- [ ] テスト再実行でGreen維持確認
- [ ] TC029（前段）およびTC037（後段）との整合性確認
- [ ] DWord結合計算精度の徹底検証

---

## ログ出力要件

### LoggingManager連携
- **処理開始ログ**: 基本データ数、結合対象数、処理開始時刻
- **結合処理ログ**: 各結合対象の詳細（Low/High値、結合結果）
- **処理完了ログ**: 結合済みデバイス数、所要時間、成功/失敗
- **エラーログ**: 例外詳細、結合失敗デバイス情報、スタックトレース
- **デバッグログ**: ビット演算詳細、計算過程、パフォーマンス情報

### ログレベル
- **Information**: 処理開始・完了
- **Debug**: DWord結合詳細、ビット演算過程
- **Warning**: 結合対象未発見、軽微な異常
- **Error**: 例外発生時、結合処理失敗時

### ログ出力例
```csharp
_logger.LogInformation("CombineDwordData開始: 基本データ数={DeviceCount}, 結合対象数={CombineTargetCount}",
    basicData.ProcessedDevices.Count, requestInfo.DWordCombineTargets.Count);

_logger.LogDebug("DWord結合実行: {DeviceName} = Low:0x{LowValue:X4} | (High:0x{HighValue:X4} << 16) = 0x{CombinedValue:X8}",
    combineInfo.CombinedName, lowValue, highValue, combinedValue);

_logger.LogInformation("CombineDwordData完了: 結合済みデバイス数={CombinedCount}, 所要時間={ElapsedMs}ms",
    result.CombinedDWordDevices.Count, elapsedMs);
```

---

## 実装時の注意点

### TDD手法厳守
- 必ずテストを先に書く（Red）
- 最小実装でテストをパスさせる（Green）
- リファクタリングで品質向上（Refactor）
- 各フェーズでテスト実行を確認

### DWord結合処理の注意
- **ビット演算精度**: OR演算とシフト演算の正確な実装
- **データ型安全性**: ushort → uint変換の適切な処理
- **エンディアン処理**: 三菱PLCのリトルエンディアン準拠
- **オーバーフロー対策**: 32bit範囲内での処理確認

### 計算検証の重要性
- DWord結合計算の正確性を複数パターンで検証
- 境界値テスト（0x0000/0x0000, 0xFFFF/0xFFFF）の実施
- 実機データとの整合性確認

### 記録の重要性
- DWord결合アルゴリズム選択の根拠を詳細記録
- ビット演算の計算過程を段階的に記録
- テスト結果は16進数/10進数両方で記録

### 文字化け対策
- 日本語ファイル名の新規作成時は`.txt`経由で作成
- 作成後は必ずReadツールで確認
- 文字化け発見時は早期に対処

---

## 参考情報

### 設計書参照先
- `documents/design/クラス設計.md` - PlcCommunicationManager詳細仕様
- `documents/design/テスト内容.md` - TC032詳細要件
- `documents/design/エラーハンドリング.md` - 例外処理方針
- `documents/design/ログ機能設計.md` - ログ出力仕様

### 開発手法
- `documents/development_methodology/development-methodology.md` - TDD実装手順

### データ型・演算参照
- C# ビット演算子: `|` (OR), `<<` (左シフト), `>>` (右シフト)
- データ型変換: `ushort` → `uint`キャスト
- リトルエンディアン: 下位バイト → 上位バイト順

### SLMP仕様書
- `pdf2img/sh080931q.pdf` - SLMP通信プロトコル仕様
- DWordデバイス仕様: page_28-32.png
- データ型定義: page_15-18.png

### PySLMPClient実装参照
- `PySLMPClient/pyslmpclient/const.py` - データ型定義
- `PySLMPClient/pyslmpclient/util.py` - DWord処理ロジック
- `PySLMPClient/tests/test_main.py` - DWord結合テスト実例

### テストデータサンプル
**配置先**: Tests/TestUtilities/TestData/DWordCombineSamples/
- BasicData_WithCombineTargets.json: 結合対象を含む基本データ
- CombineConfig_StandardPattern.json: 標準的な結合設定
- ExpectedResults_DWordCombine.json: 期待される結合結果

### DWord結合計算参考
```csharp
// 計算パターン例
// パターン1: D100=0x1234, D101=0x5678
// 結果: 0x56781234 (1450744372 decimal)

// パターン2: D200=0x0000, D201=0x1000
// 結果: 0x10000000 (268435456 decimal)

// パターン3: D300=0xFFFF, D301=0xFFFF
// 結果: 0xFFFFFFFF (4294967295 decimal, uint.MaxValue)
```

---

以上の指示に従って、TC032_CombineDwordData_DWord結合処理成功テストの実装を開始してください。

不明点や不足情報があれば、実装前に質問してください。