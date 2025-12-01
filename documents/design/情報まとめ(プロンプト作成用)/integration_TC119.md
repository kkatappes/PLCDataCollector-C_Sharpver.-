# Integration Step6 各段階データ伝達整合性 テスト実装用情報（TC119）

## ドキュメント概要

### 目的
このドキュメントは、TC119_Step6_各段階データ伝達整合性テストの実装に必要な情報を集約したものです。
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
- `integration_TC118.md` - Step6連続処理テスト（連携）
- `integration_TC121.md` - 完全サイクルテスト（対比用）

---

## 1. テスト対象機能仕様

### Step6各段階データ伝達整合性検証
**統合テスト対象**: PlcCommunicationManager（Step6データ伝達）
**名前空間**: andon.Core.Managers

#### データ伝達検証ポイント
```
Step6-1 出力: BasicProcessedResponseData
  ↓ データ伝達
Step6-2 入力: BasicProcessedResponseData
Step6-2 出力: ProcessedResponseData
  ↓ データ伝達
Step6-3 入力: ProcessedResponseData
Step6-3 出力: StructuredData
```

#### 統合Input
- **複数機器種別の混合データ**:
  - M機器（Bitデバイス）: M100, M200
  - D機器（Wordデバイス）: D300
  - D機器（DWordデバイス/PseudoDword）: D400, D500
  - 合計5機器（7ワード: M×2 + D×1 + DWord下位×2 + DWord上位×2）

- **RawResponseData（4Eフレーム）**:
  ```
  SubHeader: 54001234000000 (7バイト)
  EndCode: 0000 (2バイト)
  DataLength: 1C00 (28バイト)
  M100値: 6400 00000100 (M100=1)
  M200値: C800 00000000 (M200=0)
  D300値: 2C01 0A000000 (D300=10)
  D400下位: 9001 14000000 (D400下位=20)
  D400上位: 9101 15000000 (D401上位=21)
  D500下位: F401 1E000000 (D500下位=30)
  D500上位: F501 1F000000 (D501上位=31)
  ```

#### 統合Output
- **StructuredData**:
  - 5つの構造化デバイス（M100, M200, D300, D400, D500）
  - 各段階でのデータ変換の完全追跡
  - 値の整合性保証

#### 機能
- Step6-1 → Step6-2 → Step6-3 のデータ伝達整合性検証
- 各機器種別（Bit, Word, DWord）でのデータ一貫性検証
- DWord結合前後の値整合性検証
- タイムスタンプの時系列整合性検証

---

## 2. テストケース仕様（TC119）

### TC119_Step6_各段階データ伝達整合性
**目的**: Step6の各段階（Process→Combine→Parse）でのデータ伝達整合性を検証

#### 前提条件
- 複数機器種別のRawResponseData準備済み
- 複数機器種別のProcessedDeviceRequestInfo準備済み
- PlcCommunicationManager初期化済み

#### 入力データ
**RawResponseData（複数機器混合）**:
```csharp
var rawResponseData = new RawResponseData
{
    FrameVersion = "4E",
    RawData = "5400123400000000001C00" +
              "640000000100" +           // M100=1
              "C80000000000" +           // M200=0
              "2C010A000000" +           // D300=10
              "9001140000009101150000" + // D400=20(下位)+21(上位)
              "F4011E000000F5011F000000", // D500=30(下位)+31(上位)
    ReceivedAt = DateTime.UtcNow,
    DataLength = 110, // 55バイト × 2
    IsSuccess = true
};
```

