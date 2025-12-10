# Phase 3: appsettings.json完全廃止

**フェーズ**: Phase 3
**影響度**: 低（すべての移行が完了しているため）
**工数**: 小
**前提条件**: Phase 0, Phase 1, Phase 2-1, Phase 2-2, Phase 2-3, Phase 2-4, **Phase 2-5完了**
**状態**: 🚧 **進行中** (開始日: 2025-12-03、最終更新: 2025-12-03)

---

## 📊 実装状況サマリー（2025-12-03時点）

### ✅ 完了済みタスク（100%）

| タスク | 状態 | 確認日時 |
|-------|------|---------|
| appsettings.json削除 | ✅ 完了 | 2025-12-03 |
| Phase3統合テスト作成 | ✅ 完了（7/7合格） | 2025-12-03 |
| **Phase 3-4実装完了** | ✅ 完了 | 2025-12-03 |
| ConfigurationExtensions.cs作成 | ✅ 完了 | 2025-12-03 |
| ApplicationController.cs更新 | ✅ 完了 | 2025-12-03 |
| ExecutionOrchestrator.cs更新 | ✅ 完了 | 2025-12-03 |
| ConfigurationExtensionsTests.cs作成 | ✅ 完了（4/4合格） | 2025-12-03 |
| OptionsConfigurator.cs削除 | ✅ 完了 | 2025-12-03 |
| OptionsConfiguratorTests.cs削除 | ✅ 完了 | 2025-12-03 |
| ドキュメント更新 | ✅ 完了 | 2025-12-03 |

### 📈 実装完了サマリー

**Phase 3-4実装結果**:
- ✅ **重複コード削減**: 28行 → 4行（24行削減、85%削減）
- ✅ **バグの温床解消**: ロジック変更時の不整合リスク完全排除
- ✅ **テスト結果**: 20/20合格（100%）
- ✅ **ビルド結果**: 成功（0エラー、0警告）
- ✅ **TDD完全準拠**: Red → Green → Refactor 全サイクル成功

**重要**: MonitoringIntervalMs変換（秒→ミリ秒）はPhase2で既に実装済み（ConfigurationLoaderExcel.cs:120）。Phase3で対応するのはConnectionConfig/TimeoutConfig生成処理の重複解消のみ。

### 🔍 詳細な実装状況

#### 1. appsettings.json削除 ✅
- **確認方法**: `find andon -name "appsettings*.json"` → 0件
- **結果**: appsettings.jsonは既に削除済み
- **影響**: なし（Phase2でExcel設定へ完全移行済み）

#### 2. Phase3統合テスト ✅
- **ファイル**: `andon/Tests/Integration/Phase3_CompleteRemoval_IntegrationTests.cs`
- **テスト数**: 7テスト
- **実行結果**: 7/7合格（100%）
- **検証内容**:
  - アプリケーション起動（appsettings無し）
  - LoggingManager（ハードコード値）
  - MonitoringIntervalMs（Excel設定値）
  - PlcModel（Excel設定値）
  - SavePath（Excel設定値）
  - 複数PLC設定（独立したMonitoringIntervalMs）
  - IConfiguration空の状態（エラーなし）

#### 3. 重複コード（ConnectionConfig/TimeoutConfig生成処理）の存在 ❌

**注意**: これはMonitoringIntervalMsの秒→ミリ秒変換（ConfigurationLoaderExcel.cs:120）とは**別の話**です。MonitoringIntervalMs変換は既にPhase2で正しく実装済みです。

**重複している処理**: PlcConfiguration → ConnectionConfig/TimeoutConfig の生成処理

- **ApplicationController.cs:92-105**:
  ```csharp
  var connectionConfig = new ConnectionConfig
  {
      IpAddress = config.IpAddress,
      Port = config.Port,
      UseTcp = config.ConnectionMethod == "TCP",
      IsBinary = config.IsBinary
  };

  var timeoutConfig = new TimeoutConfig
  {
      ConnectTimeoutMs = config.Timeout,
      SendTimeoutMs = config.Timeout,
      ReceiveTimeoutMs = config.Timeout
  };
  ```

