# TC124_ErrorPropagation_Step3エラー時後続スキップ統合テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC124（ErrorPropagation_Step3エラー時後続スキップ）の統合テストケースを、TDD手法に従って実装してください。

---

## 実装概要

### 目的
PlcCommunicationManagerの完全サイクル実行において、Step3（接続段階）でエラーが発生した際の
エラー伝播動作を検証します。
このテストは、Step3でエラーが発生した時に：
1. 後続のStep4-6が実行されないこと（スキップ）
2. エラー情報が適切に記録・伝播されること
3. システムリソースが適切に管理されること
4. 統計情報にエラーが正しく反映されること
を確認します。

### 実装対象
- **テストファイル**: `Tests/Integration/Core/Managers/PlcCommunicationManagerIntegrationTests.cs`
- **テスト名前空間**: `andon.Tests.Integration.Core.Managers`
- **テストメソッド名**: `TC124_ErrorPropagation_Step3エラー時後続スキップ_接続タイムアウト`, `TC124_ErrorPropagation_Step3エラー時後続スキップ_接続拒否`, `TC124_ErrorPropagation_Step3エラー時後続スキップ_不正IP`

---

## 前提条件の確認

実装開始前に以下を確認してください：

1. **依存ファイルの存在確認**
   - `Core/Managers/PlcCommunicationManager.cs`
   - `Core/Models/AsyncOperationResult.cs`
   - `Core/Models/ConnectionResponse.cs`
   - `Core/Exceptions/PlcConnectionException.cs`
   - `Core/Interfaces/IAsyncExceptionHandler.cs`
   - `Core/Models/ConnectionStats.cs`

2. **テストユーティリティの確認**
   - `Tests/TestUtilities/Mocks/MockSocket.cs`
   - `Tests/TestUtilities/Mocks/MockExceptionGenerator.cs`
   - `Tests/TestUtilities/Assertions/ErrorPropagationAssertions.cs`

3. **開発手法ドキュメント確認**
   - `C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.md`を参照

不足しているファイルがあれば報告してください。

---

## 実装手順（TDD Red-Green-Refactor）

### Phase 1: Red（テスト失敗）

#### Step 1-1: テストファイル拡張
```
ファイル: Tests/Integration/Core/Managers/PlcCommunicationManagerIntegrationTests.cs
名前空間: andon.Tests.Integration.Core.Managers
```

#### Step 1-2: テストケース実装（3つ）

**TC124-1: 接続タイムアウト時のエラー伝播テスト**

**Arrange（準備）**:
- MockSocketを作成（接続タイムアウトをシミュレート）
- ConnectionConfigを作成
  - IpAddress = "192.168.3.250"
  - Port = 5007
  - UseTcp = true
  - ConnectionTimeoutMs = 1000
- TimeoutConfigを作成
  - ConnectTimeoutMs = 1000
- PlcCommunicationManagerインスタンス作成
- 初期統計情報を記録
- エラーカウンターを準備

**Act（実行）**:
```csharp
AsyncOperationResult<StructuredData> result = null;
Exception caughtException = null;
DateTime errorOccurredAt = DateTime.MinValue;

try
{
    // Step3: Connect（タイムアウトエラー発生）
    errorOccurredAt = DateTime.Now;
    var connectResponse = await manager.ConnectAsync(connectionConfig);

    // 以降の処理は実行されない予定
    Assert.True(false, "接続タイムアウト時は例外がスローされるべき");
}
catch (TimeoutException ex)
{
    caughtException = ex;
    result = manager.GetLastOperationResult();
}
catch (PlcConnectionException ex)
{
    caughtException = ex;
    result = manager.GetLastOperationResult();
}
```

**Assert（検証）**:

*エラータイプ検証*:
- 適切な例外タイプがスローされること（TimeoutExceptionまたはPlcConnectionException）
- エラーメッセージに「タイムアウト」が含まれること
- 例外にスタックトレースが含まれること

