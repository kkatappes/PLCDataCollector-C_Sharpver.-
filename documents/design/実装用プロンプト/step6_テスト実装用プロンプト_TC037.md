# TC037: ParseRawToStructuredData_3Eフレーム解析 テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC037_ParseRawToStructuredData_3Eフレーム解析テストケースを、TDD手法に従って実装してください。

---

## 🎯 テスト目的
PlcCommunicationManager.ParseRawToStructuredData メソッドの3Eフレーム解析機能が正常に動作することを確認

## 実装概要

### 目的
PlcCommunicationManager.ParseRawToStructuredData()メソッドのテストケースTC037を実装します。
このテストは、DWord結合済みデータから構造化データへの解析機能が正常に動作することを検証します。

### 実装対象
- **テストファイル**: `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`
- **テスト名前空間**: `andon.Tests.Unit.Core.Managers`
- **テストメソッド名**: `TC037_ParseRawToStructuredData_3Eフレーム解析`

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

4. **SLMP解析依存関係の確認**
   - `Core/Analyzers/ISlmpFrameAnalyzer.cs`
   - SLMP 3Eフレーム解析ライブラリの利用可能性

5. **開発手法ドキュメント確認**
   - `C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.md`を参照

不足しているファイルがあれば報告してください。

---

## ⭐ 重要度: 高（★マーク付きテスト）
Step6データ処理の第3段階（最終段階）として、DWord結合済みデータから構造化データへの解析が成功することを検証

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
- MockSlmpFrameAnalyzer作成（3Eフレーム解析用）
- PlcCommunicationManagerインスタンス作成（モック注入）
- ProcessedResponseData準備（DWord結合済み）
- ProcessedDeviceRequestInfo準備（3E解析設定含む）
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
- result.FrameInfo.FrameType == "3E"
- result.ParseSteps.Count > 0

#### Step 1-3: テスト実行（Red確認）
```bash
dotnet test --filter "FullyQualifiedName~TC037"
```

期待結果: テスト失敗（ParseRawToStructuredDataが未実装のため）

---

### Phase 2: Green（最小実装）

#### Step 2-1: ParseRawToStructuredData最小実装

**実装箇所**: `Core/Managers/PlcCommunicationManager.cs`

**最小実装要件**:
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

    // 2. StructuredDataオブジェクト作成
    var result = new StructuredData
    {
        IsSuccess = true,
        StructuredDevices = new List<StructuredDevice>(),
        FrameInfo = new FrameInfo
        {
            FrameType = "3E",
            DataFormat = "Binary"
        },
        ParseSteps = new List<string> { "基本構造化処理完了" },
        ProcessedAt = DateTime.UtcNow
    };

    // 3. 3Eフレーム解析（最小実装）
    // ここで実際の構造化処理を行う
    // 現在は成功データを返すのみ

    return result;
}
```

#### Step 2-2: テスト再実行（Green確認）
```bash
dotnet test --filter "FullyQualifiedName~TC037"
```

期待結果: テストがパス

---

### Phase 3: Refactor（リファクタリング）

#### Step 3-1: 完全実装
- 3Eフレーム形式の詳細解析
- 構造化データ変換の実装
- フィールドマッピングの実装
- データ型変換の実装
- エラーハンドリングの強化
- ログ出力の追加
- パフォーマンス最適化

#### Step 3-2: テスト再実行（Green維持確認）
```bash
dotnet test --filter "FullyQualifiedName~TC037"
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

### 成功条件
1. **3Eフレーム解析実行**: 3Eフレーム形式のデータが正しく解析される
2. **StructuredData生成**: 構造化データオブジェクトが生成される
3. **構造化デバイス追加**: StructuredDevices に解析結果が追加される
4. **メタデータ設定**: フレーム情報、解析ステップ等のメタデータが設定される

### テストデータ
```csharp
// DWord結合済み処理データ
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
    IsSuccess = true
};

// リクエスト情報（3Eフレーム指定）
ProcessedDeviceRequestInfo requestInfo = new ProcessedDeviceRequestInfo
{
    FrameType = "3E",
    DeviceType = "D",
    StartAddress = 100,
    Count = 4,
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
                    new FieldDefinition { Name = "Timestamp", Address = 200, DataType = "Int16" },
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
// ProcessedResponseData準備（DWord結合済み）
// ProcessedDeviceRequestInfo準備（3E解析設定含む）
// 期待する構造化結果の定義
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
// result.FrameInfo.FrameType == "3E"
// result.ParseSteps.Count > 0
```

