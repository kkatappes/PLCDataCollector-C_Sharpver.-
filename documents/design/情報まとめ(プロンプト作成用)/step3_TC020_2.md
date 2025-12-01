# Step3 ConnectAsync 不正ポート番号テスト実装用情報（TC020_2）

## ドキュメント概要

### 目的
このドキュメントは、TC020_2_ConnectAsync_不正ポート番号テストの実装に必要な情報を集約したものです。
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
- `PySLMPClient/pyslmpclient/__init__.py` - 入力検証実装
- `PySLMPClient/tests/test_main.py` - テストケース実例

---

## 1. テスト対象メソッド仕様

### ConnectAsync（Step3: PLC接続処理）
**クラス**: PlcCommunicationManager
**名前空間**: andon.Core.Managers

#### Input
- ConnectionConfig（IpAddress, Port）
  - IpAddress: "192.168.1.10"（正常なIPアドレス）
  - **Port: 70000（範囲外のポート番号、有効範囲: 1-65535）**
  - UseTcp: true
  - ConnectionType: "TCP"
  - IsBinary: false
  - FrameVersion: FrameVersion.Frame4E
- TimeoutConfig
  - ConnectTimeoutMs: 5000
  - SendTimeoutMs: 3000
  - ReceiveTimeoutMs: 5000
  - RetryTimeoutMs: 1000

#### Output
- **失敗時（TC020_2で期待）**: ArgumentOutOfRangeException スロー
  - 例外メッセージ: "ポート番号が範囲外です: 70000（有効範囲: 1-65535）"
  - パラメータ名: "connectionConfig.Port"

#### 機能
- ポート番号範囲検証（ConnectAsync実行前の事前チェック）
- 有効範囲（1-65535）チェック
- 範囲外検出時の即座の例外スロー

---

## 2. テストケース仕様（TC020_2）

### TC020_2_ConnectAsync_不正ポート番号
**目的**: 不正なポート番号が正しく検出され、例外がスローされることをテスト

#### 前提条件
- PlcCommunicationManagerインスタンスが準備済み
- 未接続状態（Status != Connected）

#### 入力データ
**ConnectionConfig**:
- IpAddress: "192.168.1.10"（正常な値）
- **Port: 70000（範囲外の値、有効範囲: 1-65535）**
- UseTcp: true
- ConnectionType: "TCP"
- IsBinary: false
- FrameVersion: FrameVersion.Frame4E

**TimeoutConfig**:
- ConnectTimeoutMs: 5000
- SendTimeoutMs: 3000
- ReceiveTimeoutMs: 5000
- RetryTimeoutMs: 1000

#### 期待動作（異常系）
- **ArgumentOutOfRangeException がスローされる**
- **例外メッセージ**: "ポート番号が範囲外です: 70000（有効範囲: 1-65535）"
- **パラメータ名**: "connectionConfig.Port"
- **接続処理は実行されない**（ポート番号検証でフェイルファスト）
- **Status: Disconnected のまま**

#### 検証項目
1. **ArgumentOutOfRangeException発生確認**
2. **例外メッセージ検証**: "ポート番号が範囲外です: 70000（有効範囲: 1-65535）"
3. **パラメータ名確認**: "Port" または "connectionConfig.Port"
4. **Status確認**: Disconnected（接続処理未実行）

#### 重要性
- **セキュリティ**: 不正ポート番号からのシステム保護
- **早期検出**: 接続試行前のフェイルファスト
- **ユーザー体験**: 明確なエラーメッセージ提供（有効範囲を明示）

#### テストバリエーション（将来拡張用）
- **範囲外上限**: 70000, 65536, 99999
- **範囲外下限**: 0, -1, -100
- **境界値**: 1（最小有効値）, 65535（最大有効値）

---

## 3. 依存クラス・設定

### ConnectionConfig（接続設定）
**取得元**: ConfigToFrameManager.LoadConfigAsync()

```csharp
public class ConnectionConfig
{
    public string IpAddress { get; set; }        // 例: "192.168.3.250"（TC020_2では"192.168.1.10"）
    public int Port { get; set; }                 // 例: 5007（TC020_2では70000）
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
    public ConnectionStatus Status { get; set; }  // TC020_2では Disconnected のまま
    public Socket? Socket { get; set; }           // TC020_2では null
    public EndPoint? RemoteEndPoint { get; set; } // TC020_2では null
    public DateTime? ConnectedAt { get; set; }    // TC020_2では null
    public TimeSpan? ConnectionTime { get; set; } // TC020_2では null
    public string? ErrorMessage { get; set; }     // TC020_2では "ポート番号が範囲外です: 70000（有効範囲: 1-65535）"
}
```

