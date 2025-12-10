# Phase 2-2: MonitoringIntervalMsのExcel設定への移行

**フェーズ**: Phase 2-2
**影響度**: 中（タイマー間隔に影響）
**工数**: **小**（Phase 1-5完了により大幅削減）
**前提条件**: Phase 0完了（✅ 2025-12-02）, Phase 1完了（✅ 2025-12-02）, Phase 2-1完了（✅ 2025-12-03）
**状態**: ✅ **完了**（2025-12-03）
**実装結果**: [Phase2_2_MonitoringInterval_Excel移行_TestResults.md](../実装結果/Phase2_2_MonitoringInterval_Excel移行_TestResults.md)

---

## 🔄 Phase 2-1からの引き継ぎ事項

### Phase 2-1完了状況（2025-12-03完了）

**実装完了日**: 2025-12-03
**実装方式**: TDD (Red→Green→Refactor)
**最終テスト結果**:
- Phase2-1専用テスト: 100% (5/5合格)
- 全体テスト: 98.6% (818/821合格)
- **Phase2-1関連エラー**: 0件（完全解決）

#### Phase 2-1完了事項
✅ **LoggingConfig全7項目のハードコード化完了**
✅ **appsettings.json削減**: 14行 → 5行（9行削減）
✅ **LoggingConfig.cs削除**: クラスファイル完全削除
✅ **IOptions<LoggingConfig>依存削除**: LoggingManager.csから削除完了
✅ **DI設定更新**: LoggingConfig DI登録削除完了
✅ **ファイルアクセス問題完全解決**:
  - LoggingManager.cs: `FileShare.Read` → `FileShare.ReadWrite` に修正
  - テストクラスに`Collection`属性追加（並行実行防止）
  - `ReadFileWithSharedAccessAsync()`ヘルパーメソッド追加（10箇所修正）
  - ファイルアクセスエラー: 31件 → 0件（完全解決）

### 現在のappsettings.json状態（Phase 2-1完了後）

```json
{
  "PlcCommunication": {
    "MonitoringIntervalMs": 1000    // ← Phase 2-2で対応（このPhase）
  }
}
```

**現在の行数**: 5行（Phase 0開始前: 101行、Phase 0完了後: 19行、Phase 1完了後: 14行、Phase 2-1完了後: 5行）

### Phase 2-2での対応範囲

⏳ **PlcCommunication.MonitoringIntervalMs**: このPhaseでExcel設定利用に移行
⏳ **appsettings.json行数**: 5行 → 0行（5行削減、appsettings.json完全空化）
⏳ **IOptions<DataProcessingConfig>依存削除**: ExecutionOrchestrator.csから削除
⏳ **DataProcessingConfig.cs削除**: クラスファイルの削除

---

## 📋 概要

MonitoringIntervalMsをappsettings.jsonからExcel設定へ移行します。

**✅ Phase 1-5完了により、Excel読み込み処理は既に実装済みです。使用箇所の修正のみで完了します。**

---

## ⚠️ Phase 1-5完了による工数削減（重要）

### 既に完了している作業

#### ✅ Phase 2完了事項（ConfigurationLoaderExcel拡張）

| 完了項目 | 実装箇所 | 内容 |
|---------|---------|------|
| **Excel読み込み実装** | ConfigurationLoaderExcel.cs:115 | `MonitoringIntervalMs = ReadCell<int>(settingsSheet, "B11", "データ取得周期(ms)")` |
| **モデル格納** | PlcConfiguration.MonitoringIntervalMs | プロパティ定義済み |
| **既定値設定** | - | 既定値: 1000ms（ReadOptionalCell使用） |
| **検証ロジック** | SettingsValidator.cs | 検証メソッド実装済み |
| **統合テスト** | Phase5統合テスト | 9個のテストケース全成功 |

### 残りの作業（小規模修正）

| 作業内容 | 影響箇所 | 工数 |
|---------|---------|------|
| **ExecutionOrchestrator.cs:75の1箇所のみ修正** | GetMonitoringInterval()メソッド内 | **小** |
| **IOptions<DataProcessingConfig>依存削除** | コンストラクタ | **小** |
| **DI登録削除** | DependencyInjectionConfigurator.cs:30 | **小** |

---

## 🎯 対象項目（1項目）

