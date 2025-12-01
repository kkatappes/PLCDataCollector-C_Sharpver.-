# アーキテクチャ テスト設計 (Part 1/2)

## テスト方針
- **TDD手法**を使用してテスト駆動開発を実施
- ApplicationController、ExecutionOrchestrator、非同期・並行処理アーキテクチャをテスト
- 継続実行モード対応、グレースフル停止制御の確実な動作確認

---

## 10. ApplicationController・ExecutionOrchestrator テスト設計

### 10.1 ApplicationController テスト設計
**目的**: アプリケーション全体のライフサイクル管理、Step1初期化フェーズ実行、Step2-7データ処理サイクルの継続実行制御をテスト

#### 正常系テスト
- **TC150_ApplicationController_StartAsync_正常開始**
  - 入力: CancellationToken（キャンセレーション制御）
  - 期待出力: Task（非同期実行完了状態）
  - 処理内容:
    - Step1初期化フェーズ実行（ExecuteStep1InitializationAsync()）
    - Step2-7継続実行開始（StartContinuousDataCycleAsync()）

- **TC151_ApplicationController_StopAsync_正常停止**
  - 入力: CancellationToken（停止制御）
  - 期待出力: Task（非同期停止完了状態）
  - 処理内容:
    - 実行中サイクルの適切な停止
    - 各Managerクラスのリソース解放
    - PLC接続の適切な切断

- **TC152_ApplicationController_ExecuteStep1InitializationAsync_初期化成功**
  - 入力: 設定ディレクトリパス（string型、デフォルト："./config/"）
  - 期待出力: InitializationResult（初期化結果オブジェクト）
    - LoadedConfigCount（読み込み設定ファイル数）
    - CreatedManagersCount（作成されたManagerインスタンス数）
    - InitializationTime（初期化処理時間）
    - IsSuccess=true（初期化成功フラグ）
    - ErrorDetails=null（エラー詳細：成功時はnull）
  - 処理内容:
    - 複数設定ファイル読み込み（MultiConfigManager.LoadAllFromDirectoryAsync()）
    - 各種Managerクラスインスタンス作成
    - 初期化検証・ヘルスチェック

- **TC153_ApplicationController_StartContinuousDataCycleAsync_継続実行開始**
  - 入力:
    - InitializationResult（Step1初期化結果）
    - CancellationToken（実行制御）
  - 期待出力: Task（継続実行タスク）
  - 処理内容:
    - ExecutionOrchestratorインスタンス作成
    - 複数PLC並行実行開始（Task.WhenAll使用）
    - エラー発生時の継続処理制御

#### 異常系テスト
- **TC154_ApplicationController_ExecuteStep1InitializationAsync_初期化失敗**
  - 入力: 存在しない設定ディレクトリパス
  - 期待出力: InitializationResult（失敗状態）
    - IsSuccess=false
    - ErrorDetails（失敗理由詳細）
    - LoadedConfigCount=0

- **TC155_ApplicationController_StartAsync_キャンセル処理**
  - 入力: 即座にキャンセルされるCancellationToken
  - 期待出力: OperationCanceledException
  - 処理内容: 適切なキャンセル処理とリソース解放

### 10.2 ExecutionOrchestrator テスト設計
**目的**: Step2-7データ処理サイクルの詳細実行制御、MonitoringIntervalMs間隔制御、単一PLC用実行ロジックをテスト

#### 正常系テスト
- **TC156_ExecutionOrchestrator_RunContinuousDataCycleAsync_継続実行**
  - 入力:
    - ConfigToFrameManager（PLC用設定・フレーム管理）
    - PlcIdentifier（PLC識別子：string型）
    - CancellationToken（実行制御）
  - 期待出力: Task（継続実行タスク）
  - 処理内容:
    - TimerService使用によるMonitoringIntervalMs間隔制御
    - ExecuteSingleCycleAsync()の繰り返し実行
    - エラー発生時の継続判定・ログ出力

