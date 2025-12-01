# TC038: ParseRawToStructuredData_4Eフレーム解析 テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC038_ParseRawToStructuredData_4Eフレーム解析テストケースを、TDD手法に従って実装してください。

---

## 🎯 テスト目的
PlcCommunicationManager.ParseRawToStructuredData メソッドの4Eフレーム解析機能が正常に動作することを確認

## 実装概要

### 目的
PlcCommunicationManager.ParseRawToStructuredData()メソッドのテストケースTC038を実装します。
このテストは、DWord結合済みデータから構造化データへの4Eフレーム解析機能が正常に動作することを検証します。

### 実装対象
- **テストファイル**: `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`
- **テスト名前空間**: `andon.Tests.Unit.Core.Managers`
- **テストメソッド名**: `TC038_ParseRawToStructuredData_4Eフレーム解析`

---

## 前提条件の確認

実装開始前に以下を確認してください：

1. **依存ファイルの存在確認**
   - `Core/Managers/PlcCommunicationManager.cs` (空実装可)
   - `Core/Interfaces/IPlcCommunicationManager.cs`
   - `Core/Models/StructuredData.cs`
   - `Core/Models/ProcessedResponseData.cs`
   - `Core/Models/StructuredDevice.cs`
   - `Core/Models/ParseConfiguration.cs`
   - `Core/Models/StructureDefinition.cs`
   - `Core/Models/FieldDefinition.cs`

2. **テストユーティリティの確認**
   - `Tests/TestUtilities/Mocks/` 配下のモッククラス
   - `Tests/TestUtilities/Stubs/` 配下のスタブクラス
   - `Tests/TestUtilities/TestData/` 配下のテストデータ

3. **前提テストの確認**
   - TC029 (ProcessReceivedRawData) が実装済みであること
   - TC032 (CombineDwordData) が実装済みであること
   - TC037 (ParseRawToStructuredData_3Eフレーム) が実装済みであること

4. **SLMP解析依存関係の確認**
   - `Core/Analyzers/ISlmpFrameAnalyzer.cs`
   - SLMP 4Eフレーム解析ライブラリの利用可能性

5. **開発手法ドキュメント確認**
   - `C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.md`を参照

不足しているファイルがあれば報告してください。

---

## ⭐ 重要度: 高（19時deadline対応）
Step6データ処理の第3段階として、4Eフレーム形式でのDWord結合済みデータから構造化データへの解析が成功することを検証

---

## 実装手順（TDD Red-Green-Refactor）

### Phase 1: Red（テスト失敗）

#### Step 1-1: テストファイル準備
```
ファイル: Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs
名前空間: andon.Tests.Unit.Core.Managers
```

#### Step 1-2: テストケース実装

**Arrange（準備）**:
- MockLoggingManager、MockErrorHandler、MockResourceManager作成
- MockSlmpFrameAnalyzer作成（4Eフレーム解析用）
- PlcCommunicationManagerインスタンス作成（モック注入）
- ProcessedResponseData準備（DWord結合済み、4Eフレーム用）
- ProcessedDeviceRequestInfo準備（4E解析設定含む）
- 期待する構造化結果の定義
- CancellationToken準備

**Act（実行）**:
```csharp
var result = await plcManager.ParseRawToStructuredData(
    processedData,
    requestInfo,
    cancellationToken
);
```

**Assert（検証）**:
- result != null
- result.IsSuccess == true
- result.StructuredDevices.Count > 0
- result.FrameInfo.FrameType == "4E"
- result.FrameInfo.HeaderSize == 13  // 4Eフレームヘッダーサイズ
- result.ParseSteps.Count > 0

#### Step 1-3: テスト実行（Red確認）
```bash
dotnet test --filter "FullyQualifiedName~TC038"
```

期待結果: テスト失敗（4Eフレーム解析が未実装のため）

---

### Phase 2: Green（最小実装）

#### Step 2-1: ParseRawToStructuredData 4Eフレーム対応実装

**実装箇所**: `Core/Managers/PlcCommunicationManager.cs`

