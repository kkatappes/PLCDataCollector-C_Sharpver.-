# Integration ErrorPropagation Step3エラー テスト実装用情報（TC124）

## ドキュメント概要

### 目的
このドキュメントは、TC124_ErrorPropagation_Step3エラー時後続スキップテストの実装に必要な情報を集約したものです。
**コード作成時に必要となる技術情報のみ**を記載しており、学習資料や説明的な内容は含みません。

### 情報取得元
本ドキュメントの情報は以下のソースから抽出・統合されています：

#### 設計書（andon/documents/design/）
- `クラス・メソッドリスト.md` - クラス・メソッドの一覧と概要
- `クラス設計.md` - 詳細なクラス設計仕様
- `テスト内容.md` - テストケース仕様
- `プロジェクト構造設計.md` - フォルダ構造・プロジェクト構成
- `依存関係.md` - クラス間の依存関係
- `エラーハンドリング.md` - エラーハンドリング仕様

#### 既存テストケース実装情報
- `integration_TC123.md` - FullCycleエラー処理テスト
- `step3_TC019.md` - 接続タイムアウトテスト
- `step3_TC020.md` - 接続拒否テスト
- `step3_TC020_1.md` - 不正IPアドレステスト
- `step3_TC020_2.md` - 不正ポート番号テスト
- `step3_TC020_3.md` - null入力テスト
- `step3_TC020_4.md` - 既接続状態再接続テスト

#### 実装参考（PySLMPClient）
- `PySLMPClient/pyslmpclient/const.py` - SLMP定数・列挙型定義
- `PySLMPClient/pyslmpclient/__init__.py` - SLMPクライアント実装
- `PySLMPClient/tests/test_main.py` - エラーハンドリングテストケース

---

## 1. テスト対象機能仕様

### Step3エラー伝播（接続エラー→後続スキップ）
**統合テスト対象**: PlcCommunicationManagerエラー伝播機能
**名前空間**: andon.Core.Managers

#### Step3エラー伝播パターン
```
Step3: ConnectAsync() エラー発生
  ↓ (エラー情報記録)
ErrorPropagation: エラー情報を後続ステップに伝播
  ↓ (スキップ判定)
Step4: SendFrameAsync() / ReceiveResponseAsync() → スキップ
  ↓
Step5: DisconnectAsync() → スキップ
  ↓
Step6: ProcessReceivedRawData() / CombineDwordData() / ParseRawToStructuredData() → スキップ
  ↓ (最終結果生成)
ErrorResponse: Step3エラー + 全後続ステップスキップ情報
```

#### Step3エラー種別
- **ConnectionTimeoutException**: 接続タイムアウト
- **ConnectionRefusedException**: 接続拒否
- **NetworkUnreachableException**: ネットワーク到達不可
- **InvalidOperationException**: 不正な操作（既接続状態での再接続等）
- **ArgumentException**: 不正な引数（null、不正IP/ポート等）
- **SocketException**: ソケットレベルエラー

#### エラー伝播Input
- ConnectionConfig（各種エラーパターン）:
  ```
  // タイムアウトエラーパターン
  IpAddress: "192.168.1.100" (応答遅延PLC)
  ConnectTimeoutMs: 100 (極短時間)

  // 接続拒否エラーパターン
  IpAddress: "192.168.1.200" (接続拒否PLC)
  Port: 5007

  // ネットワーク到達不可エラーパターン
  IpAddress: "10.0.0.1" (到達不可ネットワーク)

  // 不正引数エラーパターン
  IpAddress: null / "" / "invalid_ip"
  Port: -1 / 0 / 70000
  ```

#### エラー伝播Output
- ErrorPropagationResult（エラー伝播結果）:
  - OriginalError（元のStep3エラー情報）
  - PropagatedToSteps（エラーが伝播されたステップリスト=["Step4", "Step5", "Step6"]）
  - SkipReasons（各ステップのスキップ理由）
  - ErrorPropagationTime（エラー伝播処理時間）
  - FinalStatus（最終的な処理ステータス="Step3Failed_AllSubsequentSkipped"）

- ConnectionStats（エラー伝播統計）:
  - Step3ErrorCount（Step3エラー発生数）
  - TotalSkippedSteps（Step3エラーによりスキップされたステップ総数）
  - ErrorPropagationSuccess（エラー伝播処理成功率）
  - Step3ErrorTypes（Step3エラー種別別発生数）

