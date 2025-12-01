# Step3 ConnectAsync null入力テスト実装用情報（TC020_3）

## ドキュメント概要

### 目的
このドキュメントは、TC020_3_ConnectAsync_null入力テストの実装に必要な情報を集約したものです。
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
- **ConnectionConfig: null**
- **TimeoutConfig: null**

#### Output
- **失敗時（TC020_3で期待）**: ArgumentNullException スロー
  - 例外メッセージ: "ConnectionConfigまたはTimeoutConfigがnullです"
  - パラメータ名: "connectionConfig" または "timeoutConfig"

#### 機能
- null入力検証（ConnectAsync実行前の最優先チェック）
- ArgumentNullExceptionスロー
- 不正入力からのシステム保護

---

## 2. テストケース仕様（TC020_3）

### TC020_3_ConnectAsync_null入力
**目的**: null入力が正しく検出され、例外がスローされることをテスト

#### 前提条件
- PlcCommunicationManagerインスタンスが準備済み
- 未接続状態（Status != Connected）

#### 入力データバリエーション
本テストケースでは、3つのバリエーションをテストします：

##### バリエーション1: ConnectionConfig = null, TimeoutConfig = 正常
- **ConnectionConfig**: null
- **TimeoutConfig**: 正常なインスタンス
- **期待例外**: ArgumentNullException
- **期待パラメータ名**: "connectionConfig"

##### バリエーション2: ConnectionConfig = 正常, TimeoutConfig = null
- **ConnectionConfig**: 正常なインスタンス
- **TimeoutConfig**: null
- **期待例外**: ArgumentNullException
- **期待パラメータ名**: "timeoutConfig"

##### バリエーション3: ConnectionConfig = null, TimeoutConfig = null
- **ConnectionConfig**: null
- **TimeoutConfig**: null
- **期待例外**: ArgumentNullException
- **期待パラメータ名**: "connectionConfig" または "timeoutConfig"（どちらか一方）

#### 期待動作（異常系）
- **ArgumentNullException がスローされる**
- **例外メッセージ**: "ConnectionConfigまたはTimeoutConfigがnullです" または .NETデフォルトメッセージ
- **パラメータ名**: "connectionConfig" または "timeoutConfig"
- **接続処理は実行されない**（null検証でフェイルファスト）
- **Status: Disconnected のまま**

#### 検証項目
1. **ArgumentNullException発生確認**
2. **例外メッセージ検証**: null判定メッセージが含まれる
3. **パラメータ名確認**: "connectionConfig" または "timeoutConfig"
4. **Status確認**: Disconnected（接続処理未実行）

#### 重要性
- **セキュリティ**: NullReferenceException回避によるシステム保護
- **早期検出**: 接続処理前の最優先チェック（フェイルファスト）
- **診断性**: 明確なエラーメッセージによるデバッグ容易化
- **防御的プログラミング**: 最も基本的な入力検証

---

## 3. 依存クラス・設定

### ConnectionConfig（接続設定）
**取得元**: ConfigToFrameManager.LoadConfigAsync()

```csharp
public class ConnectionConfig
{
    public string IpAddress { get; set; }        // 例: "192.168.3.250"（TC020_3では使用されない）
    public int Port { get; set; }                 // 例: 5007（TC020_3では使用されない）
    public bool UseTcp { get; set; }              // true: TCP, false: UDP
    public string ConnectionType { get; set; }    // "TCP" or "UDP"
    public bool IsBinary { get; set; }            // false: ASCII, true: Binary
    public FrameVersion FrameVersion { get; set; } // FrameVersion.Frame4E
}
```

**TC020_3での使用**:
- バリエーション1: null
- バリエーション2: 正常なインスタンス
- バリエーション3: null

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

**TC020_3での使用**:
- バリエーション1: 正常なインスタンス
- バリエーション2: null
- バリエーション3: null

### ConnectionResponse（接続結果）
**取得元**: PlcCommunicationManager.ConnectAsync()

```csharp
public class ConnectionResponse
{
    public ConnectionStatus Status { get; set; }  // TC020_3では返らない（例外発生）
    public Socket? Socket { get; set; }           // TC020_3では返らない（例外発生）
    public EndPoint? RemoteEndPoint { get; set; } // TC020_3では返らない（例外発生）
    public DateTime? ConnectedAt { get; set; }    // TC020_3では返らない（例外発生）
    public TimeSpan? ConnectionTime { get; set; } // TC020_3では返らない（例外発生）
    public string? ErrorMessage { get; set; }     // TC020_3では返らない（例外発生）
}
```

