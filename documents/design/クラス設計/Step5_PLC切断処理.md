# Step5: PLC切断処理

## 機能概要

PLC接続の適切な切断とリソース解放

## 詳細機能

### 接続切断処理
- データ取得元：PlcCommunicationManager.ConnectAsync()（接続状態）
- 通信統計情報：PlcCommunicationManager全体の通信履歴

---

## クラス：PlcCommunicationManager

### DisconnectAsync（Step5: PLC切断処理）

**機能：** PLC接続の適切な切断と統計情報の取得

**Input:**
- 切断/リソース管理情報（PlcCommunicationManager.ConnectAsync()からの接続状態、通信統計情報）

**Output:**
- ConnectionStats（通信統計情報オブジェクト）
  - 基本統計: 接続時間、送受信フレーム数・バイト数、切断時刻
  - 応答時間統計: 履歴・平均・最大・最小応答時間
  - エラー・品質統計: エラー回数、リトライ回数、通信成功率

---

## 処理フロー

```
1. 切断要求受付
      ↓
2. 接続状態確認
   ├─ 接続中? == False → 切断完了済メッセージ出力
   │   ├─ ログ出力（"既に切断済み"）
   │   ├─ 空のConnectionStats返却
   │   └─ 処理終了
   │
   └─ 接続中? == True → 次へ
      ↓
3. 通信全体の統計情報収集
   ├─ 接続時間計算（DisconnectedAt - ConnectedAt）
   ├─ 送信フレーム数記録（TotalFramesSent）
   ├─ 受信フレーム数記録（TotalFramesReceived）
   ├─ 送信バイト数記録（TotalBytesSent）
   ├─ 受信バイト数記録（TotalBytesReceived）
   ├─ 応答時間統計計算
   │   ├─ 応答時間履歴（ResponseTimes）
   │   ├─ 平均応答時間（AverageResponseTime）
   │   ├─ 最大応答時間（MaxResponseTime）
   │   └─ 最小応答時間（MinResponseTime）
   ├─ エラー回数記録（TotalErrors）
   └─ 通信成功率計算（SuccessRate = (TotalFramesSent - TotalErrors) / TotalFramesSent）
      ↓
4. ソケットシャットダウン実行
   ├─ Socket.Shutdown(SocketShutdown.Both)
   │   ├─ 送受信両方向の通信を優雅に終了
   │   ├─ TCP FINパケット送信（TCP接続時）
   │   └─ 相手側への切断通知
   └─ 例外処理：既に切断済みソケットの安全な処理
      ↓
5. ソケットクローズ実行
   ├─ Socket.Close()
   │   ├─ ソケットリソース解放
   │   └─ OSレベルのソケット破棄
   └─ タイムアウト設定：LingerOption設定（適切な切断待機時間）
      ↓
6. ソケット破棄実行
   ├─ Socket.Dispose()
   │   ├─ .NETマネージドリソース解放
   │   └─ メモリ解放
      ↓
7. 内部状態初期化
   ├─ _socket = null（ソケット参照クリア）
   ├─ _isConnected = false（接続状態フラグリセット）
   └─ その他内部状態変数の初期化
      ↓
8. ConnectionStats返却
   ├─ 収集した統計情報オブジェクト返却
   └─ 処理完了ログ出力
```

---

## 設計方針

### エラーハンドリング
- 既切断ソケットへの安全な処理（例外スロー回避）

### 統計情報完全性
- 切断前に全統計情報を確実に収集

### リソース解放順序厳守
- Shutdown→Close→Dispose の順序遵守

### 状態整合性保証
- 切断処理完了後の確実な状態リセット

---

### Disconnect（PLC切断処理 - 同期版）

**機能：** 同期的なPLC切断処理

**Input:**
- なし（内部状態を基に処理実行）

**Output:**
- void（戻り値なし）
- 処理内容：ソケット適切切断、リソース解放、接続状態リセット

**処理詳細:**
- ソケット接続状態確認・適切な切断手順実行（Socket.Shutdown()）
- リソース解放（Socket.Close(), Socket.Dispose()）
- 内部状態初期化（_socket = null, _isConnected = false）
- 例外処理：既切断ソケットに対する安全な処理

**用途:**
- IDisposableパターン実装の補助メソッド
- 緊急時同期切断

**データ取得元:**
- 内部フィールド（_socket, _isConnected, _disposed）

---

### Dispose（IDisposable実装 - リソース管理）

**機能：** 標準.NETリソース管理パターン実装

**Input:**
- なし（IDisposableインターフェース準拠）

**Output:**
- void（戻り値なし）
- 処理内容：標準.NETリソース管理パターン実装

**処理詳細:**
- 重複実行防止（_disposedフィールドチェック）
- Disconnect()メソッド呼び出し（実際のリソース解放処理）
- GC.SuppressFinalize(this)実行（ファイナライザー抑制）
- _disposedフラグ設定（再実行防止）

**設計方針:**
- C#標準Disposableパターン準拠（重複実行防止、ファイナライザー最適化）
- Disconnect()への処理委譲（責任分離、同期処理保証）
- using文対応（自動リソース管理対応）

**データ取得元:**
- 内部フィールド（_disposed）
- Disconnect()メソッド

---

## 成功条件

- 正常に接続切断処理が完了できる

---

## データ取得元

- PlcCommunicationManager.ConnectAsync()（接続状態）
- 通信全体の統計情報（内部フィールド）