#### 機能
- Step3エラーの詳細分析・分類
- エラー情報の後続ステップへの伝播
- 後続ステップ（Step4-6）の完全スキップ
- エラー原因の詳細記録・統計更新
- リソース状態の安全な初期化
- エラー伝播処理のパフォーマンス監視

#### データ取得元
- PlcCommunicationManager.ConnectAsync()（Step3エラー）
- ErrorHandler.PropagateError()（エラー伝播処理）
- LoggingManager.LogErrorPropagation()（エラー伝播ログ）

---

## 2. テストケース仕様（TC124）

### TC124_ErrorPropagation_Step3エラー時後続スキップ
**目的**: Step3エラー発生時のエラー伝播と後続ステップ完全スキップ機能を統合テスト

#### 前提条件
- ErrorPropagationManagerが初期化済み
- ConnectionStatsが統計収集可能状態
- LoggingManagerがエラー伝播ログ出力可能状態
- リソース監視機能が有効

#### 入力データ
**ConnectionTimeoutエラーパターン**:
- ConnectionConfig:
  ```
  IpAddress: "192.168.1.100"  // 応答遅延PLC
  Port: 5007
  UseTcp: true
  ConnectTimeoutMs: 100       // 極短時間タイムアウト
  ```
- 期待エラー: ConnectionTimeoutException
- 期待スキップ: Step4, Step5, Step6すべて

**ConnectionRefusedエラーパターン**:
- ConnectionConfig:
  ```
  IpAddress: "192.168.1.200"  // 接続拒否PLC
  Port: 5007
  UseTcp: true
  ConnectTimeoutMs: 5000
  ```
- 期待エラー: ConnectionRefusedException
- 期待スキップ: Step4, Step5, Step6すべて

**NetworkUnreachableエラーパターン**:
- ConnectionConfig:
  ```
  IpAddress: "10.0.0.1"       // 到達不可ネットワーク
  Port: 5007
  UseTcp: true
  ConnectTimeoutMs: 3000
  ```
- 期待エラー: NetworkUnreachableException
- 期待スキップ: Step4, Step5, Step6すべて

**InvalidArgumentエラーパターン**:
- ConnectionConfig:
  ```
  IpAddress: null             // null入力
  Port: -1                    // 不正ポート
  UseTcp: true
  ```
- 期待エラー: ArgumentException
- 期待スキップ: Step4, Step5, Step6すべて

#### 期待出力
**ConnectionTimeoutエラー時の出力**:
- ErrorPropagationResult:
  ```
  OriginalError:
    ErrorType: "ConnectionTimeout"
    ErrorMessage: "PLCへの接続がタイムアウトしました（制限時間: 100ms、対象: 192.168.1.100:5007）"
    OccurredAt: DateTime.Now
    Step: "Step3"

  PropagatedToSteps: ["Step4", "Step5", "Step6"]

  SkipReasons:
    "Step4": "Step3接続失敗によりスキップ（送受信処理不要）"
    "Step5": "Step3接続失敗によりスキップ（切断対象なし）"
    "Step6": "Step3接続失敗によりスキップ（処理対象データなし）"

  ErrorPropagationTime: ~5-15ms
  FinalStatus: "Step3Failed_AllSubsequentSkipped"
  ```

**InvalidArgumentエラー時の出力**:
- ErrorPropagationResult:
  ```
  OriginalError:
    ErrorType: "InvalidArgument"
    ErrorMessage: "接続設定に不正な値が指定されています（IPアドレス: null、ポート: -1）"
    OccurredAt: DateTime.Now
    Step: "Step3"

  PropagatedToSteps: ["Step4", "Step5", "Step6"]

  SkipReasons:
    "Step4": "Step3引数エラーによりスキップ（接続設定不正）"
    "Step5": "Step3引数エラーによりスキップ（接続未確立）"
    "Step6": "Step3引数エラーによりスキップ（データ取得不可）"

  ErrorPropagationTime: ~1-5ms
  FinalStatus: "Step3Failed_AllSubsequentSkipped"
  ```