- **ExecutionOrchestrator.cs:188-201**:
  ```csharp
  var connectionConfig = new ConnectionConfig
  {
      IpAddress = config.IpAddress,
      Port = config.Port,
      UseTcp = config.ConnectionMethod == "TCP",
      IsBinary = config.IsBinary
  };

  var timeoutConfig = new TimeoutConfig
  {
      ConnectTimeoutMs = config.Timeout,
      SendTimeoutMs = config.Timeout,
      ReceiveTimeoutMs = config.Timeout
  };
  ```

- **問題**: 同じロジックが2箇所に存在（計28行の重複コード）
- **リスク**: ロジック変更時に片方だけ修正して不整合が発生する可能性（バグの温床）
- **対策**: ConfigurationExtensions.csで拡張メソッド化（Phase 3-4で必須対応）

**補足**: MonitoringIntervalMs変換（秒→ミリ秒）はConfigurationLoaderExcel.cs:120で既に実装済み
```csharp
MonitoringIntervalMs = ReadCell<int>(settingsSheet, "B11", "データ取得周期(sec)") * 1000,
```

#### 4. OptionsConfigurator.cs の存在 ❌
- **ファイル**: `andon/Services/OptionsConfigurator.cs`
- **状態**: まだ存在している
- **削除理由**: appsettings.json廃止により役割喪失
- **影響**: なし（使用箇所なし）

### 🎯 次のアクション（優先順位順）

1. **🔴 Phase 3-4: ConnectionConfig/TimeoutConfig生成処理の重複解消（必須）**
   - ConfigurationExtensions.cs作成（PlcConfiguration→ConnectionConfig/TimeoutConfigの拡張メソッド）
   - ApplicationController.cs更新（拡張メソッド使用）
   - ExecutionOrchestrator.cs更新（拡張メソッド使用）
   - ConfigurationExtensionsTests.cs作成
   - **期待効果**: 重複コード28行削減、バグの温床解消
   - **注意**: MonitoringIntervalMs変換（秒→ミリ秒）は既にPhase2で実装済み、このタスクとは無関係

2. **🟡 OptionsConfigurator関連削除**
   - OptionsConfigurator.cs削除
   - OptionsConfiguratorTests.cs削除

3. **🟢 ドキュメント更新**
   - README.md更新（Excel設定のみ使用を明記）
   - XMLコメント更新

---

## 🔄 Phase 2-5からの引き継ぎ事項

### Phase 2-5完了状況（2025-12-03完了）

**実装完了日**: 2025-12-03
**実装方式**: TDD (Red→Green→Refactor)
**最終テスト結果**: 100% (Phase 2-5: 4/4合格、Phase 2全体: 36/36合格)

#### Phase 2-5完了事項
✅ **SettingsValidator統合完了**（3項目の検証統合）:
- IPアドレス検証: SettingsValidator.ValidateIpAddress()使用
- ポート検証: SettingsValidator.ValidatePort()使用
- MonitoringIntervalMs検証: SettingsValidator.ValidateMonitoringIntervalMs()使用（範囲: 100～60000ms）

✅ **検証ロジックの統一**:
- ConfigurationLoaderExcel.ValidateConfiguration()がSettingsValidatorを使用
- 重複コード削減、保守性向上

✅ **エラーメッセージの標準化**:
- SettingsValidator標準メッセージに統一
- プロパティ名との一貫性向上

---

## 📋 概要

appsettings.jsonファイルを完全に削除し、Excel設定とハードコード値のみでアプリケーションを動作させます。Phase 0～Phase 2-5ですべての項目の移行が完了しているため、影響は最小限です。

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

#### 1-2. OptionsConfigurator関連ファイルを削除（appsettings.json廃止により役割喪失）

