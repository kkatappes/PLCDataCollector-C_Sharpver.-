# Phase 2-4: SavePathの利用実装

**フェーズ**: Phase 2-4（新規追加）
**影響度**: 中（データ保存先パスに影響）
**工数**: **小**（Phase 1-5完了により簡略化）
**前提条件**: Phase 0, Phase 1, Phase 2-1, Phase 2-2, **Phase 2-3完了（2025-12-03）**

---

## 📋 概要

SavePathをExcel設定から使用するようにします。現在、Excel設定から読み込まれているが、ExecutionOrchestratorでハードコードされたパスが使用されている問題を修正します。

**✅ Phase 1-5完了により、Excel読み込み処理は既に実装済みです。ハードコード削除のみで完了します。**

---

## 🔗 Phase 2-3からの引継ぎ事項（2025-12-03完了）

### ✅ Phase 2-3完了内容
- **PlcModel JSON出力実装**: 100%完了
- **Excel設定読み込み**: ConfigurationLoaderExcel.cs:116で実装済み
- **DataOutputManager統合**: PlcModelパラメータ追加完了
- **JSON出力**: source.plcModelフィールド追加完了
- **TDDサイクル**: Red→Green→Refactor完全実施（27/27テスト合格）

### 📝 Phase 2-3の成功パターンをPhase 2-4に適用

| Phase 2-3の実装パターン | Phase 2-4への適用 |
|------------------------|------------------|
| **Excel読み込み**: 既に完了（Phase 1-5） | ✅ SavePathも同様に完了済み（ConfigurationLoaderExcel.cs:117） |
| **ハードコード削除**: DataOutputManager.cs L48 | ✅ ExecutionOrchestrator.cs L238のハードコード削除 |
| **nullチェック**: `plcModel ?? ""` | ✅ SavePathも同様の処理（`savePath ?? "./output"`） |
| **インターフェース変更**: IDataOutputManager | ❌ Phase 2-4では不要（既存メソッド使用） |
| **既存テスト修正**: 30箇所修正 | ⚠️ ExecutionOrchestratorTests.csの確認必要 |

### 🎯 Phase 2-4の実装方針（Phase 2-3の教訓活用）

**Phase 2-3で学んだこと**:
1. ✅ **並行修正の重要性**: インターフェースと実装を同時修正
2. ✅ **網羅的なテスト修正**: 既存テストを一括修正
3. ✅ **ビルド確認の徹底**: Green段階完了後、即座にビルド確認
4. ✅ **TDDサイクル厳守**: Red→Green→Refactorを完全遵守

**Phase 2-4での適用**:
- ✅ ExecutionOrchestrator.cs L238の修正のみ（シンプル）
- ✅ インターフェース変更なし（Phase 2-3より簡単）
- ✅ TDDサイクル厳守（Phase 2-4専用テスト作成）
- ✅ 既存テストの影響確認（ExecutionOrchestratorTests.cs）

---

## ⚠️ Phase 1-5完了による工数削減（重要）

### 既に完了している作業

#### ✅ Phase 2完了事項（ConfigurationLoaderExcel拡張）

| 完了項目 | 実装箇所 | 内容 |
|---------|---------|------|
| **Excel読み込み実装** | ConfigurationLoaderExcel.cs:117 | `SavePath = ReadCell<string>(settingsSheet, "B13", "データ保存先パス")` |
| **モデル格納** | PlcConfiguration.SavePath | プロパティ定義済み |
| **Excel位置** | settingsシート B13セル | "データ保存先パス" |

### 残りの作業（小規模修正）

| 作業内容 | 影響箇所 | 工数 |
|---------|---------|------|
| **ハードコードされたパスを削除** | ExecutionOrchestrator.cs:228 | **小** |
| **plcConfig.SavePathを使用** | ExecutionOrchestrator.cs:228 | **小** |
| **TODOコメント削除** | ExecutionOrchestrator.cs:228 | **小** |