**4Eフレーム解析実装**:
```csharp
public async Task<StructuredData> ParseRawToStructuredData(
    ProcessedResponseData processedData,
    ProcessedDeviceRequestInfo processedRequestInfo,
    CancellationToken cancellationToken = default)
{
    // 1. 入力検証
    if (processedData == null)
        throw new ArgumentException("処理済み応答データがnullです");

    if (processedRequestInfo == null)
        throw new ArgumentException("処理済み要求情報がnullです");

    // 2. フレームタイプ判定
    var frameType = processedRequestInfo.ParseConfiguration?.FrameFormat ?? "4E";

    // 3. StructuredDataオブジェクト作成
    var result = new StructuredData
    {
        IsSuccess = true,
        StructuredDevices = new List<StructuredDevice>(),
        FrameInfo = new FrameInfo
        {
            FrameType = frameType,
            HeaderSize = frameType == "4E" ? 13 : 15,  // 4E: 13バイト, 3E: 15バイト
            DataFormat = "Binary"
        },
        ParseSteps = new List<string>(),
        ProcessedAt = DateTime.UtcNow
    };

    // 4. フレームタイプに応じた解析処理
    if (frameType == "4E")
    {
        await Parse4EFrame(processedData, processedRequestInfo, result);
    }
    else if (frameType == "3E")
    {
        await Parse3EFrame(processedData, processedRequestInfo, result);
    }
    else
    {
        throw new NotSupportedException($"未サポートのフレーム形式です: {frameType}");
    }

    return result;
}

private async Task Parse4EFrame(
    ProcessedResponseData processedData,
    ProcessedDeviceRequestInfo requestInfo,
    StructuredData result)
{
    result.ParseSteps.Add("4Eフレーム解析開始");

    // 4Eフレーム固有の解析処理
    // ヘッダーサイズ: 13バイト
    // サブヘッダー: 4バイト (固定値: 0x54001234)
    // ネットワーク情報: 5バイト
    // データ長: 2バイト
    // 終了コード: 2バイト

    foreach (var structureDef in requestInfo.ParseConfiguration.StructureDefinitions)
    {
        var structuredDevice = await ConvertToStructuredDevice4E(
            processedData, structureDef);

        result.StructuredDevices.Add(structuredDevice);
        result.ParseSteps.Add($"4E構造体解析完了: {structureDef.Name}");
    }

    result.ParseSteps.Add("4Eフレーム解析完了");
}

private async Task<StructuredDevice> ConvertToStructuredDevice4E(
    ProcessedResponseData processedData,
    StructureDefinition structureDef)
{
    var structuredDevice = new StructuredDevice
    {
        DeviceName = structureDef.Name,
        StructureType = structureDef.Name,
        Fields = new Dictionary<string, object>(),
        ParsedTimestamp = DateTime.UtcNow,
        SourceFrameType = "4E"
    };

    foreach (var fieldDef in structureDef.Fields)
    {
        var value = ResolveFieldValue4E(processedData, fieldDef);
        structuredDevice.Fields[fieldDef.Name] = value;
        structuredDevice.FieldNames.Add(fieldDef.Name);
    }

    return structuredDevice;
}

private object ResolveFieldValue4E(ProcessedResponseData processedData, FieldDefinition fieldDef)
{
    // 4Eフレーム用フィールド値解決
    // アドレス文字列が"D100_32bit"のような結合済みデバイス名かチェック
    if (fieldDef.Address.Contains("_32bit"))
    {
        var combinedDevice = processedData.CombinedDWordDevices
            .FirstOrDefault(d => d.DeviceName == fieldDef.Address);

        return combinedDevice?.CombinedValue ?? 0;
    }
    else
    {
        // 通常のデバイスアドレス（D100等）の場合
        var basicDevice = processedData.BasicProcessedDevices
            .FirstOrDefault(d => d.Address.ToString() == fieldDef.Address);

        return ConvertDataType4E(basicDevice?.Value, fieldDef.DataType);
    }
}

private object ConvertDataType4E(object? sourceValue, string targetDataType)
{
    if (sourceValue == null) return GetDefaultValue(targetDataType);

    return targetDataType switch
    {
        "Int16" => Convert.ToInt16(sourceValue),
        "Int32" => Convert.ToInt32(sourceValue),
        "UInt16" => Convert.ToUInt16(sourceValue),
        "UInt32" => Convert.ToUInt32(sourceValue),
        "Boolean" => Convert.ToBoolean(sourceValue),
        "String" => sourceValue.ToString() ?? string.Empty,
        _ => throw new NotSupportedException($"未サポートのデータ型です: {targetDataType}")
    };
}
```