**背景**:
- OptionsConfiguratorは元々appsettings.jsonからConnectionConfig/TimeoutConfigを読み込む役割
- Phase 2/3でappsettings.json廃止、Excel設定ベースに変更
- 現在はPlcConfiguration（Excel）→ ConnectionConfig/TimeoutConfigへの変換を各クラスで実装
- OptionsConfiguratorは設計変更により本来の接続点（appsettings.json）を失った

**削除対象ファイル**:
```bash
# OptionsConfigurator本体
rm andon/Services/OptionsConfigurator.cs

# OptionsConfiguratorテスト
rm andon/Tests/Unit/Services/OptionsConfiguratorTests.cs
```

**保持するファイル（現在も使用中）**:
- `andon/Core/Models/ConfigModels/ConnectionConfig.cs` → PlcCommunicationManagerで使用中
- `andon/Core/Models/ConfigModels/TimeoutConfig.cs` → PlcCommunicationManagerで使用中
- `andon/Services/DependencyInjectionConfigurator.cs` → Program.cs:31で呼び出し中

**変換処理の現在の実装箇所**:
```csharp
// ApplicationController.cs:92-105
var connectionConfig = new ConnectionConfig
{
    IpAddress = config.IpAddress,        // Excel → PlcConfiguration → ConnectionConfig
    Port = config.Port,
    UseTcp = config.ConnectionMethod == "TCP",
    IsBinary = config.IsBinary
};

var timeoutConfig = new TimeoutConfig
{
    ConnectTimeoutMs = config.Timeout,   // PlcConfiguration.Timeout → TimeoutConfig
    SendTimeoutMs = config.Timeout,
    ReceiveTimeoutMs = config.Timeout
};
```

**⚠️ 重複処理の解消が必須**:
- PlcConfiguration → ConnectionConfig/TimeoutConfig変換が2箇所で重複実装
  - ApplicationController.cs:92-105
  - ExecutionOrchestrator.cs:340-353
- **バグの温床となるため、拡張メソッドで共通化が必須**
- Phase 3-4として対応（appsettings.json削除と併せて実施）

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

**調査結果**:
- DependencyInjectionConfigurator.Configure()はProgram.cs:31で呼び出し中 → **保持必須**
- IConfiguration引数は実際には使用されていない → **削除推奨（オプション）**
- Phase 0-2で以下のConfigure<T>呼び出しは削除済み:
  - services.Configure<LoggingConfig>(...) - Phase 2-1で削除
  - services.Configure<DataProcessingConfig>(...) - Phase 2-2で削除
  - services.Configure<SystemResourcesConfig>(...) - Phase 1で削除

```csharp
// andon/Services/DependencyInjectionConfigurator.cs（現在の状態）

public static IServiceCollection ConfigureServices(
    this IServiceCollection services,
    IConfiguration configuration) // ← IConfigurationは引数で受け取るが使用しない
{
    // Singleton登録（IOptions依存なし、すべてハードコード化/Excel設定ベース）
    services.AddSingleton<ILoggingManager, LoggingManager>();
    services.AddSingleton<IExecutionOrchestrator, ExecutionOrchestrator>();
    services.AddSingleton<IDataOutputManager, DataOutputManager>();
    // 等...

    return services;
}
```

**確認事項**:
- ✅ DependencyInjectionConfigurator.Configure()は使用中 → 削除不可
- ✅ ConnectionConfig/TimeoutConfigはPlcCommunicationManagerで使用中 → 削除不可
- ⚠️ IConfiguration引数は未使用 → 削除可能（オプション）

**オプション: IConfiguration引数を削除（推奨）**
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

#### 3-4. ConnectionConfig/TimeoutConfig生成処理の重複解消（バグの温床対策・必須）

**注意**: MonitoringIntervalMs変換（秒→ミリ秒）はPhase2で既に実装済み。ここで対応するのはPlcConfiguration→ConnectionConfig/TimeoutConfigの生成処理の重複解消。

**問題点**:
- PlcConfiguration/PlcConnectionConfig → ConnectionConfig/TimeoutConfig変換が2箇所で重複実装
- ApplicationController.cs:92-105
- ExecutionOrchestrator.cs:340-353
- **バグの温床**:ロジック変更時に片方だけ修正して不整合が発生するリスク