---

## 🎯 対象項目（1項目）

| 項目 | 現状 | 修正後 | 理由 |
|------|------|--------|------|
| SavePath | ✅ Excel読み込み完了<br>❌ ハードコードされたパスを使用<br>`"C:/Users/PPESAdmin/Desktop/x/output"` | ✅ Excel設定の値を使用<br>`plcConfig.SavePath` | Excel設定による柔軟な保存先指定、開発環境固有パスの排除 |

---

## 🔍 現在の実装確認

### 問題箇所（修正が必要）

```csharp
// andon/Core/Controllers/ExecutionOrchestrator.cs:228

// TODO: Phase 1-4 Refactor - outputDirectoryを設定から取得
var outputDirectory = "C:/Users/PPESAdmin/Desktop/x/output"; // ← ハードコード

var outputResult = await _dataOutputManager.OutputToJson(
    plcConfig.IpAddress,
    plcConfig.Port,
    plcConfig.PlcModel,
    structuredData.Devices,
    outputDirectory // ← ハードコードされたパスを使用
);
```

### ConfigurationLoaderExcel.csでの実装（✅ 完了済み）

```csharp
// andon/Infrastructure/Configuration/ConfigurationLoaderExcel.cs:117
// ✅ Phase 2完了: Excel読み込み実装済み

SavePath = ReadCell<string>(settingsSheet, "B13", "データ保存先パス"),
```

### PlcConfigurationモデル（✅ 完了済み）

```csharp
// andon/Core/Models/ConfigModels/PlcConfiguration.cs
// ✅ Phase 2完了: プロパティ定義済み

public string SavePath { get; set; }
```

---

## 📝 TDDサイクル: Phase 2-4

### Step 2-4-1: SavePath利用の動作確認テスト作成（Red）

**目的**: Excel設定のSavePathを使用してデータが正しく出力されることを確認

#### テストケース名
`Phase2_4_SavePath_ExcelConfigTests.cs`

#### テストケース詳細

##### 1. test_ExecutionOrchestrator_Excel設定のSavePathを使用()

```csharp
[Test]
public async Task test_ExecutionOrchestrator_Excel設定のSavePathを使用()
{
    // Arrange
    var plcConfig = new PlcConfiguration
    {
        SavePath = "./test/custom/output" // Excel設定値
    };
    var orchestrator = CreateOrchestrator();

    // Act
    var result = await orchestrator.RunDataCycleAsync(plcConfig);

    // Assert
    Assert.That(result.Success, Is.True);

    // 指定されたパスにファイルが出力されていることを確認
    Assert.That(Directory.Exists("./test/custom/output"), Is.True);
    var jsonFiles = Directory.GetFiles("./test/custom/output", "*.json");
    Assert.That(jsonFiles.Length, Is.GreaterThan(0));

    // ハードコードされたパスにファイルが存在しないことを確認
    Assert.That(Directory.Exists("C:/Users/PPESAdmin/Desktop/x/output"), Is.False);
}
```

##### 2. test_ExecutionOrchestrator_SavePath絶対パス指定()

```csharp
[Test]
public async Task test_ExecutionOrchestrator_SavePath絶対パス指定()
{
    // Arrange
    var absolutePath = Path.Combine(Path.GetTempPath(), "andon_test_output");
    var plcConfig = new PlcConfiguration
    {
        SavePath = absolutePath // 絶対パス指定
    };
    var orchestrator = CreateOrchestrator();

    // Act
    var result = await orchestrator.RunDataCycleAsync(plcConfig);

    // Assert
    Assert.That(result.Success, Is.True);

    // 絶対パスにファイルが出力されていることを確認
    Assert.That(Directory.Exists(absolutePath), Is.True);
    var jsonFiles = Directory.GetFiles(absolutePath, "*.json");
    Assert.That(jsonFiles.Length, Is.GreaterThan(0));
}
```

