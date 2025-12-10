# Phase 2-3: PlcModelのJSON出力実装

**フェーズ**: Phase 2-3（新規追加）
**影響度**: 中（JSON出力の完全性に影響）
**工数**: **小**（Phase 1-5完了により簡略化）
**前提条件**: Phase 0完了（✅ 2025-12-02）, Phase 1完了（✅ 2025-12-02）, Phase 2-1完了（✅ 2025-12-03）, Phase 2-2完了（✅ 2025-12-03）
**状態**: ⏳ 未着手

---

## 📋 概要

PlcModelをJSON出力に追加します。現在、Excel設定から読み込まれているが、DataOutputManagerに渡されず、JSON出力に含まれていない問題を修正します。

**✅ Phase 1-5完了により、Excel読み込み処理は既に実装済みです。DataOutputManagerへの引数追加のみで完了します。**

---

## 🔄 Phase 2-2からの引き継ぎ事項

### Phase 2-2完了により確立された知見

**Phase 2-2実装結果**: [Phase2_2_MonitoringInterval_Excel移行_TestResults.md](../実装結果/Phase2_2_MonitoringInterval_Excel移行_TestResults.md)

#### ✅ TDDサイクルの成功パターン

Phase 2-2では以下のTDDサイクルを完全遵守し、8/8テスト成功を達成しました：

| ステップ | 内容 | Phase 2-2結果 |
|---------|------|---------------|
| **Red** | 失敗するテストを先に書く | 失敗: 2、合格: 6（期待通り） |
| **Green** | テストを通すための最小限のコードを実装 | 成功: 8、失敗: 0 |
| **Refactor** | 動作を保ったままコードを改善 | 成功: 8、失敗: 0 |

**Phase 2-3でも同じアプローチを適用します。**

#### ✅ 既存テスト修正の重要性

Phase 2-2では、実装完了後に以下の既存テスト修正が必要でした：

| 修正対象 | 修正内容 | 影響範囲 |
|---------|---------|---------|
| **ExecutionOrchestratorTests.cs** | DataProcessingConfig参照削除 | 1テスト削除、5箇所修正 |
| **DependencyInjectionConfigurator.cs** | DataProcessingConfig DI登録削除 | DI設定修正 |

**Phase 2-3での注意点**:
- DataOutputManager.OutputToJson()のシグネチャ変更により、以下のテストファイルで修正が必要：
  - DataOutputManagerTests.cs
  - ExecutionOrchestratorTests.cs（OutputToJson呼び出し箇所）
  - Step3_6_IntegrationTests.cs（統合テスト）

#### ✅ appsettings.json完全空化の維持

Phase 2-2完了時点でappsettings.jsonは以下の状態（コメントのみ、5行）：

```json
{
  // Phase 2-2完了: appsettings.json完全空化
  // MonitoringIntervalMsは各PlcConfigurationから取得
  // Phase 3でこのファイル自体を削除予定
}
```

**Phase 2-3はappsettings.jsonに影響しません。** PlcModelは既にExcel設定から読み込まれているため、JSON出力への追加のみです。

#### ✅ Excel設定の活用状況（Phase 2-2完了時点）

| 項目 | Excel読み込み | モデル格納 | 使用状況 |
|------|------------|----------|---------|
| **MonitoringIntervalMs** | ✅ ConfigurationLoaderExcel.cs:115 | ✅ PlcConfiguration | **✅ Phase 2-2完了: ExecutionOrchestrator.cs:98-100で使用** |
| **PlcModel** | ✅ ConfigurationLoaderExcel.cs:116 | ✅ PlcConfiguration | **⏳ Phase 2-3対象: DataOutputManagerに渡す** |
| **SavePath** | ✅ ConfigurationLoaderExcel.cs:117 | ✅ PlcConfiguration | ⏳ Phase 2-4対象: ハードコード削除 |

**Phase 2-3の作業範囲**: PlcModelをDataOutputManager.OutputToJson()に渡し、JSON出力に含める。

#### ⚠️ Phase 2-2で遭遇した問題と対策

