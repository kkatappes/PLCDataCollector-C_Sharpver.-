# サポート機能マネージャー テスト設計

## テスト方針
- **TDD手法**を使用してテスト駆動開発を実施
- LoggingManager、ErrorHandler、ResourceManagerの各サポート機能をテスト
- システム支援機能の品質確保

---

## 3. LoggingManager テスト設計

### 3.1 InitializeAsync メソッド
**目的**: ログシステム初期化機能をテスト

#### 正常系テスト
- **TC042_InitializeAsync_正常初期化**
  - 入力: LoggingConfig（ConsoleOutput=true, DetailedLog設定）
  - 期待出力:
    - ログファイル作成
    - ディレクトリ作成
    - 初期化完了ステータス

#### 異常系テスト
- **TC043_InitializeAsync_ディスク容量不足**
  - 入力: 有効なLoggingConfig
  - 前提: ディスク容量不足状態
  - 期待出力: IOException

### 3.2 各ログ出力メソッド（LogConfigAsync, LogCommunicationAsync等）
**目的**: 各種ログ出力機能をテスト

#### 正常系テスト
- **TC044_LogConfigAsync_設定ログ出力**
  - 入力: 全設定オブジェクト
  - 期待出力: 設定情報がファイル・コンソールに出力

- **TC045_LogCommunicationAsync_通信ログ出力**
  - 入力: SLMPフレーム、フレーム解析結果
  - 期待出力: 通信情報がファイル・コンソールに出力

### 3.3 SetCorrelationId メソッド
**目的**: セッション関連付けID設定機能をテスト

#### 正常系テスト
- **TC046_SetCorrelationId_正常設定**
  - 入力: SessionId（処理セッション識別子）
  - 期待出力: 関連付け設定完了状態

### 3.4 SetLogLevel メソッド
**目的**: ログレベル設定機能をテスト

#### 正常系テスト
- **TC047_SetLogLevel_正常設定**
  - 入力: LogLevel（出力レベル指定）
  - 期待出力: レベル設定完了状態

### 3.5 IsLogLevelEnabled メソッド
**目的**: ログレベル有効性確認機能をテスト

#### 正常系テスト
- **TC048_IsLogLevelEnabled_有効性確認**
  - 入力: LogLevel（確認対象レベル）
  - 期待出力: 有効性判定結果（true/false）

### 3.6 LogStateAsync メソッド
**目的**: アプリケーション状態出力機能をテスト

#### 正常系テスト
- **TC049_LogStateAsync_状態ログ出力**
  - 入力: セッション情報、サイクル情報、処理状況
  - 期待出力: 状態ログがファイル・コンソールに出力

### 3.7 LogMetricAsync メソッド
**目的**: 統計/パフォーマンス情報出力機能をテスト

#### 正常系テスト
- **TC050_LogMetricAsync_統計ログ出力**
  - 入力: 実行統計、応答時間分析、システム稼働状況
  - 期待出力: 統計ログがファイル・コンソールに出力

### 3.8 LogErrorAsync メソッド
**目的**: エラー/例外情報詳細出力機能をテスト

#### 正常系テスト
- **TC051_LogErrorAsync_エラーログ出力**
  - 入力: エラー分類、エラー詳細、回復処理結果
  - 期待出力: エラーログがファイル・コンソールに出力

### 3.9 LogDeviceDataAsync メソッド
**目的**: デバイス解釈情報出力機能をテスト

#### 正常系テスト
- **TC052_LogDeviceDataAsync_デバイスデータログ出力**
  - 入力: 生データ、解釈結果、ステータス判定
  - 期待出力: デバイスデータログがファイル・コンソールに出力

### 3.10 BeginTransaction メソッド
**目的**: トランザクション開始機能をテスト

#### 正常系テスト
- **TC053_BeginTransaction_正常開始**
  - 入力: TransactionId（トランザクション識別子）
  - 期待出力: トランザクション開始状態

### 3.11 EndTransaction メソッド
**目的**: トランザクション終了機能をテスト

