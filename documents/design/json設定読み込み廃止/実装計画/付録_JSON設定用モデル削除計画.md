# 付録: JSON設定用モデルの削除計画

**フェーズ**: 付録（オプション）
**影響度**: 低～中（Phase6機能のため本格活用前）
**工数**: 小
**前提条件**: Phase 0～Phase 3完了

---

## 📋 概要

Phase 6で追加されたJSON設定専用モデル（PlcConnectionConfig、DeviceEntry、MultiPlcConfig等）を削除し、Excel設定ベース（PlcConfiguration）に統一します。

**重要**: これらのモデルはJSON設定読み込みのために追加されたものですが、appsettings.json廃止により不要になります。

---

## 🔍 背景: 2つの設定モデルの並存

### PlcConfiguration（Excel設定用モデル）- ✅ 継続使用

**ファイル**: `andon/Core/Models/ConfigModels/PlcConfiguration.cs`

**用途**: Excel設定ファイル（.xlsx）からの読み込み専用モデル

**特徴**:
- MonitoringIntervalMs, PlcModel, SavePath等を含む完全な設定
- ConfigurationLoaderExcel.LoadAllPlcConnectionConfigs()で使用
- ExecutionOrchestrator.ExecuteMultiPlcCycleAsync_Internal()で使用
- 既存の運用で主に使用されている

### PlcConnectionConfig（JSON設定用モデル）- ❌ 削除予定

**ファイル**: `andon/Core/Models/ConfigModels/PlcConnectionConfig.cs`

**用途**: appsettings.json等のJSON設定ファイル読み込み用モデル（Phase6新規追加）

**特徴**:
- 軽量な接続特化設定（MonitoringIntervalMs, PlcModel, SavePathを含まない）
- Priority（並列実行優先度）プロパティあり
- ExecutionOrchestrator.ExecuteSinglePlcAsync()で使用
- MultiPlcCoordinator（並列実行）で使用
- **現状では本格的な活用はこれからの段階**

---

## 🎯 削除対象一覧

### 1. モデルクラス（4個）

| ファイル | 説明 | 削除理由 |
|---------|------|---------|
| PlcConnectionConfig.cs | JSON設定専用モデル | appsettings.json廃止により不要 |
| DeviceEntry.cs | JSON設定読み込み用中間型 | PlcConnectionConfigでのみ使用 |
| MultiPlcConfig.cs | JSON設定での複数PLC管理用 | PlcConnectionConfigのコンテナ |
| ParallelProcessingConfig.cs（あれば） | 並列実行設定 | JSON設定専用 |

### 2. マネージャークラス（1個）

| ファイル | 説明 | 削除理由 |
|---------|------|---------|
| MultiPlcCoordinator.cs | PlcConnectionConfig専用の並列実行ヘルパー | PlcConnectionConfig削除に伴い不要 |

### 3. 使用箇所の修正

| ファイル | 修正内容 |
|---------|---------|
| ExecutionOrchestrator.cs | ExecuteSinglePlcAsync()メソッド削除 or PlcConfiguration版に統合 |
| テストコード | MultiPlcConfigTests.cs, MultiPlcCoordinatorTests.cs削除 |

---

## 📝 TDDサイクル: 付録

### Step 付録-1: 削除影響範囲の特定テスト作成（Red）

**目的**: 削除対象クラスの依存関係を洗い出す

#### テストケース名
`Appendix_JsonConfigModels_DependencyTests.cs`

#### テストケース詳細

##### 1. test_PlcConnectionConfig_本番フローで限定的使用()

```csharp
[Test]
public void test_PlcConnectionConfig_本番フローで限定的使用()
{
    // Arrange
    var orchestrator = CreateOrchestrator();

    // Act
    var usedTypes = orchestrator.GetDependencyTypes();

    // Assert
    // ExecuteSinglePlcAsync()でのみ使用されることを確認
    Assert.That(usedTypes, Does.Contain(typeof(PlcConnectionConfig)));

    // ExecuteMultiPlcCycleAsync_Internal()では使用されていないことを確認
    var multiPlcMethod = typeof(ExecutionOrchestrator).GetMethod("ExecuteMultiPlcCycleAsync_Internal");
    var multiPlcParameters = multiPlcMethod.GetParameters();
    Assert.That(multiPlcParameters.Any(p => p.ParameterType == typeof(PlcConnectionConfig)), Is.False);
}
```