| 項目 | 移行前 | 移行後 | 理由 |
|------|--------|--------|------|
| MonitoringIntervalMs | appsettings.json<br>`PlcCommunication.MonitoringIntervalMs`<br>（**現在値: 1000ms**） | Excel設定<br>settingsシート B11セル<br>（既定値: 1000ms） | ✅ Excel読み込み実装完了、各PLC個別設定可能、既定値が一致 |

---

## 🔍 現在の実装確認

### ExecutionOrchestrator.csでの使用箇所（修正が必要）

```csharp
// andon/Core/Controllers/ExecutionOrchestrator.cs:74-76

private readonly IOptions<DataProcessingConfig> _dataProcessingConfig; // ← 削除対象

public TimeSpan GetMonitoringInterval()
{
    var intervalMs = _dataProcessingConfig.Value.MonitoringIntervalMs; // ← 修正対象（L75）
    return TimeSpan.FromMilliseconds(intervalMs);
}
```

### ConfigurationLoaderExcel.csでの実装（✅ 完了済み）

```csharp
// andon/Infrastructure/Configuration/ConfigurationLoaderExcel.cs:115
// ✅ Phase 2完了: Excel読み込み実装済み

MonitoringIntervalMs = ReadCell<int>(settingsSheet, "B11", "データ取得周期(ms)"),
```

### PlcConfigurationモデル（✅ 完了済み）

```csharp
// andon/Core/Models/ConfigModels/PlcConfiguration.cs:56
// ✅ Phase 2完了: プロパティ定義済み

public int MonitoringIntervalMs { get; set; }
```

---

## 📝 TDDサイクル: Phase 2-2

### Step 2-2-1: Excel設定値使用の動作確認テスト作成（Red）

**目的**: Excel設定のMonitoringIntervalMsを使用してタイマーが正常動作することを確認

#### テストケース名
`Phase2_2_MonitoringInterval_ExcelMigrationTests.cs`

#### テストケース詳細

##### 1. test_ExecutionOrchestrator_Excel設定値を直接使用()

```csharp
[Test]
public async Task test_ExecutionOrchestrator_Excel設定値を直接使用()
{
    // Arrange
    var plcConfig = new PlcConfiguration
    {
        MonitoringIntervalMs = 10000 // Excel設定値: 10秒
    };
    var orchestrator = CreateOrchestratorWithoutDataProcessingConfig();

    // Act
    var result = await orchestrator.RunContinuousDataCycleAsync(plcConfig);

    // Assert
    // タイマー間隔がplcConfig.MonitoringIntervalMsの値（10000ms）であることを確認
    var actualInterval = _mockTimerService.LastInterval;
    Assert.That(actualInterval, Is.EqualTo(TimeSpan.FromMilliseconds(10000)));

    // _dataProcessingConfig.Value.MonitoringIntervalMsが使用されていないことを確認
    // （IOptions依存が削除されているため、このプロパティにアクセスできないことを確認）
}
```

##### 2. test_ExecutionOrchestrator_PLC毎に異なる監視間隔()

```csharp
[Test]
public async Task test_ExecutionOrchestrator_PLC毎に異なる監視間隔()
{
    // Arrange
    var plcConfig1 = new PlcConfiguration
    {
        PlcId = "PLC1",
        MonitoringIntervalMs = 5000 // PLC1: 5秒間隔
    };
    var plcConfig2 = new PlcConfiguration
    {
        PlcId = "PLC2",
        MonitoringIntervalMs = 10000 // PLC2: 10秒間隔
    };
    var orchestrator = CreateOrchestratorWithoutDataProcessingConfig();

    // Act
    var task1 = orchestrator.RunContinuousDataCycleAsync(plcConfig1);
    var task2 = orchestrator.RunContinuousDataCycleAsync(plcConfig2);

    await Task.WhenAll(task1, task2);

    // Assert
    // 各PLCが独立した間隔で動作することを確認
    Assert.That(_mockTimerService.GetInterval("PLC1"), Is.EqualTo(TimeSpan.FromMilliseconds(5000)));
    Assert.That(_mockTimerService.GetInterval("PLC2"), Is.EqualTo(TimeSpan.FromMilliseconds(10000)));
}
```

##### 3. test_MonitoringInterval_境界値テスト()