#### 正常系テスト
- **TC054_EndTransaction_正常終了**
  - 入力: TransactionId、TransactionResult（成功/失敗結果）
  - 期待出力: トランザクション終了状態

### 3.12 FlushAsync メソッド
**目的**: バッファフラッシュ機能をテスト

#### 正常系テスト
- **TC055_FlushAsync_正常フラッシュ**
  - 入力: FlushTarget（ファイル指定）
  - 期待出力: フラッシュ完了状態

---

## 4. ErrorHandler テスト設計（バランス型例外設計対応）

### 4.0 バランス型例外設計テスト方針
**目的**: 混在型例外設計（カスタム例外 + 標準例外）、統一エラーメッセージ管理、権限チェック方式の動作検証

#### 基本テスト戦略
- **例外分類テスト**: MultiConfigLoadException（カスタム） vs 標準.NET例外の使い分け検証
- **メッセージ統一テスト**: ErrorMessages.cs定数クラスによるメッセージ管理検証
- **権限チェックテスト**: 単純ファイル作成テストによる確実性検証

### 4.1 DetermineErrorCategory メソッド
**目的**: エラー分類判定機能をテスト

#### 正常系テスト
- **TC056_DetermineErrorCategory_通信エラー**
  - 入力: SocketException, StepNumber=3
  - 期待出力:
    - ErrorCategory=CommunicationError
    - Severity=Error

- **TC057_DetermineErrorCategory_設定エラー**
  - 入力: ConfigurationException, StepNumber=1
  - 期待出力:
    - ErrorCategory=ConfigurationError
    - Severity=Fatal

### 4.2 ApplyRetryPolicy メソッド
**目的**: リトライ方針適用機能をテスト

#### 正常系テスト
- **TC058_ApplyRetryPolicy_リトライ実行**
  - 入力:
    - ErrorCategory=CommunicationError
    - StepNumber=4
    - CurrentRetryCount=1
  - 期待出力:
    - ShouldRetry=true
    - MaxRetryCount=3

- **TC059_ApplyRetryPolicy_リトライ上限**
  - 入力:
    - ErrorCategory=CommunicationError
    - CurrentRetryCount=3
  - 期待出力: ShouldRetry=false

### 4.3 RecordError メソッド
**目的**: エラー記録機能をテスト

#### 正常系テスト
- **TC060_RecordError_正常記録**
  - 入力:
    - ErrorCategory（エラー分類）
    - Severity（重要度）
    - ErrorMessage（エラーメッセージ）
    - Exception（例外オブジェクト）
    - StepNumber（ステップ番号）
  - 期待出力: 記録完了状態

### 4.4 ApplyErrorPolicy メソッド
**目的**: エラー処理方針適用機能をテスト

#### 正常系テスト
- **TC061_ApplyErrorPolicy_継続判定**
  - 入力:
    - ErrorCategory（エラー分類）
    - StepNumber（ステップ番号）
  - 期待出力: ErrorAction（継続/終了の判定結果）

### 4.5 ErrorMessages テスト設計（統一エラーメッセージ管理）
**目的**: 定数クラスによる統一エラーメッセージ管理機能をテスト

#### 正常系テスト
- **TC062_ErrorMessages_設定ファイル関連**
  - 入力: CONFIG_FILE_NOT_FOUND, ファイルパス "test.xlsx"
  - 期待出力: "設定ファイルが見つかりません: test.xlsx"
  - 検証項目: パラメータ埋め込み、日本語メッセージ品質

- **TC063_ErrorMessages_複数設定ファイル関連**
  - 入力: MULTI_CONFIG_LOAD_FAILED, 成功数=2, 失敗数=1
  - 期待出力: "複数設定ファイル読み込みに失敗しました。成功: 2件、失敗: 1件"
  - 検証項目: 複数パラメータ埋め込み、部分成功状況表現

