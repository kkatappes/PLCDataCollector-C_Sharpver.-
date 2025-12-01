# Step3 ConnectAsync UDP接続成功テスト実装用情報（TC018）

## ドキュメント概要

### 目的
このドキュメントは、TC018_ConnectAsync_UDP接続成功テストの実装に必要な情報を集約したものです。
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

### ConnectAsync（Step3: PLC接続処理）
**クラス**: PlcCommunicationManager
**名前空間**: andon.Core.Managers

#### Input
- ConnectionConfig（IpAddress, Port, UseTcp, ConnectionType, IsBinary, FrameVersion）
  - IpAddress: "192.168.1.10"（UDP接続用IPアドレス）
  - Port: 5000（UDP接続用ポート番号）
  - **UseTcp: false（UDP接続モード）**
  - **ConnectionType: "UDP"（ログ出力用文字列表現）**
  - IsBinary: false（ASCII形式通信）
  - FrameVersion: FrameVersion.Frame4E（4Eフレーム使用）
- TimeoutConfig（ConnectTimeoutMs, SendTimeoutMs, ReceiveTimeoutMs）
  - ConnectTimeoutMs: 5000（接続タイムアウト: 5秒）
  - SendTimeoutMs: 3000（送信タイムアウト: 3秒）
  - ReceiveTimeoutMs: 3000（受信タイムアウト: 3秒）

#### Output
- 成功時: ConnectionResponse（UDP接続処理結果オブジェクト）
  - Status: ConnectionStatus.Connected（接続成功）
  - Socket: System.Net.Sockets.Socket型インスタンス（接続済みUDPソケット）
  - RemoteEndPoint: System.Net.EndPoint型（"192.168.1.10:5000"）
  - ConnectedAt: DateTime型（接続完了時刻）
  - ConnectionTime: TimeSpan型（接続処理時間、疎通確認含む）
  - ErrorMessage: null（成功時はnull）
- 失敗時: 例外スロー
  - TimeoutException: 疎通確認タイムアウト時
  - SocketException: ネットワークエラー時
  - ArgumentException: 不正なIPアドレス・ポート番号時
  - InvalidOperationException: 既に接続済み状態での再接続試行時

#### 機能（UDP接続固有）
- UDP/IP接続処理
- ソケット作成・設定
  - Socket.SendTimeout設定（TimeoutConfig.SendTimeoutMsから）
  - Socket.ReceiveTimeout設定（TimeoutConfig.ReceiveTimeoutMsから）
- **UDP疎通確認（TDD・オフライン環境対応）**:
  - 模擬送信フレーム（M000-M999またはD000-D999読み込み）送信
  - ConnectTimeoutMs内に応答受信確認
  - 応答なし→TimeoutExceptionスロー
- 接続タイムアウト制御（ConnectTimeoutMs適用）
- 接続時間計測（疎通確認含む）
- エンドポイント記録
- 接続状態管理

#### データ取得元
- ConfigToFrameManager.LoadConfigAsync()（ConnectionConfig, TimeoutConfig）
- 実機PLC環境、PLCシミュレータ、またはモックUDPサーバー

---

## 2. テストケース仕様（TC018）

### TC018_ConnectAsync_UDP接続成功
**目的**: UDP接続機能が正常に動作することをテスト（疎通確認を含む）

#### 前提条件
- 有効なConnectionConfig設定（UDP接続用）
- 有効なTimeoutConfig設定
- 接続先UDPサーバーが稼働中（実機PLC、PLCシミュレータ、またはモック）
- ネットワーク到達可能
- **疎通確認用の模擬フレーム送信対応**

#### 入力データ
**ConnectionConfig**:
- IpAddress: "192.168.1.10"
- Port: 5000
- **UseTcp: false（UDP接続）**
- **ConnectionType: "UDP"**
- IsBinary: false
- FrameVersion: FrameVersion.Frame4E

**TimeoutConfig**:
- ConnectTimeoutMs: 5000
- SendTimeoutMs: 3000
- ReceiveTimeoutMs: 3000

**疎通確認用模擬フレーム（TC018で使用）**:
- **M000-M999読み込みフレーム**: "54001234000000010401006400000090E8030000"
  - 構成: サブヘッダ(54001234000000) + READコマンド(0104) + サブコマンド(0100:ビット単位) + デバイスコード(6400:M機器) + 開始番号(00000090:M000) + デバイス点数(E8030000:1000点)
