# TC118 Step6 Process→Combine→Parse連続処理統合テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC118（Step6_ProcessToCombinetoParse連続処理）の統合テストケースを、TDD手法に従って実装してください。

---

## TC118: Step6_ProcessToCombinetoParse連続処理

### 目的
Step6の3段階処理（Process→Combine→Parse）の連続実行統合テスト
19時deadline対応のPhase 2: Step6連続処理テスト

### テスト対象処理フロー
```
Step6-1: ProcessReceivedRawData（基本後処理）
  ↓
Step6-2: CombineDwordData（DWord結合処理）
  ↓
Step6-3: ParseRawToStructuredData（構造化データ変換）
```

### 前提条件
- PlcCommunicationManagerクラス実装済み
- Step6単体テスト（TC029, TC032, TC037）完了済み
- RawResponseData（Step4出力）準備済み
- ProcessedDeviceRequestInfo（Step2出力）準備済み

### TDD実装手順

#### Arrange（準備）
```csharp
// 1. RawResponseData準備（Step4の出力を模擬）
var rawResponseData = new RawResponseData
{
    FrameVersion = "4E",
    RawData = "540012340000000000000A00640000000100A8000000010002000300",
    ReceivedAt = DateTime.UtcNow,
    DataLength = 58,
    IsSuccess = true,
    ErrorMessage = null
};

// RawData詳細構造:
// 540012340000000000 - SubHeader（4Eフレーム）: 9バイト（18文字）
// 0000               - EndCode（成功）: 2バイト（4文字）
// 0A00               - データ長（10バイト）: 2バイト（4文字）
// 6400               - M100デバイスコード: 2バイト（4文字）
// 00000100           - M100値（1）: 4バイト（8文字）
// A800               - D200デバイスコード: 2バイト（4文字）
// 00000100           - D200値 下位ワード（1）: 4バイト（8文字）
// 02000300           - D200値 上位ワード（2, 3）: 4バイト（8文字）

// 2. ProcessedDeviceRequestInfo準備（Step2の出力を模擬）
var deviceRequestInfos = new List<ProcessedDeviceRequestInfo>
{
    // M100: Bitデバイス
    new ProcessedDeviceRequestInfo
    {
        DeviceType = "M",
        StartAddress = 100,
        ReadCount = 1,
        DataType = "Bit",
        IsPseudoDword = false,
        DwordSplitInfo = null
    },

    // D200: DWordデバイス（PseudoDword）
    new ProcessedDeviceRequestInfo
    {
        DeviceType = "D",
        StartAddress = 200,
        ReadCount = 1,
        DataType = "DWord",
        IsPseudoDword = true,
        DwordSplitInfo = new DwordSplitInfo
        {
            LowerWordAddress = 200,
            UpperWordAddress = 201,
            RequiresCombination = true
        }
    }
};

// 3. PlcCommunicationManager初期化
var plcCommManager = new PlcCommunicationManager(
    loggingManager,
    errorHandler,
    resourceManager
);
```

#### Act（実行）- Step6連続処理実行
```csharp
// Step6-1: ProcessReceivedRawData（基本後処理）
var basicProcessedData = plcCommManager.ProcessReceivedRawData(
    rawResponseData,
    deviceRequestInfos
);

// Step6-2: CombineDwordData（DWord結合処理）
var processedData = plcCommManager.CombineDwordData(
    basicProcessedData,
    deviceRequestInfos
);

// Step6-3: ParseRawToStructuredData（構造化データ変換）
var structuredData = plcCommManager.ParseRawToStructuredData(
    processedData,
    rawResponseData.FrameVersion
);
```

#### Assert（検証）