**解決策**: 拡張メソッドで共通化

**実装手順**:

##### 3-4-1. 拡張メソッドクラスを作成

```bash
# 新規ファイル作成
touch andon/Core/Models/ConfigModels/ConfigurationExtensions.cs
```

```csharp
// andon/Core/Models/ConfigModels/ConfigurationExtensions.cs

namespace Andon.Core.Models.ConfigModels;

/// <summary>
/// PlcConfiguration/PlcConnectionConfig用拡張メソッド
/// ConnectionConfig/TimeoutConfigへの変換を共通化
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// PlcConfigurationからConnectionConfigを生成
    /// </summary>
    public static ConnectionConfig ToConnectionConfig(this PlcConfiguration config)
    {
        return new ConnectionConfig
        {
            IpAddress = config.IpAddress,
            Port = config.Port,
            UseTcp = config.ConnectionMethod == "TCP",
            IsBinary = config.IsBinary
        };
    }

    /// <summary>
    /// PlcConfigurationからTimeoutConfigを生成
    /// </summary>
    public static TimeoutConfig ToTimeoutConfig(this PlcConfiguration config)
    {
        return new TimeoutConfig
        {
            ConnectTimeoutMs = config.Timeout,
            SendTimeoutMs = config.Timeout,
            ReceiveTimeoutMs = config.Timeout
        };
    }

    /// <summary>
    /// PlcConnectionConfigからConnectionConfigを生成
    /// </summary>
    public static ConnectionConfig ToConnectionConfig(this PlcConnectionConfig config)
    {
        return new ConnectionConfig
        {
            IpAddress = config.IPAddress,
            Port = config.Port,
            UseTcp = config.ConnectionMethod == "TCP",
            IsBinary = config.IsBinary
        };
    }

    /// <summary>
    /// PlcConnectionConfigからTimeoutConfigを生成
    /// </summary>
    public static TimeoutConfig ToTimeoutConfig(this PlcConnectionConfig config)
    {
        return new TimeoutConfig
        {
            ConnectTimeoutMs = config.Timeout,
            SendTimeoutMs = config.Timeout,
            ReceiveTimeoutMs = config.Timeout
        };
    }
}
```

##### 3-4-2. ApplicationController.csを更新

```csharp
// 修正前（ApplicationController.cs:92-105）
var connectionConfig = new ConnectionConfig
{
    IpAddress = config.IpAddress,
    Port = config.Port,
    UseTcp = config.ConnectionMethod == "TCP",
    IsBinary = config.IsBinary
};

var timeoutConfig = new TimeoutConfig
{
    ConnectTimeoutMs = config.Timeout,
    SendTimeoutMs = config.Timeout,
    ReceiveTimeoutMs = config.Timeout
};

// 修正後（拡張メソッド使用）
var connectionConfig = config.ToConnectionConfig();
var timeoutConfig = config.ToTimeoutConfig();
```

##### 3-4-3. ExecutionOrchestrator.csを更新

```csharp
// 修正前（ExecutionOrchestrator.cs:340-353）
var connectionConfig = new ConnectionConfig
{
    IpAddress = plcConfig.IPAddress,
    Port = plcConfig.Port,
    UseTcp = plcConfig.ConnectionMethod == "TCP",
    IsBinary = plcConfig.IsBinary
};

var timeoutConfig = new TimeoutConfig
{
    ConnectTimeoutMs = plcConfig.Timeout,
    SendTimeoutMs = plcConfig.Timeout,
    ReceiveTimeoutMs = plcConfig.Timeout
};

// 修正後（拡張メソッド使用）
var connectionConfig = plcConfig.ToConnectionConfig();
var timeoutConfig = plcConfig.ToTimeoutConfig();
```

##### 3-4-4. 拡張メソッドのテストを作成

```bash
# テストファイル作成
touch andon/Tests/Unit/Core/Models/ConfigModels/ConfigurationExtensionsTests.cs
```

