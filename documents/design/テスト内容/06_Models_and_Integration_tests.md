# Modelsクラス・統合・パフォーマンス テスト設計

## テスト方針
- **TDD手法**を使用してテスト駆動開発を実施
- Modelsクラス単体テスト、統合テスト、パフォーマンステストを包括的に実施
- データ転送オブジェクトの型安全性、システム全体の統合動作、性能特性を確認

---

## 8. Modelsクラス単体テスト設計

### 8.1 Core/Models/ConfigModels テスト
**目的**: 設定関連モデルクラスの個別単体テストを実施

#### 列挙型テスト
- **TC117_ConnectionType_列挙型**
  - 入力: ConnectionType値（Ethernet, Serial, USB）
  - 期待出力: 型安全な表現と適切なデフォルト値

- **TC118_FrameVersion_列挙型**
  - 入力: FrameVersion値（Frame3E, Frame4E）
  - 期待出力: 型安全な表現と適切なデフォルト値

#### ConfigModelsクラステスト
- **TC119_ConnectionConfig_コンストラクタ**
  - 入力: IPAddress, Port, UseTcp, ConnectionType, IsBinary, FrameVersion
  - 期待出力: 全プロパティnull不許容での正常インスタンス化

- **TC120_TimeoutConfig_コンストラクタ**
  - 入力: ReceiveTimeoutMs, ConnectTimeoutMs, RetryTimeoutMs
  - 期待出力: 実用的デフォルト値（3-5秒）での正常インスタンス化

- **TC121_TargetDeviceConfig_コンストラクタ**
  - 入力: MDeviceRange, DDeviceRange, DataType
  - 期待出力: 標準範囲デフォルト値での正常インスタンス化

- **TC122_SystemResourcesConfig_コンストラクタ**
  - 入力: MemoryLimitKB, MaxBufferSize, MemoryThresholdKB, LowMemoryMode
  - 期待出力: 実用的制限値（100MB制限）での正常インスタンス化

- **TC123_DataProcessingConfig_コンストラクタ**
  - 入力: TargetName, ContinuousDataMode, DataRetentionDays
  - 期待出力: 継続モード優先設定での正常インスタンス化

- **TC124_LoggingConfig_コンストラクタ**
  - 入力: ConsoleOutput, DetailedLog
  - 期待出力: 開発・運用両対応設定での正常インスタンス化

- **TC125_DataTransferConfig_コンストラクタ**
  - 入力: EnableTransfer, DestinationIpAddress, DestinationPort
  - 期待出力: セキュリティ優先（デフォルト無効）での正常インスタンス化

- **TC126_ProcessedDeviceRequestInfo_操作メソッド**
  - 入力: SplitRanges, DataTypeInfo, OptimizedRanges
  - 期待出力: AddSplitRange, GetSplitRanges等の操作メソッドが正常動作

#### ProcessedDeviceRequestInfo詳細メソッドテスト
- **TC126_1_AddSplitRange_分割範囲追加**
  - 入力:
    - デバイス範囲: "D100-D199"
    - 分割リスト: ["D100-D149", "D150-D199"]
  - 期待出力: SplitRanges辞書に正常追加、DWord分割情報保持

- **TC126_2_AddDataTypeInfo_データ型情報追加**
  - 入力:
    - デバイス範囲: "M001-M999"
    - データ型: "Bit"
  - 期待出力: DataTypeInfo辞書に正常追加、型情報保持

- **TC126_3_AddOptimizedRange_最適化範囲追加**
  - 入力:
    - デバイス種別: "D"
    - 最適化範囲: "D001-D999"
  - 期待出力: OptimizedRanges辞書に正常追加、統合範囲情報保持

- **TC126_4_GetSplitRanges_分割情報取得**
  - 前提: AddSplitRange実行済み
  - 入力: デバイス範囲指定 "D100-D199"
  - 期待出力: 対応する分割リスト ["D100-D149", "D150-D199"]

