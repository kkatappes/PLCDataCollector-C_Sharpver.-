# ハイブリッド統合ログシステム設計書

## 📅 作成日・更新履歴
- 2025-09-26: 初版作成（完全単一ファイル統合設計）
- 2025-10-02: **重要更新** - ハイブリッド統合設計への変更
- 2025-10-06: **最新更新** - 2ステップフロー対応エントリタイプ追加

## 概要
**2ステップフロー対応のSLMP継続監視アプリケーション（SimpleMonitoringService）**において、技術的詳細情報を統合ログファイル（rawdata_analysis.log）に統合し、加えて人間可読なコンソール出力を専用JSONファイル（terminal_output.txt）に分離保存するハイブリッド統合ログシステムの設計仕様。

### 🔄 2ステップフロー対応概要
- **従来**: 6ステップフロー（IntelligentMonitoringSystem）
- **最新**: 2ステップフロー（SimpleMonitoringService）
- **対象範囲**: M000-M999, D000-D999固定範囲データ取得
- **ログ最適化**: 固定範囲処理に特化したエントリタイプ追加

### 🎯 ハイブリッド統合の目的
1. **技術情報統合**: SLMP通信・診断・詳細情報を単一ファイルに統合
2. **人間可読性**: コンソール出力を別ファイルで構造化保存
3. **用途別最適化**: 技術者向け詳細ログ + 運用者向け状況ログの両立

## 背景・現状分析

### 現在の問題点
1. **情報の分散**
   - `rawdata_analysis.log`: 基本通信ログ（約30%の情報量）
   - `rawdata_analysis.json`: 同じデータの重複出力
   - `terminal_output.txt`: 豊富な情報（約70%がログファイルに含まれない）

2. **SLMPフレーム生データの欠落**
   - `rawResponseData`が常に`null`で`SlmpRawDataAnalyzer`が動作していない
   - 送信・受信フレームの生バイナリデータが記録されていない
   - 詳細なフレーム解析情報が欠如

3. **アプリケーション状態情報の欠落**
   - セッション情報、設定詳細、サイクル情報
   - 処理状況、統計情報、エラー詳細

## ハイブリッド統合ログシステム設計

### 設計原則
1. **目的別ファイル分離**:
   - `rawdata_analysis.log`: SLMP技術詳細・通信・診断情報
   - `console_output.json`: 人間可読コンソール出力・進行状況
2. **構造化データ**: エントリータイプ別の分類で見やすさを確保
3. **情報の完全性**: 全ての情報を適切なファイルに分類保存
4. **検索性**: SessionId、CycleNumber等による関連付け
5. **相互連携**: 両ログファイル間での情報関連付け

### エントリータイプ分類（2ステップフロー対応拡張）

#### 既存エントリタイプ（6ステップ・2ステップ共通）
1. **SESSION_START** - セッション開始情報
2. **CYCLE_START** - サイクル開始情報
3. **CYCLE_COMMUNICATION** - 通信実行詳細
4. **ERROR_OCCURRED** - エラー発生詳細
5. **STATISTICS** - 統計・サマリー情報
6. **PERFORMANCE_METRICS** - パフォーマンス詳細
7. **SESSION_END** - セッション終了情報

#### 🆕 2ステップフロー専用エントリタイプ
8. **SIMPLE_MONITORING_START** - SimpleMonitoringService開始
9. **FIXED_RANGE_COMMUNICATION** - 固定範囲（M/D）通信詳細
10. **DEVICE_BATCH_PROCESSING** - デバイスバッチ処理詳細
11. **MEMORY_OPTIMIZATION_METRICS** - メモリ最適化統計

---

#### 1. SESSION_START - セッション開始情報（2ステップフロー対応）
```json
{
  "EntryType": "SESSION_START",
  "Timestamp": "2025-10-06T11:47:27.527+09:00",
  "SessionId": "session_20251006_114727",
  "SessionInfo": {
    "ProcessId": 13296,
    "ApplicationName": "Andon SLMP Client - 2ステップフロー対応",
    "Version": "2.1.0-simple-monitoring",
    "Environment": "Production",
    "MonitoringMode": "SimpleMonitoring"
  },
  "ConfigurationDetails": {
    "ConfigFile": "appsettings.json",
    "ConnectionTarget": "172.30.40.15:8192",
    "SlmpSettings": "Port:8192, Binary, Version4E, UDP, RxTimeout:3000ms, ConnTimeout:10000ms, MaxReq:2",
    "MonitoringType": "2ステップフロー（固定範囲）",
    "TargetDevices": "M000-M999, D000-D999",
    "ContinuityMode": "ReturnDefaultAndContinue",
    "RawDataLogging": "有効",
    "LogOutputPath": "logs/rawdata_analysis.log",
    "MemoryOptimization": "有効（450KB制限）"
  }
}
```

