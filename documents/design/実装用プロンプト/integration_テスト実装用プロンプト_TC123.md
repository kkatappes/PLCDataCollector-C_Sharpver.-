# TC123_FullCycle_エラー発生時の適切なスキップ統合テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC123（FullCycle_エラー発生時の適切なスキップ）の統合テストケースを、TDD手法に従って実装してください。

---

## 実装概要

### 目的
PlcCommunicationManagerの完全サイクル（Step3→4→5→6）実行中に各段階でエラーが発生した際の
適切なエラーハンドリングとスキップ処理を検証します。
このテストは、エラー発生時に後続処理が正しくスキップされ、
システム状態が適切に維持され、エラー情報が正確に記録されることを確認します。

### 実装対象
- **テストファイル**: `Tests/Integration/Core/Managers/PlcCommunicationManagerIntegrationTests.cs`
- **テスト名前空間**: `andon.Tests.Integration.Core.Managers`
- **テストメソッド名**: `TC123_FullCycle_エラー発生時の適切なスキップ_Step3エラー`, `TC123_FullCycle_エラー発生時の適切なスキップ_Step4エラー`, `TC123_FullCycle_エラー発生時の適切なスキップ_Step5エラー`, `TC123_FullCycle_エラー発生時の適切なスキップ_Step6エラー`

---

## 前提条件の確認

実装開始前に以下を確認してください：

1. **依存ファイルの存在確認**
   - `Core/Managers/PlcCommunicationManager.cs`
   - `Core/Models/AsyncOperationResult.cs`
   - `Core/Exceptions/PlcCommunicationException.cs`
   - `Core/Interfaces/IAsyncExceptionHandler.cs`
   - `Core/Models/ConnectionResponse.cs`

2. **テストユーティリティの確認**
   - `Tests/TestUtilities/Mocks/MockSocket.cs`
   - `Tests/TestUtilities/Exceptions/MockExceptionGenerator.cs`
   - `Tests/TestUtilities/Assertions/ErrorHandlingAssertions.cs`

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

#### Step 1-2: テストケース実装（4つ）

**TC123-1: Step3エラー時の適切なスキップテスト**

**Arrange（準備）**:
- MockSocketを作成（接続失敗をシミュレート）
- ConnectionConfigを作成
- PlcCommunicationManagerインスタンス作成
- 初期システム状態を記録
- エラー発生後の期待状態を定義

**Act（実行）**:
```csharp
AsyncOperationResult<StructuredData> result = null;
Exception caughtException = null;

try
{
    // Step3: Connect（エラー発生）
    var connectResponse = await manager.ConnectAsync(connectionConfig);

    // 以降の処理は実行されない予定
    Assert.True(false, "Step3エラー時は例外がスローされるべき");
}
catch (Exception ex)
{
    caughtException = ex;

    // エラー情報を含むOperationResultを取得
    result = manager.GetLastOperationResult();
}
```

**Assert（検証）**:

*エラーハンドリング検証*:
- 適切な例外タイプがスローされること（PlcConnectionException）
- エラーメッセージが適切であること
- スタックトレースが含まれていること

*スキップ処理検証*:
- Step4-6の処理が実行されていないこと
- システム状態が初期状態のまま維持されていること
- リソース（Socket等）が適切に解放されていること

*エラー記録検証*:
- result.IsSuccess = false
- result.ErrorDetails に適切なエラー情報が記録されていること
- result.FailedStep = "Step3"
- 統計情報にエラーが正しく記録されていること

**TC123-2: Step4エラー時の適切なスキップテスト**

**Arrange（準備）**:
- MockSocketを作成（接続成功、送信失敗をシミュレート）
- 同様の設定

**Act（実行）**:
```csharp
try
{
    // Step3: Connect（成功）
    var connectResponse = await manager.ConnectAsync(connectionConfig);
    Assert.True(connectResponse.IsSuccess);

    // Step4: SendFrame（エラー発生）
    await manager.SendFrameAsync(frameString);

    Assert.True(false, "Step4エラー時は例外がスローされるべき");
}
catch (Exception ex)
{
    caughtException = ex;
    result = manager.GetLastOperationResult();
}
```