- **TC126_5_GetDataType_データ型取得**
  - 前提: AddDataTypeInfo実行済み
  - 入力: デバイス範囲指定 "M001-M999"
  - 期待出力: 対応するデータ型 "Bit"

- **TC126_6_GetAllDeviceTypes_全データ型一覧取得**
  - 前提: 複数のAddDataTypeInfo実行済み
  - 入力: なし
  - 期待出力: 全データ型の一覧 ["Bit", "Word", "DWord"]

- **TC126_7_ProcessedDeviceRequestInfo_境界値テスト**
  - 入力: 存在しないデバイス範囲指定
  - 期待出力: null または適切な例外

### 8.2 Core/Models（PlcCommunicationManager用データ転送オブジェクト）テスト
**目的**: PLC通信用データ転送オブジェクトの個別単体テストを実施

#### 列挙型・基本クラステスト
- **TC127_ConnectionStatus_列挙型**
  - 入力: ConnectionStatus値（Connected, Failed, Timeout）
  - 期待出力: 型安全な表現

- **TC128_ConnectionResponse_コンストラクタ**
  - 入力: Status, Socket, RemoteEndPoint, ConnectedAt, ConnectionTime, ErrorMessage
  - 期待出力: 接続成功/失敗で異なるプロパティのnull許容性管理

- **TC129_ConnectionStats_統計メソッド**
  - 入力: ResponseTimes, TotalErrors, TotalRetries
  - 期待出力: AddResponseTime, IncremetError, IncrementRetry等の統計メソッドが正常動作

#### ConnectionStats詳細統計メソッドテスト
- **TC129_1_AddResponseTime_応答時間追加**
  - 入力:
    - 応答時間: TimeSpan.FromMilliseconds(150)
    - 応答時間: TimeSpan.FromMilliseconds(200)
    - 応答時間: TimeSpan.FromMilliseconds(120)
  - 期待出力:
    - ResponseTimes履歴に追加
    - AverageResponseTime自動計算（約157ms）
    - MaxResponseTime更新（200ms）
    - MinResponseTime更新（120ms）

- **TC129_2_AddResponseTime_統計自動再計算**
  - 前提: 複数の応答時間データ追加済み
  - 入力: 新たな応答時間 TimeSpan.FromMilliseconds(300)
  - 期待出力:
    - 平均応答時間の自動再計算
    - 最大応答時間の更新確認
    - 統計精度の検証

- **TC129_3_IncremetError_エラー発生統計**
  - 入力: IncremetError()メソッド複数回実行
  - 期待出力:
    - TotalErrors カウント増加
    - SuccessRate 自動再計算（成功率低下）
    - エラー統計の正確性確認

- **TC129_4_IncrementRetry_リトライ実行統計**
  - 入力: IncrementRetry()メソッド複数回実行
  - 期待出力:
    - TotalRetries カウント増加
    - リトライ統計の正確性確認
    - 通信品質指標への反映

- **TC129_5_SuccessRate_通信成功率計算**
  - 前提:
    - 総送信回数: 100回
    - エラー発生: 5回
    - リトライ成功: 3回
  - 期待出力:
    - SuccessRate = 0.98 (98%成功率)
    - 自動計算精度の検証

- **TC129_6_ConnectionStats_統計リセット**
  - 入力: 新しいConnectionStatsインスタンス作成
  - 期待出力:
    - 全統計値が初期状態（0）
    - 履歴リストが空の状態
    - 成功率が未定義状態（データなし）

- **TC130_ProcessedResponseData_データ操作メソッド**
  - 入力: OriginalRawData, ProcessedData, OriginalRequest
  - 期待出力: AddProcessedDevice, GetDeviceValue等のデータ操作メソッドが正常動作

#### ProcessedResponseData詳細データ操作メソッドテスト
- **TC130_1_AddProcessedDevice_デバイスデータ追加**
  - 入力:
    - デバイス名: "M100"
    - 値: true
    - データ型: "Bit"
    - DWord結合フラグ: false
  - 期待出力:
    - ProcessedData辞書に正常追加
    - ProcessedDeviceCount自動増加
    - デバイス情報の正確な保持

