# Step3 ConnectAsync TCP接続成功テスト実装用情報（TC017）

## ドキュメント概要

### 目的
このドキュメントは、TC017_ConnectAsync_TCP接続成功テストの実装に必要な情報を集約したものです。
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
- `PySLMPClient/pyslmpclient/util.py` - フレーム作成ユーティリティ
- `PySLMPClient/tests/test_main.py` - テストケース実例

---

## 1. テスト対象メソッド仕様

### ConnectAsync（Step3: PLC接続処理）
**クラス**: PlcCommunicationManager
**名前空間**: andon.Core.Managers

#### Input
- ConnectionConfig（IpAddress, Port, UseTcp, ConnectionType, IsBinary, FrameVersion）
  - IpAddress: "192.168.1.10"（TCP接続用IPアドレス）
  - Port: 5000（TCP接続用ポート番号）
  - UseTcp: true（TCP接続モード）
  - ConnectionType: "TCP"（ログ出力用文字列表現）
  - IsBinary: false（ASCII形式通信）
  - FrameVersion: FrameVersion.Frame4E（4Eフレーム使用）
- TimeoutConfig（ConnectTimeoutMs, SendTimeoutMs, ReceiveTimeoutMs）
  - ConnectTimeoutMs: 5000（接続タイムアウト: 5秒）
  - SendTimeoutMs: 3000（送信タイムアウト: 3秒）
  - ReceiveTimeoutMs: 3000（受信タイムアウト: 3秒）

#### Output
- 成功時: ConnectionResponse（接続処理結果オブジェクト）
  - Status: ConnectionStatus.Connected（接続成功）
  - Socket: System.Net.Sockets.Socket型インスタンス（接続済みTCPソケット）
  - RemoteEndPoint: System.Net.EndPoint型（"192.168.1.10:5000"）
  - ConnectedAt: DateTime型（接続完了時刻）
  - ConnectionTime: TimeSpan型（接続処理時間）
  - ErrorMessage: null（成功時はnull）
- 失敗時: 例外スロー
  - TimeoutException: 接続タイムアウト時
  - SocketException: 接続拒否・ネットワークエラー時
  - ArgumentException: 不正なIPアドレス・ポート番号時
  - InvalidOperationException: 既に接続済み状態での再接続試行時

#### 機能
- TCP/IP接続処理
- ソケット作成・設定
  - Socket.SendTimeout設定（TimeoutConfig.SendTimeoutMsから）
  - Socket.ReceiveTimeout設定（TimeoutConfig.ReceiveTimeoutMsから）
- 接続タイムアウト制御（ConnectTimeoutMs適用）
- 接続時間計測
- エンドポイント記録
- 接続状態管理

#### データ取得元
- ConfigToFrameManager.LoadConfigAsync()（ConnectionConfig, TimeoutConfig）
- 実機PLC環境またはモックTCPサーバー

---

## 2. テストケース仕様（TC017）

### TC017_ConnectAsync_TCP接続成功
**目的**: TCP接続機能が正常に動作することをテスト

#### 前提条件
- 有効なConnectionConfig設定（TCP接続用）
- 有効なTimeoutConfig設定
- 接続先TCPサーバーが稼働中（実機PLCまたはモック）
- ネットワーク到達可能

#### 入力データ
**ConnectionConfig**:
- IpAddress: "192.168.1.10"
- Port: 5000
- UseTcp: true
- ConnectionType: "TCP"
- IsBinary: false
- FrameVersion: FrameVersion.Frame4E

**TimeoutConfig**:
- ConnectTimeoutMs: 5000
- SendTimeoutMs: 3000
- ReceiveTimeoutMs: 3000

#### 期待出力
- ConnectionResponse（接続成功オブジェクト）
  - Status = ConnectionStatus.Connected
  - Socket != null（TCPソケットインスタンス生成済み）
  - Socket.Connected == true（ソケット接続済み状態）
  - Socket.ProtocolType == ProtocolType.Tcp（TCPプロトコル）
  - RemoteEndPoint != null
  - RemoteEndPoint.ToString() == "192.168.1.10:5000"
  - ConnectedAt != null
  - ConnectedAt.Value <= DateTime.UtcNow（接続時刻が現在時刻以前）
  - ConnectionTime != null
  - ConnectionTime.Value.TotalMilliseconds > 0（接続処理時間が正の値）
  - ConnectionTime.Value.TotalMilliseconds < ConnectTimeoutMs（タイムアウト以内）
  - ErrorMessage == null（成功時はエラーメッセージなし）