- **TC064_ErrorMessages_通信関連**
  - 入力: CONNECTION_FAILED, IPアドレス "192.168.1.10", ポート 5000
  - 期待出力: "PLC接続に失敗しました: 192.168.1.10:5000"
  - 検証項目: ネットワーク情報表現、技術者向け詳細情報

- **TC065_ErrorMessages_権限・ファイルシステム関連**
  - 入力: WRITE_PERMISSION_CHECK_FAILED, パス "./logs/"
  - 期待出力: "書き込み権限チェックに失敗しました: ./logs/"
  - 検証項目: 権限エラー表現、パス情報表示

- **TC066_ErrorMessages_メモリ・リソース関連**
  - 入力: MEMORY_LIMIT_EXCEEDED, 現在使用量=120000, 制限値=100000
  - 期待出力: "メモリ使用量が制限を超過しました: 120000KB/100000KB"
  - 検証項目: リソース使用量表現、数値フォーマット

#### ErrorMessages網羅的テスト追加
- **TC067_ErrorMessages_メモリ警告閾値**
  - 入力: MEMORY_THRESHOLD_WARNING, 現在使用量=85000, 警告閾値=80000
  - 期待出力: "メモリ使用量が警告閾値を超過しました: 85000KB/80000KB"
  - 検証項目: 警告レベルメッセージ、閾値表現

- **TC068_ErrorMessages_リソース最適化要求**
  - 入力: RESOURCE_OPTIMIZATION_REQUIRED, 理由="メモリ使用量増加"
  - 期待出力: "リソース最適化が必要です: メモリ使用量増加"
  - 検証項目: 最適化要求メッセージ、理由説明

- **TC069_ErrorMessages_汎用システムメッセージ**
  - 入力: UNEXPECTED_ERROR, エラー詳細="NullReferenceException in ConfigManager"
  - 期待出力: "予期しないエラーが発生しました: NullReferenceException in ConfigManager"
  - 検証項目: 汎用エラーメッセージ、技術詳細情報

- **TC070_ErrorMessages_操作キャンセルメッセージ**
  - 入力: OPERATION_CANCELLED, 操作名="PLC接続処理"
  - 期待出力: "操作がキャンセルされました: PLC接続処理"
  - 検証項目: キャンセル状況表現、操作識別

- **TC071_ErrorMessages_初期化失敗メッセージ**
  - 入力: INITIALIZATION_FAILED, コンポーネント="LoggingManager"
  - 期待出力: "初期化に失敗しました: LoggingManager"
  - 検証項目: 初期化エラー表現、コンポーネント識別

- **TC072_ErrorMessages_ディレクトリ作成失敗**
  - 入力: DIRECTORY_CREATE_FAILED, パス="./logs/rawdata/"
  - 期待出力: "ディレクトリ作成に失敗しました: ./logs/rawdata/"
  - 検証項目: ファイルシステム操作エラー、パス情報

- **TC073_ErrorMessages_ログファイル書き込み失敗**
  - 入力: LOG_FILE_WRITE_FAILED, ファイルパス="./logs/terminal_output.txt"
  - 期待出力: "ログファイル書き込みに失敗しました: ./logs/terminal_output.txt"
  - 検証項目: ログシステムエラー、ファイルパス表現

- **TC074_ErrorMessages_複数設定ファイル対応無しエラー**
  - 入力: MULTI_CONFIG_NO_FILES_FOUND, パターン="*_settings.xlsx"
  - 期待出力: "指定パターンに一致する設定ファイルが見つかりません: *_settings.xlsx"
  - 検証項目: 複数設定ファイル機能エラー、パターンマッチング

- **TC075_ErrorMessages_設定項目不足エラー**
  - 入力: CONFIG_MISSING_REQUIRED_FIELD, 項目名="IpAddress"
  - 期待出力: "必須設定項目が不足しています: IpAddress"
  - 検証項目: 設定検証エラー、必須項目識別

- **TC076_ErrorMessages_設定ディレクトリ不存在**
  - 入力: CONFIG_DIRECTORY_NOT_FOUND, ディレクトリ="./config/"
  - 期待出力: "設定ディレクトリが見つかりません: ./config/"
  - 検証項目: 設定ファイル検索エラー、ディレクトリ表現

