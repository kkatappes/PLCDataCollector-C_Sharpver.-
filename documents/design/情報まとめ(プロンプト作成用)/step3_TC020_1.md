# Step3 ConnectAsync 不正IPアドレステスト実装用情報（TC020_1）

## ドキュメント概要

### 目的
このドキュメントは、TC020_1_ConnectAsync_不正IPアドレステストの実装に必要な情報を集約したものです。
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
  - **IpAddress: "999.999.999.999"（不正なIPアドレス形式）**
  - Port: 5000
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
- **失敗時（TC020_1で期待）**: ArgumentException または FormatException スロー
  - 例外メッセージ: "不正なIPアドレス形式: 999.999.999.999"
  - InnerException: FormatException（IPAddress.Parseからの元例外）

#### 機能
- IPアドレス形式検証（ConnectAsync実行前の事前チェック）
- IPAddress.Parse()による構文検証
- 不正形式検出時の即座の例外スロー

---

## 2. テストケース仕様（TC020_1）

### TC020_1_ConnectAsync_不正IPアドレス
**目的**: 不正なIPアドレス形式が正しく検出され、例外がスローされることをテスト

#### 前提条件
- PlcCommunicationManagerインスタンスが準備済み
- 未接続状態（Status != Connected）

#### 入力データ
**ConnectionConfig**:
- **IpAddress: "999.999.999.999"（各オクテットが範囲外、有効範囲: 0-255）**
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

#### 期待動作（異常系）
- **ArgumentException または FormatException がスローされる**
- **例外メッセージ**: "不正なIPアドレス形式: 999.999.999.999"
- **InnerException**: FormatException
- **接続処理は実行されない**（IPAddress検証でフェイルファスト）
- **Status: Disconnected のまま**

#### 検証項目
1. **ArgumentException または FormatException発生確認**
2. **例外メッセージ検証**: "不正なIPアドレス形式: 999.999.999.999"
3. **InnerException型確認**: FormatException
4. **Status確認**: Disconnected（接続処理未実行）

#### 重要性
- **セキュリティ**: 不正入力からのシステム保護
- **早期検出**: 接続試行前のフェイルファスト
- **ユーザー体験**: 明確なエラーメッセージ提供

---

## 3. 依存クラス・設定

### ConnectionConfig（接続設定）
**取得元**: ConfigToFrameManager.LoadConfigAsync()

```csharp
public class ConnectionConfig
{
    public string IpAddress { get; set; }        // 例: "192.168.3.250"（TC020_1では"999.999.999.999"）
    public int Port { get; set; }                 // 例: 5007
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
    public ConnectionStatus Status { get; set; }  // TC020_1では Disconnected のまま
    public Socket? Socket { get; set; }           // TC020_1では null
    public EndPoint? RemoteEndPoint { get; set; } // TC020_1では null
    public DateTime? ConnectedAt { get; set; }    // TC020_1では null
    public TimeSpan? ConnectionTime { get; set; } // TC020_1では null
    public string? ErrorMessage { get; set; }     // TC020_1では "不正なIPアドレス形式: 999.999.999.999"
}
```

### ConnectionStatus（接続状態列挙型）

```csharp
public enum ConnectionStatus
{
    Disconnected,  // TC020_1での期待状態
    Connected,
    Error
}
```

---

## 4. エラーハンドリング

### ConnectAsync スロー例外（TC020_1で検証）
**ArgumentException / FormatException**:
- **発生条件**: 不正なIPアドレス形式（IPAddress.Parse失敗時）
- **メッセージ形式**: "不正なIPアドレス形式: {IpAddress}"
- **ErrorMessages定数**: ErrorMessages.InvalidIpAddress
- **InnerException**: FormatException（IPAddress.Parseからの元例外）

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
    // null チェック
    if (connectionConfig == null || timeoutConfig == null)
    {
        throw new ArgumentNullException(
            connectionConfig == null ? nameof(connectionConfig) : nameof(timeoutConfig),
            ErrorMessages.ConfigNull);
    }

    // IPアドレス検証（TC020_1で検証）
    try
    {
        IPAddress ipAddress = IPAddress.Parse(connectionConfig.IpAddress);
    }
    catch (FormatException ex)
    {
        throw new ArgumentException(
            string.Format(ErrorMessages.InvalidIpAddress, connectionConfig.IpAddress),
            nameof(connectionConfig.IpAddress),
            ex);
    }

    // ポート番号検証
    if (connectionConfig.Port < 1 || connectionConfig.Port > 65535)
    {
        throw new ArgumentOutOfRangeException(
            nameof(connectionConfig.Port),
            string.Format(ErrorMessages.InvalidPort, connectionConfig.Port));
    }

    // 接続済みチェック
    if (_socket != null && _socket.Connected)
    {
        throw new InvalidOperationException(ErrorMessages.AlreadyConnected);
    }

    // 実際の接続処理...
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
2. **TC020_1_ConnectAsync_不正IPアドレス**（本テスト）
3. **TC020_2_ConnectAsync_不正ポート番号**
4. **TC020_4_ConnectAsync_既に接続済み状態での再接続**
5. **TC020_5_ConnectAsync_接続時間計測精度**（正常系に近い）

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック（TC020_1では不要）
- TC020_1では接続処理に到達しないため、モックSocket不要
- 入力検証のみでテスト完結