- **TC157_ExecutionOrchestrator_ExecuteSingleCycleAsync_単一サイクル成功**
  - 入力:
    - ConfigToFrameManager（設定・フレーム管理）
    - PlcCommunicationManager（PLC通信管理）
    - DataOutputManager（データ出力管理）
    - LoggingManager（ログ管理）
  - 期待出力: CycleExecutionResult（サイクル実行結果オブジェクト）
    - IsSuccess=true（サイクル成功フラグ）
    - ExecutedSteps=["Step2", "Step3", "Step4", "Step5", "Step6", "Step7"]
    - ExecutionTime（サイクル実行時間）
    - DataCount（処理データ数）
    - ErrorDetails=null（成功時はnull）
  - 処理内容:
    - Step2: ConfigToFrameManager.SplitDwordToWord() → ConfigToFrameManager.BuildFrames()
    - Step3: PlcCommunicationManager.ConnectAsync()
    - Step4: PlcCommunicationManager.SendFrameAsync() → PlcCommunicationManager.ReceiveResponseAsync()
    - Step5: PlcCommunicationManager.DisconnectAsync()
    - Step6: PlcCommunicationManager.PostprocessReceivedData() → PlcCommunicationManager.ParseRawToStructuredData()
    - Step7: DataOutputManager.OutputDataAsync()

- **TC158_ExecutionOrchestrator_GetMonitoringInterval_間隔取得**
  - 入力: ConfigToFrameManager（設定管理）
  - 期待出力: TimeSpan（監視間隔：MonitoringIntervalMs設定値をTimeSpanに変換）
  - データ取得元: ConfigToFrameManager.GetConfig<DataProcessingConfig>()（MonitoringIntervalMs設定）

#### 異常系テスト
- **TC159_ExecutionOrchestrator_ExecuteSingleCycleAsync_Step内エラー**
  - 入力: 有効な各Managerインスタンス（Step3で通信エラー発生設定）
  - 期待出力: CycleExecutionResult（部分失敗状態）
    - IsSuccess=false
    - ExecutedSteps=["Step2"]（Step3で失敗）
    - ErrorDetails（Step3エラー詳細）

- **TC160_ExecutionOrchestrator_RunContinuousDataCycleAsync_キャンセル**
  - 入力: キャンセル済みCancellationToken
  - 期待出力: OperationCanceledException
  - 処理内容: 実行中サイクルの適切な中断

### 10.3 AndonHostedService テスト設計
**目的**: .NET HostedServiceとしてのバックグラウンド実行、ApplicationControllerのライフサイクル管理をテスト

#### 正常系テスト
- **TC161_AndonHostedService_ExecuteAsync_バックグラウンド実行**
  - 入力: CancellationToken（.NETランタイムから取得）
  - 期待出力: Task（バックグラウンド実行タスク）
  - 処理内容:
    - ApplicationController.StartAsync()実行
    - CancellationToken監視による適切な終了制御

- **TC162_AndonHostedService_StartAsync_サービス開始**
  - 入力: CancellationToken（.NETランタイムから取得）
  - 期待出力: Task（開始処理完了状態）
  - 処理内容:
    - 起動ログ出力
    - ApplicationController初期化確認

- **TC163_AndonHostedService_StopAsync_サービス停止**
  - 入力: CancellationToken（.NETランタイムから取得）
  - 期待出力: Task（停止処理完了状態）
  - 処理内容:
    - ApplicationController.StopAsync()実行
    - 停止ログ出力

### 10.4 TimerService テスト設計
**目的**: MonitoringIntervalMs間隔でのタイマー制御、精密な間隔制御をテスト

#### 正常系テスト
- **TC164_TimerService_StartPeriodicExecution_周期実行**
  - 入力:
    - Func<Task>（実行する非同期処理）
    - TimeSpan（実行間隔：1000ms）
    - CancellationToken（実行制御）
  - 期待出力: Task（周期実行タスク）
  - 処理内容:
    - System.Threading.PeriodicTimer使用
    - 指定間隔での正確な実行制御
    - 前回処理未完了時の重複実行防止