*エラー伝播検証*:
- result.IsSuccess = false
- result.FailedStep = "Step3"
- result.Exception != null
- result.ErrorDetails に以下が含まれること：
  - ErrorType: "ConnectionTimeout"
  - ErrorMessage: タイムアウト詳細
  - OccurredAt: エラー発生時刻
  - FailedOperation: "ConnectAsync"

*後続スキップ検証*:
- Step4（SendFrame）が実行されていないこと
- Step5（ReceiveResponse）が実行されていないこと
- Step6（データ処理）が実行されていないこと
- result.StepResults.ContainsKey("Step4") = false
- result.StepResults.ContainsKey("Step5") = false
- result.StepResults.ContainsKey("Step6") = false

*統計情報検証*:
- manager.ConnectionStats.TotalErrors = 1
- manager.ConnectionStats.ConnectionErrors = 1
- manager.ConnectionStats.TimeoutErrors = 1
- manager.ConnectionStats.TotalAttempts = 1
- manager.ConnectionStats.SuccessfulConnections = 0
- manager.ConnectionStats.SuccessRate = 0.0

*リソース管理検証*:
- manager.IsConnected = false
- manager.GetCurrentSocket() = null
- ソケットが適切に破棄されていること

*システム状態検証*:
- システムが初期状態のまま維持されていること
- メモリリークが発生していないこと

**TC124-2: 接続拒否時のエラー伝播テスト**

**Arrange（準備）**:
- MockSocketを作成（接続拒否をシミュレート）
- 同様のConnectionConfig設定

**Act（実行）**:
```csharp
try
{
    // Step3: Connect（接続拒否エラー発生）
    var connectResponse = await manager.ConnectAsync(connectionConfig);
    Assert.True(false, "接続拒否時は例外がスローされるべき");
}
catch (SocketException ex)
{
    caughtException = ex;
    result = manager.GetLastOperationResult();
}
catch (PlcConnectionException ex)
{
    caughtException = ex;
    result = manager.GetLastOperationResult();
}
```

**Assert（検証）**:

*エラータイプ検証*:
- SocketExceptionまたはPlcConnectionExceptionがスローされること
- エラーメッセージに「接続拒否」または「Connection refused」が含まれること

*エラー伝播検証*:
- result.FailedStep = "Step3"
- result.ErrorDetails.ErrorType = "ConnectionRefused"
- IPアドレスとポート情報がエラー詳細に含まれること

*統計情報検証*:
- ConnectionStats.ConnectionErrors = 1
- ConnectionStats.RefusedErrors = 1

**TC124-3: 不正IP時のエラー伝播テスト**

**Arrange（準備）**:
- 不正なIPアドレス設定
  - IpAddress = "999.999.999.999"（不正な形式）

**Act（実行）**:
```csharp
try
{
    // Step3: Connect（不正IPエラー発生）
    var connectResponse = await manager.ConnectAsync(connectionConfig);
    Assert.True(false, "不正IP時は例外がスローされるべき");
}
catch (ArgumentException ex)
{
    caughtException = ex;
    result = manager.GetLastOperationResult();
}
catch (PlcConnectionException ex)
{
    caughtException = ex;
    result = manager.GetLastOperationResult();
}
```

**Assert（検証）**:

*エラータイプ検証*:
- ArgumentExceptionまたはPlcConnectionExceptionがスローされること
- エラーメッセージに「不正」または「invalid」が含まれること

*エラー伝播検証*:
- result.ErrorDetails.ErrorType = "InvalidIpAddress"
- 不正なIPアドレス値がエラー詳細に含まれること

*統計情報検証*:
- ConnectionStats.ValidationErrors = 1

#### Step 1-3: テスト実行（Red確認）
```bash
dotnet test --filter "FullyQualifiedName~TC124"
```

期待結果: テスト失敗（エラー伝播処理が未実装のため）

---

### Phase 2: Green（最小実装）

#### Step 2-1: エラー伝播機能実装

**AsyncOperationResult拡張（エラー詳細情報追加）**:
```csharp
public class AsyncOperationResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? FailedStep { get; set; }
    public Exception? Exception { get; set; }
    public ErrorDetails? ErrorDetails { get; set; }
    public Dictionary<string, object> StepResults { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;
}

public class ErrorDetails
{
    public string ErrorType { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string FailedOperation { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}
```

