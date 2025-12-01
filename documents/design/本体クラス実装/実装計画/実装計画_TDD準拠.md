# 本体クラス実装計画（TDD準拠版）

## 概要
andonプロジェクトの継続実行モード実現に向けた、TDD（Test-Driven Development）手法に基づく段階的な実装計画。

**TDD基本サイクル**: Red-Green-Refactor
1. **Red**: 失敗するテストを先に書く
2. **Green**: テストを通すための最小限のコードを実装
3. **Refactor**: 動作を保ったままコードを改善

---

## 前提条件

### ✅ 実装済み機能（エンジン部分）
以下の機能は完全実装済みで、動作確認済み：

- **PlcCommunicationManager** - PLC通信処理（Step3-6）
- **ConfigToFrameManager** - フレーム構築ロジック（Step2）
- **DataOutputManager** - データ出力ロジック（Step7）
- **MultiPlcConfigManager** - 複数設定管理
- **MultiPlcCoordinator** - 並列実行調整ヘルパー
- **ExecutionOrchestrator** - 複数PLC並列実行機能（ExecuteMultiPlcCycleAsync, ExecuteSinglePlcAsync）

### ✅ 追加実装完了（Phase 1で実装済み）
- **ExecutionOrchestrator** - 以下2メソッド実装完了:
  - `RunContinuousDataCycleAsync()` - 継続データサイクル実行（Phase 1実装済み）
  - `GetMonitoringInterval()` - 監視間隔取得（Phase 1実装済み）

---

## Phase 1: 最小動作環境構築（最優先）

### 目標
継続実行モードの基本動作を実現する。アプリケーションが起動し、MonitoringIntervalMs間隔でStep2-7を周期的に実行できる状態にする。

### TDD実装順序
依存関係を考慮し、下から上に向かって実装する（ボトムアップアプローチ）。

---

### Step 1-1: TimerService（基盤サービス）

#### 実装ファイル
- **テスト**: `Tests/Unit/Services/TimerServiceTests.cs`
- **実装**: `andon/Services/TimerService.cs`
- **インターフェース**: `andon/Core/Interfaces/ITimerService.cs`

#### TDDサイクル 1: 基本的な周期実行

##### Phase A: Red（失敗するテストを書く）
```csharp
[Fact]
public async Task StartPeriodicExecution_実行間隔に従って処理を繰り返し実行する()
{
    // Arrange
    var mockLogger = new MockLoggingManager();
    var timerService = new TimerService(mockLogger);
    int executionCount = 0;
    var interval = TimeSpan.FromMilliseconds(100);
    var cts = new CancellationTokenSource();

    // Act
    var task = Task.Run(async () =>
    {
        await timerService.StartPeriodicExecution(
            async () => { executionCount++; await Task.CompletedTask; },
            interval,
            cts.Token);
    });

    await Task.Delay(350); // 3回実行される時間待機
    cts.Cancel();
    await task;

    // Assert
    Assert.InRange(executionCount, 3, 4); // タイミングのずれを考慮
}
```

##### Phase B: Green（最小限の実装）
```csharp
public class TimerService : ITimerService
{
    private readonly ILoggingManager _loggingManager;

    public TimerService(ILoggingManager loggingManager)
    {
        _loggingManager = loggingManager;
    }

    public async Task StartPeriodicExecution(
        Func<Task> action,
        TimeSpan interval,
        CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(interval);

        while (!cancellationToken.IsCancellationRequested)
        {
            await timer.WaitForNextTickAsync(cancellationToken);
            await action();
        }
    }
}
```

##### Phase C: Refactor（必要に応じて改善）
- 現時点では不要

#### TDDサイクル 2: 重複実行防止

##### Phase A: Red
```csharp
[Fact]
public async Task StartPeriodicExecution_前回処理未完了時は重複実行しない()
{
    // Arrange
    var mockLogger = new MockLoggingManager();
    var timerService = new TimerService(mockLogger);
    int executionCount = 0;
    int concurrentExecutions = 0;
    int maxConcurrent = 0;
    var interval = TimeSpan.FromMilliseconds(50);
    var cts = new CancellationTokenSource();

    // Act
    var task = Task.Run(async () =>
    {
        await timerService.StartPeriodicExecution(
            async () =>
            {
                Interlocked.Increment(ref concurrentExecutions);
                maxConcurrent = Math.Max(maxConcurrent, concurrentExecutions);
                executionCount++;
                await Task.Delay(200); // 長時間処理をシミュレート
                Interlocked.Decrement(ref concurrentExecutions);
            },
            interval,
            cts.Token);
    });

    await Task.Delay(400);
    cts.Cancel();
    await task;

    // Assert
    Assert.Equal(1, maxConcurrent); // 同時実行は1つのみ
    mockLogger.Verify(m => m.LogWarning(It.IsAny<string>()), Times.AtLeastOnce());
}
```