#### Step 2-2: テスト再実行（Green確認）
```bash
dotnet test --filter "FullyQualifiedName~TC038"
```

期待結果: テストがパス

---

### Phase 3: Refactor（リファクタリング）

#### Step 3-1: 完全実装
- 4Eフレーム形式の詳細解析
- 3Eフレームとの差分処理の最適化
- フィールドマッピングの強化
- データ型変換の安全性向上
- エラーハンドリングの強化
- ログ出力の追加
- パフォーマンス最適化

#### Step 3-2: テスト再実行（Green維持確認）
```bash
dotnet test --filter "FullyQualifiedName~TC038"
```

期待結果: すべてのテストがパス（リファクタリング後も）

---

## 📋 テスト仕様

### テスト対象メソッド
```csharp
Task<StructuredData> ParseRawToStructuredData(
    ProcessedResponseData processedData,
    ProcessedDeviceRequestInfo processedRequestInfo,
    CancellationToken cancellationToken = default
)
```

### 4Eフレーム成功条件
1. **4Eフレーム解析実行**: 4Eフレーム形式のデータが正しく解析される
2. **StructuredData生成**: 構造化データオブジェクトが生成される
3. **4E固有処理**: 13バイトヘッダーサイズ等の4E仕様に準拠した処理
4. **構造化デバイス追加**: StructuredDevices に解析結果が追加される
5. **メタデータ設定**: 4Eフレーム情報、解析ステップ等のメタデータが設定される

### テストデータ（4Eフレーム用）
```csharp
// DWord結合済み処理データ（4Eフレーム用）
ProcessedResponseData processedData = new ProcessedResponseData
{
    BasicProcessedDevices = new List<ProcessedDevice>
    {
        new ProcessedDevice { DeviceType = "D", Address = 100, Value = 0x1234 },
        new ProcessedDevice { DeviceType = "D", Address = 200, Value = 0xABCD }
    },
    CombinedDWordDevices = new List<CombinedDWordDevice>
    {
        new CombinedDWordDevice
        {
            DeviceName = "D100_32bit",
            CombinedValue = 0x56781234,
            LowWordAddress = 100,
            HighWordAddress = 101
        }
    },
    IsSuccess = true,
    FrameType = "4E"  // 4Eフレーム指定
};

// リクエスト情報（4Eフレーム指定）
ProcessedDeviceRequestInfo requestInfo = new ProcessedDeviceRequestInfo
{
    FrameType = "4E",
    DeviceType = "D",
    StartAddress = 100,
    Count = 4,
    ParseConfiguration = new ParseConfiguration
    {
        FrameFormat = "4E",                    // 4Eフレーム指定
        DataFormat = "Binary",
        HeaderSize = 13,                       // 4Eフレームヘッダーサイズ
        StructureDefinitions = new List<StructureDefinition>
        {
            new StructureDefinition
            {
                Name = "ProductionData4E",
                FrameType = "4E",              // 構造体レベルでも4E指定
                Fields = new List<FieldDefinition>
                {
                    new FieldDefinition { Name = "ProductId", Address = "100", DataType = "Int16" },
                    new FieldDefinition { Name = "Timestamp", Address = "200", DataType = "Int16" },
                    new FieldDefinition { Name = "TotalCount", Address = "D100_32bit", DataType = "Int32" }
                }
            }
        }
    }
};
```

## 🧪 テスト実装パターン

### 1. Arrange（準備）
```csharp
// PlcCommunicationManagerインスタンス作成
// ProcessedResponseData準備（4Eフレーム用DWord結合済み）
// ProcessedDeviceRequestInfo準備（4E解析設定含む）
// 期待する構造化結果の定義（4E仕様準拠）
// CancellationToken準備
```

### 2. Act（実行）
```csharp
var result = await plcManager.ParseRawToStructuredData(
    processedData,
    requestInfo,
    cancellationToken
);
```

### 3. Assert（検証）
```csharp
// result != null
// result.IsSuccess == true
// result.StructuredDevices.Count > 0
// result.FrameInfo.FrameType == "4E"
// result.FrameInfo.HeaderSize == 13  // 4E固有
// result.ParseSteps.Count > 0
// result.ParseSteps[0].Contains("4Eフレーム解析")
```

## 📊 検証項目詳細

