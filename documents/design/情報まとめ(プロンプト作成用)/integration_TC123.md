# Integration FullCycle エラー処理 テスト実装用情報（TC123）

## ドキュメント概要

### 目的
このドキュメントは、TC123_FullCycle_エラー発生時の適切なスキップテストの実装に必要な情報を集約したものです。
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
- `integration_TC121.md` - FullCycle基本実行テスト
- `integration_TC122.md` - FullCycle複数サイクル統計累積テスト
- `step3_TC019.md` - 接続タイムアウトテスト
- `step3_TC023.md` - 未接続状態送信テスト
- `step3_TC026.md` - 受信タイムアウトテスト

#### 実装参考（PySLMPClient）
- `PySLMPClient/pyslmpclient/const.py` - SLMP定数・列挙型定義
- `PySLMPClient/pyslmpclient/__init__.py` - SLMPクライアント実装
- `PySLMPClient/tests/test_main.py` - エラーハンドリングテストケース

---

## 1. テスト対象機能仕様

### FullCycleエラーハンドリング（Step3-6エラー処理）
**統合テスト対象**: PlcCommunicationManagerエラーハンドリング
**名前空間**: andon.Core.Managers

#### エラーハンドリングパターン
```
Step3: 接続エラー → 後続Step4-6をスキップ → エラー情報記録
Step4: 送信エラー → 後続Step5-6をスキップ → 切断処理実行 → エラー情報記録
Step4: 受信エラー → 後続Step6をスキップ → 切断処理実行 → エラー情報記録
Step5: 切断エラー → 後続Step6をスキップ → エラー情報記録（処理継続）
Step6: データ処理エラー → エラー情報記録 → 部分的結果返却
```

#### エラーハンドリングInput
- ConnectionConfig（接続失敗用設定）:
  ```
  IpAddress: "192.168.255.255" (存在しないIP)
  Port: 9999 (閉鎖ポート)
  UseTcp: true
  ConnectionType: "TCP"
  ```
- TimeoutConfig（短時間タイムアウト設定）:
  ```
  ConnectTimeoutMs: 1000 (短時間)
  SendTimeoutMs: 500 (短時間)
  ReceiveTimeoutMs: 500 (短時間)
  ```
- SLMPフレーム（不正フレーム含む）:
  - 正常フレーム: "54001234000000010401006400000090E8030000"
  - 不正フレーム: "INVALID_FRAME_DATA"
- エラー注入設定: 各ステップでのエラー発生シミュレーション

#### エラーハンドリングOutput
- ErrorResponse（エラー応答情報）:
  - ErrorStep（エラー発生ステップ=Step3/Step4/Step5/Step6）
  - ErrorType（エラー種別=Timeout/Connection/Format/Processing）
  - ErrorMessage（詳細エラーメッセージ）
  - SkippedSteps（スキップされたステップリスト）
  - RecoveryAction（実施された復旧処理）
  - PartialResults（部分的処理結果、取得可能な場合）

- ConnectionStats（エラー統計情報）:
  - TotalErrors（合計エラー数）
  - ErrorsByStep（ステップ別エラー数）
  - ErrorsByType（エラー種別別エラー数）
  - SkippedStepCount（スキップされたステップ総数）
  - RecoverySuccessRate（復旧処理成功率）

#### 機能
- エラー発生ステップの特定・記録
- 後続ステップの適切なスキップ判定
- リソース解放処理（ソケット切断等）
- エラー情報の詳細記録・統計更新
- 部分的結果の保存・返却
- 復旧処理の実行・検証

#### データ取得元
- ConfigToFrameManager.LoadConfigAsync()（エラー注入設定）
- ErrorHandler.HandleError()（エラー処理）
- LoggingManager.LogError()（エラーログ）

---

## 2. テストケース仕様（TC123）

### TC123_FullCycle_エラー発生時の適切なスキップ
**目的**: Step3-6のエラー発生時の適切なスキップ・復旧処理を統合テスト

#### 前提条件
- ErrorHandlerが初期化済み
- LoggingManagerが動作可能状態
- モックによるエラー注入が可能な状態
- リソース監視機能が有効

#### 入力データ
**Step3エラーケース（接続エラー）**:
- ConnectionConfig:
  ```
  IpAddress: "192.168.255.255"  // 存在しないアドレス
  Port: 9999                     // 閉鎖ポート
  ConnectTimeoutMs: 1000         // 短時間タイムアウト
  ```
- 期待動作: Step4-6すべてをスキップ

**Step4エラーケース（送信エラー）**:
- ConnectionConfig: 正常設定
- SLMPフレーム: "INVALID_FRAME_FORMAT"
- 期待動作: Step6をスキップ、Step5切断は実行