##### 1. Step6-1（基本後処理）検証
```csharp
// BasicProcessedResponseData検証
Assert.NotNull(basicProcessedData);
Assert.True(basicProcessedData.IsSuccess);
Assert.NotNull(basicProcessedData.ProcessedAt);
Assert.Equal(3, basicProcessedData.BasicProcessedDevices.Count);

// M100デバイス検証
var m100Basic = basicProcessedData.BasicProcessedDevices
    .FirstOrDefault(d => d.DeviceName == "M100");
Assert.NotNull(m100Basic);
Assert.Equal("M", m100Basic.DeviceType);
Assert.Equal(100, m100Basic.StartAddress);
Assert.Equal("Bit", m100Basic.DataType);
Assert.Equal(1, m100Basic.Value);
Assert.Equal("00000100", m100Basic.RawValue);

// D200デバイス（下位ワード）検証
var d200LowerBasic = basicProcessedData.BasicProcessedDevices
    .FirstOrDefault(d => d.DeviceName == "D200");
Assert.NotNull(d200LowerBasic);
Assert.Equal("D", d200LowerBasic.DeviceType);
Assert.Equal(200, d200LowerBasic.StartAddress);
Assert.Equal("Word", d200LowerBasic.DataType);
Assert.Equal(1, d200LowerBasic.Value);

// D201デバイス（上位ワード）検証
var d201UpperBasic = basicProcessedData.BasicProcessedDevices
    .FirstOrDefault(d => d.DeviceName == "D201");
Assert.NotNull(d201UpperBasic);
Assert.Equal("D", d201UpperBasic.DeviceType);
Assert.Equal(201, d201UpperBasic.StartAddress);
Assert.Equal("Word", d201UpperBasic.DataType);
Assert.Equal(0x00030002, d201UpperBasic.Value);

// エラー・警告検証
Assert.Empty(basicProcessedData.Errors);
Assert.Empty(basicProcessedData.Warnings);
```

##### 2. Step6-2（DWord結合）検証
```csharp
// ProcessedResponseData検証
Assert.NotNull(processedData);
Assert.True(processedData.IsSuccess);
Assert.NotNull(processedData.ProcessedAt);
Assert.Equal(2, processedData.ProcessedDevices.Count);

// M100デバイス検証（変更なし）
var m100Processed = processedData.ProcessedDevices
    .FirstOrDefault(d => d.DeviceName == "M100");
Assert.NotNull(m100Processed);
Assert.Equal("M", m100Processed.DeviceType);
Assert.Equal(100, m100Processed.StartAddress);
Assert.Equal("Bit", m100Processed.DataType);
Assert.Equal(1, m100Processed.Value);

// D200デバイス検証（DWord結合後）
var d200Processed = processedData.ProcessedDevices
    .FirstOrDefault(d => d.DeviceName == "D200");
Assert.NotNull(d200Processed);
Assert.Equal("D", d200Processed.DeviceType);
Assert.Equal(200, d200Processed.StartAddress);
Assert.Equal("DWord", d200Processed.DataType);
Assert.Equal(0x00030002_00000001, d200Processed.Value);

// CombinedDWordDevices検証
Assert.Single(processedData.CombinedDWordDevices);
var d200Combined = processedData.CombinedDWordDevices.First();
Assert.Equal("D200", d200Combined.DeviceName);
Assert.Equal(200, d200Combined.LowerWordAddress);
Assert.Equal(201, d200Combined.UpperWordAddress);
Assert.Equal(1, d200Combined.LowerWordValue);
Assert.Equal(0x00030002, d200Combined.UpperWordValue);
Assert.Equal(0x00030002_00000001, d200Combined.CombinedValue);

// エラー・警告検証
Assert.Empty(processedData.Errors);
Assert.Empty(processedData.Warnings);
```

##### 3. Step6-3（構造化変換）検証
```csharp
// StructuredData検証
Assert.NotNull(structuredData);
Assert.True(structuredData.IsSuccess);
Assert.NotNull(structuredData.ParsedAt);
Assert.Equal("4E", structuredData.FrameVersion);
Assert.Equal(2, structuredData.Devices.Count);

// M100構造化デバイス検証
var m100Structured = structuredData.Devices
    .FirstOrDefault(d => d.DeviceName == "M100");
Assert.NotNull(m100Structured);
Assert.Equal("M", m100Structured.DeviceType);
Assert.Equal(100, m100Structured.StartAddress);
Assert.Equal("Bit", m100Structured.DataType);
Assert.Equal(1, m100Structured.Value);

// D200構造化デバイス検証
var d200Structured = structuredData.Devices
    .FirstOrDefault(d => d.DeviceName == "D200");
Assert.NotNull(d200Structured);
Assert.Equal("D", d200Structured.DeviceType);
Assert.Equal(200, d200Structured.StartAddress);
Assert.Equal("DWord", d200Structured.DataType);
Assert.Equal(0x00030002_00000001, d200Structured.Value);

// ParseSteps検証（3つの解析ステップ記録）
Assert.Equal(3, structuredData.ParseSteps.Count);

var step1 = structuredData.ParseSteps[0];
Assert.Equal("ProcessReceivedRawData", step1.StepName);
Assert.Equal(1, step1.StepNumber);
Assert.True(step1.IsSuccess);
Assert.NotNull(step1.ProcessedAt);
Assert.Null(step1.ErrorMessage);

var step2 = structuredData.ParseSteps[1];
Assert.Equal("CombineDwordData", step2.StepName);
Assert.Equal(2, step2.StepNumber);
Assert.True(step2.IsSuccess);
Assert.NotNull(step2.ProcessedAt);
Assert.Null(step2.ErrorMessage);

var step3 = structuredData.ParseSteps[2];
Assert.Equal("ParseRawToStructuredData", step3.StepName);
Assert.Equal(3, step3.StepNumber);
Assert.True(step3.IsSuccess);
Assert.NotNull(step3.ProcessedAt);
Assert.Null(step3.ErrorMessage);

// エラー・警告検証
Assert.Null(structuredData.ErrorDetails);
```