### 4Eフレーム解析検証
- [ ] 4Eフレーム形式の正確な識別
- [ ] 13バイトヘッダーの解析（vs 3Eの15バイト）
- [ ] サブヘッダー固定値確認（0x54001234）
- [ ] データ部の構造化解析
- [ ] 終了コードの適切な処理

### 構造化データ生成検証
- [ ] StructuredDevice オブジェクトの生成
- [ ] SourceFrameType = "4E" の設定
- [ ] フィールド値の正確な設定
- [ ] データ型変換の正確性
- [ ] 構造体階層の適切な構築

### 3Eフレームとの差分検証
- [ ] FrameInfo.FrameType = "4E" vs "3E"
- [ ] FrameInfo.HeaderSize = 13 vs 15
- [ ] ParseSteps に "4Eフレーム解析" 記録
- [ ] 4E固有の解析ロジック実行確認

---

## 技術仕様詳細

### 4Eフレーム解析アルゴリズム

#### 4Eフレーム構造解析
```csharp
// 4Eフレーム構造解析（3Eとの差分）
public class SlmpFrame4EAnalyzer
{
    // 4Eフレームヘッダー解析（13バイト）
    public FrameHeader Parse4EHeader(byte[] frameData)
    {
        var header = new FrameHeader
        {
            SubHeader = frameData[0..4],           // サブヘッダー (4バイト) - 固定値: 0x54001234
            NetworkInfo = frameData[4..9],         // ネットワーク情報 (5バイト) ※3Eは7バイト
            DataLength = BitConverter.ToUInt16(frameData, 9),   // データ長 (2バイト)
            EndCode = BitConverter.ToUInt16(frameData, 11),     // 終了コード (2バイト)
            FrameType = "4E",
            HeaderSize = 13                        // 4E固有のヘッダーサイズ
        };

        return header;
    }

    // 4Eデータ部解析（3Eとの共通処理を活用）
    public DeviceData[] Parse4EDeviceData(byte[] dataSection, StructureDefinition structureDef)
    {
        var devices = new List<DeviceData>();
        int offset = 0;

        foreach (var field in structureDef.Fields)
        {
            var device = new DeviceData
            {
                Name = field.Name,
                Address = field.Address,
                DataType = field.DataType,
                Value = ExtractValue4E(dataSection, offset, field.DataType),
                SourceFrameType = "4E"          // 4E由来であることを記録
            };

            devices.Add(device);
            offset += GetDataTypeSize(field.DataType);
        }

        return devices.ToArray();
    }

    private object ExtractValue4E(byte[] data, int offset, string dataType)
    {
        // 4E用データ抽出（3Eと同じロジックだが、4E由来であることを意識）
        return dataType switch
        {
            "Int16" => BitConverter.ToInt16(data, offset),
            "Int32" => BitConverter.ToInt32(data, offset),
            "UInt16" => BitConverter.ToUInt16(data, offset),
            "UInt32" => BitConverter.ToUInt32(data, offset),
            "Boolean" => data[offset] != 0,
            "String" => System.Text.Encoding.ASCII.GetString(data, offset, GetStringLength(data, offset)),
            _ => throw new NotSupportedException($"4Eフレームでは未サポートのデータ型です: {dataType}")
        };
    }
}
```

### 4E・3E共通化処理

#### フレーム解析の統合設計
```csharp
// フレーム解析の統合インターフェース
public interface ISlmpFrameAnalyzer
{
    Task<StructuredData> ParseToStructuredData(
        ProcessedResponseData processedData,
        ProcessedDeviceRequestInfo requestInfo,
        string frameType);
}

public class UnifiedSlmpFrameAnalyzer : ISlmpFrameAnalyzer
{
    private readonly SlmpFrame3EAnalyzer _frame3EAnalyzer;
    private readonly SlmpFrame4EAnalyzer _frame4EAnalyzer;

    public async Task<StructuredData> ParseToStructuredData(
        ProcessedResponseData processedData,
        ProcessedDeviceRequestInfo requestInfo,
        string frameType)
    {
        return frameType switch
        {
            "3E" => await _frame3EAnalyzer.Parse(processedData, requestInfo),
            "4E" => await _frame4EAnalyzer.Parse(processedData, requestInfo),
            _ => throw new NotSupportedException($"未サポートのフレーム形式: {frameType}")
        };
    }
}
```

### データモデル詳細（4E対応）

