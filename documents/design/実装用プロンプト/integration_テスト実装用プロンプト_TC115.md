# TC115: Step3to5_TCP完全サイクル正常動作 テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC115（Step3to5_TCP完全サイクル正常動作）のテストケースを、TDD手法に従って実装してください。

---

## TC115: Step3to5_TCP完全サイクル正常動作 ✅

### 目的
PlcCommunicationManagerのStep3（接続）→Step4（送信）→Step5（受信・切断）の完全サイクルをTCP接続で検証する統合テスト
19時deadline対応のPhase 2連続動作確認の重要テスト

## 実装概要

### 統合テストの位置づけ
この統合テストは、単体テスト（TC021等）とは異なり、複数のステップを連続実行して完全なPLC通信サイクルを検証します。
Step3→Step4→Step5の各段階が連続して正常動作することを確認する、Phase 2の重要な検証テストです。

### 実装対象
- **テストファイル**: `Tests/Integration/Core/Managers/PlcCommunicationManagerIntegrationTests.cs`
- **テスト名前空間**: `andon.Tests.Integration.Core.Managers`
- **テストメソッド名**: `TC115_Step3to5_TCP完全サイクル正常動作`

---

## 前提条件の確認

実装開始前に以下を確認してください：

1. **統合テスト環境の確認**
   - `Tests/Integration/` フォルダ構造の存在
   - MockPlcServerのTCP対応実装済み
   - 統合テスト用設定ファイルの準備

2. **依存する単体テストの完了確認**
   - TC021 (SendFrameAsync) が実装・テスト完了済み
   - TC029 (ProcessReceivedRawData) が実装・テスト完了済み
   - 接続・切断関連テストが完了済み

3. **必要なクラス・インターフェースの確認**
   - `Core/Managers/PlcCommunicationManager.cs`
   - `Core/Models/CycleExecutionResult.cs`
   - `Tests/TestUtilities/MockPlcServer.cs`
   - TCP対応の各種設定クラス

4. **開発手法ドキュメント確認**
   - `C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.md`を参照

不足しているファイルや前提条件があれば報告してください。

---

## 実装手順（TDD Red-Green-Refactor）

### Phase 1: Red（テスト失敗）

#### Step 1-1: 統合テストファイル準備
```
ファイル: Tests/Integration/Core/Managers/PlcCommunicationManagerIntegrationTests.cs
名前空間: andon.Tests.Integration.Core.Managers
```

#### Step 1-2: 統合テストケース実装

**Arrange（準備）**:
- MockPlcServer作成・TCP対応設定
- ConnectionConfig作成（TCP用設定）
- TimeoutConfig作成（統合テスト用長めの設定）
- PlcCommunicationManagerインスタンス作成
- 送信フレーム準備（SLMP読み込みコマンド）
- 期待される応答データ準備

**Act（実行）**:
```csharp
// 完全サイクル実行
var cycleResult = await manager.ExecuteStep3to5CycleAsync(
    connectionConfig,
    timeoutConfig,
    sendFrame,
    cancellationToken
);
```

**Assert（検証）**:
- cycleResult.IsSuccess == true
- 各ステップの結果検証（接続・送信・受信・切断）
- 統計情報の正確性検証
- 実行時間の妥当性検証

#### Step 1-3: テスト実行（Red確認）
```bash
dotnet test --filter "FullyQualifiedName~TC115" --logger "console;verbosity=detailed"
```

期待結果: テスト失敗（ExecuteStep3to5CycleAsyncが未実装のため）

---

### Phase 2: Green（最小実装）

#### Step 2-1: ExecuteStep3to5CycleAsync実装

**実装箇所**: `Core/Managers/PlcCommunicationManager.cs`

**最小実装要件**:
```csharp
public async Task<CycleExecutionResult> ExecuteStep3to5CycleAsync(
    ConnectionConfig connectionConfig,
    TimeoutConfig timeoutConfig,
    byte[] sendFrame,
    CancellationToken cancellationToken = default)
{
    var cycleResult = new CycleExecutionResult();
    var startTime = DateTime.UtcNow;

    try
    {
        // Step3: 接続（最小実装）
        cycleResult.ConnectResult = new ConnectionResponse { IsSuccess = true };
        cycleResult.TotalStepsExecuted++;

        // Step4: 送信（最小実装）
        cycleResult.SendResult = new SendResponse { IsSuccess = true };
        cycleResult.TotalStepsExecuted++;

        // Step4: 受信（最小実装）
        cycleResult.ReceiveResult = new RawResponseData
        {
            ResponseData = new byte[] { 0x01, 0x02, 0x03 }
        };
        cycleResult.TotalStepsExecuted++;

        // Step5: 切断（最小実装）
        // 切断処理（実装省略）
        cycleResult.TotalStepsExecuted++;

        cycleResult.IsSuccess = true;
        cycleResult.SuccessfulSteps = 4;
        cycleResult.TotalExecutionTime = DateTime.UtcNow - startTime;
        cycleResult.CompletedAt = DateTime.UtcNow;

        return cycleResult;
    }
    catch (Exception ex)
    {
        cycleResult.IsSuccess = false;
        cycleResult.ErrorMessage = ex.Message;
        return cycleResult;
    }
}
```