#### 異常系テスト
- **TC165_TimerService_StartPeriodicExecution_実行中例外**
  - 入力: 例外を発生させるFunc<Task>
  - 期待出力: 例外処理後の継続実行
  - 処理内容: 個別実行エラーでもタイマー継続

### 10.5 GracefulShutdownHandler テスト設計
**目的**: Ctrl+C等のシグナル処理、適切な終了処理実行、リソース解放順序制御をテスト

#### 正常系テスト
- **TC166_GracefulShutdownHandler_RegisterShutdownHandlers_ハンドラ登録**
  - 入力:
    - ApplicationController（制御対象）
    - CancellationTokenSource（キャンセレーション制御）
  - 期待出力: 登録完了状態
  - 処理内容:
    - Console.CancelKeyPress登録
    - AppDomain.ProcessExit登録
    - キャンセレーショントークン発行

- **TC167_GracefulShutdownHandler_ExecuteGracefulShutdown_適切な終了**
  - 入力:
    - ApplicationController（制御対象）
    - TimeSpan（タイムアウト時間：30秒）
  - 期待出力: ShutdownResult（終了処理結果）
    - IsGraceful=true（適切な終了処理完了フラグ）
    - ShutdownTime（終了処理にかかった時間）
    - CompletedTasks（完了した終了タスク一覧）
    - IncompleteTaskCount=0（未完了タスク数）
    - FinalResourceState（最終リソース状態）
    - ShutdownTrigger（終了契機）
  - 処理内容:
    - ApplicationController.StopAsync()実行
    - 各Managerクラスのリソース解放確認
    - タイムアウト制御

#### 異常系テスト
- **TC168_GracefulShutdownHandler_ExecuteGracefulShutdown_タイムアウト**
  - 入力: 長時間処理を含むApplicationController、短いタイムアウト時間
  - 期待出力: ShutdownResult（タイムアウト終了状態）
    - IsGraceful=false
    - IncompleteTaskCount>0
    - IncompleteTaskDetails（未完了タスク詳細一覧）

---

## 11. 非同期・並行処理アーキテクチャ テスト設計

### 11.1 AsyncExceptionHandler テスト設計
**目的**: 階層的例外ハンドリング、重要処理の個別対応、一般処理の一括処理、統一エラーログ出力をテスト

#### 正常系テスト
- **TC169_AsyncExceptionHandler_HandleCriticalOperationAsync_重要処理成功**
  - 入力:
    - Func<Task<T>>（成功する重要処理）
    - string（処理名称："PLC接続処理"）
    - CancellationToken（キャンセル制御）
  - 期待出力: AsyncOperationResult<T>（実行結果オブジェクト）
    - IsSuccess=true（実行成功フラグ）
    - Result（実行結果：成功時のみ設定）
    - Exception=null（失敗時のみ設定）
    - ExecutionTime（実行時間）
    - OperationName="PLC接続処理"（処理名称）

- **TC170_AsyncExceptionHandler_HandleGeneralOperationsAsync_一般処理一括成功**
  - 入力:
    - IEnumerable<Func<Task>>（成功する一般処理群）
    - string（処理グループ名称："ログ出力処理群"）
    - CancellationToken（キャンセル制御）
  - 期待出力: GeneralOperationResult（一括実行結果オブジェクト）
    - SuccessCount（成功処理数）
    - FailureCount=0（失敗処理数）
    - TotalExecutionTime（全体実行時間）
    - FailedOperations=[]（失敗した処理名一覧：空リスト）
    - Exceptions=[]（発生例外一覧：空リスト）

#### 異常系テスト
- **TC171_AsyncExceptionHandler_HandleCriticalOperationAsync_重要処理失敗**
  - 入力: 例外を発生するFunc<Task<T>>
  - 期待出力: AsyncOperationResult<T>（失敗状態）
    - IsSuccess=false
    - Result=null
    - Exception（発生例外）
    - ExecutionTime（実行時間）

