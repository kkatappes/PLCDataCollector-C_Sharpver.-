# Integration Step6 Process→Combine→Parse連続処理 テスト実装用情報（TC118）

## ドキュメント概要

### 目的
このドキュメントは、TC118_Step6_ProcessToCombinetoParse連続処理テストの実装に必要な情報を集約したものです。
**コード作成時に必要となる技術情報のみ**を記載しており、学習資料や説明的な内容は含みません。

### 情報取得元
本ドキュメントの情報は以下のソースから抽出・統合されています：

#### 設計書（andon/documents/design/）
- `クラス・メソッドリスト.md` - クラス・メソッドの一覧と概要
- `クラス設計.md` - 詳細なクラス設計仕様
- `テスト内容.md` - テストケース仕様
- `プロジェクト構造設計.md` - フォルダ構造・プロジェクト構成
- `依存関係.md` - クラス間の依存関係
- `各ステップio.md` - 各ステップのInput/Output詳細

#### 既存テストケース実装情報
- `step6_TC029.md` - ProcessReceivedRawData基本後処理テスト
- `step6_TC032.md` - CombineDwordData DWord結合処理テスト
- `step6_TC037.md` - ParseRawToStructuredData 3Eフレーム解析テスト
- `step6_TC038.md` - ParseRawToStructuredData 4Eフレーム解析テスト
- `integration_TC121.md` - 完全サイクルテスト（対比用）

---

## 1. テスト対象機能仕様

### Step6連続処理（Process→Combine→Parse）
**統合テスト対象**: PlcCommunicationManager（Step6のみ）
**名前空間**: andon.Core.Managers

#### Step6連続処理構成
```
Step6-1: ProcessReceivedRawData（基本後処理）
  ↓
Step6-2: CombineDwordData（DWord結合処理）
  ↓
Step6-3: ParseRawToStructuredData（構造化データ変換）
```

#### 統合Input
- **RawResponseData（Step4からの生データ）**:
  - FrameVersion: "4E"
  - RawData: "540012340000000000000A00640000000100A8000000010002000300" (16進数文字列)
  - ReceivedAt: DateTime.UtcNow
  - DataLength: 58文字（29バイト）

- **ProcessedDeviceRequestInfo（Step2からの機器情報）**:
  - M機器（M100）: DeviceType="M", StartAddress=100, ReadCount=1, DataType="Bit"
  - D機器（D200）: DeviceType="D", StartAddress=200, ReadCount=1, DataType="DWord", IsPseudoDword=true

#### 統合Output
- **StructuredData（構造化データ）**:
  - Devices: 2つの構造化デバイス（M100, D200）
  - ParseSteps: 3つの解析ステップ記録
  - ParsedAt: DateTime.UtcNow
  - IsSuccess: true

#### 機能
- Step6の3段階処理を連続実行
- 各段階のデータ変換の整合性保証
- エラー情報の伝播

---

## 2. テストケース仕様（TC118）

### TC118_Step6_ProcessToCombinetoParse連続処理
**目的**: Step6の3段階処理（Process→Combine→Parse）の連続実行を統合テスト

#### 前提条件
- RawResponseData準備済み（Step4の出力を模擬）
- ProcessedDeviceRequestInfo準備済み（Step2の出力を模擬）
- PlcCommunicationManager初期化済み

#### 入力データ
**RawResponseData（生データ）**:
```csharp
var rawResponseData = new RawResponseData
{
    FrameVersion = "4E",
    RawData = "540012340000000000000A00640000000100A8000000010002000300",
    ReceivedAt = DateTime.UtcNow,
    DataLength = 58,
    IsSuccess = true
};
```

**RawData詳細構造**:
```
54001234000000 - SubHeader（4Eフレーム）: 7バイト（14文字）
0000           - EndCode（成功）: 2バイト（4文字）
0A00           - データ長（10バイト）: 2バイト（4文字）
6400           - M100デバイスコード: 2バイト（4文字）
00000100       - M100値（1）: 4バイト（8文字）
A800           - D200デバイスコード: 2バイト（4文字）
00000100       - D200値 下位ワード（1）: 4バイト（8文字）
02000300       - D200値 上位ワード（2, 3）: 4バイト（8文字）
```

**ProcessedDeviceRequestInfo（機器情報リスト）**:
```csharp
var deviceRequestInfos = new List<ProcessedDeviceRequestInfo>
{
    new ProcessedDeviceRequestInfo
    {
        DeviceType = "M",
        StartAddress = 100,
        ReadCount = 1,
        DataType = "Bit",
        IsPseudoDword = false
    },
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
            UpperWordAddress = 201
        }
    }
};
```