- **TC130_2_AddProcessedDevice_DWord結合デバイス追加**
  - 入力:
    - デバイス名: "D100"
    - 値: 0x12345678
    - データ型: "DWord"
    - DWord結合フラグ: true
  - 期待出力:
    - ProcessedData辞書に正常追加
    - DWordCombinedCount自動増加
    - IsDwordCombinedフラグがtrue

- **TC130_3_AddError_エラーメッセージ追加**
  - 入力: エラーメッセージ "デバイスM100の読み取りに失敗しました"
  - 期待出力:
    - Errorsリストに追加
    - HasErrorsフラグがtrue
    - エラー情報の正確な保持

- **TC130_4_AddWarning_警告メッセージ追加**
  - 入力: 警告メッセージ "デバイスD200のデータ形式を自動修正しました"
  - 期待出力:
    - Warningsリストに追加
    - 警告情報の正確な保持

- **TC130_5_GetDeviceValue_デバイス値取得**
  - 前提: AddProcessedDevice実行済み
  - 入力: デバイス名 "M100"
  - 期待出力: 対応する値 true

- **TC130_6_GetDeviceType_デバイス型取得**
  - 前提: AddProcessedDevice実行済み
  - 入力: デバイス名 "D100"
  - 期待出力: 対応するデータ型 "DWord"

- **TC130_7_GetCombinedDWordDevices_DWord結合デバイス一覧取得**
  - 前提: DWord結合デバイス複数追加済み
  - 入力: なし
  - 期待出力: DWord結合されたデバイス名一覧 ["D100", "D200"]

- **TC130_8_ProcessedResponseData_統計情報自動計算**
  - 前提:
    - 通常デバイス5個追加
    - DWord結合デバイス2個追加
    - エラー1個、警告1個追加
  - 期待出力:
    - ProcessedDeviceCount = 7
    - DWordCombinedCount = 2
    - HasErrors = true
    - 統計精度の検証

- **TC131_SlmpHeader_ヘッダー情報**
  - 入力: SubHeader, NetworkNumber, StationNumber等のSLMP標準情報
  - 期待出力: 全プロパティnull不許容でのSLMP標準情報完全保持

#### SlmpHeader詳細テスト
- **TC131_1_SlmpHeader_3Eフレームヘッダー**
  - 入力:
    - SubHeader: "5000"
    - NetworkNumber: "01"
    - StationNumber: "00"
    - ModuleIONumber: "03FF"
    - MultidropNumber: "00"
    - RequestDataLength: "001B"
    - FrameType: "3E"
    - IsBinary: true
  - 期待出力: 3Eフレーム用SLMP標準ヘッダー情報の完全保持

- **TC131_2_SlmpHeader_4Eフレームヘッダー**
  - 入力:
    - SubHeader: "5400"
    - NetworkNumber: "01"
    - StationNumber: "FF"
    - ModuleIONumber: "03FF"
    - MultidropNumber: "00"
    - RequestDataLength: "001B"
    - FrameType: "4E"
    - IsBinary: true
  - 期待出力: 4Eフレーム用SLMP標準ヘッダー情報の完全保持

- **TC131_3_SlmpHeader_ASCIIフレームヘッダー**
  - 入力:
    - SubHeader: "35303030"
    - NetworkNumber: "3031"
    - StationNumber: "3030"
    - ModuleIONumber: "30334646"
    - MultidropNumber: "3030"
    - RequestDataLength: "30304142"
    - FrameType: "3E"
    - IsBinary: false
  - 期待出力: ASCII形式SLMP標準ヘッダー情報の完全保持

- **TC131_4_SlmpHeader_解析情報**
  - 入力: ParsedAt時刻設定
  - 期待出力:
    - ParsedAt設定済み（解析実行時刻）
    - FrameType判定済み（3E or 4E）
    - IsBinary判定済み（バイナリ/ASCII）

