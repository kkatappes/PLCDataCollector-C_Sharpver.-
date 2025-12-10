# Phase 2-5: SettingsValidator統合

**フェーズ**: Phase 2-5（新規追加）
**影響度**: 中（検証ロジックの統一）
**工数**: **小～中**（リファクタリング主体）
**前提条件**: Phase 0, Phase 1, Phase 2-1, Phase 2-2, Phase 2-3, **Phase 2-4完了**
**状態**: ⏳ 準備中

---

## 🔄 Phase 2-1～Phase 2-4からの引き継ぎ事項

### Phase 2-1完了状況（2025-12-03完了）

**実装完了日**: 2025-12-03
**実装方式**: TDD (Red→Green→Refactor)
**最終テスト結果**: 100% (12/12対象テスト合格)

#### Phase 2-1完了事項
✅ **LoggingConfig全7項目のハードコード化完了**
✅ **appsettings.json削減**: 14行 → 5行（9行削除）
✅ **LoggingConfig.cs削除**: クラスファイル完全削除
✅ **IOptions<LoggingConfig>依存削除**: LoggingManager.csから削除完了
✅ **DI設定更新**: LoggingConfig DI登録削除完了

### Phase 2-2完了状況（2025-12-03完了）

**実装完了日**: 2025-12-03
**実装方式**: TDD (Red→Green→Refactor)
**最終テスト結果**: 100% (8/8 Phase 2-2専用テスト合格)

#### Phase 2-2完了事項
✅ **MonitoringIntervalMs Excel移行完了**
✅ **ExecutionOrchestrator.cs**: IOptions<DataProcessingConfig>依存削除
✅ **DataProcessingConfig.cs削除**: クラスファイル完全削除
✅ **Excel設定利用**: settingsシート B11セルから読み込み（ConfigurationLoaderExcel.cs:115）

### Phase 2-3完了状況（2025-12-03完了）

**実装完了日**: 2025-12-03
**実装方式**: TDD (Red→Green→Refactor)
**最終テスト結果**: 100% (Phase 2全体: 27/27合格、Phase 2-3: 4/4合格)

#### Phase 2-3完了事項
✅ **PlcModel JSON出力実装完了**
✅ **IDataOutputManager.cs**: シグネチャに`string plcModel`パラメータ追加
✅ **DataOutputManager.cs**: JSON出力に`source.plcModel`追加
✅ **ExecutionOrchestrator.cs**: `config.PlcModel`を引数に追加
✅ **Excel設定利用**: settingsシート B12セルから読み込み（ConfigurationLoaderExcel.cs:116）

### Phase 2-4完了状況（2025-12-03完了）

**実装完了日**: 2025-12-03
**実装方式**: TDD (Red→Green→Refactor)
**最終テスト結果**: 100% (Phase 2-4: 5/5合格、関連テスト: 71/71合格)

#### Phase 2-4完了事項
✅ **SavePath利用実装完了**
✅ **ExecutionOrchestrator.cs**: ハードコードされたパス削除、`config.SavePath`使用
✅ **デフォルト値設定**: 空の場合 `"./output"` を使用
✅ **ディレクトリ自動作成**: `Directory.CreateDirectory()` で実装
✅ **Excel設定利用**: settingsシート B13セルから読み込み（ConfigurationLoaderExcel.cs:117）
✅ **環境依存排除**: 開発環境固有のパス完全削除

#### Phase 2-5への影響
⚠️ **SavePath検証の重要性増加**:
- Phase 2-4でSavePath機能が本番稼働開始
- ConfigurationLoaderExcel.ValidateConfiguration()のSavePath検証（L140-155）が重要
- SettingsValidatorへの統合により、検証ロジックの統一が必要

---

## 📋 概要

SettingsValidator.csの検証ロジックをConfigurationLoaderExcel.csのValidateConfiguration()メソッドに統合します。

**現状の問題**:
- SettingsValidator.csは実装済みだがテストでのみ使用されている
- ConfigurationLoaderExcel.ValidateConfiguration()は独自の検証ロジックを実装
- 検証ロジックが重複しており、保守性が低い