#### 2. CYCLE_START - サイクル開始情報（2ステップフロー対応）
```json
{
  "EntryType": "CYCLE_START",
  "Timestamp": "2025-10-06T11:47:27.655+09:00",
  "SessionId": "session_20251006_114727",
  "CycleNumber": 1,
  "CycleInfo": {
    "StartMessage": "--- 2ステップフローサイクル 1 ---",
    "MonitoringType": "SimpleMonitoring",
    "TargetDevices": ["M000-M999", "D000-D999"],
    "IntervalFromPrevious": 1000.0,
    "ExpectedOperations": 2
  }
}
```

#### 3. CYCLE_COMMUNICATION - 通信実行詳細（2ステップフロー対応）
```json
{
  "EntryType": "CYCLE_COMMUNICATION",
  "Timestamp": "2025-10-06T11:47:30.676+09:00",
  "SessionId": "session_20251006_114727",
  "CycleNumber": 1,
  "PhaseInfo": {
    "Phase": "FixedRangeRead",
    "StatusMessage": "M000-M999固定範囲読み取り中...",
    "DeviceRange": "M000-M999",
    "BatchSize": 128
  },
  "CommunicationDetails": {
    "OperationType": "FixedRangeBitDeviceRead",
    "DeviceCode": "M",
    "StartAddress": 0,
    "DeviceCount": 1000,
    "BatchProcessing": true,
    "BatchesCompleted": 8,
    "TotalBatches": 8,
    "Values": "Array[1000] - M000-M999データ",
    "ResponseTimeMs": 1200.45,
    "Success": true,
    "MemoryUsage": "28KB",
    "OptimizationApplied": "ArrayPool+FixedRangeProcessor"
  },
  "RawDataAnalysis": {
    "RequestFrameHex": "5400000000FF03000C001400010400000000010001000064000000",
    "ResponseFrameHex": "D4000000000300020000000000000000",
    "HexDump": "00000000: D4 00 00 00 00 03 00 02  00 00 00 00 00 00 00 00 |................|\n",
    "FrameAnalysis": {
      "SubHeader": "0x00D4",
      "SubHeaderDescription": "4Eフレーム",
      "NetworkNumber": 0,
      "PcNumber": 3,
      "UnitIONumber": "0x0002",
      "UnitStationNumber": 0,
      "ResponseDataLength": 2,
      "EndCode": "0x0000",
      "EndCodeDescription": "正常終了",
      "DataSection": "0000000000000000"
    },
    "DataSectionAnalysis": {
      "DataType": "BitDeviceData",
      "Details": [
        {
          "Byte": 0,
          "Value": "0x00",
          "Binary": "00000000",
          "Bits": "All OFF"
        },
        {
          "Byte": 1,
          "Value": "0x00",
          "Binary": "00000000",
          "Bits": "All OFF"
        }
      ]
    }
  },
  "ProcessingResult": {
    "InterpretedValue": "センサー状態: [False, False, False, False, False, False, False, False]",
    "ProcessingStatus": "✓ データ読み取り成功 - 正常処理継続",
    "LogMessages": [
      "✓ 通信成功: M100 読み取り完了",
      "  値: [OFF, OFF, OFF, OFF, OFF, OFF, OFF, OFF]",
      "  Binary: [00000000]",
      "  応答時間: 3017.37ms"
    ]
  }
}
```

#### 4. ERROR_OCCURRED - エラー発生時詳細
```json
{
  "EntryType": "ERROR_OCCURRED",
  "Timestamp": "2025-09-26T14:25:12.123+09:00",
  "SessionId": "session_20250926_114727",
  "CycleNumber": 25,
  "ErrorDetails": {
    "ErrorType": "CommunicationTimeout",
    "ErrorMessage": "SocketException - 接続がタイムアウトしました",
    "DeviceAddress": "M100",
    "OperationType": "BitDeviceRead",
    "AttemptCount": 3,
    "ResponseTimeMs": 3000.0,
    "ContinuityAction": "デフォルト値で継続中",
    "EstimatedCause": "一時的なネットワーク遅延"
  },
  "RecoveryInfo": {
    "AutoRecoveryEnabled": true,
    "RecoveryStatus": "自動回復試行中...",
    "DefaultValueReturned": [false, false, false, false, false, false, false, false]
  }
}
```