##### 4. データ伝達整合性検証
```csharp
// M100値の一貫性検証（全ステップで同じ値）
Assert.Equal(m100Basic.Value, m100Processed.Value);
Assert.Equal(m100Processed.Value, m100Structured.Value);

// D200値の変換整合性検証
Assert.Equal(1, d200LowerBasic.Value); // 下位ワード
Assert.Equal(0x00030002, d201UpperBasic.Value); // 上位ワード
Assert.Equal(0x00030002_00000001, d200Processed.Value); // 結合後
Assert.Equal(d200Processed.Value, d200Structured.Value); // 構造化後も同じ

// タイムスタンプ検証（時系列順序）
Assert.True(basicProcessedData.ProcessedAt <= processedData.ProcessedAt);
Assert.True(processedData.ProcessedAt <= structuredData.ParsedAt);
```

### データ伝達フロー詳細

#### M100デバイスのデータ伝達
```
RawData: "6400" "00000100"
  ↓ Step6-1: ProcessReceivedRawData
BasicProcessedDevice: M100, DeviceType="M", Value=1, DataType="Bit"
  ↓ Step6-2: CombineDwordData（変更なし）
ProcessedDevice: M100, DeviceType="M", Value=1, DataType="Bit"
  ↓ Step6-3: ParseRawToStructuredData
StructuredDevice: M100, DeviceType="M", Value=1, DataType="Bit"
```

#### D200デバイスのデータ伝達（DWord結合）
```
RawData: "A800" "00000100" "02000300"
  ↓ Step6-1: ProcessReceivedRawData（分割認識）
BasicProcessedDevice: D200, Value=1, DataType="Word"（下位）
BasicProcessedDevice: D201, Value=0x00030002, DataType="Word"（上位）
  ↓ Step6-2: CombineDwordData（結合）
ProcessedDevice: D200, Value=0x00030002_00000001, DataType="DWord"
CombinedDWordDevice記録: D200 (200+201)
  ↓ Step6-3: ParseRawToStructuredData
StructuredDevice: D200, Value=0x00030002_00000001, DataType="DWord"
```

### DWord結合計算例
```
D200（下位ワード）: 0x00000001 (値=1)
D201（上位ワード）: 0x00030002 (値=3, 2の組)

結合計算:
CombinedValue = (UpperWord << 32) | LowerWord
              = (0x00030002 << 32) | 0x00000001
              = 0x00030002_00000001
```

### 必要なデータ構造

#### RawResponseData（Step4出力）
```csharp
public class RawResponseData
{
    public string FrameVersion { get; set; }       // "4E"
    public string RawData { get; set; }            // 16進数文字列
    public DateTime ReceivedAt { get; set; }
    public int DataLength { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
```

#### BasicProcessedResponseData（Step6-1出力）
```csharp
public class BasicProcessedResponseData
{
    public List<BasicProcessedDevice> BasicProcessedDevices { get; set; }
    public DateTime ProcessedAt { get; set; }
    public bool IsSuccess { get; set; }
    public List<string> Errors { get; set; }
    public List<string> Warnings { get; set; }
}
```