**✅ Phase 1-5完了により、SettingsValidator.csは既に実装済みです。ConfigurationLoaderExcel.csのリファクタリングのみで完了します。**

---

## ⚠️ 既存実装の確認

### SettingsValidator.csの実装状況（✅ 完了済み）

| 検証メソッド | 実装箇所 | 検証内容 | 検証範囲 |
|------------|---------|---------|---------|
| **ValidateIpAddress** | SettingsValidator.cs:35-50 | IPアドレス形式、IPv4オクテット、0.0.0.0禁止 | 必須 |
| **ValidatePort** | SettingsValidator.cs:61-65 | ポート番号範囲（1～65535） | 必須 |
| **ValidateTimeout** | SettingsValidator.cs:106-110 | タイムアウト範囲（100～30000ms） | オプション |
| **ValidateConnectionMethod** | SettingsValidator.cs:76-80 | 接続方式（TCP, UDP） | オプション |
| **ValidateFrameVersion** | SettingsValidator.cs:91-95 | フレームバージョン（3E, 4E） | オプション |
| **ValidateMonitoringIntervalMs** | SettingsValidator.cs:121-125 | 監視間隔範囲（100～60000ms） | 必須 |

**定数定義**:
```csharp
// SettingsValidator.cs:15-24
private static readonly string[] ValidConnectionMethods = { "TCP", "UDP" };
private static readonly string[] ValidFrameVersions = { "3E", "4E" };

private const int MinPort = 1;
private const int MaxPort = 65535;
private const int MinTimeout = 100;
private const int MaxTimeout = 30000;
private const int MinMonitoringInterval = 100;
private const int MaxMonitoringInterval = 60000;
private const int RequiredIpv4OctetCount = 4;
```

### ConfigurationLoaderExcel.ValidateConfiguration()の実装状況

```csharp
// andon/Infrastructure/Configuration/ConfigurationLoaderExcel.cs:373-463

private void ValidateConfiguration(PlcConfiguration config)
{
    // ① 接続情報検証（独自実装）
    if (!System.Net.IPAddress.TryParse(config.IpAddress, out _))
    {
        throw new ArgumentException(
            $"IPアドレスの形式が不正です: {config.IpAddress}");
    }

    if (config.Port < 1 || config.Port > 65535)
    {
        throw new ArgumentException(
            $"ポート番号が範囲外です: {config.Port}（1～65535）");
    }

    // ② データ取得周期（監視間隔）検証（独自実装）
    if (config.MonitoringIntervalMs < 1 || config.MonitoringIntervalMs > 86400000)
    {
        throw new ArgumentException(
            $"データ取得周期が範囲外です: {config.MonitoringIntervalMs}（1～86400000ms）");
    }

    // ③ デバイスリスト検証（ConfigurationLoaderExcel固有）
    if (config.Devices == null || config.Devices.Count == 0)
    {
        throw new ArgumentException(
            $"デバイスが1つも設定されていません: {config.SourceExcelFile}");
    }

    foreach (var device in config.Devices)
    {
        if (device.DeviceNumber < 0 || device.DeviceNumber > 0xFFFFFF)
        {
            throw new ArgumentOutOfRangeException(
                nameof(device.DeviceNumber),
                $"デバイス番号が範囲外です: {device.DeviceNumber}（項目名: {device.ItemName}、範囲: 0～16777215）");
        }
    }

    // ④ 総点数制限チェック（ConfigurationLoaderExcel固有）
    int totalWordPoints = config.Devices
        .Where(d => d.Unit.ToLower() == "word")
        .Sum(d => d.Digits);

    int totalDwordPoints = config.Devices
        .Where(d => d.Unit.ToLower() == "dword")
        .Sum(d => d.Digits);

    int totalBitPoints = config.Devices
        .Where(d => d.Unit.ToLower() == "bit")
        .Sum(d => d.Digits);

    int bitAsWords = (totalBitPoints + 15) / 16;
    int totalPoints = totalWordPoints + (totalDwordPoints * 2) + bitAsWords;

    if (totalPoints > 255)
    {
        throw new ArgumentException(
            $"デバイス点数が上限を超えています: {totalPoints}点（最大255点）\n" +
            $"  Word: {totalWordPoints}点\n" +
            $"  Dword: {totalDwordPoints}点 (ワード換算: {totalDwordPoints * 2}点)\n" +
            $"  Bit: {totalBitPoints}点 (ワード換算: {bitAsWords}点)\n" +
            $"ファイル: {config.SourceExcelFile}");
    }

    // ⑤ 出力設定検証（ConfigurationLoaderExcel固有）
    if (string.IsNullOrWhiteSpace(config.SavePath))
    {
        throw new ArgumentException(
            $"データ保存先パスが設定されていません: {config.SourceExcelFile}");
    }

    try
    {
        Path.GetFullPath(config.SavePath);
    }
    catch (Exception ex)
    {
        throw new ArgumentException(
            $"データ保存先パスの形式が不正です: {config.SavePath}",
            ex);
    }

    if (string.IsNullOrWhiteSpace(config.PlcModel))
    {
        throw new ArgumentException(
            $"デバイス名（PLC識別名）が設定されていません: {config.SourceExcelFile}");
    }
}
```