```csharp
[Test]
[TestCase(1, ExpectedResult = true)] // 1ms（最小値） - 動作する
[TestCase(1000, ExpectedResult = true)] // 1秒（通常値） - 動作する
[TestCase(5000, ExpectedResult = true)] // 5秒（通常値） - 動作する
[TestCase(3600000, ExpectedResult = true)] // 1時間（最大値） - 動作する
[TestCase(0, ExpectedResult = false)] // 0ms（異常値） - エラー
[TestCase(-1, ExpectedResult = false)] // -1ms（異常値） - エラー
public async Task<bool> test_MonitoringInterval_境界値(int intervalMs)
{
    // Arrange
    var plcConfig = new PlcConfiguration
    {
        MonitoringIntervalMs = intervalMs
    };
    var orchestrator = CreateOrchestratorWithoutDataProcessingConfig();

    // Act & Assert
    try
    {
        await orchestrator.RunContinuousDataCycleAsync(plcConfig);
        return true; // エラーなし
    }
    catch (ArgumentException)
    {
        return false; // エラー発生（異常値）
    }
}
```

##### 4. test_GetMonitoringInterval_削除後の互換性()

```csharp
[Test]
public void test_GetMonitoringInterval_削除後の互換性()
{
    // Arrange
    var orchestrator = CreateOrchestratorWithoutDataProcessingConfig();

    // Act
    // GetMonitoringInterval()を削除した場合、代替メソッドが正常動作することを確認
    var interval = orchestrator.GetMonitoringIntervalFromPlcConfig(
        new PlcConfiguration { MonitoringIntervalMs = 5000 }
    );

    // Assert
    Assert.That(interval, Is.EqualTo(TimeSpan.FromMilliseconds(5000)));
}
```

#### 期待される結果
Step 2-2-2の実装前は失敗（IOptions依存があるため）

---

### Step 2-2-2: 実装（Green）- 簡略化版

**✅ Phase 1-5完了により、Excel読み込み処理の追加実装は不要です。使用箇所の修正のみで完了します。**

#### 作業内容

##### 1. ExecutionOrchestrator.cs を修正（小規模修正）

```csharp
// 修正前
public class ExecutionOrchestrator : IExecutionOrchestrator
{
    private readonly IOptions<DataProcessingConfig> _dataProcessingConfig; // ← 削除

    public ExecutionOrchestrator(
        // ... 他のパラメータ
        IOptions<DataProcessingConfig> dataProcessingConfig) // ← 削除
    {
        _dataProcessingConfig = dataProcessingConfig; // ← 削除
        // ...
    }

    // L74-76: GetMonitoringInterval()
    public TimeSpan GetMonitoringInterval()
    {
        var intervalMs = _dataProcessingConfig.Value.MonitoringIntervalMs; // ← 修正対象（L75）
        return TimeSpan.FromMilliseconds(intervalMs);
    }
}
```

```csharp
// 修正後
public class ExecutionOrchestrator : IExecutionOrchestrator
{
    // private readonly IOptions<DataProcessingConfig> _dataProcessingConfig; - 削除済み

    public ExecutionOrchestrator(
        // ... 他のパラメータ
        // IOptions<DataProcessingConfig> dataProcessingConfig - 削除済み
    )
    {
        // _dataProcessingConfig = dataProcessingConfig; - 削除済み
        // ...
    }

    // GetMonitoringInterval()を削除 or 以下のように変更
    // オプション1: 完全削除（推奨）
    // public TimeSpan GetMonitoringInterval() - 削除

    // オプション2: デフォルト値返却に変更
    public TimeSpan GetMonitoringInterval()
    {
        // デフォルト値を返却（各PLCの設定値を使用する場合は不要）
        return TimeSpan.FromMilliseconds(1000); // 1秒（既定値）
    }

    // オプション3: PlcConfig引数を追加
    public TimeSpan GetMonitoringInterval(PlcConfiguration plcConfig)
    {
        return TimeSpan.FromMilliseconds(plcConfig.MonitoringIntervalMs);
    }
}
```

**修正箇所の詳細**:
```csharp
// L75の1箇所のみ修正

// 変更前:
var intervalMs = _dataProcessingConfig.Value.MonitoringIntervalMs;

// 変更後:
var intervalMs = plcConfig.MonitoringIntervalMs;

// ⚠️ 注意: plcConfigは既にメソッド引数で受け取っているため、追加の変更は不要
```

**具体的な修正例**:
```csharp
// 修正前（L189-230あたり）
public async Task<CycleExecutionResult> RunDataCycleAsync(PlcConfiguration plcConfig)
{
    // ...
    var interval = GetMonitoringInterval(); // ← appsettings.jsonの値を使用
    // ...
}
```

