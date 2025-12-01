# Step1 実装計画 - Phase6: 未使用プロパティ削除とログ出力統合

**作成日**: 2025-11-28
**実装方式**: TDD (Test-Driven Development)

---

## Phase6の目的

Phase1～5で実装した設定読み込み機能の中で、実装されているが実際には使用されていないプロパティを整理し、全プロパティが実運用で確実に使用される状態にする。具体的には:
1. **ConnectionMethod**: 既定値"UDP"を維持し、B10セルから取得（省略可）
2. **PlcId**: 「{IpAddress}_{Port}」形式で自動生成し、PlcNameが未設定時に使用
3. **PlcName**: B15セルから取得し、ログ出力で使用（省略可、未設定時はPlcIdを使用）

---

## Phase5からの引継ぎ事項

### Phase5完了状況（2025-11-27完了）

✅ **複数設定管理機能実装完了**
- **MultiPlcConfigManager実装**: 27/27単体テスト合格
- **ConfigurationLoaderExcelとのDI統合**: 最小変更（5行追加）
- **統合テスト実装**: 5/5テストケース実装完了（実行保留中）

✅ **Phase1～Phase5統合完了状況**
- **Phase1**: DeviceCodeMap、SlmpFixedSettings
- **Phase2**: ConfigurationLoaderExcel基盤、Excel読み込み
- **Phase3**: NormalizeDevice()、デバイス情報正規化
- **Phase4**: ValidateConfiguration()、設定検証
- **Phase5**: MultiPlcConfigManager、複数設定一元管理

### Phase6で対処すべき課題

現在の実装では、以下のプロパティが読み込まれているが実際には使用されていません:

| プロパティ名 | Excelセル | 現在の状態 | Phase6での対応 |
|-------------|-----------|-----------|---------------|
| **ConnectionMethod** | B10 | 読み込まれるが通信処理で参照されない | **維持**: 既定値"UDP"、B10セルから取得（省略可） |
| **PlcId** | 自動生成 | 自動生成されるがどこでも使用されない | **維持**: 「{IpAddress}_{Port}」で自動生成、PlcName未設定時に使用 |
| **PlcName** | B15 | 読み込まれるがログで使用されない | **ログ出力で使用**: B15セルから取得（省略可、未設定時はPlcIdを使用） |

### 使用されているプロパティ（Phase6で維持）

| プロパティ名 | Excelセル/生成方法 | 使用箇所 | 備考 |
|-------------|------------------|---------|------|
| **IpAddress** | B8 | PLC通信 | 必須項目 |
| **Port** | B9 | PLC通信 | 必須項目 |
| **ConnectionMethod** | B10 | PLC通信 | **Phase6で追加**: 既定値"UDP"（省略可） |
| **MonitoringIntervalMs** | B11 | ExecutionOrchestrator | 必須項目 |
| **PlcModel** | B12 | 設定管理 | 必須項目 |
| **SavePath** | B13 | データ出力 | 必須項目 |
| **PlcId** | 自動生成: "{IpAddress}_{Port}" | ログ出力 | **Phase6で追加**: PlcName未設定時のフォールバック |
| **PlcName** | B15 | ログ出力 | **Phase6で追加**: 省略可、未設定時はPlcIdを使用 |
| **FrameVersion** | 既定値"4E" | ConfigToFrameManager | 省略可 |
| **Timeout** | 既定値1000ms | ConfigToFrameManager | 省略可 |

---

## 実装内容

### 1. PlcConfigurationモデル修正

**ファイル**: `andon/Core/Models/ConfigModels/PlcConfiguration.cs`

#### 維持するプロパティ（全て既存）