#### Step 2-2: テスト再実行（Green確認）
```bash
dotnet test --filter "FullyQualifiedName~TC115"
```

期待結果: テストがパス

---

### Phase 3: Refactor（リファクタリング）

#### Step 3-1: 完全実装
- 実際のTCP接続処理の実装
- 実際のSLMP送受信処理の実装
- エラーハンドリングの強化
- ログ出力の追加
- 統計情報の正確な更新
- パフォーマンス最適化

#### Step 3-2: テスト再実行（Green維持確認）
```bash
dotnet test --filter "FullyQualifiedName~TC115"
```

期待結果: すべてのテストがパス（リファクタリング後も）

---

---

## 技術仕様詳細

### TCP通信サイクル仕様

#### 完全サイクル実行フロー
```csharp
// Step3-5完全サイクル実行エンジン
public class Tcp CycleExecutionEngine
{
    public async Task<CycleExecutionResult> ExecuteFullCycle(
        ConnectionConfig config,
        TimeoutConfig timeouts,
        byte[] sendFrame)
    {
        var result = new CycleExecutionResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Step3: TCP接続確立
            var connectResult = await EstablishTcpConnection(config, timeouts.ConnectTimeoutMs);
            result.ConnectResult = connectResult;
            if (!connectResult.IsSuccess) return result;

            // Step4: SLMP送信
            var sendResult = await SendSlmpFrame(sendFrame, timeouts.SendTimeoutMs);
            result.SendResult = sendResult;
            if (!sendResult.IsSuccess) return result;

            // Step4: SLMP受信
            var receiveResult = await ReceiveSlmpResponse(timeouts.ReceiveTimeoutMs);
            result.ReceiveResult = receiveResult;
            if (receiveResult.ResponseData == null) return result;

            // Step5: TCP接続切断
            await DisconnectTcp(timeouts.DisconnectTimeoutMs);

            result.IsSuccess = true;
            result.TotalExecutionTime = stopwatch.Elapsed;
            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }
}
```

#### TCP接続処理詳細
```csharp
// TCP接続確立処理
private async Task<ConnectionResponse> EstablishTcpConnection(
    ConnectionConfig config, int timeoutMs)
{
    var tcpClient = new TcpClient();
    var response = new ConnectionResponse();

    try
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        await tcpClient.ConnectAsync(config.IpAddress, config.Port, cts.Token);

        response.IsSuccess = true;
        response.ConnectedAt = DateTime.UtcNow;
        response.Protocol = "TCP";
        response.Socket = tcpClient.Client;

        return response;
    }
    catch (SocketException ex)
    {
        response.IsSuccess = false;
        response.ErrorMessage = $"TCP接続失敗: {ex.Message}";
        return response;
    }
}
```

### データモデル詳細

#### CycleExecutionResult構造
```csharp
public class CycleExecutionResult
{
    // 基本結果
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? TotalExecutionTime { get; set; }

    // 各ステップの詳細結果
    public ConnectionResponse? ConnectResult { get; set; }
    public SendResponse? SendResult { get; set; }
    public RawResponseData? ReceiveResult { get; set; }
    public DisconnectResponse? DisconnectResult { get; set; }

    // 統計情報
    public int TotalStepsExecuted { get; set; }
    public int SuccessfulSteps { get; set; }
    public List<string> StepErrors { get; set; } = new();
    public Dictionary<string, TimeSpan> StepExecutionTimes { get; set; } = new();

    // メソッド
    public void AddStepError(string step, string error);
    public void RecordStepTime(string step, TimeSpan elapsed);
    public double GetStepSuccessRate();
}
```