**ConnectionStats拡張（詳細エラー統計追加）**:
```csharp
public class ConnectionStats
{
    // 既存プロパティ
    public int TotalAttempts { get; set; }
    public int SuccessfulConnections { get; set; }
    public int TotalErrors { get; set; }

    // 新規追加プロパティ
    public int ConnectionErrors { get; set; }
    public int TimeoutErrors { get; set; }
    public int RefusedErrors { get; set; }
    public int ValidationErrors { get; set; }

    public double SuccessRate => TotalAttempts > 0
        ? (double)SuccessfulConnections / TotalAttempts
        : 0.0;

    public void AddConnectionError(string errorType)
    {
        TotalErrors++;
        ConnectionErrors++;

        switch (errorType)
        {
            case "Timeout":
                TimeoutErrors++;
                break;
            case "Refused":
                RefusedErrors++;
                break;
            case "Validation":
                ValidationErrors++;
                break;
        }
    }
}
```

**PlcCommunicationManager エラー伝播実装**:
```csharp
public async Task<ConnectionResponse> ConnectAsync(ConnectionConfig config)
{
    // 操作結果の初期化
    if (_lastOperationResult == null)
    {
        _lastOperationResult = new AsyncOperationResult<StructuredData>
        {
            StartTime = DateTime.Now
        };
    }

    try
    {
        _connectionStats.TotalAttempts++;

        // IP検証
        if (!IPAddress.TryParse(config.IpAddress, out _))
        {
            throw new PlcConnectionException(
                $"不正なIPアドレス: {config.IpAddress}",
                new ArgumentException($"Invalid IP address: {config.IpAddress}"));
        }

        // Socket作成・接続
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.ReceiveTimeout = config.TimeoutConfig.ReceiveTimeoutMs;
        _socket.SendTimeout = config.TimeoutConfig.SendTimeoutMs;

        var endpoint = new IPEndPoint(IPAddress.Parse(config.IpAddress), config.Port);

        // タイムアウト処理付き接続
        var connectTask = _socket.ConnectAsync(endpoint);
        if (await Task.WhenAny(connectTask, Task.Delay(config.TimeoutConfig.ConnectTimeoutMs)) != connectTask)
        {
            _socket?.Dispose();
            _socket = null;
            throw new TimeoutException($"接続タイムアウト: {config.IpAddress}:{config.Port}");
        }

        // 接続成功
        _isConnected = true;
        _connectionStats.SuccessfulConnections++;

        var response = new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            Socket = _socket,
            ConnectedAt = DateTime.Now
        };

        _lastOperationResult.StepResults["Step3"] = response;

        return response;
    }
    catch (TimeoutException ex)
    {
        HandleConnectionError(ex, "Timeout", "ConnectAsync", config);
        throw new PlcConnectionException("接続タイムアウトが発生しました", ex);
    }
    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
    {
        HandleConnectionError(ex, "Refused", "ConnectAsync", config);
        throw new PlcConnectionException("接続が拒否されました", ex);
    }
    catch (ArgumentException ex)
    {
        HandleConnectionError(ex, "Validation", "ConnectAsync", config);
        throw new PlcConnectionException("接続パラメータが不正です", ex);
    }
    catch (Exception ex)
    {
        HandleConnectionError(ex, "Unknown", "ConnectAsync", config);
        throw;
    }
}

private void HandleConnectionError(
    Exception ex,
    string errorType,
    string operation,
    ConnectionConfig config)
{
    // 統計情報更新
    _connectionStats.AddConnectionError(errorType);

    // エラー詳細記録
    _lastOperationResult.IsSuccess = false;
    _lastOperationResult.FailedStep = "Step3";
    _lastOperationResult.Exception = ex;
    _lastOperationResult.ErrorDetails = new ErrorDetails
    {
        ErrorType = $"Connection{errorType}",
        ErrorMessage = ex.Message,
        OccurredAt = DateTime.Now,
        FailedOperation = operation,
        AdditionalInfo = new Dictionary<string, object>
        {
            { "IpAddress", config.IpAddress },
            { "Port", config.Port },
            { "UseTcp", config.UseTcp }
        }
    };
    _lastOperationResult.EndTime = DateTime.Now;

    // リソース解放
    _socket?.Dispose();
    _socket = null;
    _isConnected = false;
}

public AsyncOperationResult<StructuredData> GetLastOperationResult()
{
    return _lastOperationResult ?? new AsyncOperationResult<StructuredData>();
}
```