**テストケース**:
- `ToConnectionConfig_PlcConfiguration_正常変換()`
- `ToTimeoutConfig_PlcConfiguration_正常変換()`
- `ToConnectionConfig_PlcConnectionConfig_正常変換()`
- `ToTimeoutConfig_PlcConnectionConfig_正常変換()`

#### 4. テスト実行 → 全テストがパス

```bash
# 拡張メソッドのテスト
dotnet test --filter "FullyQualifiedName~ConfigurationExtensionsTests"

# Phase3統合テスト
dotnet test --filter "FullyQualifiedName~Phase3"

# 全テスト実行
dotnet test
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

2. ✅ OptionsConfigurator関連ファイルを削除
   - andon/Services/OptionsConfigurator.cs
   - andon/Tests/Unit/Services/OptionsConfiguratorTests.cs

3. ✅ Program.csの確認
   - Host.CreateDefaultBuilder(args)はappsettings.json不在でもエラーにならないことを確認

4. ✅ DI設定の最終確認
   - DependencyInjectionConfigurator.Configure()が使用中であることを確認
   - 不要なIConfiguration依存を削除（オプション）

5. ✅ ConnectionConfig/TimeoutConfigが使用中であることを確認
   - PlcCommunicationManagerで使用中
   - 削除しないこと

6. ✅ ConnectionConfig/TimeoutConfig生成処理の重複解消（Phase 3-4・必須）
   - ConfigurationExtensions.cs作成
   - ApplicationController.cs更新（拡張メソッド使用）
   - ExecutionOrchestrator.cs更新（拡張メソッド使用）
   - ConfigurationExtensionsTests.cs作成
   - 全テストがパス

7. ✅ Phase3_CompleteRemoval_IntegrationTests.cs の全テストがパス

8. ✅ 全体テストがパス（OptionsConfiguratorTests削除、ConfigurationExtensionsTests追加）

9. ✅ ビルドエラーなし

10. ✅ ドキュメント更新
    - README.md更新
    - 各クラスのXMLコメント更新

### 確認コマンド

```bash
# 拡張メソッドのテスト確認
dotnet test --filter "FullyQualifiedName~ConfigurationExtensionsTests"

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

### 2. OptionsConfigurator削除の確認

**削除対象ファイル**:
```
andon/Services/OptionsConfigurator.cs
andon/Tests/Unit/Services/OptionsConfiguratorTests.cs
```

**削除理由**:
- appsettings.json廃止により役割喪失
- 設計方針がExcel設定ベースに変更

**削除してはいけないファイル（重要）**:
```
andon/Core/Models/ConfigModels/ConnectionConfig.cs → PlcCommunicationManagerで使用中
andon/Core/Models/ConfigModels/TimeoutConfig.cs → PlcCommunicationManagerで使用中
andon/Services/DependencyInjectionConfigurator.cs → Program.cs:31で呼び出し中
```

### 3. IConfiguration依存の残存確認

**確認方法**:
```bash
# IConfigurationを使用している箇所を検索
grep -r "IConfiguration" andon/Core andon/Services andon/Infrastructure
```

**残っていても問題ない箇所**:
- DependencyInjectionConfigurator.cs:ConfigureServices()の引数（使用していなければ削除推奨）
- Program.cs（Host.CreateDefaultBuilder内部で使用）

### 4. ConnectionConfig/TimeoutConfig生成処理の重複解消（必須対応）

**重要**: MonitoringIntervalMs変換（秒→ミリ秒）は別の話で、Phase2で既に実装済み（ConfigurationLoaderExcel.cs:120）。

**現状の問題**:
PlcConfiguration/PlcConnectionConfig → ConnectionConfig/TimeoutConfig変換が2箇所で重複実装
- ApplicationController.cs:92-105
- ExecutionOrchestrator.cs:340-353
- **バグの温床**: ロジック変更時に片方だけ修正して不整合が発生するリスク