##### Phase B: Green
```csharp
public async Task StartPeriodicExecution(
    Func<Task> action,
    TimeSpan interval,
    CancellationToken cancellationToken)
{
    using var timer = new PeriodicTimer(interval);
    bool isExecuting = false;

    while (!cancellationToken.IsCancellationRequested)
    {
        try
        {
            await timer.WaitForNextTickAsync(cancellationToken);

            // 前回処理未完了時の重複実行防止
            if (isExecuting)
            {
                await _loggingManager.LogWarning("Previous cycle still running, skipping this interval");
                continue;
            }

            isExecuting = true;
            await action();
        }
        catch (OperationCanceledException)
        {
            break;
        }
        finally
        {
            isExecuting = false;
        }
    }
}
```

##### Phase C: Refactor
- 例外処理を追加して堅牢性を向上

#### TDDサイクル 3: 例外処理

##### Phase A: Red
```csharp
[Fact]
public async Task StartPeriodicExecution_処理中の例外をログに記録して継続する()
{
    // Arrange
    var mockLogger = new MockLoggingManager();
    var timerService = new TimerService(mockLogger);
    int executionCount = 0;
    var interval = TimeSpan.FromMilliseconds(50);
    var cts = new CancellationTokenSource();

    // Act
    var task = Task.Run(async () =>
    {
        await timerService.StartPeriodicExecution(
            async () =>
            {
                executionCount++;
                if (executionCount == 2)
                    throw new InvalidOperationException("Test exception");
                await Task.CompletedTask;
            },
            interval,
            cts.Token);
    });

    await Task.Delay(200);
    cts.Cancel();
    await task;

    // Assert
    Assert.True(executionCount >= 3); // 例外後も実行継続
    mockLogger.Verify(m => m.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once());
}
```

##### Phase B: Green
```csharp
public async Task StartPeriodicExecution(
    Func<Task> action,
    TimeSpan interval,
    CancellationToken cancellationToken)
{
    using var timer = new PeriodicTimer(interval);
    bool isExecuting = false;

    while (!cancellationToken.IsCancellationRequested)
    {
        try
        {
            await timer.WaitForNextTickAsync(cancellationToken);

            if (isExecuting)
            {
                await _loggingManager.LogWarning("Previous cycle still running, skipping this interval");
                continue;
            }

            isExecuting = true;
            await action();
        }
        catch (OperationCanceledException)
        {
            break;
        }
        catch (Exception ex)
        {
            await _loggingManager.LogError(ex, "Error in periodic execution");
        }
        finally
        {
            isExecuting = false;
        }
    }
}
```

#### 完了条件
- [ ] 全テストケースがパス
- [ ] コードカバレッジ90%以上
- [ ] 周期実行が正確に動作
- [ ] 重複実行が防止される
- [ ] 例外発生時も実行が継続

---

### Step 1-2: ExecutionOrchestrator（追加メソッド）

#### 実装ファイル
- **テスト**: `Tests/Unit/Core/Controllers/ExecutionOrchestratorTests.cs`（既存に追加）
- **実装**: `andon/Core/Controllers/ExecutionOrchestrator.cs`（既存に追加）

#### TDDサイクル 1: GetMonitoringInterval()

##### Phase A: Red
```csharp
[Fact]
public void GetMonitoringInterval_DataProcessingConfigから監視間隔を取得する()
{
    // Arrange
    var mockConfig = new Mock<IOptions<DataProcessingConfig>>();
    mockConfig.Setup(c => c.Value).Returns(new DataProcessingConfig
    {
        MonitoringIntervalMs = 5000
    });
    var orchestrator = new ExecutionOrchestrator(
        mockTimerService.Object,
        mockLogger.Object,
        mockConfig.Object);

    // Act
    var interval = orchestrator.GetMonitoringInterval();

    // Assert
    Assert.Equal(TimeSpan.FromMilliseconds(5000), interval);
}
```

##### Phase B: Green
```csharp
private readonly IOptions<DataProcessingConfig> _dataProcessingConfig;

public ExecutionOrchestrator(
    ITimerService timerService,
    ILoggingManager loggingManager,
    IOptions<DataProcessingConfig> dataProcessingConfig)
{
    _timerService = timerService;
    _loggingManager = loggingManager;
    _dataProcessingConfig = dataProcessingConfig;
}

public TimeSpan GetMonitoringInterval()
{
    var intervalMs = _dataProcessingConfig.Value.MonitoringIntervalMs;
    return TimeSpan.FromMilliseconds(intervalMs);
}
```

#### TDDサイクル 2: RunContinuousDataCycleAsync()

##### Phase A: Red
```csharp
[Fact]
public async Task RunContinuousDataCycleAsync_TimerServiceを使用して繰り返し実行する()
{
    // Arrange
    var mockTimerService = new Mock<ITimerService>();
    var mockLogger = new Mock<ILoggingManager>();
    var mockConfig = new Mock<IOptions<DataProcessingConfig>>();
    mockConfig.Setup(c => c.Value).Returns(new DataProcessingConfig
    {
        MonitoringIntervalMs = 1000
    });

    var orchestrator = new ExecutionOrchestrator(
        mockTimerService.Object,
        mockLogger.Object,
        mockConfig.Object);

    var mockPlcManager = new Mock<IPlcCommunicationManager>();
    var plcManagers = new List<IPlcCommunicationManager> { mockPlcManager.Object };
    var cts = new CancellationTokenSource();

    // Act
    var task = orchestrator.RunContinuousDataCycleAsync(plcManagers, cts.Token);
    cts.CancelAfter(100);
    await task;

    // Assert
    mockTimerService.Verify(
        t => t.StartPeriodicExecution(
            It.IsAny<Func<Task>>(),
            TimeSpan.FromMilliseconds(1000),
            cts.Token),
        Times.Once());
}
```