#### エラー伝播統計要件
- Step3ErrorCount: 各エラーパターンでインクリメント
- TotalSkippedSteps: エラー1回につき+3（Step4, Step5, Step6）
- ErrorPropagationSuccess: 100%（エラー伝播処理自体は成功）
- Step3ErrorTypes: エラー種別ごとの正確な集計

---

## 3. エラー伝播フロー詳細

### Step3エラー検出・分析
```csharp
try
{
    var connectionResult = await plcManager.ConnectAsync(connectionConfig, timeoutConfig);
    // 接続成功時は通常フローへ
    return await ExecuteStep4to6(connectionResult);
}
catch (TimeoutException timeoutEx)
{
    // エラー情報詳細分析
    var errorInfo = AnalyzeStep3Error(timeoutEx, "ConnectionTimeout");

    // エラー伝播処理開始
    var propagationResult = await PropagateStep3Error(errorInfo);

    return propagationResult;
}
catch (SocketException socketEx)
{
    var errorInfo = AnalyzeStep3Error(socketEx, "ConnectionRefused");
    var propagationResult = await PropagateStep3Error(errorInfo);
    return propagationResult;
}
// 他のエラーパターンも同様...
```

### Step3エラー伝播処理
```csharp
private async Task<ErrorPropagationResult> PropagateStep3Error(Step3ErrorInfo errorInfo)
{
    var propagationResult = new ErrorPropagationResult
    {
        OriginalError = errorInfo,
        PropagatedToSteps = new List<string> { "Step4", "Step5", "Step6" },
        ErrorPropagationTime = Stopwatch.StartNew()
    };

    // Step4スキップ理由設定
    propagationResult.SkipReasons["Step4"] = GenerateSkipReason("Step4", errorInfo.ErrorType);
    LogStepSkip("Step4", propagationResult.SkipReasons["Step4"]);

    // Step5スキップ理由設定
    propagationResult.SkipReasons["Step5"] = GenerateSkipReason("Step5", errorInfo.ErrorType);
    LogStepSkip("Step5", propagationResult.SkipReasons["Step5"]);

    // Step6スキップ理由設定
    propagationResult.SkipReasons["Step6"] = GenerateSkipReason("Step6", errorInfo.ErrorType);
    LogStepSkip("Step6", propagationResult.SkipReasons["Step6"]);

    // 統計更新
    UpdateErrorPropagationStats(errorInfo.ErrorType, 3);

    // リソース安全初期化
    await SafeResourceCleanup();

    propagationResult.ErrorPropagationTime.Stop();
    propagationResult.FinalStatus = "Step3Failed_AllSubsequentSkipped";

    return propagationResult;
}
```

### スキップ理由生成ロジック
```csharp
private string GenerateSkipReason(string step, string errorType)
{
    return step switch
    {
        "Step4" => errorType switch
        {
            "ConnectionTimeout" => "Step3接続失敗によりスキップ（送受信処理不要）",
            "ConnectionRefused" => "Step3接続失敗によりスキップ（PLC接続不可）",
            "InvalidArgument" => "Step3引数エラーによりスキップ（接続設定不正）",
            _ => "Step3エラーによりスキップ"
        },
        "Step5" => errorType switch
        {
            "ConnectionTimeout" => "Step3接続失敗によりスキップ（切断対象なし）",
            "ConnectionRefused" => "Step3接続失敗によりスキップ（接続未確立）",
            "InvalidArgument" => "Step3引数エラーによりスキップ（接続未確立）",
            _ => "Step3エラーによりスキップ"
        },
        "Step6" => errorType switch
        {
            "ConnectionTimeout" => "Step3接続失敗によりスキップ（処理対象データなし）",
            "ConnectionRefused" => "Step3接続失敗によりスキップ（データ取得不可）",
            "InvalidArgument" => "Step3引数エラーによりスキップ（データ取得不可）",
            _ => "Step3エラーによりスキップ"
        },
        _ => "Step3エラーによりスキップ"
    };
}
```

---

## 4. 依存クラス・設定

### ErrorPropagationResult（エラー伝播結果）
**新規作成**: Core/Models/ErrorPropagationResult.cs

```csharp
public class ErrorPropagationResult
{
    public Step3ErrorInfo OriginalError { get; set; }
    public List<string> PropagatedToSteps { get; set; } = new List<string>();
    public Dictionary<string, string> SkipReasons { get; set; } = new Dictionary<string, string>();
    public Stopwatch ErrorPropagationTime { get; set; }
    public string FinalStatus { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.Now;
}
```

