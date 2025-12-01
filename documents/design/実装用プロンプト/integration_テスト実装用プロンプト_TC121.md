# TC121: FullCycle_接続から構造化まで完全実行 テスト実装プロンプト ⭐最重要⭐

## 実装指示

**コード作成を開始してください。**

TC121（FullCycle_接続から構造化まで完全実行）のテストケースを、TDD手法に従って実装してください。

---

## TC121: FullCycle_接続から構造化まで完全実行 ⭐★★★最重要★★★⭐

### 目的
PlcCommunicationManagerの全機能統合テスト
**Step3（接続）→Step4（送受信）→Step5（切断）→Step6（データ処理）の完全サイクル実行**
**19時deadline対応の最小成功基準で最も重要なテスト**

### 前提条件
- 全基本メソッド（ConnectAsync、SendFrameAsync、ReceiveResponseAsync、DisconnectAsync）が実装済み
- 全Step6メソッド（ProcessReceivedRawData、CombineDwordData、ParseRawToStructuredData）が実装済み
- MockPlcServerが完全対応済み
- Step2からのProcessedDeviceRequestInfo提供済み
- 全関連Managerクラス（Logging、Error、Resource）設定済み

### TDD実装手順

#### Arrange（準備）
```csharp
// TCP接続設定
var connectionConfig = new ConnectionConfig
{
    IpAddress = "127.0.0.1",
    Port = 5010,
    Protocol = "TCP",
    DeviceType = "Q00UDPCPU"
};

var timeoutConfig = new TimeoutConfig
{
    ConnectTimeoutMs = 5000,
    SendTimeoutMs = 3000,
    ReceiveTimeoutMs = 3000,
    DisconnectTimeoutMs = 2000
};

// MockPlcServer準備（完全サイクル対応）
var mockPlcServer = new MockPlcServer();
mockPlcServer.StartTcpServer("127.0.0.1", 5010);
mockPlcServer.SetCompleteReadResponse(); // 完全なSLMP応答データ
mockPlcServer.SetDWordCombineTestData(); // DWord結合用テストデータ

// Step2からのリクエスト情報
var processedRequestInfo = new ProcessedDeviceRequestInfo
{
    DeviceType = "D",
    StartAddress = 100,
    Count = 10,
    FrameType = "3E",
    DWordCombineTargets = new List<DWordCombineInfo>
    {
        new DWordCombineInfo { LowWordAddress = 100, HighWordAddress = 101, CombinedName = "D100_32bit" },
        new DWordCombineInfo { LowWordAddress = 102, HighWordAddress = 103, CombinedName = "D102_32bit" }
    },
    ParseConfiguration = new ParseConfiguration
    {
        FrameFormat = "3E",
        DataFormat = "Binary",
        StructureDefinitions = new List<StructureDefinition>
        {
            new StructureDefinition
            {
                Name = "ProductionData",
                Fields = new List<FieldDefinition>
                {
                    new FieldDefinition { Name = "ProductId", Address = 100, DataType = "Int16" },
                    new FieldDefinition { Name = "BatchCount", Address = "D100_32bit", DataType = "Int32" },
                    new FieldDefinition { Name = "QualityStatus", Address = 104, DataType = "Int16" }
                }
            }
        }
    }
};

// PlcCommunicationManagerインスタンス
var manager = new PlcCommunicationManager(mockSocket, logger, errorHandler, resourceManager);

// 送信フレーム準備（Step2からの設定に基づく）
var readCommand = SlmpFrameBuilder.BuildReadCommand("D", 100, 10);
```

#### Act（実行） - 完全サイクル
```csharp
// 完全サイクル実行
var fullCycleResult = await manager.ExecuteFullCycleAsync(
    connectionConfig,
    timeoutConfig,
    readCommand,
    processedRequestInfo,
    CancellationToken.None
);
```