- **D000-D999読み込みフレーム**: "54001234000000010400A800000090E8030000"
  - 構成: サブヘッダ(54001234000000) + READコマンド(0104) + サブコマンド(0000:ワード単位) + デバイスコード(A800:D機器) + 開始番号(00000090:D000) + デバイス点数(E8030000:1000点)

#### 期待出力
- ConnectionResponse（UDP接続成功オブジェクト）
  - Status = ConnectionStatus.Connected
  - Socket != null（UDPソケットインスタンス生成済み）
  - Socket.Connected == false（UDP接続なし・正常動作）※重要
  - Socket.ProtocolType == ProtocolType.Udp（UDPプロトコル）
  - RemoteEndPoint != null
  - RemoteEndPoint.ToString() == "192.168.1.10:5000"
  - ConnectedAt != null
  - ConnectedAt.Value <= DateTime.UtcNow（接続時刻が現在時刻以前）
  - ConnectionTime != null
  - ConnectionTime.Value.TotalMilliseconds > 0（接続処理時間が正の値、疎通確認含む）
  - ConnectionTime.Value.TotalMilliseconds < ConnectTimeoutMs（タイムアウト以内）
  - ErrorMessage == null（成功時はエラーメッセージなし）

#### UDP疎通確認方法（TDD・オフライン環境対応）
**テスト実施方法**:
- PLCシミュレータまたはネットワークモックを使用
- 模擬送信フレーム（M000-M999またはD000-D999読み込み）送信
- 模擬応答データ: モックがConnectTimeoutMs内に正常応答を返却
- **検証項目**: Socket.Connected状態（false）、RemoteEndPoint設定、応答受信完了の確認
- **重要**: 実際のPLC機器不要、完全オフライン環境でのテスト実施

#### 動作フロー成功条件
1. **接続状態チェック**: 未接続状態であることを確認
2. **ソケット作成**: UDP用Socketインスタンス生成
3. **タイムアウト設定**: Socket.SendTimeout, Socket.ReceiveTimeout設定
4. **エンドポイント設定**: Socket.Connect()実行（送信先設定のみ）
5. **疎通確認**:
   - 模擬フレーム送信（M000-M999またはD000-D999読み込みフレーム）
   - ConnectTimeoutMs内に応答受信確認
   - 応答なし→TimeoutExceptionスロー
6. **接続完了**: 疎通確認成功を確認
7. **時間計測**: 接続処理時間を正確に記録（疎通確認含む）
8. **エンドポイント記録**: RemoteEndPoint設定
9. **接続時刻記録**: ConnectedAt設定
10. **戻り値生成**: ConnectionResponse生成・返却

---

## 3. UDP接続処理詳細

### UDP/IP接続手順
**UDP接続の特性**:
- **コネクションレス型通信**: TCPのような接続確立手順（3ウェイハンドシェイク）なし
- **Socket.Connect()の意味**: 送信先アドレス・ポートの設定のみ（実際の接続確立なし）
- **疎通確認の必要性**: 実際の通信可能性を確認するため、模擬フレーム送受信実施

### C# Socket UDP接続実装
```csharp
// UDP用ソケット作成
var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

// タイムアウト設定
socket.SendTimeout = timeoutConfig.SendTimeoutMs;       // 3000ms
socket.ReceiveTimeout = timeoutConfig.ReceiveTimeoutMs; // 3000ms

// エンドポイント作成
IPAddress ipAddress = IPAddress.Parse(connectionConfig.IpAddress);
IPEndPoint endPoint = new IPEndPoint(ipAddress, connectionConfig.Port);

// 送信先設定（UDP接続）
socket.Connect(endPoint);

// 疎通確認（模擬フレーム送受信）
var startTime = DateTime.UtcNow;
var verificationFrame = "54001234000000010401006400000090E8030000"; // M000-M999読み込み
byte[] frameBytes = ConvertHexStringToBytes(verificationFrame);

try
{
    // フレーム送信
    int sentBytes = socket.Send(frameBytes);

    // 応答受信（ConnectTimeoutMs内）
    byte[] receiveBuffer = new byte[1024];
    var cts = new CancellationTokenSource(timeoutConfig.ConnectTimeoutMs);
    int receivedBytes = await socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), SocketFlags.None, cts.Token);

    var connectionTime = DateTime.UtcNow - startTime;

    // 疎通確認成功
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
    // 疎通確認タイムアウト
    throw new TimeoutException($"UDP疎通確認タイムアウト: {connectionConfig.IpAddress}:{connectionConfig.Port}（タイムアウト時間: {timeoutConfig.ConnectTimeoutMs}ms）");
}
catch (SocketException ex)
{
    // ネットワークエラー
    throw new SocketException($"UDP接続失敗: {connectionConfig.IpAddress}:{connectionConfig.Port} - {ex.Message}");
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
    public bool UseTcp { get; set; }              // false (UDP使用)
    public string ConnectionType { get; set; }    // "UDP"（ログ用）
    public bool IsBinary { get; set; }            // false (ASCII形式)
    public FrameVersion FrameVersion { get; set; } // FrameVersion.Frame4E
}
```

