# Step3 ConnectAsync 接続時間計測精度テスト実装用情報（TC020_5）

## ドキュメント概要

### 目的
このドキュメントは、TC020_5_ConnectAsync_接続時間計測精度テストの実装に必要な情報を集約したものです。
**コード作成時に必要となる技術情報のみ**を記載しており、学習資料や説明的な内容は含みません。

### 情報取得元
本ドキュメントの情報は以下のソースから抽出・統合されています：

#### 設計書（andon/documents/design/）
- `クラス・メソッドリスト.md` - クラス・メソッドの一覧と概要
- `クラス設計.md` - 詳細なクラス設計仕様
- `テスト内容.md` - テストケース仕様
- `プロジェクト構造設計.md` - フォルダ構造・プロジェクト構成
- `依存関係.md` - クラス間の依存関係

#### 実装参考（PySLMPClient）
- `PySLMPClient/pyslmpclient/__init__.py` - 時間計測実装
- `PySLMPClient/tests/test_main.py` - テストケース実例

---

## 1. テスト対象メソッド仕様

### ConnectAsync（Step3: PLC接続処理）
**クラス**: PlcCommunicationManager
**名前空間**: andon.Core.Managers

#### Input
- ConnectionConfig（有効な接続設定）
  - IpAddress: "192.168.1.10"（正常なIPアドレス）
  - Port: 5000（正常なポート番号）
  - UseTcp: true
  - ConnectionType: "TCP"
  - IsBinary: false
  - FrameVersion: FrameVersion.Frame4E
- TimeoutConfig（有効なタイムアウト設定）
  - ConnectTimeoutMs: 5000
  - SendTimeoutMs: 3000
  - ReceiveTimeoutMs: 5000
  - RetryTimeoutMs: 1000
- **意図的遅延**: 500ms（モック・スタブで遅延制御）

#### Output
- ConnectionResponse（接続処理結果オブジェクト）
  - Status: Connected
  - Socket: 有効なSocketインスタンス
  - RemoteEndPoint: 接続先エンドポイント情報
  - ConnectedAt: 接続完了時刻（DateTime）
  - **ConnectionTime: 接続処理時間が正確に計測されていること**
  - ErrorMessage: null

#### 機能
- 接続処理時間の正確な計測
- DateTime.UtcNowを使用した高精度タイムスタンプ
- 開始時刻と完了時刻の差分計算
- ConnectionResponseへの計測結果記録

---

## 2. テストケース仕様（TC020_5）

### TC020_5_ConnectAsync_接続時間計測精度
**目的**: 接続時間計測が正確に行われることをテスト

#### 前提条件
- **意図的に遅延を発生させる環境（モック・スタブで遅延制御）**
- MockSocketで接続処理に500ms遅延を設定
- PlcCommunicationManagerインスタンスが準備済み
- 未接続状態（Status != Connected）

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
- ReceiveTimeoutMs: 5000
- RetryTimeoutMs: 1000

**意図的遅延**:
- 500ms（モックで制御）

#### 期待出力
**ConnectionResponse**:
- Status: Connected
- Socket: 有効なSocketインスタンス
- RemoteEndPoint: 接続先エンドポイント情報
- ConnectedAt: 接続完了時刻
- **ConnectionTime: >= 実際の接続時間（誤差±100ms以内）**
- **ConnectionTime: 500ms前後（誤差±100ms以内、実際は400ms～600ms）**
- ErrorMessage: null

#### 検証項目
1. **ConnectionTime >= 実際の接続時間（誤差±100ms）**
   - 期待範囲: 400ms～600ms
2. **ConnectedAt - 処理開始時刻 ≈ ConnectionTime（誤差±100ms）**
   - 時刻整合性確認
3. **時間計測の精度確認**
   - DateTime.UtcNowによる高精度計測
4. **ConnectionResponse全フィールドの正常性確認**
   - Status = Connected
   - Socket != null
   - RemoteEndPoint != null
   - ConnectedAt != null
   - ConnectionTime != null

#### 重要性
- **パフォーマンス分析**: 接続処理時間の可視化
- **タイムアウト判定の正確性**: 計測精度がタイムアウト判定に影響
- **ログ・診断**: 正確な時間記録による問題診断
- **SLA管理**: 接続時間のSLA監視

---

