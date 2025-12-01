# Step3 ConnectAsync 既に接続済み状態での再接続テスト実装用情報（TC020_4）

## ドキュメント概要

### 目的
このドキュメントは、TC020_4_ConnectAsync_既に接続済み状態での再接続テストの実装に必要な情報を集約したものです。
**コード作成時に必要となる技術情報のみ**を記載しており、学習資料や説明的な内容は含みません。

### 情報取得元
本ドキュメントの情報は以下のソースから抽出・統合されています：

#### 設計書（andon/documents/design/）
- `クラス・メソッドリスト.md` - クラス・メソッドの一覧と概要
- `クラス設計.md` - 詳細なクラス設計仕様
- `テスト内容.md` - テストケース仕様
- `プロジェクト構造設計.md` - フォルダ構造・プロジェクト構成
- `依存関係.md` - クラス間の依存関係
- `エラーハンドリング.md` - エラー処理仕様

#### 実装参考（PySLMPClient）
- `PySLMPClient/pyslmpclient/__init__.py` - 接続状態管理実装
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

#### 前提条件（TC020_4固有）
- **ConnectAsync成功済み（Status=Connected、Socket接続中）**
- **内部状態: _socket != null && _socket.Connected == true**

#### Output
- **失敗時（TC020_4で期待）**: InvalidOperationException スロー
  - 例外メッセージ: "既に接続済みです。再接続する場合は先にDisconnectAsync()を実行してください"

#### 機能
- 接続状態検証（ConnectAsync実行前の事前チェック）
- 既存接続の保護（自動切断しない安全設計）
- 明示的な切断要求による安全性確保

---

## 2. テストケース仕様（TC020_4）

### TC020_4_ConnectAsync_既に接続済み状態での再接続
**目的**: 接続済み状態での再接続試行が正しく検出され、例外がスローされることをテスト

#### 前提条件
- **ConnectAsync成功済み（Status=Connected、Socket接続中）**
- **内部状態: _socket != null && _socket.Connected == true**

#### テストシナリオ
1. **第1回目のConnectAsync実行** → 成功（Status=Connected）
2. **第2回目のConnectAsync実行** → InvalidOperationExceptionスロー

#### 入力データ
**ConnectionConfig（1回目・2回目共通）**:
- IpAddress: "192.168.1.10"
- Port: 5000
- UseTcp: true
- ConnectionType: "TCP"
- IsBinary: false
- FrameVersion: FrameVersion.Frame4E

**TimeoutConfig（1回目・2回目共通）**:
- ConnectTimeoutMs: 5000
- SendTimeoutMs: 3000
- ReceiveTimeoutMs: 5000
- RetryTimeoutMs: 1000

#### 期待動作（異常系）
- **InvalidOperationException がスローされる**（2回目のConnectAsync実行時）
- **例外メッセージ**: "既に接続済みです。再接続する場合は先にDisconnectAsync()を実行してください"
- **Status: Connected のまま**（既存接続は維持）
- **既存Socketは切断されない**（安全設計）

#### 検証項目
1. **1回目のConnectAsync成功確認**
   - ConnectionResponse.Status == Connected
   - ConnectionResponse.Socket != null
2. **2回目のConnectAsync例外発生確認**
   - InvalidOperationExceptionスロー
3. **例外メッセージ検証**
   - "既に接続済みです。再接続する場合は先にDisconnectAsync()を実行してください"
4. **Status確認**
   - Status == Connected（既存接続維持）