**Assert（検証）**:

*部分実行検証*:
- Step3は正常に完了していること
- Step4でエラーが発生していること
- Step5-6の処理がスキップされていること

*リソース管理検証*:
- 接続状態が適切に管理されていること
- Step3で確立した接続が適切に切断されること

*エラー記録検証*:
- result.FailedStep = "Step4"
- Step3の成功情報も含まれていること

**TC123-3: Step5エラー時の適切なスキップテスト**

**Arrange（準備）**:
- MockSocketを作成（接続・送信成功、受信失敗をシミュレート）

**Act（実行）**:
```csharp
try
{
    // Step3: Connect（成功）
    var connectResponse = await manager.ConnectAsync(connectionConfig);

    // Step4: SendFrame（成功）
    await manager.SendFrameAsync(frameString);

    // Step5: ReceiveResponse（エラー発生）
    var rawResponse = await manager.ReceiveResponseAsync();

    Assert.True(false, "Step5エラー時は例外がスローされるべき");
}
catch (Exception ex)
{
    caughtException = ex;
    result = manager.GetLastOperationResult();
}
```

**Assert（検証）**:

*部分実行検証*:
- Step3-4は正常に完了していること
- Step5でエラーが発生していること
- Step6の処理がスキップされていること

*データ状態検証*:
- 送信データは記録されていること
- 受信データは未完了状態であること

**TC123-4: Step6エラー時の適切なスキップテスト**

**Arrange（準備）**:
- 正常なSocket通信設定
- 不正な生レスポンスデータ（Step6でパースエラーが発生）

**Act（実行）**:
```csharp
try
{
    // Step3-5: 正常実行
    var connectResponse = await manager.ConnectAsync(connectionConfig);
    await manager.SendFrameAsync(frameString);
    var rawResponse = await manager.ReceiveResponseAsync();

    // Step6: データ処理（エラー発生）
    var basicProcessed = await manager.ProcessReceivedRawData(rawResponse);
    var processed = await manager.CombineDwordData(basicProcessed, deviceRequestInfos);
    var structured = await manager.ParseRawToStructuredData(processed); // エラー発生点

    Assert.True(false, "Step6エラー時は例外がスローされるべき");
}
catch (Exception ex)
{
    caughtException = ex;
    result = manager.GetLastOperationResult();
}
```

**Assert（検証）**:

*通信部分検証*:
- Step3-5は正常に完了していること
- 生レスポンスデータは取得されていること

*データ処理検証*:
- Step6の途中段階でエラーが発生していること
- 部分的に処理されたデータが適切に管理されていること

*エラー記録検証*:
- result.FailedStep = "Step6"
- Step3-5の成功情報も含まれていること
- データ処理エラーの詳細が記録されていること

#### Step 1-3: テスト実行（Red確認）
```bash
dotnet test --filter "FullyQualifiedName~TC123"
```

期待結果: テスト失敗（エラーハンドリング・スキップ処理が未実装のため）

---

### Phase 2: Green（最小実装）

#### Step 2-1: エラーハンドリング・スキップ処理実装

**AsyncOperationResult拡張**:
```csharp
public class AsyncOperationResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? FailedStep { get; set; }
    public Exception? Exception { get; set; }
    public Dictionary<string, object> StepResults { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;
}
```