## 3. 依存クラス・設定

### ConnectionConfig（接続設定）
**取得元**: ConfigToFrameManager.LoadConfigAsync()

```csharp
public class ConnectionConfig
{
    public string IpAddress { get; set; }        // 例: "192.168.1.10"
    public int Port { get; set; }                 // 例: 5000
    public bool UseTcp { get; set; }              // true: TCP, false: UDP
    public string ConnectionType { get; set; }    // "TCP" or "UDP"
    public bool IsBinary { get; set; }            // false: ASCII, true: Binary
    public FrameVersion FrameVersion { get; set; } // FrameVersion.Frame4E
}
```

### TimeoutConfig（タイムアウト設定）
**取得元**: ConfigToFrameManager.LoadConfigAsync()

```csharp
public class TimeoutConfig
{
    public int ConnectTimeoutMs { get; set; }     // 例: 5000 ← ConnectAsyncで使用
    public int SendTimeoutMs { get; set; }        // 例: 3000
    public int ReceiveTimeoutMs { get; set; }     // 例: 5000
    public int RetryTimeoutMs { get; set; }       // 例: 1000
}
```

### ConnectionResponse（接続結果）
**取得元**: PlcCommunicationManager.ConnectAsync()

```csharp
public class ConnectionResponse
{
    public ConnectionStatus Status { get; set; }  // TC020_5: Connected
    public Socket? Socket { get; set; }           // TC020_5: 有効なSocketインスタンス
    public EndPoint? RemoteEndPoint { get; set; } // TC020_5: 接続先情報
    public DateTime? ConnectedAt { get; set; }    // TC020_5: 接続完了時刻
    public TimeSpan? ConnectionTime { get; set; } // TC020_5: 接続処理時間（計測対象）
    public string? ErrorMessage { get; set; }     // TC020_5: null（成功時）
}
```

### ConnectionStatus（接続状態列挙型）

```csharp
public enum ConnectionStatus
{
    Disconnected,
    Connected,     // TC020_5での期待状態
    Error
}
```

---

## 4. 時間計測仕様

### DateTime.UtcNow使用方針
- **使用理由**: タイムゾーン非依存、夏時間影響なし
- **精度**: 約1～15ms（システム依存）
- **推奨**: 高精度計測にはStopwatchクラス併用も検討

### Stopwatchクラス（高精度計測、参考）

```csharp
using System.Diagnostics;

var stopwatch = Stopwatch.StartNew();
// 処理...
stopwatch.Stop();
var elapsedMs = stopwatch.ElapsedMilliseconds;
```

### 計測実装パターン

#### パターン1: DateTime.UtcNow（シンプル、推奨）

```csharp
var startTime = DateTime.UtcNow;

// 接続処理（500ms遅延）
await socket.ConnectAsync(endPoint, cancellationToken);

var connectionTime = DateTime.UtcNow - startTime;

return new ConnectionResponse
{
    Status = ConnectionStatus.Connected,
    Socket = socket,
    RemoteEndPoint = socket.RemoteEndPoint,
    ConnectedAt = DateTime.UtcNow,
    ConnectionTime = connectionTime,  // 計測時間を正確に記録
    ErrorMessage = null
};
```

#### パターン2: Stopwatch（高精度）

```csharp
var stopwatch = Stopwatch.StartNew();
var startTime = DateTime.UtcNow;

// 接続処理（500ms遅延）
await socket.ConnectAsync(endPoint, cancellationToken);

stopwatch.Stop();

return new ConnectionResponse
{
    Status = ConnectionStatus.Connected,
    Socket = socket,
    RemoteEndPoint = socket.RemoteEndPoint,
    ConnectedAt = DateTime.UtcNow,
    ConnectionTime = stopwatch.Elapsed,  // Stopwatchによる高精度計測
    ErrorMessage = null
};
```

---

## 5. テスト実装方針（TDD）

### 開発手法
- C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.mdに記載のTDD手法を使用

### テストファイル配置
- **ファイル名**: PlcCommunicationManagerTests.cs
- **配置先**: Tests/Unit/Core/Managers/
- **名前空間**: andon.Tests.Unit.Core.Managers