#### 5. STATISTICS - 統計・サマリー情報
```json
{
  "EntryType": "STATISTICS",
  "Timestamp": "2025-09-26T11:47:43.210+09:00",
  "SessionId": "session_20250926_114727",
  "StatisticsType": "SESSION_SUMMARY",
  "StatisticsInfo": {
    "ExecutedCycles": 2,
    "TotalCommunications": 4,
    "SuccessfulCommunications": 4,
    "FailedCommunications": 0,
    "SuccessRate": "100%",
    "AverageResponseTime": 2581.6,
    "MinResponseTime": 0.35,
    "MaxResponseTime": 3017.37,
    "TotalExecutionTime": "00:00:15.583",
    "CommunicationsByType": {
      "BitDeviceRead": 2,
      "WordDeviceRead": 2
    }
  },
  "SystemStatus": {
    "RawDataLogging": "有効",
    "ContinuousMonitoring": "有効 (Ctrl+C で停止)",
    "FinalStatus": "✓ システムは全期間を通じて動作し続けました！",
    "FinalMessage": "製造ラインの稼働を止めることなく監視が完了しました。"
  }
}
```

#### 6. PERFORMANCE_METRICS - パフォーマンス詳細
```json
{
  "EntryType": "PERFORMANCE_METRICS",
  "Timestamp": "2025-09-26T11:47:43.210+09:00",
  "SessionId": "session_20250926_114727",
  "PerformanceData": {
    "NetworkQuality": {
      "PacketLoss": 0,
      "AverageLatency": 2.3,
      "MaxLatency": 24.3,
      "ConnectionStability": "Excellent"
    },
    "SlmpPerformance": {
      "FrameProcessingTime": 0.5,
      "DataParsingTime": 0.2,
      "ResponseValidationTime": 0.1
    },
    "SystemResource": {
      "MemoryUsage": "15.2MB",
      "CpuUsage": "2.1%",
      "ThreadCount": 5
    }
  }
}
```

#### 7. SESSION_END - セッション終了情報
```json
{
  "EntryType": "SESSION_END",
  "Timestamp": "2025-10-06T11:47:43.213+09:00",
  "SessionId": "session_20251006_114727",
  "SessionSummary": {
    "Duration": "00:00:15.686",
    "FinalStatus": "正常終了",
    "ExitReason": "ユーザー停止要求 (Ctrl+C)",
    "TotalLogEntries": 52,
    "MonitoringMode": "2ステップフロー",
    "ProcessedDevices": "M000-M999, D000-D999",
    "MemoryPeakUsage": "450KB",
    "FinalMessage": "2ステップフロー監視セッション終了"
  }
}
```

---

## 🆕 2ステップフロー専用エントリタイプ詳細

#### 8. SIMPLE_MONITORING_START - SimpleMonitoringService開始
```json
{
  "EntryType": "SIMPLE_MONITORING_START",
  "Timestamp": "2025-10-06T11:47:28.123+09:00",
  "SessionId": "session_20251006_114727",
  "ServiceInfo": {
    "ServiceName": "SimpleMonitoringService",
    "Version": "2.1.0",
    "MonitoringMode": "FixedRange",
    "TargetDevices": {
      "MDeviceRange": "M000-M999 (1000デバイス)",
      "DDeviceRange": "D000-D999 (1000デバイス)"
    },
    "OptimizationSettings": {
      "MemoryOptimizer": "有効",
      "ArrayPool": "有効",
      "FixedRangeProcessor": "有効",
      "ExpectedMemoryUsage": "450KB以下"
    },
    "MonitoringInterval": 1000,
    "StartMessage": "2ステップフロー監視開始"
  }
}
```

#### 9. FIXED_RANGE_COMMUNICATION - 固定範囲（M/D）通信詳細
```json
{
  "EntryType": "FIXED_RANGE_COMMUNICATION",
  "Timestamp": "2025-10-06T11:47:29.456+09:00",
  "SessionId": "session_20251006_114727",
  "CycleNumber": 1,
  "FixedRangeDetails": {
    "DeviceType": "BitDevice",
    "DeviceCode": "M",
    "RangeDefinition": {
      "StartAddress": 0,
      "EndAddress": 999,
      "TotalCount": 1000
    },
    "BatchProcessing": {
      "OptimalBatchSize": 128,
      "TotalBatches": 8,
      "ProcessingMode": "Parallel"
    },
    "Performance": {
      "ProcessingTimeMs": 1200.45,
      "MemoryUsedKB": 28,
      "ArrayPoolHits": 8,
      "GCCollections": 0
    },
    "Results": {
      "SuccessfulReads": 1000,
      "FailedReads": 0,
      "NonZeroValues": 0,
      "ProcessingStatus": "完全成功"
    }
  }
}
```

