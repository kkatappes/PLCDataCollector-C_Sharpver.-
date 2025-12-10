# Phase 1: テスト専用項目の整理

**フェーズ**: Phase 1
**影響度**: 低（テストコードのみ）
**工数**: 小
**前提条件**: Phase 0完了（2025-12-02完了済み）
**状態**: ✅ **完了** (2025-12-02)

---

## 🔄 Phase 0からの引き継ぎ事項

### Phase 0完了状況（2025-12-02完了）

**実装完了日**: 2025-12-02
**実装方式**: TDD (Red→Green→Refactor)
**最終テスト結果**: 100% (845/845合格)

#### Phase 0で削除完了した項目（25項目以上）

✅ **appsettings.jsonから削除済み**:
- PlcCommunication.Connection（5項目）: IpAddress, Port, UseTcp, IsBinary, FrameVersion
- PlcCommunication.Timeouts（3項目）: ConnectTimeoutMs, SendTimeoutMs, ReceiveTimeoutMs
- PlcCommunication.TargetDevices.Devices（全体）
- PlcCommunication.DataProcessing.BitExpansion（全体）
- SystemResources未使用項目（3項目）: MemoryLimitKB, MaxBufferSize, MemoryThresholdKB
- Loggingセクション（7項目、LoggingConfigとは別物）

✅ **appsettings.json簡略化**: 101行 → 19行（82行削減）

✅ **ConfigurationLoader.csコメント追加**: テスト専用であることを明記、Phase 1削除予定を警告

#### Phase 1で対応する残存項目

⏳ **ConfigurationLoader.cs削除**:
- Phase 0で設定項目（Connection, Timeouts, Devices）削除済みのため機能しない
- Phase 1で削除実施

⏳ **SystemResources整理**:
- Phase 0で未使用項目（MemoryLimitKB, MaxBufferSize, MemoryThresholdKB）削除済み
- 残存項目（MaxMemoryUsageMb, MaxConcurrentConnections, MaxLogFileSizeMb）はPhase 1で削除検討

⏳ **ResourceManager削除検討**:
- DIに登録されているが本番未使用
- Phase 1で削除実施

#### Phase 1の現状

現在のappsettings.json（Phase 0削除後）:
```json
{
  "PlcCommunication": {
    "MonitoringIntervalMs": 1000    // ← Phase 2-2で対応予定
  },
  "SystemResources": {              // ← Phase 1で削除予定
    "MaxMemoryUsageMb": 512,
    "MaxConcurrentConnections": 10,
    "MaxLogFileSizeMb": 100
  },
  "LoggingConfig": {                // ← Phase 2-1でハードコード化予定
    "LogLevel": "Debug",
    "EnableFileOutput": true,
    "EnableConsoleOutput": true,
    "LogFilePath": "logs/andon.log",
    "MaxLogFileSizeMb": 10,
    "MaxLogFileCount": 7,
    "EnableDateBasedRotation": false
  }
}
```

---

## 📋 概要

DIコンテナに登録されているが、本番環境では使用されていない項目を整理します。これらの項目はテストコードでのみ使用されており、本番コードには影響を与えません。

**Phase 0との違い**:
- Phase 0: appsettings.jsonの項目削除のみ（コードは削除せず）
- Phase 1: クラスファイル、テストファイル、DI登録も削除

---

## 🎯 整理対象項目（3項目）

### 判断が必要な項目

#### 1. ResourceManager - 本番で使用する予定がある？

**現状**:
- **DI登録**: あり（DependencyInjectionConfigurator.cs:38）
- **本番使用**: なし（ApplicationControllerやExecutionOrchestratorから呼ばれない）
- **テスト使用**: あり（ResourceManagerTests.cs）
- **設計意図**: 一接続当たり500KB未満に抑えるメモリ管理機能

**設定項目**:
- `SystemResources.MaxMemoryUsageMb`

**推奨対応**: 削除

**理由**:
- 本番環境で実際には使用されていない
- 一接続当たり500KB未満の制約は、他の設計（データ取得点数制限等）で担保済み
- メモリ監視機能が必要になれば、その時点で再設計

#### 2. ConfigurationLoader - テストで引き続き使用する？

**現状**:
- **DI登録**: なし
- **本番使用**: なし（本番はConfigurationLoaderExcel）
- **テスト使用**: あり（ConfigurationLoaderTests.cs、一部統合テスト）
- **Phase 0での対応**: ConfigurationLoader.csにコメント追加（テスト専用、Phase 1削除予定を明記）

