# 各クラス・メソッド I/O情報一覧

このドキュメントは、C:\Users\1010821\Desktop\python\andon\documents\design\クラス設計.mdに記載されている各クラス/メソッドのInput/Output情報を整理したものです。

## 目次
- [ConfigToFrameManager](#configtoframemanager)
- [MultiConfigManager](#multiconfigmanager)
- [PlcCommunicationManager](#plccommunicationmanager)
- [DataOutputManager](#dataoutputmanager)
- [LoggingManager](#loggingmanager)
- [ErrorHandler](#errorhandler)
- [ResourceManager](#resourcemanager)
- [ApplicationController](#applicationcontroller)
- [ExecutionOrchestrator](#executionorchestrator)
- [AndonHostedService](#andonhostedservice)
- [TimerService](#timerservice)
- [GracefulShutdownHandler](#gracefulshutdownhandler)
- [AsyncExceptionHandler](#asyncexceptionhandler)
- [CancellationCoordinator](#cancellationcoordinator)
- [ResourceSemaphoreManager](#resourcesemaphoremanager)
- [ProgressReporter](#progressreporter)
- [ParallelExecutionController](#parallelexecutioncontroller)
- [DependencyInjectionConfigurator](#dependencyinjectionconfigurator)
- [OptionsConfigurator](#optionsconfigurator)
- [Program](#program)
- [MultiConfigDIIntegration](#multiconfigdiintegration)

---

## ConfigToFrameManager

### LoadConfigAsync（Step1: 単一設定ファイル読み込み）
**Input:**
- configFileName（string型、デフォルト："appsettings.xlsx"）
- 設定ソース（appsettings.xlsx, UTF-8：外部設定ファイルから取得）

**Output:**
- ConnectionConfig（IpAddress, Port, UseTcp, ConnectionType, IsBinary, FrameVersion）
- TimeoutConfig（ReceiveTimeoutMs, ConnectTimeoutMs）
- TargetDeviceConfig（MDeviceRange, DDeviceRange）
- MonitoringIntervalMs（データ収集周期）
- SystemResourcesConfig（MemoryLimitKB, MaxBufferSize, MemoryThresholdKB, LowMemoryMode）
- DataProcessingConfig（TargetName, ContinuousDataMode, DataRetentionDays）
- LoggingConfig（ConsoleOutput, DetailedLog）
- DataTransferConfig（EnableTransfer, DestinationIpAddress, DestinationPort）
- ActualConfigPath（string型、実際に読み込んだファイルパス）

**データ取得元:** 外部設定ファイル（configFileName指定, UTF-8形式）

**パス解決順序:** ./config/[fileName] → ./[fileName] → 環境変数ANDON_CONFIG_PATH

---

### LoadAllConfigsAsync（複数設定ファイル読み込み）
**Input:**
- configDirectory（string型、デフォルト："./config/"）
- filePattern（string型、デフォルト："*_settings.xlsx"）

**Output:**
- Dictionary<string, ConfigData>（ファイル名キーの設定データ辞書）
- LoadedConfigPaths（string[]型、実際に読み込んだファイルパス一覧）

**データ取得元:** 指定ディレクトリ内の複数設定ファイル（*_settings.xlsx, UTF-8形式）

**処理方式:** 共有データ領域への一括読み込み

---

### GetConfig（設定内容取得）
**Input:**
- 設定タイプ指定（ジェネリック型パラメータT、または設定種別指定）
- configFileName（string型、オプション：軽量インスタンス用）

**Output:**
- 指定設定オブジェクト（LoadConfigAsync/共有データから取得）
  - ConnectionConfig（IpAddress, Port, UseTcp, ConnectionType, IsBinary, FrameVersion）
  - TimeoutConfig（ReceiveTimeoutMs, ConnectTimeoutMs）
  - TargetDeviceConfig（MDeviceRange, DDeviceRange）
  - MonitoringIntervalMs（データ収集周期）
  - SystemResourcesConfig（MemoryLimitKB, MaxBufferSize, MemoryThresholdKB, LowMemoryMode）
  - DataProcessingConfig（TargetName, ContinuousDataMode, DataRetentionDays）
  - LoggingConfig（ConsoleOutput, DetailedLog）
  - DataTransferConfig（EnableTransfer, DestinationIpAddress, DestinationPort）

**データ取得元:** ConfigToFrameManager.LoadConfigAsync()（単一）/ SharedConfigData（複数）

---

### SplitDwordToWord（Step2-1: デバイスデータ前処理・DWord分割）
**Input:**
- TargetDeviceConfig（MDeviceRange, DDeviceRange, DataType：ConfigToFrameManager.LoadConfigAsync()から取得）

**Output:**
- 前処理済みデバイス要求情報（DWordは16bit×2に分割済み、最適化済み範囲情報）
- ProcessedDeviceRequestInfo（SplitRanges, DataTypeInfo, OptimizedRanges）

**データ取得元:** ConfigToFrameManager.LoadConfigAsync()（デバイス設定）

---

### BuildFrames（Step2-2: 通信フレーム構築）
**Input:**
- ConnectionConfig（IpAddress, Port, UseTcp, ConnectionType, IsBinary, FrameVersion：ConfigToFrameManager.LoadConfigAsync()から取得）
- ProcessedDeviceRequestInfo（前処理済みデバイス要求情報：ConfigToFrameManager.SplitDwordToWord()から取得）

**Output:**
- 設定値から生成したインスタンス（SLMPフレーム、16進数文字列、複数フレーム対応）
- Mデバイス用: "54001234000000010401006400000090E8030000"
  - 構成: サブヘッダ(54001234000000) + READコマンド(0401) + サブコマンド(0100:ビット単位) + デバイスコード(6400:M機器) + 開始番号(00000090:M000) + デバイス点数(E80300:1000点)
- Dデバイス用: "54001234000000010400A800000090E8030000"
  - 構成: サブヘッダ(54001234000000) + READコマンド(0401) + サブコマンド(0000:ワード単位) + デバイスコード(A800:D機器) + 開始番号(00000090:D000) + デバイス点数(E80300:1000点)

**構築コマンド:** READコマンド(0401)フレーム（2回送信方式）

**対象機器:** ビット(M機器)、ワード(D機器)をそれぞれ独立したフレームで取得

**データ取得元:** ConfigToFrameManager.LoadConfigAsync()（接続設定）、ConfigToFrameManager.SplitDwordToWord()（前処理済みデバイス要求情報）

---

## MultiConfigManager

### LoadAllFromDirectoryAsync（複数設定ファイル一括読み込み）
**Input:**
- configDirectory（string型、デフォルト："./config/"）
- filePattern（string型、デフォルト："*_settings.xlsx"）
- allowPartialSuccess（bool型、デフォルト：true）

**Output:**
- Dictionary<string, ConfigToFrameManager>（ファイル名キーの軽量インスタンス辞書）
- LoadResult（LoadedFiles, FailedFiles, TotalLoadTime）

**データ取得元:** 指定ディレクトリ内の複数設定ファイル（*_settings.xlsx, UTF-8形式）

**処理方式:** 設定データを静的共有領域に保存、各ファイル用の軽量インスタンス生成

---

### CreateManagersAsync（軽量インスタンス生成）
**Input:**
- configFileNames（string[]型：対象設定ファイル名一覧）

**Output:**
- ConfigToFrameManager[]（軽量インスタンス配列）

**データ取得元:** SharedConfigData（静的共有領域）

**動作:** 共有データを参照する軽量インスタンスを効率的に生成

---

### GetSharedConfigData（共有データアクセス）
**Input:**
- configFileName（string型：対象設定ファイル名）

**Output:**
- ConfigDataSet（指定ファイルの設定データ）

**データ取得元:** SharedConfigData（静的共有領域）

**メモリ効率:** 設定データ実体の共有によりメモリ使用量最小化

---

### ReleaseSharedData（共有データ解放）
**Input:**
- configFileName（string型、オプション：特定ファイル or 全体）

**Output:**
- ReleasedMemoryKB（解放されたメモリ量）

**動作:** 明示的なメモリ解放（長時間稼働時の最適化）

---

## PlcCommunicationManager

### ConnectAsync（Step3: PLC接続処理）
**Input:**
- ConnectionConfig（IpAddress, Port, UseTcp, ConnectionType：ConfigToFrameManager.LoadConfigAsync()から取得）
- TimeoutConfig（ConnectTimeoutMs：ConfigToFrameManager.LoadConfigAsync()から取得）

**Output:**
- ConnectionResponse（接続処理結果オブジェクト）
  - Status（ConnectionStatus型、必須）: Connected/Failed/Timeout
  - Socket（System.Net.Sockets.Socket型、null許容）: 実際の通信用ソケット（成功時のみ）
  - RemoteEndPoint（System.Net.EndPoint型、null許容）: 接続先情報（成功時のみ）
  - ConnectedAt（DateTime型、null許容）: 接続完了時刻（成功時のみ）
  - ConnectionTime（TimeSpan型、null許容）: 接続処理にかかった時間（成功時のみ）
  - ErrorMessage（string型、null許容）: エラーメッセージ（失敗時のみ）

---

### SendFrameAsync（Step4: PLCへのリクエスト送信）
**Input:**
- 設定値から生成したインスタンス（SLMPフレーム、16進数文字列：ConfigToFrameManager.BuildFrames()から取得）
- Mデバイス用: "54001234000000010401006400000090E8030000"
  - 構成: サブヘッダ(54001234000000) + READコマンド(0401) + サブコマンド(0100:ビット単位) + デバイスコード(6400:M機器) + 開始番号(00000090:M000) + デバイス点数(E80300:1000点)
- Dデバイス用: "54001234000000010400A800000090E8030000"
  - 構成: サブヘッダ(54001234000000) + READコマンド(0401) + サブコマンド(0000:ワード単位) + デバイスコード(A800:D機器) + 開始番号(00000090:D000) + デバイス点数(E80300:1000点)

**使用コマンド:** READコマンド(0401)（2回送信）

**取得対象:** ビット(M機器)、ワード(D機器)をそれぞれ独立して取得

**参考資料:** C:\Users\1010821\Desktop\python\andon\pdf2img（SLMP仕様書）

---

### ReceiveResponseAsync（Step4: PLCからのデータ受信）
**Input:**
- TimeoutConfig（ReceiveTimeoutMs：ConfigToFrameManager.LoadConfigAsync()から取得）

**Output:**
- 各種PLCの状態/生データ(16進数)
- READコマンド(0401)レスポンス：ビット・ワード・ダブルワードデータの一括受信
- 受信データ形式：SLMPレスポンスフレーム（ヘッダー + データ部）

---

### DisconnectAsync（Step5: PLC切断処理）
**Input:**
- 切断/リソース管理情報（PlcCommunicationManager.ConnectAsync()からの接続状態、通信統計情報）

**Output:**
- ConnectionStats（通信統計情報オブジェクト）
  - 基本統計: 接続時間、送受信フレーム数・バイト数、切断時刻
  - 応答時間統計: 履歴・平均・最大・最小応答時間
  - エラー・品質統計: エラー回数、リトライ回数、通信成功率

---

### PostprocessReceivedData（Step6-1: 受信データ後処理・DWord結合）
**Input:**
- Step4で受信した生データ(16進数：PlcCommunicationManager.ReceiveResponseAsync()から取得)
- ProcessedDeviceRequestInfo（送信時の前処理情報：ConfigToFrameManager.SplitDwordToWord()から取得）

**Output:**
- ProcessedResponseData（受信データ後処理結果オブジェクト）
  - 基本結果: 元生データ、処理済みデータ（デバイス名キー構造）、DWord結合フラグ、処理時刻
  - エラー情報: エラーフラグ、エラー・警告メッセージリスト
  - 統計情報: 処理デバイス数、DWord結合数（自動計算）

**処理対象:** READコマンド(0401)レスポンスデータ（ビット・ワード・ダブルワード混在データ）

**データ取得元:** PlcCommunicationManager.ReceiveResponseAsync()（受信生データ）、ConfigToFrameManager.SplitDwordToWord()（送信時前処理情報）

---

### ParseRawToStructuredData（Step6-2: 構造化データ変換）
**Input:**
- 後処理済み受信データ（PostprocessedRawData：PlcCommunicationManager.PostprocessReceivedData()から取得）

**Output:**
- StructuredData（SLMP構造化解析結果オブジェクト）
  - 基本構造化データ: SLMPヘッダー（全標準情報）、終了コード、デバイスデータ、受信時刻、エラーフラグ
  - 解析詳細情報: 解析手順記録、解釈情報、処理時間、デバイス解釈、ステータス判定
  - エラー詳細情報: 詳細エラーコード、エラー説明、影響デバイス（エラー時のみ）

**データ取得元:** PlcCommunicationManager.PostprocessReceivedData()（後処理済み受信データ）

---

## DataOutputManager

### OutputDataAsync（解析後データ出力）
**Input:**
- 構造化データ（PlcCommunicationManager.ParseRawToStructuredData()の出力）
- 出力設定（FilePath, Format）

**Output:**
- データが指定場所/形式で出力/保存される

**動作フロー成功条件:** 解析したデータを任意の場所/形式に出力/保存できる

**データ取得元:** PlcCommunicationManager.ParseRawToStructuredData()（構造化解析結果）

---

## LoggingManager

### InitializeAsync（ログシステム初期化）
**Input:**
- LoggingConfig（ConsoleOutput, DetailedLog設定）

**Output:**
- 初期化状態（ファイル作成等）

---

### SetCorrelationId（セッション関連付けID設定）
**Input:**
- SessionId（処理セッション識別子）

**Output:**
- 関連付け設定完了状態

---

### SetLogLevel（ログレベル設定）
**Input:**
- LogLevel（出力レベル指定）

**Output:**
- レベル設定完了状態

---

### IsLogLevelEnabled（ログレベル有効性確認）
**Input:**
- LogLevel（確認対象レベル）

**Output:**
- 有効性判定結果（true/false）

---

### LogConfigAsync（設定情報詳細出力）
**Input:**
- 全設定オブジェクト（ConnectionConfig, TimeoutConfig, TargetDeviceConfig, SystemResourcesConfig, DataProcessingConfig等）

**Output:**
- 設定情報ログ出力完了状態

**出力先:** ファイル、コンソール

---

### LogCommunicationAsync（通信/送受信データ情報出力）
**Input:**
- SLMPフレーム（送信/受信）
- フレーム解析結果（ヘッダー、終了コード、データ部）
- 通信統計（応答時間、成功/失敗）

**Output:**
- 通信ログ出力完了状態

**出力先:** ファイル、コンソール

---

### LogStateAsync（アプリケーション状態出力）
**Input:**
- セッション情報（開始/終了、プロセスID、実行時間）
- サイクル情報（サイクル番号、フェーズ、間隔）
- 処理状況（リアルタイムメッセージ、状態変化）

**Output:**
- 状態ログ出力完了状態

**出力先:** ファイル、コンソール

---

### LogMetricAsync（統計/パフォーマンス情報出力）
**Input:**
- 実行統計（サイクル数、成功率、失敗率）
- 応答時間分析（平均、最大、最小、分布）
- システム稼働状況（リソース使用量、パフォーマンス）

**Output:**
- 統計ログ出力完了状態

**出力先:** ファイル、コンソール

---

### LogErrorAsync（エラー/例外情報詳細出力）
**Input:**
- エラー分類（通信エラー、タイムアウト、設定エラー等）
- エラー詳細（例外オブジェクト、スタックトレース）
- 回復処理結果（リトライ結果、自動回復状況）

**Output:**
- エラーログ出力完了状態

**出力先:** ファイル、コンソール

---

### LogDeviceDataAsync（デバイス解釈情報出力）
**Input:**
- 生データ（バイナリ、16進数、数値表現）
- 解釈結果（人間が読める形式への変換）
- ステータス判定（ON/OFF、正常/異常等）

**Output:**
- デバイスデータログ出力完了状態

**出力先:** ファイル、コンソール

---

### BeginTransaction（トランザクション開始）
**Input:**
- TransactionId（トランザクション識別子）

**Output:**
- トランザクション開始状態

---

### EndTransaction（トランザクション終了）
**Input:**
- TransactionId（トランザクション識別子）
- TransactionResult（成功/失敗結果）

**Output:**
- トランザクション終了状態

---

### FlushAsync（バッファフラッシュ）
**Input:**
- FlushTarget（ファイル指定）

**Output:**
- フラッシュ完了状態

---

## ErrorHandler

### DetermineErrorCategory（エラー分類判定）
**Input:**
- Exception（発生したエラー・例外オブジェクト）
- StepNumber（Step1-7のステップ番号：実行中のクラス/メソッドから推定）

**Output:**
- ErrorCategory（ConfigurationError, CommunicationError, DataProcessingError, ResourceError, SystemError）
- Severity（Info, Warning, Error, Fatal）

---

### RecordError（エラー記録）
**Input:**
- ErrorCategory（エラー分類）
- Severity（重要度）
- ErrorMessage（エラーメッセージ）
- Exception（例外オブジェクト）
- StepNumber（ステップ番号：実行中のクラス/メソッドから推定）

**Output:**
- 記録完了状態

---

### ApplyErrorPolicy（エラー処理方針適用）
**Input:**
- ErrorCategory（エラー分類）
- StepNumber（ステップ番号：実行中のクラス/メソッドから推定）

**Output:**
- ErrorAction（継続/終了の判定結果）

---

### ApplyRetryPolicy（リトライ方針適用）
**Input:**
- ErrorCategory（エラー分類）
- StepNumber（ステップ番号：実行中のクラス/メソッドから推定）
- CurrentRetryCount（現在のリトライ回数）

**Output:**
- ShouldRetry（リトライするかどうかの判定）
- MaxRetryCount（最大リトライ回数）

---

### GetErrorStatistics（エラー統計取得）
**Input:**
- 統計期間指定（TimeSpan型、オプション：全期間または特定期間）
- エラー分類フィルター（ErrorCategory型、オプション：特定分類のみ）

**Output:**
- ErrorStatistics（エラー統計オブジェクト）
  - TotalErrorCount（総エラー発生数）
  - ErrorsByCategory（カテゴリ別エラー数）
  - ErrorsBySeverity（重要度別エラー数）
  - MostFrequentErrors（頻出エラー一覧）
  - ErrorRate（エラー発生率）
  - RecoveryRate（自動回復率）

**データ取得元:** ErrorHandler.RecordError()（エラー記録履歴）

---

## ResourceManager

### GetMemoryUsage（メモリ使用量取得）
**Input:**
- システム状態（現在のメモリ使用状況）

**Output:**
- 現在のメモリ使用量（KB単位）
- 各コンポーネント別使用量

**データ取得元:** システムAPI（GC.GetTotalMemory(), Process.GetCurrentProcess().WorkingSet64等）、各クラスの内部状態

---

### EvaluateLevel（メモリレベル判定）
**Input:**
- 現在メモリ使用量（ResourceManager.GetMemoryUsage()）
- 閾値設定（ConfigToFrameManager.LoadConfigAsync()）

**Output:**
- メモリレベル（Normal, Warning, Critical）
- 推奨アクション

**データ取得元:** ResourceManager.GetMemoryUsage()（現在使用量）、ConfigToFrameManager.LoadConfigAsync()（閾値設定）

---

### ApplyDataAndLoggingPolicy（データ/ログポリシー適用）
**Input:**
- メモリレベル（ResourceManager.EvaluateLevel()）
- ポリシー設定（ConfigToFrameManager.LoadConfigAsync()）

**Output:**
- 適用されたポリシー内容
- データ処理制限設定

**データ取得元:** ResourceManager.EvaluateLevel()（レベル判定結果）、ConfigToFrameManager.LoadConfigAsync()（システムリソース設定）

---

### OptimizeMemory（メモリ最適化実行）
**Input:**
- 最適化対象（各クラスインスタンス）
- 最適化レベル（ResourceManager.EvaluateLevel()）

**Output:**
- 最適化実行結果
- 削減されたメモリ量

**データ取得元:** PlcCommunicationManager, ConfigToFrameManager, LoggingManager各メソッド（メモリ解放対象）

---

### WriteLogAsync（メモリ状況ログ出力）
**Input:**
- メモリ使用状況（ResourceManager.GetMemoryUsage()）
- メモリレベル（ResourceManager.EvaluateLevel()）
- 最適化結果（ResourceManager.OptimizeMemory()）

**Output:**
- ログ出力完了状態

**データ取得元:** ResourceManager各メソッド（メモリ状況）、LoggingManager.LogStateAsync()（ログ出力機能）

---

### RunMonitoringLoopAsync（メモリ監視ループ実行）
**Input:**
- 監視間隔設定（ConfigToFrameManager.LoadConfigAsync()）
- 継続監視フラグ

**Output:**
- 監視ループ実行状態

**データ取得元:** ConfigToFrameManager.LoadConfigAsync()（監視設定）、ResourceManager各メソッド（監視対象メソッド）

---

## ApplicationController

### StartAsync（アプリケーション開始処理）
**Input:**
- CancellationToken（キャンセレーション制御：Program.cs、GracefulShutdownHandlerから取得）

**Output:**
- Task（非同期実行完了状態）

**処理内容:**
- Step1初期化フェーズ実行（ExecuteStep1InitializationAsync()）
- Step2-7継続実行開始（StartContinuousDataCycleAsync()）

**データ取得元:** IServiceProvider（DIコンテナ）、IHostedService（.NETランタイム）

---

### StopAsync（アプリケーション停止処理）
**Input:**
- CancellationToken（停止制御：GracefulShutdownHandlerから取得）

**Output:**
- Task（非同期停止完了状態）

**処理内容:**
- 実行中サイクルの適切な停止
- 各Managerクラスのリソース解放
- PLC接続の適切な切断

**データ取得元:** ExecutionOrchestrator（実行状態）、各Managerクラス（リソース状態）

---

### ExecuteStep1InitializationAsync（Step1初期化フェーズ実行）
**Input:**
- 設定ディレクトリパス（string型：Program.csから取得、デフォルト："./config/"）

**Output:**
- InitializationResult（初期化結果オブジェクト）
  - LoadedConfigCount（読み込み設定ファイル数）
  - CreatedManagersCount（作成されたManagerインスタンス数）
  - InitializationTime（初期化処理時間）
  - IsSuccess（初期化成功フラグ）
  - ErrorDetails（初期化エラー詳細：失敗時のみ）

**処理内容:**
- 複数設定ファイル読み込み（MultiConfigManager.LoadAllFromDirectoryAsync()）
- 各種Managerクラスインスタンス作成
- 初期化検証・ヘルスチェック

**データ取得元:** MultiConfigManager（複数設定ファイル管理）、DIコンテナ（各Managerインスタンス）

---

### StartContinuousDataCycleAsync（Step2-7継続実行開始）
**Input:**
- InitializationResult（Step1初期化結果：ExecuteStep1InitializationAsync()から取得）
- CancellationToken（実行制御：StartAsync()から取得）

**Output:**
- Task（継続実行タスク）

**処理内容:**
- ExecutionOrchestratorインスタンス作成
- 複数PLC並行実行開始（Task.WhenAll使用）
- エラー発生時の継続処理制御

**データ取得元:** ExecutionOrchestrator（実行制御）、MultiConfigManager（PLC設定情報）

---

## ExecutionOrchestrator

### RunContinuousDataCycleAsync（継続データサイクル実行）
**Input:**
- ConfigToFrameManager（PLC用設定・フレーム管理：ApplicationController.ExecuteStep1InitializationAsync()から取得）
- PlcIdentifier（PLC識別子：string型、並行実行時の識別用）
- CancellationToken（実行制御：ApplicationController.StartContinuousDataCycleAsync()から取得）

**Output:**
- Task（継続実行タスク：エラー発生またはキャンセル時まで継続）

**処理内容:**
- TimerService使用によるMonitoringIntervalMs間隔制御
- ExecuteSingleCycleAsync()の繰り返し実行
- エラー発生時の継続判定・ログ出力

**データ取得元:** TimerService（間隔制御）、ConfigToFrameManager（MonitoringIntervalMs設定）

---

### ExecuteSingleCycleAsync（単一サイクル実行）
**Input:**
- ConfigToFrameManager（設定・フレーム管理）
- PlcCommunicationManager（PLC通信管理）
- DataOutputManager（データ出力管理）
- LoggingManager（ログ管理）

**Output:**
- CycleExecutionResult（サイクル実行結果オブジェクト）
  - IsSuccess（サイクル成功フラグ）
  - ExecutedSteps（実行完了ステップ一覧：Step2, Step3, ..., Step7）
  - ExecutionTime（サイクル実行時間）
  - DataCount（処理データ数）
  - ErrorDetails（エラー詳細：失敗時のみ）

**処理内容:**
- Step2: ConfigToFrameManager.SplitDwordToWord() → ConfigToFrameManager.BuildFrames()
- Step3: PlcCommunicationManager.ConnectAsync()
- Step4: PlcCommunicationManager.SendFrameAsync() → PlcCommunicationManager.ReceiveResponseAsync()
- Step5: PlcCommunicationManager.DisconnectAsync()
- Step6: PlcCommunicationManager.PostprocessReceivedData() → PlcCommunicationManager.ParseRawToStructuredData()
- Step7: DataOutputManager.OutputDataAsync()

**データ取得元:** 各Managerクラス（Step2-7の各メソッド）

---

### GetMonitoringInterval（監視間隔取得）
**Input:**
- ConfigToFrameManager（設定管理：RunContinuousDataCycleAsync()から取得）

**Output:**
- TimeSpan（監視間隔：MonitoringIntervalMs設定値をTimeSpanに変換）

**データ取得元:** ConfigToFrameManager.GetConfig<DataProcessingConfig>()（MonitoringIntervalMs設定）

---

## AndonHostedService

### ExecuteAsync（バックグラウンド実行メイン処理）
**Input:**
- CancellationToken（.NETランタイムから取得）

**Output:**
- Task（バックグラウンド実行タスク）

**処理内容:**
- ApplicationController.StartAsync()実行
- CancellationToken監視による適切な終了制御

**データ取得元:** ApplicationController（DIで注入）、.NETランタイム（CancellationToken）

---

### StartAsync（HostedService開始処理）
**Input:**
- CancellationToken（.NETランタイムから取得）

**Output:**
- Task（開始処理完了状態）

**処理内容:**
- 起動ログ出力
- ApplicationController初期化確認

**データ取得元:** LoggingManager（ログ出力）、ApplicationController（初期化状態）

---

### StopAsync（HostedService停止処理）
**Input:**
- CancellationToken（.NETランタイムから取得）

**Output:**
- Task（停止処理完了状態）

**処理内容:**
- ApplicationController.StopAsync()実行
- 停止ログ出力

**データ取得元:** ApplicationController（停止処理）、LoggingManager（ログ出力）

---

## TimerService

### StartPeriodicExecution（周期実行開始）
**Input:**
- Func<Task>（実行する非同期処理：ExecutionOrchestrator.ExecuteSingleCycleAsync()）
- TimeSpan（実行間隔：ExecutionOrchestrator.GetMonitoringInterval()から取得）
- CancellationToken（実行制御）

**Output:**
- Task（周期実行タスク）

**処理内容:**
- System.Threading.PeriodicTimer使用
- 指定間隔での正確な実行制御
- 前回処理未完了時の重複実行防止

**データ取得元:** PeriodicTimer（.NET標準）、実行対象メソッド

---

## GracefulShutdownHandler

### RegisterShutdownHandlers（終了ハンドラ登録）
**Input:**
- ApplicationController（制御対象：DIで注入）
- CancellationTokenSource（キャンセレーション制御）

**Output:**
- 登録完了状態

**処理内容:**
- Console.CancelKeyPress登録
- AppDomain.ProcessExit登録
- キャンセレーショントークン発行

**データ取得元:** .NETランタイム（シグナル）、ApplicationController（停止処理）

---

### ExecuteGracefulShutdown（適切な終了処理実行）
**Input:**
- ApplicationController（制御対象）
- TimeSpan（タイムアウト時間：デフォルト30秒）

**Output:**
- ShutdownResult（終了処理結果）

**処理内容:**
- ApplicationController.StopAsync()実行
- 各Managerクラスのリソース解放確認
- タイムアウト制御

**データ取得元:** ApplicationController（停止状態）、各Managerクラス（リソース状態）

---

## AsyncExceptionHandler

### HandleCriticalOperationAsync<T>（重要処理用例外ハンドリング）
**Input:**
- Func<Task<T>>（実行対象の重要処理：PlcCommunicationManager各メソッド等）
- string（処理名称：エラーログ識別用）
- CancellationToken（キャンセル制御）

**Output:**
- AsyncOperationResult<T>（実行結果オブジェクト）
  - IsSuccess（bool型、必須）: 実行成功フラグ
  - Result（T型、null許容）: 実行結果（成功時のみ設定）
  - Exception（Exception型、null許容）: 発生例外（失敗時のみ設定）
  - ExecutionTime（TimeSpan型、必須）: 実行時間
  - OperationName（string型、必須）: 処理名称

**処理内容:**
- 個別try-catch実行
- 詳細例外ログ出力
- コンテキスト情報保持
- ErrorHandler.RecordError()連携

**データ取得元:** 実行対象メソッド、LoggingManager.LogErrorAsync()（詳細ログ）、ErrorHandler（エラー分類・記録）

---

### HandleGeneralOperationsAsync（一般処理用一括例外ハンドリング）
**Input:**
- IEnumerable<Func<Task>>（実行対象の一般処理群：LoggingManager各メソッド等）
- string（処理グループ名称）
- CancellationToken（キャンセル制御）

**Output:**
- GeneralOperationResult（一括実行結果オブジェクト）
  - SuccessCount（int型、必須）: 成功処理数
  - FailureCount（int型、必須）: 失敗処理数
  - TotalExecutionTime（TimeSpan型、必須）: 全体実行時間
  - FailedOperations（List<string>型、必須）: 失敗した処理名一覧
  - Exceptions（List<Exception>型、必須）: 発生例外一覧

**処理内容:**
- Task.WhenAll()での一括実行
- 失敗処理の継続実行
- 統合例外ログ出力

**データ取得元:** 実行対象メソッド群、LoggingManager.LogErrorAsync()（統合ログ）

---

## CancellationCoordinator

### CreateHierarchicalToken（階層キャンセレーショントークン作成）
**Input:**
- CancellationToken（親トークン：Program.cs、GracefulShutdownHandlerから取得）
- TimeSpan（タイムアウト時間：オプション、処理別タイムアウト設定）

**Output:**
- CancellationTokenSource（子トークンソース）

**処理内容:**
- 親トークンとタイムアウトの組み合わせ
- 階層的キャンセル制御

**データ取得元:** 親CancellationToken、ConfigToFrameManager（タイムアウト設定）

---

### RegisterCancellationCallback（キャンセル時コールバック登録）
**Input:**
- CancellationToken（対象トークン）
- Func<Task>（キャンセル時実行処理：リソース解放、接続切断等）
- string（コールバック名称）

**Output:**
- CancellationTokenRegistration（登録ハンドル）

**処理内容:**
- キャンセル時の適切な清掃処理登録
- 非同期コールバック対応

**データ取得元:** PlcCommunicationManager.DisconnectAsync()、LoggingManager.FlushAsync()等

---

## ResourceSemaphoreManager

### ExecuteWithSemaphoreAsync<T>（セマフォ制御付き実行）
**Input:**
- SemaphoreSlim（対象セマフォ：LogFileSemaphore、ConfigFileSemaphore、OutputFileSemaphore）
- Func<Task<T>>（実行対象処理）
- CancellationToken（キャンセル制御）
- TimeSpan（セマフォ取得タイムアウト：デフォルト30秒）

**Output:**
- T（実行結果）

**処理内容:**
- セマフォ取得→処理実行→セマフォ解放の確実実行
- タイムアウト制御
- 例外発生時の確実なセマフォ解放

**データ取得元:** 実行対象メソッド、SemaphoreSlim.WaitAsync()

---

### GetResourceSemaphore（リソース種別セマフォ取得）
**Input:**
- ResourceType（リソース種別列挙型：LogFile, ConfigFile, OutputFile）

**Output:**
- SemaphoreSlim（対応するセマフォ）

**データ取得元:** 内部セマフォインスタンス

---

## ProgressReporter

### Report（進捗報告実行）
**Input:**
- T（進捗情報：ProgressInfo型またはstring型）

**Output:**
- void（進捗情報の出力・通知）

**処理内容:**
- コンソール出力（リアルタイム表示）
- ログファイル記録
- 進捗率計算・表示

**データ取得元:** LoggingManager.LogStateAsync()（ログ出力）、Console.WriteLine()（コンソール表示）

---

### CreateStepProgress（ステップ別進捗レポーター作成）
**Input:**
- string（ステップ名：Step2, Step3, ...）
- int（予想処理数：デバイス数等）

**Output:**
- ProgressReporter<ProgressInfo>（ステップ専用進捗レポーター）

**データ取得元:** ステップ名、処理対象数

---

## ParallelExecutionController

### ExecuteParallelPlcOperationsAsync（複数PLC並行実行）
**Input:**
- IEnumerable<ConfigToFrameManager>（PLC用設定管理インスタンス群：MultiConfigManager.CreateManagersAsync()から取得）
- Func<ConfigToFrameManager, CancellationToken, Task<CycleExecutionResult>>（実行処理：ExecutionOrchestrator.RunContinuousDataCycleAsync()）
- CancellationToken（実行制御）

**Output:**
- ParallelExecutionResult（並行実行結果オブジェクト）
  - TotalPlcCount（int型、必須）: 対象PLC総数
  - SuccessfulPlcCount（int型、必須）: 成功PLC数
  - FailedPlcCount（int型、必須）: 失敗PLC数
  - PlcResults（Dictionary<string, CycleExecutionResult>型、必須）: PLC別実行結果
  - OverallExecutionTime（TimeSpan型、必須）: 全体実行時間
  - ContinuingPlcIds（List<string>型、必須）: 継続実行中PLC ID一覧

**処理内容:**
- Task.WhenAll()による真の並行実行
- エラー発生PLC以外の継続実行制御
- 並行実行統計・監視情報取得

**データ取得元:** 各ConfigToFrameManager、ExecutionOrchestrator、Task.WhenAll()

---

### MonitorParallelExecution（並行実行監視）
**Input:**
- IEnumerable<Task<CycleExecutionResult>>（実行中タスク群）
- IProgress<ParallelProgressInfo>（進捗レポーター）
- CancellationToken（監視制御）

**Output:**
- Task（監視タスク）

**処理内容:**
- 実行中タスクの状態監視
- 完了・エラー・継続状況のリアルタイム報告
- 全体進捗率計算・表示

**データ取得元:** Task.IsCompleted、Task.IsFaulted、ProgressReporter

---

## DependencyInjectionConfigurator

### ConfigureServices（サービス登録と構成）
**Input:**
- IServiceCollection（.NETランタイムから取得）
- IConfiguration（appsettings.json等の設定：.NETランタイムから取得）

**Output:**
- IServiceCollection（登録完了したサービスコレクション）

**処理内容:**
- RegisterCoreServices()実行
- RegisterInfrastructureServices()実行
- RegisterAsyncServices()実行
- サービスライフタイム設定（Singleton, Scoped, Transient）

**データ取得元:** IConfiguration、各サービスクラス定義

---

### RegisterCoreServices（コアサービス登録）
**Input:**
- IServiceCollection（サービスコレクション）

**Output:**
- void（コアサービス登録完了）

**処理内容:**
- ApplicationController登録（Singleton）
- ExecutionOrchestrator登録（Scoped）
- ConfigToFrameManager登録（Scoped）
- PlcCommunicationManager登録（Scoped）
- 各種インターフェースと実装のマッピング

**データ取得元:** 各コアサービスクラス定義

---

### RegisterInfrastructureServices（インフラストラクチャサービス登録）
**Input:**
- IServiceCollection（サービスコレクション）

**Output:**
- void（インフラサービス登録完了）

**処理内容:**
- LoggingManager登録（Singleton）
- ErrorHandler登録（Singleton）
- ResourceManager登録（Singleton）
- DataOutputManager登録（Singleton）
- TimerService登録（Singleton）

**データ取得元:** 各インフラサービスクラス定義

---

### RegisterAsyncServices（非同期処理サービス登録）
**Input:**
- IServiceCollection（サービスコレクション）

**Output:**
- void（非同期サービス登録完了）

**処理内容:**
- AsyncExceptionHandler登録（Singleton）
- CancellationCoordinator登録（Singleton）
- ResourceSemaphoreManager登録（Singleton）
- ParallelExecutionController登録（Singleton）
- ProgressReporter登録（Transient）

**データ取得元:** 各非同期処理サービスクラス定義

---

## OptionsConfigurator

### ConfigureOptions（Options構成とインジェクション）
**Input:**
- IServiceCollection（サービスコレクション）
- IConfiguration（appsettings.json等の設定）

**Output:**
- void（Options構成完了）

**処理内容:**
- ConnectionConfig設定バインド
- TimeoutConfig設定バインド
- TargetDeviceConfig設定バインド
- SystemResourcesConfig設定バインド
- DataProcessingConfig設定バインド
- LoggingConfig設定バインド
- DataTransferConfig設定バインド
- IOptions<T>、IOptionsSnapshot<T>としてDI可能に設定

**データ取得元:** IConfiguration、各設定クラス定義

---

### ValidateOptions（設定値検証）
**Input:**
- 各設定オブジェクト（ConnectionConfig, TimeoutConfig, TargetDeviceConfig等）

**Output:**
- ValidationResult（検証結果）
  - IsValid（検証成功フラグ）
  - ValidationErrors（検証エラー一覧）

**処理内容:**
- 必須項目存在チェック
- 値範囲チェック（ポート番号、タイムアウト値等）
- 論理整合性チェック（IpAddressとPortの組み合わせ等）
- データアノテーション検証

**データ取得元:** 各設定オブジェクト

---

## Program

### Main（アプリケーションエントリーポイント）
**Input:**
- string[] args（コマンドライン引数）

**Output:**
- int（終了コード：0=正常終了、1=異常終了）

**処理内容:**
- CreateHostBuilder()実行
- ホストビルド・実行
- グローバル例外ハンドリング
- 終了コード判定

**データ取得元:** .NETランタイム、コマンドライン引数

---

### CreateHostBuilder（ホスト構築）
**Input:**
- string[] args（コマンドライン引数）

**Output:**
- IHostBuilder（構築されたホストビルダー）

**処理内容:**
- Host.CreateDefaultBuilder()実行
- ConfigureServices()設定
- ログ設定構成
- アプリケーション設定読み込み

**データ取得元:** .NETランタイム、appsettings.json

---

### ConfigureServices（サービス構成）
**Input:**
- HostBuilderContext（ホストコンテキスト）
- IServiceCollection（サービスコレクション）

**Output:**
- void（サービス構成完了）

**処理内容:**
- DependencyInjectionConfigurator.ConfigureServices()実行
- OptionsConfigurator.ConfigureOptions()実行
- AndonHostedService登録
- GracefulShutdownHandler初期化

**データ取得元:** HostBuilderContext、DependencyInjectionConfigurator、OptionsConfigurator

---

## MultiConfigDIIntegration

### RegisterMultiConfigServices（複数設定対応サービス登録）
**Input:**
- IServiceCollection（サービスコレクション）
- string[]（設定ファイル名一覧）

**Output:**
- void（複数設定対応サービス登録完了）

**処理内容:**
- MultiConfigManager登録（Singleton）
- 設定ファイル別ConfigToFrameManager登録（Named Instance）
- ファクトリーパターン実装
- 設定ファイル識別子によるサービス解決

**データ取得元:** MultiConfigManager、設定ファイル情報

---

### CreateConfigSpecificProvider（設定ファイル固有サービスプロバイダー作成）
**Input:**
- IServiceProvider（ルートサービスプロバイダー）
- string（設定ファイル名）

**Output:**
- IServiceProvider（設定ファイル固有サービスプロバイダー）

**処理内容:**
- スコープドサービスプロバイダー作成
- 設定ファイル固有ConfigToFrameManager解決
- 設定ファイル固有PlcCommunicationManager作成
- 依存サービス解決

**データ取得元:** ルートサービスプロバイダー、MultiConfigManager

---

## 参照元ドキュメント

このドキュメントは以下のドキュメントから情報を抽出しています：
- C:\Users\1010821\Desktop\python\andon\documents\design\クラス設計.md
- C:\Users\1010821\Desktop\python\andon\documents\design\クラス・メソッドリスト.md

最終更新日: 2025-11-03
