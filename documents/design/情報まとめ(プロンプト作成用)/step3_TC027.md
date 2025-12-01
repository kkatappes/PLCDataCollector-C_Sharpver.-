# Step3 DisconnectAsync 正常切断テスト実装用情報（TC027）

## ドキュメント概要

### 目的
このドキュメントは、TC027_DisconnectAsync_正常切断テストの実装に必要な情報を集約したものです。
**コード作成時に必要となる技術情報のみ**を記載しており、学習資料や説明的な内容は含みません。

### 情報取得元
本ドキュメントの情報は以下のソースから抽出・統合されています：

#### 設計書（andon/documents/design/）
- `クラス・メソッドリスト.md` - クラス・メソッドの一覧と概要
- `クラス設計.md` - 詳細なクラス設計仕様
- `テスト内容.md` - テストケース仕様
- `プロジェクト構造設計.md` - フォルダ構造・プロジェクト構成
- `依存関係.md` - クラス間の依存関係
- `エラーハンドリング.md` - 例外処理・エラーメッセージ設計

#### 実装参考（PySLMPClient）
- `PySLMPClient/pyslmpclient/const.py` - SLMP定数・列挙型定義
- `PySLMPClient/pyslmpclient/__init__.py` - SLMPクライアント実装
- `PySLMPClient/tests/test_main.py` - テストケース実例

---

## 1. テスト対象メソッド仕様

### DisconnectAsync（Step5: PLC切断処理）
**クラス**: PlcCommunicationManager
**名前空間**: andon.Core.Managers

#### Input
- なし（現在の接続状態から切断を実行）

#### Output
- 成功時: DisconnectionResponse（切断処理結果オブジェクト）
  - Status: ConnectionStatus.Disconnected（切断完了）
  - DisconnectedAt: DateTime型（切断完了時刻）
  - DisconnectionTime: TimeSpan型（切断処理時間）
  - Statistics: ConnectionStats型（接続期間統計情報）
  - ErrorMessage: null（成功時はnull）
- 失敗時: 例外スロー
  - InvalidOperationException: 未接続状態での切断試行時
  - SocketException: ソケット切断エラー時

#### 機能
- Socket.Close()実行
- Socket.Dispose()実行
- 接続状態リセット（ConnectionStatus.Disconnected）
- 切断時間計測
- 接続期間統計計算
- 内部状態初期化

#### 統計情報計算
- TotalConnectionTime: 接続開始から切断までの総時間
- SuccessfulOperations: 成功した操作回数（送受信回数）
- TotalErrors: エラー発生回数
- SuccessRate: 成功率（SuccessfulOperations / (SuccessfulOperations + TotalErrors)）

---

## 2. テストケース仕様（TC027）

### TC027_DisconnectAsync_正常切断
**目的**: PLC切断機能が正常に動作することをテスト

#### 前提条件
- PlcCommunicationManagerが接続済み状態（ConnectAsync完了）
- 少なくとも1回の送受信操作完了（統計情報蓄積のため）
- Socket.Connected == true

#### 入力データ
- なし（現在の接続状態から実行）

#### 期待出力
- DisconnectionResponse（切断成功オブジェクト）
  - Status == ConnectionStatus.Disconnected（切断完了状態）
  - DisconnectedAt != null（切断時刻記録済み）
  - DisconnectedAt.Value <= DateTime.UtcNow（切断時刻が現在時刻以前）
  - DisconnectionTime != null（切断処理時間記録済み）
  - DisconnectionTime.Value.TotalMilliseconds > 0（切断処理時間が正の値）
  - DisconnectionTime.Value.TotalMilliseconds < 1000（1秒以内で完了）
  - Statistics != null（統計情報オブジェクト生成済み）
  - Statistics.TotalConnectionTime > TimeSpan.Zero（接続時間が正の値）
  - Statistics.SuccessfulOperations >= 0（操作回数記録）
  - Statistics.TotalErrors >= 0（エラー回数記録）
  - Statistics.SuccessRate >= 0.0 && Statistics.SuccessRate <= 1.0（成功率0-1の範囲）
  - ErrorMessage == null（成功時はエラーメッセージなし）

#### 動作フロー成功条件
1. **接続状態チェック**: 接続済み状態であることを確認
2. **切断時間計測開始**: 処理開始時刻記録
3. **統計情報収集**: 接続期間・操作回数・エラー回数の集計
4. **ソケット切断**: Socket.Close()実行
5. **リソース解放**: Socket.Dispose()実行
6. **状態更新**: ConnectionStatus.Disconnectedに変更
7. **切断完了時刻記録**: DisconnectedAt設定
8. **処理時間計算**: 切断処理時間算出
9. **統計情報生成**: ConnectionStats生成
10. **戻り値生成**: DisconnectionResponse生成・返却