##### 2. test_MultiPlcCoordinator_本番フローで使用()

```csharp
[Test]
public void test_MultiPlcCoordinator_本番フローで使用()
{
    // Arrange
    var orchestrator = CreateOrchestrator();

    // Act
    var dependencies = orchestrator.GetInjectedDependencies();

    // Assert
    // MultiPlcCoordinatorがExecutionOrchestratorから呼ばれているか確認
    Assert.That(dependencies, Does.Contain(typeof(IMultiPlcCoordinator)));
}
```

##### 3. test_DeviceEntry_PlcConnectionConfigでのみ使用()

```csharp
[Test]
public void test_DeviceEntry_PlcConnectionConfigでのみ使用()
{
    // Arrange
    var allTypes = typeof(Program).Assembly.GetTypes();

    // Act
    var usageCount = allTypes.Count(t =>
        t.GetProperties().Any(p => p.PropertyType == typeof(List<DeviceEntry>))
    );

    // Assert
    // DeviceEntryがPlcConnectionConfig以外で使用されていないことを確認
    Assert.That(usageCount, Is.EqualTo(1)); // PlcConnectionConfigのみ
}
```

#### 期待される結果
影響範囲の特定

---

### Step 付録-2: 削除実装（Green）

**作業内容**:

#### 1. ExecutionOrchestrator.ExecuteSinglePlcAsync() を削除

```csharp
// 削除前
public class ExecutionOrchestrator : IExecutionOrchestrator
{
    // PlcConnectionConfig専用メソッド
    public async Task<CycleExecutionResult> ExecuteSinglePlcAsync(PlcConnectionConfig plcConnection)
    {
        // PlcConnectionConfigを使用した処理
        // ...
    }

    // PlcConfiguration版（継続使用）
    public async Task<CycleExecutionResult> RunDataCycleAsync(PlcConfiguration plcConfig)
    {
        // PlcConfigurationを使用した処理
        // ...
    }
}
```

```csharp
// 削除後
public class ExecutionOrchestrator : IExecutionOrchestrator
{
    // ExecuteSinglePlcAsync()を削除済み

    // PlcConfiguration版（継続使用）
    public async Task<CycleExecutionResult> RunDataCycleAsync(PlcConfiguration plcConfig)
    {
        // PlcConfigurationを使用した処理
        // ...
    }
}
```

#### 2. MultiPlcCoordinator.cs を削除

```bash
rm andon/Core/Managers/MultiPlcCoordinator.cs
```

#### 3. IMultiPlcCoordinator.cs を削除（あれば）

```bash
rm andon/Core/Interfaces/IMultiPlcCoordinator.cs
```

#### 4. PlcConnectionConfig.cs を削除

```bash
rm andon/Core/Models/ConfigModels/PlcConnectionConfig.cs
```

#### 5. DeviceEntry.cs を削除

```bash
rm andon/Core/Models/ConfigModels/DeviceEntry.cs
```

#### 6. MultiPlcConfig.cs を削除

```bash
rm andon/Core/Models/ConfigModels/MultiPlcConfig.cs
```

#### 7. ParallelProcessingConfig.cs を削除（あれば）

```bash
rm andon/Core/Models/ConfigModels/ParallelProcessingConfig.cs
```

#### 8. 関連テストコードを削除 or 修正

```bash
# 削除対象テストファイル
rm andon/Tests/Unit/Core/Models/ConfigModels/PlcConnectionConfigTests.cs
rm andon/Tests/Unit/Core/Models/ConfigModels/MultiPlcConfigTests.cs
rm andon/Tests/Unit/Core/Managers/MultiPlcCoordinatorTests.cs

# 修正対象テストファイル
# ExecutionOrchestratorTests.cs - ExecuteSinglePlcAsync()のテストケースを削除
```

#### 9. DI登録の削除（あれば）

```csharp
// DependencyInjectionConfigurator.cs

// 削除前
services.AddSingleton<IMultiPlcCoordinator, MultiPlcCoordinator>(); // ← 削除

// 削除後
// IMultiPlcCoordinator登録を削除済み
```

#### 10. テスト実行 → 全テストがパス