#### Assert（検証） - 全段階検証
```csharp
// 完全サイクル成功検証
Assert.True(fullCycleResult.IsSuccess, $"完全サイクル失敗: {fullCycleResult.ErrorMessage}");
Assert.NotNull(fullCycleResult.CompletedAt);
Assert.True(fullCycleResult.TotalExecutionTime.Value.TotalSeconds < 5.0);

// Step3 接続検証
Assert.True(fullCycleResult.ConnectResult.IsSuccess);
Assert.Equal("TCP", fullCycleResult.ConnectResult.Protocol);

// Step4 送信検証
Assert.True(fullCycleResult.SendResult.IsSuccess);
Assert.True(fullCycleResult.SendResult.SentDataLength > 0);

// Step4 受信検証
Assert.NotNull(fullCycleResult.ReceiveResult.ResponseData);
Assert.True(fullCycleResult.ReceiveResult.ResponseData.Length > 0);

// Step6-1 基本処理検証
Assert.True(fullCycleResult.BasicProcessedData.IsSuccess);
Assert.True(fullCycleResult.BasicProcessedData.ProcessedDevices.Count >= 10);

// Step6-2 DWord結合検証
Assert.True(fullCycleResult.ProcessedData.IsSuccess);
Assert.True(fullCycleResult.ProcessedData.CombinedDWordDevices.Count >= 2);
var combinedD100 = fullCycleResult.ProcessedData.CombinedDWordDevices
    .FirstOrDefault(d => d.DeviceName == "D100_32bit");
Assert.NotNull(combinedD100);
Assert.True(combinedD100.CombinedValue > 0);

// Step6-3 構造化検証（最終出力）
Assert.True(fullCycleResult.StructuredData.IsSuccess);
Assert.True(fullCycleResult.StructuredData.StructuredDevices.Count >= 1);
var productionData = fullCycleResult.StructuredData.StructuredDevices
    .FirstOrDefault(d => d.DeviceName == "ProductionData");
Assert.NotNull(productionData);
Assert.True(productionData.Fields.ContainsKey("ProductId"));
Assert.True(productionData.Fields.ContainsKey("BatchCount"));
Assert.True(productionData.Fields.ContainsKey("QualityStatus"));

// 統計情報検証
Assert.Equal(1, manager.Statistics.TotalFullCycles);
Assert.Equal(1, manager.Statistics.SuccessfulFullCycles);
Assert.True(manager.Statistics.AverageFullCycleTime.TotalSeconds > 0);
```

### メソッド実装仕様

#### ExecuteFullCycleAsync実装
```csharp
public async Task<FullCycleExecutionResult> ExecuteFullCycleAsync(
    ConnectionConfig connectionConfig,
    TimeoutConfig timeoutConfig,
    byte[] sendFrame,
    ProcessedDeviceRequestInfo processedRequestInfo,
    CancellationToken cancellationToken = default)
{
    var fullCycleResult = new FullCycleExecutionResult();
    var startTime = DateTime.UtcNow;

    try
    {
        // Step3: 接続
        fullCycleResult.ConnectResult = await ConnectAsync(connectionConfig, timeoutConfig, cancellationToken);
        if (!fullCycleResult.ConnectResult.IsSuccess)
        {
            fullCycleResult.IsSuccess = false;
            fullCycleResult.ErrorMessage = $"Step3接続失敗: {fullCycleResult.ConnectResult.ErrorMessage}";
            return fullCycleResult;
        }

        // Step4: 送信
        fullCycleResult.SendResult = await SendFrameAsync(sendFrame, cancellationToken);
        if (!fullCycleResult.SendResult.IsSuccess)
        {
            fullCycleResult.IsSuccess = false;
            fullCycleResult.ErrorMessage = $"Step4送信失敗: {fullCycleResult.SendResult.ErrorMessage}";
            return fullCycleResult;
        }

        // Step4: 受信
        fullCycleResult.ReceiveResult = await ReceiveResponseAsync(timeoutConfig.ReceiveTimeoutMs, cancellationToken);
        if (fullCycleResult.ReceiveResult.ResponseData == null)
        {
            fullCycleResult.IsSuccess = false;
            fullCycleResult.ErrorMessage = "Step4受信失敗: 応答データなし";
            return fullCycleResult;
        }

        // Step6-1: 基本処理
        fullCycleResult.BasicProcessedData = await ProcessReceivedRawData(
            fullCycleResult.ReceiveResult.ResponseData,
            processedRequestInfo,
            cancellationToken);

        if (!fullCycleResult.BasicProcessedData.IsSuccess)
        {
            fullCycleResult.IsSuccess = false;
            fullCycleResult.ErrorMessage = $"Step6-1基本処理失敗: {string.Join(", ", fullCycleResult.BasicProcessedData.Errors)}";
            return fullCycleResult;
        }

        // Step6-2: DWord結合
        fullCycleResult.ProcessedData = await CombineDwordData(
            fullCycleResult.BasicProcessedData,
            processedRequestInfo,
            cancellationToken);

        if (!fullCycleResult.ProcessedData.IsSuccess)
        {
            fullCycleResult.IsSuccess = false;
            fullCycleResult.ErrorMessage = $"Step6-2DWord結合失敗: {string.Join(", ", fullCycleResult.ProcessedData.Errors)}";
            return fullCycleResult;
        }

        // Step6-3: 構造化（最終処理）
        fullCycleResult.StructuredData = await ParseRawToStructuredData(
            fullCycleResult.ProcessedData,
            processedRequestInfo,
            cancellationToken);

        if (!fullCycleResult.StructuredData.IsSuccess)
        {
            fullCycleResult.IsSuccess = false;
            fullCycleResult.ErrorMessage = $"Step6-3構造化失敗: {string.Join(", ", fullCycleResult.StructuredData.Errors)}";
            return fullCycleResult;
        }

        // Step5: 切断
        await DisconnectAsync(cancellationToken);

        // 成功
        fullCycleResult.IsSuccess = true;
        fullCycleResult.TotalExecutionTime = DateTime.UtcNow - startTime;
        fullCycleResult.CompletedAt = DateTime.UtcNow;

        // 統計更新
        Statistics.IncrementFullCycle(fullCycleResult.TotalExecutionTime.Value);

        return fullCycleResult;
    }
    catch (Exception ex)
    {
        fullCycleResult.IsSuccess = false;
        fullCycleResult.ErrorMessage = $"完全サイクル例外: {ex.Message}";
        fullCycleResult.TotalExecutionTime = DateTime.UtcNow - startTime;

        // 統計更新（失敗）
        Statistics.IncrementFullCycleError();

        return fullCycleResult;
    }
}
```

