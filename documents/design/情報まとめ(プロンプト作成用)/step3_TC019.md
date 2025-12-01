# Step3 ConnectAsync 接続タイムアウトテスト実装用情報（TC019）

## ドキュメント概要

### 目的
このドキュメントは、TC019_ConnectAsync_接続タイムアウトテストの実装に必要な情報を集約したものです。
**コード作成時に必要となる技術情報のみ**を記載しており、学習資料や説明的な内容は含みません。

### 情報取得元
本ドキュメントの情報は以下のソースから抽出・統合されています：

#### 設計書（andon/documents/design/）
- `クラス・メソッドリスト.md` - クラス・メソッドの一覧と概要
- `クラス設計.md` - 詳細なクラス設計仕様
- `テスト内容.md` - テストケース仕様
- `エラーハンドリング.md` - 例外処理・エラーメッセージ設計

---

## 1. テスト対象メソッド仕様

### ConnectAsync（Step3: PLC接続処理）
**クラス**: PlcCommunicationManager
**名前空間**: andon.Core.Managers

#### Input
- ConnectionConfig（IpAddress, Port, UseTcp）
  - **IpAddress: "192.168.100.200"（到達不可能なIPアドレス）**
  - Port: 5000
  - UseTcp: true
- TimeoutConfig（ConnectTimeoutMs）
  - **ConnectTimeoutMs: 1000（短いタイムアウト設定）**

#### Output
- 成功時: ConnectionResponse（接続処理結果オブジェクト）
- **失敗時（TC019で期待）**: TimeoutException スロー
  - 例外メッセージ: "接続タイムアウト: 192.168.100.200:5000（タイムアウト時間: 1000ms）"
  - ConnectionResponse は返却されない

#### 機能（TC019検証ポイント）
- 接続タイムアウト検出
- ConnectTimeoutMs超過時のTimeoutExceptionスロー
- 適切なエラーメッセージ生成
- リソースリーク防止（ソケット適切なクリーンアップ）

---

## 2. テストケース仕様（TC019）

### TC019_ConnectAsync_接続タイムアウト
**目的**: 接続タイムアウトが正しく検出され、TimeoutExceptionがスローされることをテスト

#### 前提条件
- 有効なConnectionConfig設定（到達不可能なIPアドレス）
- 短いConnectTimeoutMs設定（1000ms）
- **接続先が到達不可能（ネットワーク不通またはホスト不在）**

#### 入力データ
**ConnectionConfig**:
- **IpAddress: "192.168.100.200"（到達不可能なIPアドレス）**
- Port: 5000
- UseTcp: true
- ConnectionType: "TCP"
- IsBinary: false
- FrameVersion: FrameVersion.Frame4E

**TimeoutConfig**:
- **ConnectTimeoutMs: 1000（短いタイムアウト）**
- SendTimeoutMs: 3000
- ReceiveTimeoutMs: 3000

#### 期待動作（異常系）
- **TimeoutException がスローされる**
- **例外メッセージ**: "接続タイムアウト: 192.168.100.200:5000（タイムアウト時間: 1000ms）"
- **ConnectionResponse は返却されない**（例外発生のため）
- ソケットリソースが適切にクリーンアップされる

#### 検証項目
1. **TimeoutException発生確認**:
   ```csharp
   await Assert.ThrowsAsync<TimeoutException>(async () =>
       await plcCommManager.ConnectAsync(connectionConfig, timeoutConfig));
   ```

2. **例外メッセージ検証**:
   ```csharp
   var exception = await Assert.ThrowsAsync<TimeoutException>(...);
   Assert.Contains("接続タイムアウト", exception.Message);
   Assert.Contains("192.168.100.200:5000", exception.Message);
   Assert.Contains("1000ms", exception.Message);
   ```

3. **タイムアウト時間の精度確認**:
   - 処理時間がConnectTimeoutMs前後であること（誤差±200ms程度許容）
   - 極端に短い・長い時間で完了していないこと

4. **リソースクリーンアップ確認**:
   - ソケットが適切にDisposeされていること
   - 接続状態がNotConnectedまたはFailedになっていること

