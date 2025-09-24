==================================================
   Andon SLMP Client 実行ファイル配布パッケージ
==================================================

■ ファイル構成
├── andon.exe           # 実行ファイル（自己完結型）
├── appsettings.json    # 設定ファイル
└── README.txt          # このファイル

■ 動作要件
- Windows 10/11 (x64)
- .NETランタイム不要（自己完結型）
- 管理者権限：不要

■ 基本的な実行方法

【方法1】ダブルクリックで実行
1. エクスプローラーで配布フォルダを開く
2. andon.exe をダブルクリック
3. コンソールウィンドウが開いて実行開始

【方法2】コマンドプロンプトから実行
1. コマンドプロンプトを開く
2. cd "配布フォルダのパス" でディレクトリ移動
3. andon.exe と入力して実行

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
4. andon.exe を実行

【注意】ファイルのブロック解除が必要な場合があります
andon.exe を右クリック → プロパティ → セキュリティ → ブロックの解除 ✓

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

■ よくある設定例

【TCP通信（推奨）】
"UseTcp": true, "Port": 5007

【UDP通信（高速）】
"UseTcp": false, "Port": 5000

【タイムアウト短縮（高速応答）】
"ReceiveTimeoutMs": 1000, "ConnectTimeoutMs": 5000

【長時間監視】
"MaxCycles": 1000, "CycleIntervalMs": 5000

■ 連絡先・バージョン情報
バージョン: 1.0.0
作成日: 2024年9月
自己完結型実行ファイル（.NETランタイム不要）
継続動作機能付き（PLC切断時もシステム継続）