### 必要なクラス・データ転送オブジェクト

#### FullCycleExecutionResult（完全サイクル結果）
```csharp
public class FullCycleExecutionResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? TotalExecutionTime { get; set; }

    // 各ステップの結果
    public ConnectionResponse? ConnectResult { get; set; }        // Step3
    public SendResponse? SendResult { get; set; }                // Step4-送信
    public RawResponseData? ReceiveResult { get; set; }          // Step4-受信
    public BasicProcessedResponseData? BasicProcessedData { get; set; }  // Step6-1
    public ProcessedResponseData? ProcessedData { get; set; }    // Step6-2
    public StructuredData? StructuredData { get; set; }          // Step6-3（最終出力）

    // サイクル統計
    public int TotalStepsExecuted { get; set; }
    public int SuccessfulSteps { get; set; }
    public List<string> StepErrors { get; set; } = new List<string>();
    public Dictionary<string, TimeSpan> StepExecutionTimes { get; set; } = new Dictionary<string, TimeSpan>();
}
```

### 期待される完全データフロー
```
Step3: 接続確立 → ConnectionResponse
    ↓
Step4: フレーム送信 → SendResponse
    ↓
Step4: PLC応答受信 → RawResponseData (生バイトデータ)
    ↓
Step6-1: 基本処理 → BasicProcessedResponseData (デバイス値抽出)
    ↓
Step6-2: DWord結合 → ProcessedResponseData (32bit値生成)
    ↓
Step6-3: 構造化 → StructuredData (業務データ構造化) ← 最終出力
    ↓
Step7: データ出力 (別Manager担当)
```

### 完了条件

- [ ] TC121（FullCycle_接続から構造化まで完全実行）テストがパス ⭐**必須**⭐
- [ ] Step3→4→5→6の全段階が順次成功実行される
- [ ] 各段階の中間データが正しく次段階に引き継がれる
- [ ] 最終的にStructuredDataが期待する構造で生成される
- [ ] 業務データ（ProductId、BatchCount等）が正確に抽出される
- [ ] DWord結合が正確に実行される（32bit値計算）
- [ ] 実行時間が適切（< 5秒）である
- [ ] 統計情報が正しく更新される
- [ ] エラー時の段階特定が可能である
- [ ] チェックリストの該当項目にチェック

### 重要ポイント
- **最重要テスト**: TC121成功で「通信→データ取得→処理完了」を実証可能
- **19時deadline**: 最小成功基準の中で最も重要
- **完全統合**: 全Stepの連続動作を一度に検証
- **実機想定**: 実際のPLC環境での動作を模擬
- **データ品質**: 最終構造化データの完全性確認

### パフォーマンス・品質要件
- **実行時間**: 通常環境で3秒以内、最大5秒以内
- **メモリ使用量**: 完全サイクル実行中の増加量 < 50MB
- **データ精度**: 全段階でのデータ変換精度100%
- **エラー情報**: 失敗時の段階・原因の明確な特定

### テストデータ例
```csharp
// 期待する最終構造化結果
var expectedStructuredData = new StructuredDevice
{
    DeviceName = "ProductionData",
    Fields = new Dictionary<string, object>
    {
        ["ProductId"] = 12345,           // D100の値
        ["BatchCount"] = 987654321,      // D100_32bit結合値
        ["QualityStatus"] = 1            // D104の値
    },
    StructureType = "ProductionData",
    ParsedTimestamp = DateTime.Now
};
```

---

以上の指示に従って実装してください。**TC121の成功は19時deadline達成の必須条件です。**