---

## 3. Socket切断処理詳細

### TCP/UDP共通切断手順
**標準Socket切断処理**:
1. Socket.Shutdown(SocketShutdown.Both) - 送受信停止
2. Socket.Close() - ソケット切断
3. Socket.Dispose() - リソース解放

### C# Socket切断実装
```csharp
public async Task<DisconnectionResponse> DisconnectAsync()
{
    // 接続状態チェック
    if (_socket == null || !_socket.Connected)
    {
        throw new InvalidOperationException(ErrorMessages.NotConnectedForDisconnect);
    }

    var startTime = DateTime.UtcNow;

    try
    {
        // 統計情報収集
        var connectionStats = CollectConnectionStatistics();

        // ソケット切断
        if (_socket.Connected)
        {
            _socket.Shutdown(SocketShutdown.Both);
        }
        _socket.Close();
        _socket.Dispose();

        var disconnectionTime = DateTime.UtcNow - startTime;

        // 状態更新
        _connectionStatus = ConnectionStatus.Disconnected;
        _socket = null;

        return new DisconnectionResponse
        {
            Status = ConnectionStatus.Disconnected,
            DisconnectedAt = DateTime.UtcNow,
            DisconnectionTime = disconnectionTime,
            Statistics = connectionStats,
            ErrorMessage = null
        };
    }
    catch (SocketException ex)
    {
        throw new SocketException($"切断エラー: {ex.Message}");
    }
}

private ConnectionStats CollectConnectionStatistics()
{
    var totalConnectionTime = DateTime.UtcNow - _connectedAt.Value;

    return new ConnectionStats
    {
        TotalConnectionTime = totalConnectionTime,
        SuccessfulOperations = _successfulOperations,
        TotalErrors = _totalErrors,
        SuccessRate = _successfulOperations + _totalErrors > 0
            ? (double)_successfulOperations / (_successfulOperations + _totalErrors)
            : 1.0
    };
}
```

---

## 4. 依存クラス・設定

### DisconnectionResponse（切断結果）
**本メソッドの出力データ型**

```csharp
public class DisconnectionResponse
{
    public ConnectionStatus Status { get; set; }      // 切断状態（Disconnected）
    public DateTime? DisconnectedAt { get; set; }     // 切断完了時刻
    public TimeSpan? DisconnectionTime { get; set; }  // 切断処理時間
    public ConnectionStats? Statistics { get; set; }  // 接続期間統計情報
    public string? ErrorMessage { get; set; }         // エラーメッセージ（失敗時のみ）
}
```

### ConnectionStats（接続統計）
**統計情報データ型**

```csharp
public class ConnectionStats
{
    public TimeSpan TotalConnectionTime { get; set; } // 総接続時間
    public int SuccessfulOperations { get; set; }     // 成功操作数
    public int TotalErrors { get; set; }              // 総エラー数
    public double SuccessRate { get; set; }           // 成功率（0.0-1.0）
    public int TotalRetries { get; set; }             // 総リトライ数
    public TimeSpan AverageResponseTime { get; set; } // 平均応答時間
}
```

### ConnectionStatus（接続状態列挙型）
**定義**: Core/Models/ConnectionStatus.cs

```csharp
public enum ConnectionStatus
{
    NotConnected,    // 未接続状態（TC027実行後の期待状態）
    Connecting,      // 接続中状態
    Connected,       // 接続済み状態（TC027実行前の前提状態）
    Disconnecting,   // 切断中状態
    Disconnected,    // 切断済み状態（TC027で期待される状態）
    Failed           // 接続失敗状態
}
```

---

## 5. テスト実装方針（TDD）

### 開発手法
- C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.mdに記載のTDD手法を使用

### テストファイル配置
- **ファイル名**: PlcCommunicationManagerTests.cs
- **配置先**: Tests/Unit/Core/Managers/
- **名前空間**: andon.Tests.Unit.Core.Managers

### テスト実装順序
1. TC017_ConnectAsync_TCP接続成功（既実装前提）
2. TC021_SendFrameAsync_正常送信（既実装前提）
3. TC025_ReceiveResponseAsync_正常受信（既実装前提）
4. **TC027_DisconnectAsync_正常切断**（本テスト）
5. TC028: 異常系テスト（未接続状態切断）

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockSocket**: 切断対応Socketモック
  - Connected = true → false への状態変更
  - Shutdown(), Close(), Dispose() 正常実行
- **MockConnectionStatsCollector**: 統計情報収集モック
  - 接続時間、操作回数、エラー回数の累積データ返却

#### 使用するスタブ
- **DisconnectionResponseValidator**: 期待出力検証用ヘルパー
- **ConnectionStatsStubs**: 統計情報スタブ
- **ConnectionStatusValidator**: 状態遷移検証用ヘルパー