**ProcessedDeviceRequestInfo（複数機器）**:
```csharp
var deviceRequestInfos = new List<ProcessedDeviceRequestInfo>
{
    // M100: Bitデバイス
    new ProcessedDeviceRequestInfo
    {
        DeviceType = "M",
        StartAddress = 100,
        ReadCount = 1,
        DataType = "Bit",
        IsPseudoDword = false
    },

    // M200: Bitデバイス
    new ProcessedDeviceRequestInfo
    {
        DeviceType = "M",
        StartAddress = 200,
        ReadCount = 1,
        DataType = "Bit",
        IsPseudoDword = false
    },

    // D300: Wordデバイス
    new ProcessedDeviceRequestInfo
    {
        DeviceType = "D",
        StartAddress = 300,
        ReadCount = 1,
        DataType = "Word",
        IsPseudoDword = false
    },

    // D400: DWordデバイス（PseudoDword）
    new ProcessedDeviceRequestInfo
    {
        DeviceType = "D",
        StartAddress = 400,
        ReadCount = 1,
        DataType = "DWord",
        IsPseudoDword = true,
        DwordSplitInfo = new DwordSplitInfo
        {
            LowerWordAddress = 400,
            UpperWordAddress = 401
        }
    },

    // D500: DWordデバイス（PseudoDword）
    new ProcessedDeviceRequestInfo
    {
        DeviceType = "D",
        StartAddress = 500,
        ReadCount = 1,
        DataType = "DWord",
        IsPseudoDword = true,
        DwordSplitInfo = new DwordSplitInfo
        {
            LowerWordAddress = 500,
            UpperWordAddress = 501
        }
    }
};
```

#### 期待出力
**StructuredData（最終出力）**:
```csharp
Assert.Equal(5, structuredData.Devices.Count);
Assert.All(structuredData.Devices, device => Assert.True(device.IsValid));
```

**各デバイスの期待値**:
- M100: Value=1（全段階で同じ）
- M200: Value=0（全段階で同じ）
- D300: Value=10（全段階で同じ）
- D400: Value=0x00000015_00000014（結合後）
- D500: Value=0x0000001F_0000001E（結合後）

#### 動作フロー成功条件
1. **Step6-1 → Step6-2 データ伝達**:
   - BasicProcessedDevices（7つ）→ ProcessedDevices（5つ）
   - M100, M200, D300は値変更なし
   - D400, D500はワード結合

2. **Step6-2 → Step6-3 データ伝達**:
   - ProcessedDevices（5つ）→ StructuredDevices（5つ）
   - 全デバイスの値が変更なく伝達

3. **全段階整合性**:
   - Bit/Wordデバイス: Step6-1 → Step6-2 → Step6-3 で値不変
   - DWordデバイス: Step6-1（分割） → Step6-2（結合） → Step6-3（保持）

---

## 3. データ伝達詳細検証

### M100デバイス（Bit）の伝達
```
RawData: "6400" "00000100"
  ↓ Step6-1: ProcessReceivedRawData
BasicProcessedDevice:
  - DeviceName: "M100"
  - DeviceType: "M"
  - Value: 1
  - DataType: "Bit"

  ↓ Step6-2: CombineDwordData（変更なし）
ProcessedDevice:
  - DeviceName: "M100"
  - DeviceType: "M"
  - Value: 1
  - DataType: "Bit"

  ↓ Step6-3: ParseRawToStructuredData
StructuredDevice:
  - DeviceName: "M100"
  - DeviceType: "M"
  - Value: 1
  - DataType: "Bit"

整合性検証:
Assert.Equal(1, m100Basic.Value);
Assert.Equal(1, m100Processed.Value);
Assert.Equal(1, m100Structured.Value);
```

### M200デバイス（Bit）の伝達
```
RawData: "C800" "00000000"
  ↓ Step6-1: ProcessReceivedRawData
BasicProcessedDevice:
  - DeviceName: "M200"
  - DeviceType: "M"
  - Value: 0
  - DataType: "Bit"

  ↓ Step6-2: CombineDwordData（変更なし）
ProcessedDevice:
  - DeviceName: "M200"
  - DeviceType: "M"
  - Value: 0
  - DataType: "Bit"

  ↓ Step6-3: ParseRawToStructuredData
StructuredDevice:
  - DeviceName: "M200"
  - DeviceType: "M"
  - Value: 0
  - DataType: "Bit"

整合性検証:
Assert.Equal(0, m200Basic.Value);
Assert.Equal(0, m200Processed.Value);
Assert.Equal(0, m200Structured.Value);
```