### Step3ErrorInfo（Step3エラー詳細情報）
**新規作成**: Core/Models/Step3ErrorInfo.cs

```csharp
public class Step3ErrorInfo
{
    public string ErrorType { get; set; }         // "ConnectionTimeout", "ConnectionRefused", etc.
    public string ErrorMessage { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.Now;
    public string Step { get; set; } = "Step3";
    public ConnectionConfig AttemptedConnection { get; set; }
    public TimeSpan AttemptDuration { get; set; }
    public Exception OriginalException { get; set; }

    // エラー分析情報
    public bool IsRetryable { get; set; }         // リトライ可能かどうか
    public string DiagnosticInfo { get; set; }    // 診断情報
    public int ErrorSeverity { get; set; }        // エラー重要度（1-5）
}
```

### ErrorPropagationManager（エラー伝播管理）
**新規作成**: Core/Managers/ErrorPropagationManager.cs

```csharp
public interface IErrorPropagationManager
{
    Task<ErrorPropagationResult> PropagateStep3Error(Step3ErrorInfo errorInfo);
    Task<Step3ErrorInfo> AnalyzeStep3Error(Exception exception, string errorType);
    Task<string> GenerateSkipReason(string step, string errorType);
    Task UpdateErrorPropagationStats(string errorType, int skippedStepCount);
    Task SafeResourceCleanup();
}
```

### ConnectionStats（エラー伝播統計拡張）
**拡張対象**: 既存ConnectionStatsクラス

```csharp
public class ConnectionStats
{
    // 既存プロパティ...

    // Step3エラー伝播統計（新規追加）
    public int Step3ErrorCount { get; set; }
    public int TotalSkippedSteps { get; set; }
    public double ErrorPropagationSuccess { get; set; }
    public Dictionary<string, int> Step3ErrorTypes { get; set; } = new Dictionary<string, int>();
    public TimeSpan TotalErrorPropagationTime { get; set; }
    public TimeSpan AverageErrorPropagationTime { get; set; }

    // エラー伝播統計メソッド
    public void AddStep3Error(string errorType, TimeSpan propagationTime)
    {
        Step3ErrorCount++;
        Step3ErrorTypes[errorType] = Step3ErrorTypes.GetValueOrDefault(errorType, 0) + 1;
        TotalErrorPropagationTime = TotalErrorPropagationTime.Add(propagationTime);
        AverageErrorPropagationTime = TimeSpan.FromTicks(TotalErrorPropagationTime.Ticks / Step3ErrorCount);
    }

    public void AddSkippedSteps(int count)
    {
        TotalSkippedSteps += count;
    }
}
```

---

## 5. テスト実装方針（TDD）

### 開発手法
- C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.mdに記載のTDD手法を使用

### テストファイル配置
- **ファイル名**: PlcCommunicationManagerErrorPropagationTests.cs
- **配置先**: Tests/Integration/Core/Managers/
- **名前空間**: andon.Tests.Integration.Core.Managers

### テスト実装順序
1. **TC124_ErrorPropagation_Step3エラー時後続スキップ**（最優先）
   - ConnectionTimeoutエラー伝播テスト
   - ConnectionRefusedエラー伝播テスト
   - NetworkUnreachableエラー伝播テスト
   - InvalidArgumentエラー伝播テスト
2. エラー伝播統計テスト（次フェーズ）
   - エラー種別統計精度テスト
   - 伝播処理パフォーマンステスト

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockPlcCommunicationManager**: Step3エラー注入対応
- **MockErrorPropagationManager**: エラー伝播処理のモック
- **MockSocket**: 各種Step3エラーシミュレーション

#### 使用するスタブ
- **Step3ErrorStubs**: Step3各種エラーパターン
- **ErrorPropagationStubs**: エラー伝播結果のスタブ
- **SkipReasonStubs**: スキップ理由生成のスタブ

---

## 6. テストケース実装構造