#### 設計判断の重要性
- **自動切断しない理由**: 意図しない接続切断を防ぐ
- **明示的切断要求**: DisconnectAsync()呼び出しを強制
- **安全設計**: データ送信中の接続切断を防止

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
    public ConnectionStatus Status { get; set; }  // TC020_4: 1回目=Connected、2回目=例外
    public Socket? Socket { get; set; }           // TC020_4: 1回目=Socketインスタンス、2回目=例外
    public EndPoint? RemoteEndPoint { get; set; } // TC020_4: 1回目=接続先情報、2回目=例外
    public DateTime? ConnectedAt { get; set; }    // TC020_4: 1回目=接続完了時刻、2回目=例外
    public TimeSpan? ConnectionTime { get; set; } // TC020_4: 1回目=接続処理時間、2回目=例外
    public string? ErrorMessage { get; set; }     // TC020_4: 1回目=null、2回目=例外
}
```

### ConnectionStatus（接続状態列挙型）

```csharp
public enum ConnectionStatus
{
    Disconnected,  // 初期状態
    Connected,     // TC020_4: 1回目のConnectAsync成功後
    Error
}
```

### PlcCommunicationManager内部状態

```csharp
public class PlcCommunicationManager
{
    private Socket? _socket;  // TC020_4: 1回目のConnectAsync成功後に設定
    private ConnectionStatus _status;  // TC020_4: 1回目成功後=Connected

    // ...
}
```

---

## 4. エラーハンドリング

### ConnectAsync スロー例外（TC020_4で検証）
**InvalidOperationException**:
- **発生条件**: 既に接続済み状態での再接続試行（_socket != null && _socket.Connected）
- **メッセージ形式**: "既に接続済みです。再接続する場合は先にDisconnectAsync()を実行してください"
- **ErrorMessages定数**: ErrorMessages.AlreadyConnected

### エラーメッセージ統一
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    public const string InvalidIpAddress = "不正なIPアドレス形式: {0}";
    public const string InvalidPort = "ポート番号が範囲外です: {0}（有効範囲: 1-65535）";
    public const string ConfigNull = "ConnectionConfigまたはTimeoutConfigがnullです";
    public const string AlreadyConnected = "既に接続済みです。再接続する場合は先にDisconnectAsync()を実行してください";
    public const string NotConnected = "PLC未接続状態です。先にConnectAsync()を実行してください。";
}
```

### エラーハンドリング実装例

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

    // 実際の接続処理...
    _logger.LogInformation("ConnectAsync: 接続開始 - {IpAddress}:{Port}",
        connectionConfig.IpAddress, connectionConfig.Port);

    // Socket作成・接続...
    _socket = CreateSocket(connectionConfig);
    await _socket.ConnectAsync(new IPEndPoint(ipAddress, connectionConfig.Port));
    _status = ConnectionStatus.Connected;

    return new ConnectionResponse
    {
        Status = _status,
        Socket = _socket,
        RemoteEndPoint = _socket.RemoteEndPoint,
        ConnectedAt = DateTime.UtcNow,
        ConnectionTime = TimeSpan.FromMilliseconds(100),
        ErrorMessage = null
    };
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

### テスト実装順序（異常系テスト群）
1. **TC020_3_ConnectAsync_null入力**（最優先、最も基本的な異常系）
2. **TC020_1_ConnectAsync_不正IPアドレス**
3. **TC020_2_ConnectAsync_不正ポート番号**
4. **TC020_4_ConnectAsync_既に接続済み状態での再接続**（本テスト）
5. **TC020_5_ConnectAsync_接続時間計測精度**（正常系に近い）

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockSocket**: Socket接続状態をシミュレート
  - Connected = true（接続済み状態）
  - ConnectAsync成功シミュレート
- **MockPlcCommunicationManager**: 内部状態制御用（オプション）

#### 使用するスタブ
- **ConfigurationStubs**: ConnectionConfig・TimeoutConfigのスタブ

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
3. PlcCommunicationManagerインスタンス作成
   - モックSocketを注入（オプション）
4. **1回目のConnectAsync実行（成功）**
   - Status = Connected
   - Socket != null

### Act & Assert（実行・検証）
1. **1回目の結果検証**
   - Assert.NotNull(firstResponse)
   - Assert.Equal(ConnectionStatus.Connected, firstResponse.Status)
   - Assert.NotNull(firstResponse.Socket)
2. **2回目のConnectAsync実行**
   - 同じConnectionConfig・TimeoutConfigで再実行
3. **例外発生確認**
   - Assert.ThrowsAsync<InvalidOperationException>()