**問題1**: ビルドエラー（DataProcessingConfig参照が残る）
- **原因**: 既存テストコードが古いシグネチャを参照
- **対策**: Green段階完了後、全ファイルをビルドして参照エラーを洗い出す
- **Phase 2-3での対応**: DataOutputManager.OutputToJson()のシグネチャ変更後、全テストファイルをビルド確認

**問題2**: 同じプロジェクト内で複数バージョンのシグネチャが混在
- **原因**: インターフェースと実装の両方を修正する必要があるが、片方のみ修正
- **対策**: インターフェース（IDataOutputManager）と実装（DataOutputManager）を同時に修正
- **Phase 2-3での対応**: IDataOutputManager.csとDataOutputManager.csを同時修正

**問題3**: Mockオブジェクトのセットアップ更新漏れ
- **原因**: テストコードのMock.Setup()が古いシグネチャを使用
- **対策**: 全テストファイルで`It.IsAny<string>()`（plcModel用）を追加
- **Phase 2-3での対応**: DataOutputManagerを使用する全てのテストでMockセットアップを更新

#### ✅ Phase 2-2のビルド結果（参考）

```
ビルドに成功しました。
    0 エラー
    59 警告
```

**Phase 2-3でも同様の結果を目指します。**

---

## ⚠️ Phase 1-5完了による工数削減（重要）

### 既に完了している作業

#### ✅ Phase 2完了事項（ConfigurationLoaderExcel拡張）

| 完了項目 | 実装箇所 | 内容 |
|---------|---------|------|
| **Excel読み込み実装** | ConfigurationLoaderExcel.cs:116 | `PlcModel = ReadCell<string>(settingsSheet, "B12", "デバイス名")` |
| **モデル格納** | PlcConfiguration.PlcModel | プロパティ定義済み |
| **Excel位置** | settingsシート B12セル | "デバイス名（ターゲット名）" |

### 残りの作業（小規模修正）

| 作業内容 | 影響箇所 | 工数 |
|---------|---------|------|
| **DataOutputManager.OutputToJson()のシグネチャ変更** | DataOutputManager.cs | **小** |
| **ExecutionOrchestrator.csでの引数追加** | ExecutionOrchestrator.cs:227 | **小** |
| **JSON出力にPlcModelを追加** | DataOutputManager.cs | **小** |

---

## 🎯 対象項目（1項目）

| 項目 | 現状 | 修正後 | 理由 |
|------|------|--------|------|
| PlcModel | ✅ Excel読み込み完了<br>❌ DataOutputManagerに渡されず<br>❌ JSON出力に含まれない | ✅ DataOutputManagerに渡される<br>✅ JSON出力の`source.plcModel`に含まれる | 設計仕様（設定読み込み仕様.md:36）との一致 |

---

## 🔍 現在の実装確認

### 設計仕様との不一致

**設計仕様（設定読み込み仕様.md:36）**:
```json
{
  "source": {
    "timestamp": "2025-12-02T10:00:00Z",
    "ipAddress": "172.30.40.40",
    "port": 8192,
    "plcModel": "5_JRS_N2"  // ← 設計仕様では必須
  }
}
```

**現在の実装（実装されていない）**:
```json
{
  "source": {
    "timestamp": "2025-12-02T10:00:00Z",
    "ipAddress": "172.30.40.40",
    "port": 8192
    // plcModel が出力されていない！
  }
}
```

### ConfigurationLoaderExcel.csでの実装（✅ 完了済み）

```csharp
// andon/Infrastructure/Configuration/ConfigurationLoaderExcel.cs:116
// ✅ Phase 2完了: Excel読み込み実装済み

PlcModel = ReadCell<string>(settingsSheet, "B12", "デバイス名"),
```

### PlcConfigurationモデル（✅ 完了済み）

```csharp
// andon/Core/Models/ConfigModels/PlcConfiguration.cs
// ✅ Phase 2完了: プロパティ定義済み

public string PlcModel { get; set; }
```

### 問題箇所（修正が必要）

```csharp
// andon/Core/Controllers/ExecutionOrchestrator.cs:227

var outputResult = await _dataOutputManager.OutputToJson(
    plcConfig.IpAddress,
    plcConfig.Port,
    /* plcConfig.PlcModel が渡されていない！ */
    structuredData.Devices,
    outputDirectory
);
```