#### 10. DEVICE_BATCH_PROCESSING - デバイスバッチ処理詳細
```json
{
  "EntryType": "DEVICE_BATCH_PROCESSING",
  "Timestamp": "2025-10-06T11:47:30.789+09:00",
  "SessionId": "session_20251006_114727",
  "CycleNumber": 1,
  "BatchDetails": {
    "BatchId": "batch_M_001",
    "DeviceCode": "M",
    "BatchRange": {
      "StartAddress": 0,
      "Count": 128,
      "EndAddress": 127
    },
    "ProcessingInfo": {
      "BufferSize": "1024 bytes",
      "ArrayPoolUsed": true,
      "MemoryOptimized": true,
      "ProcessingTimeMs": 150.23
    },
    "CommunicationFrame": {
      "RequestSize": 32,
      "ResponseSize": 48,
      "FrameType": "4E",
      "EndCode": "0x0000"
    },
    "BatchResult": {
      "Success": true,
      "ValuesRead": 128,
      "NonZeroCount": 0,
      "ProcessingStatus": "正常完了"
    }
  }
}
```

#### 11. MEMORY_OPTIMIZATION_METRICS - メモリ最適化統計
```json
{
  "EntryType": "MEMORY_OPTIMIZATION_METRICS",
  "Timestamp": "2025-10-06T11:47:35.012+09:00",
  "SessionId": "session_20251006_114727",
  "CycleNumber": 1,
  "MemoryMetrics": {
    "CurrentUsage": {
      "TotalMemoryKB": 445,
      "ArrayPoolUsageKB": 256,
      "FixedRangeBuffersKB": 128,
      "ConnectionPoolKB": 61
    },
    "OptimizationStats": {
      "ArrayPoolHitRate": "98.5%",
      "MemoryReusedBytes": 1048576,
      "GCPrevented": 15,
      "BufferAllocationsAvoided": 32
    },
    "Performance": {
      "AllocationSpeedupPercent": 92,
      "MemoryFootprintReduction": "99.96%",
      "GCPressureReduction": "98%"
    },
    "Comparison": {
      "BeforeOptimization": "10.2MB",
      "AfterOptimization": "445KB",
      "ImprovementFactor": "22.9x"
    }
  }
}
```

## 含まれる全情報カテゴリー

### 1. SLMPフレーム生バイナリデータ
- **送信フレーム**: 完全な16進数表現
- **受信フレーム**: 完全な16進数表現
- **16進数ダンプ**: アドレス付き、ASCII表現付き
- **フレーム解析**: ヘッダー、終了コード、データ部詳細

### 2. 詳細SLMPフレーム解析
- **サブヘッダー解析**: 3E/4Eフレーム判定
- **ネットワーク情報**: ネットワーク番号、PC番号、ユニット情報
- **終了コード詳細**: エラー原因の具体的説明
- **データ部解析**: ワード/ビット別の詳細解析

### 3. アプリケーション状態情報
- **セッション情報**: 開始/終了、プロセスID、実行時間
- **設定情報詳細**: SLMP設定、継続モード、タイムアウト等
- **サイクル情報**: サイクル番号、フェーズ、間隔
- **処理状況**: リアルタイムメッセージ、状態変化

### 4. 統計・パフォーマンス情報
- **実行統計**: サイクル数、成功率、失敗率
- **応答時間分析**: 平均、最大、最小、分布
- **エラー統計**: エラー種別、頻度、回復状況
- **システム稼働状況**: リソース使用量、パフォーマンス

### 5. 設定情報の詳細
- **SLMP接続設定**: 全パラメータの詳細
- **継続動作設定**: エラーハンドリング、デフォルト値
- **タイムアウト・リトライ設定**: 全設定値
- **ログ設定**: 出力先、レベル、ローテーション

### 6. エラー・例外情報の詳細
- **エラー分類**: 通信エラー、タイムアウト、設定エラー等
- **継続機能動作**: エラー処理、デフォルト値返却
- **リトライ処理**: 試行回数、間隔、結果
- **回復処理**: 自動回復、状態復旧