### Arrange（準備）
1. Step3エラー注入用モックの準備
   ```csharp
   var mockSocket = new MockSocket();

   // タイムアウトエラー注入
   mockSocket.SetupConnectionTimeout("192.168.1.100", 5007, 100);

   // 接続拒否エラー注入
   mockSocket.SetupConnectionRefused("192.168.1.200", 5007);

   // ネットワーク到達不可エラー注入
   mockSocket.SetupNetworkUnreachable("10.0.0.1", 5007);
   ```

2. ErrorPropagationManagerの準備
3. PlcCommunicationManagerインスタンス作成（エラー伝播機能有効）
4. 期待エラー伝播結果の準備

### Act（実行）
1. 各Step3エラーパターンでの実行
   ```csharp
   // ConnectionTimeoutエラーケース
   var timeoutResult = await ExecuteFullCycleWithConnectionTimeout(plcManager);

   // ConnectionRefusedエラーケース
   var refusedResult = await ExecuteFullCycleWithConnectionRefused(plcManager);

   // NetworkUnreachableエラーケース
   var unreachableResult = await ExecuteFullCycleWithNetworkUnreachable(plcManager);

   // InvalidArgumentエラーケース
   var invalidArgResult = await ExecuteFullCycleWithInvalidArgument(plcManager);
   ```

### Assert（検証）
1. エラー伝播結果の検証
   ```csharp
   // 元のエラー情報検証
   Assert.Equal("ConnectionTimeout", timeoutResult.OriginalError.ErrorType);
   Assert.Equal("Step3", timeoutResult.OriginalError.Step);

   // 伝播ステップ検証
   Assert.Contains("Step4", timeoutResult.PropagatedToSteps);
   Assert.Contains("Step5", timeoutResult.PropagatedToSteps);
   Assert.Contains("Step6", timeoutResult.PropagatedToSteps);
   Assert.Equal(3, timeoutResult.PropagatedToSteps.Count);

   // スキップ理由検証
   Assert.Contains("接続失敗によりスキップ", timeoutResult.SkipReasons["Step4"]);
   Assert.Contains("切断対象なし", timeoutResult.SkipReasons["Step5"]);
   Assert.Contains("処理対象データなし", timeoutResult.SkipReasons["Step6"]);

   // 最終ステータス検証
   Assert.Equal("Step3Failed_AllSubsequentSkipped", timeoutResult.FinalStatus);
   ```

2. エラー伝播統計の検証
   ```csharp
   var stats = plcManager.GetConnectionStats();
   Assert.Equal(4, stats.Step3ErrorCount);  // 4パターン実行
   Assert.Equal(12, stats.TotalSkippedSteps);  // 4エラー × 3ステップスキップ
   Assert.Equal(100.0, stats.ErrorPropagationSuccess);
   ```

3. エラー伝播処理時間の検証
   ```csharp
   Assert.True(timeoutResult.ErrorPropagationTime.TotalMilliseconds >= 1);
   Assert.True(timeoutResult.ErrorPropagationTime.TotalMilliseconds <= 50);
   ```

---

## 7. DIコンテナ設定

### サービスライフタイム
- **PlcCommunicationManager**: Transient（エラー処理独立）
- **ErrorPropagationManager**: Singleton（エラー伝播処理共有）
- **Step3ErrorInfo**: Transient（エラー毎独立）
- **ErrorPropagationResult**: Transient（結果毎独立）

### インターフェース登録
```csharp
services.AddTransient<IPlcCommunicationManager, PlcCommunicationManager>();
services.AddSingleton<IErrorPropagationManager, ErrorPropagationManager>();
services.AddTransient<Step3ErrorInfo>();
services.AddTransient<ErrorPropagationResult>();
```

---

## 8. エラーハンドリング

### Step3エラー詳細分類
- **Severity 5 (Critical)**: SocketException, NetworkUnreachableException
- **Severity 4 (High)**: ConnectionRefusedException
- **Severity 3 (Medium)**: TimeoutException
- **Severity 2 (Low)**: InvalidOperationException
- **Severity 1 (Info)**: ArgumentException