##### Phase B: Green
```csharp
public async Task RunContinuousDataCycleAsync(
    List<IPlcCommunicationManager> plcManagers,
    CancellationToken cancellationToken)
{
    var interval = GetMonitoringInterval();

    await _timerService.StartPeriodicExecution(
        async () => await ExecuteMultiPlcCycleAsync(plcManagers, cancellationToken),
        interval,
        cancellationToken);
}
```

#### 完了条件
- [ ] GetMonitoringInterval()のテストがパス
- [ ] RunContinuousDataCycleAsync()のテストがパス
- [ ] 既存テストに影響がない

---

### Step 1-3: ApplicationController

#### 実装ファイル
- **テスト**: `Tests/Unit/Core/Controllers/ApplicationControllerTests.cs`
- **実装**: `andon/Core/Controllers/ApplicationController.cs`
- **インターフェース**: `andon/Core/Interfaces/IApplicationController.cs`

#### TDDサイクル 1: ExecuteStep1InitializationAsync()

##### Phase A: Red
```csharp
[Fact]
public async Task ExecuteStep1InitializationAsync_正常系_成功結果を返す()
{
    // Arrange
    var mockConfigManager = new Mock<IMultiPlcConfigManager>();
    var mockOrchestrator = new Mock<IExecutionOrchestrator>();
    var mockLogger = new Mock<ILoggingManager>();

    mockConfigManager
        .Setup(m => m.LoadAllConfigsAsync(It.IsAny<string>()))
        .ReturnsAsync(new List<PlcConnectionConfig>
        {
            new PlcConnectionConfig { PlcId = "PLC1" },
            new PlcConnectionConfig { PlcId = "PLC2" }
        });

    var controller = new ApplicationController(
        mockConfigManager.Object,
        mockOrchestrator.Object,
        mockLogger.Object);

    // Act
    var result = await controller.ExecuteStep1InitializationAsync();

    // Assert
    Assert.True(result.Success);
    Assert.Equal(2, result.PlcCount);
    mockLogger.Verify(m => m.LogInfo("Starting Step1 initialization"), Times.Once());
    mockLogger.Verify(m => m.LogInfo("Step1 initialization completed"), Times.Once());
}
```

##### Phase B: Green
```csharp
public class ApplicationController : IApplicationController
{
    private readonly IMultiPlcConfigManager _configManager;
    private readonly IExecutionOrchestrator _orchestrator;
    private readonly ILoggingManager _loggingManager;
    private List<IPlcCommunicationManager> _plcManagers;

    public ApplicationController(
        IMultiPlcConfigManager configManager,
        IExecutionOrchestrator orchestrator,
        ILoggingManager loggingManager)
    {
        _configManager = configManager;
        _orchestrator = orchestrator;
        _loggingManager = loggingManager;
    }

    public async Task<InitializationResult> ExecuteStep1InitializationAsync(
        string configDirectory = "./config/",
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _loggingManager.LogInfo("Starting Step1 initialization");

            var configs = await _configManager.LoadAllConfigsAsync(configDirectory);
            _plcManagers = new List<IPlcCommunicationManager>();

            // TODO: DIから取得したPlcCommunicationManagerを設定ごとに初期化

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

#### TDDサイクル 2: StartContinuousDataCycleAsync()

##### Phase A: Red
```csharp
[Fact]
public async Task StartContinuousDataCycleAsync_初期化成功後に継続実行を開始する()
{
    // Arrange
    var mockOrchestrator = new Mock<IExecutionOrchestrator>();
    var controller = CreateController(mockOrchestrator: mockOrchestrator);

    var initResult = new InitializationResult { Success = true, PlcCount = 2 };
    var cts = new CancellationTokenSource();

    // Act
    var task = controller.StartContinuousDataCycleAsync(initResult, cts.Token);
    cts.CancelAfter(100);
    await task;

    // Assert
    mockOrchestrator.Verify(
        o => o.RunContinuousDataCycleAsync(
            It.IsAny<List<IPlcCommunicationManager>>(),
            cts.Token),
        Times.Once());
}
```

##### Phase B: Green
```csharp
public async Task StartContinuousDataCycleAsync(
    InitializationResult initResult,
    CancellationToken cancellationToken)
{
    if (!initResult.Success || _plcManagers == null)
    {
        await _loggingManager.LogError(null, "Cannot start cycle: initialization failed");
        return;
    }

    await _loggingManager.LogInfo("Starting continuous data cycle");
    await _orchestrator.RunContinuousDataCycleAsync(_plcManagers, cancellationToken);
}
```

#### TDDサイクル 3: StartAsync() / StopAsync()

##### Phase A: Red
```csharp
[Fact]
public async Task StartAsync_Step1初期化後に継続実行を開始する()
{
    // Arrange
    var mockConfigManager = new Mock<IMultiPlcConfigManager>();
    var mockOrchestrator = new Mock<IExecutionOrchestrator>();
    var mockLogger = new Mock<ILoggingManager>();

    mockConfigManager
        .Setup(m => m.LoadAllConfigsAsync(It.IsAny<string>()))
        .ReturnsAsync(new List<PlcConnectionConfig> { new PlcConnectionConfig() });

    var controller = new ApplicationController(
        mockConfigManager.Object,
        mockOrchestrator.Object,
        mockLogger.Object);

    var cts = new CancellationTokenSource();

    // Act
    var task = controller.StartAsync(cts.Token);
    cts.CancelAfter(100);
    await task;

    // Assert
    mockOrchestrator.Verify(
        o => o.RunContinuousDataCycleAsync(It.IsAny<List<IPlcCommunicationManager>>(), cts.Token),
        Times.Once());
}
```

##### Phase B: Green
```csharp
public async Task StartAsync(CancellationToken cancellationToken)
{
    var initResult = await ExecuteStep1InitializationAsync(cancellationToken: cancellationToken);
    await StartContinuousDataCycleAsync(initResult, cancellationToken);
}