```csharp
// 修正後
public async Task<CycleExecutionResult> RunDataCycleAsync(PlcConfiguration plcConfig)
{
    // ...
    var interval = TimeSpan.FromMilliseconds(plcConfig.MonitoringIntervalMs); // ← Excel設定の値を使用
    // ...
}
```

##### 2. DependencyInjectionConfigurator.cs を修正

```csharp
// 修正前
public static IServiceCollection ConfigureServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... 他の登録

    // DataProcessingConfig登録
    services.Configure<DataProcessingConfig>(
        configuration.GetSection("PlcCommunication")); // ← 削除

    // ExecutionOrchestrator登録
    services.AddSingleton<IExecutionOrchestrator, ExecutionOrchestrator>();

    // ...
}
```

```csharp
// 修正後
public static IServiceCollection ConfigureServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... 他の登録

    // DataProcessingConfig登録を削除済み

    // ExecutionOrchestrator登録（IOptions依存なし）
    services.AddSingleton<IExecutionOrchestrator, ExecutionOrchestrator>();

    // ...
}
```

##### 3. DataProcessingConfig.cs を削除

```bash
rm andon/Core/Models/ConfigModels/DataProcessingConfig.cs
```

##### 4. appsettings.jsonから PlcCommunication セクション全体を削除

```json
// 削除前（Phase 2-1完了後、5行）
{
  "PlcCommunication": {
    "MonitoringIntervalMs": 1000  // ← 削除
  }
}
```

```json
// 削除後（Phase 2-2完了後、0行 = 空ファイル）
{
  // appsettings.json完全空化
  // Phase 3でファイル自体を削除予定
}
```

**重要**: Phase 2-2完了により、appsettings.jsonは完全に空になります（`{}`のみ）。Phase 3でファイル自体を削除します。

##### 5. テスト実行 → 全テストがパス

```bash
dotnet test --filter "FullyQualifiedName~Phase2_2"
dotnet test --filter "FullyQualifiedName~ExecutionOrchestrator"
dotnet test  # 全テスト実行
```

**⚠️ 重要**:
- ✅ Excel読み込み（ConfigurationLoaderExcel.cs:115）は既に実装完了（Phase 2完了）
- ✅ 既定値1000ms設定済み
- ✅ PlcConfiguration.MonitoringIntervalMsに格納済み
- **Excel読み込み処理の追加実装は不要。使用箇所の修正のみで完了。**

---

### Step 2-2-3: リファクタリング（Refactor）

**作業内容**:

#### 1. 各PLCごとの監視間隔処理のヘルパーメソッド抽出（必要に応じて）

```csharp
/// <summary>
/// PLC設定から監視間隔を取得
/// </summary>
/// <param name="plcConfig">PLC設定</param>
/// <returns>監視間隔</returns>
private TimeSpan GetMonitoringIntervalFromConfig(PlcConfiguration plcConfig)
{
    if (plcConfig.MonitoringIntervalMs <= 0)
    {
        throw new ArgumentException("MonitoringIntervalMs must be greater than 0");
    }

    return TimeSpan.FromMilliseconds(plcConfig.MonitoringIntervalMs);
}
```

#### 2. XMLドキュメントコメントの追加

```csharp
/// <summary>
/// 実行オーケストレータ（Excel設定ベース）
/// Phase 2-2完了: IOptions<DataProcessingConfig>依存を削除し、
/// PlcConfiguration.MonitoringIntervalMsを直接使用
/// </summary>
public class ExecutionOrchestrator : IExecutionOrchestrator
{
    // ...
}
```

#### 3. 不要なusingディレクティブの削除

```csharp
// ExecutionOrchestrator.cs

// 削除前
using Microsoft.Extensions.Options; // ← 削除（IOptions依存を削除したため）
using andon.Core.Models.ConfigModels.DataProcessingConfig; // ← 削除

// 削除後
// using Microsoft.Extensions.Options; - 削除済み
// using andon.Core.Models.ConfigModels.DataProcessingConfig; - 削除済み
```

#### 4. テスト再実行 → 全テストがパス

```bash
dotnet test --filter "FullyQualifiedName~Phase2_2"
dotnet test --filter "FullyQualifiedName~ExecutionOrchestrator"
dotnet test  # 全テスト実行
```

---

## ✅ 完了条件

### Phase 2-2完了の定義

以下の条件をすべて満たすこと：

1. ✅ ExecutionOrchestrator.cs の修正
   - IOptions<DataProcessingConfig>依存を削除
   - L75の1箇所を`plcConfig.MonitoringIntervalMs`に変更
   - GetMonitoringInterval()を削除 or デフォルト値返却に変更

