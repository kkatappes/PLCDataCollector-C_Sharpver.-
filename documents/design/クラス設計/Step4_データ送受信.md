# Step4: データ送受信

## 機能概要

PLCへのリクエスト送信とレスポンス受信

## 詳細機能

### フレーム情報送信機能（1回送信）
- データ取得元：ConfigToFrameManager.BuildFrames()（送信フレーム）
- 接続状態：PlcCommunicationManager.ConnectAsync()（接続状態）
- 送信コマンド：Random READコマンド(0403)による全デバイス一括取得

### レスポンス受信機能（1回受信）
- データ取得元：PlcCommunicationManager.SendFrameAsync()（送信完了状態）
- タイムアウト：ConfigToFrameManager.LoadConfigAsync()（タイムアウト設定）
- 受信データ：全デバイスデータ（ビット・ワード・ダブルワード混在、Random READ応答、既にDWord形式）

---

## クラス：PlcCommunicationManager

### SendFrameAsync（Step4-1: PLCへのリクエスト送信）

**機能：** 構築済みフレームのPLCへの送信

**Input:**
- 設定値から生成したインスタンス（SLMPフレーム、16進数文字列：ConfigToFrameManager.BuildFrames()から取得）
- 全デバイス一括読み込みフレーム例（Random READ）：540012340000000104030000...（ビット・ワード・ダブルワード混在指定）
- （タイムアウトはStep3のConnectAsync内でSocket.SendTimeoutに既設定済み）

**Output:**
- void（戻り値なし）
- 成功時：送信完了状態
- 失敗時：例外スロー（SocketException, TimeoutException等）

**使用コマンド：**
- Random READコマンド(0403)のみ

**取得対象：**
- 全デバイス（ビット・ワード・ダブルワード）を1回の送信で一括取得

---

## 処理フロー（Step4-1: リクエスト送信）

```
1. 接続情報確認
   ├─ 接続情報 == False → Throw error（例外スロー）
   └─ 接続情報 == True → 次へ
      ↓
2. 構築済みフレーム送信
   ├─ Socket.Send()またはSocket.SendAsync()でフレームデータ送信
   └─ SendTimeoutMs内に送信完了確認
      ↓
3. 送信完了判定
   ├─ 送信完了? == False → Throw error（TimeoutException等）
   └─ 送信完了? == True → 次へ
      ↓
4. 送信統計記録
   ├─ 送信フレーム数 +1（TotalFramesSent++）
   ├─ 送信バイト数加算（TotalBytesSent += sentBytes）
   └─ 送信時刻記録（LastSendTime = DateTime.UtcNow）
      ↓
5. void返却（正常完了）
```

**統計情報更新（ConnectionStatsへの記録）:**
- TotalFramesSent（送信フレーム総数）のインクリメント
- TotalBytesSent（送信バイト総数）の加算
- 送信時刻の記録（応答時間計算用）

---

### ReceiveResponseAsync（Step4-2: PLCからのデータ受信）

**機能：** PLCからの応答データ受信

**Input:**
- （タイムアウトはStep3のConnectAsync内でSocket.ReceiveTimeoutに既設定済み）

**Output:**
- 各種PLCの状態/生データ(16進数)
- Random READコマンド(0403)レスポンス：全デバイスデータ（ビット・ワード・ダブルワード混在、既にDWord形式）
- 受信データ形式：SLMPレスポンスフレーム（ヘッダー + データ部）
- **1回の受信で全デバイスデータ取得完了**

---

## 処理フロー（Step4-2: データ受信）

```
1. PLCレスポンス待機
   ├─ Socket.Receive()またはSocket.ReceiveAsync()でデータ受信待機
   └─ ReceiveTimeoutMs内に受信確認
      ↓
2. タイムアウト内で受信判定
   ├─ タイムアウト内で受信? == False → 時間内に受信無しのメッセージ出力（TimeoutException）
   └─ タイムアウト内で受信? == True → 次へ
      ↓
3. 受信統計記録
   ├─ 受信フレーム数 +1（TotalFramesReceived++）
   ├─ 受信バイト数加算（TotalBytesReceived += receivedBytes）
   └─ 受信時刻記録（LastReceiveTime = DateTime.UtcNow）
      ↓
4. 生データ（16進数）返却
```

**統計情報更新（ConnectionStatsへの記録）:**
- TotalFramesReceived（受信フレーム総数）のインクリメント
- TotalBytesReceived（受信バイト総数）の加算
- 応答時間記録（ResponseTimes.Add(receiveTime - sendTime)）
  - 送信時刻と受信時刻の差分を計算
  - ConnectionStats.AddResponseTime()で統計更新

---

## 成功条件

- 設定ファイルで指定した機器に対するリクエスト送信/レスポンス受信ができる
- 一度の通信で全ての機器からデータを取得（既定値 M001-M999,D001-D999）

---

## 参考資料

C:\Users\1010821\Desktop\python\andon\pdf2img（SLMP仕様書）
