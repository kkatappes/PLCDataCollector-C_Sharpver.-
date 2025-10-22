# 6ステップ→2ステップフロー移行計画書

## 概要

現在の複雑な6ステップフロー実行システムを、シンプルな2ステップ動作に変更する総合移行計画書です。

### 変更方針
- **変更前**: 複雑な6ステップフロー（機器判定→39種類デバイス探索→継続監視）
- **変更後**: シンプルな2ステップ動作（PLC接続→固定範囲データ取得）

### 目標
1. 6ステップフローから2ステップフローへの変更
2. 保守性の向上
3. 理解しやすいシンプルな動作
4. 固定デバイス範囲（M000-M999, D000-D999）での確実な動作
5. **重要**: 詳細なログ出力、メモリ削減、ワードデータ分割機能は維持

## 現在の複雑な機能分析

### 🔴 削除対象の機能（6ステップフロー関連のみ）

#### 6ステップフロー実行システム
1. **IntelligentMonitoringSystem** - 6ステップフロー実行エンジン
2. **DeviceDiscoveryManager** - 39種類デバイス対応探索システム
3. **AdaptiveMonitoringManager** - 適応型監視管理

#### デバイス判定・探索システム
4. **TypeCode特定設定** - FX/Q/R/Lシリーズ別設定
5. **DeviceCompatibilityMatrix** - デバイス互換性マトリックス
6. **CompleteDeviceMap** - 完全デバイスマップ
7. **SessionManager** - セッション管理
8. **SixStepFlowModels** - 6ステップフロー関連モデル
9. **DeviceDiscoveryModels** - デバイス探索関連モデル

#### 複雑な依存性注入（部分的）
10. **複雑な依存性注入システムの一部** - 6ステップフロー専用のサービス登録部分

## 残す機能（2ステップフロー + 既存の有用機能）

### 🟢 核心通信機能
1. **SlmpClient** - 基本SLMP通信
2. **SlmpTcpTransport/SlmpUdpTransport** - TCP/UDP通信
3. **SlmpRequestBuilder** - リクエスト構築
4. **SlmpResponseParser** - レスポンス解析

### 🟢 ログ・出力システム（維持）
5. **UnifiedLogWriter** - 統合ログシステム（7種類エントリタイプ）
6. **IntegratedOutputManager** - ターミナル・ファイル同期出力
7. **ConsoleOutputCapture** - コンソール出力キャプチャ
8. **SlmpRawDataRecorder** - SLMPフレーム16進ダンプ

### 🟢 パフォーマンス・監視システム（維持）
9. **PerformanceMonitor** - パフォーマンス監視
10. **NetworkQualityMonitor** - ネットワーク品質監視
11. **MemoryOptimizer** - メモリ最適化
12. **SlmpMemoryMonitor** - SLMP専用メモリ監視

### 🟢 データ処理システム（維持）
13. **StreamingFrameProcessor** - ストリーミングフレーム処理
14. **ChunkProcessor** - チャンク処理
15. **DataProcessor** - データ処理
16. **CompressionEngine** - 圧縮エンジン
17. **SlmpConnectionPool** - 接続プール
18. **PseudoDwordSplitter** - ワードデータ分割機能

### 🟢 基本サポート機能
19. **設定読み込み** - PLC接続情報 + ログ・監視設定
20. **基本エラーハンドリング** - SlmpException
21. **基本データ変換** - BcdConverter, BitConverter

### 🟢 定数・設定
22. **SlmpCommand** - SLMPコマンド定数
23. **DeviceCode** - デバイスコード定数
24. **EndCode** - エンドコード定数

## 新しいシンプル実装方針

### 2ステップ動作仕様
```
Step 1: 設定ファイルで接続するPLCを決定
Step 2: PLCに接続し、設定ファイルに従った間隔でM000-M999,D000-D999のデータを取得
```

### 実装アーキテクチャ

#### 1. 新しいProgram.cs
```csharp
// シンプルなメインプログラム
// - 設定ファイル読み込み（PLC接続情報のみ）
// - SlmpClientを直接インスタンス化
// - SimpleMonitoringServiceを実行
```