### TimeoutConfig（タイムアウト設定）
**取得元**: ConfigToFrameManager.LoadConfigAsync()

```csharp
public class TimeoutConfig
{
    public int ConnectTimeoutMs { get; set; }     // 例: 5000（疎通確認タイムアウト）
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
    public TimeSpan? ConnectionTime { get; set; } // 接続処理時間（疎通確認含む）
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
    Connected,       // 接続済み状態（TC018で期待される状態）
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
1. TC017_ConnectAsync_TCP接続成功
2. TC017_1_ConnectAsync_ソケットタイムアウト設定確認
3. **TC018_ConnectAsync_UDP接続成功**（本テスト）
4. TC019以降: 異常系テスト

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockSocket**: UDP接続対応Socketモック
  - Connected = false を返却（UDP正常動作）
  - ProtocolType = ProtocolType.Udp
  - RemoteEndPoint = IPEndPoint インスタンス返却
  - Send(), ReceiveAsync() メソッド実装
- **MockUdpServer**: UDP疎通確認用モックサーバー
  - 指定IPアドレス・ポートで待ち受け
  - 模擬フレーム受信時に即座に応答（ConnectTimeoutMs以内）

#### 使用するスタブ
- **ConnectionConfigStubs**: 接続設定スタブ（UDP設定）
- **TimeoutConfigStubs**: タイムアウト設定スタブ
- **ConnectionResponseValidator**: 期待出力検証用ヘルパー
- **UdpFrameStubs**: 疎通確認用模擬フレームスタブ（M000-M999, D000-D999読み込み）

---

## 6. テストケース実装構造

### Arrange（準備）
1. **ConnectionConfig準備**（UDP設定）:
   - IpAddress = "192.168.1.10"
   - Port = 5000
   - **UseTcp = false（UDP接続）**
   - **ConnectionType = "UDP"**
   - IsBinary = false
   - FrameVersion = FrameVersion.Frame4E

2. **TimeoutConfig準備**:
   - ConnectTimeoutMs = 5000
   - SendTimeoutMs = 3000
   - ReceiveTimeoutMs = 3000

3. **MockUdpServer準備**:
   - 192.168.1.10:5000で待ち受け
   - 模擬フレーム受信時に即座に応答

4. **疎通確認用模擬フレーム準備**:
   - M000-M999読み込みフレーム: "54001234000000010401006400000090E8030000"

5. **PlcCommunicationManagerインスタンス作成**:
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
   - **`result.Socket.Connected == false`（UDP正常動作、重要）**
   - `result.Socket.ProtocolType == ProtocolType.Udp`

2. **エンドポイント検証**:
   - `result.RemoteEndPoint != null`
   - `result.RemoteEndPoint.ToString() == "192.168.1.10:5000"`

3. **時刻・時間検証**:
   - `result.ConnectedAt != null`
   - `result.ConnectedAt.Value <= DateTime.UtcNow`
   - `result.ConnectionTime != null`
   - `result.ConnectionTime.Value.TotalMilliseconds > 0`（疎通確認含む）
   - `result.ConnectionTime.Value.TotalMilliseconds < timeoutConfig.ConnectTimeoutMs`

4. **エラー情報検証**:
   - `result.ErrorMessage == null`

5. **ソケットタイムアウト設定検証**:
   - `result.Socket.SendTimeout == timeoutConfig.SendTimeoutMs`（3000）
   - `result.Socket.ReceiveTimeout == timeoutConfig.ReceiveTimeoutMs`（3000）

6. **疎通確認検証**:
   - MockUdpServer が模擬フレームを受信したこと
   - MockUdpServer が応答を返却したこと
   - 応答受信がConnectTimeoutMs以内に完了したこと

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

### ConnectAsync スロー例外（TC018では発生しない）
- **TimeoutException**: 疎通確認タイムアウト（ConnectTimeoutMs超過）
  - メッセージ: "UDP疎通確認タイムアウト: {IpAddress}:{Port}（タイムアウト時間: {ConnectTimeoutMs}ms）"
- **SocketException**: ネットワークエラー
  - メッセージ: "UDP接続失敗: {IpAddress}:{Port} - {詳細}"
- **ArgumentException**: 不正なIPアドレス・ポート番号
  - メッセージ: "不正なIPアドレス形式: {IpAddress}" または "ポート番号が範囲外です: {Port}"
- **InvalidOperationException**: 既に接続済み状態での再接続試行
  - メッセージ: "既に接続済みです。再接続する場合は先にDisconnectAsync()を実行してください"

### エラーメッセージ統一
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    // UDP接続関連
    public const string UdpVerificationTimeout = "UDP疎通確認タイムアウト: {0}:{1}（タイムアウト時間: {2}ms）";
    public const string UdpConnectionFailed = "UDP接続失敗: {0}:{1} - {2}";

    // 共通接続関連
    public const string AlreadyConnected = "既に接続済みです。再接続する場合は先にDisconnectAsync()を実行してください";
    public const string InvalidIpAddress = "不正なIPアドレス形式: {0}";
    public const string InvalidPort = "ポート番号が範囲外です: {0}（有効範囲: 1-65535）";
}
```