#### 設計品質テスト
- **TC077_ErrorMessages_メッセージ一意性**
  - 検証項目: 全メッセージが一意であること、重複無し
  - 期待結果: 各メッセージIDが唯一の文字列を持つ

- **TC078_ErrorMessages_パラメータ整合性**
  - 検証項目: パラメータプレースホルダー（{0}, {1}等）と実使用の整合性
  - 期待結果: 全メッセージで必要パラメータ数が正確

### 4.6 MultiConfigLoadException テスト設計（複数設定ファイル専用例外）
**目的**: 複数設定ファイル読み込み時の詳細エラー情報保持機能をテスト

#### 正常系テスト
- **TC079_MultiConfigLoadException_完全失敗**
  - 入力:
    - directory="./config/"
    - pattern="*_settings.xlsx"
    - failures={ "PLC1.xlsx": FileNotFoundException, "PLC2.xlsx": FormatException }
  - 期待出力:
    - LoadedFiles=[]（空リスト）
    - FailedFiles=2件
    - IsPartialSuccess=false
    - TotalAttempted=2

- **TC080_MultiConfigLoadException_部分成功**
  - 入力:
    - directory="./config/"
    - pattern="*_settings.xlsx"
    - loaded=["PLC1_settings.xlsx", "PLC3_settings.xlsx"]
    - failures={ "PLC2_settings.xlsx": IOException }
  - 期待出力:
    - LoadedFiles=2件
    - FailedFiles=1件
    - IsPartialSuccess=true
    - SuccessCount=2, FailureCount=1

- **TC081_MultiConfigLoadException_詳細レポート生成**
  - 前提: 部分成功状態のMultiConfigLoadException
  - 入力: GetDetailedErrorReport()実行
  - 期待出力:
    - 成功ファイル一覧表示
    - 失敗ファイルと例外タイプ表示
    - 読み込み試行ディレクトリ・パターン表示
    - 統計情報表示（成功/失敗件数）

- **TC082_MultiConfigLoadException_特定失敗確認**
  - 前提: 複数種類の例外を含むMultiConfigLoadException
  - 入力: HasSpecificFailure(typeof(FileNotFoundException))
  - 期待出力: true（FileNotFoundExceptionが含まれる場合）

#### 継承・互換性テスト
- **TC083_MultiConfigLoadException_標準例外互換**
  - 検証項目: Exception基底クラスとの完全互換性
  - 期待結果:
    - Message プロパティ正常動作
    - InnerException プロパティ正常動作
    - StackTrace 情報保持
    - ToString() メソッド適切な出力

- **TC084_MultiConfigLoadException_シリアライゼーション**
  - 検証項目: .NET標準シリアライゼーション対応
  - 期待結果: 例外情報の完全な復元が可能

### 4.7 バランス型例外処理統合テスト
**目的**: カスタム例外と標準例外の使い分け、統一メッセージ管理の統合動作をテスト

#### 統合テスト
- **TC085_バランス型例外_使い分け判定**
  - シナリオ1（カスタム例外使用）:
    - 状況: 複数設定ファイル読み込みで部分失敗
    - 期待例外: MultiConfigLoadException
    - 期待メッセージ: ErrorMessages.MULTI_CONFIG_PARTIAL_SUCCESS使用

  - シナリオ2（標準例外使用）:
    - 状況: 単一設定ファイル読み込みでファイル不存在
    - 期待例外: FileNotFoundException
    - 期待メッセージ: ErrorMessages.CONFIG_FILE_NOT_FOUND使用

- **TC086_バランス型例外_権限チェック統合**
  - 手順:
    1. 単純ファイル作成テストで権限チェック実行
    2. 権限不足の場合、UnauthorizedAccessException発生
    3. ErrorMessages.WRITE_PERMISSION_CHECK_FAILED使用確認
  - 期待結果: 確実な権限判定と統一メッセージ使用