#### Step 2-2: テスト再実行（Green確認）
```bash
dotnet test --filter "FullyQualifiedName~TC124"
```

期待結果: すべてのテストがパス

---

### Phase 3: Refactor（リファクタリング）

#### Step 3-1: コード品質向上
- エラータイプの列挙型化
  - ErrorTypeをenumで定義
  - 文字列比較を型安全に変更
- ログ出力追加
  - エラー発生時の詳細ログ
  - 統計情報の定期的なログ出力
  - デバッグ用トレースログ
- エラーメッセージの国際化
  - リソースファイルによるメッセージ管理
- パフォーマンス最適化
  - エラー処理のオーバーヘッド削減
- ドキュメントコメント追加
  - エラー伝播動作の説明
  - 統計情報の説明

#### Step 3-2: テスト再実行（Green維持確認）
```bash
dotnet test --filter "FullyQualifiedName~TC124"
```

期待結果: すべてのテストがパス（リファクタリング後も）

---

## 技術仕様詳細

### エラータイプ分類

#### Step3エラータイプ一覧
1. **ConnectionTimeout**: 接続タイムアウト
   - 原因: PLC応答なし、ネットワーク遅延
   - 対処: タイムアウト値の調整、ネットワーク確認

2. **ConnectionRefused**: 接続拒否
   - 原因: PLCサービス停止、ファイアウォール
   - 対処: PLCサービス起動、ファイアウォール設定

3. **InvalidIpAddress**: 不正IPアドレス
   - 原因: 設定ミス、入力検証不足
   - 対処: 設定値の確認、検証強化

4. **NetworkUnreachable**: ネットワーク到達不能
   - 原因: ルーティング問題、ケーブル断線
   - 対処: ネットワーク構成確認

5. **HostUnreachable**: ホスト到達不能
   - 原因: PLCシャットダウン、IP設定ミス
   - 対処: PLC起動状態確認、IP設定確認

### 統計情報詳細

#### ConnectionStats計測項目
```csharp
public class ConnectionStats
{
    // 基本統計
    public int TotalAttempts { get; set; }              // 総接続試行回数
    public int SuccessfulConnections { get; set; }      // 成功接続回数
    public int TotalErrors { get; set; }                // 総エラー回数

    // エラー分類統計
    public int ConnectionErrors { get; set; }           // 接続エラー総数
    public int TimeoutErrors { get; set; }              // タイムアウトエラー
    public int RefusedErrors { get; set; }              // 接続拒否エラー
    public int ValidationErrors { get; set; }           // 検証エラー
    public int NetworkErrors { get; set; }              // ネットワークエラー

    // パフォーマンス統計
    public List<double> ResponseTimes { get; set; } = new();    // 接続時間履歴
    public double AverageResponseTime => ResponseTimes.Any()
        ? ResponseTimes.Average()
        : 0.0;
    public double MaxResponseTime => ResponseTimes.Any()
        ? ResponseTimes.Max()
        : 0.0;
    public double MinResponseTime => ResponseTimes.Any()
        ? ResponseTimes.Min()
        : 0.0;

    // 成功率
    public double SuccessRate => TotalAttempts > 0
        ? (double)SuccessfulConnections / TotalAttempts
        : 0.0;

    // エラー率
    public double ErrorRate => TotalAttempts > 0
        ? (double)TotalErrors / TotalAttempts
        : 0.0;
}
```

### エラー伝播フロー

