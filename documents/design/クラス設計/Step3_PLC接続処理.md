# Step3: PLC接続処理

## 機能概要

PLCとの通信接続・切断処理

## 詳細機能

### PLC通信機能（非同期、接続/切断）
- データ取得元：ConfigToFrameManager.LoadConfigAsync()（接続設定）
- 接続状態：PlcCommunicationManager.ConnectAsync()（接続状態）

---

## クラス：PlcCommunicationManager

### ConnectAsync（Step3: PLC接続処理）

**機能：** PLCへの接続確立

**Input:**
- ConnectionConfig（IpAddress, Port, UseTcp, ConnectionType：ConfigToFrameManager.LoadConfigAsync()から取得）
  - ConnectionType：ログ出力・可読性用の文字列表現（"TCP", "UDP"）、実際の接続判定はUseTcpを使用
- TimeoutConfig（ConnectTimeoutMs, SendTimeoutMs, ReceiveTimeoutMs：ConfigToFrameManager.LoadConfigAsync()から取得）

**Output:**

成功時：ConnectionResponse（接続処理結果オブジェクト）
- Status（ConnectionStatus型、必須）: Connected
- Socket（System.Net.Sockets.Socket型、null許容）: 実際の通信用ソケット（成功時のみ）
- RemoteEndPoint（System.Net.EndPoint型、null許容）: 接続先情報（成功時のみ）
- ConnectedAt（DateTime型、null許容）: 接続完了時刻（成功時のみ）
- ConnectionTime（TimeSpan型、null許容）: 接続処理にかかった時間（成功時のみ）
- ErrorMessage=null（成功時はnull）

失敗時：例外スロー
- TimeoutException：接続タイムアウト時
- SocketException：接続拒否、ネットワークエラー時
- ArgumentException：不正なIPアドレス・ポート番号時
- InvalidOperationException：既に接続済み状態での再接続試行時

---

## 処理フロー

```
1. 接続状態チェック
   ├─ 既接続時 → InvalidOperationExceptionスロー
   └─ 未接続時 → 次へ
      ↓
2. ソケット作成
   ├─ Socket インスタンス生成
   └─ UDP/TCP選択
      ↓
3. 接続実行
   ├─ TCP：Socket.ConnectAsync
   │   └─ ConnectTimeoutMs適用
   │
   └─ UDP：Socket.Connect（送信先設定）
       └─ TC021フレーム疎通確認（TDD・オフライン対応）
           ├─ M000-M999読み込みフレーム送信
           │   "54001234000000010401006400000090E8030000"
           │   または
           ├─ D000-D999読み込みフレーム送信
           │   "54001234000000010400A800000090E8030000"
           ├─ ConnectTimeoutMs内に応答受信確認
           │   ├─ 本番：実PLC
           │   └─ テスト：モック応答
           └─ 応答なし→TimeoutExceptionスロー
      ↓
4. 接続成功判定
   ├─ 成功 → 5へ進む
   └─ 失敗/タイムアウト → 例外スロー
      ↓
5. ソケットタイムアウト設定
   ├─ Socket.SendTimeout = SendTimeoutMs
   └─ Socket.ReceiveTimeout = ReceiveTimeoutMs
      ↓
6. 接続情報記録
   ├─ ConnectedAt（接続時刻記録）
   ├─ ConnectionTime（接続所要時間計算）
   └─ RemoteEndPoint（接続先情報保存）
      ↓
7. ConnectionResponse返却
   ├─ Status = Connected
   ├─ Socket（設定済みソケットインスタンス）
   └─ その他接続情報
```

---

## 設計方針

### エラーハンドリング
- 例外スロー方式採用（.NET標準、エラー処理の強制力が高い）

### UDP疎通確認
- TC021の既知SLMPフレームを使用した疎通確認
  - 本番環境：実PLC動作確認
  - テスト環境：モック応答による完全オフライン実施

### タイムアウト設定
- ソケットレベルでのタイムアウト設定により、Step4（送受信処理）で個別にタイムアウト制御を実装する必要がなくなる
- Socket.SendTimeout/ReceiveTimeoutは接続後のみ有効なため、接続成功確認後に設定する
- これにより送受信処理の実装が簡潔になり、保守性が向上する

---

## 成功条件

- 読み込んだ設定ファイルの情報から意図した機器に接続処理ができる
- 接続に失敗した場合はエラーを返すこと

---

## データ取得元

- ConfigToFrameManager.LoadConfigAsync()（接続設定）
- TimeoutConfig（タイムアウト設定）