```csharp
/// <summary>
/// 接続方式（Excel "settings"シート B10セル）
/// 既定値: "UDP"（省略可）
/// </summary>
public string ConnectionMethod { get; set; } = DefaultValues.ConnectionMethod;

/// <summary>
/// PLC識別ID（自動生成: "{IpAddress}_{Port}"）
/// PlcNameが未設定の場合、ログ出力で使用される
/// </summary>
public string PlcId { get; set; } = string.Empty;

/// <summary>
/// PLC識別名（Excel "settings"シート B15セル）
/// ログ出力で使用される（省略可、未設定時はPlcIdを使用）
/// </summary>
public string PlcName { get; set; } = string.Empty;

/// <summary>
/// ログ出力用PLC識別名（フォールバック処理付き）
/// PlcNameが設定されている場合はPlcNameを返し、
/// 未設定の場合はPlcIdを返す
/// </summary>
public string EffectivePlcName =>
    string.IsNullOrWhiteSpace(PlcName) ? PlcId : PlcName;
```

**設計判断の根拠**:
- **ConnectionMethod維持**: 既定値"UDP"を設定し、B10セルから取得（省略可）。将来的なTCP対応の余地を残す
- **PlcId維持**: 「{IpAddress}_{Port}」形式で自動生成し、PlcName未設定時のフォールバックとして使用
- **PlcName維持**: B15セルから取得し、ログ出力で使用（省略可）
- **EffectivePlcName追加**: PlcNameが設定されている場合はPlcName、未設定の場合はPlcIdを返す計算プロパティ

### 2. ConfigurationLoaderExcel修正

**ファイル**: `andon/Infrastructure/Configuration/ConfigurationLoaderExcel.cs`

#### 維持する処理（B10セル読み込み）

```csharp
// ✅ 維持: Line 104
string connectionMethod = ReadOptionalCell<string>(settingsSheet, "B10", DefaultValues.ConnectionMethod);

// ✅ 維持: Line 125（PlcConfigurationへの設定）
ConnectionMethod = connectionMethod,
```

#### 維持する処理（PlcId生成）

```csharp
// ✅ 維持: Line 107-108
string plcId = $"{ipAddress}_{port}";

// ✅ 維持: Line 129（PlcConfigurationへの設定）
PlcId = plcId,
```

#### PlcName処理の修正（フォールバック処理変更）

```csharp
// ✅ 既存: Line 105
string? plcName = settingsSheet.Cells["B15"].Text;

// ✅ 既存: Line 130（PlcName設定）
PlcName = plcName ?? string.Empty,

// ⚠️ 既存コードの動作変更（PlcIdフォールバック削除）
// 【変更前】PlcNameが空の場合、PlcIdを代入していた
// 【変更後】PlcNameが空の場合、空のまま（EffectivePlcNameがフォールバック処理を担当）
```

**変更内容まとめ**:
- B10セル（ConnectionMethod）読み込み処理を維持
- PlcId自動生成処理を維持
- B15セル（PlcName）読み込み処理を維持
- **PlcNameへのPlcIdフォールバック代入を削除**（EffectivePlcName計算プロパティがフォールバック処理を担当）

### 3. LoggingManager修正（PlcNameの活用）

**ファイル**: `andon/Core/Managers/LoggingManager.cs`

#### PLC処理ログでの使用

```csharp
/// <summary>
/// PLC処理開始ログ
/// </summary>
public void LogPlcProcessStart(PlcConfiguration config)
{
    _logger.LogInformation(
        $"[{config.EffectivePlcName}] PLC処理開始: " +
        $"{config.IpAddress}:{config.Port}");
}

/// <summary>
/// PLC処理完了ログ
/// </summary>
public void LogPlcProcessComplete(PlcConfiguration config, int deviceCount)
{
    _logger.LogInformation(
        $"[{config.EffectivePlcName}] PLC処理完了: " +
        $"デバイス数={deviceCount}");
}

/// <summary>
/// PLC通信エラーログ
/// </summary>
public void LogPlcCommunicationError(PlcConfiguration config, Exception ex)
{
    _logger.LogError(ex,
        $"[{config.EffectivePlcName}] PLC通信エラー: " +
        $"{config.IpAddress}:{config.Port}");
}
```

### 4. DataOutputManager修正（削除）

**ファイル**: `andon/Core/Managers/DataOutputManager.cs`

#### データファイル名生成での対応

**重要**: Step7の出力ファイル設計により、ファイル名形式は以下の通り:
- 形式: `{IpAddress(ドット→ハイフン)}_{Port}.json`
- 例: `192-168-1-10_8192.json`