### 7. 時系列・関連情報
- **SessionId**: 関連ログのグループ化
- **CycleNumber**: サイクル内の処理順序
- **タイムスタンプ**: ミリ秒精度の時刻情報
- **処理間隔**: フェーズ間、サイクル間の時間

### 8. デバイス解釈情報
- **生データ**: バイナリ、16進数、数値表現
- **解釈結果**: 人間が読める形式への変換
- **ステータス判定**: ON/OFF、正常/異常等
- **変化検出**: 前回値との比較、変化通知

### 9. 診断情報（拡張準備）
- **PLC接続診断**: 接続状態、品質評価
- **ネットワーク診断**: 到達性、レスポンス品質
- **通信品質分析**: パケットロス、遅延分析
- **異常検出**: パターン分析、予兆検出

## 実装フェーズプラン

### Phase 1: JSON出力廃止とログ統合基盤
1. **設定変更**
   - `appsettings.json`で`EnableJsonExport = false`
   - JSON関連処理の削除

2. **統合ログ基盤実装**
   - エントリータイプ別出力構造
   - SessionId生成・管理
   - CycleNumber追跡

### Phase 2: 欠落情報の実装
1. **SLMPフレーム生データ取得**
   - `SlmpClientWithTestLogging`での実際のrawData取得
   - 送信・受信両方のフレーム記録
   - `SlmpRawDataAnalyzer`の正常動作

2. **詳細フレーム解析統合**
   - 既存の`SlmpRawDataAnalyzer`機能をログに統合
   - エラーコード詳細説明
   - データ部の完全解析

### Phase 3: ターミナル情報の完全統合
1. **アプリケーション状態統合**
   - `ContinuityExample.cs`の拡張
   - セッション管理、設定情報出力
   - サイクル・フェーズ情報統合

2. **統計・パフォーマンス統合**
   - リアルタイム統計計算
   - パフォーマンスメトリクス追加
   - システム稼働状況監視

### Phase 4: 見やすさとパフォーマンス向上
1. **出力最適化**
   - 構造化されたJSON形式での出力
   - 情報レベル別の制御
   - ファイルサイズ管理

2. **検索・分析機能**
   - エントリータイプ別フィルタ
   - SessionId/CycleNumber検索
   - 統計データ抽出

## 期待される効果

### 1. 情報の完全性
- **現在30% → 100%**: 全ての情報が単一ファイルで確認可能
- **データロスなし**: ターミナル出力の全情報を保持
- **診断情報**: 問題発生時の完全な状況把握

### 2. 運用効率の向上
- **単一ファイル管理**: ファイル数の削減（3ファイル → 1ファイル）
- **検索性向上**: 構造化データでの高速検索
- **トラブルシューティング**: 問題原因の迅速な特定

### 3. 将来拡張性
- **診断機能追加**: PLC接続診断の統合準備
- **分析機能**: 統計データの自動分析
- **監視機能**: リアルタイム異常検出

## 実装対象ファイル

### A. 既存ファイル拡張
1. **ContinuityExample.cs**: セッション管理、統計出力の追加
2. **RealMachineTestLogger.cs**: 統合ログ出力の実装
3. **SlmpClientWithTestLogging.cs**: 生データ取得の実装
4. **appsettings.json**: JSON出力無効化

### B. 新規作成（必要に応じて）
1. **UnifiedLogWriter.cs**: 統合ログ出力専用クラス
2. **SessionManager.cs**: セッション管理クラス
3. **PerformanceMonitor.cs**: パフォーマンス監視クラス

## 出力ファイル構成（ハイブリッド統合後）

### 技術詳細ログファイル
- **rawdata_analysis.log**: SLMP技術詳細情報統合ファイル
  - セッション情報
  - 通信詳細（生データ含む）
  - フレーム解析詳細
  - アプリケーション状態
  - 統計・パフォーマンス情報
  - エラー・診断情報

### 人間可読コンソール出力ファイル
- **console_output.json**: 構造化コンソール出力ファイル
  - システム起動・終了メッセージ
  - 6ステップフロー進行状況
  - ユーザー向け実行結果
  - エラー・警告メッセージ
  - 実行サマリー情報

### 📊 出力ファイル仕様更新（2025年10月2日）

#### 統一後の出力ファイル構成
1. **logs/rawdata_analysis.log** - 統合ログファイル（メイン）
2. **logs/rawdata_analysis.json** - JSON構造化ログ（復活・必須）
3. **logs/terminal_output.txt** - ターミナル出力ファイル（統一）