4. **例外メッセージ検証**
   - Assert.Contains("既に接続済みです", exception.Message)
   - Assert.Contains("DisconnectAsync", exception.Message)
5. **Status確認**（オプション）
   - Status == Connected（既存接続維持）

---

## 7. DIコンテナ設定

### サービスライフタイム
- **PlcCommunicationManager**: Transient（PLC別インスタンス）
  - **重要**: Transientスコープにより、各インスタンスが独立した内部状態を持つ
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
- **警告ログ**: 接続済み状態での再接続試行時
  - ログレベル: Warning
  - メッセージ: "ConnectAsync: 既に接続済み"
  - 接続情報: 現在の接続先（IPアドレス、ポート）

### ログレベル
- **Warning**: 接続済み状態での再接続試行時（TC020_4）

### ログ実装例

```csharp
if (_socket != null && _socket.Connected)
{
    _logger.LogWarning("ConnectAsync: 既に接続済み - {RemoteEndPoint}",
        _socket.RemoteEndPoint?.ToString() ?? "Unknown");
    throw new InvalidOperationException(ErrorMessages.AlreadyConnected);
}
```

---

## 9. テスト実装チェックリスト

### TC020_4実装前
- [ ] PlcCommunicationManagerクラス作成（空実装）
- [ ] IPlcCommunicationManagerインターフェース作成
- [ ] ConnectAsyncメソッドシグネチャ定義
- [ ] DisconnectAsyncメソッドシグネチャ定義
- [ ] ConnectionConfig・TimeoutConfigモデル作成
- [ ] ConnectionResponseモデル作成
- [ ] ConnectionStatus列挙型作成
- [ ] ErrorMessagesクラス作成
- [ ] MockSocket作成（接続状態シミュレート）

### TC020_4実装中
- [ ] Arrange: ConnectionConfig準備
- [ ] Arrange: TimeoutConfig準備
- [ ] Arrange: PlcCommunicationManagerインスタンス作成
- [ ] Arrange: 1回目のConnectAsync実行（成功）
- [ ] Assert: 1回目の結果検証（Status=Connected、Socket!=null）
- [ ] Act: 2回目のConnectAsync実行
- [ ] Assert: InvalidOperationException発生確認
- [ ] Assert: 例外メッセージ検証

### TC020_4実装後
- [ ] テスト実行・Red確認
- [ ] ConnectAsync本体実装（接続済みチェック追加）
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施
  - [ ] エラーメッセージ統一化
  - [ ] ログ出力追加
  - [ ] 内部状態管理の整理

---

## 10. 参考情報

### Socket.Connected プロパティ
- **型**: bool
- **用途**: ソケットの接続状態確認
- **true**: 接続中
- **false**: 未接続または切断済み
- **注意**: 最後の操作時点の状態を示す（リアルタイム検証ではない）

### DisconnectAsync 設計
**目的**: 明示的な接続切断

```csharp
public async Task DisconnectAsync()
{
    if (_socket != null && _socket.Connected)
    {
        await _socket.DisconnectAsync(false);  // 再利用しない
        _socket.Dispose();
        _socket = null;
        _status = ConnectionStatus.Disconnected;
        _logger.LogInformation("DisconnectAsync: 接続を切断しました");
    }
}
```

### 安全な再接続パターン
```csharp
// 既存接続がある場合は先に切断
if (plcCommManager.IsConnected)
{
    await plcCommManager.DisconnectAsync();
}

// 再接続
await plcCommManager.ConnectAsync(connectionConfig, timeoutConfig);
```

### 接続状態管理のベストプラクティス
- **明示的な状態管理**: _socketとStatusの同期
- **スレッドセーフ**: 並行アクセス時の状態保護
- **例外安全**: 接続処理中の例外時の状態維持

---

## 11. C#実装例（完全版）

### PlcCommunicationManager内部状態管理