#### 動作フロー成功条件
1. **接続状態チェック**: 未接続状態であることを確認
2. **ソケット作成**: TCP用Socketインスタンス生成
3. **タイムアウト設定**: Socket.SendTimeout, Socket.ReceiveTimeout設定
4. **接続実行**: Socket.ConnectAsync実行、ConnectTimeoutMs適用
5. **接続完了**: Socket.Connected == trueを確認
6. **時間計測**: 接続処理時間を正確に記録
7. **エンドポイント記録**: RemoteEndPoint設定
8. **接続時刻記録**: ConnectedAt設定
9. **戻り値生成**: ConnectionResponse生成・返却

---

## 3. TCP接続処理詳細

### TCP/IP接続手順
**標準TCPハンドシェイク（3ウェイハンドシェイク）**:
1. クライアント → サーバー: SYNパケット送信
2. サーバー → クライアント: SYN-ACKパケット返信
3. クライアント → サーバー: ACKパケット送信
4. 接続確立完了

### C# Socket TCP接続実装
```csharp
// TCP用ソケット作成
var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

// タイムアウト設定
socket.SendTimeout = timeoutConfig.SendTimeoutMs;       // 3000ms
socket.ReceiveTimeout = timeoutConfig.ReceiveTimeoutMs; // 3000ms

// エンドポイント作成
IPAddress ipAddress = IPAddress.Parse(connectionConfig.IpAddress);
IPEndPoint endPoint = new IPEndPoint(ipAddress, connectionConfig.Port);

// 接続実行（タイムアウト制御付き）
var cts = new CancellationTokenSource(timeoutConfig.ConnectTimeoutMs);
var startTime = DateTime.UtcNow;

try
{
    await socket.ConnectAsync(endPoint, cts.Token);
    var connectionTime = DateTime.UtcNow - startTime;

    // 接続成功
    return new ConnectionResponse
    {
        Status = ConnectionStatus.Connected,
        Socket = socket,
        RemoteEndPoint = socket.RemoteEndPoint,
        ConnectedAt = DateTime.UtcNow,
        ConnectionTime = connectionTime,
        ErrorMessage = null
    };
}
catch (OperationCanceledException)
{
    // タイムアウト
    throw new TimeoutException($"接続タイムアウト: {connectionConfig.IpAddress}:{connectionConfig.Port}（タイムアウト時間: {timeoutConfig.ConnectTimeoutMs}ms）");
}
catch (SocketException ex)
{
    // 接続失敗
    throw new SocketException($"接続失敗: {connectionConfig.IpAddress}:{connectionConfig.Port} - {ex.Message}");
}
```

---

## 4. 依存クラス・設定

### ConnectionConfig（接続設定）
**取得元**: ConfigToFrameManager.LoadConfigAsync()

```csharp
public class ConnectionConfig
{
    public string IpAddress { get; set; }        // 例: "192.168.1.10"
    public int Port { get; set; }                 // 例: 5000
    public bool UseTcp { get; set; }              // true (TCP使用)
    public string ConnectionType { get; set; }    // "TCP"（ログ用）
    public bool IsBinary { get; set; }            // false (ASCII形式)
    public FrameVersion FrameVersion { get; set; } // FrameVersion.Frame4E
}
```

### TimeoutConfig（タイムアウト設定）
**取得元**: ConfigToFrameManager.LoadConfigAsync()

```csharp
public class TimeoutConfig
{
    public int ConnectTimeoutMs { get; set; }     // 例: 5000（接続タイムアウト）
    public int SendTimeoutMs { get; set; }        // 例: 3000（送信タイムアウト）
    public int ReceiveTimeoutMs { get; set; }     // 例: 3000（受信タイムアウト）
    public int RetryTimeoutMs { get; set; }       // 例: 1000（リトライ間隔）
}
```