#### 出力ファイル統一方針
- **terminal_output.txt形式への統一**: `console_output.json` → `terminal_output.txt`
- **JSON出力の復活**: 運用性向上のため `rawdata_analysis.json` を必須ファイルとして復活
- **バッチファイル期待値との整合**: `run_rawdata_logging.bat` で期待されるファイル名に統一

#### 修正された設定項目
```json
// appsettings.json - 修正後の設定
{
  "UnifiedLoggingSettings": {
    "LogFilePath": "logs/rawdata_analysis.log",
    "JsonExportPath": "logs/rawdata_analysis.json",
    "EnableJsonExport": true
  },
  "IntegratedOutput": {
    "OutputFilePath": "logs/terminal_output.txt",
    "EnableOutput": true,
    "OutputFormat": "text"
  }
}
```

### 削除・移行対象ファイル（更新）
- ~~console_output.json~~ → **terminal_output.txt**に統一
- **rawdata_analysis.json** - 統合により削除予定だったが、運用性向上のため復活

## コンソール出力JSON保存システム詳細設計

### 📋 コンソール出力エントリータイプ定義

#### 1. CONSOLE_INFO - 一般情報出力
```json
{
  "EntryType": "CONSOLE_INFO",
  "Timestamp": "2025-10-02T10:30:15.123+09:00",
  "SessionId": "session_20251002_103015",
  "Level": "Information",
  "Category": "SystemStatus",
  "Message": "✅ 依存性注入設定完了 - IntelligentMonitoringSystem準備完了",
  "Context": {
    "StepNumber": null,
    "PhaseInfo": "Initialization"
  }
}
```

#### 2. CONSOLE_PROGRESS - 進行状況出力
```json
{
  "EntryType": "CONSOLE_PROGRESS",
  "Timestamp": "2025-10-02T10:30:20.456+09:00",
  "SessionId": "session_20251002_103015",
  "Level": "Information",
  "Category": "StepProgress",
  "Message": "🚀 6ステップフロー実行開始",
  "Context": {
    "StepNumber": 1,
    "PhaseInfo": "Step1_ConfigurationLoad",
    "ProgressPercentage": 16.7
  }
}
```

#### 3. CONSOLE_RESULT - 実行結果出力
```json
{
  "EntryType": "CONSOLE_RESULT",
  "Timestamp": "2025-10-02T10:30:45.789+09:00",
  "SessionId": "session_20251002_103015",
  "Level": "Information",
  "Category": "StepResult",
  "Message": "Step 4完了: 総スキャン数 45,056個, アクティブ 0個を検出",
  "Context": {
    "StepNumber": 4,
    "PhaseInfo": "Step4_DeviceScan",
    "ResultData": {
      "TotalScanned": 45056,
      "ActiveDevices": 0,
      "ScanDuration": "2.5s"
    }
  }
}
```

#### 4. CONSOLE_ERROR - エラー出力
```json
{
  "EntryType": "CONSOLE_ERROR",
  "Timestamp": "2025-10-02T10:30:25.234+09:00",
  "SessionId": "session_20251002_103015",
  "Level": "Error",
  "Category": "Communication",
  "Message": "❌ Step 2でReadTypeName失敗、フォールバック処理を実行",
  "Context": {
    "StepNumber": 2,
    "PhaseInfo": "Step2_DeviceInfo",
    "ErrorDetails": {
      "ErrorType": "SlmpCommunicationException",
      "FallbackAction": "Q00CPU推定で継続"
    }
  }
}
```

#### 5. CONSOLE_HEADER - セクションヘッダー出力
```json
{
  "EntryType": "CONSOLE_HEADER",
  "Timestamp": "2025-10-02T10:30:10.000+09:00",
  "SessionId": "session_20251002_103015",
  "Level": "Information",
  "Category": "SystemHeader",
  "Message": "SLMP インテリジェント監視システム v2.0",
  "Context": {
    "HeaderType": "ApplicationTitle",
    "Version": "2.0",
    "SubTitle": "全39デバイス対応・完全探索システム"
  }
}
```

### 🏗️ ConsoleOutputManager 実装設計