## 📊 検証項目詳細

### 3Eフレーム解析検証
- [ ] 3Eフレーム形式の正確な識別
- [ ] フレームヘッダーの解析
- [ ] データ部の構造化解析
- [ ] 終了コードの適切な処理

### 構造化データ生成検証
- [ ] StructuredDevice オブジェクトの生成
- [ ] フィールド値の正確な設定
- [ ] データ型変換の正確性
- [ ] 構造体階層の適切な構築

### メタデータ設定検証
- [ ] FrameInfo の適切な設定
- [ ] ParseSteps の記録
- [ ] 処理時間の記録
- [ ] エラー・警告情報の設定

---

## 技術仕様詳細

### 3Eフレーム解析アルゴリズム

#### フレーム構造解析
```csharp
// 3Eフレーム構造解析
public class SlmpFrame3EAnalyzer
{
    // フレームヘッダー解析
    public FrameHeader ParseHeader(byte[] frameData)
    {
        var header = new FrameHeader
        {
            SubHeader = frameData[0..4],           // サブヘッダー (4バイト)
            NetworkInfo = frameData[4..11],        // ネットワーク情報 (7バイト)
            DataLength = BitConverter.ToUInt16(frameData, 11),  // データ長 (2バイト)
            EndCode = BitConverter.ToUInt16(frameData, 13)      // 終了コード (2バイト)
        };

        return header;
    }

    // データ部解析
    public DeviceData[] ParseDeviceData(byte[] dataSection, StructureDefinition structureDef)
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
                Value = ExtractValue(dataSection, offset, field.DataType)
            };

            devices.Add(device);
            offset += GetDataTypeSize(field.DataType);
        }

        return devices.ToArray();
    }
}
```

#### 構造化データ変換
```csharp
// 構造化データ変換エンジン
public class StructuredDataConverter
{
    public StructuredDevice ConvertToStructuredDevice(
        ProcessedResponseData processedData,
        StructureDefinition structureDef)
    {
        var structuredDevice = new StructuredDevice
        {
            DeviceName = structureDef.Name,
            StructureType = structureDef.Name,
            Fields = new Dictionary<string, object>(),
            ParsedTimestamp = DateTime.UtcNow
        };

        foreach (var fieldDef in structureDef.Fields)
        {
            var value = ResolveFieldValue(processedData, fieldDef);
            structuredDevice.Fields[fieldDef.Name] = value;
        }

        return structuredDevice;
    }

    private object ResolveFieldValue(ProcessedResponseData processedData, FieldDefinition fieldDef)
    {
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
                .FirstOrDefault(d => d.Address == fieldDef.Address);

            return ConvertDataType(basicDevice?.Value, fieldDef.DataType);
        }
    }
}
```

### データモデル詳細

#### StructuredData構造
```csharp
public class StructuredData
{
    // 基本結果
    public bool IsSuccess { get; set; }
    public List<StructuredDevice> StructuredDevices { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
    public long ProcessingTimeMs { get; set; }

    // 3Eフレーム情報
    public FrameInfo FrameInfo { get; set; }
    public List<string> ParseSteps { get; set; } = new();

    // エラー・統計情報
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int TotalStructuredDevices { get; set; }

    // メソッド
    public void AddStructuredDevice(StructuredDevice device);
    public StructuredDevice GetStructuredDevice(string deviceName);
    public void AddParseStep(string step);
}
```

#### StructuredDevice構造
```csharp
public class StructuredDevice
{
    public string DeviceName { get; set; }          // "ProductionData"
    public string StructureType { get; set; }       // 構造体種別
    public Dictionary<string, object> Fields { get; set; } = new();
    public DateTime ParsedTimestamp { get; set; }   // 解析時刻
    public string SourceFrameType { get; set; }     // "3E"
    public List<string> FieldNames { get; set; } = new();

    // メソッド
    public T GetField<T>(string fieldName);
    public void SetField(string fieldName, object value);
    public bool HasField(string fieldName);
}
```