### ConnectionResponse（接続結果）
**本メソッドの出力データ型**

```csharp
public class ConnectionResponse
{
    public ConnectionStatus Status { get; set; }  // 接続状態（Connected等）
    public Socket? Socket { get; set; }           // 実際の通信用ソケット
    public EndPoint? RemoteEndPoint { get; set; } // 接続先情報
    public DateTime? ConnectedAt { get; set; }    // 接続完了時刻
    public TimeSpan? ConnectionTime { get; set; } // 接続処理時間
    public string? ErrorMessage { get; set; }     // エラーメッセージ（失敗時のみ）
}
```

### ConnectionStatus（接続状態列挙型）
**定義**: Core/Models/ConnectionStatus.cs

```csharp
public enum ConnectionStatus
{
    NotConnected,    // 未接続状態
    Connecting,      // 接続中状態
    Connected,       // 接続済み状態（TC017で期待される状態）
    Disconnecting,   // 切断中状態
    Disconnected,    // 切断済み状態
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
1. **TC017_ConnectAsync_TCP接続成功**（本テスト、最優先）
2. TC017_1_ConnectAsync_ソケットタイムアウト設定確認（TCP接続の検証拡張）
3. TC018_ConnectAsync_UDP接続成功
4. TC019-TC020: 異常系テスト

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockSocket**: TCP接続対応Socketモック
  - Connected = true を返却
  - ProtocolType = ProtocolType.Tcp
  - RemoteEndPoint = IPEndPoint インスタンス返却
- **MockTcpServer**: TCP接続受付用モックサーバー
  - 指定IPアドレス・ポートで待ち受け
  - 接続要求に即座に応答（ConnectTimeoutMs以内）

#### 使用するスタブ
- **ConnectionConfigStubs**: 接続設定スタブ（TCP設定）
- **TimeoutConfigStubs**: タイムアウト設定スタブ
- **ConnectionResponseValidator**: 期待出力検証用ヘルパー

---

## 6. テストケース実装構造

### Arrange（準備）
1. **ConnectionConfig準備**（TCP設定）:
   - IpAddress = "192.168.1.10"
   - Port = 5000
   - UseTcp = true
   - ConnectionType = "TCP"
   - IsBinary = false
   - FrameVersion = FrameVersion.Frame4E

2. **TimeoutConfig準備**:
   - ConnectTimeoutMs = 5000
   - SendTimeoutMs = 3000
   - ReceiveTimeoutMs = 3000

3. **MockTcpServer準備**:
   - 192.168.1.10:5000で待ち受け
   - 接続要求に即座に応答

4. **PlcCommunicationManagerインスタンス作成**:
   - 未接続状態で初期化

### Act（実行）
1. ConnectAsync実行:
   ```csharp
   var result = await plcCommManager.ConnectAsync(
       connectionConfig,
       timeoutConfig
   );
   ```

### Assert（検証）
1. **接続状態検証**:
   - `result.Status == ConnectionStatus.Connected`
   - `result.Socket != null`
   - `result.Socket.Connected == true`
   - `result.Socket.ProtocolType == ProtocolType.Tcp`

2. **エンドポイント検証**:
   - `result.RemoteEndPoint != null`
   - `result.RemoteEndPoint.ToString() == "192.168.1.10:5000"`

3. **時刻・時間検証**:
   - `result.ConnectedAt != null`
   - `result.ConnectedAt.Value <= DateTime.UtcNow`
   - `result.ConnectionTime != null`
   - `result.ConnectionTime.Value.TotalMilliseconds > 0`
   - `result.ConnectionTime.Value.TotalMilliseconds < timeoutConfig.ConnectTimeoutMs`

4. **エラー情報検証**:
   - `result.ErrorMessage == null`

5. **ソケットタイムアウト設定検証**（TC017_1で詳細検証）:
   - `result.Socket.SendTimeout == timeoutConfig.SendTimeoutMs`（3000）
   - `result.Socket.ReceiveTimeout == timeoutConfig.ReceiveTimeoutMs`（3000）

---

## 7. DIコンテナ設定

### サービスライフタイム
- **PlcCommunicationManager**: Transient（PLC別インスタンス）
- **ConfigToFrameManager**: Transient（設定別インスタンス）
- **LoggingManager**: Singleton（共有リソース）
- **ErrorHandler**: Singleton（共有リソース）

### インターフェース登録
```csharp
services.AddTransient<IPlcCommunicationManager, PlcCommunicationManager>();
services.AddTransient<IConfigToFrameManager, ConfigToFrameManager>();
services.AddSingleton<ILoggingManager, LoggingManager>();
services.AddSingleton<IErrorHandler, ErrorHandler>();
```

---

## 8. エラーハンドリング

### ConnectAsync スロー例外（TC017では発生しない）
- **TimeoutException**: 接続タイムアウト（ConnectTimeoutMs超過）
  - メッセージ: "接続タイムアウト: {IpAddress}:{Port}（タイムアウト時間: {ConnectTimeoutMs}ms）"
- **SocketException**: 接続拒否、ネットワークエラー
  - メッセージ: "接続失敗: {IpAddress}:{Port} - {詳細}"
- **ArgumentException**: 不正なIPアドレス・ポート番号
  - メッセージ: "不正なIPアドレス形式: {IpAddress}" または "ポート番号が範囲外です: {Port}"
- **InvalidOperationException**: 既に接続済み状態での再接続試行
  - メッセージ: "既に接続済みです。再接続する場合は先にDisconnectAsync()を実行してください"

### エラーメッセージ統一
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    // 接続関連
    public const string ConnectionTimeout = "接続タイムアウト: {0}:{1}（タイムアウト時間: {2}ms）";
    public const string ConnectionFailed = "接続失敗: {0}:{1} - {2}";
    public const string AlreadyConnected = "既に接続済みです。再接続する場合は先にDisconnectAsync()を実行してください";

    // 入力検証
    public const string InvalidIpAddress = "不正なIPアドレス形式: {0}";
    public const string InvalidPort = "ポート番号が範囲外です: {0}（有効範囲: 1-65535）";
}
```