### D300デバイス（Word）の伝達
```
RawData: "2C01" "0A000000"
  ↓ Step6-1: ProcessReceivedRawData
BasicProcessedDevice:
  - DeviceName: "D300"
  - DeviceType: "D"
  - Value: 10
  - DataType: "Word"

  ↓ Step6-2: CombineDwordData（変更なし）
ProcessedDevice:
  - DeviceName: "D300"
  - DeviceType: "D"
  - Value: 10
  - DataType: "Word"

  ↓ Step6-3: ParseRawToStructuredData
StructuredDevice:
  - DeviceName: "D300"
  - DeviceType: "D"
  - Value: 10
  - DataType: "Word"

整合性検証:
Assert.Equal(10, d300Basic.Value);
Assert.Equal(10, d300Processed.Value);
Assert.Equal(10, d300Structured.Value);
```

### D400デバイス（DWord/PseudoDword）の伝達
```
RawData: "9001" "14000000" "9101" "15000000"
  ↓ Step6-1: ProcessReceivedRawData（分割認識）
BasicProcessedDevice (D400):
  - DeviceName: "D400"
  - DeviceType: "D"
  - Value: 20 (0x14)
  - DataType: "Word"

BasicProcessedDevice (D401):
  - DeviceName: "D401"
  - DeviceType: "D"
  - Value: 21 (0x15)
  - DataType: "Word"

  ↓ Step6-2: CombineDwordData（結合）
ProcessedDevice (D400):
  - DeviceName: "D400"
  - DeviceType: "D"
  - Value: 0x00000015_00000014 (89478488084)
  - DataType: "DWord"

CombinedDWordDevice記録:
  - DeviceName: "D400"
  - LowerWordValue: 20 (0x14)
  - UpperWordValue: 21 (0x15)
  - CombinedValue: 0x00000015_00000014

  ↓ Step6-3: ParseRawToStructuredData
StructuredDevice (D400):
  - DeviceName: "D400"
  - DeviceType: "D"
  - Value: 0x00000015_00000014
  - DataType: "DWord"

整合性検証:
Assert.Equal(20, d400LowerBasic.Value);
Assert.Equal(21, d401UpperBasic.Value);
var expectedCombined = (21UL << 32) | 20UL;
Assert.Equal(expectedCombined, d400Processed.Value);
Assert.Equal(d400Processed.Value, d400Structured.Value);
```

### D500デバイス（DWord/PseudoDword）の伝達
```
RawData: "F401" "1E000000" "F501" "1F000000"
  ↓ Step6-1: ProcessReceivedRawData（分割認識）
BasicProcessedDevice (D500):
  - DeviceName: "D500"
  - DeviceType: "D"
  - Value: 30 (0x1E)
  - DataType: "Word"

BasicProcessedDevice (D501):
  - DeviceName: "D501"
  - DeviceType: "D"
  - Value: 31 (0x1F)
  - DataType: "Word"

  ↓ Step6-2: CombineDwordData（結合）
ProcessedDevice (D500):
  - DeviceName: "D500"
  - DeviceType: "D"
  - Value: 0x0000001F_0000001E (133143986206)
  - DataType: "DWord"

CombinedDWordDevice記録:
  - DeviceName: "D500"
  - LowerWordValue: 30 (0x1E)
  - UpperWordValue: 31 (0x1F)
  - CombinedValue: 0x0000001F_0000001E

  ↓ Step6-3: ParseRawToStructuredData
StructuredDevice (D500):
  - DeviceName: "D500"
  - DeviceType: "D"
  - Value: 0x0000001F_0000001E
  - DataType: "DWord"

整合性検証:
Assert.Equal(30, d500LowerBasic.Value);
Assert.Equal(31, d501UpperBasic.Value);
var expectedCombined = (31UL << 32) | 30UL;
Assert.Equal(expectedCombined, d500Processed.Value);
Assert.Equal(d500Processed.Value, d500Structured.Value);
```

---

## 4. 統合テスト実装構造

### Arrange（準備）

#### 1. RawResponseData準備（複数機器混合）
```csharp
var rawResponseData = new RawResponseData
{
    FrameVersion = "4E",
    RawData = BuildComplexRawData(),
    ReceivedAt = DateTime.UtcNow,
    DataLength = 110,
    IsSuccess = true
};

private string BuildComplexRawData()
{
    return "5400123400000000001C00" +  // SubHeader + EndCode + DataLength
           "640000000100" +              // M100=1
           "C80000000000" +              // M200=0
           "2C010A000000" +              // D300=10
           "9001140000009101150000" +    // D400=20+21
           "F4011E000000F5011F000000";   // D500=30+31
}
```

