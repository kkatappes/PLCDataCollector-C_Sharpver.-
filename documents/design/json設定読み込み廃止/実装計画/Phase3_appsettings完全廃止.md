# Phase 3: appsettings.json完全廃止

**フェーズ**: Phase 3
**影響度**: 低（すべての移行が完了しているため）
**工数**: 小
**前提条件**: Phase 0, Phase 1, Phase 2-1, Phase 2-2, Phase 2-3, Phase 2-4完了

---

## 📋 概要

appsettings.jsonファイルを完全に削除し、Excel設定とハードコード値のみでアプリケーションを動作させます。Phase 0～Phase 2-4ですべての項目の移行が完了しているため、影響は最小限です。

---

## 🎯 作業内容

| 作業項目 | 詳細 | 影響度 |
|---------|------|--------|
| **appsettings.json削除** | すべての環境から削除 | 低 |
| **IConfiguration依存の確認** | 不要な依存を削除 | 低 |
| **DI設定の最終確認** | 不要なconfiguration参照を削除 | 低 |
| **統合テスト** | appsettings.json無しで全機能が動作することを確認 | 低 |

---

## 📝 TDDサイクル: Phase 3

### Step 3-1: 完全廃止後の統合テスト作成（Red）

**目的**: appsettings.json無しで全機能が正常動作することを確認

#### テストケース名
`Phase3_CompleteRemoval_IntegrationTests.cs`

#### テストケース詳細

##### 1. test_アプリケーション起動_appsettings無し()

```csharp
[Test]
public async Task test_アプリケーション起動_appsettings無し()
{
    // Arrange
    // appsettings.jsonが存在しない状態を再現
    var host = CreateHostWithoutAppsettings();

    // Act
    var startResult = await host.StartAsync();

    // Assert
    Assert.That(startResult, Is.Not.Null);
    // 正常起動、エラーログなし
    _mockLoggingManager.Verify(
        x => x.LogError(It.Is<string>(s => s.Contains("appsettings"))),
        Times.Never
    );
}
```

##### 2. test_PLC通信_appsettings無し()

```csharp
[Test]
public async Task test_PLC通信_appsettings無し()
{
    // Arrange
    var plcConfig = LoadPlcConfigFromExcel(); // Excel設定のみ使用
    var orchestrator = CreateOrchestratorWithoutAppsettings();

    // Act
    var result = await orchestrator.RunDataCycleAsync(plcConfig);

    // Assert
    Assert.That(result.Success, Is.True);
    // Step3-6の全処理が正常実行
    Assert.That(result.Step3_ConnectSuccess, Is.True);
    Assert.That(result.Step4_SendSuccess, Is.True);
    Assert.That(result.Step5_ReceiveSuccess, Is.True);
    Assert.That(result.Step6_DisconnectSuccess, Is.True);
}
```

##### 3. test_ログ出力_appsettings無し()

```csharp
[Test]
public void test_ログ出力_appsettings無し()
{
    // Arrange
    // appsettings.json無しでLoggingManagerを作成（ハードコード値使用）
    var loggingManager = new LoggingManager();

    // Act
    loggingManager.LogInfo("Test message");

    // Assert
    // ハードコード値でログ出力が正常動作
    Assert.That(File.Exists("./logs/log.txt"), Is.True);

    var logContent = File.ReadAllText("./logs/log.txt");
    Assert.That(logContent, Does.Contain("Test message"));
}
```

##### 4. test_複数PLC並列実行_appsettings無し()

```csharp
[Test]
public async Task test_複数PLC並列実行_appsettings無し()
{
    // Arrange
    var plcConfigs = LoadAllPlcConfigsFromExcel(); // Excel設定のみ使用
    var orchestrator = CreateOrchestratorWithoutAppsettings();

    // Act
    var result = await orchestrator.ExecuteMultiPlcCycleAsync_Internal(plcConfigs);

    // Assert
    Assert.That(result.Success, Is.True);
    // 各PLCが独立した監視間隔で動作
    foreach (var plcResult in result.PlcResults)
    {
        Assert.That(plcResult.Success, Is.True);
    }
}
```

##### 5. test_MonitoringIntervalMs_Excel設定値使用()