### テスト実装順序（異常系→正常系）
1. **TC020_3_ConnectAsync_null入力**（最優先、最も基本的な異常系）
2. **TC020_1_ConnectAsync_不正IPアドレス**
3. **TC020_2_ConnectAsync_不正ポート番号**
4. **TC020_4_ConnectAsync_既に接続済み状態での再接続**
5. **TC020_5_ConnectAsync_接続時間計測精度**（本テスト、正常系に近い）

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockSocket**: Socket接続状態をシミュレート
  - ConnectAsync実行時に500ms遅延
  - Connected = true（接続成功）
  - RemoteEndPointを返す

#### 使用するスタブ
- **ConfigurationStubs**: ConnectionConfig・TimeoutConfigのスタブ
- **DelayedSocketStub**: 遅延制御可能なSocketスタブ

---

## 6. テストケース実装構造

### Arrange（準備）
1. ConnectionConfigの準備
   - IpAddress = "192.168.1.10"
   - Port = 5000
   - UseTcp = true
   - ConnectionType = "TCP"
   - IsBinary = false
   - FrameVersion = FrameVersion.Frame4E
2. TimeoutConfigの準備
   - ConnectTimeoutMs = 5000
   - SendTimeoutMs = 3000
   - ReceiveTimeoutMs = 5000
   - RetryTimeoutMs = 1000
3. **MockSocketで500ms遅延設定**
   - socket.ConnectAsync()実行時に500ms待機
4. **処理開始時刻記録**
   - testStartTime = DateTime.UtcNow
5. **期待値設定**
   - expectedDelay = 500ms
   - tolerance = 100ms
6. PlcCommunicationManagerインスタンス作成
   - モックSocketを注入

### Act（実行）
1. ConnectAsync実行
   - connectionConfig, timeoutConfigを渡す
2. **実際の経過時間計測**
   - actualElapsedTime = DateTime.UtcNow - testStartTime

### Assert（検証）
1. **ConnectionResponse null チェック**
   - Assert.NotNull(result)
2. **Status検証**
   - Assert.Equal(ConnectionStatus.Connected, result.Status)
3. **Socket検証**
   - Assert.NotNull(result.Socket)
4. **ConnectionTime範囲検証**
   - Assert.True(result.ConnectionTime.Value >= expectedDelay - tolerance)
   - Assert.True(result.ConnectionTime.Value <= expectedDelay + tolerance)
   - 実際: 400ms～600ms範囲内
5. **ConnectedAtとConnectionTimeの整合性検証**
   - timeFromConnectedAt = result.ConnectedAt.Value - testStartTime
   - Assert.True(Math.Abs((timeFromConnectedAt - result.ConnectionTime.Value).TotalMilliseconds) <= tolerance.TotalMilliseconds)
6. **RemoteEndPoint検証**
   - Assert.NotNull(result.RemoteEndPoint)
7. **ErrorMessage検証**
   - Assert.Null(result.ErrorMessage)

---

## 7. DIコンテナ設定

### サービスライフタイム
- **PlcCommunicationManager**: Transient（PLC別インスタンス）
- **ConfigToFrameManager**: Transient（設定別インスタンス）
- **LoggingManager**: Singleton（共有リソース）
- **ErrorHandler**: Singleton（共有リソース）

### インターフェース登録
**ファイル**: Program.cs または ServiceCollectionExtensions.cs

```csharp
services.AddTransient<IPlcCommunicationManager, PlcCommunicationManager>();
services.AddTransient<IConfigToFrameManager, ConfigToFrameManager>();
services.AddSingleton<ILoggingManager, LoggingManager>();
services.AddSingleton<IErrorHandler, ErrorHandler>();
```

### テスト用DIコンテナ
**ファイル**: Tests/TestUtilities/TestServiceProvider.cs

```csharp
public static class TestServiceProvider
{
    public static ServiceProvider BuildTestServices()
    {
        var services = new ServiceCollection();

        // 実装クラス登録
        services.AddTransient<IPlcCommunicationManager, PlcCommunicationManager>();
        services.AddTransient<IConfigToFrameManager, ConfigToFrameManager>();

        // モック・スタブ登録
        services.AddSingleton<ILoggingManager, MockLoggingManager>();
        services.AddSingleton<IErrorHandler, MockErrorHandler>();

        return services.BuildServiceProvider();
    }
}
```

---

## 8. ログ出力要件