**Step4エラーケース（受信タイムアウト）**:
- ConnectionConfig: 正常設定
- TimeoutConfig.ReceiveTimeoutMs: 100  // 極短時間
- 期待動作: Step6をスキップ、Step5切断は実行

**Step5エラーケース（切断エラー）**:
- 切断時にソケットエラー発生
- 期待動作: Step6をスキップ、エラー記録後処理継続

**Step6エラーケース（データ処理エラー）**:
- 不正な受信データ形式
- 期待動作: 部分的結果を返却、エラー詳細記録

#### 期待出力
**Step3エラー時の出力**:
- ErrorResponse:
  ```
  ErrorStep: "Step3"
  ErrorType: "ConnectionTimeout"
  ErrorMessage: "PLCへの接続がタイムアウトしました（制限時間: 1000ms）"
  SkippedSteps: ["Step4", "Step5", "Step6"]
  RecoveryAction: "ConnectionAborted"
  PartialResults: null
  ```

**Step4エラー時の出力**:
- ErrorResponse:
  ```
  ErrorStep: "Step4"
  ErrorType: "FormatException"
  ErrorMessage: "不正なSLMPフレーム形式です: INVALID_FRAME_FORMAT"
  SkippedSteps: ["Step6"]
  RecoveryAction: "DisconnectionExecuted"
  PartialResults: null
  ```

**Step6エラー時の出力**:
- ErrorResponse:
  ```
  ErrorStep: "Step6"
  ErrorType: "ProcessingError"
  ErrorMessage: "データ解析中にエラーが発生しました: 不正なデータ形式"
  SkippedSteps: []
  RecoveryAction: "PartialResultsSaved"
  PartialResults: ProcessedResponseData (部分データ)
  ```

#### エラー統計要件
- ErrorsByStep: Step3=1, Step4=2, Step5=1, Step6=1の適切な集計
- SkippedStepCount: 各ケースでスキップされたステップ数の正確な記録
- RecoverySuccessRate: 復旧処理成功率の正確な計算

---

## 3. エラーハンドリングフロー詳細

### Step3エラーハンドリング
```csharp
try
{
    var connectResult = await plcManager.ConnectAsync(connectionConfig, timeoutConfig);
    // 成功時は次のステップへ
}
catch (TimeoutException ex)
{
    // Step4-6をスキップ
    errorResponse.ErrorStep = "Step3";
    errorResponse.ErrorType = "ConnectionTimeout";
    errorResponse.SkippedSteps = ["Step4", "Step5", "Step6"];
    errorResponse.RecoveryAction = "ConnectionAborted";

    // ログ記録・統計更新
    LogError(ex, "Step3");
    UpdateErrorStats("Step3", "Timeout");

    return errorResponse;
}
```

### Step4エラーハンドリング
```csharp
try
{
    await plcManager.SendFrameAsync(frame);
    var response = await plcManager.ReceiveResponseAsync(timeoutConfig);
    // 成功時はStep5へ
}
catch (Exception ex)
{
    // Step6をスキップ、Step5は実行
    errorResponse.ErrorStep = "Step4";
    errorResponse.SkippedSteps = ["Step6"];

    try
    {
        // 切断処理は実行
        await plcManager.DisconnectAsync();
        errorResponse.RecoveryAction = "DisconnectionExecuted";
    }
    catch (Exception disconnectEx)
    {
        errorResponse.RecoveryAction = "DisconnectionFailed";
        LogError(disconnectEx, "Step5Recovery");
    }

    return errorResponse;
}
```

### Step6エラーハンドリング
```csharp
try
{
    var processedData = await plcManager.ProcessReceivedRawData(rawData);
    var combinedData = await plcManager.CombineDwordData(processedData);
    var structuredData = await plcManager.ParseRawToStructuredData(combinedData);
    // 全処理成功
}
catch (Exception ex)
{
    // 部分的結果の保存・返却
    errorResponse.ErrorStep = "Step6";
    errorResponse.ErrorType = "ProcessingError";
    errorResponse.RecoveryAction = "PartialResultsSaved";

    // 部分的に処理できたデータがあれば保存
    if (processedData != null)
    {
        errorResponse.PartialResults = processedData;
    }

    return errorResponse;
}
```

---

## 4. 依存クラス・設定

### ErrorResponse（エラー応答情報）
**新規作成**: Core/Models/ErrorResponse.cs