- **TC172_AsyncExceptionHandler_HandleGeneralOperationsAsync_部分失敗**
  - 入力: 成功処理と失敗処理が混在するIEnumerable<Func<Task>>
  - 期待出力: GeneralOperationResult（部分失敗状態）
    - SuccessCount>0, FailureCount>0
    - FailedOperations（失敗処理名一覧）
    - Exceptions（発生例外一覧）

### 11.2 CancellationCoordinator テスト設計
**目的**: CancellationToken階層管理、適切なキャンセル伝達、グレースフル停止制御をテスト

#### 正常系テスト
- **TC173_CancellationCoordinator_CreateHierarchicalToken_階層トークン作成**
  - 入力:
    - CancellationToken（親トークン）
    - TimeSpan（タイムアウト時間：5秒）
  - 期待出力: CancellationTokenSource（子トークンソース）
  - 処理内容:
    - 親トークンとタイムアウトの組み合わせ
    - 階層的キャンセル制御

- **TC174_CancellationCoordinator_RegisterCancellationCallback_コールバック登録**
  - 入力:
    - CancellationToken（対象トークン）
    - Func<Task>（キャンセル時実行処理）
    - string（コールバック名称："リソース解放"）
  - 期待出力: CancellationTokenRegistration（登録ハンドル）
  - 処理内容:
    - キャンセル時の適切な清掃処理登録
    - 非同期コールバック対応

### 11.3 ResourceSemaphoreManager テスト設計
**目的**: 共有リソースの排他制御、ログファイル・設定ファイル競合回避、パフォーマンス最適化をテスト

#### 正常系テスト
- **TC175_ResourceSemaphoreManager_ExecuteWithSemaphoreAsync_セマフォ制御実行**
  - 入力:
    - SemaphoreSlim（LogFileSemaphore：同時アクセス数1）
    - Func<Task<T>>（実行対象処理）
    - CancellationToken（キャンセル制御）
    - TimeSpan（セマフォ取得タイムアウト：30秒）
  - 期待出力: T（実行結果）
  - 処理内容:
    - セマフォ取得→処理実行→セマフォ解放の確実実行
    - タイムアウト制御
    - 例外発生時の確実なセマフォ解放

- **TC176_ResourceSemaphoreManager_GetResourceSemaphore_リソース種別セマフォ取得**
  - 入力: ResourceType（LogFile, ConfigFile, OutputFile）
  - 期待出力: SemaphoreSlim（対応するセマフォ）
  - 検証項目:
    - LogFileSemaphore：同時アクセス数1
    - ConfigFileSemaphore：同時アクセス数3
    - OutputFileSemaphore：同時アクセス数2

### 11.4 ProgressReporter テスト設計
**目的**: IProgress<T>実装、リアルタイム進捗報告、UI・コンソール出力対応をテスト

#### 正常系テスト
- **TC177_ProgressReporter_Report_進捗報告**
  - 入力: ProgressInfo（現在ステップ、進捗率、メッセージ）
  - 期待出力: void（進捗情報の出力・通知）
  - 処理内容:
    - コンソール出力（リアルタイム表示）
    - ログファイル記録
    - 進捗率計算・表示

- **TC178_ProgressReporter_CreateStepProgress_ステップ別進捗作成**
  - 入力:
    - string（ステップ名："Step3"）
    - int（予想処理数：100）
  - 期待出力: ProgressReporter<ProgressInfo>（ステップ専用進捗レポーター）

### 11.5 ParallelExecutionController テスト設計
**目的**: 複数PLC並行実行制御、Task.WhenAll活用、エラー発生時継続制御をテスト

#### 正常系テスト
- **TC179_ParallelExecutionController_ExecuteParallelPlcOperationsAsync_並行実行成功**
  - 入力:
    - IEnumerable<ConfigToFrameManager>（PLC用設定管理インスタンス群）
    - Func<ConfigToFrameManager, CancellationToken, Task<CycleExecutionResult>>（実行処理）
    - CancellationToken（実行制御）
  - 期待出力: ParallelExecutionResult（並行実行結果オブジェクト）
    - TotalPlcCount（対象PLC総数）
    - SuccessfulPlcCount（成功PLC数）
    - FailedPlcCount=0（失敗PLC数）
    - PlcResults（PLC別実行結果）
    - OverallExecutionTime（全体実行時間）
    - ContinuingPlcIds（継続実行中PLC ID一覧）