#### 2. ProcessedDeviceRequestInfo準備（複数機器）
```csharp
var deviceRequestInfos = new List<ProcessedDeviceRequestInfo>
{
    CreateBitDeviceInfo("M", 100),
    CreateBitDeviceInfo("M", 200),
    CreateWordDeviceInfo("D", 300),
    CreateDWordDeviceInfo("D", 400),
    CreateDWordDeviceInfo("D", 500)
};

private ProcessedDeviceRequestInfo CreateBitDeviceInfo(string type, int addr)
{
    return new ProcessedDeviceRequestInfo
    {
        DeviceType = type,
        StartAddress = addr,
        ReadCount = 1,
        DataType = "Bit",
        IsPseudoDword = false
    };
}

private ProcessedDeviceRequestInfo CreateWordDeviceInfo(string type, int addr)
{
    return new ProcessedDeviceRequestInfo
    {
        DeviceType = type,
        StartAddress = addr,
        ReadCount = 1,
        DataType = "Word",
        IsPseudoDword = false
    };
}

private ProcessedDeviceRequestInfo CreateDWordDeviceInfo(string type, int addr)
{
    return new ProcessedDeviceRequestInfo
    {
        DeviceType = type,
        StartAddress = addr,
        ReadCount = 1,
        DataType = "DWord",
        IsPseudoDword = true,
        DwordSplitInfo = new DwordSplitInfo
        {
            LowerWordAddress = addr,
            UpperWordAddress = addr + 1
        }
    };
}
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
// Step6-1: ProcessReceivedRawData
var basicProcessedData = plcCommManager.ProcessReceivedRawData(
    rawResponseData,
    deviceRequestInfos
);

// Step6-2: CombineDwordData
var processedData = plcCommManager.CombineDwordData(
    basicProcessedData,
    deviceRequestInfos
);

// Step6-3: ParseRawToStructuredData
var structuredData = plcCommManager.ParseRawToStructuredData(
    processedData,
    rawResponseData.FrameVersion
);
```

---

### Assert（検証）

#### 1. デバイス数検証
```csharp
// Step6-1: 7デバイス（M×2 + D×1 + DWord下位×2 + DWord上位×2）
Assert.Equal(7, basicProcessedData.BasicProcessedDevices.Count);

// Step6-2: 5デバイス（M×2 + D×1 + DWord×2）
Assert.Equal(5, processedData.ProcessedDevices.Count);

// Step6-3: 5デバイス（M×2 + D×1 + DWord×2）
Assert.Equal(5, structuredData.Devices.Count);
```

#### 2. M100デバイス伝達整合性検証
```csharp
// Step6-1出力
var m100Basic = basicProcessedData.BasicProcessedDevices
    .First(d => d.DeviceName == "M100");
Assert.Equal("M", m100Basic.DeviceType);
Assert.Equal(100, m100Basic.StartAddress);
Assert.Equal(1, m100Basic.Value);
Assert.Equal("Bit", m100Basic.DataType);

// Step6-2出力
var m100Processed = processedData.ProcessedDevices
    .First(d => d.DeviceName == "M100");
Assert.Equal("M", m100Processed.DeviceType);
Assert.Equal(100, m100Processed.StartAddress);
Assert.Equal(1, m100Processed.Value);
Assert.Equal("Bit", m100Processed.DataType);

// Step6-3出力
var m100Structured = structuredData.Devices
    .First(d => d.DeviceName == "M100");
Assert.Equal("M", m100Structured.DeviceType);
Assert.Equal(100, m100Structured.StartAddress);
Assert.Equal(1, m100Structured.Value);
Assert.Equal("Bit", m100Structured.DataType);

// 全段階整合性検証
Assert.Equal(m100Basic.Value, m100Processed.Value);
Assert.Equal(m100Processed.Value, m100Structured.Value);
```

#### 3. M200デバイス伝達整合性検証
```csharp
// Step6-1出力
var m200Basic = basicProcessedData.BasicProcessedDevices
    .First(d => d.DeviceName == "M200");
Assert.Equal(0, m200Basic.Value);

// Step6-2出力
var m200Processed = processedData.ProcessedDevices
    .First(d => d.DeviceName == "M200");
Assert.Equal(0, m200Processed.Value);

// Step6-3出力
var m200Structured = structuredData.Devices
    .First(d => d.DeviceName == "M200");
Assert.Equal(0, m200Structured.Value);

// 全段階整合性検証
Assert.Equal(m200Basic.Value, m200Processed.Value);
Assert.Equal(m200Processed.Value, m200Structured.Value);
```