### LoggingManager連携
- **Informationログ**: 接続開始・完了時
  - ログレベル: Information
  - メッセージ: "ConnectAsync: 接続開始 - {IpAddress}:{Port}"
  - メッセージ: "ConnectAsync: 接続成功 - {IpAddress}:{Port} ({ConnectionTimeMs}ms)"
- **接続時間記録**: パフォーマンス分析用

### ログレベル
- **Information**: 接続開始・完了時（TC020_5）

### ログ実装例

```csharp
_logger.LogInformation("ConnectAsync: 接続開始 - {IpAddress}:{Port}",
    connectionConfig.IpAddress, connectionConfig.Port);

var startTime = DateTime.UtcNow;

// 接続処理...
await socket.ConnectAsync(endPoint, cancellationToken);

var connectionTime = DateTime.UtcNow - startTime;

_logger.LogInformation("ConnectAsync: 接続成功 - {IpAddress}:{Port} ({ConnectionTimeMs}ms)",
    connectionConfig.IpAddress, connectionConfig.Port, connectionTime.TotalMilliseconds);
```

---

## 9. テスト実装チェックリスト

### TC020_5実装前
- [ ] PlcCommunicationManagerクラス作成（空実装）
- [ ] IPlcCommunicationManagerインターフェース作成
- [ ] ConnectAsyncメソッドシグネチャ定義
- [ ] ConnectionConfig・TimeoutConfigモデル作成
- [ ] ConnectionResponseモデル作成
- [ ] ConnectionStatus列挙型作成
- [ ] MockSocket作成（遅延制御機能付き）
- [ ] DelayedSocketStub作成

### TC020_5実装中
- [ ] Arrange: ConnectionConfig準備
- [ ] Arrange: TimeoutConfig準備
- [ ] Arrange: MockSocketで500ms遅延設定
- [ ] Arrange: 処理開始時刻記録
- [ ] Arrange: 期待値・許容誤差設定
- [ ] Act: ConnectAsync実行
- [ ] Act: 実際の経過時間計測
- [ ] Assert: ConnectionResponse null チェック
- [ ] Assert: Status検証
- [ ] Assert: Socket検証
- [ ] Assert: ConnectionTime範囲検証
- [ ] Assert: ConnectedAtとConnectionTimeの整合性検証
- [ ] Assert: RemoteEndPoint検証
- [ ] Assert: ErrorMessage検証

### TC020_5実装後
- [ ] テスト実行・Red確認
- [ ] ConnectAsync本体実装（時間計測ロジック追加）
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施
  - [ ] 時間計測精度向上（Stopwatch検討）
  - [ ] ログ出力追加
  - [ ] コード重複削除

---

## 10. 参考情報

### DateTime.UtcNowとDateTime.Nowの違い
- **DateTime.UtcNow**: UTC時刻、タイムゾーン非依存、推奨
- **DateTime.Now**: ローカル時刻、タイムゾーン依存、非推奨（計測用途）

### Stopwatchクラスの優位性
- **高精度**: 約0.1μs～1μs精度（システム依存）
- **パフォーマンス**: オーバーヘッド極小
- **用途**: マイクロ秒単位の精密計測

### 時間計測の誤差要因
- **GC（ガベージコレクション）**: 計測中にGC発生で誤差
- **スレッドスケジューリング**: OS側のスレッド切り替えで誤差
- **システム負荷**: CPU使用率が高い場合に誤差増大
- **I/O待機**: 非同期I/O待機時間の計測精度

### テストデータバリエーション（将来拡張用）
**配置先**: Tests/TestUtilities/TestData/ConnectionDelays/

- **遅延なし**: 0ms（最速接続）
- **短時間遅延**: 100ms, 200ms
- **中時間遅延**: 500ms（TC020_5）, 1000ms
- **長時間遅延**: 3000ms, 5000ms（タイムアウト近く）

---

## 11. C#実装例（完全版）

### ConnectAsync（時間計測実装）