#### FrameInfo構造
```csharp
public class FrameInfo
{
    public string FrameType { get; set; }       // "3E"
    public string DataFormat { get; set; }     // "Binary"
    public int HeaderSize { get; set; }        // 15バイト (3Eフレーム)
    public int DataSize { get; set; }          // データ部サイズ
    public ushort EndCode { get; set; }        // 終了コード
    public DateTime ParsedAt { get; set; }     // 解析時刻
}
```

### フィールドマッピング仕様
```csharp
// フィールド定義とデータマッピング
public class FieldMappingEngine
{
    public Dictionary<string, object> MapFields(
        List<FieldDefinition> fieldDefinitions,
        ProcessedResponseData sourceData)
    {
        var mappedFields = new Dictionary<string, object>();

        foreach (var fieldDef in fieldDefinitions)
        {
            var mappedValue = MapSingleField(fieldDef, sourceData);
            mappedFields[fieldDef.Name] = mappedValue;
        }

        return mappedFields;
    }

    private object MapSingleField(FieldDefinition fieldDef, ProcessedResponseData sourceData)
    {
        return fieldDef.DataType switch
        {
            "Int16" => MapToInt16(fieldDef, sourceData),
            "Int32" => MapToInt32(fieldDef, sourceData),
            "UInt16" => MapToUInt16(fieldDef, sourceData),
            "UInt32" => MapToUInt32(fieldDef, sourceData),
            "Boolean" => MapToBoolean(fieldDef, sourceData),
            "String" => MapToString(fieldDef, sourceData),
            _ => throw new NotSupportedException($"未サポートのデータ型: {fieldDef.DataType}")
        };
    }
}
```

---

## エラーハンドリング詳細

### スロー例外
- **DataProcessingException**: 3Eフレーム解析エラー
  - 不正なフレーム構造
  - 構造定義とデータの不整合
  - フィールドマッピング失敗
- **ArgumentException**: 不正な引数
  - ProcessedResponseDataがnull
  - ProcessedDeviceRequestInfoがnull
  - ParseConfigurationが未設定
- **InvalidOperationException**: 無効な操作
  - 構造定義が空
  - 必須フィールドの欠如
  - データ型変換エラー
- **NotSupportedException**: 未サポート操作
  - 未対応フレーム形式
  - 未対応データ型

### エラーメッセージ統一
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    // 3Eフレーム解析エラー
    public const string InvalidFrameStructure = "3Eフレーム構造が不正です: {0}";
    public const string FrameHeaderParseError = "フレームヘッダー解析に失敗しました: {0}";
    public const string DataSectionParseError = "データ部解析に失敗しました: {0}";
    public const string UnsupportedFrameType = "未サポートのフレーム形式です: {0}";

    // 構造化処理エラー
    public const string StructureDefinitionMissing = "構造定義が指定されていません。";
    public const string FieldMappingFailed = "フィールドマッピングに失敗しました: {0}";
    public const string DataTypeConversionError = "データ型変換エラー: {0} → {1}";
    public const string UnsupportedDataType = "未サポートのデータ型です: {0}";

    // データ整合性エラー
    public const string ProcessedDataNull = "処理済み応答データがnullです。";
    public const string ParseConfigurationNull = "解析設定がnullです。";
    public const string RequiredFieldMissing = "必須フィールドが見つかりません: {0}";

    // 処理フローエラー
    public const string InvalidParseOrder = "不正な解析順序です。DWord結合処理を先に実行してください。";
}
```

### エラー分類と対処方針
```csharp
// エラー分類
public enum ParseErrorType
{
    FrameStructureError,    // フレーム構造エラー → Error（処理停止）
    FieldMappingError,      // フィールドマッピングエラー → Warning（継続処理可能）
    DataTypeError,          // データ型エラー → Error（処理停止）
    ConfigurationError      // 設定エラー → Error（処理停止）
}

// エラー対処例
private void HandleParseError(ParseErrorType errorType, string details)
{
    switch (errorType)
    {
        case ParseErrorType.FieldMappingError:
            _logger.LogWarning("フィールドマッピング警告: {Details}", details);
            // 該当フィールドをスキップして継続
            break;

        case ParseErrorType.FrameStructureError:
            throw new DataProcessingException($"フレーム構造エラー: {details}");

        // その他のエラー処理...
    }
}
```

## 🔧 モック・依存関係

### 必要なモック
```csharp
// ILoggingManager - ログ出力用
Mock<ILoggingManager> mockLogging;