- **TC180_ParallelExecutionController_MonitorParallelExecution_並行実行監視**
  - 入力:
    - IEnumerable<Task<CycleExecutionResult>>（実行中タスク群）
    - IProgress<ParallelProgressInfo>（進捗レポーター）
    - CancellationToken（監視制御）
  - 期待出力: Task（監視タスク）
  - 処理内容:
    - 実行中タスクの状態監視
    - 完了・エラー・継続状況のリアルタイム報告
    - 全体進捗率計算・表示

#### 異常系テスト
- **TC181_ParallelExecutionController_ExecuteParallelPlcOperationsAsync_部分失敗**
  - 入力: 一部PLC処理が失敗するシナリオ
  - 期待出力: ParallelExecutionResult（部分失敗状態）
    - SuccessfulPlcCount>0, FailedPlcCount>0
    - エラー発生PLC以外の継続実行制御

---

## テスト統計

### ApplicationController・ExecutionOrchestrator
- **総テストケース数**: 19個 (TC150～TC168)
  - ApplicationController: 6ケース
  - ExecutionOrchestrator: 5ケース
  - AndonHostedService: 3ケース
  - TimerService: 2ケース
  - GracefulShutdownHandler: 3ケース

### 非同期・並行処理アーキテクチャ
- **総テストケース数**: 13個 (TC169～TC181)
  - AsyncExceptionHandler: 4ケース
  - CancellationCoordinator: 2ケース
  - ResourceSemaphoreManager: 2ケース
  - ProgressReporter: 2ケース
  - ParallelExecutionController: 3ケース

### 合計
- **総テストケース数**: 32個
- **優先度**: 高（Step1-6完了後に実装）
# アーキテクチャ テスト設計 (Part 2/2)

## 12. DI（依存性注入）コンテナ設計 テスト設計

### 12.1 DependencyInjectionConfigurator テスト設計
**目的**: DIコンテナ設定、サービス登録、ライフタイム管理、インターフェースマッピングをテスト

#### 正常系テスト
- **TC182_DependencyInjectionConfigurator_ConfigureServices_サービス登録**
  - 入力:
    - IServiceCollection（DIコンテナ）
    - IConfiguration（設定情報：appsettings.json、環境変数）
  - 期待出力: IServiceCollection（設定完了済みDIコンテナ）
  - 処理内容:
    - 全主要クラスのインターフェース登録
    - ライフタイム設定（Singleton/Transient）
    - Optionsパターン設定値注入
    - HostedService登録

- **TC183_DependencyInjectionConfigurator_RegisterCoreServices_コアサービス登録**
  - 入力: IServiceCollection（DIコンテナ）
  - 期待出力: IServiceCollection（コア登録完了済み）
  - 検証項目:
    - ApplicationController → IApplicationController（Singleton）
    - ExecutionOrchestrator → IExecutionOrchestrator（Transient：PLC別インスタンス）
    - ConfigToFrameManager → IConfigToFrameManager（Transient：設定ファイル別）
    - PlcCommunicationManager → IPlcCommunicationManager（Transient：PLC別）
    - DataOutputManager → IDataOutputManager（Singleton：共有リソース）

- **TC184_DependencyInjectionConfigurator_RegisterInfrastructureServices_インフラサービス登録**
  - 入力: IServiceCollection（DIコンテナ）
  - 期待出力: IServiceCollection（インフラ登録完了済み）
  - 検証項目:
    - LoggingManager → ILoggingManager（Singleton：ログ集約）
    - ErrorHandler → IErrorHandler（Singleton：エラー統計）
    - ResourceManager → IResourceManager（Singleton：システム監視）
    - AsyncExceptionHandler → IAsyncExceptionHandler（Singleton：例外処理統一）
    - ResourceSemaphoreManager → IResourceSemaphoreManager（Singleton：共有リソース制御）