---

## 9. ログ出力要件

### LoggingManager連携
- **接続開始ログ**: 接続先情報（IpAddress:Port）、接続モード（UDP）、疎通確認開始
- **疎通確認ログ**: 模擬フレーム送信、応答受信確認
- **接続完了ログ**: 接続時間（疎通確認含む）、エンドポイント情報
- **エラーログ**: 例外詳細、スタックトレース（TC018では発生しない）

### ログレベル
- **Information**: 接続開始・疎通確認・接続完了
- **Debug**: ソケット設定詳細、タイムアウト設定、模擬フレーム詳細
- **Error**: 例外発生時（TC018では発生しない）

### ログ出力例
```
[Information] UDP接続開始: 192.168.1.10:5000, タイムアウト=5000ms
[Debug] ソケット設定: SendTimeout=3000ms, ReceiveTimeout=3000ms
[Information] UDP疎通確認開始: フレーム送信（M000-M999読み込み）
[Debug] 模擬フレーム送信: 54001234000000010401006400000090E8030000（38バイト）
[Information] UDP疎通確認成功: 応答受信完了（120ms）
[Information] UDP接続完了: 192.168.1.10:5000, 接続時間=150ms（疎通確認含む）
```

---

## 10. テスト実装チェックリスト

### TC018実装前
- [ ] PlcCommunicationManagerクラス作成（空実装またはTCP実装済み）
- [ ] IPlcCommunicationManagerインターフェース作成
- [ ] ConnectAsyncメソッドでUDP対応追加
- [ ] ConnectionResponseモデル作成
- [ ] ConnectionStatusモデル作成
- [ ] ConnectionConfig, TimeoutConfigモデル作成
- [ ] MockSocket（UDP対応）, MockUdpServer作成
- [ ] 疎通確認用模擬フレームスタブ作成

### TC018実装中
- [ ] Arrange: ConnectionConfig準備（UDP設定）
- [ ] Arrange: TimeoutConfig準備
- [ ] Arrange: MockUdpServer起動
- [ ] Arrange: 疎通確認用模擬フレーム準備
- [ ] Act: ConnectAsync呼び出し
- [ ] Assert: Status検証（Connected）
- [ ] Assert: Socket検証（UDP、Connected=false）
- [ ] Assert: RemoteEndPoint検証
- [ ] Assert: ConnectedAt検証
- [ ] Assert: ConnectionTime検証（疎通確認含む）
- [ ] Assert: ErrorMessage検証（null）
- [ ] Assert: 疎通確認動作検証

### TC018実装後
- [ ] テスト実行・Red確認
- [ ] ConnectAsync本体実装（UDP接続処理・疎通確認）
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施
- [ ] TC019（接続タイムアウト）への準備

---

## 11. 参考情報

### UDP/IP通信仕様
- プロトコル: UDP/IP（User Datagram Protocol / Internet Protocol）
- **コネクションレス型通信**: 接続確立手順なし
- **Socket.Connect()の意味**: 送信先設定のみ、実際の接続なし
- **疎通確認の必要性**: 実際の通信可能性を確認するため
- ポート範囲: 1-65535