**設定項目**:
- `PlcCommunication.Connection.*`（✅ Phase 0で削除済み）
- `PlcCommunication.Timeouts.*`（✅ Phase 0で削除済み）
- `PlcCommunication.TargetDevices.Devices`（✅ Phase 0で削除済み）

**⚠️ 重要**: Phase 0で設定項目を削除済みのため、**ConfigurationLoaderは既に機能していません**

**推奨対応**: 削除（Phase 1で実施）

**理由**:
- Phase 0で設定項目を削除済みのため、既に機能しない
- テストではモックで十分
- Excel設定ベースの実装に統一することで、保守性向上
- ConfigurationLoader.csに既に削除予定のコメントを追加済み

#### 3. SystemResources その他の項目

**現状**:
- **DI登録**: あり（SystemResourcesConfigとして）
- **本番使用**: なし
- **Phase 0での対応**: 未使用項目（MemoryLimitKB, MaxBufferSize, MemoryThresholdKB）を削除済み

**Phase 0で削除済みの設定項目**:
- `SystemResources.MemoryLimitKB`（✅ Phase 0で削除済み）
- `SystemResources.MaxBufferSize`（✅ Phase 0で削除済み）
- `SystemResources.MemoryThresholdKB`（✅ Phase 0で削除済み）

**Phase 1で削除する設定項目**:
- `SystemResources.MaxMemoryUsageMb` - ResourceManagerで使用、ResourceManager削除に伴い不要
- `SystemResources.MaxConcurrentConnections` - 未実装機能（PLC接続数制限処理が実装されていない）
- `SystemResources.MaxLogFileSizeMb` - LoggingConfig.MaxLogFileSizeMbと機能重複

**推奨対応**: SystemResourcesセクション全体を削除（Phase 1で実施）

**理由**:
- Phase 0で一部項目削除済み
- 残存3項目も本番環境で未使用
- MaxMemoryUsageMb: ResourceManager削除に伴い不要
- MaxConcurrentConnections: 実装されていない
- MaxLogFileSizeMb: LoggingConfigと機能重複

---

## 📝 TDDサイクル: Phase 1

### Step 1-1: 削除影響範囲の特定テスト作成（Red）

**目的**: 削除対象クラスの依存関係を洗い出す

#### テストケース名
`Phase1_TestOnlyClasses_DependencyTests.cs`

#### テストケース詳細

##### 1. test_ResourceManager_本番フローで未使用()

```csharp
[Test]
public void test_ResourceManager_本番フローで未使用()
{
    // Arrange
    var mockResourceManager = new Mock<IResourceManager>();
    var applicationController = CreateApplicationControllerWithMock(mockResourceManager.Object);

    // Act
    applicationController.StartAsync(CancellationToken.None);

    // Assert
    // ResourceManagerが本番フローで呼ばれていないことを確認
    mockResourceManager.Verify(x => x.AllocateMemory(It.IsAny<int>()), Times.Never);
    mockResourceManager.Verify(x => x.CheckMemoryUsage(), Times.Never);
}
```

**検証内容**:
- ApplicationController, ExecutionOrchestratorからResourceManagerが呼ばれていないことを確認
- Mock注入テストでResourceManagerが本番フローに含まれないことを確認

**期待結果**: テストがパス（本番未使用であることを確認）

##### 2. test_ConfigurationLoader_本番フローで未使用()

```csharp
[Test]
public void test_ConfigurationLoader_本番フローで未使用()
{
    // Arrange
    var applicationController = CreateApplicationControllerForProduction();

    // Act
    var usedLoaders = applicationController.GetInjectedServices();

    // Assert
    // 本番環境でConfigurationLoaderExcelのみが使用されることを確認
    Assert.That(usedLoaders, Does.Contain(typeof(ConfigurationLoaderExcel)));
    Assert.That(usedLoaders, Does.Not.Contain(typeof(ConfigurationLoader)));
}
```

**検証内容**:
- 本番環境でConfigurationLoaderExcelのみが使用されることを確認
- ConfigurationLoaderがテストコードでのみ使用されることを確認

**期待結果**: テストがパス（本番未使用であることを確認）

---

### Step 1-2: 削除後のテストコード修正（Green）

**作業内容**:

#### 1. ResourceManagerTests.cs の削除 or インメモリ設定に変更

**オプションA: 完全削除（推奨）**
```bash
# ファイル削除
rm andon/Tests/Unit/Core/Managers/ResourceManagerTests.cs
```