**Phase 3-4での対応（必須）**:
- ✅ 拡張メソッドで共通化（ConfigurationExtensions.cs作成）
- ✅ ApplicationController.csを更新（拡張メソッド使用）
- ✅ ExecutionOrchestrator.csを更新（拡張メソッド使用）
- ✅ 拡張メソッドのテスト作成

**効果**:
- 重複コード削除（14行 → 2行、2箇所で計24行削減）
- バグの温床解消
- 保守性・可読性向上

### 5. バックアップの作成

**推奨**:
```bash
# appsettings.json削除前にバックアップを作成
cp andon/appsettings.json andon/appsettings.json.bak

# OptionsConfigurator削除前にバックアップを作成
cp andon/Services/OptionsConfigurator.cs andon/Services/OptionsConfigurator.cs.bak
cp andon/Tests/Unit/Services/OptionsConfiguratorTests.cs andon/Tests/Unit/Services/OptionsConfiguratorTests.cs.bak

# 動作確認後、バックアップを削除
rm andon/appsettings.json.bak
rm andon/Services/OptionsConfigurator.cs.bak
rm andon/Tests/Unit/Services/OptionsConfiguratorTests.cs.bak
```

---

## 📊 Phase 3完了後の状態

### 削除されたファイル（Phase 0～3の累積）

#### 設定ファイル（Phase 3）
```
andon/appsettings.json
andon/appsettings.Development.json（あれば）
andon/appsettings.Production.json（あれば）
```

#### Options設定クラス（Phase 3）
```
andon/Services/OptionsConfigurator.cs
andon/Tests/Unit/Services/OptionsConfiguratorTests.cs
```
**削除理由**: appsettings.json廃止により役割喪失（Excel設定ベースに変更）

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

#### PlcCommunicationManager用設定モデル（使用中）
```
andon/Core/Models/ConfigModels/ConnectionConfig.cs（使用中）
andon/Core/Models/ConfigModels/TimeoutConfig.cs（使用中）
andon/Core/Models/ConfigModels/ConfigurationExtensions.cs（Phase 3-4で新規作成）
```
**保持理由**: PlcCommunicationManagerで使用中
**変換処理**: PlcConfiguration/PlcConnectionConfig → ConnectionConfig/TimeoutConfig（拡張メソッドで共通化）

#### DI設定
```
andon/Services/DependencyInjectionConfigurator.cs（使用中）
```
**保持理由**: Program.cs:31で呼び出し中

#### マネージャークラス（ハードコード化/Excel設定ベース）
```
andon/Core/Managers/LoggingManager.cs（ハードコード値使用）
andon/Core/Managers/DataOutputManager.cs（Excel設定使用）
andon/Core/Controllers/ExecutionOrchestrator.cs（Excel設定使用）
```

---

## 🔄 Phase 0～Phase 2-5との違い

| フェーズ | 作業内容 | 影響度 | 本番環境への影響 |
|---------|---------|--------|---------------|
| **Phase 0** | 未使用項目削除（JSON編集） | なし | なし |
| **Phase 1** | テスト専用項目削除（クラス削除） | 低 | なし（テストのみ） |
| **Phase 2-1** | LoggingConfigハードコード化 | 高 | あり（ログ機能） |
| **Phase 2-2** | MonitoringIntervalMs Excel移行 | 中 | あり（タイマー間隔） |
| **Phase 2-3** | PlcModel JSON出力実装 | 中 | あり（JSON出力） |
| **Phase 2-4** | SavePath利用実装 | 中 | あり（保存先パス） |
| **Phase 2-5** | SettingsValidator統合 | 中 | あり（検証ロジック統一、MonitoringIntervalMs検証範囲最適化） |
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

✅ **OptionsConfigurator削除**
- appsettings.json廃止により役割喪失
- 設計方針がExcel設定ベースに変更
- ConnectionConfig/TimeoutConfigはPlcCommunicationManagerで引き続き使用

✅ **ConnectionConfig/TimeoutConfig生成処理の重複解消**（Phase 3-4・バグの温床対策）
- ConfigurationExtensions.cs作成（拡張メソッドで共通化）
- 重複コード削除（24行削減）
- バグの温床解消、保守性向上