### エラーメッセージ統一
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    // Step3エラーメッセージ
    public const string Step3ConnectionTimeout = "PLCへの接続がタイムアウトしました（制限時間: {0}ms、対象: {1}:{2}）";
    public const string Step3ConnectionRefused = "PLCが接続を拒否しました（対象: {0}:{1}）";
    public const string Step3NetworkUnreachable = "PLCのネットワークに到達できません（对象: {0}:{1}）";
    public const string Step3InvalidArgument = "接続設定に不正な値が指定されています（IPアドレス: {0}、ポート: {1}）";

    // エラー伝播メッセージ
    public const string ErrorPropagationStarted = "Step3エラーの伝播処理を開始します（エラー種別: {0}）";
    public const string StepSkippedDueToStep3Error = "Step{0}をスキップしました（理由: {1}）";
    public const string ErrorPropagationCompleted = "Step3エラー伝播処理が完了しました（処理時間: {0}ms、スキップステップ数: {1}）";
}
```

---

## 9. ログ出力要件

### LoggingManager連携
- Step3エラー発生ログ: エラー種別、詳細メッセージ、接続設定情報
- エラー伝播開始ログ: 伝播対象ステップ、処理開始時刻
- ステップスキップログ: スキップされたステップ、スキップ理由
- エラー伝播完了ログ: 処理時間、統計情報更新内容

### ログレベル
- **Error**: Step3エラー発生
- **Information**: エラー伝播処理開始・完了
- **Debug**: 各ステップスキップ詳細、統計更新詳細

---

## 10. テスト実装チェックリスト

### TC124実装前
- [ ] ErrorPropagationResultクラス作成
- [ ] Step3ErrorInfoクラス作成
- [ ] ErrorPropagationManagerインターフェース・実装作成
- [ ] ConnectionStatsエラー伝播統計プロパティ追加
- [ ] Step3エラー注入用モック作成

### TC124実装中
- [ ] Arrange: Step3エラー注入モック準備
- [ ] Act: ConnectionTimeoutエラーケース実行
- [ ] Act: ConnectionRefusedエラーケース実行
- [ ] Act: NetworkUnreachableエラーケース実行
- [ ] Act: InvalidArgumentエラーケース実行
- [ ] Assert: エラー伝播結果検証
- [ ] Assert: スキップステップ検証
- [ ] Assert: スキップ理由検証
- [ ] Assert: エラー伝播統計検証
- [ ] Assert: エラー伝播処理時間検証

### TC124実装後
- [ ] テスト実行・Red確認
- [ ] ErrorPropagationManager実装（最小実装）
- [ ] Step3エラー分析機能実装
- [ ] エラー伝播処理機能実装
- [ ] スキップ理由生成機能実装
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施（エラー伝播最適化）

---

## 11. パフォーマンス要件

### エラー伝播処理時間制限
- ConnectionTimeout: 5-15ms以内
- ConnectionRefused: 3-10ms以内
- NetworkUnreachable: 10-20ms以内
- InvalidArgument: 1-5ms以内

### メモリ使用量制限
- Step3ErrorInfo: 1KB以下
- ErrorPropagationResult: 2KB以下
- ログ出力: エラー1件あたり500バイト以下

### CPU使用率制限
- エラー伝播処理: CPU使用率+5%以下
- 統計更新: CPU使用率+2%以下

---

## 12. 診断・トラブルシューティング

### Step3エラー診断情報
```csharp
private string GenerateDiagnosticInfo(Exception exception, ConnectionConfig config)
{
    return exception switch
    {
        TimeoutException => $"ネットワーク遅延またはPLC応答遅延の可能性。制限時間: {config.ConnectTimeoutMs}ms",
        SocketException socketEx when socketEx.SocketErrorCode == SocketError.ConnectionRefused =>
            "PLCのSLMP通信機能が無効化されているか、ポート番号が間違っている可能性",
        SocketException socketEx when socketEx.SocketErrorCode == SocketError.NetworkUnreachable =>
            "ネットワーク設定またはルーティング設定に問題がある可能性",
        ArgumentException => "接続設定の入力値チェックが必要",
        _ => "予期しないエラーが発生"
    };
}
```

### リトライ可能判定
```csharp
private bool IsRetryable(Exception exception)
{
    return exception switch
    {
        TimeoutException => true,           // リトライ可能
        SocketException socketEx when socketEx.SocketErrorCode == SocketError.TimedOut => true,
        SocketException socketEx when socketEx.SocketErrorCode == SocketError.ConnectionRefused => false,
        ArgumentException => false,         // リトライ不可
        _ => false
    };
}
```

---

以上が TC124_ErrorPropagation_Step3エラー時後続スキップテスト実装に必要な情報です。