#### 統合テスト用設定
```csharp
// 統合テスト専用設定
public class IntegrationTestConfig
{
    // TCP接続設定
    public string TestServerAddress { get; set; } = "127.0.0.1";
    public int TestServerPort { get; set; } = 5010;
    public int MaxConcurrentConnections { get; set; } = 1;

    // タイムアウト設定（統合テスト用長め設定）
    public int ConnectTimeoutMs { get; set; } = 10000;  // 10秒
    public int SendTimeoutMs { get; set; } = 5000;      // 5秒
    public int ReceiveTimeoutMs { get; set; } = 5000;   // 5秒
    public int DisconnectTimeoutMs { get; set; } = 3000; // 3秒

    // 統合テスト制御
    public bool EnableDetailedLogging { get; set; } = true;
    public bool RecordPerformanceMetrics { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
}
```

### MockPlcServer TCP対応
```csharp
// TCP対応MockPLCサーバー
public class MockPlcServerTcp
{
    private TcpListener _tcpListener;
    private readonly Dictionary<string, byte[]> _responseMap = new();

    public void StartTcpServer(string ipAddress, int port)
    {
        _tcpListener = new TcpListener(IPAddress.Parse(ipAddress), port);
        _tcpListener.Start();

        Task.Run(AcceptTcpConnections);
    }

    private async Task AcceptTcpConnections()
    {
        while (true)
        {
            var tcpClient = await _tcpListener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleTcpClient(tcpClient));
        }
    }

    private async Task HandleTcpClient(TcpClient client)
    {
        using var networkStream = client.GetStream();
        var buffer = new byte[1024];

        while (client.Connected)
        {
            var bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0) break;

            // SLMP応答生成・送信
            var request = buffer[0..bytesRead];
            var response = GenerateSlmpResponse(request);
            await networkStream.WriteAsync(response, 0, response.Length);
        }

        client.Close();
    }
}
```

---

## エラーハンドリング詳細

### スロー例外
- **ConnectionException**: TCP接続エラー
  - 接続タイムアウト
  - 接続拒否（サーバー未起動）
  - ネットワーク到達不可
- **SendException**: 送信エラー
  - 送信タイムアウト
  - 接続切断による送信失敗
  - バッファオーバーフロー
- **ReceiveException**: 受信エラー
  - 受信タイムアウト
  - 不正なSLMP応答
  - データ破損
- **DisconnectException**: 切断エラー
  - 切断タイムアウト
  - 強制切断失敗

### エラーメッセージ統一
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    // TCP統合テストエラー
    public const string TcpConnectionFailed = "TCP接続に失敗しました: {0}";
    public const string TcpSendFailed = "TCP送信に失敗しました: {0}";
    public const string TcpReceiveFailed = "TCP受信に失敗しました: {0}";
    public const string TcpDisconnectFailed = "TCP切断に失敗しました: {0}";

    // サイクル実行エラー
    public const string CycleExecutionFailed = "完全サイクル実行に失敗しました: {0}";
    public const string StepExecutionFailed = "ステップ{0}の実行に失敗しました: {1}";
    public const string MockServerNotReady = "MockPLCサーバーが準備できていません。";

    // 統合テスト環境エラー
    public const string IntegrationTestConfigMissing = "統合テスト設定が見つかりません。";
    public const string TestServerStartFailed = "テストサーバーの起動に失敗しました: {0}";
}
```

### エラー分類と対処方針
```csharp
// 統合テストエラー分類
public enum IntegrationTestErrorType
{
    EnvironmentSetupError,    // 環境設定エラー → テスト停止
    ConnectionError,          // 接続エラー → リトライ後停止
    CommunicationError,       // 通信エラー → リトライ後停止
    DataValidationError,      // データ検証エラー → テスト失敗
    PerformanceError          // パフォーマンスエラー → 警告
}