---

## 6. テストケース実装構造

### Arrange（準備）
1. **PlcCommunicationManager接続済み状態**:
   - ConnectAsync完了状態
   - Socket.Connected == true
   - _connectedAt設定済み

2. **操作履歴蓄積**:
   - SendFrameAsync完了（1回以上）
   - ReceiveResponseAsync完了（1回以上）
   - _successfulOperations >= 1
   - _totalErrors = 0（正常系テストのため）

3. **MockSocket準備**:
   - Connected = true（接続済み状態）
   - Shutdown(), Close(), Dispose() 正常実行可能

### Act（実行）
1. DisconnectAsync実行:
   ```csharp
   var result = await plcCommManager.DisconnectAsync();
   ```

### Assert（検証）
1. **切断状態検証**:
   - `result.Status == ConnectionStatus.Disconnected`
   - `plcCommManager.ConnectionStatus == ConnectionStatus.Disconnected`

2. **時刻・時間検証**:
   - `result.DisconnectedAt != null`
   - `result.DisconnectedAt.Value <= DateTime.UtcNow`
   - `result.DisconnectionTime != null`
   - `result.DisconnectionTime.Value.TotalMilliseconds > 0`
   - `result.DisconnectionTime.Value.TotalMilliseconds < 1000`

3. **統計情報検証**:
   - `result.Statistics != null`
   - `result.Statistics.TotalConnectionTime > TimeSpan.Zero`
   - `result.Statistics.SuccessfulOperations >= 1`
   - `result.Statistics.TotalErrors == 0`
   - `result.Statistics.SuccessRate == 1.0`（正常系のため）

4. **エラー情報検証**:
   - `result.ErrorMessage == null`

5. **内部状態検証**:
   - `plcCommManager.Socket == null`（リソース解放確認）

---

## 7. エラーハンドリング

### DisconnectAsync スロー例外（TC027では発生しない）
- **InvalidOperationException**: 未接続状態での切断試行
  - メッセージ: "未接続状態です。切断前にConnectAsync()を実行してください"
- **SocketException**: ソケット切断エラー
  - メッセージ: "切断エラー: {詳細}"

### エラーメッセージ統一
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    // 切断関連
    public const string NotConnectedForDisconnect = "未接続状態です。切断前にConnectAsync()を実行してください";
    public const string DisconnectionError = "切断エラー: {0}";
}
```

---

## 8. ログ出力要件

### LoggingManager連携
- **切断開始ログ**: 切断処理開始、接続継続時間
- **統計情報ログ**: 成功操作数、エラー数、成功率
- **切断完了ログ**: 切断処理時間、最終ステータス
- **エラーログ**: 例外詳細、スタックトレース（TC027では発生しない）

### ログレベル
- **Information**: 切断開始・完了
- **Debug**: 統計情報詳細、ソケット状態
- **Error**: 例外発生時（TC027では発生しない）

### ログ出力例
```
[Information] PLC切断処理開始: 接続継続時間=120s
[Debug] 統計情報: 成功操作数=5, エラー数=0, 成功率=100%
[Information] PLC切断完了: 切断処理時間=15ms
```

---

## 9. リソース管理とメモリリーク対策

### IDisposable実装
```csharp
public void Dispose()
{
    Dispose(true);
    GC.SuppressFinalize(this);
}

protected virtual void Dispose(bool disposing)
{
    if (!_disposed && disposing)
    {
        if (_socket != null)
        {
            if (_socket.Connected)
            {
                try
                {
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Close();
                }
                catch (SocketException)
                {
                    // 切断時のエラーは無視
                }
            }
            _socket.Dispose();
            _socket = null;
        }
        _disposed = true;
    }
}
```

### using文での自動リソース解放
```csharp
// テストでの使用例
using (var plcCommManager = new PlcCommunicationManager())
{
    await plcCommManager.ConnectAsync(connectionConfig, timeoutConfig);
    await plcCommManager.SendFrameAsync(frame);
    await plcCommManager.ReceiveResponseAsync(timeoutConfig.ReceiveTimeoutMs);
    var result = await plcCommManager.DisconnectAsync();
    // using終了時に自動でDispose()実行
}
```

---

## 10. テスト実装チェックリスト

### TC027実装前
- [ ] DisconnectionResponseモデル作成
- [ ] ConnectionStatsモデル作成
- [ ] DisconnectAsyncメソッドシグネチャ定義
- [ ] CollectConnectionStatisticsメソッド作成
- [ ] MockSocket切断対応実装

### TC027実装中
- [ ] Arrange: 接続済み状態設定（ConnectAsync完了）
- [ ] Arrange: 操作履歴蓄積（Send/Receive実行済み）
- [ ] Arrange: MockSocket接続状態設定
- [ ] Act: DisconnectAsync呼び出し
- [ ] Assert: Status検証（Disconnected）
- [ ] Assert: DisconnectedAt検証（時刻記録）
- [ ] Assert: DisconnectionTime検証（処理時間記録）
- [ ] Assert: Statistics検証（統計情報生成）
- [ ] Assert: ErrorMessage検証（null）
- [ ] Assert: 内部状態検証（Socket = null）

### TC027実装後
- [ ] テスト実行・Red確認
- [ ] DisconnectAsync本体実装（切断処理）
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施
- [ ] TC028（未接続状態切断）への準備

---

## 11. 参考情報

### Socket切断仕様
- Shutdown(): 送受信停止（graceful shutdown）
- Close(): ソケット切断
- Dispose(): リソース解放
- Connected状態: true → false への変更

**重要**: TC027ではMockSocketを使用、実機PLC不要

### テストデータサンプル
**配置先**: Tests/TestUtilities/TestData/DisconnectionSamples/

- DisconnectionResponse.json: 切断成功時の期待出力サンプル
- ConnectionStats.json: 統計情報サンプル

---

## 12. PySLMPClient実装参考情報

### 切断処理実装（Python実装例）

#### PySLMPClientでの切断処理
```python
import socket
import time