#### クラス構造
```csharp
namespace SlmpClient.Core
{
    public class ConsoleOutputManager : IAsyncDisposable
    {
        private readonly ILogger<ConsoleOutputManager> _logger;
        private readonly string _outputFilePath;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Channel<ConsoleEntry> _outputQueue;
        private readonly SemaphoreSlim _writeSemaphore;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Task? _backgroundWriter;

        public ConsoleOutputManager(ILogger<ConsoleOutputManager> logger,
                                   IConfiguration configuration);

        public Task WriteInfoAsync(string message, string category = "General",
                                  int? stepNumber = null, object? context = null);
        public Task WriteProgressAsync(string message, int stepNumber,
                                      string phaseInfo, double? progressPercentage = null);
        public Task WriteResultAsync(string message, int stepNumber,
                                    string phaseInfo, object? resultData = null);
        public Task WriteErrorAsync(string message, string category = "General",
                                   int? stepNumber = null, object? errorDetails = null);
        public Task WriteHeaderAsync(string message, string headerType,
                                    object? context = null);

        private Task ProcessOutputQueueAsync();
        private async Task WriteEntryToFileAsync(ConsoleEntry entry);
    }
}
```

#### ConsoleEntry データ構造
```csharp
namespace SlmpClient.Core
{
    public class ConsoleEntry
    {
        public string EntryType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string SessionId { get; set; } = string.Empty;
        public string Level { get; set; } = "Information";
        public string Category { get; set; } = "General";
        public string Message { get; set; } = string.Empty;
        public ConsoleContext? Context { get; set; }
    }

    public class ConsoleContext
    {
        public int? StepNumber { get; set; }
        public string? PhaseInfo { get; set; }
        public double? ProgressPercentage { get; set; }
        public object? ResultData { get; set; }
        public object? ErrorDetails { get; set; }
        public string? HeaderType { get; set; }
        public string? Version { get; set; }
        public string? SubTitle { get; set; }
    }
}
```

### 📝 Program.cs統合実装

#### Console.WriteLine置き換え実装
```csharp
public class Program
{
    private static ConsoleOutputManager? _consoleOutputManager;

    public static async Task<int> Main(string[] args)
    {
        // ConsoleOutputManager初期化
        var config = LoadConfiguration();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _consoleOutputManager = new ConsoleOutputManager(
            loggerFactory.CreateLogger<ConsoleOutputManager>(), config);

        // ヘッダー出力（既存のConsole.WriteLineを置き換え）
        await _consoleOutputManager.WriteHeaderAsync(
            "SLMP インテリジェント監視システム v2.0",
            "ApplicationTitle",
            new { Version = "2.0", SubTitle = "全39デバイス対応・完全探索システム" });

        // 通常のコンソール出力も同時実行
        Console.WriteLine("===================================================");
        Console.WriteLine("    SLMP インテリジェント監視システム v2.0");
        Console.WriteLine("    全39デバイス対応・完全探索システム");
        Console.WriteLine("===================================================");

        try
        {
            await _consoleOutputManager.WriteInfoAsync(
                "実行モード: IntelligentMonitoring (6ステップフロー)", "SystemMode");
            Console.WriteLine("実行モード: IntelligentMonitoring (6ステップフロー)");

            await RunIntelligentMonitoringAsync(config, loggerFactory);

            await _consoleOutputManager.WriteInfoAsync("実行完了", "SystemStatus");
            Console.WriteLine("実行完了");

            return 0;
        }
        catch (Exception ex)
        {
            await _consoleOutputManager.WriteErrorAsync(
                $"エラーが発生しました: {ex.Message}", "SystemError",
                errorDetails: new { ExceptionType = ex.GetType().Name, StackTrace = ex.StackTrace });
            Console.WriteLine($"❌ エラーが発生しました: {ex.Message}");
            return 1;
        }
        finally
        {
            await _consoleOutputManager.DisposeAsync();
        }
    }
}
```

### ⚙️ appsettings.json設定拡張

```json
{
  "PlcConnection": {
    // 既存設定...
  },
  "ConsoleOutputSettings": {
    "EnableCapture": true,
    "OutputFilePath": "logs/console_output.json",
    "OutputLevel": "Information",
    "EnableFileRotation": true,
    "MaxFileSizeMB": 10,
    "MaxFileCount": 5,
    "FlushIntervalMs": 1000
  },
  "UnifiedLoggingSettings": {
    // 既存統合ログ設定...
  }
}
```

### 🧪 完全実装・テスト手順

#### Phase 1: コアシステム実装
1. **ConsoleOutputManager.cs作成**
   ```bash
   # ファイル作成場所
   C:\Users\1010821\Desktop\python\andon\andon\Core\ConsoleOutputManager.cs
   ```