##### 3. test_ExecutionOrchestrator_SavePath空の場合デフォルトパス使用()

```csharp
[Test]
public async Task test_ExecutionOrchestrator_SavePath空の場合デフォルトパス使用()
{
    // Arrange
    var plcConfig = new PlcConfiguration
    {
        SavePath = "" // 空文字列
    };
    var orchestrator = CreateOrchestrator();

    // Act
    var result = await orchestrator.RunDataCycleAsync(plcConfig);

    // Assert
    Assert.That(result.Success, Is.True);

    // デフォルトパス（例: "./output"）にファイルが出力されていることを確認
    Assert.That(Directory.Exists("./output"), Is.True);
}
```

##### 4. test_ExecutionOrchestrator_SavePathディレクトリ作成()

```csharp
[Test]
public async Task test_ExecutionOrchestrator_SavePathディレクトリ作成()
{
    // Arrange
    var newPath = "./test/new/directory/structure";
    if (Directory.Exists(newPath))
    {
        Directory.Delete(newPath, true); // テスト前にクリーンアップ
    }

    var plcConfig = new PlcConfiguration
    {
        SavePath = newPath // 存在しないディレクトリ
    };
    var orchestrator = CreateOrchestrator();

    // Act
    var result = await orchestrator.RunDataCycleAsync(plcConfig);

    // Assert
    Assert.That(result.Success, Is.True);

    // ディレクトリが自動的に作成されていることを確認
    Assert.That(Directory.Exists(newPath), Is.True);
}
```

##### 5. test_ExecutionOrchestrator_複数PLC異なるSavePath()

```csharp
[Test]
public async Task test_ExecutionOrchestrator_複数PLC異なるSavePath()
{
    // Arrange
    var plcConfig1 = new PlcConfiguration
    {
        PlcId = "PLC1",
        SavePath = "./output/plc1"
    };
    var plcConfig2 = new PlcConfiguration
    {
        PlcId = "PLC2",
        SavePath = "./output/plc2"
    };
    var orchestrator = CreateOrchestrator();

    // Act
    var task1 = orchestrator.RunDataCycleAsync(plcConfig1);
    var task2 = orchestrator.RunDataCycleAsync(plcConfig2);
    await Task.WhenAll(task1, task2);

    // Assert
    // 各PLCが独立した保存先に出力していることを確認
    Assert.That(Directory.Exists("./output/plc1"), Is.True);
    Assert.That(Directory.Exists("./output/plc2"), Is.True);

    var plc1Files = Directory.GetFiles("./output/plc1", "*.json");
    var plc2Files = Directory.GetFiles("./output/plc2", "*.json");
    Assert.That(plc1Files.Length, Is.GreaterThan(0));
    Assert.That(plc2Files.Length, Is.GreaterThan(0));
}
```

#### 期待される結果
Step 2-4-2の実装前は失敗（ハードコードされたパスが使用されるため）

---

### Step 2-4-2: 実装（Green）- 簡略化版

**✅ Phase 1-5完了により、Excel読み込み処理の追加実装は不要です。ハードコード削除のみで完了します。**

#### 作業内容

##### 1. ExecutionOrchestrator.cs のハードコード削除

```csharp
// 修正前（L228あたり）
// TODO: Phase 1-4 Refactor - outputDirectoryを設定から取得
var outputDirectory = "C:/Users/PPESAdmin/Desktop/x/output"; // ← ハードコード

var outputResult = await _dataOutputManager.OutputToJson(
    plcConfig.IpAddress,
    plcConfig.Port,
    plcConfig.PlcModel,
    structuredData.Devices,
    outputDirectory
);
```

```csharp
// 修正後
// TODOコメント削除済み
var outputDirectory = GetValidatedOutputDirectory(plcConfig.SavePath); // ← Excel設定を使用

var outputResult = await _dataOutputManager.OutputToJson(
    plcConfig.IpAddress,
    plcConfig.Port,
    plcConfig.PlcModel,
    structuredData.Devices,
    outputDirectory
);
```

