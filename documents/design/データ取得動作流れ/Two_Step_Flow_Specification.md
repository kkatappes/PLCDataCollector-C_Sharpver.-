# 2ステップフロー仕様書

## 概要

従来の複雑な6ステップフローから、シンプルで理解しやすい2ステップフローに変更したPLC通信システムの正式仕様書です。

## システム目標

1. **シンプル化**: 6ステップフローから2ステップフローへの大幅簡素化
2. **保守性向上**: 理解しやすく、メンテナンスしやすいアーキテクチャ
3. **確実な動作**: 固定デバイス範囲での安定した通信
4. **重要機能維持**: ログ出力、パフォーマンス監視、メモリ最適化機能は完全維持

## 2ステップフロー仕様

### Step 1: PLC接続決定
- 設定ファイル（appsettings.json）からPLC接続情報を読み込み
- 接続先PLC、通信方式、フレームバージョンを決定

### Step 2: 固定範囲データ取得
- PLCに接続し、以下の固定デバイス範囲のデータを継続取得：
  - **ビットデバイス**: M000-M999 (1000個)
  - **ワードデバイス**: D000-D999 (1000個)
- 設定ファイルで指定された間隔（デフォルト1000ms）で継続実行

## 技術仕様

### デバイス範囲
```
M000-M999: ビットデバイス（1000個）
D000-D999: ワードデバイス（1000個）
```

### 通信設定
- **プロトコル**: SLMP（Seamless Message Protocol）
- **フレームバージョン**: 3E/4E対応
- **通信方式**: TCP/UDP対応
- **データ形式**: Binary/ASCII対応
- **読み取り間隔**: 設定可能（デフォルト1000ms）

### 設定ファイル構成
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

## 維持される重要機能

### 1. 統合ログシステム（完全維持）
- **UnifiedLogWriter**: 7種類エントリタイプ対応
  - SESSION_START: セッション開始情報
  - CYCLE_START: サイクル開始情報
  - CYCLE_COMMUNICATION: 通信実行詳細（生データ含む）
  - ERROR_OCCURRED: エラー発生詳細
  - STATISTICS: 統計情報
  - PERFORMANCE_METRICS: パフォーマンス情報
  - SESSION_END: セッション終了情報

### 2. 生データ記録機能（完全維持）
- **SlmpRawDataRecorder**: SLMPフレーム16進ダンプ出力
- **M000-M999, D000-D999の全データ記録**
- **構造化JSONログ**と**視覚的ターミナル表示**の両立

### 3. パフォーマンス監視（完全維持）
- **PerformanceMonitor**: パフォーマンス監視システム
- **NetworkQualityMonitor**: ネットワーク品質監視
- **応答時間、通信統計の継続記録**

### 4. メモリ最適化（完全維持）
- **MemoryOptimizer**: メモリ最適化機能
- **SlmpMemoryMonitor**: SLMP専用メモリ監視
- **PseudoDwordSplitter**: ワードデータ分割機能

### 5. データ処理システム（完全維持）
- **StreamingFrameProcessor**: ストリーミングフレーム処理
- **ChunkProcessor**: チャンク処理
- **CompressionEngine**: 圧縮エンジン
- **SlmpConnectionPool**: 接続プール

## アプリケーション実装仕様

### メインコンポーネント
```csharp
public class SimpleMonitoringService
{
    // M000-M999の読み取り（固定範囲）
    // D000-D999の読み取り（固定範囲）
    // 設定間隔でのループ実行
    // UnifiedLogWriter使用（詳細ログ出力維持）
    // SlmpRawDataRecorder使用（16進ダンプ維持）
    // PerformanceMonitor使用（パフォーマンス監視維持）
    // PseudoDwordSplitter使用（ワードデータ分割維持）
    // MemoryOptimizer使用（メモリ最適化維持）
}
```

### 依存性注入構成
- **SlmpClient**: 基本SLMP通信
- **UnifiedLogWriter**: 統合ログシステム
- **PerformanceMonitor**: パフォーマンス監視
- **MemoryOptimizer**: メモリ最適化
- **SimpleMonitoringService**: 2ステップフロー実行サービス

## エラーハンドリング仕様

### 継続稼働優先モード
- **基本方針**: ReturnDefaultAndContinue
- **PLC接続失敗**: 自動再接続試行、ログ記録継続
- **部分読み取り失敗**: 該当デバイスはデフォルト値、他は継続
- **ログ書き込み失敗**: コンソール出力継続、エラー統計記録

### デフォルト値
- **ビットデバイス**: false
- **ワードデバイス**: 0

## ログ出力仕様

### ログファイル出力先
- **統合ログ**: `logs/rawdata_analysis.json`
- **ターミナル出力**: `logs/terminal_output.txt`
- **生データログ**: `logs/intelligent_monitoring_log.log`

### ログローテーション
- **最大ファイルサイズ**: 50MB
- **保持期間**: 14日間
- **自動圧縮**: 有効

## 実行方法

### 開発環境
```bash
cd C:\Users\1010821\Desktop\python\andon\andon
dotnet run
```

### 配布環境
```bash
# バッチファイル実行（推奨）
run_rawdata_logging.bat

# 直接実行
andon.exe
```

## パフォーマンス要件

### 処理性能
- **デバイス数**: 合計1998個（M999個 + D999個）
- **読み取り間隔**: 1000ms（設定可能）
- **応答時間**: 通常3000ms以内
- **メモリ使用量**: 最適化により従来比30%削減

### 通信要件
- **バッチサイズ**: 動的最適化
- **同時接続数**: 8（設定可能）
- **タイムアウト**: 受信3000ms、接続10000ms

## セキュリティ・安全性

### 通信セキュリティ
- **接続先固定**: 設定ファイルで明示的指定
- **タイムアウト設定**: 適切な通信タイムアウト
- **エラー継続**: システム停止を回避

### データ保護
- **ログファイル**: ローカルディスクのみ
- **設定ファイル**: 暗号化なし（現場で編集可能）
- **ネットワーク**: PLC専用ネットワーク想定

## 品質保証

### テスト要件
- **単体テスト**: SimpleMonitoringService
- **統合テスト**: 2ステップフロー全体
- **パフォーマンステスト**: 1998デバイス継続読み取り
- **エラーハンドリングテスト**: 接続断、部分エラー対応

### 開発手法
- **TDD**: Red-Green-Refactor サイクル
- **SOLID原則**: 全コードで適用
- **依存性注入**: コンストラクタベース
- **非同期プログラミング**: async/await with CancellationToken

## 制限事項

### 対応デバイス
- **固定範囲のみ**: M000-M999, D000-D999
- **動的探索なし**: デバイス自動発見機能は削除
- **PLC型式判定なし**: 設定ファイルで明示的指定

### 運用制限
- **同時実行不可**: 単一プロセスのみ
- **設定変更**: アプリケーション再起動必須
- **ログ管理**: 手動ローテーション・削除

## 互換性

### 後方互換性
- **ログファイル形式**: 既存形式を維持
- **設定ファイル**: 一部セクション削除のみ
- **実行ファイル**: 同一インターフェース

### 移行要件
- **設定ファイル更新**: 6ステップ関連設定削除
- **バッチファイル**: 変更なし
- **ログ監視ツール**: 既存ツール継続利用可能

---

*この仕様書は、6ステップフローから2ステップフローへの変更における正式な技術仕様を定義しています。実装時はこの仕様に従い、TDD手法により品質を確保してください。*