- **TC131_5_SlmpHeader_プロパティnull安全性**
  - 入力: 各プロパティのnull設定試行
  - 期待出力:
    - 全プロパティでnull不許容
    - 適切な例外発生（ArgumentNullException等）

- **TC131_6_SlmpHeader_SLMP仕様準拠性**
  - 入力: 三菱電機公式仕様書準拠のヘッダー値
  - 期待出力:
    - 公式サンプルとの完全一致
    - SLMP通信プロトコル標準への完全準拠
    - ネットワーク情報の正確な保持

- **TC132_StructuredData_構造化メソッド**
  - 入力: Header, EndCode, DeviceData, ReceivedAt等
  - 期待出力: AddStructuredDevice, GetDeviceValue等の構造化メソッドが正常動作

#### StructuredData詳細構造化メソッドテスト
- **TC132_1_AddStructuredDevice_構造化デバイスデータ追加**
  - 入力:
    - デバイス名: "M100"
    - 値: true
    - データ型: "Bit"
    - 解釈フラグ: true
  - 期待出力:
    - DeviceData辞書に正常追加
    - TotalDevices自動増加
    - デバイス構造化情報の正確な保持

- **TC132_2_AddParseStep_解析手順追加**
  - 入力: 解析手順 "SLMPヘッダー解析完了"
  - 期待出力:
    - ParseStepsリストに追加
    - 解析履歴の正確な記録
    - デバッグ情報の保持

- **TC132_3_AddDeviceInterpretation_デバイス解釈情報追加**
  - 入力:
    - デバイス名: "M100"
    - 解釈情報: { "Status": "ON", "Description": "動作中" }
  - 期待出力:
    - DeviceInterpretations辞書に追加
    - 人間が読める形式での情報保持

- **TC132_4_AddStatusJudgment_ステータス判定追加**
  - 入力:
    - デバイス名: "M100"
    - ステータス判定: "正常"
  - 期待出力:
    - StatusJudgments辞書に追加
    - ON/OFF、正常/異常判定の保持

- **TC132_5_SetErrorDetails_エラー詳細設定**
  - 入力:
    - 詳細エラーコード: "E001"
    - エラー説明: "デバイス読み取りエラー"
    - 影響デバイス: ["M100", "M101"]
    - エラー詳細情報: { "原因": "通信タイムアウト" }
  - 期待出力:
    - IsErrorフラグがtrue
    - ErrorMessage設定
    - 詳細エラー情報の完全保持

### 8.3 Core/Models（非同期・並行処理用データ転送オブジェクト）テスト
**目的**: 非同期処理結果の構造化、並行実行状態管理、進捗情報保持、例外処理結果管理をテスト

#### 列挙型テスト
- **TC133_ResourceType_列挙型**
  - 入力: ResourceType値（LogFile, ConfigFile, OutputFile）
  - 期待出力: 型安全な表現
  - 検証項目:
    - LogFile: ログファイルリソース
    - ConfigFile: 設定ファイルリソース
    - OutputFile: データ出力ファイルリソース

#### 非同期処理結果クラステスト
- **TC134_AsyncOperationResult_コンストラクタ**
  - 入力:
    - 成功時: Result, ExecutionTime, OperationName, StartTime
    - 失敗時: Exception, ExecutionTime, OperationName, StartTime
  - 期待出力: 成功/失敗の論理的整合性での正常インスタンス化
    - 成功時: IsSuccess=true, Result設定, Exception=null
    - 失敗時: IsSuccess=false, Result=null, Exception設定

- **TC135_GeneralOperationResult_一括処理結果**
  - 入力: OperationGroupName, ExecutedAt
  - 期待出力: 一括処理統計管理での正常インスタンス化
  - 検証項目:
    - AddSuccess(string operationName): 成功処理追加
    - AddFailure(string operationName, Exception exception): 失敗処理・例外追加
    - GetSuccessRate(): 成功率計算（0.0-1.0）