```csharp
public class ErrorResponse
{
    public string ErrorStep { get; set; }  // "Step3", "Step4", "Step5", "Step6"
    public string ErrorType { get; set; }  // "Timeout", "Connection", "Format", "Processing"
    public string ErrorMessage { get; set; }
    public List<string> SkippedSteps { get; set; } = new List<string>();
    public string RecoveryAction { get; set; }  // "ConnectionAborted", "DisconnectionExecuted", etc.
    public object? PartialResults { get; set; }
    public DateTime ErrorOccurredAt { get; set; } = DateTime.Now;
    public TimeSpan ErrorHandlingTime { get; set; }
}
```

### ConnectionStats（エラー統計拡張）
**拡張対象**: 既存ConnectionStatsクラス

```csharp
public class ConnectionStats
{
    // 既存プロパティ...

    // エラー統計（新規追加）
    public int TotalErrors { get; set; }
    public Dictionary<string, int> ErrorsByStep { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, int> ErrorsByType { get; set; } = new Dictionary<string, int>();
    public int SkippedStepCount { get; set; }
    public double RecoverySuccessRate { get; set; }

    // エラー統計メソッド
    public void AddError(string step, string errorType)
    {
        TotalErrors++;
        ErrorsByStep[step] = ErrorsByStep.GetValueOrDefault(step, 0) + 1;
        ErrorsByType[errorType] = ErrorsByType.GetValueOrDefault(errorType, 0) + 1;
    }

    public void AddSkippedSteps(int count)
    {
        SkippedStepCount += count;
    }

    public void UpdateRecoveryRate(bool recoverySuccess)
    {
        // 復旧処理成功率の更新ロジック
    }
}
```

### ErrorHandler（エラー処理ハンドラー）
**拡張対象**: 既存ErrorHandlerクラス

```csharp
public interface IErrorHandler
{
    Task<ErrorResponse> HandleStepError(string step, Exception exception);
    Task<List<string>> DetermineSkippedSteps(string errorStep);
    Task<bool> ExecuteRecovery(string errorStep, IPlcCommunicationManager plcManager);
    Task LogErrorDetails(ErrorResponse errorResponse);
}
```

---

## 5. テスト実装方針（TDD）

### 開発手法
- C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.mdに記載のTDD手法を使用

### テストファイル配置
- **ファイル名**: PlcCommunicationManagerErrorHandlingTests.cs
- **配置先**: Tests/Integration/Core/Managers/
- **名前空間**: andon.Tests.Integration.Core.Managers

### テスト実装順序
1. **TC123_FullCycle_エラー発生時の適切なスキップ**（最優先）
   - Step3エラーハンドリングテスト
   - Step4エラーハンドリングテスト
   - Step5エラーハンドリングテスト
   - Step6エラーハンドリングテスト
2. エラー統計テスト（次フェーズ）
   - エラー集計精度テスト
   - 復旧処理成功率テスト

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockPlcCommunicationManager**: エラー注入対応PlcCommunicationManager
- **MockErrorHandler**: エラー処理のモック
- **MockSocket**: 各種ネットワークエラーシミュレーション

#### 使用するスタブ
- **ErrorInjectionStubs**: 各ステップでのエラー注入
- **RecoveryStubs**: 復旧処理シミュレーション
- **PartialDataStubs**: 部分的処理結果のスタブ

---

## 6. テストケース実装構造

### Arrange（準備）
1. エラー注入用モックの準備
   ```csharp
   var mockSocket = new MockSocket();
   mockSocket.SetupConnectionFailure("192.168.255.255", 9999);
   mockSocket.SetupSendFailure("INVALID_FRAME_FORMAT");
   mockSocket.SetupReceiveTimeout(100);
   ```
2. ErrorHandler・LoggingManagerの準備
3. PlcCommunicationManagerインスタンス作成（エラーハンドリング有効）
4. 期待エラー応答の準備

### Act（実行）
1. 各ステップでのエラー注入テスト
   ```csharp
   // Step3エラーケース
   var step3ErrorResult = await ExecuteFullCycleWithStep3Error(plcManager);

   // Step4エラーケース
   var step4ErrorResult = await ExecuteFullCycleWithStep4Error(plcManager);

   // その他エラーケース...
   ```

### Assert（検証）
1. エラー応答の検証
   ```csharp
   Assert.Equal("Step3", step3ErrorResult.ErrorStep);
   Assert.Equal("ConnectionTimeout", step3ErrorResult.ErrorType);
   Assert.Contains("Step4", step3ErrorResult.SkippedSteps);
   Assert.Contains("Step5", step3ErrorResult.SkippedSteps);
   Assert.Contains("Step6", step3ErrorResult.SkippedSteps);
   ```
2. スキップステップの検証
3. 復旧処理の検証
4. エラー統計の検証
5. リソース解放の検証

---