#### StructuredData構造（4E対応拡張）
```csharp
public class StructuredData
{
    // 基本結果
    public bool IsSuccess { get; set; }
    public List<StructuredDevice> StructuredDevices { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
    public long ProcessingTimeMs { get; set; }

    // フレーム情報（4E/3E両対応）
    public FrameInfo FrameInfo { get; set; }
    public List<string> ParseSteps { get; set; } = new();

    // エラー・統計情報
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int TotalStructuredDevices { get; set; }

    // 4E固有情報
    public bool Is4EFrame => FrameInfo?.FrameType == "4E";
    public int HeaderSize => FrameInfo?.HeaderSize ?? (Is4EFrame ? 13 : 15);

    // メソッド
    public void AddStructuredDevice4E(StructuredDevice device);
    public void Add4EParseStep(string step);
}
```

#### FrameInfo構造（4E対応）
```csharp
public class FrameInfo
{
    public string FrameType { get; set; }       // "4E" or "3E"
    public string DataFormat { get; set; }     // "Binary"
    public int HeaderSize { get; set; }        // 4E: 13バイト, 3E: 15バイト
    public int DataSize { get; set; }          // データ部サイズ
    public ushort EndCode { get; set; }        // 終了コード
    public DateTime ParsedAt { get; set; }     // 解析時刻

    // 4E固有プロパティ
    public bool Is4E => FrameType == "4E";
    public byte[] SubHeader4E { get; set; }    // 4E用サブヘッダー (0x54001234)
    public byte[] NetworkInfo4E { get; set; }  // 4E用ネットワーク情報 (5バイト)
}
```

---

## エラーハンドリング詳細（4E対応）

### スロー例外（4E追加）
- **Data4EProcessingException**: 4Eフレーム解析エラー
  - 不正な4Eフレーム構造
  - 4E構造定義とデータの不整合
  - 4Eフィールドマッピング失敗
- **UnsupportedFrame4EException**: 4E未サポート操作
  - 4Eで未対応のデータ型
  - 4Eで未対応の構造体定義

### エラーメッセージ統一（4E追加）
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    // 4Eフレーム解析エラー
    public const string Invalid4EFrameStructure = "4Eフレーム構造が不正です: {0}";
    public const string Frame4EHeaderParseError = "4Eフレームヘッダー解析に失敗しました: {0}";
    public const string Data4ESectionParseError = "4Eデータ部解析に失敗しました: {0}";
    public const string Unsupported4EFrameType = "未サポートの4Eフレーム形式です: {0}";

    // 4E固有エラー
    public const string Invalid4ESubHeader = "4Eサブヘッダーが不正です。期待値: 0x54001234, 実際値: {0}";
    public const string Invalid4EHeaderSize = "4Eヘッダーサイズが不正です。期待値: 13バイト, 実際値: {0}バイト";
    public const string Unsupported4EDataType = "4Eフレームでは未サポートのデータ型です: {0}";

    // フレーム共通エラー
    public const string FrameTypeMismatch = "フレーム形式が一致しません。要求: {0}, 実際: {1}";
    public const string MultipleFrameTypesNotSupported = "複数のフレーム形式の混在はサポートされていません。";
}
```

---

## 🔧 モック・依存関係（4E対応）

### 必要なモック（4E追加）
```csharp
// ISlmpFrame4EAnalyzer - 4Eフレーム解析用
Mock<ISlmpFrame4EAnalyzer> mockFrame4EAnalyzer;

// ILoggingManager - ログ出力用（4E情報含む）
Mock<ILoggingManager> mockLogging;

// IErrorHandler - エラー処理用（4E例外含む）
Mock<IErrorHandler> mockErrorHandler;

// IResourceManager - リソース管理用
Mock<IResourceManager> mockResourceManager;
```

### 設定値（4E対応）
```csharp
// 4E解析処理タイムアウト
Parse4EProcessingTimeout = 5000ms

// 4Eログレベル
LogLevel4E = Debug

// 4Eメモリ制限
Max4EParseMemoryMb = 100

