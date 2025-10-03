# PLC接続診断統合プラン

## 概要
現在のSLMP継続監視アプリケーションに、詳細な接続診断機能を統合し、PLC接続状況とデバイスアクセス状況を常時監視・出力する機能を追加する。

## 背景・課題
### 現在の状況
- 通信状況: `"Success": true` で実際のPLC通信は成功
- データ状況: 全て0/false → PLCプログラム側でデータ未設定の可能性
- **問題**: 継続機能により真の接続失敗とデータ未設定の区別ができない

### 解決したい課題
以下の3つの状況を明確に切り分けたい：
1. **PLCオンライン + データ未設定** ← 現在の状況と推定
2. **PLCオフライン（継続機能でデフォルト値返却）**
3. **アプリ接続失敗（継続機能でデフォルト値返却）**

## 実装プラン

### 1. 起動時詳細診断フェーズ
#### 実装場所
`ContinuityExample.RunExample()` の開始直後に診断フェーズを追加

#### 出力例（Q00CPU対応・ハイブリッド統合版）
```
=== PLC接続詳細診断開始 ===
📡 ネットワーク接続テスト（Q00CPU対応）
  - UDP通信: 172.30.40.15:8192 → OK (15ms) [運用ポート]
  - UDP通信: 172.30.40.15:5007 → OK (12ms) [代替ポート]
  - ポート到達性: UDP対応確認済み
  - TCP診断: スキップ（Q00CPU非対応）
  - 継続機能無効での生通信: 成功

🏭 PLCシステム情報（Q00CPU検出）
  - CPU型式: Q00CPU (フォールバック推定)
  - CPU状態: 推定稼働中
  - SLMP通信バージョン: 4E (3Eフレームドロップ確認)
  - ReadTypeName: フォールバック処理で継続
  - 応答可能デバイス範囲確認: OK

📊 監視対象デバイス診断
  - M0-M127 アクセス: OK (全て初期値:OFF)
  - D0-D99 アクセス: OK (全て初期値:0)
  - デバイス実在性: 確認済み
  - データ設定状況: 未設定（PLCプログラム側要確認）

✅ 診断結果: Q00CPU正常接続、UDP+4E通信、デバイスアクセス可能、データ未設定
=== 継続監視開始 ===
```

#### ハイブリッド統合出力先
- **詳細診断データ** → `rawdata_analysis.log` (DIAGNOSTIC_NETWORKエントリ)
- **診断結果表示** → `console_output.json` + コンソール画面

### 2. 継続監視中のリアルタイム診断情報（Q00CPU・ハイブリッド統合対応）
#### 各データ取得サイクルで追加出力

```
--- サイクル 15 (2025-10-02 10:23:45) ---
🔗 ネットワーク状態（Q00CPU-UDP):
  - 接続状態: アクティブ (UDP:8192)
  - 応答時間: 2.3ms (良好)
  - 通信品質: 100% (エラー無し)

📡 SLMP通信詳細:
  - 送信フレーム: 4E ASCII (3Eはドロップ確認済み)
  - 受信フレーム: 正常 (終了コード: 0x0000)
  - データ整合性: OK
  - フォールバック処理: 0回

🎯 デバイスアクセス状況:
  - M0-M127: アクセス成功 (値変化: なし)
  - D0-D99: アクセス成功 (値変化: なし)
  - Q00CPU応答性: 正常

📊 統計情報:
  - 総通信回数: 30回
  - 成功率: 100%
  - 平均応答時間: 2.1ms
  - 最大応答時間: 24.3ms
  - UDP通信: 安定
```

#### ハイブリッド統合出力先
- **通信統計詳細** → `rawdata_analysis.log` (STATISTICS, PERFORMANCE_METRICSエントリ)
- **監視状況表示** → `console_output.json` + コンソール画面

### 3. エラー/異常時の詳細情報
#### 通信エラー発生時の出力例

```
❌ 通信エラー検出 (サイクル 25)
🔍 詳細診断:
  - エラー種別: タイムアウト
  - 発生時刻: 2025-09-26 14:25:12
  - 試行回数: 3回
  - 最終エラー: SocketException - 接続がタイムアウトしました
  - 継続機能: デフォルト値で継続中
  - 推定原因: 一時的なネットワーク遅延

🔄 自動回復試行中...
```