#### 4. D300デバイス伝達整合性検証
```csharp
// Step6-1出力
var d300Basic = basicProcessedData.BasicProcessedDevices
    .First(d => d.DeviceName == "D300");
Assert.Equal(10, d300Basic.Value);

// Step6-2出力
var d300Processed = processedData.ProcessedDevices
    .First(d => d.DeviceName == "D300");
Assert.Equal(10, d300Processed.Value);

// Step6-3出力
var d300Structured = structuredData.Devices
    .First(d => d.DeviceName == "D300");
Assert.Equal(10, d300Structured.Value);

// 全段階整合性検証
Assert.Equal(d300Basic.Value, d300Processed.Value);
Assert.Equal(d300Processed.Value, d300Structured.Value);
```

#### 5. D400デバイス（DWord）伝達整合性検証
```csharp
// Step6-1出力（分割）
var d400LowerBasic = basicProcessedData.BasicProcessedDevices
    .First(d => d.DeviceName == "D400");
Assert.Equal(20, d400LowerBasic.Value);

var d401UpperBasic = basicProcessedData.BasicProcessedDevices
    .First(d => d.DeviceName == "D401");
Assert.Equal(21, d401UpperBasic.Value);

// Step6-2出力（結合）
var d400Processed = processedData.ProcessedDevices
    .First(d => d.DeviceName == "D400");
Assert.Equal("DWord", d400Processed.DataType);

var expectedD400Combined = ((ulong)21 << 32) | (ulong)20;
Assert.Equal(expectedD400Combined, d400Processed.Value);

// CombinedDWordDevice記録検証
var d400Combined = processedData.CombinedDWordDevices
    .First(d => d.DeviceName == "D400");
Assert.Equal(400, d400Combined.LowerWordAddress);
Assert.Equal(401, d400Combined.UpperWordAddress);
Assert.Equal(20, d400Combined.LowerWordValue);
Assert.Equal(21, d400Combined.UpperWordValue);
Assert.Equal(expectedD400Combined, d400Combined.CombinedValue);

// Step6-3出力（保持）
var d400Structured = structuredData.Devices
    .First(d => d.DeviceName == "D400");
Assert.Equal("DWord", d400Structured.DataType);
Assert.Equal(expectedD400Combined, d400Structured.Value);

// 結合整合性検証
Assert.Equal(d400Processed.Value, d400Structured.Value);
```

#### 6. D500デバイス（DWord）伝達整合性検証
```csharp
// Step6-1出力（分割）
var d500LowerBasic = basicProcessedData.BasicProcessedDevices
    .First(d => d.DeviceName == "D500");
Assert.Equal(30, d500LowerBasic.Value);

var d501UpperBasic = basicProcessedData.BasicProcessedDevices
    .First(d => d.DeviceName == "D501");
Assert.Equal(31, d501UpperBasic.Value);

// Step6-2出力（結合）
var d500Processed = processedData.ProcessedDevices
    .First(d => d.DeviceName == "D500");
Assert.Equal("DWord", d500Processed.DataType);

var expectedD500Combined = ((ulong)31 << 32) | (ulong)30;
Assert.Equal(expectedD500Combined, d500Processed.Value);

// CombinedDWordDevice記録検証
var d500Combined = processedData.CombinedDWordDevices
    .First(d => d.DeviceName == "D500");
Assert.Equal(500, d500Combined.LowerWordAddress);
Assert.Equal(501, d500Combined.UpperWordAddress);
Assert.Equal(30, d500Combined.LowerWordValue);
Assert.Equal(31, d500Combined.UpperWordValue);
Assert.Equal(expectedD500Combined, d500Combined.CombinedValue);

// Step6-3出力（保持）
var d500Structured = structuredData.Devices
    .First(d => d.DeviceName == "D500");
Assert.Equal("DWord", d500Structured.DataType);
Assert.Equal(expectedD500Combined, d500Structured.Value);

// 結合整合性検証
Assert.Equal(d500Processed.Value, d500Structured.Value);
```