---

## 9. ログ出力要件

### LoggingManager連携
- **接続開始ログ**: 接続先情報（IpAddress:Port）、接続モード（TCP/UDP）
- **接続完了ログ**: 接続時間、エンドポイント情報
- **エラーログ**: 例外詳細、スタックトレース（TC017では発生しない）

### ログレベル
- **Information**: 接続開始・完了
- **Debug**: ソケット設定詳細、タイムアウト設定
- **Error**: 例外発生時（TC017では発生しない）

### ログ出力例
```
[Information] TCP接続開始: 192.168.1.10:5000, タイムアウト=5000ms
[Debug] ソケット設定: SendTimeout=3000ms, ReceiveTimeout=3000ms
[Information] TCP接続完了: 192.168.1.10:5000, 接続時間=120ms
```

---

## 10. テスト実装チェックリスト

### TC017実装前
- [ ] PlcCommunicationManagerクラス作成（空実装）
- [ ] IPlcCommunicationManagerインターフェース作成
- [ ] ConnectAsyncメソッドシグネチャ定義
- [ ] ConnectionResponseモデル作成
- [ ] ConnectionStatusモデル作成
- [ ] ConnectionConfig, TimeoutConfigモデル作成
- [ ] MockSocket, MockTcpServer作成

### TC017実装中
- [ ] Arrange: ConnectionConfig準備（TCP設定）
- [ ] Arrange: TimeoutConfig準備
- [ ] Arrange: MockTcpServer起動
- [ ] Act: ConnectAsync呼び出し
- [ ] Assert: Status検証（Connected）
- [ ] Assert: Socket検証（TCP接続済み）
- [ ] Assert: RemoteEndPoint検証
- [ ] Assert: ConnectedAt検証
- [ ] Assert: ConnectionTime検証
- [ ] Assert: ErrorMessage検証（null）

### TC017実装後
- [ ] テスト実行・Red確認
- [ ] ConnectAsync本体実装（TCP接続処理）
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施
- [ ] TC017_1（ソケットタイムアウト設定確認）への準備