### UDP vs TCP比較
| 項目 | TCP | UDP |
|------|-----|-----|
| 接続確立 | 3ウェイハンドシェイク | なし |
| Socket.Connected | true（接続後） | false（常に） |
| 信頼性 | 高（再送制御あり） | 低（再送なし） |
| 速度 | 比較的遅い | 速い |
| 用途 | 確実な通信 | リアルタイム通信 |

**重要**: TC018ではモックUDPサーバーを使用、実機PLC不要

### テストデータサンプル
**配置先**: Tests/TestUtilities/TestData/ConnectionSamples/

- UdpConnectionConfig.json: UDP接続用設定サンプル
- UdpConnectionResponse.json: UDP接続成功時の期待出力サンプル
- UdpVerificationFrames.json: 疎通確認用模擬フレームサンプル

---

## 12. PySLMPClient実装参考情報

### UDP接続実装（Python実装例）

#### PySLMPClientでのUDP接続
```python
import socket

class SLMPClient:
    def connect_udp(self, ip_address, port, timeout_sec):
        self.__socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.__socket.settimeout(timeout_sec)

        # UDP送信先設定
        self.__remote_address = (ip_address, port)

        try:
            start_time = time.time()

            # 疎通確認（模擬フレーム送受信）
            verification_frame = self.build_verification_frame()
            self.__socket.sendto(verification_frame, self.__remote_address)

            # 応答受信（タイムアウト制御）
            response, _ = self.__socket.recvfrom(1024)
            connection_time = time.time() - start_time

            self.__is_connected = True
            return {
                'status': 'Connected',
                'connection_time': connection_time,
                'remote_endpoint': f"{ip_address}:{port}"
            }
        except socket.timeout:
            raise TimeoutError(f"UDP verification timeout: {ip_address}:{port}")
        except socket.error as e:
            raise ConnectionError(f"UDP connection failed: {e}")
```

#### C#実装例（TC018対応）
```csharp
public async Task<ConnectionResponse> ConnectAsync(
    ConnectionConfig connectionConfig,
    TimeoutConfig timeoutConfig)
{
    // 接続状態チェック
    if (_socket != null && _connectionStatus == ConnectionStatus.Connected)
    {
        throw new InvalidOperationException(ErrorMessages.AlreadyConnected);
    }

    // UDP用ソケット作���
    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

    // タイムアウト設定
    _socket.SendTimeout = timeoutConfig.SendTimeoutMs;
    _socket.ReceiveTimeout = timeoutConfig.ReceiveTimeoutMs;

    // エンドポイント作成
    IPAddress ipAddress = IPAddress.Parse(connectionConfig.IpAddress);
    IPEndPoint endPoint = new IPEndPoint(ipAddress, connectionConfig.Port);

    // 送信先設定（UDP接続）
    _socket.Connect(endPoint);

    // 疎通確認
    var startTime = DateTime.UtcNow;
    var verificationFrame = "54001234000000010401006400000090E8030000"; // M000-M999読み込み
    byte[] frameBytes = ConvertHexStringToBytes(verificationFrame);

    try
    {
        // フレーム送信
        int sentBytes = _socket.Send(frameBytes);

        // 応答受信（ConnectTimeoutMs内）
        byte[] receiveBuffer = new byte[1024];
        var cts = new CancellationTokenSource(timeoutConfig.ConnectTimeoutMs);
        int receivedBytes = await _socket.ReceiveAsync(
            new ArraySegment<byte>(receiveBuffer),
            SocketFlags.None,
            cts.Token);

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
            string.Format(ErrorMessages.UdpVerificationTimeout,
                connectionConfig.IpAddress,
                connectionConfig.Port,
                timeoutConfig.ConnectTimeoutMs));
    }
    catch (SocketException ex)
    {
        throw new SocketException(
            string.Format(ErrorMessages.UdpConnectionFailed,
                connectionConfig.IpAddress,
                connectionConfig.Port,
                ex.Message));
    }
}
```

### 実装時の重要ポイント

1. **UDP接続の意味**: Socket.Connect()は送信先設定のみ、実際の接続確立なし
2. **Socket.Connectedの値**: UDP接続ではfalseが正常動作（重要）
3. **疎通確認の実装**: 模擬フレーム送受信により実際の通信可能性を確認
4. **タイムアウト制御**: ConnectTimeoutMsで疎通確認全体のタイムアウト管理
5. **エラーハンドリング**: OperationCanceledException（タイムアウト）とSocketException（ネットワークエラー）の区別
6. **接続時間計測**: 疎通確認を含む全体の処理時間を計測

---

以上が TC018_ConnectAsync_UDP接続成功テスト実装に必要な情報です。