#### ProcessedResponseData（Step6-2出力）
```csharp
public class ProcessedResponseData
{
    public List<ProcessedDevice> ProcessedDevices { get; set; }
    public List<CombinedDWordDevice> CombinedDWordDevices { get; set; }
    public DateTime ProcessedAt { get; set; }
    public bool IsSuccess { get; set; }
    public List<string> Errors { get; set; }
    public List<string> Warnings { get; set; }
}
```

#### StructuredData（Step6-3出力）
```csharp
public class StructuredData
{
    public List<StructuredDevice> Devices { get; set; }
    public List<ParseStep> ParseSteps { get; set; }
    public string FrameVersion { get; set; }
    public DateTime ParsedAt { get; set; }
    public bool IsSuccess { get; set; }
    public ErrorDetails? ErrorDetails { get; set; }
}
```

#### ProcessedDeviceRequestInfo（Step2出力）
```csharp
public class ProcessedDeviceRequestInfo
{
    public string DeviceType { get; set; }
    public int StartAddress { get; set; }
    public int ReadCount { get; set; }
    public string DataType { get; set; }
    public bool IsPseudoDword { get; set; }
    public DwordSplitInfo? DwordSplitInfo { get; set; }
}
```

### エラーハンドリング（TC118では発生しない）

#### Step6-1エラー時
- BasicProcessedResponseData.IsSuccess = false
- 以降のStep6-2, 6-3はスキップ
- エラー情報を BasicProcessedResponseData.Errors に記録
- ParseSteps記録: Step1のみ（IsSuccess=false）

#### Step6-2エラー時
- ProcessedResponseData.IsSuccess = false
- Step6-3はスキップ
- エラー情報を ProcessedResponseData.Errors に記録
- ParseSteps記録: Step1, Step2（Step2の IsSuccess=false）

#### Step6-3エラー時
- StructuredData.IsSuccess = false
- エラー情報を StructuredData.ErrorDetails に記録
- ParseSteps記録: Step1, Step2, Step3（Step3の IsSuccess=false）

### 処理時間目安
- Step6-1（基本後処理）: 5-20ms
- Step6-2（DWord結合）: 3-15ms
- Step6-3（構造化変換）: 5-20ms
- **合計**: 13-55ms（通常30-40ms）

### ログ出力例
```
[Information] Step6-1開始: 基本後処理 - 入力データ長=58文字
[Debug] Step6-1: M100（Bit）値抽出 = 1
[Debug] Step6-1: D200（Word下位）値抽出 = 1
[Debug] Step6-1: D201（Word上位）値抽出 = 0x00030002
[Information] Step6-1完了: 基本処理デバイス数=3
[Information] Step6-2開始: DWord結合処理 - 結合対象=1
[Debug] Step6-2: D200結合 = D200(1) + D201(0x00030002) → 0x00030002_00000001
[Information] Step6-2完了: 結合完了デバイス数=1
[Information] Step6-3開始: 構造化データ変換 - 対象デバイス数=2
[Debug] Step6-3: M100構造化完了
[Debug] Step6-3: D200構造化完了
[Information] Step6-3完了: 構造化デバイス数=2
[Information] Step6連続処理完了: 合計処理時間=50ms, 解析ステップ数=3
```

### 完了条件

- [ ] TC118（Step6_ProcessToCombinetoParse連続処理）テストがパス
- [ ] Step6-1基本後処理が正しく実行され、3つの基本デバイスが生成されることを確認
- [ ] Step6-2 DWord結合処理が正しく実行され、D200が結合されることを確認
- [ ] Step6-3構造化データ変換が正しく実行され、2つの構造化デバイスが生成されることを確認
- [ ] ParseSteps記録が3つのステップで正しく記録されることを確認
- [ ] データ伝達整合性が全ステップで保証されることを確認
- [ ] タイムスタンプが時系列順序で記録されることを確認
- [ ] ログ出力がStep6連続処理の詳細を適切に記録することを確認
- [ ] チェックリストの該当項目にチェック

### 重要ポイント
- **統合テスト**: Step6の3段階処理を連続実行する統合テスト
- **データ整合性**: M100とD200の値が全ステップで一貫していることを確認
- **DWord結合**: PseudoDwordデバイス（D200）の下位・上位ワード結合処理
- **Phase2必須**: 19時deadline対応のPhase2 Step6連続処理テスト
- **TDD準拠**: Red→Green→Refactorサイクルを遵守

---

以上の指示に従って実装してください。