---

## 📝 TDDサイクル: Phase 2-3

### Step 2-3-1: PlcModelのJSON出力テスト作成（Red）

**目的**: PlcModelがJSON出力に正しく含まれることを確認

#### テストケース名
`Phase2_3_PlcModel_JsonOutputTests.cs`

#### テストケース詳細

##### 1. test_DataOutputManager_PlcModelをJSON出力()

```csharp
[Test]
public async Task test_DataOutputManager_PlcModelをJSON出力()
{
    // Arrange
    string plcModel = "5_JRS_N2";
    var devices = CreateSampleDevices();
    var dataOutputManager = new DataOutputManager(_loggingManager);

    // Act
    var result = await dataOutputManager.OutputToJson(
        "172.30.40.40",
        8192,
        plcModel, // ← 新規追加パラメータ
        devices,
        "./output"
    );

    // Assert
    Assert.That(result.Success, Is.True);

    // JSON出力内容の確認
    var jsonContent = File.ReadAllText(result.OutputFilePath);
    var jsonObject = JsonSerializer.Deserialize<JsonDocument>(jsonContent);

    var sourcePlcModel = jsonObject.RootElement.GetProperty("source").GetProperty("plcModel").GetString();
    Assert.That(sourcePlcModel, Is.EqualTo("5_JRS_N2"));
}
```

##### 2. test_DataOutputManager_PlcModel空文字列の場合()

```csharp
[Test]
public async Task test_DataOutputManager_PlcModel空文字列の場合()
{
    // Arrange
    string plcModel = ""; // 空文字列
    var devices = CreateSampleDevices();
    var dataOutputManager = new DataOutputManager(_loggingManager);

    // Act
    var result = await dataOutputManager.OutputToJson(
        "172.30.40.40",
        8192,
        plcModel,
        devices,
        "./output"
    );

    // Assert
    Assert.That(result.Success, Is.True);

    // JSON出力内容の確認
    var jsonContent = File.ReadAllText(result.OutputFilePath);
    var jsonObject = JsonSerializer.Deserialize<JsonDocument>(jsonContent);

    // 空文字列でもフィールドは存在する
    var sourcePlcModel = jsonObject.RootElement.GetProperty("source").GetProperty("plcModel").GetString();
    Assert.That(sourcePlcModel, Is.EqualTo(""));
}
```

##### 3. test_DataOutputManager_PlcModelがnullの場合()

```csharp
[Test]
public async Task test_DataOutputManager_PlcModelがnullの場合()
{
    // Arrange
    string plcModel = null; // null
    var devices = CreateSampleDevices();
    var dataOutputManager = new DataOutputManager(_loggingManager);

    // Act
    var result = await dataOutputManager.OutputToJson(
        "172.30.40.40",
        8192,
        plcModel,
        devices,
        "./output"
    );

    // Assert
    Assert.That(result.Success, Is.True);

    // JSON出力内容の確認
    var jsonContent = File.ReadAllText(result.OutputFilePath);
    var jsonObject = JsonSerializer.Deserialize<JsonDocument>(jsonContent);

    // nullの場合、フィールドが存在しない or 空文字列
    if (jsonObject.RootElement.GetProperty("source").TryGetProperty("plcModel", out var plcModelElement))
    {
        Assert.That(plcModelElement.GetString(), Is.Empty);
    }
}
```

##### 4. test_ExecutionOrchestrator_PlcModelをDataOutputManagerに渡す()

```csharp
[Test]
public async Task test_ExecutionOrchestrator_PlcModelをDataOutputManagerに渡す()
{
    // Arrange
    var plcConfig = new PlcConfiguration
    {
        IpAddress = "172.30.40.40",
        Port = 8192,
        PlcModel = "5_JRS_N2"
    };
    var orchestrator = CreateOrchestratorWithMockDataOutputManager();

    // Act
    await orchestrator.RunDataCycleAsync(plcConfig);

    // Assert
    // DataOutputManager.OutputToJson()がplcConfig.PlcModelを受け取ったことを確認
    _mockDataOutputManager.Verify(
        x => x.OutputToJson(
            "172.30.40.40",
            8192,
            "5_JRS_N2", // ← plcConfig.PlcModelが渡されている
            It.IsAny<List<DeviceData>>(),
            It.IsAny<string>()
        ),
        Times.Once
    );
}
```