```csharp
[Test]
public async Task test_MonitoringIntervalMs_Excel設定値使用()
{
    // Arrange
    var plcConfig = new PlcConfiguration
    {
        MonitoringIntervalMs = 5000 // Excel設定値
    };
    var orchestrator = CreateOrchestratorWithoutAppsettings();

    // Act
    await orchestrator.RunContinuousDataCycleAsync(plcConfig);

    // Assert
    // Excel設定の値（5000ms）でタイマーが動作
    var actualInterval = _mockTimerService.LastInterval;
    Assert.That(actualInterval, Is.EqualTo(TimeSpan.FromMilliseconds(5000)));
}
```

##### 6. test_PlcModel_JSON出力()

```csharp
[Test]
public async Task test_PlcModel_JSON出力()
{
    // Arrange
    var plcConfig = new PlcConfiguration
    {
        PlcModel = "5_JRS_N2" // Excel設定値
    };
    var orchestrator = CreateOrchestratorWithoutAppsettings();

    // Act
    await orchestrator.RunDataCycleAsync(plcConfig);

    // Assert
    // PlcModelがJSON出力に含まれる
    var jsonContent = File.ReadAllText("./output/data.json");
    Assert.That(jsonContent, Does.Contain("\"plcModel\": \"5_JRS_N2\""));
}
```

##### 7. test_SavePath_Excel設定値使用()

```csharp
[Test]
public async Task test_SavePath_Excel設定値使用()
{
    // Arrange
    var plcConfig = new PlcConfiguration
    {
        SavePath = "./custom/output" // Excel設定値
    };
    var orchestrator = CreateOrchestratorWithoutAppsettings();

    // Act
    await orchestrator.RunDataCycleAsync(plcConfig);

    // Assert
    // Excel設定の値でデータが出力される
    Assert.That(Directory.Exists("./custom/output"), Is.True);
    var jsonFiles = Directory.GetFiles("./custom/output", "*.json");
    Assert.That(jsonFiles.Length, Is.GreaterThan(0));
}
```

#### 期待される結果
Step 3-2の実装前は失敗（appsettings.json依存があるため）

---

### Step 3-2: 実装（Green）

**作業内容**:

#### 1. appsettings.json ファイルを削除（すべての環境から）

```bash
# 本番環境用
rm andon/appsettings.json

# 開発環境用
rm andon/appsettings.Development.json

# その他の環境用
rm andon/appsettings.Production.json
rm andon/appsettings.Staging.json
# 等、環境別設定ファイルがあれば削除
```

#### 2. Program.cs の確認

**重要**: Host.CreateDefaultBuilder(args)は appsettings.json不在でもエラーにならない

```csharp
// andon/Program.cs

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args) // ← appsettings.json不在でもOK
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<AndonHostedService>();
                services.ConfigureServices(hostContext.Configuration);
            });
}
```

**確認事項**:
- Host.CreateDefaultBuilder(args)は自動的にappsettings.jsonを探すが、無くてもエラーにならない
- IConfigurationは空の状態で作成される
- 不要なIConfiguration依存がないか確認

#### 3. DI設定の最終確認

```csharp
// andon/Services/DependencyInjectionConfigurator.cs

public static IServiceCollection ConfigureServices(
    this IServiceCollection services,
    IConfiguration configuration) // ← IConfigurationは引数で受け取るが使用しない
{
    // Phase 0-2で以下のConfigure<T>呼び出しは削除済み
    // services.Configure<LoggingConfig>(...) - Phase 2-1で削除
    // services.Configure<DataProcessingConfig>(...) - Phase 2-2で削除
    // services.Configure<SystemResourcesConfig>(...) - Phase 1で削除

    // Singleton登録（IOptions依存なし）
    services.AddSingleton<ILoggingManager, LoggingManager>();
    services.AddSingleton<IExecutionOrchestrator, ExecutionOrchestrator>();
    services.AddSingleton<IDataOutputManager, DataOutputManager>();
    // 等...

    return services;
}
```

**確認事項**:
- IConfiguration引数が実際に使用されていないことを確認
- 不要な場合は引数を削除（オプション）

**オプション: IConfiguration引数を削除**
```csharp
// 修正前
public static IServiceCollection ConfigureServices(
    this IServiceCollection services,
    IConfiguration configuration) // ← 使用していない
{
    // ...
}

// 修正後
public static IServiceCollection ConfigureServices(
    this IServiceCollection services)
{
    // ...
}

// Program.cs も修正
.ConfigureServices((hostContext, services) =>
{
    services.AddHostedService<AndonHostedService>();
    services.ConfigureServices(); // configuration引数を削除
});
```