---

## 🎯 対象項目（6項目 + Phase 2-4対応2項目）

| 項目 | 現状 | 修正後 | 対応方法 |
|------|------|--------|---------|
| IPアドレス検証 | ❌ 独自実装（簡易版） | ✅ SettingsValidator.ValidateIpAddress()使用 | リファクタリング |
| ポート番号検証 | ❌ 独自実装（範囲のみ） | ✅ SettingsValidator.ValidatePort()使用 | リファクタリング |
| MonitoringIntervalMs検証 | ❌ 独自実装（範囲が異なる） | ✅ SettingsValidator.ValidateMonitoringIntervalMs()使用 | リファクタリング |
| Timeout検証 | ❌ 未実装 | ✅ SettingsValidator.ValidateTimeout()使用（将来拡張） | 新規追加 |
| ConnectionMethod検証 | ❌ 未実装 | ✅ SettingsValidator.ValidateConnectionMethod()使用（将来拡張） | 新規追加 |
| FrameVersion検証 | ❌ 未実装 | ✅ SettingsValidator.ValidateFrameVersion()使用（将来拡張） | 新規追加 |
| **SavePath検証** | ✅ 実装済み（L140-155） | ✅ **SettingsValidator統合検討**（Phase 2-4対応） | リファクタリング検討 |
| **PlcModel検証** | ✅ 実装済み（L157-161） | ✅ **SettingsValidator統合検討**（Phase 2-3対応） | リファクタリング検討 |

**ConfigurationLoaderExcel固有の検証（維持）**:
- ③ デバイスリスト検証
- ④ 総点数制限チェック
- ⑤ 出力設定検証の一部（SavePath、PlcModelはSettingsValidator統合検討）

**Phase 2-4対応の注記**:
- SavePath検証（L140-155）はPhase 2-4で重要性増加（本番稼働開始）
- PlcModel検証（L157-161）はPhase 2-3で重要性増加（JSON出力に使用）
- 将来的にSettingsValidatorへの統合を検討（現時点では既存実装維持でも可）

---

## ⚠️ 検証範囲の相違点（重要）

### MonitoringIntervalMs検証範囲の違い

| 実装箇所 | 最小値 | 最大値 | 意図 |
|---------|-------|-------|------|
| **SettingsValidator.cs** | 100ms | 60000ms（60秒） | 推奨範囲（現実的な使用範囲） |
| **ConfigurationLoaderExcel.cs** | 1ms | 86400000ms（24時間） | 技術的制約範囲 |

**統合時の対応**:
- ⚠️ **SettingsValidator.csの範囲（100～60000ms）を使用する**
- 理由：Phase 2-2での既定値は1000msであり、SettingsValidatorの推奨範囲内
- ConfigurationLoaderExcelの広範な範囲は過剰（1msや24時間は非現実的）

---

## 📝 TDDサイクル: Phase 2-5

### Step 2-5-1: SettingsValidator統合テスト作成（Red）