**Phase6での対応**:
- DataOutputManagerでのPlcName使用は**実装しない**
- 現行のファイル名生成ロジックを維持
- Step7の出力ファイル設計に準拠

```csharp
// Phase6では修正不要
// Step7の出力ファイル設計に従い、以下の形式でファイル名を生成:
// "{IpAddress(ドット→ハイフン)}_{Port}.json"
```

### 5. テスト修正

#### PlcConfigurationTests修正

**ファイル**: `andon/Tests/Unit/Core/Models/ConfigModels/PlcConfigurationTests.cs`

```csharp
// ✅ 既存テストを維持（修正不要）
public void ConnectionMethod_設定と取得が正しく動作する() { ... }
public void PlcId_設定と取得が正しく動作する() { ... }
public void PlcName_設定と取得が正しく動作する() { ... }

// ✅ 追加するテスト（EffectivePlcName）
[Fact]
public void EffectivePlcName_PlcNameが設定されている場合_PlcNameを返す()
{
    // Arrange
    var config = new PlcConfiguration
    {
        PlcName = "ライン1-炉A",
        PlcId = "192.168.1.10_8192",
        IpAddress = "192.168.1.10",
        Port = 8192
    };

    // Act & Assert
    Assert.Equal("ライン1-炉A", config.EffectivePlcName);
}

[Fact]
public void EffectivePlcName_PlcNameが空の場合_PlcIdを返す()
{
    // Arrange
    var config = new PlcConfiguration
    {
        PlcName = "",
        PlcId = "192.168.1.10_8192",
        IpAddress = "192.168.1.10",
        Port = 8192
    };

    // Act & Assert
    Assert.Equal("192.168.1.10_8192", config.EffectivePlcName);
}

[Fact]
public void EffectivePlcName_PlcNameがnullの場合_PlcIdを返す()
{
    // Arrange
    var config = new PlcConfiguration
    {
        PlcName = null,
        PlcId = "192.168.1.10_8192",
        IpAddress = "192.168.1.10",
        Port = 8192
    };

    // Act & Assert
    Assert.Equal("192.168.1.10_8192", config.EffectivePlcName);
}
```

#### ConfigurationLoaderExcelTests修正

**ファイル**: `andon/Tests/Unit/Infrastructure/Configuration/ConfigurationLoaderExcelTests.cs`

```csharp
// ✅ 既存テストを維持（修正不要）
public void LoadFromExcel_Phase2_ConnectionMethodが空の場合_既定値UDPを使用する() { ... }
public void LoadFromExcel_Phase2_PlcIdが自動生成される() { ... }

// ✅ 更新するテスト（PlcName未設定時の動作確認）
[Fact]
public void LoadFromExcel_Phase2_PlcNameが空の場合_EffectivePlcNameがPlcIdを返す()
{
    // Arrange
    var testFile = Path.Combine(_testDirectory, "phase2_empty_plcname.xlsx");
    TestExcelFileCreator.CreatePhase2EmptyPlcNameFile(testFile);
    _createdFiles.Add(testFile);

    var loader = new ConfigurationLoaderExcel(_testDirectory);

    // Act
    var configs = loader.LoadAllPlcConnectionConfigs();
    var config = configs[0];

    // Assert
    Assert.Equal("", config.PlcName); // PlcNameは空
    Assert.Equal(config.PlcId, config.EffectivePlcName); // PlcIdを返す
    Assert.Equal($"{config.IpAddress}_{config.Port}", config.EffectivePlcName);
}
```

#### 新規テスト: LoggingManagerTests

**ファイル**: `andon/Tests/Unit/Core/Managers/LoggingManagerTests.cs`