// IErrorHandler - エラー処理用
Mock<IErrorHandler> mockErrorHandler;

// IResourceManager - リソース管理用
Mock<IResourceManager> mockResourceManager;

// ISlmpFrameAnalyzer - 3Eフレーム解析用
Mock<ISlmpFrameAnalyzer> mockFrameAnalyzer;
```

### 設定値
```csharp
// 解析処理タイムアウト
ParseProcessingTimeout = 5000ms

// ログレベル
LogLevel = Debug

// メモリ制限
MaxParseMemoryMb = 100

// 3Eフレーム設定
Frame3EConfig = {
    HeaderSize = 4,
    EndCodeSize = 2,
    DataFormat = "Binary"
}
```

## 📈 成功基準

### 機能的成功基準
1. **正常完了**: メソッドが例外なく完了
2. **3E解析**: 3Eフレーム形式の正確な解析
3. **構造化**: データの適切な構造化
4. **処理時間**: 適切な処理時間での完了（< 200ms）

### 構造化データ検証例
```csharp
// 期待する構造化結果
StructuredDevice expectedDevice = new StructuredDevice
{
    DeviceName = "ProductionData",
    Fields = new Dictionary<string, object>
    {
        ["ProductId"] = 0x1234,      // D100の値
        ["Timestamp"] = 0xABCD,      // D200の値
        ["TotalCount"] = 0x56781234  // D100_32bitの結合値
    },
    StructureType = "ProductionData",
    ParsedTimestamp = DateTime.Now
};
```

### 非機能的成功基準
1. **メモリ使用量**: 解析処理中のメモリ使用量が閾値内
2. **ログ出力**: 解析処理の詳細ログ出力
3. **エラーハンドリング**: 解析失敗時の適切なエラー処理

## 🚨 注意事項

### 3Eフレーム解析の注意
- **フレーム形式**: 3Eフレーム仕様への準拠
- **バイト順序**: データ並び順の正確な処理
- **データ型**: Int16/Int32等の適切な型変換
- **エラーハンドリング**: 解析エラー時の適切な処理

### 構造化処理の注意
- **フィールドマッピング**: アドレスとフィールドの正確な対応
- **型安全性**: データ型の安全な変換
- **メモリ管理**: 大きな構造体の適切な管理
- **パフォーマンス**: 解析処理の効率性

## 📋 チェックリスト

### 実装前チェック
- [ ] 3Eフレーム仕様の理解
- [ ] 構造化定義の準備
- [ ] テスト用処理済みデータの準備
- [ ] 期待結果の詳細定義

### 実装後チェック
- [ ] 3Eフレーム解析結果の正確性確認
- [ ] 構造化データの完全性確認
- [ ] 実行時間が適切（< 300ms）
- [ ] メモリリークなし

### 3Eフレーム解析テストケース
```csharp
// テストケース1: 基本的な3E解析
// ヘッダー: 0x44303030, 終了コード: 0x3030, データ: 各種デバイス値

// テストケース2: DWord結合値を含む解析
// 32bitデータの正確な構造化

// テストケース3: 複数構造体の解析
// 複数の構造体定義の同時解析
```

### Phase 1基本動作確認での位置づけ
- **Step6データ処理系（4テスト中の3番目）**
- **推定実行時間**: 12-18分
- **★重要度**: 高（最小成功基準に含まれる）
- **前提テスト**: TC023（基本後処理）→ TC026（DWord結合）
- **最終出力**: 構造化データ（Step7への入力）

### 依存関係とデータフロー
```
TC023: 生データ → BasicProcessedResponseData
    ↓
TC026: BasicProcessedResponseData → ProcessedResponseData
    ↓