public async Task StopAsync(CancellationToken cancellationToken)
{
    await _loggingManager.LogInfo("Stopping application");
    // リソース解放処理（Phase 2で拡張）
}
```

#### 完了条件
- [ ] ExecuteStep1InitializationAsync()の正常系・異常系テストがパス
- [ ] StartContinuousDataCycleAsync()のテストがパス
- [ ] StartAsync() / StopAsync()のテストがパス
- [ ] コードカバレッジ85%以上

---

### Step 1-4: DependencyInjectionConfigurator

#### 実装ファイル
- **テスト**: `Tests/Unit/Services/DependencyInjectionConfiguratorTests.cs`
- **実装**: `andon/Services/DependencyInjectionConfigurator.cs`

#### TDDサイクル 1: DIコンテナ設定

##### Phase A: Red
```csharp
[Fact]
public void Configure_必要なサービスをすべて登録する()
{
    // Arrange
    var services = new ServiceCollection();

    // Act
    DependencyInjectionConfigurator.Configure(services);
    var provider = services.BuildServiceProvider();

    // Assert - Singleton
    Assert.NotNull(provider.GetService<IApplicationController>());
    Assert.Same(
        provider.GetService<IApplicationController>(),
        provider.GetService<IApplicationController>());

    // Assert - Transient
    Assert.NotNull(provider.GetService<IExecutionOrchestrator>());
    Assert.NotSame(
        provider.GetService<IExecutionOrchestrator>(),
        provider.GetService<IExecutionOrchestrator>());

    // Assert - TimerService
    Assert.NotNull(provider.GetService<ITimerService>());
}
```

##### Phase B: Green
```csharp
public static class DependencyInjectionConfigurator
{
    public static void Configure(IServiceCollection services)
    {
        // Singleton登録
        services.AddSingleton<IApplicationController, ApplicationController>();
        services.AddSingleton<ILoggingManager, LoggingManager>();
        services.AddSingleton<IErrorHandler, ErrorHandler>();
        services.AddSingleton<IResourceManager, ResourceManager>();

        // Transient登録
        services.AddTransient<IExecutionOrchestrator, ExecutionOrchestrator>();
        services.AddTransient<IPlcCommunicationManager, PlcCommunicationManager>();
        services.AddTransient<IConfigToFrameManager, ConfigToFrameManager>();
        services.AddTransient<IDataOutputManager, DataOutputManager>();
        services.AddTransient<ITimerService, TimerService>();

        // MultiConfig関連
        services.AddTransient<IMultiPlcConfigManager, MultiPlcConfigManager>();
        services.AddTransient<IMultiPlcCoordinator, MultiPlcCoordinator>();
    }
}
```

#### 完了条件
- [ ] DI登録テストがパス
- [ ] Singleton/Transientのライフタイムが正しい

---

### Step 1-5: AndonHostedService

#### 実装ファイル
- **テスト**: `Tests/Unit/Services/AndonHostedServiceTests.cs`
- **実装**: `andon/Services/AndonHostedService.cs`

#### TDDサイクル 1: HostedServiceライフサイクル

##### Phase A: Red
```csharp
[Fact]
public async Task StartAsync_ApplicationControllerのStartAsyncを呼び出す()
{
    // Arrange
    var mockController = new Mock<IApplicationController>();
    var mockLogger = new Mock<ILoggingManager>();
    var service = new AndonHostedService(mockController.Object, mockLogger.Object);

    // Act
    await service.StartAsync(CancellationToken.None);

    // Assert
    mockLogger.Verify(m => m.LogInfo("AndonHostedService starting"), Times.Once());
}
```

##### Phase B: Green
```csharp
public class AndonHostedService : BackgroundService
{
    private readonly IApplicationController _controller;
    private readonly ILoggingManager _loggingManager;