## 実装対象ファイル

### A. 新規作成ファイル

#### 1. `ConnectionDiagnostic.cs`
**責任**: 詳細診断ロジック
- ネットワーク到達性テスト
- PLC状態情報取得
- デバイス実在性確認
- 通信品質測定

#### 2. `CommunicationDashboard.cs`
**責任**: リアルタイム通信状況表示
- 通信統計情報管理
- 異常検出・通知
- パフォーマンスメトリクス

### B. 既存ファイル拡張

#### 1. `ContinuityExample.cs`
**追加機能:**
- 起動時診断フェーズの統合
- サイクル毎診断情報出力
- 統計情報表示とレポート

#### 2. `SlmpClientWithTestLogging.cs`
**追加機能:**
- 通信品質メトリクス追加
- エラー詳細情報取得
- SLMP フレーム解析情報

#### 3. `appsettings.json`（Q00CPU対応設定統合）
**追加設定セクション:**
```json
{
  "PlcConnection": {
    "HostName": "172.30.40.15",
    "Port": 8192,                    // Q00CPU運用ポート（5007も対応）
    "UseTcp": false,                 // Q00CPU: UDP必須
    "FrameVersion": "4E",            // Q00CPU: 4E必須（3Eドロップ）
    "IsBinary": false,               // ASCII推奨
    "Timeout": 10000,
    "NetworkNo": 0,
    "PCNo": 255,
    "RequestDestination": 1023,
    "RequestSource": 0
  },
  "DiagnosticSettings": {
    "EnableDetailedDiagnostic": true,
    "DiagnosticLevel": "Verbose",
    "ShowNetworkStats": true,
    "ShowDeviceDetails": true,
    "StatisticsInterval": 10,
    "EnableErrorAnalysis": true,
    "Q00CpuMode": true,              // Q00CPU特別対応有効
    "SkipTcpDiagnostic": true,       // TCP診断スキップ
    "AlternativePorts": [5007],      // 代替ポート（標準SLMP）
    "EnableFallbackMode": true       // フォールバック診断
  },
  "ConsoleOutputSettings": {
    "EnableCapture": true,
    "OutputFilePath": "logs/console_output.json",
    "EnableDiagnosticConsole": true
  }
}
```

## 出力先統合（ハイブリッド統合設計対応）

### 📅 更新情報
- **2025-09-26**: 初版作成
- **2025-10-02**: **重要更新** - ハイブリッド統合設計との整合性確保

### 設計原則更新
最新の「ハイブリッド統合ログシステム設計」に準拠し、診断情報を適切に分類・統合します。

### ターミナル出力
- **起動時**: 詳細診断結果の表示
- **継続監視中**: 各サイクルの詳細状況
- **エラー時**: 即座に詳細診断情報

### ファイル出力（ハイブリッド統合対応）

#### 技術詳細情報 → rawdata_analysis.log
- **接続診断詳細データ**: ネットワーク到達性、PLC状態情報
- **通信統計情報**: 応答時間、成功率、エラー統計
- **SLMPフレーム解析**: 診断時の通信フレーム詳細
- **エラー診断情報**: 通信エラー、異常検出詳細

#### 人間可読情報 → console_output.json
- **診断結果サマリー**: 接続OK/NG、システム状態
- **接続状況表示**: リアルタイム接続品質情報
- **エラー通知メッセージ**: ユーザー向けエラー通知
- **統計表示情報**: 運用者向け統計サマリー

#### 削除・統合対象（最新設計準拠）
- ~~logs/connection_diagnosis.json~~ → **rawdata_analysis.log**に統合
- ~~logs/communication_stats.json~~ → **rawdata_analysis.log**に統合
- ~~logs/terminal_output.txt~~ → **console_output.json**に移行

## 診断レベル設定

### Basic
- 基本的な接続状況のみ
- エラー時の最小限情報

### Normal
- 通信統計情報を含む
- デバイスアクセス状況