TC031: ProcessedResponseData → StructuredData（最終出力）
```

### Step3-6完全サイクルでの重要性
- **TC066（完全サイクル）の前提**: この解析成功が完全サイクル成功の必要条件
- **最終出力確認**: Step6の最終段階として、データ変換完了を確認
- **Step7連携**: 構造化データがStep7に正しく引き渡されることの確認

---

## 実装記録・ドキュメント作成要件

### 必須作業項目

#### 1. 進捗記録開始
**ファイル**: `documents/implementation_records/progress_notes/2025-11-06_TC037実装.md`
- 実装開始時刻
- 目標（TC037テスト実装完了）
- 実装方針（3Eフレーム解析アルゴリズム）
- 進捗状況のリアルタイム更新

#### 2. 実装記録作成
**ファイル**: `documents/implementation_records/method_records/ParseRawToStructuredData実装記録.md`
- 実装判断根拠
  - なぜこの解析アルゴリズムを選択したか
  - 検討した他の方法との比較（逐次解析 vs 一括解析）
  - 技術選択の根拠とトレードオフ（メモリ使用量 vs 処理速度）
- 発生した問題と解決過程
- 3Eフレーム解析・構造化処理の実装詳細

#### 3. テスト結果保存
**ファイル**: `documents/implementation_records/execution_logs/TC037_テスト結果.log`
- 単体テスト結果（成功/失敗、実行時間、カバレッジ）
- 3Eフレーム解析精度テスト結果
- 構造化データ変換テスト結果
- Red-Green-Refactorの各フェーズ結果
- パフォーマンステスト結果（実行時間、メモリ使用量）
- エラーログとデバッグ情報

---

## 完了条件

以下すべてが満たされた時点で実装完了とする：

### 機能的完了条件
- [ ] TC037テストがパス
- [ ] ParseRawToStructuredData本体実装完了
- [ ] 3Eフレーム解析機能の完全実装
- [ ] 構造化データ生成機能の実装
- [ ] フィールドマッピング機能の実装

### 3Eフレーム解析完了条件
- [ ] フレームヘッダー解析機能（15バイト解析）
- [ ] データ部解析機能（可変長データ対応）
- [ ] 終了コード判定機能（正常/異常判別）
- [ ] 3種類のテストケース検証完了（基本/DWord結合/複数構造体）

### 構造化処理完了条件
- [ ] StructuredDevice生成機能
- [ ] フィールド値マッピング機能（6データ型対応）
- [ ] DWord結合値対応（_32bit接尾辞処理）
- [ ] 複数構造体定義の同時処理機能

### 非機能的完了条件
- [ ] エラーハンドリング完了（4種類の例外対応）
- [ ] ログ出力機能完了（4レベル対応）
- [ ] パフォーマンス要件満足（< 200ms）
- [ ] メモリ使用量要件満足（< 100MB）

### ドキュメント完了条件
- [ ] 進捗記録作成完了
- [ ] 実装記録作成完了（3Eフレーム解析アルゴリズム詳細含む）
- [ ] テスト結果ログ保存完了
- [ ] C:\Users\1010821\Desktop\python\andon\documents\design\チェックリスト\step6_test実施リスト.mdの該当項目にチェック

### 品質保証完了条件
- [ ] リファクタリング完了（コード品質向上）
- [ ] テスト再実行でGreen維持確認
- [ ] TC029、TC032との統合テスト確認
- [ ] 3Eフレーム解析精度の徹底検証

---

## ログ出力要件

### LoggingManager連携
- **処理開始ログ**: 処理済みデータ数、構造定義数、処理開始時刻
- **フレーム解析ログ**: 3Eフレーム構造解析詳細（ヘッダー、終了コード）
- **構造化処理ログ**: 各構造体の変換詳細（フィールド数、データ型変換）
- **処理完了ログ**: 構造化デバイス数、解析ステップ数、所要時間、成功/失敗
- **エラーログ**: 例外詳細、解析失敗箇所、スタックトレース
- **デバッグログ**: フレーム解析詳細、フィールドマッピング過程、パフォーマンス情報

### ログレベル
- **Information**: 処理開始・完了
- **Debug**: 3Eフレーム解析詳細、構造化処理過程
- **Warning**: フィールドマッピング警告、軽微な異常
- **Error**: 例外発生時、解析処理失敗時

### ログ出力例
```csharp
_logger.LogInformation("ParseRawToStructuredData開始: 処理済みデバイス数={ProcessedCount}, 構造定義数={StructureDefCount}",
    processedData.BasicProcessedDevices.Count + processedData.CombinedDWordDevices.Count,
    requestInfo.ParseConfiguration.StructureDefinitions.Count);