**目的**: SettingsValidatorのメソッドがConfigurationLoaderExcel.ValidateConfiguration()で正しく使用されることを確認

#### テストケース名
`Phase2_5_SettingsValidator_IntegrationTests.cs`

#### テストケース詳細

##### 1. test_ValidateConfiguration_不正なIPアドレス_SettingsValidator使用()

```csharp
[Test]
public void test_ValidateConfiguration_不正なIPアドレス_SettingsValidator使用()
{
    // Arrange
    var config = CreateValidPlcConfiguration();
    config.IpAddress = "999.999.999.999"; // 不正なIPアドレス

    // Act & Assert
    var ex = Assert.Throws<ArgumentException>(() =>
        ConfigurationLoaderExcel.ValidateConfigurationPublic(config));

    // SettingsValidator.ValidateIpAddress()のエラーメッセージであることを確認
    Assert.That(ex.Message, Contains.Substring("IPAddressの形式が不正です"));
}
```

##### 2. test_ValidateConfiguration_ポート範囲外_SettingsValidator使用()

```csharp
[Test]
public void test_ValidateConfiguration_ポート範囲外_SettingsValidator使用()
{
    // Arrange
    var config = CreateValidPlcConfiguration();
    config.Port = 99999; // 範囲外（1～65535）

    // Act & Assert
    var ex = Assert.Throws<ArgumentException>(() =>
        ConfigurationLoaderExcel.ValidateConfigurationPublic(config));

    // SettingsValidator.ValidatePort()のエラーメッセージであることを確認
    Assert.That(ex.Message, Contains.Substring("Portの値が範囲外です"));
}
```

##### 3. test_ValidateConfiguration_MonitoringIntervalMs範囲外_SettingsValidator使用()

```csharp
[Test]
public void test_ValidateConfiguration_MonitoringIntervalMs範囲外_SettingsValidator使用()
{
    // Arrange
    var config = CreateValidPlcConfiguration();
    config.MonitoringIntervalMs = 50; // 範囲外（100～60000ms）

    // Act & Assert
    var ex = Assert.Throws<ArgumentException>(() =>
        ConfigurationLoaderExcel.ValidateConfigurationPublic(config));

    // SettingsValidator.ValidateMonitoringIntervalMs()のエラーメッセージであることを確認
    Assert.That(ex.Message, Contains.Substring("MonitoringIntervalMsの値が範囲外です"));
}
```

##### 4. test_ValidateConfiguration_全項目正常_SettingsValidator使用()

```csharp
[Test]
public void test_ValidateConfiguration_全項目正常_SettingsValidator使用()
{
    // Arrange
    var config = CreateValidPlcConfiguration();
    config.IpAddress = "172.30.40.40";
    config.Port = 8192;
    config.MonitoringIntervalMs = 1000;

    // Act & Assert
    Assert.DoesNotThrow(() =>
        ConfigurationLoaderExcel.ValidateConfigurationPublic(config));
}
```

**実装アプローチ**:
- ConfigurationLoaderExcel.ValidateConfiguration()をpublicにするためのテスト用ラッパーメソッドを追加
- または、リフレクションを使用してprivateメソッドをテスト

---

### Step 2-5-2: ConfigurationLoaderExcel.csのリファクタリング（Green）

**目的**: SettingsValidatorのメソッドを使用するようにValidateConfiguration()をリファクタリング

#### 修正箇所
`andon/Infrastructure/Configuration/ConfigurationLoaderExcel.cs:373-463`

#### 修正前（現在の実装）
```csharp
private void ValidateConfiguration(PlcConfiguration config)
{
    // ① 接続情報検証（独自実装）
    if (!System.Net.IPAddress.TryParse(config.IpAddress, out _))
    {
        throw new ArgumentException(
            $"IPアドレスの形式が不正です: {config.IpAddress}");
    }

    if (config.Port < 1 || config.Port > 65535)
    {
        throw new ArgumentException(
            $"ポート番号が範囲外です: {config.Port}（1～65535）");
    }

    // ② データ取得周期（監視間隔）検証（独自実装）
    if (config.MonitoringIntervalMs < 1 || config.MonitoringIntervalMs > 86400000)
    {
        throw new ArgumentException(
            $"データ取得周期が範囲外です: {config.MonitoringIntervalMs}（1～86400000ms）");
    }

    // ... 残りの検証ロジック ...
}
```