##### 2. ヘルパーメソッドの追加（オプション）

```csharp
/// <summary>
/// 出力ディレクトリの検証と作成
/// </summary>
/// <param name="savePath">保存先パス（Excel設定）</param>
/// <returns>検証済み出力ディレクトリパス</returns>
private string GetValidatedOutputDirectory(string savePath)
{
    // 空文字列/null の場合はデフォルトパス
    if (string.IsNullOrWhiteSpace(savePath))
    {
        _loggingManager.LogWarning("SavePath is null or empty, using default: ./output");
        savePath = "./output";
    }

    // ディレクトリが存在しない場合は作成
    if (!Directory.Exists(savePath))
    {
        _loggingManager.LogInfo($"Creating output directory: {savePath}");
        Directory.CreateDirectory(savePath);
    }

    return savePath;
}
```

**または、シンプルな実装**:
```csharp
// 修正後（シンプル版）
var outputDirectory = string.IsNullOrWhiteSpace(plcConfig.SavePath)
    ? "./output"
    : plcConfig.SavePath;

// ディレクトリ作成（存在しない場合）
if (!Directory.Exists(outputDirectory))
{
    Directory.CreateDirectory(outputDirectory);
}

var outputResult = await _dataOutputManager.OutputToJson(
    plcConfig.IpAddress,
    plcConfig.Port,
    plcConfig.PlcModel,
    structuredData.Devices,
    outputDirectory
);
```

##### 3. テスト実行 → 全テストがパス

```bash
dotnet test --filter "FullyQualifiedName~Phase2_4"
dotnet test --filter "FullyQualifiedName~ExecutionOrchestrator"
dotnet test  # 全テスト実行
```

**⚠️ 重要**:
- ✅ Excel読み込み（ConfigurationLoaderExcel.cs:117）は既に実装完了（Phase 2完了）
- ✅ PlcConfiguration.SavePathに格納済み
- **Excel読み込み処理の追加実装は不要。ハードコード削除のみで完了。**

---

### Step 2-4-3: リファクタリング（Refactor）

**作業内容**:

#### 1. パス検証ロジックの拡張（オプション）

```csharp
/// <summary>
/// 出力ディレクトリの検証と作成（拡張版）
/// </summary>
/// <param name="savePath">保存先パス（Excel設定）</param>
/// <returns>検証済み出力ディレクトリパス</returns>
private string GetValidatedOutputDirectory(string savePath)
{
    // 空文字列/null の場合はデフォルトパス
    if (string.IsNullOrWhiteSpace(savePath))
    {
        _loggingManager.LogWarning("SavePath is null or empty, using default: ./output");
        return CreateDirectoryIfNotExists("./output");
    }

    // 不正な文字チェック
    var invalidChars = Path.GetInvalidPathChars();
    if (savePath.Any(c => invalidChars.Contains(c)))
    {
        _loggingManager.LogError($"SavePath contains invalid characters: {savePath}");
        throw new ArgumentException($"Invalid SavePath: {savePath}");
    }

    // ディレクトリ作成
    return CreateDirectoryIfNotExists(savePath);
}

/// <summary>
/// ディレクトリが存在しない場合は作成
/// </summary>
/// <param name="path">ディレクトリパス</param>
/// <returns>ディレクトリパス</returns>
private string CreateDirectoryIfNotExists(string path)
{
    if (!Directory.Exists(path))
    {
        _loggingManager.LogInfo($"Creating output directory: {path}");
        Directory.CreateDirectory(path);
    }

    return path;
}
```

#### 2. XMLドキュメントコメントの追加

```csharp
/// <summary>
/// データサイクル実行
/// Phase 2-4完了: Excel設定のSavePathを使用
/// </summary>
/// <param name="plcConfig">PLC設定（SavePathを含む）</param>
/// <returns>サイクル実行結果</returns>
public async Task<CycleExecutionResult> RunDataCycleAsync(PlcConfiguration plcConfig)
{
    // ...
}
```