#### Step3エラー発生時のフロー
```
1. ConnectAsync実行
   ↓
2. エラー発生（Timeout/Refused/Validation）
   ↓
3. HandleConnectionError呼び出し
   ↓
4. 統計情報更新（ConnectionStats.AddConnectionError）
   ↓
5. エラー詳細記録（AsyncOperationResult.ErrorDetails設定）
   ↓
6. リソース解放（Socket.Dispose）
   ↓
7. 例外スロー（PlcConnectionException）
   ↓
8. 後続Step4-6はスキップ（実行されない）
   ↓
9. 呼び出し元でcatch
   ↓
10. GetLastOperationResultでエラー詳細取得
```

---

## モック・テストデータ実装

### MockSocket拡張（Step3エラーシミュレーション）
```csharp
public class MockSocket
{
    private readonly SocketBehavior _behavior;

    public enum SocketBehavior
    {
        ConnectSuccess,
        ConnectTimeout,
        ConnectRefused,
        ConnectNetworkUnreachable
    }

    public MockSocket(SocketBehavior behavior)
    {
        _behavior = behavior;
    }

    public async Task ConnectAsync(EndPoint endpoint)
    {
        switch (_behavior)
        {
            case SocketBehavior.ConnectSuccess:
                Connected = true;
                return;

            case SocketBehavior.ConnectTimeout:
                await Task.Delay(10000); // タイムアウトまで待機
                throw new TimeoutException("接続タイムアウト");

            case SocketBehavior.ConnectRefused:
                throw new SocketException((int)SocketError.ConnectionRefused);

            case SocketBehavior.ConnectNetworkUnreachable:
                throw new SocketException((int)SocketError.NetworkUnreachable);
        }
    }

    public bool Connected { get; private set; }
}
```

### ErrorPropagationAssertions
```csharp
public static class ErrorPropagationAssertions
{
    public static void AssertStep3ErrorPropagation(
        AsyncOperationResult<StructuredData> result,
        Type expectedExceptionType,
        string expectedErrorType)
    {
        // 基本検証
        Assert.False(result.IsSuccess, "IsSuccessがfalseであること");
        Assert.Equal("Step3", result.FailedStep);
        Assert.NotNull(result.Exception);
        Assert.IsType(expectedExceptionType, result.Exception);

        // エラー詳細検証
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal(expectedErrorType, result.ErrorDetails.ErrorType);
        Assert.NotEmpty(result.ErrorDetails.ErrorMessage);
        Assert.True(result.ErrorDetails.OccurredAt > DateTime.MinValue);
        Assert.Equal("ConnectAsync", result.ErrorDetails.FailedOperation);

        // 後続ステップスキップ検証
        Assert.False(result.StepResults.ContainsKey("Step4"), "Step4はスキップされること");
        Assert.False(result.StepResults.ContainsKey("Step5"), "Step5はスキップされること");
        Assert.False(result.StepResults.ContainsKey("Step6"), "Step6はスキップされること");

        // Step3の記録がないことを確認（エラーで失敗したため）
        Assert.False(result.StepResults.ContainsKey("Step3"), "Step3は失敗したため記録されないこと");
    }

    public static void AssertConnectionStats(
        ConnectionStats stats,
        int expectedTotalAttempts,
        int expectedErrors,
        int expectedConnectionErrors,
        string errorType)
    {
        Assert.Equal(expectedTotalAttempts, stats.TotalAttempts);
        Assert.Equal(expectedErrors, stats.TotalErrors);
        Assert.Equal(expectedConnectionErrors, stats.ConnectionErrors);
        Assert.Equal(0, stats.SuccessfulConnections);
        Assert.Equal(0.0, stats.SuccessRate);

        // エラータイプ別統計
        switch (errorType)
        {
            case "Timeout":
                Assert.Equal(1, stats.TimeoutErrors);
                break;
            case "Refused":
                Assert.Equal(1, stats.RefusedErrors);
                break;
            case "Validation":
                Assert.Equal(1, stats.ValidationErrors);
                break;
        }
    }

    public static void AssertResourceCleanup(PlcCommunicationManager manager)
    {
        Assert.False(manager.IsConnected, "接続状態がfalseであること");
        Assert.Null(manager.GetCurrentSocket(), "ソケットが解放されていること");
    }
}
```