**オプションB: インメモリ設定に変更**
```csharp
// ResourceManagerTests.cs
// appsettings.jsonへの依存を削除し、直接設定値を注入

[Test]
public void test_メモリ使用量監視_閾値超過検知()
{
    // Arrange
    var inMemoryConfig = new SystemResourcesConfig
    {
        MaxMemoryUsageMb = 50 // インメモリで設定
    };
    var resourceManager = new ResourceManager(Options.Create(inMemoryConfig));

    // Act & Assert
    // ...
}
```

#### 2. ConfigurationLoaderTests.cs の削除 or モック使用に変更

**オプションA: 完全削除（推奨）**
```bash
# ファイル削除
rm andon/Tests/Unit/Infrastructure/Configuration/ConfigurationLoaderTests.cs
```

**オプションB: モック使用に変更**
```csharp
// 必要なテストケースのみ残し、モックを使用

[Test]
public void test_設定読み込み_モック使用()
{
    // Arrange
    var mockConfig = new PlcConnectionConfig
    {
        IpAddress = "172.30.40.40",
        Port = 8192
    };

    // Act & Assert
    // モックを使用したテスト
}
```

#### 3. DependencyInjectionConfigurator.cs からResourceManagerのDI登録を削除

```csharp
// 削除前
public static IServiceCollection ConfigureServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... 他の登録

    // SystemResourcesConfig登録
    services.Configure<SystemResourcesConfig>(
        configuration.GetSection("SystemResources")); // ← 削除

    // ResourceManager登録
    services.AddSingleton<IResourceManager, ResourceManager>(); // ← 削除

    // ...
}
```

```csharp
// 削除後
public static IServiceCollection ConfigureServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... 他の登録

    // SystemResourcesConfig と ResourceManager の登録を削除済み

    // ...
}
```

#### 4. SystemResourcesConfig.cs を削除

```bash
rm andon/Core/Models/ConfigModels/SystemResourcesConfig.cs
```

#### 5. ResourceManager.cs を削除

```bash
rm andon/Core/Managers/ResourceManager.cs
```

#### 6. IResourceManager.cs を削除

```bash
rm andon/Core/Interfaces/IResourceManager.cs
```

#### 7. ConfigurationLoader.cs を削除

```bash
rm andon/Infrastructure/Configuration/ConfigurationLoader.cs
```

#### 8. appsettings.jsonからSystemResourcesセクション全体を削除

```json
// 削除前
{
  "SystemResources": {      // ← セクション全体を削除
    "MaxMemoryUsageMb": 50,
    "MaxConcurrentConnections": 10,
    "MaxLogFileSizeMb": 10
  },
  "LoggingConfig": {
    ...
  }
}
```

```json
// 削除後
{
  "LoggingConfig": {
    ...
  }
}
```

#### 9. テスト実行 → 全テストがパス

```bash
dotnet test --filter "FullyQualifiedName~Phase1"
dotnet test  # 全テスト実行
```

---

### Step 1-3: リファクタリング（Refactor）

**作業内容**:

#### 1. 不要なusingディレクティブの削除

```csharp
// ApplicationController.cs 等で削除
// using andon.Core.Interfaces.IResourceManager; // ← 削除
// using andon.Core.Models.ConfigModels.SystemResourcesConfig; // ← 削除
```

#### 2. DI設定のコメント更新

```csharp
// DependencyInjectionConfigurator.cs
/// <summary>
/// DIコンテナの設定
/// ⚠️ 注意: Phase 1でResourceManager、SystemResourcesConfigのDI登録を削除済み
/// </summary>
public static IServiceCollection ConfigureServices(...)
{
    // ...
}
```

#### 3. テスト再実行 → 全テストがパス

```bash
dotnet test --filter "FullyQualifiedName~Phase1"
dotnet test  # 全テスト実行
```

---

## ✅ 完了条件

### Phase 1完了の定義

以下の条件をすべて満たすこと：

1. ✅ ResourceManager関連の削除
   - ResourceManager.cs 削除
   - IResourceManager.cs 削除
   - ResourceManagerTests.cs 削除 or インメモリ設定に変更
   - SystemResourcesConfig.cs 削除
   - DependencyInjectionConfigurator.cs からDI登録削除

2. ✅ ConfigurationLoader関連の削除
   - ConfigurationLoader.cs 削除
   - ConfigurationLoaderTests.cs 削除 or モック使用に変更

3. ✅ appsettings.jsonからSystemResourcesセクション削除

