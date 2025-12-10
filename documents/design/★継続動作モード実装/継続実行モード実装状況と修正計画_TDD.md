# 継続実行モード実装状況と修正計画（TDD準拠）

**作成日**: 2025-11-28
**最終更新**: 2025-11-28
**対象バージョン**: Phase 1 継続実行モード実装
**実装方針**: TDD（Test-Driven Development）

---

## 📝 更新履歴

### 2025-11-28 更新2: 実装状況との整合性確認・文書修正

**変更内容**:
1. **インターフェース定義の追加を反映**
   - IPlcCommunicationManagerに `ExecuteStep3to5CycleAsync()` と `ExecuteFullCycleAsync()` を追加
   - 実装済みメソッドのインターフェース定義が不足していた問題を解決

2. **ExecutionOrchestratorの実装パス整理**
   - パス1（MultiPlcConfig版、実装済み）とパス2（継続実行モード用、未実装）を明確化
   - 両パスの設計思想の違いを記載（ステートレス vs ステートフル）

3. **ConfigToFrameManagerの対応型を明記**
   - TargetDeviceConfig版（appsettings.json用）
   - PlcConfiguration版（Excel読み込み用）
   - 各メソッドの行番号と内部実装を明記

4. **実装上の課題を詳細化**
   - PlcConfiguration情報の保持方法が未決定
   - PlcCommunicationManagerから設定情報を取得する手段が必要
   - 3つの解決オプションを提示

5. **TDD計画を実装現状に合わせて修正**
   - ExecutionOrchestratorへのDI追加要件を明記
   - テスト用メソッドの追加要件を明記
   - 実装前の設計決定事項（Phase 0）を追加

6. **まとめセクションの大幅更新**
   - 実装済み機能と未実装機能を明確化
   - 設計上の課題を整理
   - Phase 0（設計決定）を次のアクションに追加

**修正方針**: 元の実装を基準として文書を合わせる

---

## 📋 目次