---

## 実装記録・ドキュメント作成要件

### 必須作業項目

#### 1. 進捗記録開始
**ファイル**: `documents/implementation_records/progress_notes/2025-11-06_TC124実装.md`
- 実装開始時刻
- 目標（TC124エラー伝播テスト実装完了）
- 実装方針（Step3エラー時の伝播動作検証）

#### 2. 実装記録作成
**ファイル**: `documents/implementation_records/method_records/エラー伝播機能実装記録.md`
- エラータイプ分類の設計判断
- 統計情報の設計根拠
- エラー伝播フローの実装方針
- 発生した問題と解決過程

#### 3. テスト結果保存
**ファイル**: `documents/implementation_records/execution_logs/TC124_エラー伝播テスト結果.log`
- 各エラータイプでのテスト結果
- 統計情報の正確性検証結果
- リソース解放確認結果
- パフォーマンス影響測定結果

---

## 完了条件

以下すべてが満たされた時点で実装完了とする：

- [ ] TC124-1（接続タイムアウト時エラー伝播）テストがパス
- [ ] TC124-2（接続拒否時エラー伝播）テストがパス
- [ ] TC124-3（不正IP時エラー伝播）テストがパス
- [ ] AsyncOperationResult拡張（ErrorDetails追加）実装完了
- [ ] ConnectionStats拡張（詳細エラー統計）実装完了
- [ ] エラー伝播機能実装完了
- [ ] リファクタリング完了（エラータイプ列挙型化、ログ出力等）
- [ ] テスト再実行でGreen維持確認
- [ ] 進捗記録作成完了
- [ ] 実装記録作成完了
- [ ] C:\Users\1010821\Desktop\python\andon\documents\design\チェックリスト\step3to6_test実装用プロンプト.mdの該当項目にチェック

---

## 実装時の注意点

### TDD手法厳守
- 必ずテストを先に書く（Red）
- 最小実装でテストをパスさせる（Green）
- リファクタリングで品質向上（Refactor）
- 各フェーズでテスト実行を確認

### エラー伝播の正確性
- エラータイプの正確な分類
- エラー情報の完全性（すべての情報を記録）
- 統計情報の正確性（カウンター漏れなし）
- 後続ステップのスキップ保証

### リソース管理の徹底
- エラー時のソケット確実解放
- メモリリークの防止
- 接続状態の正確な管理
- ガベージコレクションへの協力

### 統計情報の正確性
- すべてのエラーがカウントされること
- エラータイプ別統計の正確性
- 成功率・エラー率の正確な計算
- 履歴データの適切な管理

### テストの網羅性
- 主要なエラータイプをすべてカバー
- 境界値ケースの考慮
- 異常系の徹底的なテスト
- 実際の運用で発生しうるエラーの再現

### 記録の重要性
- エラー伝播設計の判断根拠を記録
- テスト結果は統計データも含めて保存
- パフォーマンス影響を数値で測定・記録

### 文字化け対策
- 日本語ファイル名の新規作成時は`.txt`経由で作成
- 作成後は必ずReadツールで確認
- 文字化け発見時は早期に対処

---

## 参考情報

### 設計書参照先
- `documents/design/クラス設計.md`
- `documents/design/エラーハンドリング.md`
- `documents/design/テスト内容.md`

### 開発手法
- `documents/development_methodology/development-methodology.md`

### C#例外処理ベストプラクティス
- Microsoft公式ドキュメント
- .NET例外処理ガイドライン
- Socket通信エラーハンドリング

### 既存テスト参照
- `integration_TC121.md`（正常系完全サイクルテスト）
- `integration_TC123.md`（エラー発生時スキップテスト）

### ソケット通信エラー参照
- SocketError列挙型ドキュメント
- TCP/IP通信エラーコード一覧

---

以上の指示に従って、TC124_ErrorPropagation_Step3エラー時後続スキップ統合テストの実装を開始してください。

不明点や不足情報があれば、実装前に質問してください。