// 4Eフレーム設定
Frame4EConfig = {
    HeaderSize = 13,
    SubHeaderValue = 0x54001234,
    NetworkInfoSize = 5,
    EndCodeSize = 2,
    DataFormat = "Binary"
}
```

## 📈 成功基準（4E対応）

### 機能的成功基準
1. **正常完了**: メソッドが例外なく完了
2. **4E解析**: 4Eフレーム形式の正確な解析
3. **構造化**: データの適切な構造化
4. **3E互換性**: 3Eフレーム機能を維持
5. **処理時間**: 適切な処理時間での完了（< 200ms）

### 構造化データ検証例（4E）
```csharp
// 期待する4E構造化結果
StructuredDevice expected4EDevice = new StructuredDevice
{
    DeviceName = "ProductionData4E",
    Fields = new Dictionary<string, object>
    {
        ["ProductId"] = 0x1234,      // D100の値
        ["Timestamp"] = 0xABCD,      // D200の値
        ["TotalCount"] = 0x56781234  // D100_32bitの結合値
    },
    StructureType = "ProductionData4E",
    SourceFrameType = "4E",          // 4E由来
    ParsedTimestamp = DateTime.Now
};
```

### 非機能的成功基準
1. **メモリ使用量**: 4E解析処理中のメモリ使用量が閾値内
2. **ログ出力**: 4E解析処理の詳細ログ出力
3. **エラーハンドリング**: 4E解析失敗時の適切なエラー処理
4. **3E共存**: 3Eフレーム処理への影響なし

## 🚨 注意事項

### 4Eフレーム解析の注意
- **フレーム構造**: SLMP 4Eフレーム仕様への準拠
- **ヘッダーサイズ**: 13バイト（3Eは15バイト）の差分に注意
- **サブヘッダー固定値**: 0x54001234の確認
- **バイト順序**: データ並び順の正確な処理
- **3Eとの差分**: 共通処理と固有処理の適切な分離

### 構造化処理の注意
- **フレーム識別**: 4E/3Eの正しい識別と処理分岐
- **フィールドマッピング**: アドレスとフィールドの正確な対応
- **型安全性**: データ型の安全な変換
- **メモリ管理**: 大きな構造体の適切な管理
- **パフォーマンス**: 解析処理の効率性

## 📋 チェックリスト

### 実装前チェック
- [ ] 4Eフレーム仕様の理解
- [ ] 3Eフレームとの差分把握
- [ ] 4E構造化定義の準備
- [ ] テスト用4E処理済みデータの準備
- [ ] 4E期待結果の詳細定義

### 実装後チェック
- [ ] 4Eフレーム解析結果の正確性確認
- [ ] 3Eフレーム機能の非破綻確認
- [ ] 構造化データの完全性確認
- [ ] 実行時間が適切（< 300ms）
- [ ] メモリリークなし

### 4Eフレーム解析テストケース
```csharp
// テストケース1: 基本的な4E解析
// ヘッダー: 0x54001234, 終了コード: 0x0000, データ: 各種デバイス値

// テストケース2: 4E DWord結合値を含む解析
// 32bitデータの正確な構造化