- **TC136_ParallelExecutionResult_並行実行結果**
  - 入力: TotalCount, StartTime
  - 期待出力: 並行実行効果測定での正常インスタンス化
  - 検証項目:
    - AddPlcResult(string plcId, CycleExecutionResult result): PLC結果追加
    - UpdateContinuingPlcs(List<string> continuingIds): 継続PLC更新
    - CalculateEfficiency(): 並行実行効率計算
    - GetFailedPlcIds(): 失敗PLC ID一覧取得

- **TC137_ProgressInfo_進捗情報基底クラス**
  - 入力: CurrentStep, Progress, Message, ElapsedTime
  - 期待出力: 統一された進捗表現での正常インスタンス化
  - 検証項目:
    - CurrentStep: 現在実行ステップ（例："Step3", "PLC接続中"）
    - Progress: 進捗率（0.0-1.0）
    - Message: 進捗メッセージ（人間向け表示用）
    - EstimatedTimeRemaining: 推定残り時間（null許容）
    - ElapsedTime: 経過時間
    - ReportedAt: 報告時刻

- **TC138_ParallelProgressInfo_並行実行進捗情報**
  - 継承関係: ProgressInfo（基底クラス）を継承
  - 入力: Step, PlcProgresses, ElapsedTime
  - 期待出力: 複数PLC並行実行の進捗情報保持
  - 検証項目:
    - ActivePlcCount: 実行中PLC数
    - CompletedPlcCount: 完了PLC数
    - FailedPlcCount: 失敗PLC数
    - PlcProgresses: PLC別進捗率（PlcId→進捗率）
    - OverallProgress: 全体進捗率（全PLC平均進捗）

#### ApplicationController用データ転送オブジェクトテスト
- **TC139_InitializationResult_初期化結果**
  - 入力:
    - 成功時: ConfigCount, ManagerCount, Time, CheckResults
    - 失敗時: errorDetails, failures
  - 期待出力: Step1初期化結果保持
  - 検証項目:
    - IsSuccess: 初期化成功フラグ
    - LoadedConfigCount: 読み込み設定ファイル数
    - CreatedManagersCount: 作成されたManagerインスタンス数
    - InitializationTime: 初期化処理時間
    - ErrorDetails: エラー詳細（失敗時のみ設定）

- **TC140_CycleExecutionResult_サイクル実行結果**
  - 入力:
    - 成功時: Steps, Time, DataCount, PlcId, StepResults
    - 失敗時: PlcId, ErrorDetails, CompletedSteps
  - 期待出力: Step2-7単一サイクル実行結果保持
  - 検証項目:
    - IsSuccess: サイクル実行成功フラグ
    - ExecutedSteps: 実行完了したステップ一覧
    - ExecutionTime: サイクル全体の実行時間
    - DataCount: 処理されたデバイスデータ数
    - PlcIdentifier: 対象PLCの識別子
    - ErrorDetails: エラー詳細（失敗時のみ設定、成功時はnull）
    - StepResults: 各ステップの実行結果詳細
    - WarningMessages: 警告メッセージ一覧

- **TC141_ShutdownResult_終了処理結果**
  - 入力:
    - 正常終了用: Time, Completed, State, Trigger
    - タイムアウト終了用: Time, Completed, Incomplete, Details, Trigger
  - 期待出力: アプリケーション終了処理結果保持
  - 検証項目:
    - IsGraceful: 適切な終了処理完了フラグ
    - ShutdownTime: 終了処理にかかった時間
    - CompletedTasks: 完了した終了タスク一覧
    - IncompleteTaskCount: 未完了タスク数（タイムアウト時）
    - IncompleteTaskDetails: 未完了タスク詳細一覧
    - FinalResourceState: 最終リソース状態
    - ShutdownTrigger: 終了契機

---

## 9. 統合テスト設計

### 9.1 Step1-2統合テスト
**目的**: 設定読み込みからフレーム構築までの統合機能をテスト