#### 期待される結果
Step 2-3-2の実装前は失敗（PlcModelパラメータが存在しないため）

---

### Step 2-3-2: 実装（Green）- 簡略化版

**✅ Phase 1-5完了により、Excel読み込み処理の追加実装は不要です。DataOutputManagerへの引数追加のみで完了します。**

#### 作業内容

##### 1. DataOutputManager.cs のシグネチャ変更

```csharp
// 修正前
public async Task<DataOutputResult> OutputToJson(
    string ipAddress,
    int port,
    List<DeviceData> devices,
    string outputDirectory)
{
    // ...

    var jsonData = new
    {
        source = new
        {
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ipAddress = ipAddress,
            port = port
            // plcModel が含まれていない
        },
        devices = devices
    };

    // ...
}
```

```csharp
// 修正後
public async Task<DataOutputResult> OutputToJson(
    string ipAddress,
    int port,
    string plcModel, // ← 新規追加パラメータ
    List<DeviceData> devices,
    string outputDirectory)
{
    // ...

    var jsonData = new
    {
        source = new
        {
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ipAddress = ipAddress,
            port = port,
            plcModel = plcModel ?? "" // ← plcModelを追加（nullの場合は空文字列）
        },
        devices = devices
    };

    // ...
}
```

##### 2. IDataOutputManager.cs のシグネチャ変更

```csharp
// 修正前
public interface IDataOutputManager
{
    Task<DataOutputResult> OutputToJson(
        string ipAddress,
        int port,
        List<DeviceData> devices,
        string outputDirectory);
}
```

```csharp
// 修正後
public interface IDataOutputManager
{
    Task<DataOutputResult> OutputToJson(
        string ipAddress,
        int port,
        string plcModel, // ← 新規追加パラメータ
        List<DeviceData> devices,
        string outputDirectory);
}
```

##### 3. ExecutionOrchestrator.cs での呼び出し修正

```csharp
// 修正前（L227あたり）
var outputResult = await _dataOutputManager.OutputToJson(
    plcConfig.IpAddress,
    plcConfig.Port,
    /* plcConfig.PlcModel が渡されていない */
    structuredData.Devices,
    outputDirectory
);
```

```csharp
// 修正後
var outputResult = await _dataOutputManager.OutputToJson(
    plcConfig.IpAddress,
    plcConfig.Port,
    plcConfig.PlcModel, // ← PlcModelを追加
    structuredData.Devices,
    outputDirectory
);
```

##### 4. テスト実行 → 全テストがパス

```bash
dotnet test --filter "FullyQualifiedName~Phase2_3"
dotnet test --filter "FullyQualifiedName~DataOutputManager"
dotnet test  # 全テスト実行
```

**⚠️ 重要**:
- ✅ Excel読み込み（ConfigurationLoaderExcel.cs:116）は既に実装完了（Phase 2完了）
- ✅ PlcConfiguration.PlcModelに格納済み
- **Excel読み込み処理の追加実装は不要。DataOutputManagerへの引数追加のみで完了。**

---

### Step 2-3-3: リファクタリング（Refactor）

**作業内容**:

#### 1. nullチェックとデフォルト値の処理

```csharp
// DataOutputManager.cs

/// <summary>
/// PlcModelのnull/空文字列チェック
/// </summary>
/// <param name="plcModel">PLCモデル</param>
/// <returns>検証済みPLCモデル（nullの場合は空文字列）</returns>
private string ValidatePlcModel(string plcModel)
{
    if (string.IsNullOrWhiteSpace(plcModel))
    {
        _loggingManager.LogWarning("PlcModel is null or empty, using empty string");
        return "";
    }

    return plcModel;
}

public async Task<DataOutputResult> OutputToJson(
    string ipAddress,
    int port,
    string plcModel,
    List<DeviceData> devices,
    string outputDirectory)
{
    var validatedPlcModel = ValidatePlcModel(plcModel);

    var jsonData = new
    {
        source = new
        {
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ipAddress = ipAddress,
            port = port,
            plcModel = validatedPlcModel
        },
        devices = devices
    };

    // ...
}
```