- **TC185_DependencyInjectionConfigurator_RegisterAsyncServices_非同期処理サービス登録**
  - 入力: IServiceCollection（DIコンテナ）
  - 期待出力: IServiceCollection（非同期登録完了済み）
  - 検証項目:
    - CancellationCoordinator → ICancellationCoordinator（Singleton：キャンセル制御統一）
    - ParallelExecutionController → IParallelExecutionController（Singleton：並行実行制御）
    - ProgressReporter<T> → IProgressReporter<T>（Transient：進捗報告個別）
    - TimerService → ITimerService（Transient：タイマー個別）

- **TC186_DependencyInjectionConfigurator_RegisterHostedServices_HostedService登録**
  - 入力: IServiceCollection（DIコンテナ）
  - 期待出力: IServiceCollection（HostedService登録完了済み）
  - 検証項目:
    - AndonHostedService登録（IHostedService）
    - GracefulShutdownHandler登録（バックグラウンドサービス）

### 12.2 OptionsConfigurator テスト設計
**目的**: Optionsパターン設定値注入、型安全設定管理、バリデーション設定をテスト

#### 正常系テスト
- **TC187_OptionsConfigurator_ConfigureOptions_Options設定注入**
  - 入力:
    - IServiceCollection（DIコンテナ）
    - IConfiguration（設定情報：appsettings.json等）
  - 期待出力: IServiceCollection（Options設定完了済み）
  - 処理内容:
    - IOptions<ConnectionConfig>注入設定
    - IOptions<TimeoutConfig>注入設定
    - IOptions<SystemResourcesConfig>注入設定
    - IOptions<LoggingConfig>注入設定
    - バリデーション設定（DataAnnotations）

- **TC188_OptionsConfigurator_ValidateOptions_設定値バリデーション**
  - 入力: IServiceCollection（DIコンテナ）
  - 期待出力: IServiceCollection（バリデーション設定完了済み）
  - 処理内容:
    - 必須項目チェック設定
    - 範囲値チェック設定
    - 形式チェック設定（IPアドレス、ポート番号等）

### 12.3 ServiceLifetimeManager テスト設計
**目的**: サービスライフタイム最適化、メモリ効率管理、パフォーマンス最適化をテスト

#### 正常系テスト
- **TC189_ServiceLifetimeManager_DetermineLifetime_ライフタイム判定**
  - 入力:
    - Type（対象クラス型）
    - ServiceRole（サービス役割：Core, Infrastructure, Async）
  - 期待出力: ServiceLifetime（Singleton, Scoped, Transient）
  - 処理内容:
    - Singleton判定条件：共有リソース、統計管理、システム監視クラス
    - Transient判定条件：PLC別、設定ファイル別、進捗報告クラス
    - パフォーマンス・メモリ効率考慮

- **TC190_ServiceLifetimeManager_ValidateLifetimeConsistency_ライフタイム整合性検証**
  - 入力: IServiceCollection（DIコンテナ）
  - 期待出力: ValidationResult（整合性検証結果）
  - 処理内容:
    - 依存関係ライフタイム検証
    - Singleton→Transient依存の警告
    - 循環依存チェック

### 12.4 MultiConfigDIIntegration テスト設計
**目的**: 複数設定ファイルとDIコンテナ統合、軽量インスタンス注入、設定別サービス解決をテスト

#### 正常系テスト
- **TC191_MultiConfigDIIntegration_RegisterMultiConfigServices_複数設定対応サービス登録**
  - 入力:
    - IServiceCollection（DIコンテナ）
    - MultiConfigManager（複数設定管理：事前初期化済み）
  - 期待出力: IServiceCollection（複数設定対応完了済み）
  - 処理内容:
    - 設定ファイル別ConfigToFrameManagerファクトリ登録
    - PLC別PlcCommunicationManagerファクトリ登録
    - 軽量インスタンス生成器登録