#### 3. TODOコメントの削除

```csharp
// 削除前
// TODO: Phase 1-4 Refactor - outputDirectoryを設定から取得
var outputDirectory = "C:/Users/PPESAdmin/Desktop/x/output";

// 削除後
// TODOコメント削除済み、Excel設定のSavePathを使用
var outputDirectory = GetValidatedOutputDirectory(plcConfig.SavePath);
```

#### 4. テスト再実行 → 全テストがパス

```bash
dotnet test --filter "FullyQualifiedName~Phase2_4"
dotnet test --filter "FullyQualifiedName~ExecutionOrchestrator"
dotnet test  # 全テスト実行
```

---

## ✅ 完了条件

### Phase 2-4完了の定義

以下の条件をすべて満たすこと：

1. ✅ ExecutionOrchestrator.cs の修正
   - ハードコードされたパス`"C:/Users/PPESAdmin/Desktop/x/output"`を削除
   - plcConfig.SavePathを使用
   - TODOコメント削除

2. ✅ GetValidatedOutputDirectory()ヘルパーメソッドの追加（推奨）

3. ✅ Phase2_4_SavePath_ExcelConfigTests.cs の全テストがパス

4. ✅ 既存のすべてのExecutionOrchestrator関連テストがパス

5. ✅ 全体テストがパス

6. ✅ ビルドエラーなし

### 確認コマンド

```bash
# Phase 2-4のテスト確認
dotnet test --filter "FullyQualifiedName~Phase2_4"

# ExecutionOrchestrator関連テスト確認
dotnet test --filter "FullyQualifiedName~ExecutionOrchestrator"

# 全体テスト確認
dotnet test

# ビルド確認
dotnet build
```

---

## 🚨 注意事項

### 1. 相対パスと絶対パスの扱い

**相対パス**:
- `"./output"` - 実行ファイルと同じディレクトリの`output`サブディレクトリ
- `"../output"` - 親ディレクトリの`output`サブディレクトリ

**絶対パス**:
- `"C:/Users/1010821/Desktop/output"` - Windowsの絶対パス
- `"/home/user/output"` - Linuxの絶対パス

**推奨**:
- 開発環境では相対パス
- 本番環境では絶対パス（デプロイ時に設定）

### 2. ディレクトリ作成権限

**注意点**:
- SavePathに指定されたディレクトリが存在しない場合、自動的に作成される
- 作成に失敗する可能性がある場合（権限不足等）、エラーハンドリングが必要

**推奨実装**:
```csharp
try
{
    Directory.CreateDirectory(savePath);
}
catch (UnauthorizedAccessException ex)
{
    _loggingManager.LogError($"Insufficient permissions to create directory: {savePath}");
    throw;
}
catch (IOException ex)
{
    _loggingManager.LogError($"Failed to create directory: {savePath}, {ex.Message}");
    throw;
}
```

### 3. パスの検証

**検証すべき項目**:
- 不正な文字が含まれていないか
- パスが長すぎないか（Windows: 260文字制限）
- ネットワークパスか（`\\server\share`形式）

**推奨実装**:
```csharp
var invalidChars = Path.GetInvalidPathChars();
if (savePath.Any(c => invalidChars.Contains(c)))
{
    throw new ArgumentException($"Invalid SavePath: {savePath}");
}

if (savePath.Length > 260)
{
    _loggingManager.LogWarning($"SavePath is very long ({savePath.Length} chars), may cause issues on Windows");
}
```

### 4. 既存テストコードの修正

**影響を受けるテストコード**:
- ExecutionOrchestratorTests.cs（ハードコードされたパスを前提としているテストケース）

