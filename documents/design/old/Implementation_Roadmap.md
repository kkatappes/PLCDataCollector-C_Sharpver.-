# 2ステップフロー実装ロードマップ

## 概要

6ステップフローから2ステップフローへの移行における具体的な実装手順を定義した開発者向けロードマップです。

## 実装戦略

### 開発手法
- **TDD実装**: Red-Green-Refactor サイクル必須
- **段階的移行**: Phase1→Phase2→Phase3の順次実行
- **品質保証**: 各フェーズでのテスト実行とバックアップ
- **SOLID原則**: 全新規コードで適用

### 成功基準
1. **コンパイルエラー0**: 各フェーズでビルド成功
2. **既存機能維持**: ログ・監視・データ処理機能の完全維持
3. **パフォーマンス向上**: メモリ使用量とレスポンス時間の改善
4. **テストカバレッジ**: 新規コードの80%以上

## Phase 1: 6ステップフロー機能の無効化

### 目標
現在の6ステップフローを無効化し、2ステップフロー用の基盤を準備

### 実装手順

#### 1.1 Program.cs の修正
```csharp
// 変更前
await RunIntelligentMonitoringAsync(config, loggerFactory, earlyServiceProvider);

// 変更後
await RunSimpleMonitoringAsync(config, loggerFactory, earlyServiceProvider);
```

**具体的な作業**:
- `RunIntelligentMonitoringAsync` メソッドを `RunSimpleMonitoringAsync` に変更
- `IntelligentMonitoringSystem` 呼び出しを `SimpleMonitoringService` 呼び出しに変更
- 6ステップフロー説明文を2ステップフロー説明文に変更

#### 1.2 appsettings.json の調整
**削除する設定セクション**:
```json
// 削除対象
"DeviceDiscoverySettings": { ... },
"IntelligentMonitoringSettings": { ... },
"TypeCodeSpecificSettings": { ... },
"ContinuitySettings": { ... }
```

**残す設定セクション**:
```json
{
  "PlcConnection": {
    "IpAddress": "172.30.40.15",
    "Port": 8192,
    "UseTcp": false,
    "IsBinary": false,
    "FrameVersion": "4E",
    "ReceiveTimeoutMs": 3000,
    "ConnectTimeoutMs": 10000
  },
  "MonitoringSettings": {
    "IntervalMs": 1000,
    "MaxCycles": 0,
    "EnablePerformanceMonitoring": true
  },
  "UnifiedLoggingSettings": {
    "LogFilePath": "logs/rawdata_analysis.json",
    "MaxLogFileSizeMB": 50,
    "LogLevel": "Trace",
    "EnableStructuredLogging": true
  },
  "ConsoleOutputSettings": {
    "EnableCapture": true,
    "OutputFilePath": "logs/terminal_output.txt",
    "OutputLevel": "Information"
  },
  "DiagnosticSettings": {
    "EnableDetailedDiagnostic": true,
    "EnableEnhancedHexDump": true
  }
}
```

#### 1.3 依存性注入システムの調整
**BuildServiceProvider メソッドの修正**:
```csharp
// 削除対象
services.AddSingleton<IntelligentMonitoringSystem>(provider => { ... });

// 新規追加
services.AddSingleton<SimpleMonitoringService>(provider => { ... });
```

**維持する依存性注入**:
- `SlmpClient`
- `UnifiedLogWriter`
- `ConsoleOutputManager`
- `IntegratedOutputManager`
- `PerformanceMonitor`
- `MemoryOptimizer`

### Phase 1 完了基準
- [ ] プロジェクトがビルド成功
- [ ] 6ステップフロー関連設定が削除済み
- [ ] 依存性注入が2ステップフロー対応済み
- [ ] 既存ログ・監視機能が影響なし

## Phase 2: 新しい2ステップ機能の実装

### 目標
SimpleMonitoringService を実装し、M000-M999, D000-D999の固定範囲データ取得を実現

### 2.1 SimpleMonitoringService.cs 作成

#### インターフェース設計
```csharp
namespace SlmpClient.Core
{
    public interface ISimpleMonitoringService
    {
        Task<MonitoringResult> RunTwoStepFlowAsync(IConfiguration config, CancellationToken cancellationToken);
        Task<BitDeviceResult> ReadBitDevicesAsync(string deviceCode, int startAddress, int count);
        Task<WordDeviceResult> ReadWordDevicesAsync(string deviceCode, int startAddress, int count);
        string GetStatusReport();
    }
}
```