```csharp
public async Task<ConnectionResponse> ConnectAsync(
    ConnectionConfig connectionConfig,
    TimeoutConfig timeoutConfig)
{
    // null チェック（TC020_3）
    if (connectionConfig == null || timeoutConfig == null)
    {
        throw new ArgumentNullException(
            connectionConfig == null ? nameof(connectionConfig) : nameof(timeoutConfig),
            ErrorMessages.ConfigNull);
    }

    // IPアドレス検証（TC020_1）
    IPAddress ipAddress;
    try
    {
        ipAddress = IPAddress.Parse(connectionConfig.IpAddress);
    }
    catch (FormatException ex)
    {
        throw new ArgumentException(
            string.Format(ErrorMessages.InvalidIpAddress, connectionConfig.IpAddress),
            nameof(connectionConfig.IpAddress),
            ex);
    }

    // ポート番号検証（TC020_2）
    if (connectionConfig.Port < 1 || connectionConfig.Port > 65535)
    {
        throw new ArgumentOutOfRangeException(
            nameof(connectionConfig.Port),
            connectionConfig.Port,
            string.Format(ErrorMessages.InvalidPort, connectionConfig.Port));
    }

    // 接続済みチェック（TC020_4）
    if (_socket != null && _socket.Connected)
    {
        _logger.LogWarning("ConnectAsync: 既に接続済み");
        throw new InvalidOperationException(ErrorMessages.AlreadyConnected);
    }

    // 実際の接続処理（TC020_5: 時間計測）
    _logger.LogInformation("ConnectAsync: 接続開始 - {IpAddress}:{Port}",
        connectionConfig.IpAddress, connectionConfig.Port);

    var startTime = DateTime.UtcNow;  // 開始時刻記録

    try
    {
        _socket = CreateSocket(connectionConfig);

        // タイムアウト設定
        using var cts = new CancellationTokenSource(timeoutConfig.ConnectTimeoutMs);

        // 接続実行（遅延発生箇所）
        await _socket.ConnectAsync(new IPEndPoint(ipAddress, connectionConfig.Port), cts.Token);

        _status = ConnectionStatus.Connected;

        var connectionTime = DateTime.UtcNow - startTime;  // 接続時間計測
        var connectedAt = DateTime.UtcNow;  // 接続完了時刻

        _logger.LogInformation("ConnectAsync: 接続成功 - {IpAddress}:{Port} ({ConnectionTimeMs}ms)",
            connectionConfig.IpAddress, connectionConfig.Port, connectionTime.TotalMilliseconds);

        return new ConnectionResponse
        {
            Status = _status,
            Socket = _socket,
            RemoteEndPoint = _socket.RemoteEndPoint,
            ConnectedAt = connectedAt,
            ConnectionTime = connectionTime,  // 計測結果を記録
            ErrorMessage = null
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "ConnectAsync: 接続失敗 - {IpAddress}:{Port}",
            connectionConfig.IpAddress, connectionConfig.Port);
        _status = ConnectionStatus.Error;
        throw;
    }
}

private Socket CreateSocket(ConnectionConfig config)
{
    var socket = new Socket(AddressFamily.InterNetwork,
        config.UseTcp ? SocketType.Stream : SocketType.Dgram,
        config.UseTcp ? ProtocolType.Tcp : ProtocolType.Udp);
    return socket;
}
```

### TC020_5テスト実装例