_logger.LogDebug("3Eフレーム解析: ヘッダー=0x{Header:X8}, 終了コード=0x{EndCode:X4}, データ長={DataLength}バイト",
    frameHeader.SubHeader, frameHeader.EndCode, frameHeader.DataLength);

_logger.LogDebug("構造化処理: {StructureName} - フィールド={FieldName}:{DataType} = {Value}",
    structureDef.Name, fieldDef.Name, fieldDef.DataType, mappedValue);

_logger.LogInformation("ParseRawToStructuredData完了: 構造化デバイス数={StructuredCount}, 解析ステップ数={StepCount}, 所要時間={ElapsedMs}ms",
    result.StructuredDevices.Count, result.ParseSteps.Count, elapsedMs);
```

---

## 実装時の注意点

### TDD手法厳守
- 必ずテストを先に書く（Red）
- 最小実装でテストをパスさせる（Green）
- リファクタリングで品質向上（Refactor）
- 各フェーズでテスト実行を確認

### 3Eフレーム解析の注意
- **フレーム構造**: SLMP 3Eフレーム仕様への厳密な準拠
- **バイト順序**: リトルエンディアンでの正確な解析
- **データ型安全性**: 型変換時の範囲チェック
- **エラーハンドリング**: 解析失敗時の適切な例外処理

### 構造化処理の注意
- **フィールドマッピング精度**: アドレスとフィールドの正確な対応
- **DWord結合値対応**: "_32bit"接尾辞付きデバイス名の適切な処理
- **データ型変換**: 6種類のデータ型（Int16/32, UInt16/32, Boolean, String）への安全な変換
- **メモリ管理**: 大きな構造体の適切な管理

### 記録の重要性
- 3Eフレーム解析アルゴリズム選択の根拠を詳細記録
- 構造化処理の変換過程を段階的に記録
- テスト結果は具体的な変換値も含めて記録

### 文字化け対策
- 日本語ファイル名の新規作成時は`.txt`経由で作成
- 作成後は必ずReadツールで確認
- 文字化け発見時は早期に対処

---

## 参考情報

### 設計書参照先
- `documents/design/クラス設計.md` - PlcCommunicationManager詳細仕様
- `documents/design/テスト内容.md` - TC037詳細要件
- `documents/design/エラーハンドリング.md` - 例外処理方針
- `documents/design/ログ機能設計.md` - ログ出力仕様

### 開発手法
- `documents/development_methodology/development-methodology.md` - TDD実装手順

### SLMP仕様書
- `pdf2img/sh080931q.pdf` - SLMP通信プロトコル仕様
- 3Eフレーム構造: page_42-45.png
- フレームヘッダー仕様: page_46-48.png
- データ型定義: page_15-18.png

### 構造化処理参照
- C# Dictionary操作: 動的フィールド管理
- リフレクション: 動的データ型変換
- LINQ: データ検索・フィルタリング

### PySLMPClient実装参照
- `PySLMPClient/pyslmpclient/const.py` - データ型定義
- `PySLMPClient/pyslmpclient/__init__.py` - フレーム解析ロジック
- `PySLMPClient/pyslmpclient/util.py` - データ変換ユーティリティ
- `PySLMPClient/tests/test_main.py` - 構造化処理テスト実例

### テストデータサンプル
**配置先**: Tests/TestUtilities/TestData/StructuredDataSamples/
- ProcessedData_WithCombined.json: DWord結合済み処理データ
- StructureDefinitions_Production.json: 生産データ構造定義
- ExpectedResults_StructuredData.json: 期待される構造化結果

### 構造化データ変換参考
```csharp
// 構造化変換パターン例
// 入力: ProcessedResponseData (BasicDevices + CombinedDWordDevices)
// 構造定義: ProductionData { ProductId:Int16, Timestamp:Int16, TotalCount:Int32 }
// 出力: StructuredDevice with Fields["ProductId"]=0x1234, Fields["TotalCount"]=0x56781234

// フィールドマッピング例
// ProductId → D100 (BasicProcessedDevice)
// Timestamp → D200 (BasicProcessedDevice)
// TotalCount → D100_32bit (CombinedDWordDevice)
```

---

以上の指示に従って、TC037_ParseRawToStructuredData_3Eフレーム解析テストの実装を開始してください。

不明点や不足情報があれば、実装前に質問してください。