```csharp
public class PlcCommunicationManager : IPlcCommunicationManager
{
    private Socket? _socket;
    private ConnectionStatus _status = ConnectionStatus.Disconnected;
    private readonly ILogger<PlcCommunicationManager> _logger;

    public PlcCommunicationManager(ILogger<PlcCommunicationManager> logger)
    {
        _logger = logger;
    }

    public bool IsConnected => _socket != null && _socket.Connected;

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
            _logger.LogWarning("ConnectAsync: 既に接続済み - {RemoteEndPoint}",
                _socket.RemoteEndPoint?.ToString() ?? "Unknown");
            throw new InvalidOperationException(ErrorMessages.AlreadyConnected);
        }

        // 実際の接続処理
        _logger.LogInformation("ConnectAsync: 接続開始 - {IpAddress}:{Port}",
            connectionConfig.IpAddress, connectionConfig.Port);

        var startTime = DateTime.UtcNow;

        try
        {
            _socket = CreateSocket(connectionConfig);
            await _socket.ConnectAsync(new IPEndPoint(ipAddress, connectionConfig.Port));
            _status = ConnectionStatus.Connected;

            var connectionTime = DateTime.UtcNow - startTime;

            _logger.LogInformation("ConnectAsync: 接続成功 - {IpAddress}:{Port} ({ConnectionTimeMs}ms)",
                connectionConfig.IpAddress, connectionConfig.Port, connectionTime.TotalMilliseconds);

            return new ConnectionResponse
            {
                Status = _status,
                Socket = _socket,
                RemoteEndPoint = _socket.RemoteEndPoint,
                ConnectedAt = DateTime.UtcNow,
                ConnectionTime = connectionTime,
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

    public async Task DisconnectAsync()
    {
        if (_socket != null && _socket.Connected)
        {
            await _socket.DisconnectAsync(false);
            _socket.Dispose();
            _socket = null;
            _status = ConnectionStatus.Disconnected;
            _logger.LogInformation("DisconnectAsync: 接続を切断しました");
        }
    }

    private Socket CreateSocket(ConnectionConfig config)
    {
        var socket = new Socket(AddressFamily.InterNetwork,
            config.UseTcp ? SocketType.Stream : SocketType.Dgram,
            config.UseTcp ? ProtocolType.Tcp : ProtocolType.Udp);
        return socket;
    }
}
```

### TC020_4テスト実装例

```csharp
[Fact]
public async Task TC020_4_ConnectAsync_既に接続済み状態での再接続()
{
    // Arrange
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

    var plcCommManager = new PlcCommunicationManager(_logger);

    // 1回目のConnectAsync実行（成功を期待）
    var firstResponse = await plcCommManager.ConnectAsync(connectionConfig, timeoutConfig);

    // 1回目の結果検証
    Assert.NotNull(firstResponse);
    Assert.Equal(ConnectionStatus.Connected, firstResponse.Status);
    Assert.NotNull(firstResponse.Socket);

    // Act & Assert: 2回目のConnectAsync実行（例外を期待）
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(
        async () => await plcCommManager.ConnectAsync(connectionConfig, timeoutConfig)
    );

    // 例外メッセージ検証
    Assert.Contains("既に接続済みです", exception.Message);
    Assert.Contains("DisconnectAsync", exception.Message);
}
```

### 正常な再接続パターンのテスト実装例（参考）

```csharp
[Fact]
public async Task ConnectAsync_DisconnectAsync後の再接続_成功()
{
    // Arrange
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

    var plcCommManager = new PlcCommunicationManager(_logger);

    // 1回目のConnectAsync実行
    var firstResponse = await plcCommManager.ConnectAsync(connectionConfig, timeoutConfig);
    Assert.Equal(ConnectionStatus.Connected, firstResponse.Status);

    // DisconnectAsync実行
    await plcCommManager.DisconnectAsync();

    // Act: 2回目のConnectAsync実行（正常に接続できるはず）
    var secondResponse = await plcCommManager.ConnectAsync(connectionConfig, timeoutConfig);

    // Assert
    Assert.NotNull(secondResponse);
    Assert.Equal(ConnectionStatus.Connected, secondResponse.Status);
    Assert.NotNull(secondResponse.Socket);
}
```

---

以上が TC020_4_ConnectAsync_既に接続済み状態での再接続テスト実装に必要な情報です。