// エラー対処方針
private void HandleIntegrationTestError(IntegrationTestErrorType errorType, Exception ex)
{
    switch (errorType)
    {
        case IntegrationTestErrorType.ConnectionError:
            _logger.LogWarning("接続エラー（リトライ実行）: {Error}", ex.Message);
            // リトライロジック実行
            break;

        case IntegrationTestErrorType.EnvironmentSetupError:
            _logger.LogError("環境設定エラー（テスト停止）: {Error}", ex.Message);
            throw new InvalidOperationException($"統合テスト環境エラー: {ex.Message}", ex);

        // その他のエラー処理...
    }
}
```

---

## 統合テスト特有の検証項目

### Phase 2連続動作確認
1. **ステップ連続性**: Step3→4→5が中断なく実行される
2. **状態遷移**: 各ステップ間での適切な状態遷移
3. **リソース管理**: TCP接続リソースの適切な管理
4. **エラー伝播**: 途中ステップでのエラー時の適切な処理

### TCP特有検証
- **3-wayハンドシェイク**: 接続確立時の正確なハンドシェイク
- **ストリーム送信**: TCPストリームでの正確なデータ送信
- **バッファリング**: 受信バッファの適切な処理
- **FIN/ACK**: 切断時の適切なシーケンス

### パフォーマンス検証
- **レスポンス時間**: 各ステップの実行時間測定
- **スループット**: 連続実行時のスループット測定
- **リソース使用量**: CPU・メモリ使用量の監視
- **コネクション効率**: TCP接続の確立・切断効率

---

## 実装記録・ドキュメント作成要件

### 必須作業項目

#### 1. 進捗記録開始
**ファイル**: `documents/implementation_records/progress_notes/2025-11-06_TC115実装.md`
- 実装開始時刻
- 目標（TC115統合テスト実装完了）
- 実装方針（TCP完全サイクルテスト）
- 進捗状況のリアルタイム更新

#### 2. 実装記録作成
**ファイル**: `documents/implementation_records/method_records/ExecuteStep3to5CycleAsync実装記録.md`
- 実装判断根拠
  - なぜこの統合テスト方式を選択したか
  - 検討した他の方法との比較（個別テスト vs 統合テスト）
  - 技術選択の根拠とトレードオフ（単体テスト分離性 vs 実際動作検証）
- 発生した問題と解決過程
- TCP統合テスト特有の実装詳細

#### 3. テスト結果保存
**ファイル**: `documents/implementation_records/execution_logs/TC115_テスト結果.log`
- 統合テスト結果（成功/失敗、実行時間、カバレッジ）
- 各ステップ（Step3-5）の詳細実行結果
- TCP接続・通信・切断の詳細ログ
- Red-Green-Refactorの各フェーズ結果
- パフォーマンステスト結果（実行時間、メモリ使用量、リソース使用量）
- エラーログとデバッグ情報

---

## 完了条件

以下すべてが満たされた時点で実装完了とする：

### 機能的完了条件
- [ ] TC115統合テストがパス
- [ ] ExecuteStep3to5CycleAsync本体実装完了
- [ ] TCP完全サイクル実行機能の実装
- [ ] CycleExecutionResult生成機能の実装
- [ ] 各ステップ結果記録機能の実装

### TCP統合処理完了条件
- [ ] TCP接続確立機能（3-wayハンドシェイク）
- [ ] SLMPフレーム送信機能（ストリーム送信）
- [ ] SLMP応答受信機能（バッファリング受信）
- [ ] TCP接続切断機能（FIN/ACKシーケンス）
- [ ] ステップ間状態遷移の適切な管理

### Phase 2連続動作完了条件
- [ ] Step3→4→5の中断なき連続実行
- [ ] 各ステップ成功時の次ステップ自動進行
- [ ] 途中ステップ失敗時の適切なエラーハンドリング
- [ ] 統計情報の正確な更新（接続回数、送受信回数等）

### 非機能的完了条件
- [ ] エラーハンドリング完了（4種類の例外対応）
- [ ] ログ出力機能完了（4レベル対応）
- [ ] パフォーマンス要件満足（< 2秒）
- [ ] リソース管理要件満足（メモリ < 10MB）

### ドキュメント完了条件
- [ ] 進捗記録作成完了
- [ ] 実装記録作成完了（TCP統合テスト詳細含む）
- [ ] テスト結果ログ保存完了
- [ ] C:\Users\1010821\Desktop\python\andon\documents\design\チェックリスト\integration_test実施リスト.mdの該当項目にチェック

### 品質保証完了条件
- [ ] リファクタリング完了（コード品質向上）
- [ ] テスト再実行でGreen維持確認
- [ ] 単体テスト（TC021等）との整合性確認
- [ ] MockPlcServerとの統合動作確認

---

## ログ出力要件

### LoggingManager連携
- **サイクル開始ログ**: 実行設定（TCP設定、タイムアウト設定）、開始時刻
- **ステップ実行ログ**: 各ステップ（Step3-5）の開始・完了・所要時間
- **TCP通信ログ**: 接続確立、データ送受信、切断の詳細
- **サイクル完了ログ**: 総実行ステップ数、成功ステップ数、総所要時間、成功/失敗
- **エラーログ**: 例外詳細、失敗ステップ、リトライ情報、スタックトレース
- **デバッグログ**: TCP接続詳細、SLMPフレーム内容、パフォーマンス情報

### ログレベル
- **Information**: サイクル開始・完了、各ステップ完了
- **Debug**: TCP通信詳細、SLMPフレーム内容、ステップ間状態遷移
- **Warning**: リトライ実行、パフォーマンス警告、軽微な異常
- **Error**: 例外発生時、サイクル実行失敗時

### ログ出力例
```csharp
_logger.LogInformation("TCP完全サイクル開始: サーバー={ServerAddress}:{Port}, タイムアウト={TotalTimeoutMs}ms",
    config.IpAddress, config.Port,
    timeoutConfig.ConnectTimeoutMs + timeoutConfig.SendTimeoutMs + timeoutConfig.ReceiveTimeoutMs);