2. **ConsoleEntry.cs作成**
   ```bash
   # ファイル作成場所
   C:\Users\1010821\Desktop\python\andon\andon\Core\ConsoleEntry.cs
   ```

3. **設定クラス追加**
   ```bash
   # ファイル作成場所
   C:\Users\1010821\Desktop\python\andon\andon\Core\ConsoleOutputSettings.cs
   ```

#### Phase 2: Program.cs統合
1. **依存性注入設定**
   - ServiceProvider にConsoleOutputManager追加
   - IConfiguration からConsoleOutputSettings読み込み
   - ログgerFactory との連携

2. **Console.WriteLine置き換え**
   - 既存出力箇所の特定（約20箇所）
   - 段階的置き換え実装
   - 元のコンソール出力も並行実行

#### Phase 3: IntelligentMonitoringSystem統合
1. **6ステップフロー統合**
   ```csharp
   // Step実行時の出力例
   await _consoleOutputManager.WriteProgressAsync(
       "Step 1実行: 設定ファイルからPLC接続情報を取得",
       1, "Step1_ConfigurationLoad", 16.7);

   await _consoleOutputManager.WriteResultAsync(
       "Step 1完了: PLC接続先='製造ラインPLC' (172.30.40.15:8192)",
       1, "Step1_ConfigurationLoad",
       new { ConnectionTarget = "製造ラインPLC", Host = "172.30.40.15", Port = 8192 });
   ```

2. **エラーハンドリング統合**
   ```csharp
   // エラー発生時の統合出力
   await _consoleOutputManager.WriteErrorAsync(
       "Step 2でReadTypeName失敗、フォールバック処理を実行",
       "Communication", 2,
       new {
           ErrorType = "SlmpCommunicationException",
           FallbackAction = "Q00CPU推定で継続",
           OriginalError = ex.Message
       });
   ```

#### Phase 4: テスト・検証
1. **単体テスト**
   ```bash
   # テスト実行
   dotnet test andon.Tests --filter "ConsoleOutputManager"
   ```

2. **統合テスト**
   ```bash
   # 全システム実行テスト
   cd C:\Users\1010821\Desktop\python\andon
   dotnet run

   # 出力ファイル確認
   cat logs/console_output.json
   cat logs/rawdata_analysis.log
   ```

3. **成功確認項目**
   - [ ] console_output.json生成
   - [ ] 全CONSOLE_*エントリタイプ記録
   - [ ] SessionId連携動作
   - [ ] ファイル競合エラーなし
   - [ ] 既存統合ログ動作継続
   - [ ] パフォーマンス影響なし

### 🔄 後日完全再現手順

#### 1. 環境準備
```bash
# 作業ディレクトリ確認
cd C:\Users\1010821\Desktop\python\andon

# バックアップ作成
cp andon/Program.cs andon/Program.cs.backup.20251002
cp andon/appsettings.json andon/appsettings.json.backup.20251002
```

#### 2. ファイル作成順序（重要）
1. **ConsoleEntry.cs** （データ構造定義）
2. **ConsoleOutputSettings.cs** （設定クラス）
3. **ConsoleOutputManager.cs** （メインクラス ）
4. **appsettings.json** （設定追加）
5. **Program.cs** （統合実装）

#### 3. 実装確認コマンド
```bash
# コンパイル確認
dotnet build

# 実行テスト
dotnet run

# ログファイル確認
ls -la logs/
head -20 logs/console_output.json
```

#### 4. 期待される最終成果物
```bash
logs/
├── rawdata_analysis.log          # 技術詳細ログ（既存）
├── console_output.json           # NEW: コンソール出力JSON
├── console_output_20251002.json  # ローテーション（必要に応じて）
└── intelligent_monitoring_log.log # 既存ログ（影響なし）
```

### 📊 ハイブリッド統合システムの利点

#### 技術者向け詳細情報（rawdata_analysis.log）
- SLMPフレーム生データ
- 通信エラー詳細
- パフォーマンス統計
- 診断情報

#### 運用者向け状況情報（console_output.json）
- システム実行状況
- 6ステップフロー進行
- エラー・警告サマリー
- 成功・失敗結果

#### 相互連携
- 同一SessionIdでの関連付け
- タイムスタンプ同期
- 問題発生時の両ログ参照

この設計により、技術的詳細と運用状況の両方を効率的に管理できるハイブリッド統合ログシステムが実現されます。