#### 期待出力
**StructuredData（構造化データ）**:
```csharp
Assert.NotNull(structuredData);
Assert.True(structuredData.IsSuccess);
Assert.NotNull(structuredData.ParsedAt);
Assert.Equal(2, structuredData.Devices.Count);
Assert.Equal(3, structuredData.ParseSteps.Count);
```

**構造化デバイス（M100）**:
```csharp
var m100Device = structuredData.Devices.FirstOrDefault(d => d.DeviceName == "M100");
Assert.NotNull(m100Device);
Assert.Equal("M", m100Device.DeviceType);
Assert.Equal(100, m100Device.StartAddress);
Assert.Equal("Bit", m100Device.DataType);
Assert.Equal(1, m100Device.Value);
```

**構造化デバイス（D200）**:
```csharp
var d200Device = structuredData.Devices.FirstOrDefault(d => d.DeviceName == "D200");
Assert.NotNull(d200Device);
Assert.Equal("D", d200Device.DeviceType);
Assert.Equal(200, d200Device.StartAddress);
Assert.Equal("DWord", d200Device.DataType);
Assert.Equal(0x00030002_00000001, d200Device.Value); // DWord結合後の値
```

**解析ステップ記録**:
```csharp
Assert.Equal("ProcessReceivedRawData", structuredData.ParseSteps[0].StepName);
Assert.Equal("CombineDwordData", structuredData.ParseSteps[1].StepName);
Assert.Equal("ParseRawToStructuredData", structuredData.ParseSteps[2].StepName);
Assert.All(structuredData.ParseSteps, step => Assert.True(step.IsSuccess));
```

#### 動作フロー成功条件
1. **Step6-1（基本後処理）成功**:
   - BasicProcessedResponseData生成
   - M100, D200（下位・上位ワード）の基本処理完了
   - エラー・警告なし

2. **Step6-2（DWord結合）成功**:
   - ProcessedResponseData生成
   - D200の下位・上位ワード結合完了
   - CombinedDWordDevices記録

3. **Step6-3（構造化変換）成功**:
   - StructuredData生成
   - 2つの構造化デバイス生成（M100, D200）
   - 3つの解析ステップ記録

---

## 3. Step別詳細仕様

### Step6-1: ProcessReceivedRawData（基本後処理）

#### 詳細情報
**参照**: step6_TC029.md（基本後処理成功テスト）

#### Input
- RawResponseData（生データ）
- ProcessedDeviceRequestInfo（機器情報リスト）

#### Output
- BasicProcessedResponseData:
  - BasicProcessedDevices: 3つ（M100, D200下位, D200上位）
  - ProcessedAt: DateTime.UtcNow
  - IsSuccess: true

#### 検証ポイント
- 生データからのデバイス値抽出成功
- 3つの基本処理デバイス生成
- PseudoDword機器の分割認識（D200 → D200, D201）

#### BasicProcessedResponseData構造
```csharp
// M100デバイス
{
    DeviceName = "M100",
    DeviceType = "M",
    StartAddress = 100,
    DataType = "Bit",
    Value = 1,
    RawValue = "00000100"
}

// D200デバイス（下位ワード）
{
    DeviceName = "D200",
    DeviceType = "D",
    StartAddress = 200,
    DataType = "Word",
    Value = 1,
    RawValue = "00000100"
}

// D201デバイス（上位ワード）
{
    DeviceName = "D201",
    DeviceType = "D",
    StartAddress = 201,
    DataType = "Word",
    Value = 2,
    RawValue = "02000300"
}
```

---

### Step6-2: CombineDwordData（DWord結合処理）

#### 詳細情報
**参照**: step6_TC032.md（DWord結合処理成功テスト）

#### Input
- BasicProcessedResponseData（基本処理データ）
- ProcessedDeviceRequestInfo（DWord情報）

#### Output
- ProcessedResponseData:
  - ProcessedDevices: 2つ（M100, D200結合後）
  - CombinedDWordDevices: 1つ（D200）
  - ProcessedAt: DateTime.UtcNow
  - IsSuccess: true

#### 検証ポイント
- D200の下位・上位ワード結合成功
- DWord結合情報記録
- M100デバイスは変更なし（結合不要）