_logger.LogDebug("Step3開始: TCP接続確立 → {ServerAddress}:{Port}", config.IpAddress, config.Port);
_logger.LogInformation("Step3完了: TCP接続成功、所要時間={ElapsedMs}ms", connectElapsed.TotalMilliseconds);

_logger.LogDebug("Step4開始: SLMPフレーム送信、データ長={DataLength}バイト", sendFrame.Length);
_logger.LogInformation("Step4完了: 送受信成功、送信={SendMs}ms、受信={ReceiveMs}ms",
    sendElapsed.TotalMilliseconds, receiveElapsed.TotalMilliseconds);

_logger.LogInformation("TCP完全サイクル完了: 総ステップ数={TotalSteps}, 成功={SuccessSteps}, 総所要時間={TotalMs}ms",
    result.TotalStepsExecuted, result.SuccessfulSteps, result.TotalExecutionTime.TotalMilliseconds);
```

---

## 実装時の注意点

### TDD手法厳守
- 必ずテストを先に書く（Red）
- 最小実装でテストをパスさせる（Green）
- リファクタリングで品質向上（Refactor）
- 各フェーズでテスト実行を確認

### 統合テスト特有の注意
- **環境依存性**: TCP接続環境の適切な準備
- **テスト分離**: 他の統合テストとの干渉回避
- **リソース管理**: TCP接続リソースの確実な解放
- **エラーハンドリング**: 途中ステップでの失敗時の適切な後処理

### TCP通信の注意
- **接続タイムアウト**: 適切なタイムアウト設定（統合テスト用長め設定）
- **ストリーム管理**: TCPストリームの適切な読み書き
- **接続状態管理**: 各ステップでの接続状態の確認
- **ポートリソース**: テスト完了後の確実なポート解放

### Phase 2連続動作の注意
- **状態遷移**: Step間での状態の適切な引き継ぎ
- **エラー伝播**: 途中ステップでのエラー時の適切な処理
- **統計更新**: 各ステップ実行結果の正確な記録
- **パフォーマンス**: 連続実行時のパフォーマンス特性の把握

### 記録の重要性
- TCP統合テスト方式選択の根拠を詳細記録
- 各ステップの実行結果を時系列で記録
- テスト結果は実行時間・リソース使用量も含めて記録

### 文字化け対策
- 日本語ファイル名の新規作成時は`.txt`経由で作成
- 作成後は必ずReadツールで確認
- 文字化け発見時は早期に対処

---

## 参考情報

### 設計書参照先
- `documents/design/クラス設計.md` - PlcCommunicationManager詳細仕様
- `documents/design/テスト内容.md` - TC115詳細要件
- `documents/design/エラーハンドリング.md` - 例外処理方針
- `documents/design/ログ機能設計.md` - ログ出力仕様

### 開発手法
- `documents/development_methodology/development-methodology.md` - TDD実装手順

### TCP通信参照
- .NET TcpClient/TcpListener: TCP接続管理
- NetworkStream: ストリーム送受信
- Socket: 低レベルTCP制御
- CancellationToken: 非同期タイムアウト制御

### SLMP仕様書
- `pdf2img/sh080931q.pdf` - SLMP通信プロトコル仕様
- TCP対応SLMP仕様: page_8-12.png
- フレーム構造: page_42-45.png

### 統合テスト参照
- .NET統合テストフレームワーク: ASP.NET Core Test Host
- xUnit統合テスト: IClassFixture, ICollectionFixture
- テストデータ管理: JSON設定ファイル

### MockPlcServer実装参照
- TCP対応MockServerの設計パターン
- 非同期TCP接続処理
- SLMPプロトコル応答生成

### テストデータサンプル
**配置先**: Tests/TestUtilities/TestData/IntegrationTestSamples/
- TcpConnectionConfig.json: TCP接続設定サンプル
- SlmpCommandFrames.bin: SLMP送信フレームサンプル
- ExpectedTcpResponses.bin: 期待されるTCP応答サンプル

### パフォーマンス測定参考
- Stopwatch: 高精度時間測定
- PerformanceCounter: システムリソース監視
- GC.GetTotalMemory: メモリ使用量測定
- Process.GetCurrentProcess: プロセスリソース監視

---

以上の指示に従って、TC115_Step3to5_TCP完全サイクル正常動作統合テストの実装を開始してください。

不明点や不足情報があれば、実装前に質問してください。