    public AndonHostedService(
        IApplicationController controller,
        ILoggingManager loggingManager)
    {
        _controller = controller;
        _loggingManager = loggingManager;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await _loggingManager.LogInfo("AndonHostedService starting");
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _controller.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _loggingManager.LogError(ex, "ExecuteAsync failed");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _loggingManager.LogInfo("AndonHostedService stopping");
        await _controller.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
```

#### 完了条件
- [ ] StartAsync()のテストがパス
- [ ] ExecuteAsync()のテストがパス
- [ ] StopAsync()のテストがパス

---

### Step 1-6: Program.cs

#### 実装ファイル
- **統合テスト**: `Tests/Integration/ApplicationStartupTests.cs`
- **実装**: `andon/Program.cs`

#### TDDサイクル 1: Hostビルド・起動

##### Phase A: Red（統合テスト）
```csharp
[Fact]
public async Task Application_正常に起動して終了する()
{
    // Arrange
    var cts = new CancellationTokenSource();
    cts.CancelAfter(TimeSpan.FromSeconds(2));

    // Act & Assert
    var exitCode = await Program.Main(new string[] { });

    Assert.Equal(0, exitCode);
}
```

##### Phase B: Green
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Andon
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                var host = CreateHostBuilder(args).Build();
                await host.RunAsync();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Application failed: {ex.Message}");
                return 1;
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    DependencyInjectionConfigurator.Configure(services);
                    services.AddHostedService<AndonHostedService>();
                });
    }
}
```

#### 完了条件
- [ ] アプリケーションが起動する
- [ ] DIコンテナが正しく構成される
- [ ] HostedServiceが開始される

---

### Phase 1 統合テスト

#### 実装ファイル
- **統合テスト**: `Tests/Integration/Phase1_IntegrationTests.cs`

#### テストケース

```csharp
[Fact]
public async Task Phase1統合_アプリケーションが周期的に実行される()
{
    // Arrange
    var host = Program.CreateHostBuilder(new string[] { }).Build();
    var cts = new CancellationTokenSource();

    // Act
    var task = host.RunAsync(cts.Token);
    await Task.Delay(TimeSpan.FromSeconds(10)); // 数サイクル実行
    cts.Cancel();
    await task;

    // Assert
    // ログファイルから実行回数を確認
    var logContent = await File.ReadAllTextAsync("logs/application.log");
    Assert.Contains("Starting continuous data cycle", logContent);
    Assert.Contains("Executing multi-PLC cycle", logContent);
}
```

### Phase 1 完了条件（全体）
- [ ] 全ユニットテストがパス（コードカバレッジ85%以上）
- [ ] 統合テストがパス
- [ ] アプリケーションが起動する（`dotnet run`でエラーなく開始）
- [ ] Step1初期化が成功する（設定ファイル読み込み、インスタンス作成）
- [ ] Step2-7が周期的に実行される（MonitoringIntervalMs間隔）
- [ ] ログ出力が正常に行われる
- [ ] Ctrl+Cで適切に終了する

---

## Phase 2: 実運用対応（高優先度）

### 目標
実運用環境で安定動作するために必要な機能を追加する。

### TDD実装順序

---

### Step 2-1: LoggingManager（拡張）- ✅ 実装済み（Phase3で完了）

LoggingManagerは**Phase3で完全実装済み**です。

#### 実装完了内容
- ✅ ファイル出力機能（InitializeFileWriter, WriteToFileAsync）
- ✅ ログレベル設定（ParseLogLevel, ShouldLog: Debug/Information/Warning/Error）
- ✅ ログファイルローテーション（CheckAndRotateFileAsync, RotateLogFileAsync）
- ✅ コンソール出力機能（ILogger<T>経由）
- ✅ 排他制御（SemaphoreSlim使用）

#### 完了条件
- [x] 全ログレベル（Debug, Information, Warning, Error）実装済み
- [x] ファイル出力が正常に動作
- [x] コンソール出力が正常に動作
- [x] ログローテーション機能実装済み

---

### Step 2-2: ErrorHandler（拡張）- ✅ 実装済み（Phase2で完了）

ErrorHandlerは**Phase2で完全実装済み**です。

#### 実装完了内容
- ✅ エラー分類機能（DetermineErrorCategory: 8種類の例外分類）
- ✅ リトライポリシー（ShouldRetry, GetMaxRetryCount, GetRetryDelayMs）

#### 完了条件
- [x] エラー分類テストがパス（Phase2完了）
- [x] リトライポリシーテストがパス（Phase2完了）

---

### Step 2-3: GracefulShutdownHandler

#### TDDサイクル 1: シャットダウンハンドラー登録

##### Phase A: Red
```csharp
[Fact]
public void RegisterShutdownHandlers_Ctrl_C押下時にExecuteGracefulShutdownが呼ばれる()
{
    // Arrange
    var mockController = new Mock<IApplicationController>();
    var mockLogger = new Mock<ILoggingManager>();
    var handler = new GracefulShutdownHandler(mockLogger.Object);
    var cts = new CancellationTokenSource();

    // Act
    handler.RegisterShutdownHandlers(mockController.Object, cts);

    // シミュレート: Console.CancelKeyPressイベント発火
    // （実際のテストではイベントをトリガーする方法を使用）

    // Assert
    mockController.Verify(c => c.StopAsync(It.IsAny<CancellationToken>()), Times.Once());
}
```

##### Phase B: Green
```csharp
public class GracefulShutdownHandler
{
    private readonly ILoggingManager _loggingManager;
    private CancellationTokenSource _shutdownCts;

    public GracefulShutdownHandler(ILoggingManager loggingManager)
    {
        _loggingManager = loggingManager;
    }

    public void RegisterShutdownHandlers(
        IApplicationController controller,
        CancellationTokenSource cts)
    {
        _shutdownCts = cts;

        Console.CancelKeyPress += async (sender, e) =>
        {
            e.Cancel = true;
            await _loggingManager.LogInfo("Shutdown signal received (Ctrl+C)");
            await ExecuteGracefulShutdown(controller);
        };

        AppDomain.CurrentDomain.ProcessExit += async (sender, e) =>
        {
            await _loggingManager.LogInfo("Process exit signal received");
            await ExecuteGracefulShutdown(controller);
        };
    }

    public async Task<ShutdownResult> ExecuteGracefulShutdown(
        IApplicationController controller,
        TimeSpan timeout = default)
    {
        if (timeout == default)
            timeout = TimeSpan.FromSeconds(30);

        try
        {
            _shutdownCts?.Cancel();

            using var timeoutCts = new CancellationTokenSource(timeout);
            await controller.StopAsync(timeoutCts.Token);

            return new ShutdownResult { Success = true };
        }
        catch (Exception ex)
        {
            await _loggingManager.LogError(ex, "Graceful shutdown failed");
            return new ShutdownResult { Success = false, ErrorMessage = ex.Message };
        }
    }
}
```

#### 完了条件
- [ ] シャットダウンハンドラー登録のテストがパス
- [ ] ExecuteGracefulShutdown()のテストがパス

---

### Step 2-4: ConfigurationWatcher

#### TDDサイクル 1: ファイル変更監視

##### Phase A: Red
```csharp
[Fact]
public async Task StartWatching_設定ファイル変更時にイベント発火()
{
    // Arrange
    var watcher = new ConfigurationWatcher();
    bool eventRaised = false;
    string changedFile = null;

    watcher.OnConfigurationChanged += (sender, args) =>
    {
        eventRaised = true;
        changedFile = args.FilePath;
    };

    var testDir = "./test_config/";
    Directory.CreateDirectory(testDir);

    // Act
    watcher.StartWatching(testDir);
    await File.WriteAllTextAsync(Path.Combine(testDir, "test.json"), "{}");
    await Task.Delay(500); // イベント発火待機

    // Assert
    Assert.True(eventRaised);
    Assert.Contains("test.json", changedFile);
}
```

##### Phase B: Green
```csharp
public class ConfigurationWatcher : IConfigurationWatcher
{
    public event EventHandler<ConfigurationChangedEventArgs> OnConfigurationChanged;

    private FileSystemWatcher _watcher;

    public void StartWatching(string configDirectory)
    {
        _watcher = new FileSystemWatcher(configDirectory)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            Filter = "*.json"
        };

        _watcher.Changed += OnFileChanged;
        _watcher.EnableRaisingEvents = true;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        OnConfigurationChanged?.Invoke(this,
            new ConfigurationChangedEventArgs { FilePath = e.FullPath });
    }
}
```

#### 完了条件
- [ ] ファイル変更検知のテストがパス

---

### Step 2-5: CommandLineOptions

#### TDDサイクル 1: コマンドライン引数解析

##### Phase A: Red
```csharp
[Fact]
public void Parse_ConfigPathオプションを正しく解析する()
{
    // Arrange
    var args = new[] { "--config", "/custom/path/" };

    // Act
    var result = Parser.Default.ParseArguments<CommandLineOptions>(args);

    // Assert
    result.WithParsed(opts =>
    {
        Assert.Equal("/custom/path/", opts.ConfigPath);
    });
}
```

##### Phase B: Green
```csharp
using CommandLine;

public class CommandLineOptions
{
    [Option('c', "config", Required = false,
        HelpText = "Configuration directory path")]
    public string ConfigPath { get; set; } = "./config/";

    [Option('v', "version", Required = false,
        HelpText = "Show version information")]
    public bool ShowVersion { get; set; }

    [Option('h', "help", Required = false,
        HelpText = "Show help information")]
    public bool ShowHelp { get; set; }
}
```

#### 完了条件
- [ ] 各オプションの解析テストがパス

---

### Step 2-6: ExitCodeManager

#### TDDサイクル 1: 終了コード管理

##### Phase A: Red
```csharp
[Fact]
public void FromException_TimeoutExceptionの場合はTimeoutErrorコードを返す()
{
    // Arrange
    var ex = new TimeoutException();

    // Act
    var exitCode = ExitCodeManager.FromException(ex);

    // Assert
    Assert.Equal(ExitCodeManager.TimeoutError, exitCode);
}
```

##### Phase B: Green
```csharp
public static class ExitCodeManager
{
    public const int Success = 0;
    public const int ConfigurationError = 1;
    public const int ConnectionError = 2;
    public const int TimeoutError = 3;
    public const int DataProcessingError = 4;
    public const int UnknownError = 99;

    public static int FromException(Exception ex)
    {
        return ex switch
        {
            MultiConfigLoadException => ConfigurationError,
            TimeoutException => TimeoutError,
            SocketException => ConnectionError,
            _ => UnknownError
        };
    }
}
```

#### 完了条件
- [ ] 全例外タイプの終了コード変換テストがパス

---

### Phase 2 完了条件（全体）
- [ ] 全ユニットテストがパス
- [ ] Ctrl+C、プロセス終了時に適切な終了処理が実行される
- [ ] ログファイル・コンソールに詳細なログが出力される
- [ ] エラー発生時に適切なリトライ処理が実行される
- [ ] 設定ファイル変更時に動的に再読み込みされる
- [ ] コマンドライン引数が正しく解析される
- [ ] 終了コードが適切に設定される

---

## Phase 3: 高度な機能（中優先度）

### 目標
アプリケーションの高度な機能をサポートするクラス群を実装する。

### 実装対象クラス（9つ）
- AsyncExceptionHandler.cs - 階層的例外ハンドリング
- CancellationCoordinator.cs - キャンセレーション制御
- ResourceSemaphoreManager.cs - 共有リソース排他制御
- ProgressReporter.cs - 進捗報告
- ParallelExecutionController.cs - 並行実行制御
- OptionsConfigurator.cs - Optionsパターン設定
- ServiceLifetimeManager.cs - サービスライフタイム管理
- MultiConfigDIIntegration.cs - 複数設定DI統合
- ResourceManager.cs - リソース管理（拡張）

**各クラスも同様にTDDサイクルで実装**
1. Red: テスト先行
2. Green: 最小実装
3. Refactor: リファクタリング

### Phase 3 完了条件
- [ ] 全ユニットテストがパス
- [ ] 階層的な例外処理が動作する
- [ ] キャンセレーション処理が正しく伝播する
- [ ] 共有リソースの排他制御が機能する
- [ ] 進捗情報がリアルタイムで報告される
- [ ] 並行実行の制御が正しく動作する

---

## Phase 4: オプション機能（低優先度）

### 目標
あると便利だが、なくても動作するユーティリティクラスを実装する。

### 実装対象クラス（2つ）
- CsvFileWriter.cs - CSV出力ユーティリティ
- DataTransferManager.cs - データ転送ユーティリティ

**TDDサイクルで実装**

### Phase 4 完了条件
- [ ] 全ユニットテストがパス
- [ ] CSV出力機能が動作する
- [ ] 古いファイルの自動削除が機能する
- [ ] データ転送機能が動作する

---

## TDD実装時の共通注意点

### 1. Red-Green-Refactorサイクルの厳守
- **必ずテストを先に書く**（Red）
- **テストを通すための最小限のコードのみ実装**（Green）
- **動作を保ったままリファクタリング**（Refactor）

### 2. 小さなステップで進める
- 1つのTDDサイクルは15分以内に完了するサイズにする
- 大きな機能は複数のTDDサイクルに分割する

### 3. テスト可能な設計
- **依存性注入**: すべての依存関係はコンストラクタで注入
- **純粋関数の活用**: 副作用のない関数を優先
- **単一責任原則**: 各クラスは1つの責任のみを持つ

### 4. モック/スタブの活用
- 外部依存（PLC通信、ファイルI/O等）は必ずモック化
- Moqライブラリを使用してインターフェースをモック

```csharp
// モックの例
var mockLogger = new Mock<ILoggingManager>();
mockLogger.Setup(m => m.LogInfo(It.IsAny<string>())).Returns(Task.CompletedTask);
```

### 5. 境界値テスト
- 正常系だけでなく、境界値・異常系も必ずテスト
- 空配列、null、タイムアウト、例外発生などをカバー

### 6. コードカバレッジ目標
- **Phase 1**: 85%以上
- **Phase 2以降**: 90%以上

### 7. 非同期処理のテスト
- `async/await`を正しく使用
- タイムアウトテストには`Task.Delay()`や`CancellationTokenSource`を活用

### 8. テスト実行の自動化
```bash
# 全テスト実行
dotnet test

# 特定のテストクラス実行
dotnet test --filter "FullyQualifiedName~TimerServiceTests"

# カバレッジ測定
dotnet test /p:CollectCoverage=true
```

---

## 参照ドキュメント

- **TDD手法**: `documents/development_methodology/development-methodology.md`
- **プロジェクト構造設計**: `documents/design/プロジェクト構造設計.md`
- **クラス設計**: `documents/design/クラス設計.md`
- **テスト内容**: `documents/design/テスト内容.md`
- **実装チェックリスト**: `documents/design/実装チェックリスト.md`

---

## 全体実装状況（2025年12月1日更新）

### ✅ Phase 1完了（2025年11月27日）
**継続実行モード基盤実装**
- Step 1-1: TimerService - 周期実行、重複実行防止、例外処理（3テストケース）
- Step 1-2: ExecutionOrchestrator - GetMonitoringInterval, RunContinuousDataCycleAsync（2テストケース）
- Step 1-3: ApplicationController - ExecuteStep1InitializationAsync, StartContinuousDataCycleAsync, StartAsync/StopAsync（4テストケース）
- Step 1-4: DependencyInjectionConfigurator - DIコンテナ設定（1テストケース）
- Step 1-5: AndonHostedService - HostedServiceライフサイクル（3テストケース）
- Step 1-6: Program.cs - Hostビルド・起動（1テストケース）
- **テスト結果**: 15/15成功（100%）
- **TDD準拠**: Red-Green-Refactor厳守

### ✅ Phase 2完了（2025年11月27日）
**実運用機能実装**
- Step 2-2: ErrorHandler - エラー分類、リトライポリシー（既存実装拡張）
- Step 2-3: GracefulShutdownHandler - 適切な終了処理（既存実装拡張）
- Step 2-4: ConfigurationWatcher - 設定ファイル変更監視（5テストケース）
- Step 2-5: CommandLineOptions - コマンドライン引数解析（12テストケース）
- Step 2-6: ExitCodeManager - 終了コード管理（15テストケース）
- **テスト結果**: 32/32成功（100%）
- **TDD準拠**: Red-Green-Refactor厳守

### ✅ Phase 2 Step 2-7部分完了（2025年12月1日）
**ConfigurationLoaderExcel統合**
- TDDサイクル 1: ConfigurationLoaderExcelのDI登録（1テストケース）
- TDDサイクル 2: ApplicationController起動時の設定読み込み（10テストケース）
- **実装内容**:
  - DependencyInjectionConfigurator.cs - ConfigurationLoaderExcel Singleton登録
  - ApplicationController.cs - ConfigurationLoaderExcel依存性注入、ExecuteStep1InitializationAsync()拡張
  - ApplicationControllerTests.cs - 統合テスト追加
- **テスト結果**: 11/11成功（100%、TDDサイクル1-2）
- **TDD準拠**: Red-Green-Refactor厳守
- **残作業**: TDDサイクル 3-4（統合テスト、エラーケーステスト、目標8-10テストケース追加）

### ✅ Phase 3完了（2025年11月27日-12月1日）
**高度な機能実装**
- **Part1（2025-11-27）**: AsyncExceptionHandler、CancellationCoordinator、ResourceSemaphoreManager（28/28テスト）
- **Part2（2025-11-27）**: ProgressInfo、ParallelProgressInfo、ProgressReporter（39/39テスト）
- **Part3（2025-11-27）**: ParallelExecutionResult、ParallelExecutionController（16/16テスト）
- **Part4（2025-11-27）**: SystemResourcesConfig、LoggingConfig、OptionsConfigurator（10/10テスト）
- **Part5（2025-11-28）**: ServiceLifetimeManager、MultiConfigDIIntegration、ResourceManager（32/32テスト）
- **Part6（2025-11-28）**: LoggingManager拡張（ファイル出力、ログレベル、ローテーション）（28/28テスト）
- **Part7（2025-11-28）**: ConfigurationWatcher Excel対応、ApplicationController統合（16/16テスト）
- **Part8（2025-12-01）**: DependencyInjectionConfigurator DIコンテナ統合（12/12テスト）
- **テスト結果**: 177/177成功（100%）
- **TDD準拠**: Red-Green-Refactor厳守
- **実装効果**:
  - Phase3実装クラス（7クラス）が実運用可能
  - 階層的例外ハンドリング、並行実行制御、進捗報告、適切な終了処理、設定ファイル監視が全て利用可能
  - Options<T>設定完全化（SystemResourcesConfig、LoggingConfig）

### ⏳ Phase 2 Step 2-7残作業
- TDDサイクル 3: 統合テスト（実Excelファイル `5JRS_N2.xlsx` 使用）
- TDDサイクル 4: エラーケーステスト（不正ファイル、ロック等）

### ⏳ Phase 4未着手
- CsvFileWriter、DataTransferManager等、オプション機能（2クラス）

---

## 累計実装・テスト統計（2025年12月1日時点）

- **Phase 1**: 15/15テスト成功（100%）
- **Phase 2**: 32/32テスト成功（100%）
- **Phase 2 Step 2-7**: 11/11テスト成功（100%）
- **Phase 3**: 177/177テスト成功（100%）
- **合計**: 235/235テスト成功（100%）

---

## 作成日時
- **作成日**: 2025年11月27日
- **最終更新**: 2025年12月1日
- **バージョン**: 3.0（Phase 3完了）