- **TC142_統合_設定読み込み_フレーム構築**
  - 手順:
    1. LoadConfigAsync実行
    2. GetConfig実行（ConnectionConfig, TargetDeviceConfig取得）
    3. SplitDwordToWord実行（DWord分割処理含む）
    4. BuildFrames実行
  - 期待結果: 正しいSLMPフレームが生成される（DWord分割対応）

### 9.2 Step3-6 PLC通信核心部 専用統合テスト
**目的**: PLC通信の最重要部分であるStep3-6の統合動作を徹底検証

#### TC143_1 Step3-6基本連続実行テスト
- **手順**:
  1. Step3: ConnectAsync実行（TCP/UDP別）
  2. Step4: SendFrameAsync → ReceiveResponseAsync実行
  3. Step5: DisconnectAsync実行
  4. Step6: PostprocessReceivedData → ParseRawToStructuredData実行
- **期待結果**: 全ステップが順次成功し、各ステップの戻り値が正常
- **検証項目**:
  - 接続状態の適切な遷移（未接続→接続中→接続済み→切断済み）
  - 各ステップの実行時間が仕様内
  - メモリ使用量の適切な推移

#### TC143_2 Step3-6データ継承精度テスト
- **手順**:
  1. Step3で取得した接続情報（Socket, EndPoint）がStep4-5で正確に使用されること
  2. Step4で受信したデータがStep6で正確に処理されること
  3. Step6で生成した構造化データが完全であること
- **期待結果**: データの完全性が保持され、情報の欠損・変更がない
- **検証項目**:
  - Socket接続情報の一貫性
  - 受信データの16進数形式→バイナリ→構造化データの変換精度
  - SLMPヘッダー情報の完全保持

#### TC143_3 Step3-6段階的エラーハンドリングテスト
- **シナリオ1 Step3失敗**: 接続失敗時のStep4-6適切なスキップ
- **シナリオ2 Step4失敗**: 通信失敗時のStep5切断処理実行とエラー伝播
- **シナリオ3 Step5失敗**: 切断失敗時の適切なリソース解放
- **シナリオ4 Step6失敗**: データ処理失敗時のエラー情報保持
- **期待結果**: 各段階でのエラー発生に対する適切な対応とリソース管理
- **検証項目**:
  - エラー発生ステップ以降の適切な処理スキップ
  - リソースリーク無し
  - エラー詳細情報の正確な記録

#### TC143_4 Step3-6 DWord処理統合テスト
- **前提**: DWord型デバイス（D001-D999）を対象とした通信
- **手順**:
  1. ConfigToFrameManager.SplitDwordToWord()でDWord→16bit×2分割済み
  2. Step3-4: 分割後フレームでの通信実行
  3. Step6: PostprocessReceivedData()でのDWord結合処理
  4. 最終的な32bitデータの正確性確認
- **期待結果**: DWord分割→通信→結合の一連処理が正確
- **検証項目**:
  - 16bit×2→32bit結合の数値精度
  - エンディアン処理の正確性
  - 複数DWordデバイスの一括処理精度

#### TC143_5 Step3-6タイムアウト制御統合テスト
- **シナリオ**:
  - Step3: ConnectTimeoutMsでの接続タイムアウト
  - Step4: ReceiveTimeoutMsでの受信タイムアウト
  - Step5: DisconnectTimeoutMsでの切断タイムアウト
- **期待結果**: 各ステップで設定されたタイムアウト時間の正確な制御
- **検証項目**:
  - タイムアウト時間の精度（±100ms以内）
  - タイムアウト時の適切な例外発生
  - タイムアウト後のリソース状態

#### TC143_6 Step3-6リソース管理統合テスト
- **検証項目**:
  - **メモリ管理**: 各ステップでのメモリ使用量推移
  - **Socket管理**: 接続・切断での適切なSocket状態管理
  - **バッファ管理**: 送受信バッファの適切な確保・解放
- **手順**: 100回連続でStep3-6実行後のリソース状態確認
- **期待結果**:
  - メモリリーク無し
  - Socket接続残存無し
  - システムリソースの適切な解放