#### 2. XMLドキュメントコメントの追加

```csharp
/// <summary>
/// JSON形式でデータを出力
/// </summary>
/// <param name="ipAddress">PLCのIPアドレス</param>
/// <param name="port">PLCのポート</param>
/// <param name="plcModel">PLCモデル（デバイス名）</param> // ← 追加
/// <param name="devices">デバイスデータリスト</param>
/// <param name="outputDirectory">出力先ディレクトリ</param>
/// <returns>データ出力結果</returns>
public async Task<DataOutputResult> OutputToJson(
    string ipAddress,
    int port,
    string plcModel,
    List<DeviceData> devices,
    string outputDirectory)
{
    // ...
}
```

#### 3. テスト再実行 → 全テストがパス

```bash
dotnet test --filter "FullyQualifiedName~Phase2_3"
dotnet test --filter "FullyQualifiedName~DataOutputManager"
dotnet test  # 全テスト実行
```

---

## ✅ 完了条件

### Phase 2-3完了の定義

以下の条件をすべて満たすこと：

1. ✅ DataOutputManager.cs の修正
   - OutputToJson()のシグネチャに`string plcModel`パラメータを追加
   - JSON出力に`source.plcModel`を追加

2. ✅ IDataOutputManager.cs の修正
   - OutputToJson()のシグネチャに`string plcModel`パラメータを追加

3. ✅ ExecutionOrchestrator.cs の修正
   - OutputToJson()呼び出し時に`plcConfig.PlcModel`を渡す

4. ✅ Phase2_3_PlcModel_JsonOutputTests.cs の全テストがパス

5. ✅ 既存のすべてのDataOutputManager関連テストがパス

6. ✅ 全体テストがパス

7. ✅ ビルドエラーなし

### 確認コマンド

```bash
# Phase 2-3のテスト確認
dotnet test --filter "FullyQualifiedName~Phase2_3"

# DataOutputManager関連テスト確認
dotnet test --filter "FullyQualifiedName~DataOutputManager"

# 全体テスト確認
dotnet test

# ビルド確認
dotnet build
```

---

## 🚨 注意事項

### 1. 既存テストコードの修正

**影響を受けるテストコード**:
- DataOutputManagerTests.cs
- ExecutionOrchestratorTests.cs（DataOutputManagerを使用している箇所）
- Step3_6_IntegrationTests.cs（統合テスト）

**修正内容**:
```csharp
// 修正前（既存テスト）
await _dataOutputManager.OutputToJson(
    "172.30.40.40",
    8192,
    devices,
    "./output"
);

// 修正後
await _dataOutputManager.OutputToJson(
    "172.30.40.40",
    8192,
    "5_JRS_N2", // ← PlcModelを追加
    devices,
    "./output"
);
```

### 2. PlcModelの設計仕様

**設定読み込み仕様.md:36での定義**:
```json
{
  "source": {
    "plcModel": "5_JRS_N2"  // ← 必須フィールド
  }
}
```

**Phase 2-3完了により**:
- ✅ JSON出力に`source.plcModel`が含まれる
- ✅ Excel設定（settingsシート B12セル）から読み込まれる
- ✅ 設計仕様との一致

### 3. nullと空文字列の扱い

**推奨実装**:
- PlcModelがnullの場合: 空文字列に変換
- PlcModelが空文字列の場合: そのまま出力
- JSON出力では`"plcModel": ""`となる

**理由**:
- JSON形式の一貫性を保つ
- クライアント側でのパース処理が容易

---

## 📊 JSON出力形式の変更

### 修正前（PlcModel未出力）

```json
{
  "source": {
    "timestamp": "2025-12-02T10:00:00Z",
    "ipAddress": "172.30.40.40",
    "port": 8192
  },
  "devices": [...]
}
```

### 修正後（PlcModel出力）

```json
{
  "source": {
    "timestamp": "2025-12-02T10:00:00Z",
    "ipAddress": "172.30.40.40",
    "port": 8192,
    "plcModel": "5_JRS_N2"  // ← 追加
  },
  "devices": [...]
}
```