```bash
dotnet build  # ビルドエラーがないことを確認
dotnet test --filter "FullyQualifiedName~Appendix"
dotnet test  # 全テスト実行
```

---

### Step 付録-3: リファクタリング（Refactor）

**作業内容**:

#### 1. 不要なusingディレクティブの削除

```csharp
// ExecutionOrchestrator.cs 等で削除
// using andon.Core.Models.ConfigModels.PlcConnectionConfig; // ← 削除
// using andon.Core.Models.ConfigModels.DeviceEntry; // ← 削除
// using andon.Core.Managers.MultiPlcCoordinator; // ← 削除
```

#### 2. コメント更新（PlcConfiguration中心の設計であることを明記）

```csharp
/// <summary>
/// 実行オーケストレータ（Excel設定ベース）
/// PlcConfigurationモデルを使用した統一設計
/// ⚠️ 注意: PlcConnectionConfigは削除済み（JSON設定廃止により不要）
/// </summary>
public class ExecutionOrchestrator : IExecutionOrchestrator
{
    // ...
}
```

#### 3. ドキュメント更新

**README.md更新**:
```markdown
## 設計方針

本アプリケーションは、Excel設定ファイルベースの単一設計を採用しています。

### 設定モデル

- **PlcConfiguration**: Excel設定読み込み用モデル（唯一の設定モデル）
- ~~PlcConnectionConfig~~: 削除済み（JSON設定廃止により不要）
- ~~MultiPlcConfig~~: 削除済み（JSON設定廃止により不要）
```

#### 4. テスト再実行 → 全テストがパス

```bash
dotnet test --filter "FullyQualifiedName~Appendix"
dotnet test  # 全テスト実行
```

---

## ✅ 完了条件

### 付録完了の定義

以下の条件をすべて満たすこと：

1. ✅ モデルクラスの削除
   - PlcConnectionConfig.cs
   - DeviceEntry.cs
   - MultiPlcConfig.cs
   - ParallelProcessingConfig.cs（あれば）

2. ✅ マネージャークラスの削除
   - MultiPlcCoordinator.cs
   - IMultiPlcCoordinator.cs（あれば）

3. ✅ ExecutionOrchestrator.cs の修正
   - ExecuteSinglePlcAsync()メソッド削除

4. ✅ テストコードの削除 or 修正
   - PlcConnectionConfigTests.cs 削除
   - MultiPlcConfigTests.cs 削除
   - MultiPlcCoordinatorTests.cs 削除
   - ExecutionOrchestratorTests.cs の該当テストケース削除

5. ✅ DI登録の削除（あれば）

6. ✅ Appendix_JsonConfigModels_DependencyTests.cs の全テストがパス

7. ✅ 全体テストがパス

8. ✅ ビルドエラーなし

### 確認コマンド

```bash
# 付録のテスト確認
dotnet test --filter "FullyQualifiedName~Appendix"

# 全体テスト確認
dotnet test

# ビルド確認
dotnet build
```

---

## 🚨 注意事項

### 1. PlcConnectionConfigの使用状況確認

**確認方法**:
```bash
# PlcConnectionConfigを使用している箇所を検索
grep -r "PlcConnectionConfig" andon/Core andon/Services andon/Infrastructure
```

**削除可能な条件**:
- Phase6で追加されたばかりで本格活用前
- ExecuteSinglePlcAsync()以外で使用されていない
- MultiPlcCoordinator以外で使用されていない

### 2. MultiPlcCoordinatorの削除タイミング

**判断基準**:
- **削除推奨**: 並列実行機能がPlcConfigurationベースで実装予定の場合
- **保留**: MultiPlcCoordinatorを使用した並列実行機能が既に運用されている場合

**保留する場合の対応**:
- PlcConnectionConfigをPlcConfigurationに変換するアダプターを実装
- MultiPlcCoordinatorをPlcConfiguration対応に修正

### 3. 削除時のテストコード修正

**影響を受けるテストコード**:
- ExecutionOrchestratorTests.cs
- 統合テスト（PlcConnectionConfigを使用している場合）