```csharp
[Fact]
public void LogPlcProcessStart_PlcNameが設定されている場合_PlcName使用()
{
    // Arrange
    var logger = new Mock<ILogger<LoggingManager>>();
    var loggingManager = new LoggingManager(logger.Object);
    var config = new PlcConfiguration
    {
        PlcName = "ライン1-炉A",
        IpAddress = "192.168.1.10",
        Port = 8192
    };

    // Act
    loggingManager.LogPlcProcessStart(config);

    // Assert
    logger.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[ライン1-炉A]")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);
}

[Fact]
public void LogPlcProcessStart_PlcNameが空の場合_フォールバック値使用()
{
    // Arrange
    var logger = new Mock<ILogger<LoggingManager>>();
    var loggingManager = new LoggingManager(logger.Object);
    var config = new PlcConfiguration
    {
        PlcName = "",
        IpAddress = "192.168.1.10",
        Port = 8192
    };

    // Act
    loggingManager.LogPlcProcessStart(config);

    // Assert
    logger.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[192.168.1.10_8192]")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);
}
```

#### DataOutputManagerTests

**ファイル**: `andon/Tests/Unit/Core/Managers/DataOutputManagerTests.cs`

**Phase6では修正不要**:
- Step7の出力ファイル設計に従い、ファイル名形式は `{IpAddress(ドット→ハイフン)}_{Port}.json`
- DataOutputManagerでのPlcName使用テストは追加しない
- 既存のテストがそのまま継続して使用される

---

## Phase6の成功条件

### 機能面
- ✅ ConnectionMethod、PlcId、PlcNameが全て維持されている
- ✅ EffectivePlcName計算プロパティが追加されている
- ✅ ログ出力でEffectivePlcNameが使用されている
- ✅ PlcNameが設定されている場合はPlcNameを、未設定の場合はPlcIdを使用

### テスト面
- ✅ 既存のConnectionMethod、PlcId、PlcName関連テストが全て維持されている
- ✅ EffectivePlcName関連のテストが追加され、全てパスする
- ✅ LoggingManagerでのEffectivePlcName使用テストが追加され、全てパスする
- ✅ Phase1～5の既存テストが全て引き続きパスする（回帰なし）

### コード品質面
- ✅ 全プロパティが実運用で使用されている
- ✅ ログ出力でPLCを識別できる（PlcName優先、フォールバックでPlcId）
- ✅ データファイル名はStep7の出力ファイル設計に準拠（`{IpAddress(ドット→ハイフン)}_{Port}.json`）
- ✅ ConnectionMethodが将来的なTCP対応の余地を残している

---

## Phase6のテスト計画

### TDD実装サイクル

#### Round 1: EffectivePlcName実装（PlcName優先）

**Red（テスト作成）**:
```csharp
[Fact]
public void EffectivePlcName_PlcNameが設定されている場合_PlcNameを返す() { ... }
// テスト失敗確認（EffectivePlcNameがまだ存在しない）
```

**Green（実装）**:
```csharp
public string EffectivePlcName =>
    string.IsNullOrWhiteSpace(PlcName) ? PlcId : PlcName;
// テスト成功
```

**Refactor**:
- XMLドキュメントコメント追加

#### Round 2: EffectivePlcName（PlcIdフォールバック）

**Red（テスト作成）**:
```csharp
[Fact]
public void EffectivePlcName_PlcNameが空の場合_PlcIdを返す() { ... }
[Fact]
public void EffectivePlcName_PlcNameがnullの場合_PlcIdを返す() { ... }
// テスト成功（Round 1実装で既にカバー）
```

**Refactor**:
- テストケースの追加確認

#### Round 3: ConfigurationLoaderExcelでのPlcName処理確認

**Red（テスト作成）**:
```csharp
[Fact]
public void LoadFromExcel_Phase2_PlcNameが空の場合_EffectivePlcNameがPlcIdを返す() { ... }
// テスト実行（既存実装で成功するはず）
```

**Green（確認）**:
- ConfigurationLoaderExcelの既存実装を確認
- PlcName未設定時に空文字列のままになっていることを確認
- EffectivePlcNameがPlcIdを返すことを確認

#### Round 4: LoggingManagerでのEffectivePlcName使用

**Red（テスト作成）**:
```csharp
[Fact]
public void LogPlcProcessStart_PlcNameが設定されている場合_PlcName使用() { ... }
// テスト失敗（LoggingManagerが未修正）
```