#### ProcessedResponseData構造
```csharp
// M100デバイス（変更なし）
{
    DeviceName = "M100",
    DeviceType = "M",
    StartAddress = 100,
    DataType = "Bit",
    Value = 1
}

// D200デバイス（DWord結合後）
{
    DeviceName = "D200",
    DeviceType = "D",
    StartAddress = 200,
    DataType = "DWord",
    Value = 0x00030002_00000001, // 下位1 + 上位(2,3)
    RawValue = "00000100_02000300"
}

// CombinedDWordDevices記録
{
    DeviceName = "D200",
    LowerWordAddress = 200,
    UpperWordAddress = 201,
    LowerWordValue = 1,
    UpperWordValue = 0x00030002,
    CombinedValue = 0x00030002_00000001
}
```

---

### Step6-3: ParseRawToStructuredData（構造化データ変換）

#### 詳細情報
**参照**: step6_TC037.md, step6_TC038.md（構造化変換テスト）

#### Input
- ProcessedResponseData（処理済みデータ）
- FrameVersion（"4E"）

#### Output
- StructuredData:
  - Devices: 2つ（M100, D200）
  - ParseSteps: 3つの解析ステップ記録
  - FrameVersion: "4E"
  - ParsedAt: DateTime.UtcNow
  - IsSuccess: true

#### 検証ポイント
- 2つの構造化デバイス生成
- フレームバージョン情報保持
- 3つの解析ステップ記録

#### StructuredData構造
```csharp
// M100構造化デバイス
{
    DeviceName = "M100",
    DeviceType = "M",
    StartAddress = 100,
    DataType = "Bit",
    Value = 1,
    Unit = null,
    Description = null
}

// D200構造化デバイス
{
    DeviceName = "D200",
    DeviceType = "D",
    StartAddress = 200,
    DataType = "DWord",
    Value = 0x00030002_00000001,
    Unit = null,
    Description = null
}

// ParseSteps記録
[
    {
        StepName = "ProcessReceivedRawData",
        StepNumber = 1,
        IsSuccess = true,
        ProcessedAt = [DateTime],
        ErrorMessage = null
    },
    {
        StepName = "CombineDwordData",
        StepNumber = 2,
        IsSuccess = true,
        ProcessedAt = [DateTime],
        ErrorMessage = null
    },
    {
        StepName = "ParseRawToStructuredData",
        StepNumber = 3,
        IsSuccess = true,
        ProcessedAt = [DateTime],
        ErrorMessage = null
    }
]
```

---

## 4. 統合テスト実装構造

### Arrange（準備）

#### 1. RawResponseData準備
```csharp
// Step4の出力を模擬（4Eフレーム、M100+D200）
var rawResponseData = new RawResponseData
{
    FrameVersion = "4E",
    RawData = "540012340000000000000A00640000000100A8000000010002000300",
    ReceivedAt = DateTime.UtcNow,
    DataLength = 58,
    IsSuccess = true,
    ErrorMessage = null
};
```

#### 2. ProcessedDeviceRequestInfo準備
```csharp
// Step2の出力を模擬（M100 Bit + D200 DWord）
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
```

#### 3. PlcCommunicationManager初期化
```csharp
var plcCommManager = new PlcCommunicationManager(
    loggingManager,
    errorHandler,
    resourceManager
);
```

---

### Act（実行）

#### Step6連続処理実行
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

---

### Assert（検証）

#### 1. Step6-1（基本後処理）検証
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

#### 2. Step6-2（DWord結合）検証
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

#### 3. Step6-3（構造化変換）検証
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

// ParseSteps検証
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

#### 4. データ伝達整合性検証
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

---

## 5. データ伝達フロー詳細

### M100デバイスのデータ伝達
```
RawData: "6400" "00000100"
  ↓ Step6-1: ProcessReceivedRawData
BasicProcessedDevice: M100, DeviceType="M", Value=1, DataType="Bit"
  ↓ Step6-2: CombineDwordData（変更なし）
ProcessedDevice: M100, DeviceType="M", Value=1, DataType="Bit"
  ↓ Step6-3: ParseRawToStructuredData
StructuredDevice: M100, DeviceType="M", Value=1, DataType="Bit"
```

### D200デバイスのデータ伝達（DWord結合）
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

---

## 6. エラーハンドリング

### Step6連続処理エラー処理（TC118では発生しない）

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