#### 4. テスト実行 → 全テストがパス

```bash
dotnet test --filter "FullyQualifiedName~Phase3"
dotnet test  # 全テスト実行
```

---

### Step 3-3: リファクタリング（Refactor）

**作業内容**:

#### 1. コメント・ドキュメントの更新

**README.md更新**:
```markdown
# andon

## 設定ファイル

本アプリケーションはExcel設定ファイルのみを使用します。appsettings.jsonは不要です。

### Excel設定ファイル

`settings.xlsx`に以下の設定を記載してください：

- **settingsシート**:
  - B8: PLCのIPアドレス
  - B9: PLCのポート
  - B10: 接続方式（TCP/UDP）
  - B11: データ取得周期(ms)
  - B12: デバイス名（PLCモデル）
  - B13: データ保存先パス
  - B15: PLC名称

- **データ収集デバイスシート**:
  - デバイスリスト定義

詳細は `documents/design/Step1_設定ファイル読み込み実装/設定ファイル内容.md` を参照してください。
```

**各クラスのXMLコメント更新**:
```csharp
// LoggingManager.cs
/// <summary>
/// ログ管理クラス（ハードコード化版）
/// appsettings.json不要、固定値で動作
/// </summary>
public class LoggingManager : ILoggingManager
{
    // ...
}

// ExecutionOrchestrator.cs
/// <summary>
/// 実行オーケストレータ（Excel設定ベース）
/// appsettings.json不要、Excel設定とハードコード値で動作
/// </summary>
public class ExecutionOrchestrator : IExecutionOrchestrator
{
    // ...
}
```

#### 2. 不要なNuGetパッケージの削除確認

**確認対象**:
```xml
<!-- andon/andon.csproj -->

<!-- Microsoft.Extensions.Configuration.Json が不要か確認 -->
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="x.x.x" />

<!-- 他のIConfiguration関連パッケージも確認 -->
```

**注意**:
- Host.CreateDefaultBuilder()が内部的に使用している可能性があるため、削除前に動作確認
- 削除してもビルドエラーが出ないことを確認

#### 3. テスト再実行 → 全テストがパス

```bash
dotnet test --filter "FullyQualifiedName~Phase3"
dotnet test  # 全テスト実行
```

---

## ✅ 完了条件

### Phase 3完了の定義

以下の条件をすべて満たすこと：

1. ✅ appsettings.jsonファイルを削除（すべての環境）

2. ✅ Program.csの確認
   - Host.CreateDefaultBuilder(args)はappsettings.json不在でもエラーにならないことを確認

3. ✅ DI設定の最終確認
   - 不要なIConfiguration依存を削除（オプション）

4. ✅ Phase3_CompleteRemoval_IntegrationTests.cs の全テストがパス

5. ✅ 全体テストがパス

6. ✅ ビルドエラーなし

7. ✅ ドキュメント更新
   - README.md更新
   - 各クラスのXMLコメント更新

### 確認コマンド

```bash
# Phase 3のテスト確認
dotnet test --filter "FullyQualifiedName~Phase3"

# 全体テスト確認
dotnet test

# ビルド確認
dotnet build

# 本番環境での起動確認
dotnet run --project andon/andon.csproj
```

---

## 🚨 注意事項

### 1. appsettings.json削除の確認

**削除対象ファイル**:
```
andon/appsettings.json
andon/appsettings.Development.json
andon/appsettings.Production.json
andon/appsettings.Staging.json
```

**削除前の確認**:
```bash
# appsettings.jsonファイルの一覧を確認
find andon -name "appsettings*.json"
```

### 2. IConfiguration依存の残存確認

**確認方法**:
```bash
# IConfigurationを使用している箇所を検索
grep -r "IConfiguration" andon/Core andon/Services andon/Infrastructure
```

**残っていても問題ない箇所**:
- DependencyInjectionConfigurator.cs:ConfigureServices()の引数（使用していなければ削除推奨）
- Program.cs（Host.CreateDefaultBuilder内部で使用）

### 3. バックアップの作成

**推奨**:
```bash
# appsettings.json削除前にバックアップを作成
cp andon/appsettings.json andon/appsettings.json.bak

# 動作確認後、バックアップを削除
rm andon/appsettings.json.bak
```