**修正内容**:
```csharp
// 修正前（PlcConnectionConfig使用）
var plcConnection = new PlcConnectionConfig
{
    IpAddress = "172.30.40.40",
    Port = 8192
};
await _orchestrator.ExecuteSinglePlcAsync(plcConnection);

// 修正後（PlcConfiguration使用）
var plcConfig = new PlcConfiguration
{
    IpAddress = "172.30.40.40",
    Port = 8192
};
await _orchestrator.RunDataCycleAsync(plcConfig);
```

---

## 📊 削除の影響評価

| 影響範囲 | 影響度 | 詳細 |
|---------|--------|------|
| **本番環境** | 低～中 | ExecuteSinglePlcAsync()が使用されている場合は中、そうでなければ低 |
| **テスト環境** | 低 | テストコードの修正が必要 |
| **並列実行機能** | 中 | MultiPlcCoordinatorを使用している場合は影響あり |
| **ビルド** | なし | ビルドエラーなし（削除後） |

---

## 📁 削除対象ファイル一覧

### モデルクラス
```
andon/Core/Models/ConfigModels/PlcConnectionConfig.cs
andon/Core/Models/ConfigModels/DeviceEntry.cs
andon/Core/Models/ConfigModels/MultiPlcConfig.cs
andon/Core/Models/ConfigModels/ParallelProcessingConfig.cs（あれば）
```

### マネージャークラス
```
andon/Core/Managers/MultiPlcCoordinator.cs
andon/Core/Interfaces/IMultiPlcCoordinator.cs（あれば）
```

### テストファイル
```
andon/Tests/Unit/Core/Models/ConfigModels/PlcConnectionConfigTests.cs
andon/Tests/Unit/Core/Models/ConfigModels/MultiPlcConfigTests.cs
andon/Tests/Unit/Core/Managers/MultiPlcCoordinatorTests.cs
```

### 修正対象ファイル
```
andon/Core/Controllers/ExecutionOrchestrator.cs - ExecuteSinglePlcAsync()削除
andon/Tests/Unit/Core/Controllers/ExecutionOrchestratorTests.cs - 該当テストケース削除
```

---

## 🔄 Phase 3との違い

| 項目 | Phase 3 | 付録 |
|------|---------|------|
| **削除対象** | appsettings.jsonファイル | PlcConnectionConfig関連クラス |
| **影響度** | 低（すべて移行済み） | 低～中（Phase6機能） |
| **必須度** | 必須（appsettings.json廃止完了に必要） | オプション（設計統一化のため推奨） |
| **作業内容** | ファイル削除、DI確認 | クラス削除、メソッド削除、テスト修正 |

---

## 📈 付録完了後の設計

### 設定管理の統一化

**削除前（2つのモデルが並存）**:
```
PlcConfiguration（Excel設定用）- ExecuteMultiPlcCycleAsync_Internal()で使用
PlcConnectionConfig（JSON設定用）- ExecuteSinglePlcAsync()で使用
```

**削除後（単一モデルに統一）**:
```
PlcConfiguration（唯一の設定モデル）- すべての機能で使用
```

### メリット

| 項目 | 詳細 |
|------|------|
| **設計統一** | PlcConfigurationのみを使用、保守性向上 |
| **コード削減** | PlcConnectionConfig関連の複雑性を削減 |
| **テスト簡素化** | 単一モデルのみをテストすればOK |
| **拡張容易** | 将来的な拡張もPlcConfigurationの範囲内で実施 |

---

## 🎉 完了メッセージ

**付録完了により、設計がExcel設定ベース（PlcConfiguration）に完全統一されました！**

### 達成したこと

✅ **JSON設定用モデルの完全削除**
- PlcConnectionConfig削除
- DeviceEntry削除
- MultiPlcConfig削除
- MultiPlcCoordinator削除

✅ **設計の単一化**
- PlcConfigurationのみを使用
- Excel設定ベースに統一
- 保守性大幅向上

✅ **Phase 0～Phase 3 + 付録の累積成果**
- appsettings.json完全廃止
- JSON設定用モデル完全削除
- Excel設定とハードコード値のみで動作
- 設計の単一化・簡素化

### 次の推奨アクション

1. ドキュメント最終更新
2. 本番環境デプロイ
3. 運用マニュアル更新

---

## 🔗 関連文書

- [Phase3_appsettings完全廃止.md](./Phase3_appsettings完全廃止.md)
- [00_実装計画概要.md](./00_実装計画概要.md)