#### 修正後（SettingsValidator使用）
```csharp
private readonly SettingsValidator _validator = new SettingsValidator();

private void ValidateConfiguration(PlcConfiguration config)
{
    // ① 接続情報検証（SettingsValidator使用）
    _validator.ValidateIpAddress(config.IpAddress);
    _validator.ValidatePort(config.Port);

    // ② データ取得周期（監視間隔）検証（SettingsValidator使用）
    _validator.ValidateMonitoringIntervalMs(config.MonitoringIntervalMs);

    // 将来拡張: オプション項目の検証
    // _validator.ValidateTimeout(config.Timeout);
    // _validator.ValidateConnectionMethod(config.ConnectionMethod);
    // _validator.ValidateFrameVersion(config.FrameVersion);

    // ③ デバイスリスト検証（ConfigurationLoaderExcel固有）
    if (config.Devices == null || config.Devices.Count == 0)
    {
        throw new ArgumentException(
            $"デバイスが1つも設定されていません: {config.SourceExcelFile}");
    }

    // ... 残りのConfigurationLoaderExcel固有検証ロジック ...
}
```

#### 修正内容
| 行番号 | 修正内容 | 影響範囲 |
|-------|---------|---------|
| **13-14** | `private readonly SettingsValidator _validator;`フィールド追加 | ConfigurationLoaderExcelクラス |
| **21-26** | コンストラクタで`_validator = new SettingsValidator();`を初期化 | コンストラクタ |
| **376-379** | IPアドレス検証を`_validator.ValidateIpAddress(config.IpAddress);`に置換 | ValidateConfiguration() |
| **381-384** | ポート検証を`_validator.ValidatePort(config.Port);`に置換 | ValidateConfiguration() |
| **386-390** | MonitoringIntervalMs検証を`_validator.ValidateMonitoringIntervalMs(config.MonitoringIntervalMs);`に置換 | ValidateConfiguration() |

---

### Step 2-5-3: テスト実行とエラー修正（Green継続）

**目的**: 全テストがパスすることを確認

#### 実行するテスト
```bash
dotnet test --filter "FullyQualifiedName~Phase2_5_SettingsValidator_IntegrationTests" --logger "console;verbosity=minimal"
```

**期待される結果**:
- ✅ test_ValidateConfiguration_不正なIPアドレス_SettingsValidator使用: 成功
- ✅ test_ValidateConfiguration_ポート範囲外_SettingsValidator使用: 成功
- ✅ test_ValidateConfiguration_MonitoringIntervalMs範囲外_SettingsValidator使用: 成功
- ✅ test_ValidateConfiguration_全項目正常_SettingsValidator使用: 成功

#### エラー発生時の対応
- エラーメッセージの不一致 → SettingsValidatorのメッセージ形式を確認
- 検証範囲の不一致 → SettingsValidatorの定数値を確認
- 依存関係エラー → SettingsValidatorの初期化方法を確認

---

### Step 2-5-4: 既存テストへの影響確認（Regression）

**目的**: SettingsValidator統合が既存テストに影響を与えないことを確認

#### 実行するテスト
```bash
# ConfigurationLoaderExcel関連の全テスト
dotnet test --filter "FullyQualifiedName~ConfigurationLoaderExcelTests" --logger "console;verbosity=minimal"

# Phase0～Phase2-1の統合テスト
dotnet test --filter "FullyQualifiedName~Phase0" --logger "console;verbosity=minimal"
dotnet test --filter "FullyQualifiedName~Phase1" --logger "console;verbosity=minimal"
dotnet test --filter "FullyQualifiedName~Phase2_1" --logger "console;verbosity=minimal"
```

**期待される結果**:
- ✅ ConfigurationLoaderExcelの全テストが成功
- ✅ Phase0～Phase2-1の全テストが成功