// テストケース3: 4E・3E混在環境での正常動作
// フレーム形式の正しい識別と処理分岐
```

---

## 実装記録・ドキュメント作成要件

### 必須作業項目

#### 1. 進捗記録開始
**ファイル**: `documents/implementation_records/progress_notes/2025-11-06_TC038実装.md`
- 実装開始時刻
- 目標（TC038テスト実装完了）
- 実装方針（4Eフレーム解析アルゴリズム、3Eとの差分対応）
- 進捗状況のリアルタイム更新

#### 2. 実装記録作成
**ファイル**: `documents/implementation_records/method_records/ParseRawToStructuredData_4E実装記録.md`
- 実装判断根拠
  - なぜ4E解析が必要か（3Eとの使い分け）
  - 4E固有処理の設計方針
  - 3Eとの共通化設計判断
  - 技術選択の根拠とトレードオフ
- 発生した問題と解決過程
- 4Eフレーム解析・構造化処理の実装詳細

#### 3. テスト結果保存
**ファイル**: `documents/implementation_records/execution_logs/TC038_4Eテスト結果.log`
- 単体テスト結果（成功/失敗、実行時間、カバレッジ）
- 4Eフレーム解析精度テスト結果
- 3E・4E両対応テスト結果
- Red-Green-Refactorの各フェーズ結果
- パフォーマンステスト結果（実行時間、メモリ使用量）
- エラーログとデバッグ情報

---

## 完了条件

以下すべてが満たされた時点で実装完了とする：

### 機能的完了条件
- [ ] TC038テストがパス
- [ ] ParseRawToStructuredData 4Eフレーム対応実装完了
- [ ] 4Eフレーム解析機能の完全実装
- [ ] 4E構造化データ生成機能の実装
- [ ] 4Eフィールドマッピング機能の実装

### 4Eフレーム解析完了条件
- [ ] 4Eフレームヘッダー解析機能（13バイト解析）
- [ ] 4Eデータ部解析機能（可変長データ対応）
- [ ] 4E終了コード判定機能（正常/異常判別）
- [ ] 3種類の4Eテストケース検証完了（基本/DWord結合/3E混在）

### 3E・4E統合完了条件
- [ ] フレーム形式自動判定機能
- [ ] 3E・4E共通処理の統合
- [ ] フレーム固有処理の適切な分離
- [ ] 既存3E機能の非破綻確認

### 非機能的完了条件
- [ ] エラーハンドリング完了（4E固有例外対応）
- [ ] ログ出力機能完了（4E情報含む）
- [ ] パフォーマンス要件満足（< 200ms）
- [ ] メモリ使用量要件満足（< 100MB）

### ドキュメント完了条件
- [ ] 進捗記録作成完了
- [ ] 実装記録作成完了（4E解析アルゴリズム詳細、3E差分含む）
- [ ] テスト結果ログ保存完了
- [ ] C:\Users\1010821\Desktop\python\andon\documents\design\チェックリスト\step3to6_test実装用プロンプト.mdの該当項目にチェック

### 品質保証完了条件
- [ ] リファクタリング完了（コード品質向上）
- [ ] テスト再実行でGreen維持確認
- [ ] TC029、TC032、TC037との統合テスト確認
- [ ] 4Eフレーム解析精度の徹底検証

---

## 実装時の注意点

### TDD手法厳守
- 必ずテストを先に書く（Red）
- 最小実装でテストをパスさせる（Green）
- リファクタリングで品質向上（Refactor）
- 各フェーズでテスト実行を確認

### 4Eフレーム解析の注意
- **フレーム構造**: SLMP 4Eフレーム仕様への厳密な準拠
- **3Eとの差分**: ヘッダーサイズ、ネットワーク情報サイズの差異
- **バイト順序**: リトルエンディアンでの正確な解析
- **データ型安全性**: 型変換時の範囲チェック
- **エラーハンドリング**: 4E解析失敗時の適切な例外処理

### 3E・4E統合の注意
- **フレーム判定**: 正確なフレーム形式識別
- **共通処理**: 重複コードの適切な統合
- **固有処理**: フレーム固有ロジックの明確な分離
- **後方互換性**: 既存3E機能への影響排除

### 記録の重要性
- 4Eフレーム解析アルゴリズム選択の根拠を詳細記録
- 3Eとの差分対応方針を明確に記録
- テスト結果は具体的な変換値も含めて記録

### 文字化け対策
- 日本語ファイル名の新規作成時は`.txt`経由で作成
- 作成後は必ずReadツールで確認
- 文字化け発見時は早期に対処

---

## 参考情報

### 設計書参照先
- `documents/design/クラス設計.md` - PlcCommunicationManager詳細仕様
- `documents/design/テスト内容.md` - TC038詳細要件
- `documents/design/エラーハンドリング.md` - 例外処理方針
- `documents/design/ログ機能設計.md` - ログ出力仕様

### 開発手法
- `documents/development_methodology/development-methodology.md` - TDD実装手順

### SLMP仕様書
- `pdf2img/sh080931q.pdf` - SLMP通信プロトコル仕様
- 4Eフレーム構造: page_38-41.png
- 3E・4E差分: page_42-45.png
- フレームヘッダー仕様: page_46-48.png
- データ型定義: page_15-18.png

### 既存実装参照
- `step6_TC037.md` - 3Eフレーム解析実装（ベース）
- `step6_TC029.md` - 基本処理実装
- `step6_TC032.md` - DWord結合実装

### PySLMPClient実装参照
- `PySLMPClient/pyslmpclient/const.py` - データ型定義
- `PySLMPClient/pyslmpclient/__init__.py` - フレーム解析ロジック
- `PySLMPClient/pyslmpclient/util.py` - データ変換ユーティリティ
- `PySLMPClient/tests/test_main.py` - 構造化処理テスト実例

---

以上の指示に従って、TC038_ParseRawToStructuredData_4Eフレーム解析テストの実装を開始してください。

不明点や不足情報があれば、実装前に質問してください。