## 7. DIコンテナ設定

### サービスライフタイム
- **PlcCommunicationManager**: Transient（エラー処理独立）
- **ErrorHandler**: Singleton（エラー処理共有）
- **ErrorResponse**: Transient（エラー毎独立）
- **LoggingManager**: Singleton（共有リソース）

### インターフェース登録
```csharp
services.AddTransient<IPlcCommunicationManager, PlcCommunicationManager>();
services.AddSingleton<IErrorHandler, ErrorHandler>();
services.AddTransient<ErrorResponse>();
services.AddSingleton<ILoggingManager, LoggingManager>();
```

---

## 8. エラーハンドリング

### エラー分類体系
- **ConnectionError**: 接続関連エラー（Step3）
- **CommunicationError**: 通信関連エラー（Step4）
- **DisconnectionError**: 切断関連エラー（Step5）
- **ProcessingError**: データ処理関連エラー（Step6）

### エラーメッセージ統一
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    // Step3エラーメッセージ
    public const string ConnectionTimeout = "PLCへの接続がタイムアウトしました（制限時間: {0}ms）";
    public const string ConnectionRefused = "PLCが接続を拒否しました（アドレス: {0}:{1}）";

    // Step4エラーメッセージ
    public const string SendFrameInvalid = "不正なSLMPフレーム形式です: {0}";
    public const string ReceiveTimeout = "PLCからのデータ受信がタイムアウトしました（制限時間: {0}ms）";

    // Step5エラーメッセージ
    public const string DisconnectionFailed = "PLC切断処理に失敗しました: {0}";

    // Step6エラーメッセージ
    public const string ProcessingFailed = "データ解析中にエラーが発生しました: {0}";

    // 復旧処理メッセージ
    public const string RecoveryExecuted = "復旧処理を実行しました: {0}";
    public const string RecoveryFailed = "復旧処理に失敗しました: {0}";
}
```

---

## 9. ログ出力要件

### LoggingManager連携
- エラー発生ログ: ステップ、エラー種別、詳細メッセージ
- スキップ実行ログ: スキップされたステップリスト
- 復旧処理ログ: 復旧処理内容、成功/失敗
- 統計更新ログ: エラー統計情報の更新内容

### ログレベル
- **Error**: エラー発生、復旧処理失敗
- **Warning**: 復旧処理実行、ステップスキップ
- **Information**: エラー統計更新

---

## 10. テスト実装チェックリスト

### TC123実装前
- [ ] ErrorResponseクラス作成
- [ ] ConnectionStatsエラー統計プロパティ追加
- [ ] ErrorHandlerインターフェース拡張
- [ ] エラー注入用モック作成
- [ ] エラーメッセージ定数定義

### TC123実装中
- [ ] Arrange: エラー注入モック準備
- [ ] Act: Step3エラーケース実行
- [ ] Act: Step4エラーケース実行
- [ ] Act: Step5エラーケース実行
- [ ] Act: Step6エラーケース実行
- [ ] Assert: エラー応答検証
- [ ] Assert: スキップステップ検証
- [ ] Assert: 復旧処理検証
- [ ] Assert: エラー統計検証

### TC123実装後
- [ ] テスト実行・Red確認
- [ ] エラーハンドリング機能実装（最小実装）
- [ ] スキップ判定ロジック実装
- [ ] 復旧処理機能実装
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施（エラー処理最適化）

---

## 11. エラー回復戦略

### Step3エラー回復
- 接続タイムアウト: 設定確認要求、ネットワーク診断実行
- 接続拒否: IPアドレス確認要求、PLC状態確認

### Step4エラー回復
- 送信エラー: 接続状態確認、再接続試行
- 受信タイムアウト: タイムアウト時間延長、リトライ実行

### Step5エラー回復
- 切断エラー: 強制切断実行、リソース解放確認

### Step6エラー回復
- 処理エラー: 部分データ保存、エラー詳細記録、次回処理継続

---

## 12. パフォーマンス影響分析

### エラーハンドリングオーバーヘッド
- エラー検出時間: 平均5-10ms
- 復旧処理時間: 平均50-200ms
- ログ出力時間: 平均1-5ms
- 統計更新時間: 平均1-3ms

### リソース使用量
- エラー情報保存メモリ: 平均1-5KB
- ログファイル使用量: エラー1件あたり500-2000バイト
- CPU使用率増加: エラー処理時+5-15%

### 制限・閾値
- 連続エラー上限: 10回（システム停止判定）
- エラー統計保持期間: 24時間
- ログファイル最大サイズ: 100MB

---

以上が TC123_FullCycle_エラー発生時の適切なスキップテスト実装に必要な情報です。