#### 実装クラス設計
```csharp
namespace SlmpClient.Core
{
    /// <summary>
    /// シンプル監視サービス - 2ステップフロー実行
    /// TDD手法で実装: Red-Green-Refactor サイクル
    /// SOLID原則適用: 単一責任・依存性注入・インターフェース分離
    /// </summary>
    public class SimpleMonitoringService : ISimpleMonitoringService
    {
        private readonly ISlmpClientFull _slmpClient;
        private readonly ILogger<SimpleMonitoringService> _logger;
        private readonly UnifiedLogWriter _unifiedLogWriter;
        private readonly PerformanceMonitor _performanceMonitor;
        private readonly MemoryOptimizer _memoryOptimizer;
        private readonly PseudoDwordSplitter _pseudoDwordSplitter;
        private readonly IConfiguration _configuration;

        // 固定デバイス範囲定数
        private const string BIT_DEVICE_CODE = "M";
        private const string WORD_DEVICE_CODE = "D";
        private const int BIT_START_ADDRESS = 0;
        private const int WORD_START_ADDRESS = 0;
        private const int BIT_DEVICE_COUNT = 1000; // M000-M999
        private const int WORD_DEVICE_COUNT = 1000; // D000-D999

        public SimpleMonitoringService(
            ISlmpClientFull slmpClient,
            ILogger<SimpleMonitoringService> logger,
            UnifiedLogWriter unifiedLogWriter,
            PerformanceMonitor performanceMonitor,
            MemoryOptimizer memoryOptimizer,
            PseudoDwordSplitter pseudoDwordSplitter,
            IConfiguration configuration)
        {
            _slmpClient = slmpClient ?? throw new ArgumentNullException(nameof(slmpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unifiedLogWriter = unifiedLogWriter ?? throw new ArgumentNullException(nameof(unifiedLogWriter));
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
            _memoryOptimizer = memoryOptimizer ?? throw new ArgumentNullException(nameof(memoryOptimizer));
            _pseudoDwordSplitter = pseudoDwordSplitter ?? throw new ArgumentNullException(nameof(pseudoDwordSplitter));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<MonitoringResult> RunTwoStepFlowAsync(IConfiguration config, CancellationToken cancellationToken)
        {
            // Step 1: PLC接続
            // Step 2: 固定範囲データ取得ループ
        }

        public async Task<BitDeviceResult> ReadBitDevicesAsync(string deviceCode, int startAddress, int count)
        {
            // M000-M999 読み取り実装
        }

        public async Task<WordDeviceResult> ReadWordDevicesAsync(string deviceCode, int startAddress, int count)
        {
            // D000-D999 読み取り実装
        }

        public string GetStatusReport()
        {
            // パフォーマンス統計とステータス情報を返す
        }
    }
}
```

#### 2.2 データモデル定義
```csharp
namespace SlmpClient.Core
{
    public class MonitoringResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
        public bool MonitoringStarted { get; set; }
        public int CycleCount { get; set; }
    }

    public class BitDeviceResult
    {
        public bool Success { get; set; }
        public bool[] Values { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan ResponseTime { get; set; }
    }

    public class WordDeviceResult
    {
        public bool Success { get; set; }
        public ushort[] Values { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan ResponseTime { get; set; }
    }
}
```

### 2.3 RunSimpleMonitoringAsync メソッド実装