---

## 📊 Phase 3完了後の状態

### 削除されたファイル（Phase 0～3の累積）

#### 設定ファイル
```
andon/appsettings.json
andon/appsettings.Development.json（あれば）
andon/appsettings.Production.json（あれば）
```

#### モデルクラス（Phase 1, 2-1, 2-2で削除）
```
andon/Core/Models/ConfigModels/LoggingConfig.cs
andon/Core/Models/ConfigModels/DataProcessingConfig.cs
andon/Core/Models/ConfigModels/SystemResourcesConfig.cs
```

#### マネージャークラス（Phase 1で削除）
```
andon/Core/Managers/ResourceManager.cs
andon/Core/Interfaces/IResourceManager.cs
```

#### 設定読み込みクラス（Phase 1で削除）
```
andon/Infrastructure/Configuration/ConfigurationLoader.cs
```

### 残っているファイル（Excel設定ベース）

#### Excel設定読み込み
```
andon/Infrastructure/Configuration/ConfigurationLoaderExcel.cs（使用中）
andon/Core/Models/ConfigModels/PlcConfiguration.cs（使用中）
```

#### マネージャークラス（ハードコード化/Excel設定ベース）
```
andon/Core/Managers/LoggingManager.cs（ハードコード値使用）
andon/Core/Managers/DataOutputManager.cs（Excel設定使用）
andon/Core/Controllers/ExecutionOrchestrator.cs（Excel設定使用）
```

---

## 🔄 Phase 0～Phase 2-4との違い

| フェーズ | 作業内容 | 影響度 | 本番環境への影響 |
|---------|---------|--------|---------------|
| **Phase 0** | 未使用項目削除（JSON編集） | なし | なし |
| **Phase 1** | テスト専用項目削除（クラス削除） | 低 | なし（テストのみ） |
| **Phase 2-1** | LoggingConfigハードコード化 | 高 | あり（ログ機能） |
| **Phase 2-2** | MonitoringIntervalMs Excel移行 | 中 | あり（タイマー間隔） |
| **Phase 2-3** | PlcModel JSON出力実装 | 中 | あり（JSON出力） |
| **Phase 2-4** | SavePath利用実装 | 中 | あり（保存先パス） |
| **Phase 3** | **appsettings.json完全削除** | **低** | **なし（すべて移行済み）** |

---

## 📈 Phase 3完了後の次のステップ

Phase 3完了後、appsettings.json廃止は完了です。

### 追加作業（オプション）

#### 1. JSON設定用モデルの削除

Phase 6で追加されたJSON設定専用モデル（PlcConnectionConfig等）の削除を実施します。

→ [付録_JSON設定用モデル削除計画.md](./付録_JSON設定用モデル削除計画.md)

#### 2. ドキュメント最終更新

- プロジェクト全体のドキュメント更新
- デプロイ手順の更新（appsettings.json不要を明記）
- 運用マニュアルの更新

#### 3. 本番環境デプロイ

- Phase 0～Phase 3の変更を本番環境にデプロイ
- 動作確認（Excel設定のみで正常動作すること）
- ログ出力確認（ハードコード値で正常動作すること）

---

## 🎉 完了メッセージ

**Phase 3完了により、appsettings.json廃止が完了しました！**

### 達成したこと

✅ **appsettings.json完全廃止**
- Excel設定とハードコード値のみで動作
- 設定ファイル管理の簡素化
- デプロイ時の設定漏れリスク削減

✅ **Phase 0～Phase 3の累積成果**
- 25項目以上の未使用項目削除（Phase 0）
- 3項目のテスト専用項目削除（Phase 1）
- 7項目のハードコード化（Phase 2-1）
- 1項目のExcel移行（Phase 2-2）
- PlcModelのJSON出力実装（Phase 2-3）
- SavePathの利用実装（Phase 2-4）

✅ **Phase 1-5完了による工数削減**
- MonitoringIntervalMs、PlcModel、SavePathのExcel読み込み実装完了
- Phase 2の工数大幅削減（中 → 小）

### 次の推奨アクション

1. 付録のJSON設定用モデル削除計画を実施（オプション）
2. ドキュメント最終更新
3. 本番環境デプロイ

→ [付録_JSON設定用モデル削除計画.md](./付録_JSON設定用モデル削除計画.md)