#### 使用するスタブ
- **ConfigurationStubs**: ConnectionConfig・TimeoutConfigのスタブ
  - InvalidIpAddressConfig: 不正IPアドレス設定スタブ

---

## 6. テストケース実装構造

### Arrange（準備）
1. ConnectionConfigの準備
   - IpAddress = "999.999.999.999"（不正な値）
   - Port = 5000（正常な値）
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
   - Assert.ThrowsAsync<ArgumentException>() または Assert.ThrowsAsync<FormatException>()
   - ConnectAsync実行
2. **例外メッセージ検証**
   - Assert.Contains("不正なIPアドレス形式", exception.Message)
   - Assert.Contains("999.999.999.999", exception.Message)
3. **InnerException検証**
   - Assert.IsType<FormatException>(exception.InnerException)
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
- **エラーログ**: 不正IPアドレス検出時
  - ログレベル: Error
  - メッセージ: "ConnectAsync: 不正なIPアドレス形式: {IpAddress}"
  - 例外情報: ArgumentException詳細
  - スタックトレース: 含む

### ログレベル
- **Error**: 不正IPアドレス検出時（TC020_1）

### ログ実装例

```csharp
try
{
    IPAddress ipAddress = IPAddress.Parse(connectionConfig.IpAddress);
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
```

---

## 9. テスト実装チェックリスト

### TC020_1実装前
- [ ] PlcCommunicationManagerクラス作成（空実装）
- [ ] IPlcCommunicationManagerインターフェース作成
- [ ] ConnectAsyncメソッドシグネチャ定義
- [ ] ConnectionConfig・TimeoutConfigモデル作成
- [ ] ConnectionResponseモデル作成
- [ ] ConnectionStatus列挙型作成
- [ ] ErrorMessagesクラス作成

### TC020_1実装中
- [ ] Arrange: ConnectionConfig準備（IpAddress = "999.999.999.999"）
- [ ] Arrange: TimeoutConfig準備
- [ ] Arrange: PlcCommunicationManagerインスタンス作成
- [ ] Act: ConnectAsync呼び出し
- [ ] Assert: ArgumentException または FormatException発生確認
- [ ] Assert: 例外メッセージ検証
- [ ] Assert: InnerException型確認

### TC020_1実装後
- [ ] テスト実行・Red確認
- [ ] ConnectAsync本体実装（IPアドレス検証ロジック追加）
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施
  - [ ] エラーメッセージ統一化
  - [ ] ログ出力追加
  - [ ] コード重複削除

---

## 10. 参考情報

### IPアドレス形式仕様
- **有効範囲**: 各オクテット 0-255
- **形式**: "xxx.xxx.xxx.xxx"（ドット区切り4オクテット）
- **検証方法**: IPAddress.Parse()（.NET標準API）

### .NET IPAddress.Parse動作
- **成功**: 正常なIPアドレス文字列 → IPAddressインスタンス
- **失敗**: 不正な形式 → FormatExceptionスロー
- **エラー例**:
  - "999.999.999.999" → FormatException（範囲外）
  - "192.168.1" → FormatException（オクテット不足）
  - "192.168.1.1.1" → FormatException（オクテット過剰）
  - "abc.def.ghi.jkl" → FormatException（数値以外）

### テストデータバリエーション（将来拡張用）
**配置先**: Tests/TestUtilities/TestData/InvalidIpAddresses/

- 範囲外: "999.999.999.999", "256.0.0.1"
- オクテット不足: "192.168.1"
- オクテット過剰: "192.168.1.1.1"
- 数値以外: "abc.def.ghi.jkl"
- 空文字列: ""
- null: null（TC020_3でカバー）

---

## 11. C#実装例（完全版）

### ConnectAsync（IPアドレス検証部分）

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

### TC020_1テスト実装例

```csharp
[Fact]
public async Task TC020_1_ConnectAsync_不正IPアドレス()
{
    // Arrange
    var connectionConfig = new ConnectionConfig
    {
        IpAddress = "999.999.999.999",  // 不正なIPアドレス
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

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentException>(
        async () => await plcCommManager.ConnectAsync(connectionConfig, timeoutConfig)
    );

    // 例外メッセージ検証
    Assert.Contains("不正なIPアドレス形式", exception.Message);
    Assert.Contains("999.999.999.999", exception.Message);

    // InnerException検証
    Assert.NotNull(exception.InnerException);
    Assert.IsType<FormatException>(exception.InnerException);
}
```

---

以上が TC020_1_ConnectAsync_不正IPアドレステスト実装に必要な情報です。