#### TC143_7 Step3-6パフォーマンス測定統合テスト
- **測定項目**:
  - **全体実行時間**: Step3-6完了までの時間
  - **各ステップ実行時間**: Step別の処理時間分析
  - **スループット**: 1分間に処理可能なサイクル数
- **期待結果**:
  - 全体実行時間: 平均3秒以内
  - Step3接続時間: 1秒以内
  - Step4通信時間: 1秒以内
  - 連続実行時のパフォーマンス劣化無し

#### TC143_8 Step3-6エラー回復・リトライ統合テスト
- **シナリオ**:
  1. Step3接続失敗→リトライ→成功
  2. Step4通信失敗→リトライ→成功
  3. 最大リトライ回数到達時の適切な処理終了
- **期待結果**: ErrorHandlerと連携したリトライ制御の正確な動作
- **検証項目**:
  - リトライ間隔の制御
  - リトライ回数の正確なカウント
  - 最終失敗時の適切なエラー情報

#### TC143_9 Step3-6並行実行統合テスト（複数PLC対応）
- **前提**: 複数PLC設定ファイル（PLC1, PLC2, PLC3）
- **手順**: 各PLCに対してStep3-6を並行実行
- **期待結果**: 各PLCの独立した処理実行とリソース競合回避
- **検証項目**:
  - PLC別の独立したSocket管理
  - 並行実行時のメモリ効率
  - エラー発生時の他PLC処理への影響無し

### 9.3 エラーハンドリング統合テスト
**目的**: エラー発生時の処理フローをテスト

- **TC144_統合_エラーハンドリング_リトライ**
  - 手順:
    1. ConnectAsync実行（タイムアウト発生）
    2. ErrorHandler.DetermineErrorCategory実行
    3. ErrorHandler.ApplyRetryPolicy実行
    4. リトライ実行
  - 期待結果: 適切なリトライ処理が実行される

---

## 10. パフォーマンステスト

### 10.1 メモリ使用量テスト
- **TC145_Performance_メモリ使用量**
  - 目的: 長時間実行時のメモリリーク検証
  - 手順: 1000回の通信サイクル実行
  - 期待結果: メモリ使用量が閾値内に収まる

### 10.2 応答時間テスト
- **TC146_Performance_応答時間**
  - 目的: 通信応答時間の性能検証
  - 手順: 連続100回の通信測定
  - 期待結果: 平均応答時間が仕様内（例: 100ms以下）

---

## テスト統計

### Modelsクラス単体テスト
- **総テストケース数**: 約50個以上 (TC117～TC141 + 詳細メソッドテスト)
  - ConfigModels: 約20ケース
  - PlcCommunicationManager用DTO: 約25ケース
  - 非同期・並行処理用DTO: 約10ケース
  - ApplicationController用DTO: 5ケース

### 統合テスト
- **総テストケース数**: 約12個 (TC142～TC144 + 詳細サブテスト)
  - Step1-2統合: 1ケース
  - Step3-6統合: 10ケース（最重要）
  - エラーハンドリング統合: 1ケース

### パフォーマンステスト
- **総テストケース数**: 2個 (TC145～TC146)
  - メモリ使用量: 1ケース
  - 応答時間: 1ケース

### 合計
- **総テストケース数**: 約64個以上
- **優先度**:
  - Modelsクラス: 低（データ転送オブジェクトの型安全性確保）
  - 統合テスト: 最重要（システム全体の統合動作確認）
  - パフォーマンステスト: 最重要（システム全体の性能検証）

---

## 実装優先順位
1. **Step3-6統合テスト** - 最優先（PLC通信核心部の完全動作確認）
2. **パフォーマンステスト** - 最優先（システム全体の性能特性確認）
3. **Step1-2統合テスト** - 高優先度（設定・フレーム構築の統合確認）
4. **エラーハンドリング統合テスト** - 高優先度（エラー処理フローの確認）
5. **Modelsクラス単体テスト** - 低優先度（データ転送オブジェクトの型安全性確保）