### ConnectionStatus（接続状態列挙型）

```csharp
public enum ConnectionStatus
{
    Disconnected,  // TC020_2での期待状態
    Connected,
    Error
}
```

---

## 4. エラーハンドリング

### ConnectAsync スロー例外（TC020_2で検証）
**ArgumentOutOfRangeException**:
- **発生条件**: ポート番号が1-65535の範囲外
- **メッセージ形式**: "ポート番号が範囲外です: {Port}（有効範囲: 1-65535）"
- **ErrorMessages定数**: ErrorMessages.InvalidPort
- **パラメータ名**: "connectionConfig.Port" または "Port"

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
        _logger.LogError(ex, "ConnectAsync: 不正なIPアドレス形式: {IpAddress}",
            connectionConfig.IpAddress);
        throw new ArgumentException(
            string.Format(ErrorMessages.InvalidIpAddress, connectionConfig.IpAddress),
            nameof(connectionConfig.IpAddress),
            ex);
    }

    // ポート番号検証（TC020_2）
    if (connectionConfig.Port < 1 || connectionConfig.Port > 65535)
    {
        _logger.LogError("ConnectAsync: 不正なポート番号: {Port}", connectionConfig.Port);
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
3. **TC020_2_ConnectAsync_不正ポート番号**（本テスト）
4. **TC020_4_ConnectAsync_既に接続済み状態での再接続**
5. **TC020_5_ConnectAsync_接続時間計測精度**（正常系に近い）

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック（TC020_2では不要）
- TC020_2では接続処理に到達しないため、モックSocket不要
- 入力検証のみでテスト完結

#### 使用するスタブ
- **ConfigurationStubs**: ConnectionConfig・TimeoutConfigのスタブ
  - InvalidPortConfig: 不正ポート番号設定スタブ

---

## 6. テストケース実装構造

### Arrange（準備）
1. ConnectionConfigの準備
   - IpAddress = "192.168.1.10"（正常な値）
   - Port = 70000（不正な値、範囲外）
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
   - DIコンテナまたは直接インスタンス化

### Act & Assert（実行・検証）
1. **例外発生確認**
   - Assert.ThrowsAsync<ArgumentOutOfRangeException>()
   - ConnectAsync実行
2. **例外メッセージ検証**
   - Assert.Contains("ポート番号が範囲外です", exception.Message)
   - Assert.Contains("70000", exception.Message)
   - Assert.Contains("有効範囲: 1-65535", exception.Message)
3. **パラメータ名検証**
   - Assert.Equal("Port", exception.ParamName) または Assert.Contains("Port", exception.ParamName)
4. **Status確認**（オプション、ConnectionResponseが返らないため不要の場合あり）
   - 例外発生のため、Statusは変化しない（Disconnected維持）

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
- **エラーログ**: 不正ポート番号検出時
  - ログレベル: Error
  - メッセージ: "ConnectAsync: 不正なポート番号: {Port}"
  - ポート番号: 例外発生値（70000）

### ログレベル
- **Error**: 不正ポート番号検出時（TC020_2）

### ログ実装例

```csharp
if (connectionConfig.Port < 1 || connectionConfig.Port > 65535)
{
    _logger.LogError("ConnectAsync: 不正なポート番号: {Port}", connectionConfig.Port);
    throw new ArgumentOutOfRangeException(
        nameof(connectionConfig.Port),
        connectionConfig.Port,
        string.Format(ErrorMessages.InvalidPort, connectionConfig.Port));
}
```

---

## 9. テスト実装チェックリスト

### TC020_2実装前
- [ ] PlcCommunicationManagerクラス作成（空実装）
- [ ] IPlcCommunicationManagerインターフェース作成
- [ ] ConnectAsyncメソッドシグネチャ定義
- [ ] ConnectionConfig・TimeoutConfigモデル作成
- [ ] ConnectionResponseモデル作成
- [ ] ConnectionStatus列挙型作成
- [ ] ErrorMessagesクラス作成

### TC020_2実装中
- [ ] Arrange: ConnectionConfig準備（Port = 70000）
- [ ] Arrange: TimeoutConfig準備
- [ ] Arrange: PlcCommunicationManagerインスタンス作成
- [ ] Act: ConnectAsync呼び出し
- [ ] Assert: ArgumentOutOfRangeException発生確認
- [ ] Assert: 例外メッセージ検証
- [ ] Assert: パラメータ名確認

### TC020_2実装後
- [ ] テスト実行・Red確認
- [ ] ConnectAsync本体実装（ポート番号検証ロジック追加）
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施
  - [ ] エラーメッセージ統一化
  - [ ] ログ出力追加
  - [ ] コード重複削除

---

## 10. 参考情報

### ポート番号仕様
- **有効範囲**: 1-65535
- **Well-known ports**: 0-1023（予約済み、管理者権限必要）
- **Registered ports**: 1024-49151（登録済みポート）
- **Dynamic/Private ports**: 49152-65535（動的・プライベートポート）

### TCP/UDPポート範囲
- **最小値**: 1（ポート0は予約済み）
- **最大値**: 65535（16ビット符号なし整数の最大値）

### SLMP標準ポート番号
- **TCP**: 通常5007（Q00UDPCPUデフォルト）
- **UDP**: 通常5007（Q00UDPCPUデフォルト）

### テストデータバリエーション（将来拡張用）
**配置先**: Tests/TestUtilities/TestData/InvalidPorts/

- **範囲外上限**: 70000, 65536, 99999, 100000
- **範囲外下限**: 0, -1, -100, -1000
- **境界値正常**: 1（最小有効値）, 65535（最大有効値）
- **境界値異常**: 0（最小値-1）, 65536（最大値+1）

---

## 11. C#実装例（完全版）

### ConnectAsync（ポート番号検証部分）

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
        _logger.LogError(ex, "ConnectAsync: 不正なIPアドレス形式: {IpAddress}",
            connectionConfig.IpAddress);
        throw new ArgumentException(
            string.Format(ErrorMessages.InvalidIpAddress, connectionConfig.IpAddress),
            nameof(connectionConfig.IpAddress),
            ex);
    }

    // ポート番号検証（TC020_2）
    if (connectionConfig.Port < 1 || connectionConfig.Port > 65535)
    {
        _logger.LogError("ConnectAsync: 不正なポート番号: {Port}", connectionConfig.Port);
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
}
```

### TC020_2テスト実装例

```csharp
[Fact]
public async Task TC020_2_ConnectAsync_不正ポート番号()
{
    // Arrange
    var connectionConfig = new ConnectionConfig
    {
        IpAddress = "192.168.1.10",  // 正常な値
        Port = 70000,                 // 不正なポート番号（範囲外）
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

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
        async () => await plcCommManager.ConnectAsync(connectionConfig, timeoutConfig)
    );

    // 例外メッセージ検証
    Assert.Contains("ポート番号が範囲外です", exception.Message);
    Assert.Contains("70000", exception.Message);
    Assert.Contains("有効範囲: 1-65535", exception.Message);

    // パラメータ名検証
    Assert.Contains("Port", exception.ParamName);
}
```

### 境界値テスト実装例（参考）

```csharp
[Theory]
[InlineData(0)]      // 最小値-1（範囲外下限）
[InlineData(-1)]     // 負数
[InlineData(65536)]  // 最大値+1（範囲外上限）
[InlineData(70000)]  // 範囲外
[InlineData(99999)]  // 範囲外
public async Task ConnectAsync_不正ポート番号_境界値テスト(int invalidPort)
{
    // Arrange
    var connectionConfig = new ConnectionConfig
    {
        IpAddress = "192.168.1.10",
        Port = invalidPort,
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

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
        async () => await plcCommManager.ConnectAsync(connectionConfig, timeoutConfig)
    );

    Assert.Contains("ポート番号が範囲外です", exception.Message);
    Assert.Contains(invalidPort.ToString(), exception.Message);
}

[Theory]
[InlineData(1)]      // 最小有効値
[InlineData(5007)]   // SLMP標準ポート
[InlineData(65535)]  // 最大有効値
public async Task ConnectAsync_正常ポート番号_境界値テスト(int validPort)
{
    // Arrange
    var connectionConfig = new ConnectionConfig
    {
        IpAddress = "192.168.1.10",
        Port = validPort,
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

    // Act
    // ポート番号検証はパスするはず（その後の接続処理は別途モック化）
    // 実際のテスト実装では、モックSocketで接続処理をシミュレート
}
```

---

以上が TC020_2_ConnectAsync_不正ポート番号テスト実装に必要な情報です。
