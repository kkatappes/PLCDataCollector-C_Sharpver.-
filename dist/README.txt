==================================================
   Andon SLMP Client 実行ファイル配布パッケージ
==================================================

■ ファイル構成
├── andon.exe                    # 実行ファイル（自己完結型）
├── appsettings.json             # 設定ファイル
├── run_rawdata_logging.bat      # 生データロギング実行スクリプト
├── README.txt                   # このファイル
└── README_ログ出力について.txt   # ログ機能詳細説明

■ 動作要件
- Windows 10/11 (x64)
- .NETランタイム不要（自己完結型）
- 管理者権限：不要

■ 基本的な実行方法

【方法1】通常実行（継続機能デモ）
1. エクスプローラーで配布フォルダを開く
2. andon.exe をダブルクリック
3. コンソールウィンドウが開いて継続機能デモが実行

【方法2】生データロギング実行（推奨）
1. エクスプローラーで配布フォルダを開く
2. run_rawdata_logging.bat をダブルクリック
3. 生データ解析とファイル出力が有効になって実行
   → logs/rawdata_analysis.log にログ保存
   → logs/rawdata_analysis.json にJSON形式で保存

【方法3】コマンドプロンプトから実行
通常実行:          andon.exe
生データログ実行:  andon.exe rawdata

■ 実行時の画面表示例

正常実行時：
SLMP継続機能デモを開始します...
✓ 設定ファイルを読み込みました: appsettings.json
=== 稼働第一のSLMP継続機能デモ ===
接続先: 192.168.1.10:5007
継続モード: ReturnDefaultAndContinue

製造ライン監視開始...
--- サイクル 1 ---
センサー状態読み取り中...
センサー状態: [False, False, False, False, False, False, False, False]
生産カウンター読み取り中...
生産カウンター: [0, 0, 0, 0]
✓ データ読み取り成功 - 正常処理継続

■ 設定変更方法（重要）

【現場でのPLC設定変更手順】
1. andon.exe を停止（実行中の場合）
2. appsettings.json をメモ帳で開く
3. 必要な設定を変更（下記参照）
4. ファイルを保存
5. andon.exe を再実行

【よく変更する設定項目】
{
  "PlcConnection": {
    "IpAddress": "192.168.1.100",  // ← PLCのIPアドレス
    "Port": 5007,                  // ← 通信ポート
    "UseTcp": true,                // ← TCP(true) or UDP(false)
  },
  "TimeoutSettings": {
    "ReceiveTimeoutMs": 3000,      // ← 受信タイムアウト
    "ConnectTimeoutMs": 10000      // ← 接続タイムアウト
  },
  "MonitoringSettings": {
    "MaxCycles": 10,               // ← 監視回数
    "CycleIntervalMs": 1000        // ← 監視間隔（ミリ秒）
  },
  "RawDataLoggingSettings": {      // ← ログ出力設定
    "EnableRawDataAnalysis": true, // ← 生データ解析の有効化
    "EnableFileLogging": true,     // ← ファイル出力の有効化
    "LogFilePath": "logs/rawdata_analysis.log",     // ← ログファイルパス
    "JsonExportPath": "logs/rawdata_analysis.json"  // ← JSONファイルパス
  }
}

■ 終了方法

【正常終了】
実行中の画面で任意のキーを押す
→ "デモ完了。何かキーを押してください..." が表示されたらキーを押す

【強制終了】
- Ctrl + C を押す
- または コンソールウィンドウの [×] をクリック

■ 別PCでの使用方法

【配布手順】
1. 配布フォルダ全体をUSBメモリにコピー
2. 別PCの任意のフォルダに貼り付け（例：C:\SLMP\）
3. appsettings.json でIPアドレスを編集
4. run_rawdata_logging.bat を実行（ログ出力が必要な場合・推奨）
   または andon.exe を実行（通常実行）

【注意】ファイルのブロック解除が必要な場合があります
andon.exe を右クリック → プロパティ → セキュリティ → ブロックの解除 ✓

【ログファイルが必要な場合】
生データロギング機能を使用して、通信の詳細情報をファイルに保存できます。
- 実行方法: run_rawdata_logging.bat をダブルクリック
- 出力ファイル: logs/rawdata_analysis.log （テキスト形式）
                logs/rawdata_analysis.json （JSON形式）
- 詳細は README_ログ出力について.txt を参照

■ 実際のPLC接続時の動作

【PLC接続成功時】
PLC接続成功
製造ライン監視開始...
--- サイクル 1 ---
センサー状態: [True, False, True, False, True, False, False, True]
生産カウンター: [1250, 25, 3, 0]
✓ データ読み取り成功 - 正常処理継続

【PLC接続失敗時（継続動作）】
✗ システムレベルエラー: SLMP connection error to 192.168.1.10:5007 - Connection timeout
=== 統計情報 ===
総操作数: 20, 総エラー数: 20, 継続動作数: 20
エラー率: 100.0%, 継続率: 100.0%

■ トラブルシューティング

問題：設定ファイルが見つからない
対処：andon.exeと同じフォルダにappsettings.jsonがあるか確認

問題：PLCに接続できない
対処：IPアドレス、ポート番号、ネットワーク接続を確認

問題：実行できない
対処：ファイルのブロック解除、管理者権限で実行

問題：文字化け
対処：コンソールの文字コード設定を確認

問題：継続動作しない
対処：appsettings.json の ErrorHandlingMode を "ReturnDefaultAndContinue" に設定

問題：ログファイルが作成されない
対処：run_rawdata_logging.bat を使用するか、andon.exe rawdata で実行

問題：ログファイルの場所がわからない
対処：実行ファイルと同じフォルダの logs フォルダ内に作成されます

■ よくある設定例

【TCP通信（推奨）】
"UseTcp": true, "Port": 5007

【UDP通信（高速）】
"UseTcp": false, "Port": 5000

【タイムアウト短縮（高速応答）】
"ReceiveTimeoutMs": 1000, "ConnectTimeoutMs": 5000

【長時間監視】
"MaxCycles": 1000, "CycleIntervalMs": 5000

【生データロギング有効化】
"RawDataLoggingSettings": { "EnableRawDataAnalysis": true, "EnableFileLogging": true }

■ 連絡先・バージョン情報
バージョン: 1.0.0
作成日: 2024年9月
自己完結型実行ファイル（.NETランタイム不要）
継続動作機能付き（PLC切断時もシステム継続）