#### 7. タイムスタンプ整合性検証
```csharp
// 各段階のタイムスタンプ取得
var processedAtStep1 = basicProcessedData.ProcessedAt;
var processedAtStep2 = processedData.ProcessedAt;
var processedAtStep3 = structuredData.ParsedAt;

// 時系列順序検証
Assert.True(processedAtStep1 <= processedAtStep2);
Assert.True(processedAtStep2 <= processedAtStep3);

// 各段階の処理時間検証（妥当性）
var step1Duration = processedAtStep2 - processedAtStep1;
var step2Duration = processedAtStep3 - processedAtStep2;
Assert.True(step1Duration.TotalMilliseconds < 1000); // 1秒以内
Assert.True(step2Duration.TotalMilliseconds < 1000); // 1秒以内
```

#### 8. 全体整合性マトリックス検証
```csharp
// 全デバイスの伝達整合性を一括検証
var devices = new[]
{
    ("M100", 1),
    ("M200", 0),
    ("D300", 10)
};

foreach (var (deviceName, expectedValue) in devices)
{
    var basicValue = basicProcessedData.BasicProcessedDevices
        .First(d => d.DeviceName == deviceName).Value;
    var processedValue = processedData.ProcessedDevices
        .First(d => d.DeviceName == deviceName).Value;
    var structuredValue = structuredData.Devices
        .First(d => d.DeviceName == deviceName).Value;

    Assert.Equal(expectedValue, basicValue);
    Assert.Equal(basicValue, processedValue);
    Assert.Equal(processedValue, structuredValue);
}
```

---

## 5. データ伝達整合性マトリックス

### Bit/Wordデバイス伝達マトリックス
| デバイス | Step6-1出力 | Step6-2出力 | Step6-3出力 | 整合性 |
|---------|-----------|-----------|-----------|--------|
| M100 | Value=1 | Value=1 | Value=1 | ✅ 不変 |
| M200 | Value=0 | Value=0 | Value=0 | ✅ 不変 |
| D300 | Value=10 | Value=10 | Value=10 | ✅ 不変 |

### DWordデバイス伝達マトリックス
| デバイス | Step6-1出力（分割） | Step6-2出力（結合） | Step6-3出力（保持） | 整合性 |
|---------|------------------|-----------------|----------------|--------|
| D400 | D400=20, D401=21 | Value=0x15_14 | Value=0x15_14 | ✅ 結合正常 |
| D500 | D500=30, D501=31 | Value=0x1F_1E | Value=0x1F_1E | ✅ 結合正常 |

---

## 6. エラーハンドリング

### データ伝達エラー（TC119では発生しない）

#### Step6-1失敗時のエラー伝播
```csharp
if (!basicProcessedData.IsSuccess)
{
    // Step6-2, 6-3をスキップ
    // エラー情報をそのまま伝播
    return CreateErrorResult(basicProcessedData.Errors);
}
```

#### Step6-2失敗時のエラー伝播
```csharp
if (!processedData.IsSuccess)
{
    // Step6-3をスキップ
    // エラー情報をそのまま伝播
    return CreateErrorResult(processedData.Errors);
}
```