#### Program.cs への追加
```csharp
/// <summary>
/// シンプル監視システム実行（2ステップフロー）
/// </summary>
private static async Task RunSimpleMonitoringAsync(IConfiguration config, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
{
    var integratedOutputManager = serviceProvider.GetService<IntegratedOutputManager>();
    var consoleOutputManager = serviceProvider.GetService<ConsoleOutputManager>();

    try
    {
        // セッション開始ログ
        if (integratedOutputManager != null)
        {
            var sessionInfo = new SessionStartInfo
            {
                SessionId = $"session_{DateTime.Now:yyyyMMdd_HHmmss}",
                ProcessId = Environment.ProcessId,
                ApplicationName = "SLMP シンプル監視システム",
                Version = "v2.1",
                Environment = "Production"
            };

            var configDetails = new ConfigurationDetails
            {
                ConfigFile = "appsettings.json",
                ConnectionTarget = "固定範囲データ取得システム",
                SlmpSettings = "2ステップフロー実行",
                ContinuityMode = "ReturnDefaultAndContinue",
                RawDataLogging = "Enabled",
                LogOutputPath = "logs/rawdata_analysis.log"
            };

            await integratedOutputManager.WriteSessionStartAsync(sessionInfo, configDetails);
        }

        // 2ステップフロー説明
        if (consoleOutputManager != null)
        {
            await consoleOutputManager.WriteHeaderAsync("シンプル監視システム開始", "SystemStart",
                context: new {
                    Steps = new string[] {
                        "1. 設定ファイルで接続するPLCを決定",
                        "2. PLCに接続し、設定ファイルに従った間隔でM000-M999,D000-D999のデータを取得"
                    }
                });
        }

        Console.WriteLine("🚀 シンプル監視システム開始");
        Console.WriteLine("2ステップフロー:");
        Console.WriteLine("1. 設定ファイルで接続するPLCを決定");
        Console.WriteLine("2. PLCに接続し、設定ファイルに従った間隔でM000-M999,D000-D999のデータを取得");
        Console.WriteLine();

        // SimpleMonitoringServiceを取得
        var monitoringService = serviceProvider.GetService<SimpleMonitoringService>();
        if (monitoringService == null)
        {
            throw new InvalidOperationException("SimpleMonitoringServiceの初期化に失敗しました");
        }

        // キャンセレーショントークン設定
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        // 2ステップフロー実行
        Console.WriteLine("🎯 2ステップフロー実行開始");
        var result = await monitoringService.RunTwoStepFlowAsync(config, cts.Token);

        if (result.Success)
        {
            Console.WriteLine("✅ 2ステップフロー実行完了");
            Console.WriteLine($"🔄 サイクル数: {result.CycleCount}");
            Console.WriteLine($"⏱️ 実行時間: {result.Duration.TotalSeconds:F1}秒");
        }
        else
        {
            Console.WriteLine($"❌ 2ステップフロー実行失敗: {result.ErrorMessage}");
            throw new InvalidOperationException(result.ErrorMessage);
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine();
        Console.WriteLine("⏹️ ユーザーによりキャンセルされました");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ シンプル監視実行エラー: {ex.Message}");
        Console.WriteLine($"詳細: {ex}");
        throw;
    }
}
```

### 2.4 統合システム活用実装

#### UnifiedLogWriter の活用
```csharp
// セッション開始
await _unifiedLogWriter.WriteSessionStartAsync(sessionInfo, configDetails);

// サイクル開始
await _unifiedLogWriter.WriteCycleStartAsync(cycleInfo);

// 通信実行（生データ記録）
await _unifiedLogWriter.WriteCommunicationAsync(communicationInfo, rawDataAnalysis);

// パフォーマンス統計
await _unifiedLogWriter.WriteStatisticsAsync(statisticsInfo);
```

#### PerformanceMonitor の活用
```csharp
// パフォーマンス監視開始
_performanceMonitor.StartMonitoring();

// 通信レスポンス時間記録
_performanceMonitor.RecordResponseTime(responseTime);

// メモリ使用量記録
_performanceMonitor.RecordMemoryUsage();
```

#### PseudoDwordSplitter の活用
```csharp
// ワードデータ分割処理
var splitResult = await _pseudoDwordSplitter.SplitWordDataAsync(wordData);
```

### Phase 2 完了基準
- [ ] SimpleMonitoringService.cs が完全実装済み
- [ ] M000-M999, D000-D999の読み取りが正常動作
- [ ] 統合ログシステムが正常稼働
- [ ] パフォーマンス監視が正常稼働
- [ ] エラーハンドリングが適切に動作

## Phase 3: 6ステップフロー専用ファイルの整理

### 目標
不要になった6ステップフロー専用クラスファイルの削除・移動

### 3.1 削除対象ファイル一覧

#### Core フォルダ（6ステップフロー専用）
```bash
# 削除予定ファイル
rm andon/Core/IntelligentMonitoringSystem.cs
rm andon/Core/DeviceDiscoveryManager.cs
rm andon/Core/AdaptiveMonitoringManager.cs
rm andon/Core/SessionManager.cs
rm andon/Core/CompleteDeviceMap.cs
rm andon/Core/DeviceCompatibilityMatrix.cs
rm andon/Core/ApplicationConfiguration.cs
rm andon/Core/SixStepFlowModels.cs
rm andon/Core/DeviceDiscoveryModels.cs
```

#### Tests フォルダ（6ステップフロー関連）
```bash
# 削除予定テストファイル
rm andon.Tests/Core/IntelligentMonitoringSystemFallbackTests.cs
rm andon.Tests/Core/SessionManagerTests.cs
```

### 3.2 維持対象ファイル確認

#### 維持するCore フォルダ（有用機能）
- ✅ `UnifiedLogWriter.cs` - 統合ログシステム
- ✅ `IntegratedOutputManager.cs` - ターミナル・ファイル同期出力
- ✅ `ConsoleOutputCapture.cs` - コンソール出力キャプチャ
- ✅ `SlmpRawDataRecorder.cs` - SLMPフレーム16進ダンプ
- ✅ `NetworkQualityMonitor.cs` - ネットワーク品質監視
- ✅ `SlmpRawDataModels.cs` - 生データモデル
- ✅ `SlmpClient.cs` - 基本SLMP通信