class SLMPClient:
    def disconnect(self):
        if not self.__is_connected or self.__socket is None:
            raise RuntimeError("Not connected")

        start_time = time.time()

        try:
            # 統計情報収集
            connection_stats = self.collect_connection_stats()

            # ソケット切断
            self.__socket.shutdown(socket.SHUT_RDWR)
            self.__socket.close()

            disconnection_time = time.time() - start_time

            # 状態更新
            self.__is_connected = False
            self.__socket = None

            return {
                'status': 'Disconnected',
                'disconnected_at': time.time(),
                'disconnection_time': disconnection_time,
                'statistics': connection_stats
            }

        except socket.error as e:
            raise RuntimeError(f"Disconnection error: {e}")

    def collect_connection_stats(self):
        total_connection_time = time.time() - self.__connected_at
        success_rate = self.__successful_operations / (self.__successful_operations + self.__total_errors) if (self.__successful_operations + self.__total_errors) > 0 else 1.0

        return {
            'total_connection_time': total_connection_time,
            'successful_operations': self.__successful_operations,
            'total_errors': self.__total_errors,
            'success_rate': success_rate
        }
```

#### C#実装例（TC027対応）
```csharp
public async Task<DisconnectionResponse> DisconnectAsync()
{
    // 接続状態チェック
    if (_socket == null || !_socket.Connected)
    {
        throw new InvalidOperationException(ErrorMessages.NotConnectedForDisconnect);
    }

    var startTime = DateTime.UtcNow;

    try
    {
        // 統計情報収集
        var connectionStats = CollectConnectionStatistics();

        // ソケット切断
        if (_socket.Connected)
        {
            _socket.Shutdown(SocketShutdown.Both);
        }
        _socket.Close();
        _socket.Dispose();

        var disconnectionTime = DateTime.UtcNow - startTime;

        // 状態更新
        _connectionStatus = ConnectionStatus.Disconnected;
        _socket = null;

        return new DisconnectionResponse
        {
            Status = ConnectionStatus.Disconnected,
            DisconnectedAt = DateTime.UtcNow,
            DisconnectionTime = disconnectionTime,
            Statistics = connectionStats,
            ErrorMessage = null
        };
    }
    catch (SocketException ex)
    {
        throw new SocketException(
            string.Format(ErrorMessages.DisconnectionError, ex.Message));
    }
}

private ConnectionStats CollectConnectionStatistics()
{
    var totalConnectionTime = DateTime.UtcNow - _connectedAt.Value;

    return new ConnectionStats
    {
        TotalConnectionTime = totalConnectionTime,
        SuccessfulOperations = _successfulOperations,
        TotalErrors = _totalErrors,
        SuccessRate = _successfulOperations + _totalErrors > 0
            ? (double)_successfulOperations / (_successfulOperations + _totalErrors)
            : 1.0,
        TotalRetries = _totalRetries,
        AverageResponseTime = _totalRetries > 0
            ? TimeSpan.FromMilliseconds(_totalResponseTime / _totalRetries)
            : TimeSpan.Zero
    };
}
```

### 実装時の重要ポイント

1. **graceful shutdown**: Socket.Shutdown(SocketShutdown.Both)で送受信停止
2. **リソース解放**: Close()とDispose()の両方実行
3. **状態管理**: _connectionStatus と _socket の両方をクリア
4. **統計情報**: 接続開始時刻からの経過時間計算
5. **エラーハンドリング**: 切断時のSocketExceptionを適切にキャッチ

---

以上が TC027_DisconnectAsync_正常切断テスト実装に必要な情報です。