### Verbose
- 全ての診断情報
- フレーム解析詳細
- パフォーマンスメトリクス

## ハイブリッド統合設計との連携

### 統合ログエントリタイプ活用

#### PLC診断専用エントリタイプ（rawdata_analysis.log）
```json
{
  "EntryType": "DIAGNOSTIC_NETWORK",
  "SessionId": "session_20251002_103015",
  "DiagnosticInfo": {
    "TestType": "NetworkReachability",
    "TargetHost": "172.30.40.15",
    "TargetPort": 8192,
    "Protocol": "UDP",
    "ResponseTime": 12.3,
    "Result": "Success"
  }
}
```

#### コンソール出力エントリタイプ（console_output.json）
```json
{
  "EntryType": "CONSOLE_DIAGNOSTIC",
  "Message": "📡 ネットワーク接続テスト: 172.30.40.15:5007 → OK (12ms)",
  "Category": "DiagnosticResult",
  "Context": {
    "DiagnosticPhase": "NetworkTest",
    "ResultStatus": "Success"
  }
}
```

### 期待される効果

#### 1. 明確な状況切り分け
- **✅ PLC正常接続 + データ未設定** ← 現在の状況
- **❌ PLCオフライン** - ネットワーク到達不可
- **❌ アプリ接続失敗** - SLMP通信エラー
- **✅ PLC正常接続 + データ設定済み** - 理想状態

#### 2. 運用面での改善
- トラブルシューティング時間短縮
- 予防保全情報の提供
- 通信品質の可視化
- 既存ハイブリッド統合システムとの完全連携

#### 3. ユーザー体験（ハイブリッド出力対応）
```bash
run_rawdata_logging.bat 実行
↓
自動的に接続診断実行（約30秒）
├─ 技術詳細 → rawdata_analysis.log記録
└─ 診断結果 → console_output.json + 画面表示
↓
診断結果表示（接続OK/NG、データ状況等）
↓
詳細情報を含む継続監視開始
├─ SLMP通信詳細 → rawdata_analysis.log
└─ 監視状況 → console_output.json + 画面表示
```

## 実装順序

1. **Phase 1**: `ConnectionDiagnostic.cs` 作成・基本診断機能
2. **Phase 2**: `ContinuityExample.cs` への統合
3. **Phase 3**: リアルタイム診断情報の追加
4. **Phase 4**: 設定ファイル拡張・詳細レベル対応
5. **Phase 5**: ファイル出力・統計情報機能

## 📚 参考資料・関連文書

### 🎯 実装時必須参照資料

#### 最新技術制約・通信設定
- **Q00CPU_UDP通信対応_完全解決プラン_20251002.md**
  - Q00CPU特有の通信制限（TCP非対応、3Eフレームドロップ）
  - 診断時のポート設定（8192運用、5007代替対応）
  - UDP+4Eフレーム必須設定

#### ハイブリッド統合設計
- **Complete_Unified_Logging_System_Design.md**
  - 技術詳細情報のrawdata_analysis.log統合仕様
  - 人間可読情報のconsole_output.json分離仕様
  - 診断エントリタイプの統合指針

### ⚠️ 重要な実装制約

#### Q00CPU対応必須事項
1. **TCP診断機能の削除**
   - Q00CPUはTCP通信完全非対応
   - TCP接続テストは常に失敗するため診断項目から除外

2. **フレームタイプ制約**
   - 3Eフレーム診断は無意味（完全にドロップされる）
   - 4Eフレーム診断のみ実装

3. **ポート設定診断**
   - 運用ポート8192、代替5007での診断実装
   - ポート自動選択機能の検討

#### ハイブリッド統合制約
1. **出力先統合**
   - 独立診断ファイル作成禁止
   - 既存ハイブリッドシステムへの統合必須

2. **エントリタイプ統一**
   - 既存7エントリタイプとの整合性確保
   - 新規DIAGNOSTICエントリタイプの適切な分類

## 注意事項

- 既存の継続機能は維持
- パフォーマンスへの影響を最小化
- 診断情報は設定で無効化可能
- **ハイブリッド統合設計との完全な整合性確保**
- **Q00CPU制限事項への完全対応**