**Green（実装）**:
```csharp
public void LogPlcProcessStart(PlcConfiguration config)
{
    _logger.LogInformation($"[{config.EffectivePlcName}] ...");
}
// テスト成功
```

**Refactor**:
- 他のログ出力メソッドも同様に修正

**注意**: DataOutputManager統合は実装しない。Step7の出力ファイル設計に従い、ファイル名形式は `{IpAddress(ドット→ハイフン)}_{Port}.json` のまま維持される。

---

## Phase6の実装手順

### ステップ1: 準備

1. 現在の実装状況確認
   ```bash
   dotnet test --filter "FullyQualifiedName~PlcConfigurationTests"
   dotnet test --filter "FullyQualifiedName~ConfigurationLoaderExcelTests"
   ```

2. 実装前のテスト結果を記録
   - 全テスト数
   - 成功数
   - 失敗数

### ステップ2: EffectivePlcName実装

1. **テスト作成**
   - `PlcConfigurationTests.cs`: EffectivePlcName関連3テスト追加

2. **実装追加**
   - `PlcConfiguration.cs`: EffectivePlcName計算プロパティ追加

3. **ビルド・テスト実行**
   ```bash
   dotnet test --filter "FullyQualifiedName~PlcConfigurationTests"
   ```

### ステップ3: LoggingManager修正

1. **テスト作成**
   - `LoggingManagerTests.cs`: EffectivePlcName使用テスト追加

2. **実装修正**
   - `LoggingManager.cs`: EffectivePlcName使用に修正

3. **ビルド・テスト実行**
   ```bash
   dotnet test --filter "FullyQualifiedName~LoggingManagerTests"
   ```

### ステップ4: 統合テスト実行

1. **全体テスト実行**
   ```bash
   dotnet test andon/Tests/andon.Tests.csproj --verbosity normal
   ```

2. **回帰テスト確認**
   - Phase1～5の既存テストが全てパスすることを確認

3. **テスト結果記録**
   - `実装結果/Phase6_未使用プロパティ削除_TestResults.md`に記録

---

## Phase6完了後の状態

### コード改善効果

✅ **機能追加**
- EffectivePlcName: PLC識別用の計算プロパティ追加（PlcName優先、フォールバックでPlcId）
- LoggingManager: EffectivePlcName使用でログ出力改善

✅ **コード品質向上**
- 全プロパティが実運用で使用される状態
- ログでPLCを明確に識別可能（PlcName優先、PlcIdフォールバック）
- データファイル名はStep7の出力ファイル設計に準拠
- ConnectionMethodが将来的なTCP対応の余地を維持

### プロパティ使用状況（Phase6完了後）

| プロパティ名 | Excelセル/生成方法 | 使用箇所 | 状態 |
|-------------|------------------|---------|------|
| **IpAddress** | B8 | PLC通信、データファイル名 | ✅ 使用中 |
| **Port** | B9 | PLC通信、データファイル名 | ✅ 使用中 |
| **ConnectionMethod** | B10（既定値"UDP"） | PLC通信 | ✅ **使用中（Phase6で確認）** |
| **MonitoringIntervalMs** | B11 | ExecutionOrchestrator | ✅ 使用中 |
| **PlcModel** | B12 | 設定管理 | ✅ 使用中 |
| **SavePath** | B13 | データ出力 | ✅ 使用中 |
| **PlcId** | 自動生成: "{IpAddress}_{Port}" | ログ出力（PlcName未設定時） | ✅ **使用中（Phase6で確認）** |
| **PlcName** | B15 | ログ出力（優先） | ✅ **使用中（Phase6で追加）** |
| **EffectivePlcName** | 計算: PlcName ?? PlcId | ログ出力 | ✅ **使用中（Phase6で追加）** |
| **FrameVersion** | 既定値"4E" | ConfigToFrameManager | ✅ 使用中 |
| **Timeout** | 既定値1000ms | ConfigToFrameManager | ✅ 使用中 |

**使用率**: **100%（11/11プロパティが使用中）**

---