1. [現在の実装状況](#現在の実装状況)
2. [問題点の詳細分析](#問題点の詳細分析)
3. [TDDサイクルによる修正計画](#tddサイクルによる修正計画)
4. [データフロー検証結果](#データフロー検証結果)
5. [実装チェックリスト](#実装チェックリスト)

---

## 現在の実装状況

### ✅ 実装完了している機能

#### Step1: 設定ファイル読み込み
- **ConfigurationLoader.LoadPlcConnectionConfig()**: 完全実装
  - appsettings.json → TargetDeviceConfig
  - 設定検証機能完備
- **Excel読み込み**: PlcConfiguration 実装済み

#### Step2: フレーム構築
- **ConfigToFrameManager**: 完全実装（2種類の設定型に対応）
  - `BuildReadRandomFrameFromConfig(TargetDeviceConfig)` → byte[]
    - appsettings.json用の設定型
  - `BuildReadRandomFrameFromConfigAscii(TargetDeviceConfig)` → string
    - appsettings.json用の設定型（ASCII形式）
  - `BuildReadRandomFrameFromConfig(PlcConfiguration)` → byte[]（L151-168）
    - Excel読み込み用の設定型
    - 内部で SlmpFrameBuilder.BuildReadRandomRequest() を呼び出し
  - `BuildReadRandomFrameFromConfigAscii(PlcConfiguration)` → string（L125-142）
    - Excel読み込み用の設定型（ASCII形式）
    - 内部で SlmpFrameBuilder.BuildReadRandomRequestAscii() を呼び出し

#### Step3-5: PLC通信サイクル
- **PlcCommunicationManager.ExecuteStep3to5CycleAsync()**: 完全実装
  - ConnectAsync() → ConnectionResponse
  - SendFrameAsync() → void
  - ReceiveResponseAsync() → RawResponseData
  - DisconnectAsync() → DisconnectResult
  - 戻り値: CycleExecutionResult
  - **インターフェース**: IPlcCommunicationManager.ExecuteStep3to5CycleAsync() (2025-11-28追加)

#### Step6: データ処理
- **ProcessReceivedRawData()**: 完全実装
  - RawResponseData → BasicProcessedResponseData
- **ParseRawToStructuredData()**: 完全実装
  - ProcessedResponseData → StructuredData
- **ExecuteFullCycleAsync()**: Step3-6統合完了（単独実行可能）
  - **インターフェース**: IPlcCommunicationManager.ExecuteFullCycleAsync() (2025-11-28追加)

#### Step7: データ出力
- **DataOutputManager.OutputToJson()**: 完全実装
  - ProcessedResponseData → JSON出力

---

### 📌 実装済みだが文書に未記載の機能

#### ExecutionOrchestrator の別実装パス（MultiPlcConfig版）

**ファイル**: `andon/Core/Controllers/ExecutionOrchestrator.cs`
**該当箇所**: L95-204

ExecutionOrchestratorには、継続実行モード用とは別に、MultiPlcConfig を使用した実装パスが存在します。

```csharp
// パス1: MultiPlcConfig版（実装済み）
public async Task<MultiPlcExecutionResult> ExecuteMultiPlcCycleAsync(
    MultiPlcConfig config,
    CancellationToken cancellationToken = default)
{
    // MultiPlcCoordinator を使用した並列/順次実行制御
    // ExecuteSinglePlcAsync() → ExecuteStep3to5CycleAsync() を呼び出し
}

// パス2: 継続実行モード用（未実装）
private async Task ExecuteMultiPlcCycleAsync_Internal(
    List<IPlcCommunicationManager> plcManagers,
    CancellationToken cancellationToken)
{
    // TODO: Phase 1で実装予定（現在は空実装）
}
```

**パス1の特徴（実装済み）**:
- MultiPlcConfig構造体を受け取る
- MultiPlcCoordinator による並列/順次実行制御
- ExecuteSinglePlcAsync() 経由で ExecuteStep3to5CycleAsync() を使用
- Step3-5のみ実行（Step6データ処理は含まない）
- フレーム構築: SlmpFrameBuilder.BuildReadRandomRequest() を直接呼び出し
- PlcCommunicationManagerをメソッド内で新規作成

**パス2の想定（未実装）**:
- List<IPlcCommunicationManager> を受け取る
- 継続実行モード（MonitoringIntervalMs 間隔）
- ExecuteFullCycleAsync() を使用してStep3-6を実行する想定
- フレーム構築: ConfigToFrameManager を使用する想定
- PlcCommunicationManagerは事前に初期化されたものを使用
- **現在は空実装**

**実装上の考慮点**:
- パス1とパス2は設計思想が異なる
- パス1: 設定から毎回Managerを生成する「ステートレス」アプローチ
- パス2: 事前初期化されたManagerを再利用する「ステートフル」アプローチ
- どちらを継続実行モードの標準とするか検討が必要

---

### ❌ 未実装・不完全な機能

#### 🔴 問題1: ExecutionOrchestrator の周期実行ロジック（パス2）が空実装

**ファイル**: `andon/Core/Controllers/ExecutionOrchestrator.cs`
**該当箇所**: L82-88
**関連**: パス1（L95-204）は実装済みだが、継続実行モードでは使用されていない

```csharp
private async Task ExecuteMultiPlcCycleAsync_Internal(
    List<IPlcCommunicationManager> plcManagers,
    CancellationToken cancellationToken)
{
    // TODO: Phase 1で実装予定（現在は空実装）
    await Task.CompletedTask;
}
```

**影響**: StartContinuousDataCycleAsync() が MonitoringIntervalMs 間隔で呼び出されるが、**何も処理されない**

---

#### 🔴 問題2: ApplicationController の PlcManager 初期化が未完成

**ファイル**: `andon/Core/Controllers/ApplicationController.cs`
**該当箇所**: L48-74

```csharp
public async Task<InitializationResult> ExecuteStep1InitializationAsync(...)
{
    var configs = _configManager.GetAllConfigurations();
    _plcManagers = new List<IPlcCommunicationManager>();

    // TODO: DIから取得したPlcCommunicationManagerを設定ごとに初期化

    return new InitializationResult
    {
        Success = true,
        PlcCount = configs.Count
    };
}
```

**影響**: _plcManagers リストが空のまま、周期実行に渡される

**実装上の課題**:

1. **PlcCommunicationManagerの生成方法**
   - パターンA: `new PlcCommunicationManager(connectionConfig, timeoutConfig)` で直接生成
   - パターンB: IServiceProvider経由でDIコンテナから取得
   - **参考実装**: ExecutionOrchestrator.ExecuteSinglePlcAsync()（L167-170）ではパターンAを採用

2. **設定情報の変換**
   - PlcConfiguration → ConnectionConfig の変換が必要
   - PlcConfiguration → TimeoutConfig の変換が必要
   - **参考実装**: ExecutionOrchestrator.ExecuteSinglePlcAsync()（L152-164）
     ```csharp
     var connectionConfig = new ConnectionConfig
     {
         IpAddress = plcConfig.IPAddress,
         Port = plcConfig.Port,
         UseTcp = plcConfig.ConnectionMethod == "TCP"
     };

     var timeoutConfig = new TimeoutConfig
     {
         ConnectTimeoutMs = plcConfig.Timeout,
         SendTimeoutMs = plcConfig.Timeout,
         ReceiveTimeoutMs = plcConfig.Timeout
     };
     ```

3. **PlcConfiguration vs PlcConnectionConfig**
   - PlcConfiguration: MultiPlcConfigManagerが返す型（Excel読み込み用）
   - PlcConnectionConfig: ExecutionOrchestratorが使用している型
   - 両者の互換性・変換方法を明確化する必要あり

4. **PlcCommunicationManagerへの情報保持**
   - 現在のPlcCommunicationManagerは ConnectionConfig と TimeoutConfig のみを保持
   - フレーム構築に必要な情報（デバイスリスト、FrameVersion等）は保持していない
   - ExecuteMultiPlcCycleAsync_Internal で PlcConfiguration を参照する手段が必要
   - **選択肢**:
     - A: PlcConfiguration自体をPlcCommunicationManagerまたは別のクラスで保持
     - B: PlcConfigurationとPlcCommunicationManagerを対応付けるDictionary等を使用
     - C: カスタムラッパークラス（PlcManager）を作成して両方を保持

---

## 問題点の詳細分析

### データフローの断絶箇所

```
【期待される動作フロー】
ApplicationController.StartAsync()
  ↓
ExecuteStep1InitializationAsync()
  - MultiPlcConfigManager.GetAllConfigurations() → List<PlcConfiguration>
  - ★各設定から PlcCommunicationManager を生成（未実装）
  - _plcManagers に追加
  ↓ InitializationResult (_plcManagers が設定済み)
StartContinuousDataCycleAsync(_plcManagers)
  ↓ MonitoringIntervalMs 間隔で実行
ExecuteMultiPlcCycleAsync_Internal(_plcManagers)
  - ★各 PlcCommunicationManager に対してサイクル実行（未実装）
  ↓
Step2-7 処理


【現在の実際の動作】
ApplicationController.StartAsync()
  ↓
ExecuteStep1InitializationAsync()
  - MultiPlcConfigManager.GetAllConfigurations() → List<PlcConfiguration>
  - _plcManagers = new List<>() ← ★空のまま
  ↓ InitializationResult (_plcManagers = 空)
StartContinuousDataCycleAsync(_plcManagers)
  ↓ MonitoringIntervalMs 間隔で実行
ExecuteMultiPlcCycleAsync_Internal(_plcManagers)
  - await Task.CompletedTask ← ★何もしない
  ↓
処理終了（何も起こらない）
```

---

## TDDサイクルによる修正計画

### TDD基本原則

1. **Red**: テストを先に書き、失敗することを確認
2. **Green**: 最小限の実装でテストを通す
3. **Refactor**: コードを改善・整理

---

### Phase 1: ExecuteMultiPlcCycleAsync_Internal の実装

#### 実装方針の選択肢

ExecutionOrchestratorには既に実装済みのパス1（MultiPlcConfig版）が存在します。パス2（継続実行モード用）の実装には2つの選択肢があります：

**選択肢A: パス1の実装パターンを踏襲**
- PlcCommunicationManagerを受け取らず、設定から毎回生成
- ExecuteStep3to5CycleAsync() を使用（Step6処理なし）
- ConfigToFrameManager または SlmpFrameBuilder を直接使用

**選択肢B: 文書の当初計画通りに実装**
- 事前初期化されたPlcCommunicationManagerを使用
- ExecuteFullCycleAsync() を使用（Step3-6完全サイクル）
- ConfigToFrameManager を使用してフレーム構築

**推奨**: 選択肢Bを採用（理由：Step6データ処理を含む完全サイクルが必要、リソースの再利用が効率的）

---

#### TDDサイクル1: 基本的な1つのPLCに対するサイクル実行（選択肢B）

##### Red: テスト作成

**テストファイル**: `Tests/Unit/Core/Controllers/ExecutionOrchestratorTests.cs`

**注意**: ExecuteMultiPlcCycleAsync_Internal は private メソッドのため、テスト用に以下のいずれかの対応が必要：
1. テスト用のpublicラッパーメソッド `ExecuteSingleCycleAsync()` を追加
2. InternalsVisibleTo属性を使用してinternalに変更
3. リフレクションを使用（非推奨）

**推奨**: オプション1（テスト用publicメソッド追加）

```csharp
[Fact]
public async Task ExecuteMultiPlcCycleAsync_Internal_SinglePlc_ExecutesFullCycle()
{
    // Arrange
    var mockPlcManager = new Mock<IPlcCommunicationManager>();
    var mockConfigToFrameManager = new Mock<IConfigToFrameManager>();
    var mockDataOutputManager = new Mock<IDataOutputManager>();
    var mockTimerService = new Mock<ITimerService>();
    var config = Options.Create(new DataProcessingConfig { MonitoringIntervalMs = 1000 });

    // ExecutionOrchestratorのコンストラクタを拡張する必要がある
    var orchestrator = new ExecutionOrchestrator(
        mockTimerService.Object,
        config,
        mockConfigToFrameManager.Object,
        mockDataOutputManager.Object);

    var plcManagers = new List<IPlcCommunicationManager> { mockPlcManager.Object };

    var expectedResult = new FullCycleExecutionResult { IsSuccess = true };
    mockPlcManager
        .Setup(m => m.ExecuteFullCycleAsync(
            It.IsAny<ConnectionConfig>(),
            It.IsAny<TimeoutConfig>(),
            It.IsAny<byte[]>(),
            It.IsAny<ProcessedDeviceRequestInfo>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(expectedResult);

    // Act
    // ExecuteMultiPlcCycleAsync_Internal を public メソッド経由で呼び出し
    await orchestrator.ExecuteSingleCycleAsync(plcManagers, CancellationToken.None);

    // Assert
    mockPlcManager.Verify(
        m => m.ExecuteFullCycleAsync(
            It.IsAny<ConnectionConfig>(),
            It.IsAny<TimeoutConfig>(),
            It.IsAny<byte[]>(),
            It.IsAny<ProcessedDeviceRequestInfo>(),
            It.IsAny<CancellationToken>()),
        Times.Once);
}
```

**課題**: 上記テストを動作させるには以下の変更が必要：
1. ExecutionOrchestratorのコンストラクタに IConfigToFrameManager と IDataOutputManager を追加
2. テスト用publicメソッド ExecuteSingleCycleAsync() を追加
3. PlcCommunicationManagerから設定情報（ConnectionConfig等）を取得する手段が必要

##### Green: 最小限の実装

**実装箇所**: `andon/Core/Controllers/ExecutionOrchestrator.cs`

```csharp
// テスト用に public メソッドを追加
public async Task ExecuteSingleCycleAsync(
    List<IPlcCommunicationManager> plcManagers,
    CancellationToken cancellationToken)
{
    await ExecuteMultiPlcCycleAsync_Internal(plcManagers, cancellationToken);
}

private async Task ExecuteMultiPlcCycleAsync_Internal(
    List<IPlcCommunicationManager> plcManagers,
    CancellationToken cancellationToken)
{
    // 最小限の実装: 1つ目のPLCのみ処理
    if (plcManagers == null || plcManagers.Count == 0)
        return;

    var manager = plcManagers[0];

    // Step2: フレーム構築（仮実装）
    var frame = new byte[] { 0x00 }; // TODO: 実際のフレーム構築

    // Step3-6: 完全サイクル実行
    var result = await manager.ExecuteFullCycleAsync(
        new ConnectionConfig(),  // TODO: 実際の設定
        new TimeoutConfig(),     // TODO: 実際の設定
        frame,
        new ProcessedDeviceRequestInfo(), // TODO: 実際の設定
        cancellationToken);

    // Step7: データ出力（TODO）
}
```

##### Refactor: コード改善

- ハードコードされた設定値を適切な場所から取得
- エラーハンドリング追加
- ログ出力追加

---

#### TDDサイクル2: 複数PLCへの対応

##### Red: テスト作成

```csharp
[Fact]
public async Task ExecuteMultiPlcCycleAsync_Internal_MultiplePlcs_ExecutesAllCycles()
{
    // Arrange
    var mockPlcManager1 = new Mock<IPlcCommunicationManager>();
    var mockPlcManager2 = new Mock<IPlcCommunicationManager>();
    var mockTimerService = new Mock<ITimerService>();
    var config = Options.Create(new DataProcessingConfig { MonitoringIntervalMs = 1000 });

    var orchestrator = new ExecutionOrchestrator(mockTimerService.Object, config);
    var plcManagers = new List<IPlcCommunicationManager>
    {
        mockPlcManager1.Object,
        mockPlcManager2.Object
    };

    // Act
    await orchestrator.ExecuteSingleCycleAsync(plcManagers, CancellationToken.None);

    // Assert
    mockPlcManager1.Verify(m => m.ExecuteFullCycleAsync(...), Times.Once);
    mockPlcManager2.Verify(m => m.ExecuteFullCycleAsync(...), Times.Once);
}
```

##### Green: foreach ループでの実装

```csharp
private async Task ExecuteMultiPlcCycleAsync_Internal(
    List<IPlcCommunicationManager> plcManagers,
    CancellationToken cancellationToken)
{
    if (plcManagers == null || plcManagers.Count == 0)
        return;

    foreach (var manager in plcManagers)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Step2-7処理
        var frame = BuildFrame(manager); // TODO
        var result = await manager.ExecuteFullCycleAsync(...);
        await OutputData(result); // TODO
    }
}
```

##### Refactor: 並列実行対応（必要に応じて）

---

#### TDDサイクル3: Step2フレーム構築の統合

##### Red: テスト作成

```csharp
[Fact]
public async Task ExecuteMultiPlcCycleAsync_Internal_BuildsCorrectFrame()
{
    // Arrange
    var mockConfigToFrameManager = new Mock<IConfigToFrameManager>();
    var mockPlcManager = new Mock<IPlcCommunicationManager>();

    byte[] expectedFrame = new byte[] { 0x54, 0x00, ... };
    mockConfigToFrameManager
        .Setup(m => m.BuildReadRandomFrameFromConfig(It.IsAny<PlcConfiguration>()))
        .Returns(expectedFrame);

    // Act & Assert
    // フレーム構築が正しく呼ばれることを検証
}
```

##### Green: ConfigToFrameManager の統合

```csharp
private readonly IConfigToFrameManager _configToFrameManager;

public ExecutionOrchestrator(
    ITimerService timerService,
    IOptions<DataProcessingConfig> dataProcessingConfig,
    IConfigToFrameManager configToFrameManager)
{
    _timerService = timerService;
    _dataProcessingConfig = dataProcessingConfig;
    _configToFrameManager = configToFrameManager;
}

private async Task ExecuteMultiPlcCycleAsync_Internal(...)
{
    foreach (var manager in plcManagers)
    {
        // Step2: フレーム構築
        var config = GetPlcConfiguration(manager); // TODO: 実装が必要
        var frame = _configToFrameManager.BuildReadRandomFrameFromConfig(config);

        // Step3-6: 実行
        var result = await manager.ExecuteFullCycleAsync(...);
    }
}
```

**課題**: `GetPlcConfiguration(manager)` の実装方法
- PlcCommunicationManagerからPlcConfigurationを取得する手段が必要
- **選択肢**:
  1. PlcCommunicationManagerとPlcConfigurationを紐付けるDictionaryを管理
  2. カスタムラッパークラスを作成
  3. PlcCommunicationManagerにPlcConfiguration参照を保持させる（設計変更が必要）

---

#### TDDサイクル4: Step7データ出力の統合

##### Red: テスト作成

```csharp
[Fact]
public async Task ExecuteMultiPlcCycleAsync_Internal_OutputsDataAfterCycle()
{
    // Arrange
    var mockDataOutputManager = new Mock<IDataOutputManager>();

    // Act
    await orchestrator.ExecuteSingleCycleAsync(plcManagers, CancellationToken.None);

    // Assert
    mockDataOutputManager.Verify(
        m => m.OutputToJson(
            It.IsAny<ProcessedResponseData>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<Dictionary<string, DeviceEntryInfo>>()),
        Times.Once);
}
```

##### Green: DataOutputManager の統合

```csharp
private readonly IDataOutputManager _dataOutputManager;

private async Task ExecuteMultiPlcCycleAsync_Internal(...)
{
    foreach (var manager in plcManagers)
    {
        // Step2-6: 実行
        var result = await manager.ExecuteFullCycleAsync(...);

        // Step7: データ出力
        if (result.IsSuccess && result.ProcessedData != null)
        {
            _dataOutputManager.OutputToJson(
                result.ProcessedData,
                outputDirectory,
                ipAddress,
                port,
                deviceConfig);
        }
    }
}
```

---

### Phase 2: ApplicationController の PlcManager 初期化実装

#### TDDサイクル1: 単一PLC設定からのManager生成

##### Red: テスト作成

**テストファイル**: `Tests/Unit/Core/Controllers/ApplicationControllerTests.cs`

```csharp
[Fact]
public async Task ExecuteStep1InitializationAsync_SingleConfig_CreatesPlcManager()
{
    // Arrange
    var mockConfigManager = new Mock<MultiPlcConfigManager>();
    var mockOrchestrator = new Mock<IExecutionOrchestrator>();
    var mockLoggingManager = new Mock<ILoggingManager>();

    var config = new PlcConfiguration
    {
        IpAddress = "192.168.1.1",
        Port = 5000,
        Devices = new List<DeviceSpecification> { /* ... */ }
    };

    mockConfigManager
        .Setup(m => m.GetAllConfigurations())
        .Returns(new List<PlcConfiguration> { config });

    var controller = new ApplicationController(
        mockConfigManager.Object,
        mockOrchestrator.Object,
        mockLoggingManager.Object);

    // Act
    var result = await controller.ExecuteStep1InitializationAsync();

    // Assert
    Assert.True(result.Success);
    Assert.Equal(1, result.PlcCount);

    // PlcManagersプロパティを追加して検証可能にする
    var plcManagers = controller.GetPlcManagers();
    Assert.Single(plcManagers);
}
```

##### Green: 最小限の実装

```csharp
public class ApplicationController : IApplicationController
{
    private readonly MultiPlcConfigManager _configManager;
    private readonly IExecutionOrchestrator _orchestrator;
    private readonly ILoggingManager _loggingManager;
    private readonly IConfigurationWatcher? _configurationWatcher;
    private List<IPlcCommunicationManager>? _plcManagers;

    // テスト用にアクセサを追加
    public List<IPlcCommunicationManager> GetPlcManagers() => _plcManagers ?? new List<IPlcCommunicationManager>();

    public async Task<InitializationResult> ExecuteStep1InitializationAsync(
        string configDirectory = "./config/",
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _loggingManager.LogInfo("Starting Step1 initialization");

            var configs = _configManager.GetAllConfigurations();
            _plcManagers = new List<IPlcCommunicationManager>();

            // PlcCommunicationManager を設定ごとに初期化
            foreach (var config in configs)
            {
                var connectionConfig = new ConnectionConfig
                {
                    IpAddress = config.IpAddress,
                    Port = config.Port,
                    UseTcp = config.ConnectionMethod == "TCP"
                };

                var timeoutConfig = new TimeoutConfig
                {
                    ConnectTimeoutMs = config.Timeout,
                    SendTimeoutMs = config.Timeout,
                    ReceiveTimeoutMs = config.Timeout
                };

                var manager = new PlcCommunicationManager(
                    connectionConfig,
                    timeoutConfig);

                _plcManagers.Add(manager);
            }

            await _loggingManager.LogInfo("Step1 initialization completed");

            return new InitializationResult
            {
                Success = true,
                PlcCount = configs.Count
            };
        }
        catch (Exception ex)
        {
            await _loggingManager.LogError(ex, "Step1 initialization failed");
            return new InitializationResult { Success = false, ErrorMessage = ex.Message };
        }
    }
}
```

**重大な課題**: 上記実装では PlcConfiguration の情報が失われる
- PlcCommunicationManagerには ConnectionConfig と TimeoutConfig しか保持されない
- フレーム構築に必要な情報（Devices, FrameVersion, 出力先パス等）が参照できない
- **解決策が必要**:
  - オプション1: `Dictionary<IPlcCommunicationManager, PlcConfiguration>` を追加管理
  - オプション2: カスタムラッパークラス作成
    ```csharp
    public class PlcManagerContext
    {
        public IPlcCommunicationManager Manager { get; set; }
        public PlcConfiguration Configuration { get; set; }
    }
    private List<PlcManagerContext>? _plcManagerContexts;
    ```
  - オプション3: ExecutionOrchestratorに PlcConfiguration リストも渡す

**推奨**: オプション2（ラッパークラス）またはオプション3（設定リストも渡す）

##### Refactor: DI統合とエラーハンドリング強化

- PlcCommunicationManager の生成をFactoryパターンで実装（オプション）
- 各PLC設定の検証
- 初期化失敗時の詳細なエラー情報
- PlcConfiguration参照の保持方法を決定・実装

---

#### TDDサイクル2: 複数PLC設定への対応

##### Red: テスト作成

```csharp
[Fact]
public async Task ExecuteStep1InitializationAsync_MultipleConfigs_CreatesMultipleManagers()
{
    // Arrange
    var configs = new List<PlcConfiguration>
    {
        new PlcConfiguration { IpAddress = "192.168.1.1", Port = 5000 },
        new PlcConfiguration { IpAddress = "192.168.1.2", Port = 5001 },
        new PlcConfiguration { IpAddress = "192.168.1.3", Port = 5002 }
    };

    mockConfigManager
        .Setup(m => m.GetAllConfigurations())
        .Returns(configs);

    // Act
    var result = await controller.ExecuteStep1InitializationAsync();

    // Assert
    Assert.Equal(3, result.PlcCount);
    Assert.Equal(3, controller.GetPlcManagers().Count);
}
```

##### Green: foreachループで対応（既に実装済み）

---

#### TDDサイクル3: 初期化失敗時のエラーハンドリング

##### Red: テスト作成

```csharp
[Fact]
public async Task ExecuteStep1InitializationAsync_InvalidConfig_ReturnsFailure()
{
    // Arrange
    mockConfigManager
        .Setup(m => m.GetAllConfigurations())
        .Throws(new InvalidOperationException("Invalid configuration"));

    // Act
    var result = await controller.ExecuteStep1InitializationAsync();

    // Assert
    Assert.False(result.Success);
    Assert.Contains("Invalid configuration", result.ErrorMessage);
}
```

##### Green: try-catch 実装（既存コードで対応済み）

---

### Phase 3: 統合テスト

#### 統合テスト1: Step1 → 周期実行の完全フロー

```csharp
[Fact]
public async Task ContinuousMode_Step1ToStep7_ExecutesSuccessfully()
{
    // Arrange
    // モックの準備

    // Act
    await controller.StartAsync(cts.Token);
    await Task.Delay(3000); // 3秒間実行
    cts.Cancel();

    // Assert
    // 周期実行が正常に動作したことを検証
}
```

---

## データフロー検証結果

### ✅ 単独実行時の正常なデータフロー

**ExecuteFullCycleAsync() による完全サイクル**:

```
PlcConfiguration
  ↓
ConfigToFrameManager.BuildReadRandomFrameFromConfig()
  ↓ byte[] frame
PlcCommunicationManager.ExecuteFullCycleAsync()
  ├─ Step3: ConnectAsync() → ConnectionResponse
  ├─ Step4-送信: SendFrameAsync(frame) → void
  ├─ Step4-受信: ReceiveResponseAsync() → RawResponseData
  ├─ Step6-1: ProcessReceivedRawData() → BasicProcessedResponseData
  ├─ Step6-2: データ変換 → ProcessedResponseData
  ├─ Step6-3: ParseRawToStructuredData() → StructuredData
  └─ Step5: DisconnectAsync() → DisconnectResult
    ↓
FullCycleExecutionResult
  ├─ ConnectResult: ConnectionResponse
  ├─ SendResult: SendResponse
  ├─ ReceiveResult: RawResponseData
  ├─ BasicProcessedData: BasicProcessedResponseData
  ├─ ProcessedData: ProcessedResponseData
  ├─ StructuredData: StructuredData
  └─ DisconnectResult: DisconnectResult
```

**検証結果**: ✅ 正常動作

---

### ❌ 継続実行モードでの断絶したデータフロー

**現在の状態**:

```
ApplicationController.StartAsync()
  ↓
ExecuteStep1InitializationAsync()
  ├─ MultiPlcConfigManager.GetAllConfigurations() → List<PlcConfiguration>
  └─ _plcManagers = new List<>() ← ★空リスト
    ↓ InitializationResult (Success=true, PlcCount=N, but _plcManagers=empty)
StartContinuousDataCycleAsync(_plcManagers)
  ↓ MonitoringIntervalMs 間隔で実行
TimerService.StartPeriodicExecution(() => ExecuteMultiPlcCycleAsync_Internal(...))
  ↓
ExecuteMultiPlcCycleAsync_Internal(_plcManagers)
  └─ await Task.CompletedTask ← ★何もしない
    ↓
（終了）
```

**問題**: Step2-7 が一切実行されない

---

### ✅ 修正後の期待される動作フロー

```
ApplicationController.StartAsync()
  ↓
ExecuteStep1InitializationAsync()
  ├─ MultiPlcConfigManager.GetAllConfigurations() → List<PlcConfiguration>
  ├─ foreach (config in configs)
  │   ├─ ConnectionConfig 生成
  │   ├─ TimeoutConfig 生成
  │   ├─ PlcCommunicationManager 生成
  │   └─ _plcManagers.Add(manager)
  └─ _plcManagers (populated)
    ↓ InitializationResult (Success=true, PlcCount=N, _plcManagers filled)
StartContinuousDataCycleAsync(_plcManagers)
  ↓ MonitoringIntervalMs 間隔で実行
TimerService.StartPeriodicExecution(() => ExecuteMultiPlcCycleAsync_Internal(...))
  ↓
ExecuteMultiPlcCycleAsync_Internal(_plcManagers)
  └─ foreach (manager in _plcManagers)
      ├─ Step2: ConfigToFrameManager.BuildFrame() → byte[] frame
      ├─ Step3-6: manager.ExecuteFullCycleAsync(frame) → FullCycleExecutionResult
      └─ Step7: DataOutputManager.OutputToJson(result.ProcessedData)
    ↓
（周期実行継続）
```

---

## 実装チェックリスト

### Phase 1: ExecuteMultiPlcCycleAsync_Internal 実装

- [ ] **TDDサイクル1**: 単一PLC基本サイクル
  - [ ] Red: テスト作成 (ExecutionOrchestratorTests.cs)
  - [ ] Green: 最小限実装
  - [ ] Refactor: エラーハンドリング追加
  - [ ] テスト実行・パス確認

- [ ] **TDDサイクル2**: 複数PLC対応
  - [ ] Red: テスト作成
  - [ ] Green: foreach ループ実装
  - [ ] Refactor: 並列実行考慮
  - [ ] テスト実行・パス確認

- [ ] **TDDサイクル3**: Step2フレーム構築統合
  - [ ] Red: テスト作成
  - [ ] Green: ConfigToFrameManager 統合
  - [ ] Refactor: コード整理
  - [ ] テスト実行・パス確認

- [ ] **TDDサイクル4**: Step7データ出力統合
  - [ ] Red: テスト作成
  - [ ] Green: DataOutputManager 統合
  - [ ] Refactor: 出力パス設定
  - [ ] テスト実行・パス確認

### Phase 2: ApplicationController 初期化実装

- [ ] **TDDサイクル1**: 単一PLC Manager生成
  - [ ] Red: テスト作成 (ApplicationControllerTests.cs)
  - [ ] Green: 最小限実装
  - [ ] Refactor: DI統合
  - [ ] テスト実行・パス確認

- [ ] **TDDサイクル2**: 複数PLC Manager生成
  - [ ] Red: テスト作成
  - [ ] Green: foreach実装
  - [ ] Refactor: エラーハンドリング
  - [ ] テスト実行・パス確認

- [ ] **TDDサイクル3**: 初期化失敗ハンドリング
  - [ ] Red: テスト作成
  - [ ] Green: try-catch実装
  - [ ] Refactor: ログ出力追加
  - [ ] テスト実行・パス確認

### Phase 3: 統合テスト

- [ ] **統合テスト1**: Step1 → 周期実行フロー
  - [ ] テスト作成
  - [ ] テスト実行・パス確認

- [ ] **統合テスト2**: エラーリカバリー
  - [ ] 接続失敗時の継続動作
  - [ ] データ処理失敗時の継続動作
  - [ ] テスト実行・パス確認

- [ ] **統合テスト3**: 複数PLC並列実行
  - [ ] テスト作成
  - [ ] テスト実行・パス確認

### Phase 4: コードレビューとドキュメント更新

- [ ] コードレビュー実施
- [ ] ドキュメント更新
  - [ ] アプリケーション動作フロー.md
  - [ ] クラス設計.md
  - [ ] 各ステップio.md
- [ ] リファクタリング

---

## 実装時の注意事項

### TDD実践のポイント

1. **必ずテストを先に書く**
   - 実装前にテストを書くことで、インターフェースと期待動作を明確化
   - テストが失敗（Red）することを確認

2. **最小限の実装でテストを通す**
   - ハードコードでも良いので、まずテストを通す（Green）
   - 過度な設計を避ける

3. **動作するコードができてからリファクタリング**
   - テストが通ってから、コードを改善（Refactor）
   - テストが常にパスすることを確認しながら進める

4. **1つのテストで1つの機能**
   - テストケースを細かく分割
   - 失敗時の原因特定を容易にする

### コード品質の維持

- **各Phaseでの全テスト実行**
  - 新しいコード追加後、既存テストが壊れていないか確認
  - リグレッションテストの徹底

- **継続的なリファクタリング**
  - 重複コードの排除
  - 命名の改善
  - 複雑度の低減

- **ログ出力の充実**
  - 各処理ステップでログ出力
  - エラー時の詳細情報記録

### テスト用publicメソッドの取り扱い

#### 概要

TDD実装のため、以下のテスト専用publicメソッドを追加しています。これらは設計文書に記載されていませんが、TDD実践上の標準的な手法です。

#### 追加されるテスト用メソッド

**ApplicationController.cs**:
```csharp
/// <summary>
/// テスト用: PlcManagers リストへのアクセサ
/// </summary>
/// <remarks>
/// 本番コード: internal または条件付きコンパイルに変更予定
/// テスト目的: ExecuteStep1InitializationAsync() の検証
/// </remarks>
public List<IPlcCommunicationManager> GetPlcManagers()
    => _plcManagers ?? new List<IPlcCommunicationManager>();
```

**ExecutionOrchestrator.cs**:
```csharp
/// <summary>
/// テスト用: ExecuteMultiPlcCycleAsync_Internal() の公開ラッパー
/// </summary>
/// <remarks>
/// 本番コード: internal または削除予定
/// テスト目的: 周期実行ロジックの単体テスト
/// </remarks>
public async Task ExecuteSingleCycleAsync(
    List<IPlcCommunicationManager> plcManagers,
    CancellationToken cancellationToken)
{
    await ExecuteMultiPlcCycleAsync_Internal(plcManagers, cancellationToken);
}
```

#### 実装後の対応方針

**オプション1: internal アクセス修飾子への変更**
```csharp
// テストプロジェクトから参照可能、外部からは非公開
[assembly: InternalsVisibleTo("andon.Tests")]

internal List<IPlcCommunicationManager> GetPlcManagers()
    => _plcManagers ?? new List<IPlcCommunicationManager>();
```

**オプション2: 条件付きコンパイル**
```csharp
#if DEBUG
/// <summary>
/// テスト専用メソッド（DEBUGビルド時のみ有効）
/// </summary>
public List<IPlcCommunicationManager> GetPlcManagers()
    => _plcManagers ?? new List<IPlcCommunicationManager>();
#endif
```

**オプション3: そのまま維持**
- publicのまま維持（インターフェースに含めない）
- ドキュメントで「テスト専用」と明記
- コードレビュー時に使用箇所を確認

#### 推奨される対応

**Phase 3 統合テスト完了後**:
1. すべてのテストがパスすることを確認
2. テスト用メソッドを `internal` に変更
3. `AssemblyInfo.cs` に `InternalsVisibleTo` 属性を追加
4. テストが引き続きパスすることを確認

```csharp
// AssemblyInfo.cs または ApplicationController.cs の冒頭
[assembly: InternalsVisibleTo("andon.Tests")]
```

#### 設計文書への記載について

これらのメソッドは以下の理由により、設計文書への記載は不要と判断：
- **一時的な存在**: TDD実装のための一時的な措置
- **テスト専用**: 本番コードからは使用されない
- **標準的手法**: TDDでは一般的に使用される手法
- **将来的に変更**: internal 化または削除予定

---

## まとめ

### 現状の課題

#### 🔴 継続実行モード（パス2）が未実装

1. **ExecutionOrchestrator.ExecuteMultiPlcCycleAsync_Internal() が空実装**
   - 継続実行モード用の周期実行ロジック（パス2）が未実装
   - パス1（MultiPlcConfig版）は実装済みだが、継続実行モードでは使用されていない

2. **ApplicationController.ExecuteStep1InitializationAsync() で PlcManager が未初期化**
   - PlcCommunicationManagerの生成処理が未実装
   - _plcManagers リストが空のまま

#### ⚠️ 設計上の課題

3. **PlcConfiguration情報の保持方法が未決定**
   - PlcCommunicationManagerにはConnectionConfig/TimeoutConfigしか保持されない
   - フレーム構築に必要な情報（Devices, FrameVersion等）の参照手段が必要

4. **インターフェース定義の更新完了（2025-11-28）**
   - ✅ IPlcCommunicationManagerに以下を追加済み:
     - ExecuteStep3to5CycleAsync()
     - ExecuteFullCycleAsync()

### 実装済みの機能

- ✅ ExecutionOrchestrator パス1（MultiPlcConfig版）
- ✅ PlcCommunicationManager 完全実装
- ✅ ConfigToFrameManager（TargetDeviceConfig版、PlcConfiguration版）
- ✅ MultiPlcConfigManager
- ✅ DataOutputManager

### 解決方針

TDDサイクルに従い、テストファースト開発で以下を実装:

#### Phase 0: 設計決定（実装前）

- **PlcConfiguration参照の保持方法を決定**
  - 推奨オプション: カスタムラッパークラス（PlcManagerContext）またはExecutionOrchestratorに設定リストも渡す
  - ExecutionOrchestrator のコンストラクタ拡張要否を決定

#### Phase 1: ExecuteMultiPlcCycleAsync_Internal の実装

- 実装方針の選択（選択肢AまたはB）
- ExecutionOrchestratorへのDI追加（IConfigToFrameManager, IDataOutputManager）
- テスト用publicメソッド追加
- Step2-7完全サイクルの実装

#### Phase 2: ApplicationController の PlcManager 初期化実装

- PlcCommunicationManager生成処理の実装
- PlcConfiguration情報の保持実装
- エラーハンドリング強化

#### Phase 3: 統合テスト

- Step1 → 周期実行の完全フロー検証
- エラーリカバリー検証
- 複数PLC並列実行検証

### 期待される効果

- 継続実行モードが正常動作
- MonitoringIntervalMs 間隔でのデータ収集サイクル実行
- 複数PLCへの対応
- Step3-6完全サイクル（データ処理含む）の実行
- エラー時の適切なハンドリングと継続実行

---

**次のアクション**:
1. Phase 0: PlcConfiguration参照の保持方法を決定
2. Phase 1: TDDサイクル1 の Red（テスト作成）から開始