**TC020_3での動作**:
- null入力検出により、ConnectionResponseは生成されない
- ArgumentNullException即座にスロー

### ConnectionStatus（接続状態列挙型）

```csharp
public enum ConnectionStatus
{
    Disconnected,  // TC020_3での状態（例外発生前）
    Connected,
    Error
}
```

---

## 4. エラーハンドリング

### ConnectAsync スロー例外（TC020_3で検証）
**ArgumentNullException**:
- **発生条件**: ConnectionConfig または TimeoutConfig が null
- **メッセージ形式**: "ConnectionConfigまたはTimeoutConfigがnullです" または .NETデフォルト
- **ErrorMessages定数**: ErrorMessages.ConfigNull
- **パラメータ名**: "connectionConfig" または "timeoutConfig"

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

#### 実装パターン1: 個別nullチェック（推奨）

```csharp
public async Task<ConnectionResponse> ConnectAsync(
    ConnectionConfig connectionConfig,
    TimeoutConfig timeoutConfig)
{
    // ConnectionConfig null チェック
    if (connectionConfig == null)
    {
        throw new ArgumentNullException(
            nameof(connectionConfig),
            ErrorMessages.ConfigNull);
    }

    // TimeoutConfig null チェック
    if (timeoutConfig == null)
    {
        throw new ArgumentNullException(
            nameof(timeoutConfig),
            ErrorMessages.ConfigNull);
    }

    // 以降の処理...
}
```