---

## 3. 接続タイムアウト処理詳細

### タイムアウト検出メカニズム
**CancellationTokenSourceによるタイムアウト制御**:
```csharp
var cts = new CancellationTokenSource(timeoutConfig.ConnectTimeoutMs);

try
{
    await socket.ConnectAsync(endPoint, cts.Token);
}
catch (OperationCanceledException)
{
    // タイムアウト発生
    throw new TimeoutException(
        $"接続タイムアウト: {connectionConfig.IpAddress}:{connectionConfig.Port}（タイムアウト時間: {timeoutConfig.ConnectTimeoutMs}ms）");
}
```

### タイムアウト時の動作
1. **ConnectTimeoutMs経過**: CancellationTokenSourceがキャンセルを通知
2. **OperationCanceledException発生**: Socket.ConnectAsyncがキャンセル
3. **TimeoutExceptionスロー**: ConnectAsync内で例外を変換
4. **リソースクリーンアップ**: finallyブロックでソケットDispose

---

## 4. 依存クラス・設定

### ConnectionConfig（接続設定）
```csharp
public class ConnectionConfig
{
    public string IpAddress { get; set; }        // "192.168.100.200"（到達不可能）
    public int Port { get; set; }                 // 5000
    public bool UseTcp { get; set; }              // true
    public string ConnectionType { get; set; }    // "TCP"
    public bool IsBinary { get; set; }            // false
    public FrameVersion FrameVersion { get; set; } // FrameVersion.Frame4E
}
```

### TimeoutConfig（タイムアウト設定）
```csharp
public class TimeoutConfig
{
    public int ConnectTimeoutMs { get; set; }     // 1000（短いタイムアウト）
    public int SendTimeoutMs { get; set; }        // 3000
    public int ReceiveTimeoutMs { get; set; }     // 3000
    public int RetryTimeoutMs { get; set; }       // 1000
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
1. TC017_ConnectAsync_TCP接続成功（正常系）
2. TC017_1, TC018（その他正常系）
3. **TC019_ConnectAsync_接続タイムアウト**（本テスト、異常系基本）
4. TC020以降: その他異常系

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockSocket**: タイムアウトシミュレーション対応
  - ConnectAsync実行時にOperationCanceledExceptionスロー
- **MockUnreachableServer**: 到達不可能サーバーシミュレーション
  - 接続要求に応答しない（タイムアウトまで待機）

---

## 6. テストケース実装構造

### Arrange（準備）
1. **ConnectionConfig準備**（到達不可能設定）:
   - IpAddress = "192.168.100.200"（到達不可能）
   - Port = 5000
   - UseTcp = true

2. **TimeoutConfig準備**:
   - ConnectTimeoutMs = 1000（短いタイムアウト）

3. **MockUnreachableServer準備**（オプション）:
   - 接続要求に応答しない設定

4. **PlcCommunicationManagerインスタンス作成**

### Act（実行）
1. ConnectAsync実行（例外発生を期待）:
   ```csharp
   var exception = await Assert.ThrowsAsync<TimeoutException>(async () =>
       await plcCommManager.ConnectAsync(connectionConfig, timeoutConfig));
   ```

### Assert（検証）
1. **TimeoutException発生確認**:
   - `exception != null`
   - `exception is TimeoutException`

2. **例外メッセージ検証**:
   - `exception.Message.Contains("接続タイムアウト")`
   - `exception.Message.Contains("192.168.100.200:5000")`
   - `exception.Message.Contains("1000ms")`

3. **タイムアウト時間精度確認**:
   - 処理時間 ≈ ConnectTimeoutMs（誤差±200ms）

4. **リソース状態確認**:
   - ソケットDispose済み
   - 接続状態: NotConnected または Failed

---

## 7. エラーハンドリング

### ConnectAsync スロー例外（TC019で検証）
**TimeoutException**:
- **発生条件**: ConnectTimeoutMs超過
- **メッセージ形式**: "接続タイムアウト: {IpAddress}:{Port}（タイムアウト時間: {ConnectTimeoutMs}ms）"
- **ErrorMessages定数**: ErrorMessages.ConnectionTimeout

### エラーメッセージ統一
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    public const string ConnectionTimeout = "接続タイムアウト: {0}:{1}（タイムアウト時間: {2}ms）";
}
```