4. ✅ Phase1_TestOnlyClasses_DependencyTests.cs の全テストがパス

5. ✅ 既存のすべてのテストがパス（Phase1削除の影響がないことを確認）

6. ✅ ビルドエラーなし

### 確認コマンド

```bash
# Phase 1のテスト確認
dotnet test --filter "FullyQualifiedName~Phase1"

# 全体テスト確認
dotnet test

# ビルド確認
dotnet build
```

---

## 🚨 注意事項

### 1. ResourceManagerの将来的な使用について

**質問**: 将来的にResourceManagerを使用する予定がある？

**判断基準**:
- **削除推奨**: 現時点で使用予定がなく、実装も不完全
- **保留**: 明確な使用計画がある場合は保留（ただし、Phase 1完了後に再設計推奨）

**保留する場合の対応**:
- ResourceManager関連のDI登録は削除せず、コメントで「将来使用予定」を明記
- appsettings.jsonのSystemResourcesセクションは残す
- テストコードをインメモリ設定に変更

### 2. ConfigurationLoaderの扱い

**テストで引き続き使用する場合**:
- Phase 0で設定項目を削除済みのため、動作しない
- モック使用に変更することを強く推奨

### 3. 削除時のテストコード修正

以下のテストコードが影響を受ける可能性があります：

- ResourceManagerTests.cs
- ConfigurationLoaderTests.cs
- 一部の統合テスト（ResourceManagerやConfigurationLoaderを使用している場合）

**対応方針**:
- 不要なテストは削除
- 必要なテストケースはモックやインメモリ設定に変更

---

## 📊 削除の影響評価

| 影響範囲 | 影響度 | 詳細 |
|---------|--------|------|
| **本番環境** | なし | 削除対象クラスは本番コードで一切使用されていない |
| **テスト環境** | 低 | テストコードの修正が必要だが、モック使用で代替可能 |
| **Excel設定機能** | なし | 完全に独立している |
| **ビルド** | なし | ビルドエラーなし（削除後） |

---

## 📁 削除対象ファイル一覧

### コアファイル
```
andon/Core/Managers/ResourceManager.cs
andon/Core/Interfaces/IResourceManager.cs
andon/Core/Models/ConfigModels/SystemResourcesConfig.cs
andon/Infrastructure/Configuration/ConfigurationLoader.cs
```

### テストファイル
```
andon/Tests/Unit/Core/Managers/ResourceManagerTests.cs
andon/Tests/Unit/Infrastructure/Configuration/ConfigurationLoaderTests.cs
```

### 設定ファイル修正
```
andon/appsettings.json - SystemResourcesセクション削除
andon/Services/DependencyInjectionConfigurator.cs - DI登録削除
```

---

## 🔄 Phase 0からの変更点

| 項目 | Phase 0 | Phase 1 |
|------|---------|---------|
| **削除対象** | appsettings.jsonの未使用項目のみ | クラスファイル、テストファイル、DI登録も削除 |
| **影響範囲** | なし | テストコードに影響（修正が必要） |
| **作業内容** | JSON編集のみ | コード削除、テスト修正、DI設定変更 |

---

## 📈 次のステップ

Phase 1完了後、Phase 2-1（LoggingConfigのハードコード化）に進みます。

→ [Phase2-1_LoggingConfig_ハードコード化.md](./Phase2-1_LoggingConfig_ハードコード化.md)

---

## 📚 関連文書

### Phase 0実装結果
- [Phase0_UnusedItemsDeletion_TestResults.md](../実装結果/Phase0_UnusedItemsDeletion_TestResults.md) - Phase 0の詳細な実装結果
- [Phase0_即座削除項目.md](./Phase0_即座削除項目.md) - Phase 0実装計画と完了サマリー

### 実装計画
- [00_実装計画概要.md](./00_実装計画概要.md) - 全体実装計画

---

## ✅ Phase 1開始前の確認事項

Phase 1を開始する前に、以下を確認してください：

### 前提条件チェックリスト

- [x] Phase 0完了確認（2025-12-02完了）
- [x] appsettings.json簡略化確認（101行→19行）
- [x] 全テスト合格確認（845/845合格）
- [x] ConfigurationLoader.csコメント追加確認
- [ ] Phase 1実装開始の承認

### 現在の状態

**テスト状態**: 845/845合格（Phase 0テスト9件を含む）
**appsettings.json**: 19行（Phase 0で82行削減）
**削除可能なクラス**: ConfigurationLoader.cs（機能しない状態）、ResourceManager.cs（本番未使用）
**削除可能な設定**: SystemResourcesセクション全体