#### エラー発生時の対応
- エラーメッセージ変更による失敗 → テストケースのアサーションを修正
- 検証範囲変更による失敗 → テストデータを調整

---

### Step 2-5-5: DI設定の追加（Green継続、オプション）

**目的**: SettingsValidatorをDIコンテナに登録（将来拡張用）

#### 修正箇所
`andon/Services/DependencyInjectionConfigurator.cs`

#### 修正内容
```csharp
// andon/Services/DependencyInjectionConfigurator.cs

public static void ConfigureServices(IServiceCollection services)
{
    // ... 既存のDI設定 ...

    // Phase 2-5: SettingsValidator統合（将来拡張用）
    services.AddSingleton<SettingsValidator>();

    // ConfigurationLoaderExcelでのSettingsValidator使用
    // 注: 現時点ではコンストラクタで直接インスタンス化しているが、
    //     将来的にはDI経由での取得に変更可能
}
```

**注意**:
- このステップは**オプション**です
- 現在の実装では、ConfigurationLoaderExcelがSettingsValidatorを直接インスタンス化
- 将来的にSettingsValidatorをDI経由で取得する場合のみ必要

---

### Step 2-5-6: リファクタリングとコード整理（Refactor）

**目的**: コードの可読性と保守性を向上させる

#### リファクタリング対象
1. **ConfigurationLoaderExcel.cs**:
   - ValidateConfiguration()メソッドのコメント整理
   - SettingsValidator使用箇所の明示

2. **SettingsValidator.cs**:
   - エラーメッセージの統一性確認
   - 定数値の妥当性確認

#### リファクタリング内容例
```csharp
// andon/Infrastructure/Configuration/ConfigurationLoaderExcel.cs:373-463

private void ValidateConfiguration(PlcConfiguration config)
{
    // ===== Phase 2-5: SettingsValidator統合 =====
    // 基本設定項目の検証（SettingsValidator使用）
    _validator.ValidateIpAddress(config.IpAddress);
    _validator.ValidatePort(config.Port);
    _validator.ValidateMonitoringIntervalMs(config.MonitoringIntervalMs);

    // 将来拡張: オプション項目の検証
    // _validator.ValidateTimeout(config.Timeout);
    // _validator.ValidateConnectionMethod(config.ConnectionMethod);
    // _validator.ValidateFrameVersion(config.FrameVersion);

    // ===== ConfigurationLoaderExcel固有の検証 =====
    // ③ デバイスリスト検証
    ValidateDeviceList(config);

    // ④ 総点数制限チェック
    ValidateTotalDevicePoints(config);

    // ⑤ 出力設定検証
    ValidateOutputSettings(config);
}

private void ValidateDeviceList(PlcConfiguration config)
{
    if (config.Devices == null || config.Devices.Count == 0)
    {
        throw new ArgumentException(
            $"デバイスが1つも設定されていません: {config.SourceExcelFile}");
    }

    foreach (var device in config.Devices)
    {
        if (device.DeviceNumber < 0 || device.DeviceNumber > 0xFFFFFF)
        {
            throw new ArgumentOutOfRangeException(
                nameof(device.DeviceNumber),
                $"デバイス番号が範囲外です: {device.DeviceNumber}（項目名: {device.ItemName}、範囲: 0～16777215）");
        }
    }
}

private void ValidateTotalDevicePoints(PlcConfiguration config)
{
    // ... 総点数制限チェックロジック ...
}

private void ValidateOutputSettings(PlcConfiguration config)
{
    // ... SavePath、PlcModel検証ロジック ...
}
```

---

## 📊 完了判定基準

### 必須条件
- ✅ SettingsValidatorのメソッドがConfigurationLoaderExcel.ValidateConfiguration()で使用されている
- ✅ Phase 2-5統合テストが全て成功（4/4テスト）
- ✅ ConfigurationLoaderExcel関連の全既存テストが成功
- ✅ Phase 0～Phase 2-1の全既存テストが成功
- ✅ IPアドレス、ポート、MonitoringIntervalMsの検証がSettingsValidator経由で実行される