#### データ不整合検出時
```csharp
// Bit/Wordデバイスの値変化検出
if (m100Processed.Value != m100Basic.Value)
{
    throw new DataIntegrityException("M100値が変化しました");
}

// DWord結合計算検証
var expectedCombined = ((ulong)upperValue << 32) | (ulong)lowerValue;
if (d400Processed.Value != expectedCombined)
{
    throw new DataIntegrityException("D400結合計算が不正です");
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
1. TC029, TC032, TC037: Step6の単体テスト（完了済み）
2. TC118: Step6連続処理（前提）
3. **TC119**: Step6データ伝達整合性（本テスト）
4. TC121: 完全サイクル（Step3-6）

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するスタブ
- **ComplexRawResponseDataStubs**: 複数機器混合生データスタブ
- **ComplexDeviceRequestInfoStubs**: 複数機器情報スタブ
- **DataIntegrityValidator**: データ整合性検証ヘルパー

---

## 8. ログ出力要件

### LoggingManager連携
- **Step6-1開始ログ**: 入力デバイス数
- **Step6-1完了ログ**: 出力デバイス数（7つ）
- **Step6-2開始ログ**: DWord結合対象数（2つ）
- **Step6-2完了ログ**: 出力デバイス数（5つ）
- **Step6-3開始ログ**: 構造化対象数（5つ）
- **Step6-3完了ログ**: 構造化デバイス数（5つ）
- **整合性検証ログ**: 各デバイスの伝達状況

### ログレベル
- **Information**: 各ステップ開始・完了、デバイス数
- **Debug**: 詳細情報（各デバイス値、結合計算詳細）

### ログ出力例
```
[Information] Step6-1開始: 基本後処理 - 入力機器数=5
[Debug] Step6-1: M100（Bit）値抽出 = 1
[Debug] Step6-1: M200（Bit）値抽出 = 0
[Debug] Step6-1: D300（Word）値抽出 = 10
[Debug] Step6-1: D400（Word下位）値抽出 = 20
[Debug] Step6-1: D401（Word上位）値抽出 = 21
[Debug] Step6-1: D500（Word下位）値抽出 = 30
[Debug] Step6-1: D501（Word上位）値抽出 = 31
[Information] Step6-1完了: 基本処理デバイス数=7
[Information] Step6-2開始: DWord結合処理 - 結合対象=2
[Debug] Step6-2: D400結合 = 20 + (21 << 32) → 0x00000015_00000014
[Debug] Step6-2: D500結合 = 30 + (31 << 32) → 0x0000001F_0000001E
[Information] Step6-2完了: 処理済みデバイス数=5（結合済み2）
[Information] Step6-3開始: 構造化データ変換 - 対象デバイス数=5
[Debug] Step6-3: M100構造化完了 - 値=1
[Debug] Step6-3: M200構造化完了 - 値=0
[Debug] Step6-3: D300構造化完了 - 値=10
[Debug] Step6-3: D400構造化完了 - 値=0x00000015_00000014
[Debug] Step6-3: D500構造化完了 - 値=0x0000001F_0000001E
[Information] Step6-3完了: 構造化デバイス数=5
[Information] データ伝達整合性検証: 全デバイス整合性確認済み
```

---

## 9. テスト実装チェックリスト

### TC119実装前
- [ ] TC118（連続処理）完了確認
- [ ] ComplexRawResponseDataStubs作成
- [ ] ComplexDeviceRequestInfoStubs作成
- [ ] DataIntegrityValidator作成

### TC119実装中
- [ ] Arrange: 複数機器混合RawResponseData準備
- [ ] Arrange: 複数機器ProcessedDeviceRequestInfo準備
- [ ] Act: Step6-1実行
- [ ] Act: Step6-2実行
- [ ] Act: Step6-3実行
- [ ] Assert: デバイス数検証（7→5→5）
- [ ] Assert: M100伝達整合性検証
- [ ] Assert: M200伝達整合性検証
- [ ] Assert: D300伝達整合性検証
- [ ] Assert: D400伝達整合性検証（DWord結合）
- [ ] Assert: D500伝達整合性検証（DWord結合）
- [ ] Assert: タイムスタンプ整合性検証
- [ ] Assert: 全体整合性マトリックス検証

### TC119実装後
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施
- [ ] TC122（複数サイクル統計累積）への準備

---

## 10. 参考情報

### DWord結合計算詳細
```csharp
// 32ビットシフト演算
ulong CombineDWordValue(uint lowerWord, uint upperWord)
{
    return ((ulong)upperWord << 32) | (ulong)lowerWord;
}

// D400の例:
// lowerWord = 20 (0x00000014)
// upperWord = 21 (0x00000015)
// combined = (0x00000015 << 32) | 0x00000014
//          = 0x0000001500000000 | 0x00000014
//          = 0x0000001500000014
```

### データ型変換フロー
```
RawData (hex string) → byte[] → uint32 → ...
  Step6-1: → BasicProcessedDevice (uint32 Value)
  Step6-2: → ProcessedDevice (ulong Value if DWord)
  Step6-3: → StructuredDevice (object Value)
```

---

以上が TC119_Step6_各段階データ伝達整合性テスト実装に必要な情報です。