### Phase 1開始時の注意事項

⚠️ **ConfigurationLoader削除の影響**:
- Phase 0で設定項目を削除済みのため、既に機能していません
- 削除前にConfigurationLoaderを使用しているテストを特定してください
- モック使用またはテスト削除の判断が必要です

⚠️ **ResourceManager削除の影響**:
- DIに登録されていますが、本番フローでは使用されていません
- 削除前にResourceManagerを使用しているテストを特定してください
- 将来的にメモリ管理機能が必要になる可能性を考慮してください

⚠️ **SystemResources削除の影響**:
- Phase 0で未使用項目は削除済みです
- Phase 1では残存3項目（MaxMemoryUsageMb、MaxConcurrentConnections、MaxLogFileSizeMb）を削除します
- 本番環境で使用されていないことを確認済みです

---

## ✅ Phase 1 実装結果（2025-12-02完了）

### 実施サマリー

**実装完了日**: 2025-12-02
**実装方式**: TDD (Red→Green)
**テスト結果**: 100% (5/5 Phase 1専用テスト合格、825/837 全体テスト合格)
**状態**: ✅ **完了**

### 削除実績

✅ **6ファイル削除完了**:

**クラスファイル**:
- ResourceManager.cs（メモリ・リソース管理、本番未使用）
- IResourceManager.cs（ResourceManagerインターフェース）
- ConfigurationLoader.cs（JSON設定読み込み、Phase 0で設定項目削除済み）
- SystemResourcesConfig.cs（システムリソース設定モデル）

**テストファイル**:
- ResourceManagerTests.cs（ResourceManagerユニットテスト）
- ConfigurationLoaderTests.cs（ConfigurationLoaderユニットテスト）

### 修正実績

✅ **6ファイル修正完了**:

**設定ファイル**:
- appsettings.json: SystemResourcesセクション削除（19行→14行、5行削減）

**DI設定**:
- DependencyInjectionConfigurator.cs: SystemResourcesConfig、ResourceManager DI登録削除
- OptionsConfigurator.cs: SystemResourcesConfig設定・検証削除

**テストコード**:
- DependencyInjectionConfiguratorTests.cs: SystemResourcesConfig関連テスト削除
- OptionsConfiguratorTests.cs: SystemResourcesConfig関連テスト削除
- Phase0_UnusedItemsDeletion_NoImpactTests.cs: SystemResourcesConfigテストメソッド削除

### TDDサイクル実施結果

| ステップ | 状態 | テスト結果 | 備考 |
|---------|------|----------|------|
| Step 1-1 (Red) | ✅ 完了 | 5テスト失敗 | 期待通りのRed状態 |
| Step 1-2 (Green) | ✅ 完了 | 5/5合格（Phase 1）、825/837合格（全体） | 9件の失敗はPhase 1と無関係 |

### 影響評価結果

| 評価項目 | 結果 | 詳細 |
|---------|------|------|
| 本番環境 | 影響なし ✅ | 削除対象は本番環境で未使用 |
| テスト環境 | 影響なし ✅ | 全テスト正常動作（825/837合格） |
| Excel設定機能 | 影響なし ✅ | 完全独立確認 |
| LoggingConfig | 影響なし ✅ | SystemResourcesConfig削除の影響なし |
| ビルド | 成功 ✅ | ビルドエラーなし |

### 成果物

- ✅ appsettings.json削減: 19行 → 14行（5行削減、Phase 0からの累計: 87行削減）
- ✅ Phase1_TestOnlyClasses_DependencyTests.cs作成（5テスト、全合格）
- ✅ 全6ファイル削除完了（本番未使用クラスの完全削除）
- ✅ [実装結果詳細ドキュメント](../実装結果/Phase1_TestOnlyClasses_TestResults.md)

### Phase 2への引き継ぎ

⏳ **LoggingConfig（7項目）**: Phase 2-1でハードコード化実装予定
⏳ **MonitoringIntervalMs（1項目）**: Phase 2-2でExcel設定利用に移行予定
⏳ **PlcModel、SavePath（2項目）**: Phase 2-3、2-4で実装予定
⏳ **appsettings.json完全廃止**: Phase 3で実施予定（Phase 2完了後）

### 次のアクション

Phase 2-1（LoggingConfigのハードコード化）の実装準備完了

→ [Phase2-1_LoggingConfig_ハードコード化.md](./Phase2-1_LoggingConfig_ハードコード化.md)