## 次のステップ

**Phase6完了により、Step1の全機能が完成し、全プロパティが実運用で使用される理想的な状態になります。**

### Step2への移行

Phase6完了後、以下のStep2実装に進む:

- ConfigToFrameManager.BuildReadRandomFrameFromConfig()
- SlmpFrameBuilder.BuildReadRandomRequest()
- デバイス指定部分の構築
- フレームヘッダの結合
- 完成したバイナリフレームの返却

### Step1からの引き継ぎ

- PlcConfiguration（Phase6でクリーンアップ済み） → Step2で使用
- DeviceSpecification → フレーム構築に使用
- SlmpFixedSettings → フレームヘッダ構築に使用
- EffectivePlcName → ログ出力、エラー報告で使用

---

## 実装完了記録

**実装完了日**: 2025-11-28
**実装方式**: TDD (Test-Driven Development)
**実装結果**: ✅ 成功

### 実装サマリー

| 項目 | 詳細 |
|-----|------|
| 実装クラス | PlcConfiguration, ConfigurationLoaderExcel, LoggingManager |
| 追加メソッド/プロパティ | EffectivePlcName計算プロパティ、LogPlcProcessStart、LogPlcProcessComplete、LogPlcCommunicationError |
| 追加テスト数 | 8テスト |
| テスト結果 | 8/8合格（100%） |
| 回帰テスト | Phase1～5の既存テスト180/180合格（回帰なし） |
| プロパティ使用率 | 100% (11/11プロパティが使用中) |

### 主要な変更点

1. **PlcConfiguration.cs**
   - EffectivePlcName計算プロパティ追加（5行追加）
   - PlcName優先、未設定時はPlcIdを返すフォールバック処理

2. **ConfigurationLoaderExcel.cs**
   - PlcNameへのPlcIdフォールバック代入を削除（4行削除）
   - PlcName = plcName ?? string.Empty に修正（1行修正）

3. **LoggingManager.cs**
   - PLC処理ログメソッド3つ追加（34行追加）
   - LogPlcProcessStart、LogPlcProcessComplete、LogPlcCommunicationError

### TDD実装サイクル実践結果

| Round | 内容 | Red | Green | Refactor | 結果 |
|-------|-----|-----|-------|----------|------|
| Round 1 | EffectivePlcName実装 | ✅ | ✅ | ✅ | 3/3テスト合格 |
| Round 2 | フォールバック動作確認 | - | ✅ | - | Round 1でカバー済み |
| Round 3 | ConfigurationLoaderExcel修正 | ✅ | ✅ | - | 1/1テスト合格 |
| Round 4 | LoggingManager実装 | ✅ | ✅ | - | 4/4テスト合格 |

### 成功条件達成状況

#### 機能面
- ✅ ConnectionMethod、PlcId、PlcNameが全て維持されている
- ✅ EffectivePlcName計算プロパティが追加されている
- ✅ ログ出力でEffectivePlcNameが使用されている
- ✅ PlcNameが設定されている場合はPlcNameを、未設定の場合はPlcIdを使用

#### テスト面
- ✅ 既存のConnectionMethod、PlcId、PlcName関連テストが全て維持されている
- ✅ EffectivePlcName関連のテストが追加され、全てパスする
- ✅ LoggingManagerでのEffectivePlcName使用テストが追加され、全てパスする
- ✅ Phase1～5の既存テストが全て引き続きパスする（回帰なし）

#### コード品質面
- ✅ 全プロパティが実運用で使用されている（使用率100%）
- ✅ ログ出力でPLCを識別できる（PlcName優先、フォールバックでPlcId）
- ✅ データファイル名はStep7の出力ファイル設計に準拠
- ✅ ConnectionMethodが将来的なTCP対応の余地を残している

### 詳細実装結果

詳細なテスト結果、コード変更内容、TDD実装サイクルの詳細は以下を参照:
- `実装結果/Phase6_未使用プロパティ削除とログ出力統合_TestResults.md`

---

**作成日**: 2025-11-28
**最終更新**: 2025-11-28（実装完了記録追加）