### エラー分類（ErrorHandler連携）
- **ErrorCategory**: CommunicationError
- **Severity**: Error
- **ShouldRetry**: true（リトライ推奨、TC019ではStep3のリトライ方針に従う）
- **ErrorAction**: Abort（処理中断）

---

## 8. ログ出力要件

### LoggingManager連携
- **接続開始ログ**: 接続先情報、タイムアウト設定
- **エラーログ**: TimeoutException詳細、スタックトレース

### ログレベル
- **Information**: 接続開始
- **Error**: タイムアウト発生

### ログ出力例
```
[Information] TCP接続開始: 192.168.100.200:5000, タイムアウト=1000ms
[Error] 接続タイムアウト: 192.168.100.200:5000（タイムアウト時間: 1000ms）
  Exception: TimeoutException
  StackTrace: ...
```

---

## 9. テスト実装チェックリスト

### TC019実装前
- [ ] TC017（TCP接続成功）完了済み
- [ ] ConnectAsyncでタイムアウト制御実装済み
- [ ] ErrorMessages.ConnectionTimeout定義済み

### TC019実装中
- [ ] Arrange: ConnectionConfig準備（到達不可能IPアドレス）
- [ ] Arrange: TimeoutConfig準備（短いタイムアウト）
- [ ] Act: ConnectAsync呼び出し（Assert.ThrowsAsync内）
- [ ] Assert: TimeoutException発生確認
- [ ] Assert: 例外メッセージ検証
- [ ] Assert: タイムアウト時間精度確認
- [ ] Assert: リソースクリーンアップ確認

### TC019実装後
- [ ] テスト実行・Red確認（例外がスローされない場合）
- [ ] ConnectAsync実装でタイムアウト処理確認・修正
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施
- [ ] TC020（接続拒否）への準備

---

## 10. 参考情報

### 到達不可能IPアドレスの選定
- **192.168.100.x**: プライベートネットワーク範囲、通常は到達不可能
- **10.255.255.x**: プライベートネットワーク範囲（代替案）
- **注意**: 実環境で使用されていないIPアドレスを選択

### タイムアウト時間の選定
- **1000ms**: 短いタイムアウト、テスト実行時間短縮
- **注意**: 環境によっては実際の接続失敗時間が異なる可能性

---

## 11. PySLMPClient実装参考情報

### タイムアウト処理（Python実装例）
```python
import socket

class SLMPClient:
    def connect_tcp(self, ip_address, port, timeout_sec):
        self.__socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.__socket.settimeout(timeout_sec)

        try:
            self.__socket.connect((ip_address, port))
        except socket.timeout:
            raise TimeoutError(f"Connection timeout: {ip_address}:{port}")
```

### C#実装例（TC019対応）
```csharp
public async Task<ConnectionResponse> ConnectAsync(
    ConnectionConfig connectionConfig,
    TimeoutConfig timeoutConfig)
{
    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    var cts = new CancellationTokenSource(timeoutConfig.ConnectTimeoutMs);
    IPAddress ipAddress = IPAddress.Parse(connectionConfig.IpAddress);
    IPEndPoint endPoint = new IPEndPoint(ipAddress, connectionConfig.Port);

    try
    {
        await socket.ConnectAsync(endPoint, cts.Token);
        // 成功時の処理...
    }
    catch (OperationCanceledException)
    {
        // タイムアウト発生 - TC019検証ポイント
        socket.Dispose(); // リソースクリーンアップ
        throw new TimeoutException(
            string.Format(ErrorMessages.ConnectionTimeout,
                connectionConfig.IpAddress,
                connectionConfig.Port,
                timeoutConfig.ConnectTimeoutMs));
    }
    finally
    {
        // リソースクリーンアップ（例外発生時も実行）
        cts?.Dispose();
    }
}
```

---

以上が TC019_ConnectAsync_接続タイムアウトテスト実装に必要な情報です。