- **TC192_MultiConfigDIIntegration_CreateConfigSpecificProvider_設定別サービスプロバイダ作成**
  - 入力:
    - IServiceProvider（メインプロバイダ）
    - string（設定ファイル名："PLC1_settings.xlsx"）
  - 期待出力: IServiceProvider（設定専用プロバイダ）
  - 処理内容:
    - 設定ファイル専用スコープ作成
    - 該当設定値の注入
    - 軽量インスタンス解決

---

## 13. Program.cs・エントリーポイント設計 テスト設計

### 13.1 Program テスト設計
**目的**: アプリケーション起動制御、Host・DIコンテナ初期化、コマンドライン引数処理、設定ファイル初期読み込み、HostedService起動をテスト

#### 正常系テスト
- **TC193_Program_Main_正常起動終了**
  - 入力: string[] args（有効なコマンドライン引数）
  - 期待出力: int（終了コード：0=正常終了）
  - 処理内容:
    - コマンドライン引数解析
    - CreateHostBuilder()実行
    - Host.RunAsync()実行
    - 最上位例外処理

- **TC194_Program_CreateHostBuilder_Host構築**
  - 入力: string[] args（コマンドライン引数）
  - 期待出力: IHostBuilder（構築済みHostBuilder）
  - 処理内容:
    - Generic Host初期化
    - ConfigureServices()実行
    - ConfigureConfiguration()実行
    - ConfigureLogging()実行

- **TC195_Program_ConfigureServices_サービス設定**
  - 入力:
    - HostBuilderContext（Hostコンテキスト）
    - IServiceCollection（DIコンテナ）
  - 期待出力: void（サービス登録完了）
  - 処理内容:
    - DependencyInjectionConfigurator.ConfigureServices()実行
    - OptionsConfigurator.ConfigureOptions()実行
    - AndonHostedService登録

- **TC196_Program_ConfigureConfiguration_設定統合**
  - 入力:
    - HostBuilderContext（Hostコンテキスト）
    - IConfigurationBuilder（設定ビルダー）
  - 期待出力: void（設定読み込み完了）
  - 処理内容:
    - appsettings.json読み込み
    - 環境変数読み込み
    - コマンドライン引数読み込み
    - 設定優先順位設定（コマンドライン > 環境変数 > appsettings.json）

- **TC197_Program_ConfigureLogging_ログ設定**
  - 入力:
    - HostBuilderContext（Hostコンテキスト）
    - ILoggingBuilder（ログビルダー）
  - 期待出力: void（ログ設定完了）
  - 処理内容:
    - コンソールログ設定
    - ファイルログ設定
    - ログレベル設定
    - ログフォーマット設定

#### 異常系テスト
- **TC198_Program_Main_一般エラー終了**
  - 入力: 例外を発生する設定
  - 期待出力: int（終了コード：1=一般エラー）
  - 処理内容: 最上位例外キャッチと適切な終了コード返却

- **TC199_Program_Main_設定エラー終了**
  - 入力: 不正な設定ファイル
  - 期待出力: int（終了コード：2=設定エラー）

- **TC200_Program_Main_権限エラー終了**
  - 入力: 権限不足状態
  - 期待出力: int（終了コード：3=権限エラー）

### 13.2 CommandLineOptions テスト設計
**目的**: コマンドライン引数解析、起動オプション管理、バリデーション実行をテスト

#### 正常系テスト
- **TC201_CommandLineOptions_Parse_引数解析成功**
  - 入力: string[] args（"--config-dir ./config --log-level Debug --console"）
  - 期待出力: CommandLineOptions（解析済みオプション）
    - ConfigDirectory="./config"
    - LogLevel="Debug"
    - ConsoleOutput=true
    - ShowVersion=false
    - ShowHelp=false
    - DryRun=false

- **TC202_CommandLineOptions_Parse_バージョン表示**
  - 入力: string[] args（"--version"）
  - 期待出力: CommandLineOptions（ShowVersion=true）

- **TC203_CommandLineOptions_Parse_ヘルプ表示**
  - 入力: string[] args（"--help"）
  - 期待出力: CommandLineOptions（ShowHelp=true）