**PlcCommunicationManager エラーハンドリング実装**:
```csharp
private AsyncOperationResult<StructuredData> _lastOperationResult = new();

public async Task<StructuredData> ExecuteFullCycleAsync(
    ConnectionConfig config,
    string frameString,
    List<ProcessedDeviceRequestInfo> deviceRequestInfos)
{
    _lastOperationResult = new AsyncOperationResult<StructuredData>
    {
        StartTime = DateTime.Now
    };

    try
    {
        // Step3: Connect
        var connectResponse = await ConnectAsync(config);
        _lastOperationResult.StepResults["Step3"] = connectResponse;

        // Step4: SendFrame
        await SendFrameAsync(frameString);
        _lastOperationResult.StepResults["Step4"] = "Success";

        // Step5: ReceiveResponse
        var rawResponse = await ReceiveResponseAsync();
        _lastOperationResult.StepResults["Step5"] = rawResponse;

        // Step6: データ処理
        var basicProcessed = await ProcessReceivedRawData(rawResponse);
        var processed = await CombineDwordData(basicProcessed, deviceRequestInfos);
        var structured = await ParseRawToStructuredData(processed);
        _lastOperationResult.StepResults["Step6"] = structured;

        _lastOperationResult.IsSuccess = true;
        _lastOperationResult.Data = structured;
        _lastOperationResult.EndTime = DateTime.Now;

        return structured;
    }
    catch (Exception ex)
    {
        _lastOperationResult.IsSuccess = false;
        _lastOperationResult.Exception = ex;
        _lastOperationResult.FailedStep = DetermineFailedStep(ex);
        _lastOperationResult.EndTime = DateTime.Now;

        // リソース解放
        await CleanupResourcesOnError();

        throw;
    }
}

private string DetermineFailedStep(Exception ex)
{
    return ex switch
    {
        PlcConnectionException => "Step3",
        PlcSendException => "Step4",
        PlcReceiveException => "Step5",
        PlcDataProcessingException => "Step6",
        _ => "Unknown"
    };
}

private async Task CleanupResourcesOnError()
{
    try
    {
        if (_socket?.Connected == true)
        {
            await DisconnectAsync();
        }
    }
    catch
    {
        // クリーンアップ失敗は無視
    }
}

public AsyncOperationResult<StructuredData> GetLastOperationResult()
{
    return _lastOperationResult;
}
```

#### Step 2-2: テスト再実行（Green確認）
```bash
dotnet test --filter "FullyQualifiedName~TC123"
```

期待結果: すべてのテストがパス

---

### Phase 3: Refactor（リファクタリング）

#### Step 3-1: コード品質向上
- エラーハンドリングの統一化
  - 例外タイプ別の詳細なエラー情報
  - エラーメッセージの国際化対応
- ログ出力強化
  - エラー発生時の詳細ログ
  - スキップされた処理の記録
  - リソース解放の確認ログ
- パフォーマンス最適化
  - エラー時のリソース解放処理の高速化
- ドキュメントコメント追加

#### Step 3-2: テスト再実行（Green維持確認）
```bash
dotnet test --filter "FullyQualifiedName~TC123"
```

期待結果: すべてのテストがパス（リファクタリング後も）

---

## 技術仕様詳細

### エラータイプ・段階対応表

#### Step3エラー（接続段階）
- **PlcConnectionException**: PLC接続失敗
- **SocketException**: ソケット作成・接続エラー
- **TimeoutException**: 接続タイムアウト
- **スキップ対象**: Step4-6全て

#### Step4エラー（送信段階）
- **PlcSendException**: フレーム送信失敗
- **InvalidFrameException**: 不正フレーム形式
- **SocketException**: 送信時ソケットエラー
- **スキップ対象**: Step5-6

#### Step5エラー（受信段階）
- **PlcReceiveException**: レスポンス受信失敗
- **TimeoutException**: 受信タイムアウト
- **SocketException**: 受信時ソケットエラー
- **スキップ対象**: Step6

#### Step6エラー（データ処理段階）
- **PlcDataProcessingException**: データ処理全般エラー
- **DataParseException**: データ解析エラー
- **DataIntegrityException**: データ整合性エラー
- **スキップ対象**: なし（最終段階）

### リソース解放仕様

#### 接続リソース解放
```csharp
private async Task CleanupResourcesOnError()
{
    // Socket解放
    if (_socket != null)
    {
        try
        {
            if (_socket.Connected)
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
            }
        }
        catch { /* 無視 */ }
        finally
        {
            _socket?.Dispose();
            _socket = null;
        }
    }

    // 接続状態リセット
    _connectionResponse = null;
    _isConnected = false;

    // 統計情報更新
    _connectionStats?.AddError("ResourceCleanup");
}
```

### エラー情報記録仕様

#### AsyncOperationResult詳細情報
- **FailedStep**: エラー発生段階の特定
- **Exception**: 元の例外オブジェクト
- **StepResults**: 各段階の実行結果（成功分も含む）
- **Duration**: 開始からエラー発生までの時間
- **ResourcesReleased**: リソース解放完了フラグ