```csharp
[Fact]
public async Task TC020_5_ConnectAsync_接続時間計測精度()
{
    // Arrange
    var testStartTime = DateTime.UtcNow;  // テスト開始時刻
    var expectedDelay = TimeSpan.FromMilliseconds(500);  // 期待遅延時間
    var tolerance = TimeSpan.FromMilliseconds(100);  // 許容誤差

    var connectionConfig = new ConnectionConfig
    {
        IpAddress = "192.168.1.10",
        Port = 5000,
        UseTcp = true,
        ConnectionType = "TCP",
        IsBinary = false,
        FrameVersion = FrameVersion.Frame4E
    };

    var timeoutConfig = new TimeoutConfig
    {
        ConnectTimeoutMs = 5000,
        SendTimeoutMs = 3000,
        ReceiveTimeoutMs = 5000,
        RetryTimeoutMs = 1000
    };

    // MockSocketで500ms遅延設定
    var mockSocket = new MockSocket();
    mockSocket.SetConnectDelay(expectedDelay);

    var plcCommManager = new PlcCommunicationManager(_logger, mockSocket);

    // Act
    var result = await plcCommManager.ConnectAsync(connectionConfig, timeoutConfig);
    var actualElapsedTime = DateTime.UtcNow - testStartTime;

    // Assert
    Assert.NotNull(result);
    Assert.Equal(ConnectionStatus.Connected, result.Status);
    Assert.NotNull(result.Socket);
    Assert.NotNull(result.RemoteEndPoint);
    Assert.Null(result.ErrorMessage);

    // ConnectionTime範囲検証（期待: 400ms～600ms）
    Assert.True(result.ConnectionTime.Value >= expectedDelay - tolerance,
        $"ConnectionTimeが期待範囲より短い: {result.ConnectionTime.Value.TotalMilliseconds}ms < {(expectedDelay - tolerance).TotalMilliseconds}ms");
    Assert.True(result.ConnectionTime.Value <= expectedDelay + tolerance,
        $"ConnectionTimeが期待範囲より長い: {result.ConnectionTime.Value.TotalMilliseconds}ms > {(expectedDelay + tolerance).TotalMilliseconds}ms");

    // ConnectedAtとConnectionTimeの整合性検証
    var timeFromConnectedAt = result.ConnectedAt.Value - testStartTime;
    var timeDifference = Math.Abs((timeFromConnectedAt - result.ConnectionTime.Value).TotalMilliseconds);
    Assert.True(timeDifference <= tolerance.TotalMilliseconds,
        $"ConnectedAtとConnectionTimeの差が許容誤差を超える: {timeDifference}ms > {tolerance.TotalMilliseconds}ms");
}
```

### MockSocket実装例（遅延制御付き）

```csharp
public class MockSocket : Socket
{
    private TimeSpan _connectDelay = TimeSpan.Zero;

    public MockSocket()
        : base(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
    {
    }

    public void SetConnectDelay(TimeSpan delay)
    {
        _connectDelay = delay;
    }

    public override async Task ConnectAsync(EndPoint remoteEP, CancellationToken cancellationToken)
    {
        // 意図的な遅延発生
        await Task.Delay(_connectDelay, cancellationToken);

        // 接続成功をシミュレート
        // 実際のSocket.ConnectAsyncは呼び出さない（単体テストのため）
    }

    public override bool Connected => true;  // 常に接続済み状態を返す

    public override EndPoint? RemoteEndPoint => new IPEndPoint(IPAddress.Parse("192.168.1.10"), 5000);
}
```

### Stopwatch使用パターン実装例（参考）

```csharp
public async Task<ConnectionResponse> ConnectAsync(
    ConnectionConfig connectionConfig,
    TimeoutConfig timeoutConfig)
{
    // 前処理（null検証、IP検証、ポート検証、接続済みチェック）...

    _logger.LogInformation("ConnectAsync: 接続開始 - {IpAddress}:{Port}",
        connectionConfig.IpAddress, connectionConfig.Port);

    var stopwatch = Stopwatch.StartNew();  // 高精度計測開始
    var startTime = DateTime.UtcNow;

    try
    {
        _socket = CreateSocket(connectionConfig);

        using var cts = new CancellationTokenSource(timeoutConfig.ConnectTimeoutMs);
        await _socket.ConnectAsync(new IPEndPoint(ipAddress, connectionConfig.Port), cts.Token);

        stopwatch.Stop();  // 計測停止
        _status = ConnectionStatus.Connected;

        var connectionTime = stopwatch.Elapsed;  // Stopwatchによる高精度計測
        var connectedAt = DateTime.UtcNow;

        _logger.LogInformation("ConnectAsync: 接続成功 - {IpAddress}:{Port} ({ConnectionTimeMs}ms)",
            connectionConfig.IpAddress, connectionConfig.Port, connectionTime.TotalMilliseconds);

        return new ConnectionResponse
        {
            Status = _status,
            Socket = _socket,
            RemoteEndPoint = _socket.RemoteEndPoint,
            ConnectedAt = connectedAt,
            ConnectionTime = connectionTime,
            ErrorMessage = null
        };
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        _logger.LogError(ex, "ConnectAsync: 接続失敗 - {IpAddress}:{Port} ({ElapsedMs}ms)",
            connectionConfig.IpAddress, connectionConfig.Port, stopwatch.ElapsedMilliseconds);
        _status = ConnectionStatus.Error;
        throw;
    }
}
```

---

以上が TC020_5_ConnectAsync_接続時間計測精度テスト実装に必要な情報です。