#### 実装パターン2: 統合nullチェック

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

    // 以降の処理...
}
```

#### 実装パターン3: C# 8.0以降のnull許容参照型（参考）

```csharp
public async Task<ConnectionResponse> ConnectAsync(
    ConnectionConfig connectionConfig,  // nullable無効（null不可）
    TimeoutConfig timeoutConfig)        // nullable無効（null不可）
{
    // C# 8.0以降では、コンパイル時に警告
    // ただし、ランタイムでnullが渡される可能性があるため、明示的チェックも推奨

    ArgumentNullException.ThrowIfNull(connectionConfig);  // C# 10.0以降
    ArgumentNullException.ThrowIfNull(timeoutConfig);     // C# 10.0以降

    // 以降の処理...
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
1. **TC020_3_ConnectAsync_null入力**（最優先、最も基本的な異常系）← 本テスト
2. **TC020_1_ConnectAsync_不正IPアドレス**
3. **TC020_2_ConnectAsync_不正ポート番号**
4. **TC020_4_ConnectAsync_既に接続済み状態での再接続**
5. **TC020_5_ConnectAsync_接続時間計測精度**（正常系に近い）

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック（TC020_3では不要）
- TC020_3では接続処理に到達しないため、モックSocket不要
- null検証のみでテスト完結

#### 使用するスタブ
- **ConfigurationStubs**: ConnectionConfig・TimeoutConfigのスタブ
  - NullConfig: null設定スタブ（実際には不要、テスト内で直接null使用）

---

## 6. テストケース実装構造

### バリエーション1: ConnectionConfig = null

#### Arrange（準備）
1. ConnectionConfig = null
2. TimeoutConfigの準備
   - ConnectTimeoutMs = 5000
   - SendTimeoutMs = 3000
   - ReceiveTimeoutMs = 5000
   - RetryTimeoutMs = 1000
3. PlcCommunicationManagerインスタンス作成

#### Act & Assert（実行・検証）
1. **例外発生確認**
   - Assert.ThrowsAsync<ArgumentNullException>()
   - ConnectAsync実行
2. **パラメータ名検証**
   - Assert.Equal("connectionConfig", exception.ParamName)
3. **例外メッセージ検証**（オプション）
   - Assert.Contains("ConnectionConfig", exception.Message) または Assert.Contains("null", exception.Message)

### バリエーション2: TimeoutConfig = null

#### Arrange（準備）
1. ConnectionConfigの準備
   - IpAddress = "192.168.1.10"
   - Port = 5000
   - UseTcp = true
   - ConnectionType = "TCP"
   - IsBinary = false
   - FrameVersion = FrameVersion.Frame4E
2. TimeoutConfig = null
3. PlcCommunicationManagerインスタンス作成

#### Act & Assert（実行・検証）
1. **例外発生確認**
   - Assert.ThrowsAsync<ArgumentNullException>()
   - ConnectAsync実行
2. **パラメータ名検証**
   - Assert.Equal("timeoutConfig", exception.ParamName)
3. **例外メッセージ検証**（オプション）
   - Assert.Contains("TimeoutConfig", exception.Message) または Assert.Contains("null", exception.Message)

### バリエーション3: 両方null

#### Arrange（準備）
1. ConnectionConfig = null
2. TimeoutConfig = null
3. PlcCommunicationManagerインスタンス作成

#### Act & Assert（実行・検証）
1. **例外発生確認**
   - Assert.ThrowsAsync<ArgumentNullException>()
   - ConnectAsync実行
2. **パラメータ名検証**（柔軟な検証）
   - Assert.True(exception.ParamName == "connectionConfig" || exception.ParamName == "timeoutConfig")
3. **例外メッセージ検証**（オプション）
   - Assert.Contains("null", exception.Message)

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
- **エラーログ**: null入力検出時
  - ログレベル: Error
  - メッセージ: "ConnectAsync: ConnectionConfigまたはTimeoutConfigがnullです"
  - 例外情報: ArgumentNullException詳細
  - スタックトレース: 含む

### ログレベル
- **Error**: null入力検出時（TC020_3）

### ログ実装例

#### ログ有り実装

```csharp
if (connectionConfig == null)
{
    _logger.LogError("ConnectAsync: ConnectionConfigがnullです");
    throw new ArgumentNullException(
        nameof(connectionConfig),
        ErrorMessages.ConfigNull);
}

if (timeoutConfig == null)
{
    _logger.LogError("ConnectAsync: TimeoutConfigがnullです");
    throw new ArgumentNullException(
        nameof(timeoutConfig),
        ErrorMessages.ConfigNull);
}
```

#### ログ無し実装（シンプル、推奨）

```csharp
// null検証はログ無しで即座にスロー（パフォーマンス優先）
if (connectionConfig == null)
{
    throw new ArgumentNullException(
        nameof(connectionConfig),
        ErrorMessages.ConfigNull);
}

if (timeoutConfig == null)
{
    throw new ArgumentNullException(
        nameof(timeoutConfig),
        ErrorMessages.ConfigNull);
}
```

---

## 9. テスト実装チェックリスト

### TC020_3実装前
- [ ] PlcCommunicationManagerクラス作成（空実装）
- [ ] IPlcCommunicationManagerインターフェース作成
- [ ] ConnectAsyncメソッドシグネチャ定義
- [ ] ConnectionConfig・TimeoutConfigモデル作成
- [ ] ErrorMessagesクラス作成

### TC020_3実装中（バリエーション1）
- [ ] Arrange: ConnectionConfig = null
- [ ] Arrange: TimeoutConfig準備
- [ ] Arrange: PlcCommunicationManagerインスタンス作成
- [ ] Act: ConnectAsync呼び出し
- [ ] Assert: ArgumentNullException発生確認
- [ ] Assert: パラメータ名確認（"connectionConfig"）

### TC020_3実装中（バリエーション2）
- [ ] Arrange: ConnectionConfig準備
- [ ] Arrange: TimeoutConfig = null
- [ ] Arrange: PlcCommunicationManagerインスタンス作成
- [ ] Act: ConnectAsync呼び出し
- [ ] Assert: ArgumentNullException発生確認
- [ ] Assert: パラメータ名確認（"timeoutConfig"）

### TC020_3実装中（バリエーション3）
- [ ] Arrange: ConnectionConfig = null
- [ ] Arrange: TimeoutConfig = null
- [ ] Arrange: PlcCommunicationManagerインスタンス作成
- [ ] Act: ConnectAsync呼び出し
- [ ] Assert: ArgumentNullException発生確認
- [ ] Assert: パラメータ名確認（どちらか一方）

### TC020_3実装後
- [ ] テスト実行・Red確認（全3バリエーション）
- [ ] ConnectAsync本体実装（null検証ロジック追加）
- [ ] テスト実行・Green確認（全3バリエーション）
- [ ] リファクタリング実施
  - [ ] エラーメッセージ統一化
  - [ ] ログ出力追加（オプション）
  - [ ] コード重複削除

---

## 10. 参考情報

### ArgumentNullException仕様
- **.NET標準例外**: System.ArgumentNullException
- **基底クラス**: System.ArgumentException
- **用途**: null不可パラメータにnullが渡された時
- **コンストラクタ**:
  - ArgumentNullException(string paramName)
  - ArgumentNullException(string paramName, string message)

### .NET null検証パターン
- **従来パターン**: if (obj == null) throw new ArgumentNullException(...)
- **C# 10.0以降**: ArgumentNullException.ThrowIfNull(obj)
- **C# 11.0以降**: 必須パラメータ（required修飾子）

### null許容参照型（C# 8.0以降）
- **有効化**: <Nullable>enable</Nullable>（.csprojに追加）
- **nullable型**: ConnectionConfig?（null許容）
- **non-nullable型**: ConnectionConfig（null不可）
- **コンパイル時警告**: null代入時に警告

---

## 11. C#実装例（完全版）

### ConnectAsync（null検証部分）

```csharp
public async Task<ConnectionResponse> ConnectAsync(
    ConnectionConfig connectionConfig,
    TimeoutConfig timeoutConfig)
{
    // ConnectionConfig null チェック（TC020_3 バリエーション1）
    if (connectionConfig == null)
    {
        throw new ArgumentNullException(
            nameof(connectionConfig),
            ErrorMessages.ConfigNull);
    }

    // TimeoutConfig null チェック（TC020_3 バリエーション2）
    if (timeoutConfig == null)
    {
        throw new ArgumentNullException(
            nameof(timeoutConfig),
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
        throw new InvalidOperationException(ErrorMessages.AlreadyConnected);
    }

    // 実際の接続処理...
}
```

### TC020_3テスト実装例

```csharp
[Fact]
public async Task TC020_3_1_ConnectAsync_ConnectionConfig_null()
{
    // Arrange
    ConnectionConfig connectionConfig = null;  // null

    var timeoutConfig = new TimeoutConfig
    {
        ConnectTimeoutMs = 5000,
        SendTimeoutMs = 3000,
        ReceiveTimeoutMs = 5000,
        RetryTimeoutMs = 1000
    };

    var plcCommManager = new PlcCommunicationManager(_logger);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(
        async () => await plcCommManager.ConnectAsync(connectionConfig, timeoutConfig)
    );

    // パラメータ名検証
    Assert.Equal("connectionConfig", exception.ParamName);
}

[Fact]
public async Task TC020_3_2_ConnectAsync_TimeoutConfig_null()
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

    TimeoutConfig timeoutConfig = null;  // null

    var plcCommManager = new PlcCommunicationManager(_logger);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(
        async () => await plcCommManager.ConnectAsync(connectionConfig, timeoutConfig)
    );

    // パラメータ名検証
    Assert.Equal("timeoutConfig", exception.ParamName);
}

[Fact]
public async Task TC020_3_3_ConnectAsync_両方_null()
{
    // Arrange
    ConnectionConfig connectionConfig = null;  // null
    TimeoutConfig timeoutConfig = null;         // null

    var plcCommManager = new PlcCommunicationManager(_logger);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(
        async () => await plcCommManager.ConnectAsync(connectionConfig, timeoutConfig)
    );

    // パラメータ名検証（どちらか一方）
    Assert.True(
        exception.ParamName == "connectionConfig" ||
        exception.ParamName == "timeoutConfig",
        $"期待: 'connectionConfig' または 'timeoutConfig', 実際: '{exception.ParamName}'"
    );
}
```

### Theory属性を使用した統合テスト実装例（参考）

```csharp
[Theory]
[MemberData(nameof(NullConfigTestData))]
public async Task TC020_3_ConnectAsync_null入力_Theory(
    ConnectionConfig connectionConfig,
    TimeoutConfig timeoutConfig,
    string expectedParamName)
{
    // Arrange
    var plcCommManager = new PlcCommunicationManager(_logger);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(
        async () => await plcCommManager.ConnectAsync(connectionConfig, timeoutConfig)
    );

    // パラメータ名検証
    if (expectedParamName == "either")
    {
        Assert.True(
            exception.ParamName == "connectionConfig" ||
            exception.ParamName == "timeoutConfig"
        );
    }
    else
    {
        Assert.Equal(expectedParamName, exception.ParamName);
    }
}

public static IEnumerable<object[]> NullConfigTestData()
{
    var validConnectionConfig = new ConnectionConfig
    {
        IpAddress = "192.168.1.10",
        Port = 5000,
        UseTcp = true,
        ConnectionType = "TCP",
        IsBinary = false,
        FrameVersion = FrameVersion.Frame4E
    };

    var validTimeoutConfig = new TimeoutConfig
    {
        ConnectTimeoutMs = 5000,
        SendTimeoutMs = 3000,
        ReceiveTimeoutMs = 5000,
        RetryTimeoutMs = 1000
    };

    yield return new object[] { null, validTimeoutConfig, "connectionConfig" };
    yield return new object[] { validConnectionConfig, null, "timeoutConfig" };
    yield return new object[] { null, null, "either" };
}
```

---

以上が TC020_3_ConnectAsync_null入力テスト実装に必要な情報です。