---

## モック・テストデータ実装

### MockExceptionGenerator
```csharp
public class MockExceptionGenerator
{
    public static PlcConnectionException CreateStep3Error()
    {
        return new PlcConnectionException("接続失敗: PLCが応答しません");
    }

    public static PlcSendException CreateStep4Error()
    {
        return new PlcSendException("送信失敗: ソケットが切断されました");
    }

    public static PlcReceiveException CreateStep5Error()
    {
        return new PlcReceiveException("受信失敗: タイムアウトが発生しました");
    }

    public static PlcDataProcessingException CreateStep6Error()
    {
        return new PlcDataProcessingException("データ処理失敗: 不正なレスポンス形式です");
    }
}
```

### ErrorHandlingAssertions
```csharp
public static class ErrorHandlingAssertions
{
    public static void AssertErrorSkipBehavior(
        AsyncOperationResult<StructuredData> result,
        string expectedFailedStep,
        Type expectedExceptionType)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(expectedFailedStep, result.FailedStep);
        Assert.IsType(expectedExceptionType, result.Exception);

        // 後続ステップが実行されていないことを確認
        var stepNumber = int.Parse(expectedFailedStep.Replace("Step", ""));
        for (int i = stepNumber + 1; i <= 6; i++)
        {
            Assert.False(result.StepResults.ContainsKey($"Step{i}"));
        }
    }

    public static void AssertResourceCleanup(PlcCommunicationManager manager)
    {
        Assert.False(manager.IsConnected);
        Assert.Null(manager.GetCurrentSocket());
    }
}
```

---

## 実装記録・ドキュメント作成要件

### 必須作業項目

#### 1. 進捗記録開始
**ファイル**: `documents/implementation_records/progress_notes/2025-11-06_TC123実装.md`
- 実装開始時刻
- 目標（TC123エラーハンドリング・スキップ処理テスト実装完了）
- 実装方針（段階別エラーハンドリング戦略）

#### 2. 実装記録作成
**ファイル**: `documents/implementation_records/method_records/エラーハンドリング実装記録.md`
- エラータイプ分類の設計判断
- リソース解放戦略の選択根拠
- スキップ処理の実装方針
- 発生した問題と解決過程

#### 3. テスト結果保存
**ファイル**: `documents/implementation_records/execution_logs/TC123_エラーハンドリングテスト結果.log`
- 各段階でのエラー発生テスト結果
- リソース解放確認結果
- パフォーマンス影響測定結果
- エラーログとデバッグ情報

---

## 完了条件

以下すべてが満たされた時点で実装完了とする：

- [ ] TC123-1（Step3エラー時スキップ）テストがパス
- [ ] TC123-2（Step4エラー時スキップ）テストがパス
- [ ] TC123-3（Step5エラー時スキップ）テストがパス
- [ ] TC123-4（Step6エラー時スキップ）テストがパス
- [ ] AsyncOperationResult拡張実装完了
- [ ] エラーハンドリング・スキップ処理実装完了
- [ ] リソース解放機能実装完了
- [ ] リファクタリング完了（ログ出力、例外統一化等）
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

### エラーハンドリングの重要性
- 各段階で発生可能なエラーを網羅
- リソースリークの防止
- 例外情報の詳細な記録
- 再現可能なエラー状態の維持

### スキップ処理の正確性
- 後続処理が確実に実行されないことの確認
- 部分実行結果の適切な管理
- システム状態の一貫性維持

### リソース管理の徹底
- ソケット、メモリ等の確実な解放
- エラー時でも解放処理が実行されることの保証
- ガベージコレクションへの適切な協力

### 記録の重要性
- エラーハンドリング戦略の判断根拠を記録
- テスト結果は例外情報も含めて保存
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

### 既存テスト参照
- `integration_TC121.md`（正常系完全サイクルテスト）
- `integration_TC122.md`（複数サイクル統計テスト）

---

以上の指示に従って、TC123_FullCycle_エラー発生時の適切なスキップ統合テストの実装を開始してください。

不明点や不足情報があれば、実装前に質問してください。