✅ **Phase 0～Phase 3の累積成果**
- 25項目以上の未使用項目削除（Phase 0）
- 3項目のテスト専用項目削除（Phase 1）
- 7項目のハードコード化（Phase 2-1）
- 1項目のExcel移行（Phase 2-2）
- PlcModelのJSON出力実装（Phase 2-3）
- SavePathの利用実装（Phase 2-4）
- SettingsValidator統合、検証ロジック統一（Phase 2-5）
- **appsettings.json完全廃止**（Phase 3）
- **OptionsConfigurator削除**（Phase 3）
- **ConnectionConfig/TimeoutConfig生成処理の重複解消**（Phase 3-4）

✅ **Phase 1-5完了による工数削減**
- MonitoringIntervalMs、PlcModel、SavePathのExcel読み込み実装完了
- SettingsValidator実装完了（6つの検証メソッド）
- Phase 2の工数大幅削減（中 → 小）

✅ **Phase 2-5完了による保守性向上**
- 検証ロジックの統一（SettingsValidator集約）
- エラーメッセージの標準化
- MonitoringIntervalMs検証範囲の最適化（100～60000ms）
- 重複コード削減、拡張性向上

### 次の推奨アクション

1. **Phase 3実施前の必須タスク**: 外部テストデータ更新（5JRS_N2.xlsx の MonitoringIntervalMs を1 → 1000に修正）
2. Phase 3-4実施: **ConnectionConfig/TimeoutConfig生成処理の重複解消（必須）**
   - ConfigurationExtensions.cs作成
   - ApplicationController.cs更新
   - ExecutionOrchestrator.cs更新
   - ConfigurationExtensionsTests.cs作成
3. Phase 3実施: appsettings.json完全廃止、OptionsConfigurator削除
4. 付録のJSON設定用モデル削除計画を実施（オプション）
5. ドキュメント最終更新
6. 本番環境デプロイ

---

## 🔗 関連ドキュメント

### 前提条件（完了済み）
- [Phase 0: 即座削除項目](Phase0_即座削除項目.md) → **完了** ✅ (2025-12-02)
- [Phase 1: テスト専用項目整理](Phase1_テスト専用項目整理.md) → **完了** ✅ (2025-12-02)
- [Phase 2-1: LoggingConfigハードコード化](Phase2-1_LoggingConfig_ハードコード化.md) → **完了** ✅ (2025-12-03)
- [Phase 2-2: MonitoringIntervalMsのExcel移行](Phase2-2_MonitoringIntervalMs_Excel移行.md) → **完了** ✅ (2025-12-03)
- [Phase 2-3: PlcModelのJSON出力実装](Phase2-3_PlcModel_JSON出力実装.md) → **完了** ✅ (2025-12-03)
- [Phase 2-4: SavePathの利用実装](Phase2-4_SavePath_利用実装.md) → **完了** ✅ (2025-12-03)
- [Phase 2-5: SettingsValidator統合](Phase2-5_SettingsValidator統合.md) → **完了** ✅ (2025-12-03)

### 実装結果
- [Phase 0 実装結果](../実装結果/Phase0_UnusedItemsDeletion_TestResults.md)
- [Phase 1 実装結果](../実装結果/Phase1_TestOnlyClasses_TestResults.md)
- [Phase 2-1 実装結果](../実装結果/Phase2_1_LoggingConfig_Hardcoding_TestResults.md)
- [Phase 2-2 実装結果](../実装結果/Phase2_2_MonitoringInterval_Excel移行_TestResults.md)
- [Phase 2-3 実装結果](../実装結果/Phase2_3_PlcModel_JSON出力_TestResults.md)
- [Phase 2-4 実装結果](../実装結果/Phase2_4_SavePath_利用実装_TestResults.md)
- [Phase 2-5 実装結果](../実装結果/Phase2_5_SettingsValidator統合_TestResults.md)

### 次フェーズ
→ [付録_JSON設定用モデル削除計画.md](./付録_JSON設定用モデル削除計画.md)