### オプション条件
- ⏳ SettingsValidatorのDI登録（将来拡張用）
- ⏳ Timeout、ConnectionMethod、FrameVersionの検証追加（将来拡張用）
- ⏳ ConfigurationLoaderExcel固有検証ロジックのメソッド分割（リファクタリング）

---

## 🚀 実装後の期待される状態

### コード品質の向上
1. **検証ロジックの統一**:
   - SettingsValidator.csが唯一の検証ロジック実装場所
   - ConfigurationLoaderExcel.csは検証ロジックを呼び出すのみ
   - 検証ロジックの保守性向上

2. **テストカバレッジの統一**:
   - SettingsValidatorTestsで検証ロジックをテスト
   - ConfigurationLoaderExcelTestsで統合動作をテスト
   - 重複テストの削減

3. **将来拡張の容易化**:
   - 新規検証項目の追加が容易（SettingsValidatorに追加するのみ）
   - 検証範囲の変更が容易（SettingsValidatorの定数を変更するのみ）

### 検証範囲の最適化
| 項目 | 変更前（ConfigurationLoaderExcel独自） | 変更後（SettingsValidator統合） |
|------|-----------------------------------|----------------------------|
| MonitoringIntervalMs | 1～86400000ms | 100～60000ms（推奨範囲） |
| IPアドレス | 簡易検証 | 厳密検証（IPv4形式、0.0.0.0禁止） |
| ポート | 1～65535 | 1～65535（定数管理） |

---

## 📝 補足事項

### Phase 2-2との関係
- Phase 2-2: MonitoringIntervalMsの使用箇所をExcel設定に移行
- Phase 2-5: MonitoringIntervalMsの検証ロジックをSettingsValidatorに統合
- **独立実施可能**: Phase 2-2完了前にPhase 2-5を実施しても問題なし

### Phase 3との関係
- Phase 3: appsettings.json完全廃止
- Phase 2-5完了により、Phase 3実施時の検証ロジック調整が不要

### 技術負債の解消
- ✅ 検証ロジックの重複を解消
- ✅ SettingsValidatorがテスト専用から本番コードへ昇格
- ✅ 検証範囲の統一（MonitoringIntervalMs: 100～60000ms）

---

## 🔗 関連ドキュメント

### 前提条件（完了済み）
- [Phase 0: 即座削除項目](Phase0_即座削除項目.md) → **完了** ✅
- [Phase 1: テスト専用項目整理](Phase1_テスト専用項目整理.md) → **完了** ✅
- [Phase 2-1: LoggingConfigハードコード化](Phase2-1_LoggingConfig_ハードコード化.md) → **完了** ✅
- [Phase 2-2: MonitoringIntervalMsのExcel移行](Phase2-2_MonitoringIntervalMs_Excel移行.md) → **完了** ✅
- [Phase 2-3: PlcModelのJSON出力実装](Phase2-3_PlcModel_JSON出力実装.md) → **完了** ✅
- [Phase 2-4: SavePathの利用実装](Phase2-4_SavePath_利用実装.md) → **完了** ✅

### 実装結果
- [Phase 2-1 実装結果](../実装結果/Phase2_1_LoggingConfig_Hardcoding_TestResults.md)
- [Phase 2-2 実装結果](../実装結果/Phase2_2_MonitoringInterval_Excel移行_TestResults.md)
- [Phase 2-3 実装結果](../実装結果/Phase2_3_PlcModel_JSON出力_TestResults.md)
- [Phase 2-4 実装結果](../実装結果/Phase2_4_SavePath_利用実装_TestResults.md)
- [Phase 2-5 実装結果](../実装結果/Phase2_5_SettingsValidator統合_TestResults.md) → **完了** ✅ (2025-12-03)

### 実装ファイル
- [SettingsValidator.cs実装](../../andon/Infrastructure/Configuration/SettingsValidator.cs)
- [ConfigurationLoaderExcel.cs実装](../../andon/Infrastructure/Configuration/ConfigurationLoaderExcel.cs)

### 次フェーズ
- [Phase 3: appsettings.json完全廃止](Phase3_appsettings完全廃止.md) → ⏳ 未着手