- **TC087_バランス型例外_メッセージ一貫性**
  - 検証項目: カスタム例外・標準例外共にErrorMessagesクラス使用
  - 期待結果:
    - MultiConfigLoadException: ErrorMessages使用
    - 標準例外: ErrorMessages使用（適切なwrap）
    - メッセージ品質・形式の統一性確保

---

## 5. ResourceManager テスト設計

### 5.1 GetMemoryUsage メソッド
**目的**: メモリ使用量取得機能をテスト

#### 正常系テスト
- **TC088_GetMemoryUsage_正常取得**
  - 入力: 現在のシステム状態
  - 期待出力:
    - 現在のメモリ使用量（KB単位）
    - 各コンポーネント別使用量

### 5.2 EvaluateLevel メソッド
**目的**: メモリレベル判定機能をテスト

#### 正常系テスト
- **TC089_EvaluateLevel_正常レベル**
  - 入力:
    - 現在使用量=1000KB
    - 閾値設定（MemoryLimitKB=10000, MemoryThresholdKB=8000）
  - 期待出力:
    - メモリレベル=Normal
    - 推奨アクション=None

- **TC090_EvaluateLevel_警告レベル**
  - 入力:
    - 現在使用量=8500KB
    - 閾値設定（MemoryLimitKB=10000, MemoryThresholdKB=8000）
  - 期待出力:
    - メモリレベル=Warning
    - 推奨アクション=OptimizeMemory

### 5.3 ApplyDataAndLoggingPolicy メソッド
**目的**: データ/ログポリシー適用機能をテスト

#### 正常系テスト
- **TC091_ApplyDataAndLoggingPolicy_ポリシー適用**
  - 入力:
    - メモリレベル（ResourceManager.EvaluateLevel()）
    - ポリシー設定（ConfigToFrameManager.LoadConfigAsync()）
  - 期待出力:
    - 適用されたポリシー内容
    - データ処理制限設定

### 5.4 OptimizeMemory メソッド
**目的**: メモリ最適化実行機能をテスト

#### 正常系テスト
- **TC092_OptimizeMemory_最適化実行**
  - 入力:
    - 最適化対象（各クラスインスタンス）
    - 最適化レベル（ResourceManager.EvaluateLevel()）
  - 期待出力:
    - 最適化実行結果
    - 削減されたメモリ量

### 5.5 WriteLogAsync メソッド
**目的**: メモリ状況ログ出力機能をテスト

#### 正常系テスト
- **TC093_WriteLogAsync_メモリログ出力**
  - 入力:
    - メモリ使用状況（ResourceManager.GetMemoryUsage()）
    - メモリレベル（ResourceManager.EvaluateLevel()）
    - 最適化結果（ResourceManager.OptimizeMemory()）
  - 期待出力: ログ出力完了状態

### 5.6 RunMonitoringLoopAsync メソッド
**目的**: メモリ監視ループ実行機能をテスト

#### 正常系テスト
- **TC094_RunMonitoringLoopAsync_監視ループ実行**
  - 入力:
    - 監視間隔設定（ConfigToFrameManager.LoadConfigAsync()）
    - 継続監視フラグ
  - 期待出力: 監視ループ実行状態

---

## テスト統計

### LoggingManager
- **総テストケース数**: 14個 (TC042～TC055)
- **正常系**: 13ケース
- **異常系**: 1ケース

### ErrorHandler
- **総テストケース数**: 33個 (TC056～TC087)
  - DetermineErrorCategory: 2ケース
  - ApplyRetryPolicy: 2ケース
  - RecordError: 1ケース
  - ApplyErrorPolicy: 1ケース
  - ErrorMessages: 16ケース
  - MultiConfigLoadException: 6ケース
  - バランス型例外統合: 3ケース
  - 設計品質: 2ケース

### ResourceManager
- **総テストケース数**: 7個 (TC088～TC094)
- **正常系**: 7ケース

### 合計
- **総テストケース数**: 54個
- **優先度**: 高（Step3-6完了後に実装）