2. ✅ DependencyInjectionConfigurator.cs の修正
   - services.Configure<DataProcessingConfig>(...) を削除

3. ✅ DataProcessingConfig.cs を削除

4. ✅ appsettings.jsonから PlcCommunication.MonitoringIntervalMs を削除

5. ✅ Phase2_2_MonitoringInterval_ExcelMigrationTests.cs の全テストがパス

6. ✅ 既存のすべてのExecutionOrchestrator関連テストがパス

7. ✅ 全体テストがパス

8. ✅ ビルドエラーなし

### 確認コマンド

```bash
# Phase 2-2のテスト確認
dotnet test --filter "FullyQualifiedName~Phase2_2"

# ExecutionOrchestrator関連テスト確認
dotnet test --filter "FullyQualifiedName~ExecutionOrchestrator"

# 全体テスト確認
dotnet test

# ビルド確認
dotnet build
```

---

## 🚨 注意事項

### 1. Excel設定の既定値

**Phase 2完了時の設定**:
- 既定値: 1000ms（1秒）
- Excel設定（settingsシート B11セル）が空の場合、自動的に1000msが使用される

**Phase 2-1完了後のappsettings.json設定**:
- 現在値: 1000ms（Phase 2-1完了時点で既に1000ms）

**✅ 重要**: appsettings.json現在値（1000ms）とExcel既定値（1000ms）が一致しているため、動作変更はありません。

**推奨事項**:
- Excel設定シートに明示的に値を記載（推奨: 1000ms、または運用に合わせた値）
- 既定値1000msは一般的な設定として適切

### 2. 各PLCごとに異なる監視間隔の設定

**メリット**:
- Excel設定で各PLCごとに異なる監視間隔を設定可能
- 例: PLC1は5秒間隔、PLC2は10秒間隔

**実装方法**:
```csharp
// 各PLCのExcel設定ファイル（settingsシート B11セル）に異なる値を設定
// PLC1: 5000 (5秒)
// PLC2: 10000 (10秒)

// ExecutionOrchestrator.csで個別に使用
var interval = TimeSpan.FromMilliseconds(plcConfig.MonitoringIntervalMs);
```

### 3. 境界値の検証

**検証すべき境界値**:
- 最小値: 1ms
- 通常値: 1000ms, 5000ms, 10000ms
- 最大値: 3600000ms（1時間）
- 異常値: 0ms, -1ms（エラーハンドリング）

**実装推奨**:
```csharp
if (plcConfig.MonitoringIntervalMs <= 0)
{
    throw new ArgumentException("MonitoringIntervalMs must be greater than 0");
}

if (plcConfig.MonitoringIntervalMs > 3600000) // 1時間超
{
    _loggingManager.LogWarning($"MonitoringIntervalMs ({plcConfig.MonitoringIntervalMs}ms) is very large (> 1 hour)");
}
```

---

## 📊 Excel移行のメリット・デメリット

### メリット

| 項目 | 詳細 |
|------|------|
| **柔軟性向上** | 各PLCごとに異なる監視間隔を設定可能 |
| **統一性** | 既存のExcel設定管理と統一 |
| **追加設定不要** | appsettings.json不要 |
| **工数削減** | Phase 2完了により、Excel読み込み実装済み |

### デメリット

| 項目 | 詳細 | 対応策 |
|------|------|--------|
| **既定値変更** | 5000ms → 1000ms | Excel設定に明示的に記載 |
| **設定ファイル依存** | Excel設定ファイルが必須 | 既存運用で問題なし |

---

## 🔄 Phase 2-1との違い

| 項目 | Phase 2-1 | Phase 2-2 |
|------|-----------|-----------|
| **対象項目** | LoggingConfig 7項目 | MonitoringIntervalMs 1項目 |
| **移行先** | ハードコード化 | Excel設定 |
| **影響度** | 高 | 中 |
| **工数** | 中 | **小（Phase 2完了により削減）** |
| **Excel読み込み実装** | 不要 | **✅ 完了済み（Phase 2）** |
| **使用箇所修正** | 複数箇所 | **1箇所のみ（L75）** |

---

## 📈 次のステップ

Phase 2-2完了後、Phase 2-3（PlcModelのJSON出力実装）に進みます。

→ [Phase2-3_PlcModel_JSON出力実装.md](./Phase2-3_PlcModel_JSON出力実装.md)