#### 維持するUtils フォルダ（有用機能）
- ✅ `MemoryOptimizer.cs` - メモリ最適化
- ✅ `CompressionEngine.cs` - 圧縮エンジン
- ✅ `SlmpMemoryMonitor.cs` - SLMP専用メモリ監視
- ✅ `StreamingFrameProcessor.cs` - ストリーミングフレーム処理
- ✅ `ChunkProcessor.cs` - チャンク処理
- ✅ `SlmpConnectionPool.cs` - 接続プール
- ✅ `DataProcessor.cs` - データ処理
- ✅ `PseudoDwordSplitter.cs` - ワードデータ分割

### 3.3 新規テストファイル作成

#### SimpleMonitoringServiceTests.cs 作成
```csharp
namespace SlmpClient.Tests.Core
{
    /// <summary>
    /// SimpleMonitoringService のTDDテスト
    /// Red-Green-Refactor サイクルで実装
    /// </summary>
    [TestFixture]
    public class SimpleMonitoringServiceTests
    {
        private SimpleMonitoringService _service;
        private Mock<ISlmpClientFull> _mockSlmpClient;
        private Mock<ILogger<SimpleMonitoringService>> _mockLogger;
        private Mock<UnifiedLogWriter> _mockUnifiedLogWriter;

        [SetUp]
        public void Setup()
        {
            // モックオブジェクト初期化
        }

        [Test]
        public async Task RunTwoStepFlowAsync_正常ケース_成功を返す()
        {
            // Red: テスト失敗を確認
            // Green: 最小実装で成功させる
            // Refactor: コード改善
        }

        [Test]
        public async Task ReadBitDevicesAsync_M000から999_正確な値を返す()
        {
            // M000-M999の読み取りテスト
        }

        [Test]
        public async Task ReadWordDevicesAsync_D000から999_正確な値を返す()
        {
            // D000-D999の読み取りテスト
        }

        [Test]
        public async Task RunTwoStepFlowAsync_PLC接続失敗_適切なエラーハンドリング()
        {
            // エラーハンドリングテスト
        }
    }
}
```

### Phase 3 完了基準
- [ ] 6ステップフロー専用ファイルが削除済み
- [ ] 維持対象ファイルが正常動作確認済み
- [ ] SimpleMonitoringServiceTests.cs が完全実装済み
- [ ] 全テストが成功
- [ ] プロジェクトがクリーンビルド成功

## 最終検証チェックリスト

### 機能検証
- [ ] M000-M999のビットデバイス読み取りが正常動作
- [ ] D000-D999のワードデバイス読み取りが正常動作
- [ ] 1000ms間隔での継続稼働が安定動作
- [ ] Ctrl+Cでの正常終了が動作

### ログ検証
- [ ] 7種類エントリタイプが正常出力
  - [ ] SESSION_START
  - [ ] CYCLE_START
  - [ ] CYCLE_COMMUNICATION
  - [ ] ERROR_OCCURRED
  - [ ] STATISTICS
  - [ ] PERFORMANCE_METRICS
  - [ ] SESSION_END
- [ ] SLMPフレーム16進ダンプが正常出力
- [ ] ターミナル出力とファイル出力の同期確認

### パフォーマンス検証
- [ ] メモリ使用量が従来比30%削減
- [ ] レスポンス時間が3000ms以内
- [ ] CPU使用率が適切な範囲
- [ ] ログファイルサイズが制限内

### エラーハンドリング検証
- [ ] PLC接続失敗時の自動再接続
- [ ] 部分読み取り失敗時の継続処理
- [ ] ログファイル書き込み失敗時の対応
- [ ] メモリ不足時の適切な処理

### 配布環境検証
- [ ] run_rawdata_logging.bat が正常実行
- [ ] andon.exe 直接実行が正常動作
- [ ] appsettings.json 設定変更が反映
- [ ] logs フォルダが自動作成

## リスク管理

### ロールバック準備
1. **現在のコードのGitバックアップ**
2. **Phase毎のチェックポイント作成**
3. **重要機能の動作確認スクリプト準備**

### トラブルシューティング
1. **ビルドエラー**: 依存関係の確認、NuGetパッケージの復元
2. **実行時エラー**: appsettings.json の設定確認
3. **パフォーマンス劣化**: MemoryOptimizer の設定確認
4. **ログ出力異常**: UnifiedLogWriter の権限・パス確認

---

*この実装ロードマップは、6ステップフローから2ステップフローへの移行における詳細な実装手順を定義しています。TDD手法とSOLID原則に従い、品質を確保しながら段階的に実装してください。*