---

## 11. 参考情報

### TCP/IP通信仕様
- プロトコル: TCP/IP（Transmission Control Protocol / Internet Protocol）
- 3ウェイハンドシェイク: SYN → SYN-ACK → ACK
- 接続指向型通信: 信頼性の高いデータ転送
- ポート範囲: 1-65535（ウェルノウンポート: 1-1023）

**重要**: TC017ではモックTCPサーバーを使用、実機PLC不要

### テストデータサンプル
**配置先**: Tests/TestUtilities/TestData/ConnectionSamples/

- TcpConnectionConfig.json: TCP接続用設定サンプル
- TcpConnectionResponse.json: TCP接続成功時の期待出力サンプル

---

## 12. PySLMPClient実装参考情報

### TCP接続実装（Python実装例）

#### PySLMPClientでのTCP接続
```python
import socket

class SLMPClient:
    def connect_tcp(self, ip_address, port, timeout_sec):
        self.__socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.__socket.settimeout(timeout_sec)

        try:
            start_time = time.time()
            self.__socket.connect((ip_address, port))
            connection_time = time.time() - start_time

            self.__is_connected = True
            return {
                'status': 'Connected',
                'connection_time': connection_time,
                'remote_endpoint': f"{ip_address}:{port}"
            }
        except socket.timeout:
            raise TimeoutError(f"Connection timeout: {ip_address}:{port}")
        except socket.error as e:
            raise ConnectionError(f"Connection failed: {e}")
```

#### C#実装例（TC017対応）
```csharp
public async Task<ConnectionResponse> ConnectAsync(
    ConnectionConfig connectionConfig,
    TimeoutConfig timeoutConfig)
{
    // 接続状態チェック
    if (_socket != null && _socket.Connected)
    {
        throw new InvalidOperationException(ErrorMessages.AlreadyConnected);
    }

    // TCP用ソケット作成
    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    // タイムアウト設定
    _socket.SendTimeout = timeoutConfig.SendTimeoutMs;
    _socket.ReceiveTimeout = timeoutConfig.ReceiveTimeoutMs;

    // エンドポイント作成
    IPAddress ipAddress = IPAddress.Parse(connectionConfig.IpAddress);
    IPEndPoint endPoint = new IPEndPoint(ipAddress, connectionConfig.Port);

    // 接続実行（タイムアウト制御）
    var cts = new CancellationTokenSource(timeoutConfig.ConnectTimeoutMs);
    var startTime = DateTime.UtcNow;

    try
    {
        await _socket.ConnectAsync(endPoint, cts.Token);
        var connectionTime = DateTime.UtcNow - startTime;

        _connectionStatus = ConnectionStatus.Connected;

        return new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            Socket = _socket,
            RemoteEndPoint = _socket.RemoteEndPoint,
            ConnectedAt = DateTime.UtcNow,
            ConnectionTime = connectionTime,
            ErrorMessage = null
        };
    }
    catch (OperationCanceledException)
    {
        throw new TimeoutException(
            string.Format(ErrorMessages.ConnectionTimeout,
                connectionConfig.IpAddress,
                connectionConfig.Port,
                timeoutConfig.ConnectTimeoutMs));
    }
    catch (SocketException ex)
    {
        throw new SocketException(
            string.Format(ErrorMessages.ConnectionFailed,
                connectionConfig.IpAddress,
                connectionConfig.Port,
                ex.Message));
    }
}
```

### 実装時の重要ポイント

1. **ソケットタイムアウト設定**: Socket.SendTimeout, Socket.ReceiveTimeoutを接続後すぐに設定
2. **接続時間計測**: DateTime.UtcNowを使用した正確な時間計測
3. **エンドポイント記録**: Socket.RemoteEndPointによる接続先情報取得
4. **エラーハンドリング**: OperationCanceledException（タイムアウト）とSocketException（接続失敗）の区別
5. **接続状態管理**: _connectionStatus フィールドによる状態追跡

---

以上が TC017_ConnectAsync_TCP接続成功テスト実装に必要な情報です。