---

## 🔄 Phase 2-2との違い

| 項目 | Phase 2-2（✅ 完了） | Phase 2-3（⏳ 未着手） |
|------|-----------|-----------|
| **対象項目** | MonitoringIntervalMs | PlcModel |
| **修正内容** | 使用箇所の変更（appsettings.json → Excel設定） | JSON出力への追加 |
| **影響度** | 中（タイマー間隔） | 中（JSON出力の完全性） |
| **工数** | 小 | **小** |
| **Excel読み込み実装** | **✅ 完了済み（Phase 1-5）** | **✅ 完了済み（Phase 1-5）** |
| **修正箇所** | ExecutionOrchestrator.cs:98-100の1箇所 | DataOutputManager.cs, IDataOutputManager.cs, ExecutionOrchestrator.cs:243 |
| **削除ファイル** | DataProcessingConfig.cs | なし |
| **DI設定変更** | あり（DataProcessingConfig削除） | なし |
| **appsettings.json影響** | あり（PlcCommunicationセクション削除） | **なし** |
| **TDDサイクル** | Red→Green→Refactor（8/8テスト成功） | 同様のアプローチを適用予定 |
| **既存テスト修正** | ExecutionOrchestratorTests.cs（6箇所） | DataOutputManagerTests.cs, ExecutionOrchestratorTests.cs, 統合テスト |

### Phase 2-2からの教訓

**Phase 2-2で成功した点**:
- ✅ TDDサイクルの厳格な遵守（Red→Green→Refactor）
- ✅ 境界値テストの実施（1ms, 1000ms, 5000ms, 3600000ms, 0ms, -1ms）
- ✅ 既存テストの網羅的な修正（ExecutionOrchestratorTests.cs 6箇所）
- ✅ インターフェースと実装の同時修正（IOptions依存削除）

**Phase 2-3で適用すべき教訓**:
- ⚠️ DataOutputManager.OutputToJson()のシグネチャ変更時、IDataOutputManagerも同時修正
- ⚠️ 既存テストでMock.Setup()のシグネチャを更新（`It.IsAny<string>()`追加）
- ⚠️ Green段階完了後、必ず全ファイルのビルドで参照エラーを確認
- ⚠️ Refactor段階でXMLドキュメントコメント、using文の整理を実施

---

## 📈 次のステップ

### Phase 2-3実装前の準備

**Phase 2-2完了情報の確認**:
- ✅ 実装結果: [Phase2_2_MonitoringInterval_Excel移行_TestResults.md](../実装結果/Phase2_2_MonitoringInterval_Excel移行_TestResults.md)
- ✅ TDDサイクル: Red→Green→Refactor 完全遵守
- ✅ テスト結果: 8/8合格（100%）
- ✅ 全体テスト結果: 825/837合格
- ✅ appsettings.json: 完全空化（5行、コメントのみ）

**Phase 2-3実装開始時の確認事項**:
1. Phase 2-2実装結果文書を読み、TDDサイクルの成功パターンを理解
2. Phase 2-2で遭遇した問題（ビルドエラー、シグネチャ変更の影響範囲）を確認
3. 既存テスト修正の必要性を認識（DataOutputManagerTests.cs, ExecutionOrchestratorTests.cs, 統合テスト）
4. IDataOutputManager.csとDataOutputManager.csの同時修正を計画

### Phase 2-3完了後の進行先

Phase 2-3完了後、Phase 2-4（SavePathの利用実装）に進みます。

→ [Phase2-4_SavePath_利用実装.md](./Phase2-4_SavePath_利用実装.md)

### 累積進捗（Phase 0～2-2完了時点）

| 項目 | Phase 0開始前 | Phase 2-2完了後 | 累積削減量 |
|------|-------------|---------------|------------|
| **appsettings.json** | 101行 | 5行（コメントのみ） | **96行削減（95%削減）** |
| **削除クラスファイル** | - | - | **10ファイル削除** |
| **削除テストファイル** | - | - | **3ファイル削除** |

**Phase 2-3ではappsettings.jsonへの影響はありません。** Excel設定からJSON出力への橋渡しのみを実装します。