#### 2. 新しいSimpleMonitoringService
```csharp
public class SimpleMonitoringService
{
    // - M000-M999の読み取り（固定範囲）
    // - D000-D999の読み取り（固定範囲）
    // - 設定間隔でのループ実行
    // - UnifiedLogWriter使用（詳細ログ出力維持）
    // - SlmpRawDataRecorder使用（16進ダンプ維持）
    // - PerformanceMonitor使用（パフォーマンス監視維持）
    // - PseudoDwordSplitter使用（ワードデータ分割維持）
    // - MemoryOptimizer使用（メモリ最適化維持）
}
```

#### 3. 修正されたappsettings.json（重要機能は維持）
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
    "MaxCycles": 0,  // 0 = 無限ループ
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

## 設定ファイル調整

### 削除する設定セクション（6ステップフロー関連のみ）
- `DeviceDiscoverySettings` - デバイス探索設定
- `IntelligentMonitoringSettings` - 6ステップフロー設定
- `TypeCodeSpecificSettings` - 型コード別設定
- `ContinuitySettings` - 継続性設定

### 残す設定セクション（有用機能は維持）
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
    "LogLevel": "Trace",
    "EnableStructuredLogging": true
  },
  "ConsoleOutputSettings": {
    "EnableCapture": true,
    "OutputFilePath": "logs/terminal_output.txt"
  },
  "DiagnosticSettings": {
    "EnableDetailedDiagnostic": true,
    "EnableEnhancedHexDump": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SlmpClient": "Trace"
    }
  }
}
```

## 削除対象クラス一覧（6ステップフロー関連のみ）

### Core フォルダ（6ステップフロー専用）
- `IntelligentMonitoringSystem.cs` - 6ステップフロー実行エンジン
- `DeviceDiscoveryManager.cs` - 39種類デバイス探索システム
- `AdaptiveMonitoringManager.cs` - 適応型監視管理
- `SessionManager.cs` - セッション管理
- `CompleteDeviceMap.cs` - 完全デバイスマップ
- `DeviceCompatibilityMatrix.cs` - デバイス互換性マトリックス
- `ApplicationConfiguration.cs` - アプリケーション設定
- `SixStepFlowModels.cs` - 6ステップフロー関連モデル
- `DeviceDiscoveryModels.cs` - デバイス探索関連モデル

### 維持するCore フォルダ（有用機能）
- ✅ `UnifiedLogWriter.cs` - 統合ログシステム（維持）
- ✅ `IntegratedOutputManager.cs` - ターミナル・ファイル同期出力（維持）
- ✅ `ConsoleOutputCapture.cs` - コンソール出力キャプチャ（維持）
- ✅ `SlmpRawDataRecorder.cs` - SLMPフレーム16進ダンプ（維持）
- ✅ `NetworkQualityMonitor.cs` - ネットワーク品質監視（維持）
- ✅ `SlmpRawDataModels.cs` - 生データモデル（維持）

### 維持するUtils フォルダ（有用機能）
- ✅ `MemoryOptimizer.cs` - メモリ最適化（維持）
- ✅ `CompressionEngine.cs` - 圧縮エンジン（維持）
- ✅ `SlmpMemoryMonitor.cs` - SLMP専用メモリ監視（維持）
- ✅ `StreamingFrameProcessor.cs` - ストリーミングフレーム処理（維持）
- ✅ `ChunkProcessor.cs` - チャンク処理（維持）
- ✅ `SlmpConnectionPool.cs` - 接続プール（維持）
- ✅ `DataProcessor.cs` - データ処理（維持）
- ✅ `PseudoDwordSplitter.cs` - ワードデータ分割（維持）

### Tests フォルダ（6ステップフロー関連のみ削除）
削除対象:
- `Core/IntelligentMonitoringSystemFallbackTests.cs`
- `Core/SessionManagerTests.cs`

維持対象:
- ✅ `Core/UnifiedLogWriterTests.cs` - ログシステムテスト（維持）
- ✅ `MemoryOptimizationTests.cs` - メモリ最適化テスト（維持）
- ✅ `ConnectionPoolIntegrationTests.cs` - 接続プールテスト（維持）
- ✅ その他の有用機能のテストファイル（維持）

## 期待される効果

### フロー簡素化
- **変更前**: 6ステップフロー（機器判定→39種類デバイス探索→継続監視）
- **変更後**: 2ステップフロー（PLC接続→固定範囲データ取得）
- **削除対象**: 約10-15ファイル（6ステップフロー関連のみ）
- **維持機能**: ログ・監視・データ処理の有用機能は完全維持

### 保守性向上
- 理解しやすいシンプルなフロー
- 複雑なデバイス判定ロジックの排除
- 固定範囲での確実な動作

### 機能維持
- ✅ 詳細なログ出力機能（UnifiedLogWriter）
- ✅ パフォーマンス監視機能（PerformanceMonitor）
- ✅ メモリ最適化機能（MemoryOptimizer）
- ✅ ワードデータ分割機能（PseudoDwordSplitter）
- ✅ SLMPフレーム16進ダンプ（SlmpRawDataRecorder）

## 移行戦略

### リスク管理
1. **段階的移行**: Phase1→Phase2→Phase3の順次実行
2. **ロールバック準備**: 現在のコードのGitバックアップ保持
3. **テスト優先**: 各フェーズでのテスト実行必須

### 品質保証
1. **TDD実装**: Red-Green-Refactor サイクル遵守
2. **SOLID原則**: 全新規コードで適用
3. **既存機能テスト**: 維持機能の回帰テスト実行

### 検証項目
1. **機能検証**: M000-M999, D000-D999の正確な読み取り
2. **ログ検証**: 7種類エントリタイプの正常出力
3. **パフォーマンス検証**: 1000ms間隔での安定稼働
4. **エラーハンドリング検証**: 継続稼働機能の確認

## 具体的な実装手順

### Phase 1: 6ステップフロー機能の無効化
1. **Program.cs の修正**
   - IntelligentMonitoringSystem呼び出しを削除
   - SimpleMonitoringService呼び出しに変更
   - 依存性注入システムは必要な部分のみ残す（ログ・監視・データ処理用）

2. **appsettings.json の調整**
   - 6ステップフロー関連設定を削除
   - ログ・監視・データ処理設定は維持
   - MonitoringSettings を2ステップ用に調整

### Phase 2: 新しい2ステップ機能の実装
3. **SimpleMonitoringService.cs 作成**
   - M000-M999 読み取りメソッド実装
   - D000-D999 読み取りメソッド実装
   - 既存のログ・監視・データ処理システムを活用
   - UnifiedLogWriter, PerformanceMonitor, PseudoDwordSplitter等を統合

4. **既存の有用機能の継続利用**
   - 詳細なログ出力機能
   - パフォーマンス監視機能
   - メモリ最適化機能
   - ワードデータ分割機能

### Phase 3: 6ステップフロー専用ファイルの整理
5. **6ステップフロー専用クラスファイルの削除/移動**
   - Core/IntelligentMonitoringSystem.cs
   - Core/DeviceDiscoveryManager.cs
   - Core/AdaptiveMonitoringManager.cs
   - **注意**: UnifiedLogWriter.cs等の有用なファイルは残す

## 実行方法

### 開発環境
```batch
cd C:\Users\1010821\Desktop\python\andon\andon
dotnet run
```

### 配布環境
```batch
andon.exe
```

## 注意事項

1. **フロー変更**: 6ステップフローから2ステップフローに変更されます
2. **設定ファイル**: appsettings.jsonの6ステップフロー関連設定が変更されます
3. **ログ出力**: **詳細なログ出力機能は完全に維持されます**
4. **デバイス範囲**: M000-M999, D000-D999の固定範囲のみ対応
5. **有用機能維持**: メモリ最適化、パフォーマンス監視、ワードデータ分割等は完全維持

## 重要なポイント

**✅ 維持される機能**
- UnifiedLogWriter（統合ログシステム）
- SlmpRawDataRecorder（16進ダンプ）
- PerformanceMonitor（パフォーマンス監視）
- MemoryOptimizer（メモリ最適化）
- PseudoDwordSplitter（ワードデータ分割）
- その他全ての有用なデータ処理機能

**❌ 削除される機能**
- 6ステップフロー実行エンジン
- 39種類デバイス自動探索
- PLC型式自動判定システム

---

*この移行計画書は、6ステップフローから2ステップフローへの変更における総合的な移行戦略を定義しています。重要な機能（ログ・監視・データ処理）は完全に維持されます。*