- **TC204_CommandLineOptions_Parse_ドライ実行**
  - 入力: string[] args（"--dry-run"）
  - 期待出力: CommandLineOptions（DryRun=true）

- **TC205_CommandLineOptions_Validate_オプションバリデーション成功**
  - 入力: 有効なCommandLineOptions
  - 期待出力: ValidationResult（IsValid=true）
  - 処理内容:
    - 設定ディレクトリ存在チェック
    - ログレベル形式チェック
    - オプション組み合わせチェック

#### 異常系テスト
- **TC206_CommandLineOptions_Parse_不正引数**
  - 入力: string[] args（"--invalid-option"）
  - 期待出力: ArgumentException

- **TC207_CommandLineOptions_Validate_設定ディレクトリ不存在**
  - 入力: 存在しないディレクトリを含むCommandLineOptions
  - 期待出力: ValidationResult（IsValid=false、エラー詳細）

### 13.3 ExitCodeManager テスト設計
**目的**: 終了コード管理、エラー分類、統一的な終了処理をテスト

#### 正常系テスト
- **TC208_ExitCodeManager_DetermineExitCode_終了コード判定**
  - 入力: Exception（各種例外タイプ）
  - 期待出力: ExitCode（適切な終了コード）
  - 検証項目:
    - FileNotFoundException → ConfigurationError（2）
    - SocketException → NetworkError（4）
    - UnauthorizedAccessException → PermissionError（3）
    - IOException → FileSystemError（5）
    - 一般Exception → GeneralError（1）

- **TC209_ExitCodeManager_LogExitInformation_終了情報ログ出力**
  - 入力:
    - ExitCode（終了コード）
    - Exception（例外：null許容）
    - TimeSpan（実行時間）
  - 期待出力: Task（ログ出力完了）
  - 処理内容:
    - 終了コード・理由ログ出力
    - 実行時間・統計ログ出力
    - エラー詳細ログ出力（エラー時のみ）

---

## テスト統計

### DIコンテナ設計
- **総テストケース数**: 11個 (TC182～TC192)
  - DependencyInjectionConfigurator: 5ケース
  - OptionsConfigurator: 2ケース
  - ServiceLifetimeManager: 2ケース
  - MultiConfigDIIntegration: 2ケース

### Program.cs・エントリーポイント
- **総テストケース数**: 17個 (TC193～TC209)
  - Program: 8ケース
  - CommandLineOptions: 7ケース
  - ExitCodeManager: 2ケース

### Part2合計
- **総テストケース数**: 28個
- **優先度**: 中（アーキテクチャ基盤完了後に実装）

---

## モック・スタブ実装方針

### テストデータ管理
- **設定ファイル**: テスト用appsettings.xlsx（正常系・異常系各種）
- **SLMPフレームサンプル**: 三菱電機公式仕様書準拠の実例データ
- **応答データ**: PLCシミュレータ用の既知応答パターン

### モック・スタブライブラリ
- **Moq**: インターフェースベースモック作成
- **xUnit/NUnit**: 単体テストフレームワーク
- **FluentAssertions**: テスト検証表現力向上

### DIコンテナテスト用モック設計
- **IServiceCollection/IServiceProvider**: DIコンテナ動作検証用モック
- **IConfiguration**: 設定情報注入テスト用スタブ
- **各種Managerインターフェース**: 依存性注入・解決テスト用モック

### Program.csテスト用モック設計
- **IHostBuilder/IHost**: Host構築・実行テスト用モック
- **コマンドライン引数**: 各種起動パターンのテストデータセット
- **環境変数**: 設定優先順位テスト用スタブ

---

## 実装優先順位
1. **ApplicationController・ExecutionOrchestrator** - 最優先（アプリケーション制御基盤）
2. **非同期・並行処理アーキテクチャ** - 高優先度（並行実行・例外処理基盤）
3. **DIコンテナ設計** - 中優先度（保守性・テスタビリティ基盤）
4. **Program.cs・エントリーポイント** - 中優先度（起動制御・統合点）