### エラー伝播の仕組み
```csharp
// Step6-1失敗時
if (!basicProcessedData.IsSuccess)
{
    // Step6-2, 6-3をスキップ
    return CreateErrorStructuredData(basicProcessedData.Errors);
}

// Step6-2失敗時
if (!processedData.IsSuccess)
{
    // Step6-3をスキップ
    return CreateErrorStructuredData(processedData.Errors);
}

// Step6-3失敗時
if (!structuredData.IsSuccess)
{
    // エラー詳細を記録
    structuredData.ErrorDetails = CreateErrorDetails(parseError);
}
```

---

## 7. テスト実装方針（TDD）

### 開発手法
- C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.mdに記載のTDD手法を使用

### テストファイル配置
- **ファイル名**: PlcCommunicationManager_IntegrationTests.cs
- **配置先**: Tests/Integration/
- **名前空間**: andon.Tests.Integration

### テスト実装順序
1. TC029, TC032, TC037, TC038: Step6の単体テスト（完了済み）
2. TC115, TC116: Step3-5完全サイクル（完了済み）
3. **TC118**: Step6連続処理（本テスト）
4. TC119: Step6データ伝達整合性（次テスト）
5. TC121: 完全サイクル（Step3-6）

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するスタブ
- **RawResponseDataStubs**: Step4出力データスタブ（4Eフレーム）
- **ProcessedDeviceRequestInfoStubs**: Step2出力データスタブ（M+D機器）
- **ExpectedBasicProcessedDataStubs**: Step6-1期待出力スタブ
- **ExpectedProcessedDataStubs**: Step6-2期待出力スタブ
- **ExpectedStructuredDataStubs**: Step6-3期待出力スタブ

---

## 8. 依存クラス・設定

### RawResponseData（Step4出力）
```csharp
public class RawResponseData
{
    public string FrameVersion { get; set; }
    public string RawData { get; set; }
    public DateTime ReceivedAt { get; set; }
    public int DataLength { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### BasicProcessedResponseData（Step6-1出力）
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

### ProcessedResponseData（Step6-2出力）
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

### StructuredData（Step6-3出力）
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

### ProcessedDeviceRequestInfo（Step2出力）
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

---

## 9. ログ出力要件

### LoggingManager連携
- **Step6-1開始ログ**: 入力データ情報
- **Step6-1完了ログ**: 基本処理デバイス数
- **Step6-2開始ログ**: DWord結合対象数
- **Step6-2完了ログ**: 結合完了デバイス数
- **Step6-3開始ログ**: 構造化対象数
- **Step6-3完了ログ**: 構造化デバイス数
- **Step6連続処理完了ログ**: 合計処理時間

### ログレベル
- **Information**: 各ステップ開始・完了
- **Debug**: 詳細情報（デバイス値、結合詳細等）

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

---

## 10. テスト実装チェックリスト

### TC118実装前
- [ ] 単体テスト完了確認（TC029, TC032, TC037, TC038）
- [ ] RawResponseDataStubs作成
- [ ] ProcessedDeviceRequestInfoStubs作成
- [ ] 期待出力スタブ作成（3種類）

### TC118実装中
- [ ] Arrange: RawResponseData準備（4Eフレーム）
- [ ] Arrange: ProcessedDeviceRequestInfo準備（M+D機器）
- [ ] Act: Step6-1（ProcessReceivedRawData）実行
- [ ] Act: Step6-2（CombineDwordData）実行
- [ ] Act: Step6-3（ParseRawToStructuredData）実行
- [ ] Assert: Step6-1検証（基本処理デバイス3つ）
- [ ] Assert: Step6-2検証（DWord結合1つ）
- [ ] Assert: Step6-3検証（構造化デバイス2つ）
- [ ] Assert: データ伝達整合性検証

### TC118実装後
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施
- [ ] TC119（データ伝達整合性）への準備

---

## 11. 参考情報

### Step6処理時間（目安）
- Step6-1（基本後処理）: 5-20ms
- Step6-2（DWord結合）: 3-15ms
- Step6-3（構造化変換）: 5-20ms
- **合計**: 13-55ms（通常30-40ms）

### DWord結合計算例
```
D200（下位ワード）: 0x00000001 (値=1)
D201（上位ワード）: 0x00030002 (値=3, 2の組)

結合計算:
CombinedValue = (UpperWord << 32) | LowerWord
              = (0x00030002 << 32) | 0x00000001
              = 0x00030002_00000001
```

### フレームバージョンの違い
- **3Eフレーム**: SubHeader "5000", EndCode "0000"
- **4Eフレーム**: SubHeader "54001234000000", EndCode "0000"

---

以上が TC118_Step6_ProcessToCombinetoParse連続処理テスト実装に必要な情報です。