**修正内容**:
```csharp
// 修正前（既存テスト）
var result = await _orchestrator.RunDataCycleAsync(plcConfig);

// ハードコードされたパスにファイルが存在することを確認
Assert.That(File.Exists("C:/Users/PPESAdmin/Desktop/x/output/data.json"), Is.True);

// 修正後
var plcConfig = new PlcConfiguration
{
    SavePath = "./test_output" // テスト用パス
};
var result = await _orchestrator.RunDataCycleAsync(plcConfig);

// テスト用パスにファイルが存在することを確認
Assert.That(File.Exists("./test_output/data.json"), Is.True);
```

---

## 📊 SavePath使用のメリット・デメリット

### メリット

| 項目 | 詳細 |
|------|------|
| **柔軟性向上** | 各PLCごとに異なる保存先を設定可能 |
| **環境依存排除** | 開発環境固有のパスがコードから削除される |
| **保守性向上** | 保存先変更時にExcel設定の修正のみで対応可能 |
| **統一性** | 既存のExcel設定管理と統一 |

### デメリット

| 項目 | 詳細 | 対応策 |
|------|------|--------|
| **設定ファイル依存** | Excel設定が必須 | デフォルト値（"./output"）を提供 |
| **パス検証必要** | 不正なパスの可能性 | GetValidatedOutputDirectory()で検証 |

---

## 🔄 Phase 2-3との違い（Phase 2-3: 2025-12-03完了）

| 項目 | Phase 2-3（完了） | Phase 2-4（これから実装） |
|------|-----------|-----------|
| **対象項目** | PlcModel | SavePath |
| **修正内容** | JSON出力への追加（source.plcModel） | ハードコード削除（outputDirectory） |
| **影響度** | 中（JSON出力の完全性） | 中（データ保存先パス） |
| **工数** | 小 | **小（Phase 2-3より簡単）** |
| **完了日** | ✅ **2025-12-03** | ⏳ 未着手 |
| **Excel読み込み実装** | ✅ 完了済み（ConfigurationLoaderExcel.cs:116） | ✅ 完了済み（ConfigurationLoaderExcel.cs:117） |
| **修正箇所** | 4ファイル（IDataOutputManager, DataOutputManager, ExecutionOrchestrator, 新規テスト） | **1ファイル**（ExecutionOrchestrator.cs:238のみ） |
| **インターフェース変更** | ✅ あり（IDataOutputManager） | ❌ **なし**（既存メソッド使用） |
| **既存テスト修正** | ✅ 30箇所修正（DataOutputManagerTests 24箇所、統合テスト 5箇所、Mockセットアップ 1箇所） | ⚠️ 確認必要（ExecutionOrchestratorTests.cs） |
| **新規テスト** | Phase2_3_PlcModel_JsonOutputTests.cs（4テスト） | Phase2_4_SavePath_ExcelConfigTests.cs（5テスト予定） |
| **TDDサイクル結果** | ✅ Red→Green→Refactor完全実施（100%合格） | ⏳ 未実施 |
| **設計仕様準拠** | ✅ JSON出力に`source.plcModel`追加完了 | ⏳ ハードコード削除により柔軟性向上 |

### Phase 2-4がPhase 2-3より簡単な理由

1. **インターフェース変更なし**: Phase 2-3ではIDataOutputManagerの変更が必要だったが、Phase 2-4では不要
2. **修正箇所が1箇所のみ**: ExecutionOrchestrator.cs L238のみ修正
3. **既存テスト修正が少ない**: Phase 2-3では30箇所修正が必要だったが、Phase 2-4では最小限の確認のみ
4. **Phase 2-3の成功パターン適用**: 同様のアプローチで実装可能

---

## 📈 次のステップ

Phase 2-4完了後、Phase 2-5（SettingsValidator統合）またはPhase 3（appsettings.json完全廃止）に進みます。

→ [Phase2-5_SettingsValidator統合.md](./Phase2-5_SettingsValidator統合.md)
→ [Phase3_appsettings完全廃止.md](./Phase3_appsettings完全廃止.md)
