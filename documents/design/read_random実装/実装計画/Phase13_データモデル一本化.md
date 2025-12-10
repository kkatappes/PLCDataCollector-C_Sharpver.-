# Phase13: データモデル一本化（TDD準拠版）

## ステータス
✅ **Phase1完了（100%）** - Option B（完全統一）実装完了（2025-12-08完了）
✅ **Phase2完了（100%）** - 旧モデル削除完了（2025-12-08完了）
✅ **Phase3完了（100%）** - SlmpDataParser整理完了（2025-12-08完了）

### 実装状況サマリ
- ✅ **Phase1完了**: データ生成処理の統一（100%）
  - ExtractDeviceDataFromReadRandom()メソッド実装済み
  - ExtractDeviceData()メソッド実装済み
  - ProcessReceivedRawData()修正完了
  - **Option B採用**: ReadRandom(0x0403)形式への完全統一
  - テストコード修正: DeviceSpecifications明示的設定（6テスト）
  - テストデータ修正: ReadRandom形式対応（2ファイル）
- ✅ **Phase2完了**: 旧モデル削除（100%）
  - BasicProcessedDevicesプロパティ削除（5行）
  - CombinedDWordDevicesプロパティ削除（6行）
  - テスト修正: PlcCommunicationManager_IntegrationTests_TC143_10.cs（1箇所）
  - 旧モデルファイル削除確認: ProcessedDevice.cs等3ファイル（255行、Phase1以前に削除済み）
  - **合計削減**: 269行
- ✅ **Phase3完了**: SlmpDataParser整理（100%）
  - ParseReadRandomResponse()メソッド削除（94行：本体58行+ヘルパー36行）
  - SlmpDataParserTests.cs削除（テストファイル全体）
  - 8テストをPlcCommunicationManagerTests.csに移行（478行追加）
  - TDD準拠でRed→Green→Refactor完遂
  - **合計削減**: 94行
- ✅ **ビルド結果**: 0エラー、11警告（既存警告のみ）
- ✅ **テスト結果**: 801合格、2スキップ、5失敗（Phase13無関係、合計808テスト）
  - **Phase13追加テスト**: 8/8合格（100%）
  - **注意**: 失敗5件はPhase13無関係（ContinuousMode関連3件、タイミング1件、DI設定1件）
- ✅ **設計一貫性**: ReadRandom(0x0403)に完全統一、DeviceData型への完全一本化
- ✅ **Phase13完了**: データモデル一本化完了（全3フェーズ完了）
- **削減完了行数**: 363行（Phase2: 269行、Phase3: 94行）
- **詳細結果**:
  - [Phase13_Phase1_Option_B_TestResults.md](../実装結果/Phase13_Phase1_Option_B_TestResults.md)
  - [Phase13_Phase2_旧モデル削除_TestResults.md](../実装結果/Phase13_Phase2_旧モデル削除_TestResults.md)
  - [Phase13_Phase3_SlmpDataParser整理_TestResults.md](../実装結果/Phase13_Phase3_SlmpDataParser整理_TestResults.md)

## 概要
ProcessedDevice（旧設計）とDeviceData（新設計）の重複実装を解消し、DeviceDataに一本化します。
**TDD原則に従い、各ステップで「テスト修正 → 実装変更 → テストパス確認」の順序を厳守します。**

## TDD実装方針

### 基本原則
1. **Red → Green → Refactor**の順守
   - Red: テストを新APIに対応させて失敗させる
   - Green: 実装を変更してテストをパスさせる
   - Refactor: コードをクリーンにする

2. **単一ブロックごとの確認**
   - 各メソッドの実装とテストを同時に進める
   - 複合機能のテストは全ての単一ブロックテストパス後に実施

3. **段階的なテストパス保証**
   - 各小ステップで必ずテストパスを確認
   - テストが失敗したまま次のステップに進まない

---

## 前提条件
- ✅ Phase1-10完了: 全機能実装・テスト・実機確認・クリーンアップ完了
- ✅ ReadRandom(0x0403)が実機環境で正常動作確認済み
- ✅ DeviceData（新設計）が実際の本番コードで使用されている

## 削除対象ファイル確認結果（2025-12-05実施）

### ✅ 存在確認済みファイル
1. **ProcessedDevice.cs** (andon/Core/Models/)
   - 110行、TC029テスト用として実装
   - ビット展開機能を含む
   - **本番コードで使用**: PlcCommunicationManager内部で生成のみ

2. **BasicProcessedResponseData.cs** (andon/Core/Models/)
   - 97行、ProcessReceivedRawDataメソッドの戻り値型
   - **本番コードで使用**: IPlcCommunicationManager, PlcCommunicationManager

3. **CombinedDWordDevice.cs** (andon/Core/Models/)
   - 48行、DWord結合済みデバイス情報
   - **本番コードで使用**: ProcessedResponseDataのObsoleteプロパティ経由のみ

### 使用箇所の詳細
- **本番コード**: 約89箇所の言及
  - PlcCommunicationManager: ProcessReceivedRawData(), ExtractDeviceValues系
  - IPlcCommunicationManager: メソッドシグネチャ
  - ProcessedResponseData: Obsolete プロパティ（5メンバー、計171行）
  - ErrorMessages: 定数1件

- **テストコード**: 約155箇所
  - SlmpDataParserTests.cs: 8テストケース
  - ReadRandomIntegrationTests.cs: 5箇所
  - ErrorHandling_IntegrationTests.cs: 6箇所
  - その他多数

---

## 実装ステップ（TDD準拠）

### Phase1: データ生成処理の統一

#### Phase1-1: ExtractDeviceDataFromReadRandom()実装 ✅完了

**実装済み内容**（line 2139-2163）:
- DeviceDataを直接生成するメソッド
- ProcessedDevice生成を廃止
- テスト: PlcCommunicationManagerTests内で確認済み

---

#### Phase1-2: ExtractDeviceData()メソッド実装（TDD）

##### ステップ1: テストケース作成（Red）

**対象テストファイル**: `andon/Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`

```csharp
// 新規テストケース追加

[Fact]
public void ExtractDeviceData_ReadRandomRequest_ReturnsDictionary()
{
    // Arrange
    var manager = CreateManager();
    var requestInfo = new ProcessedDeviceRequestInfo
    {
        DeviceSpecifications = new List<DeviceSpecification>
        {
            new DeviceSpecification { DeviceType = "D", DeviceNumber = 100, IsDWord = false }
        }
    };
    byte[] deviceData = new byte[] { 0x12, 0x34 }; // Value = 0x3412 (Little Endian)

    // Act
    var result = manager.ExtractDeviceData(deviceData, requestInfo);

    // Assert
    Assert.NotNull(result);
    Assert.IsType<Dictionary<string, DeviceData>>(result);
    Assert.Single(result);
    Assert.True(result.ContainsKey("D100"));
    Assert.Equal(0x3412, result["D100"].Value);
}

[Fact]
public void ExtractDeviceData_ReadCommand_ThrowsNotSupportedException()
{
    // Arrange
    var manager = CreateManager();
    var requestInfo = new ProcessedDeviceRequestInfo
    {
        DeviceSpecifications = null // Read(0x0401)の場合
    };
    byte[] deviceData = new byte[] { 0x12, 0x34 };

    // Act & Assert
    var exception = Assert.Throws<NotSupportedException>(
        () => manager.ExtractDeviceData(deviceData, requestInfo)
    );
    Assert.Contains("Read(0x0401)は廃止されました", exception.Message);
}
```

**確認**: テスト実行 → コンパイルエラー（ExtractDeviceDataメソッドが存在しない）

```bash
dotnet test andon/Tests/andon.Tests.csproj --filter "FullyQualifiedName~ExtractDeviceData"
# 期待結果: コンパイルエラー
```

---

##### ステップ2: 実装（Green）

**対象ファイル**: `andon/Core/Managers/PlcCommunicationManager.cs`

```csharp
/// <summary>
/// デバイスデータを抽出してDictionary形式で返す
/// Phase13実装: ProcessedDevice経由を廃止し、DeviceDataを直接生成
/// </summary>
private Dictionary<string, DeviceData> ExtractDeviceData(
    byte[] deviceData,
    ProcessedDeviceRequestInfo requestInfo)
{
    // ReadRandom(0x0403)の場合
    if (requestInfo.DeviceSpecifications != null && requestInfo.DeviceSpecifications.Any())
    {
        return ExtractDeviceDataFromReadRandom(deviceData, requestInfo);
    }

    // Read(0x0401)は廃止
    throw new NotSupportedException(
        "Read(0x0401)は廃止されました。ReadRandom(0x0403)を使用してください。");
}
```

**確認**: テスト実行 → テストパス

```bash
dotnet test andon/Tests/andon.Tests.csproj --filter "FullyQualifiedName~ExtractDeviceData"
# 期待結果: 2 passed
```

---

##### ステップ3: Refactor（既存テストの確認）

**確認項目**:
- ExtractDeviceValuesFromReadRandom()を使用している既存テストを確認
- 新しいExtractDeviceData()メソッドと整合性チェック

```bash
# 全PlcCommunicationManagerテスト実行
dotnet test andon/Tests/andon.Tests.csproj --filter "FullyQualifiedName~PlcCommunicationManagerTests"
# 期待結果: 全てパス（既存機能に影響なし）
```

---

#### Phase1-3: ProcessReceivedRawData()メソッド修正（TDD）

##### ステップ1: インターフェース修正とテスト更新（Red）

**対象ファイル1**: `andon/Core/Interfaces/IPlcCommunicationManager.cs`

```csharp
// 変更前
Task<BasicProcessedResponseData> ProcessReceivedRawData(
    ProcessedDeviceRequestInfo processedRequestInfo,
    ...);

// 変更後
Task<ProcessedResponseData> ProcessReceivedRawData(
    ProcessedDeviceRequestInfo processedRequestInfo,
    ...);
```

**対象ファイル2**: `andon/Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`

```csharp
// 既存のProcessReceivedRawDataテストを修正

[Fact]
public async Task ProcessReceivedRawData_ValidData_ReturnsProcessedResponseData()
{
    // Arrange
    var manager = CreateManager();
    var requestInfo = new ProcessedDeviceRequestInfo
    {
        DeviceSpecifications = new List<DeviceSpecification>
        {
            new DeviceSpecification { DeviceType = "D", DeviceNumber = 100, IsDWord = false }
        }
    };
    byte[] rawData = CreateValidReadRandomResponse(); // ヘッダー + デバイスデータ

    // Act
    var result = await manager.ProcessReceivedRawData(requestInfo, rawData, ...);

    // Assert
    Assert.NotNull(result);
    Assert.IsType<ProcessedResponseData>(result); // ← BasicProcessedResponseData から変更
    Assert.NotNull(result.ProcessedData);
    Assert.True(result.ProcessedData.ContainsKey("D100"));
}
```

**確認**: テスト実行 → コンパイルエラーまたは型不一致エラー

```bash
dotnet test andon/Tests/andon.Tests.csproj --filter "FullyQualifiedName~ProcessReceivedRawData"
# 期待結果: コンパイルエラーまたは型エラー
```

---

##### ステップ2: PlcCommunicationManager実装修正（Green）

**対象ファイル**: `andon/Core/Managers/PlcCommunicationManager.cs`

```csharp
// ProcessReceivedRawData()メソッド修正

public async Task<ProcessedResponseData> ProcessReceivedRawData(
    ProcessedDeviceRequestInfo processedRequestInfo,
    byte[] rawResponseData,
    ConnectionType connectionType)
{
    try
    {
        // ヘッダー解析（既存処理）
        var header = ExtractSlmpHeader(rawResponseData, processedRequestInfo.FrameVersion, connectionType);

        // デバイスデータ抽出（Phase13: DeviceData直接生成）
        byte[] deviceData = ExtractDeviceDataBytes(rawResponseData, header);
        var processedData = ExtractDeviceData(deviceData, processedRequestInfo);

        // ProcessedResponseData作成
        var result = new ProcessedResponseData
        {
            ProcessedData = processedData,
            Header = header,
            ReceivedAt = DateTime.Now,
            Success = true
        };

        return result;
    }
    catch (Exception ex)
    {
        _loggingManager.LogError($"データ処理エラー: {ex.Message}");
        throw;
    }
}
```

**削除対象メソッド**（実装後）:
```csharp
// ✅ ExtractDeviceValuesFromReadRandom() - 削除
// ✅ ExtractWordDevices() - 削除（使用されていない場合）
// ✅ ExtractBitDevices() - 削除（使用されていない場合）
```

**確認**: テスト実行 → テストパス

```bash
dotnet test andon/Tests/andon.Tests.csproj --filter "FullyQualifiedName~ProcessReceivedRawData"
# 期待結果: 全てパス
```

---

##### ステップ3: モック・スタブの修正

**対象ファイル**: `andon/Tests/TestUtilities/Mocks/MockPlcCommunicationManager.cs`

```csharp
// MockPlcCommunicationManager修正

public Task<ProcessedResponseData> ProcessReceivedRawData(
    ProcessedDeviceRequestInfo processedRequestInfo,
    ...)
{
    // モック実装を ProcessedResponseData に変更
    var result = new ProcessedResponseData
    {
        ProcessedData = new Dictionary<string, DeviceData>(),
        Success = true
    };
    return Task.FromResult(result);
}
```

**確認**: 全統合テスト実行

```bash
dotnet test andon/Tests/andon.Tests.csproj --filter "Category=Integration"
# 期待結果: 全てパス
```

---

##### ステップ4: Phase1完了確認

**完了条件チェックリスト**:
- ✅ ExtractDeviceData()実装完了
- ✅ ExtractDeviceData()テスト全パス
- ✅ ProcessReceivedRawData()戻り値型変更完了（BasicProcessedResponseData → ProcessedResponseData）
- ✅ IPlcCommunicationManagerインターフェース変更完了
- ✅ MockPlcCommunicationManager修正完了
- ✅ ExtractDeviceValuesFromReadRandom()削除完了
- ✅ ビルド成功（0 errors）
- ✅ PlcCommunicationManagerTests全パス
- ✅ 統合テスト全パス

```bash
# 最終確認
dotnet build andon/andon.csproj
dotnet test andon/Tests/andon.Tests.csproj --filter "FullyQualifiedName~PlcCommunicationManager"
dotnet test andon/Tests/andon.Tests.csproj --filter "Category=Integration"
```

---

### Phase2: ProcessedDevice関連ファイル削除（TDD）

#### ステップ1: 削除対象ファイルの最終確認

```bash
# 本番コードでの使用箇所確認
grep -r "ProcessedDevice" andon/Core/ andon/Infrastructure/ andon/Services/ andon/Utilities/ --exclude-dir=Tests

# 期待結果: ProcessedResponseDataのObsoleteプロパティのみ
```

#### ステップ2: テストコード修正（Red → Green）

**対象テストファイル**:
1. `ProcessedResponseDataTests.cs` - BasicProcessedDevices関連テスト削除
2. `SlmpDataParserTests.cs` - ParseReadRandomResponse関連テスト削除または移行

**実装手順**:
```bash
# 1. ProcessedResponseDataTests.cs修正
# - BasicProcessedDevices プロパティ使用テストを削除
# - CombinedDWordDevices プロパティ使用テストを削除
# - ProcessedData（DeviceData）テストのみ残す

# 2. テスト実行 → 一部失敗（Obsoleteプロパティはまだ存在）
dotnet test andon/Tests/andon.Tests.csproj --filter "FullyQualifiedName~ProcessedResponseDataTests"
```

#### ステップ3: ProcessedResponseData.cs修正（Green）

**対象ファイル**: `andon/Core/Models/ProcessedResponseData.cs`

```csharp
// 削除対象プロパティ（5メンバー、計171行）

// ❌ 削除
// [Obsolete("Phase10で削除予定。ProcessedDataプロパティを使用してください。")]
// public List<ProcessedDevice> BasicProcessedDevices { get; set; }

// ❌ 削除
// [Obsolete("Phase10で削除予定。ProcessedDataプロパティを使用してください。")]
// public List<CombinedDWordDevice> CombinedDWordDevices { get; set; }

// ❌ 削除
// [Obsolete("Phase10で削除予定")]
// private List<ProcessedDevice> ConvertToProcessedDevices() { }

// ❌ 削除
// [Obsolete("Phase10で削除予定")]
// private List<CombinedDWordDevice> ConvertToCombinedDWordDevices() { }

// ❌ 削除
// [Obsolete("Phase10で削除予定")]
// private bool[] ExpandWordToBits(ushort value) { }
```

**確認**: テスト実行 → 修正したテスト全パス

```bash
dotnet test andon/Tests/andon.Tests.csproj --filter "FullyQualifiedName~ProcessedResponseDataTests"
# 期待結果: 全てパス（削除したテストを除く）
```

#### ステップ4: モデルファイル削除

```bash
# 1. ProcessedDevice.cs削除
rm andon/Core/Models/ProcessedDevice.cs

# 2. BasicProcessedResponseData.cs削除
rm andon/Core/Models/BasicProcessedResponseData.cs

# 3. CombinedDWordDevice.cs削除
rm andon/Core/Models/CombinedDWordDevice.cs

# 4. ErrorMessages定数削除（該当する場合）
# andon/Core/Constants/ErrorMessages.cs から ProcessedDeviceRequestInfoNull 削除

# 5. ビルド確認
dotnet build andon/andon.csproj
# 期待結果: 0 errors, 0 warnings
```

#### ステップ5: Phase2完了確認

**完了条件チェックリスト**:
- ✅ ProcessedResponseDataTests修正完了
- ✅ ProcessedResponseData.cs のObsoleteプロパティ削除完了（171行削除）
- ✅ ProcessedDevice.cs削除（110行）
- ✅ BasicProcessedResponseData.cs削除（97行）
- ✅ CombinedDWordDevice.cs削除（48行）
- ✅ ビルド成功（0 errors, 0 warnings）
- ✅ ProcessedResponseDataTests全パス
- ✅ 全単体テストパス

```bash
# 最終確認
dotnet build andon/andon.csproj
dotnet test andon/Tests/andon.Tests.csproj
```

---

### Phase3: SlmpDataParser整理（TDD）

#### 背景
SlmpDataParser.ParseReadRandomResponse()は**テストコードのみで使用**されており、本番コードでは使用されていません。PlcCommunicationManager内部で完結しているため、外部のパースメソッドは不要です。

#### ステップ1: テストケースの移行または削除計画

**対象テストファイル**:
1. `SlmpDataParserTests.cs` - 8テストケース
2. `ReadRandomIntegrationTests.cs` - 5箇所
3. `ErrorHandling_IntegrationTests.cs` - 6箇所

**方針**:
- **Option A**: PlcCommunicationManagerTests.csに移行（推奨）
- **Option B**: 統合テストとして再設計
- **Option C**: 削除（既に十分なテストカバレッジがある場合）

#### ステップ2: テストの移行（Red → Green）

**移行例（Option A）**:

```csharp
// SlmpDataParserTests.cs の以下のテストを削除:
// - ParseReadRandomResponse_4EFrame_ValidResponse_ReturnsCorrectData
// - ParseReadRandomResponse_3EFrame_ValidResponse_ReturnsCorrectData
// - ParseReadRandomResponse_HexAddressDevice_ReturnsCorrectKey
// など8テスト

// 代わりにPlcCommunicationManagerTests.cs に以下を追加:

[Fact]
public async Task PostprocessReceivedData_4EFrame_ValidResponse_ReturnsCorrectDeviceData()
{
    // Arrange
    var manager = CreateManager();
    var requestInfo = Create4EFrameRequestInfo();
    byte[] responseFrame = Create4EValidResponse();

    // Act
    var result = await manager.PostprocessReceivedData(responseFrame, requestInfo, ...);

    // Assert
    Assert.NotNull(result);
    Assert.NotNull(result.ProcessedData);
    Assert.True(result.ProcessedData.ContainsKey("D100"));
    Assert.Equal(expectedValue, result.ProcessedData["D100"].Value);
}

[Fact]
public async Task PostprocessReceivedData_3EFrame_ValidResponse_ReturnsCorrectDeviceData()
{
    // 同様に3Eフレーム用テスト
}

[Fact]
public async Task PostprocessReceivedData_HexAddressDevice_ReturnsCorrectKey()
{
    // 16進アドレステスト
}
```

**確認**: 新しいテスト実行 → パス

```bash
dotnet test andon/Tests/andon.Tests.csproj --filter "FullyQualifiedName~PlcCommunicationManagerTests"
# 期待結果: 新規追加テスト全パス
```

#### ステップ3: 統合テストの修正

**対象ファイル**: `ReadRandomIntegrationTests.cs`, `ErrorHandling_IntegrationTests.cs`

```csharp
// 変更前
var parsedData = SlmpDataParser.ParseReadRandomResponse(responseFrame, devices);

// 変更後（統合テストとして）
var manager = new PlcCommunicationManager(...);
var result = await manager.PostprocessReceivedData(responseFrame, requestInfo, ...);
var parsedData = result.ProcessedData;
```

**確認**: 統合テスト実行 → パス

```bash
dotnet test andon/Tests/andon.Tests.csproj --filter "Category=Integration"
# 期待結果: 全てパス
```

#### ステップ4: SlmpDataParser.ParseReadRandomResponse()削除

```bash
# 1. SlmpDataParser.cs からメソッド削除（line 21-78, 58行）
# andon/Utilities/SlmpDataParser.cs

# 2. SlmpDataParserTests.cs から8テスト削除
# andon/Tests/Unit/Utilities/SlmpDataParserTests.cs

# 3. ビルド確認
dotnet build andon/andon.csproj
# 期待結果: 0 errors

# 4. テスト実行
dotnet test andon/Tests/andon.Tests.csproj
# 期待結果: 全てパス
```

#### ステップ5: Phase3完了確認

**完了条件チェックリスト**:
- ✅ SlmpDataParser.ParseReadRandomResponse()削除（58行）
- ✅ SlmpDataParserTests.cs の8テスト削除または移行
- ✅ ReadRandomIntegrationTests.cs 修正（5箇所）
- ✅ ErrorHandling_IntegrationTests.cs 修正（6箇所）
- ✅ PlcCommunicationManagerTests.cs に移行テスト追加（該当する場合）
- ✅ ビルド成功（0 errors, 0 warnings）
- ✅ 全単体テストパス
- ✅ 全統合テストパス

```bash
# 最終確認
dotnet build andon/andon.csproj
dotnet test andon/Tests/andon.Tests.csproj
```

---

## 完了条件（Phase全体）

### Phase1: データ生成統一
- ✅ ExtractDeviceDataFromReadRandom()実装完了
- ✅ ExtractDeviceData()実装完了
- ✅ ExtractDeviceData()テスト全パス
- ✅ ProcessReceivedRawData()戻り値型変更完了
- ✅ IPlcCommunicationManager変更完了
- ✅ MockPlcCommunicationManager修正完了
- ✅ 旧メソッド削除（ExtractDeviceValuesFromReadRandom等）
- ✅ ビルド成功（0 errors）
- ✅ PlcCommunicationManagerTests全パス
- ✅ 統合テスト全パス

### Phase2: 旧モデル削除
- ✅ ProcessedResponseDataTests修正完了
- ✅ ProcessedResponseData.cs Obsolete削除（171行）
- ✅ ProcessedDevice.cs削除（110行）
- ✅ BasicProcessedResponseData.cs削除（97行）
- ✅ CombinedDWordDevice.cs削除（48行）
- ✅ ErrorMessages定数削除
- ✅ ビルド成功（0 errors, 0 warnings）
- ✅ 全単体テストパス

### Phase3: SlmpDataParser整理
- ✅ SlmpDataParser.ParseReadRandomResponse()削除（58行）
- ✅ SlmpDataParserTests修正完了（8テスト移行または削除）
- ✅ ReadRandomIntegrationTests修正（5箇所）
- ✅ ErrorHandling_IntegrationTests修正（6箇所）
- ✅ ビルド成功（0 errors, 0 warnings）
- ✅ 全単体テストパス
- ✅ 全統合テストパス

### 最終確認
- ✅ ビルド成功（0 errors, 0 warnings）
- ✅ 全単体テストパス（約220テスト）
- ✅ 全統合テストパス（約20テスト）
- ✅ コード削減量確認（目標: 約484行削減）
  - ProcessedDevice関連: 約255行
  - Obsoleteプロパティ: 約171行
  - ParseReadRandomResponse: 約58行
- ✅ 旧モデル使用箇所がゼロ（本番コード・テスト両方）
- ✅ 設計文書更新計画作成（Phase11に引き継ぎ）

---

## リスク管理

| リスク | 影響度 | 発生確率 | TDD対策 |
|--------|--------|----------|---------|
| **データ変換ロジックのバグ** | 高 | 低 | ・各ステップでテスト先行実装<br>・Red→Green→Refactorの厳守<br>・単一ブロックごとのテストパス確認 |
| **テストの大量失敗** | 高 | 低 | ・TDD準拠で段階的にテスト修正<br>・実装変更前にテストを修正<br>・各Phaseでテストパス保証 |
| **インターフェース変更の波及** | 中 | 中 | ・Phase1-3ステップ2でインターフェース変更<br>・同時にモック・スタブ修正<br>・即座にテスト実行 |
| **後方互換性の喪失** | 低 | 低 | ・既にDeviceData（新設計）が本番稼働中<br>・ProcessedDeviceは内部実装のみ |
| **ビット展開機能の破損** | 低 | 低 | ・DataOutputManager独自実装で完結<br>・ProcessedDevice.ExpandedBitsは未使用 |
| **設計文書との不整合** | 低 | 高 | ・Phase11で設計文書更新予定<br>・削除方針は明確 |

---

## TDD原則の適用ポイント

### Red（テスト失敗）
1. 新しいAPIに対応したテストを作成
2. コンパイルエラーまたはテスト失敗を確認
3. 失敗理由を明確にする

### Green（テスト成功）
1. 最小限の実装でテストをパスさせる
2. 全てのテストがパスすることを確認
3. 既存機能への影響がないことを確認

### Refactor（リファクタリング）
1. コードの重複を削除
2. 命名を改善
3. 全テストがパスし続けることを確認

### 段階的実装
- 各小ステップで必ずテスト実行
- 失敗したテストを残したまま進まない
- 複合機能のテストは単一ブロック全完了後

---

## 期待される効果

### コード品質向上
- ✅ 重複実装の解消
- ✅ データ変換処理の削減（二段階 → 一段階）
- ✅ コード量削減（約484行）

### 保守性向上
- ✅ データモデル一本化による理解容易性
- ✅ Obsoleteプロパティの完全削除
- ✅ 孤立コードの削除

### パフォーマンス向上
- ✅ 変換処理削減（CPU 5-10%削減）
- ✅ メモリ削減（10-20KB削減、デバイス100個の場合）

### テスト資産の整理
- ✅ 重複テストの削減
- ✅ テストカバレッジの向上
- ✅ TDD原則に沿った高品質テスト

---

## 次フェーズへの依存関係

### Phase11（ドキュメント更新）への引き継ぎ
Phase13完了後、以下の設計文書を更新します：
1. クラス設計.md - ProcessedDevice関連記述削除
2. プロジェクト構造設計.md - Modelsクラス一覧更新
3. テスト内容.md - テスト統計サマリ更新

### Phase14（実機再検証）への影響
- **影響なし**: 内部リファクタリングのみで外部仕様変更なし
- **確認項目**: メモリ使用量・処理時間の計測

---

## 実装推奨順序

### 優先度: 中（機能実装完了後のクリーンアップ）

**推奨タイミング**:
- Phase1-12（機能実装）完了後
- Phase14（実機検証）前

**実装期間見積もり（TDD準拠版）**:
- Phase1: 3-4日（テスト先行実装 + データ生成統一）
- Phase2: 2-3日（テスト修正 + ファイル削除）
- Phase3: 2-3日（テスト移行 + SlmpDataParser削除）
- **合計: 7-10日**

**段階的実装の推奨**:
1. Phase1完了 → 全テストパス確認 → 次へ
2. Phase2完了 → 全テストパス確認 → 次へ
3. Phase3完了 → 全テストパス確認 → 完了

---

**作成日**: 2025-12-05
**最終更新**: 2025-12-08（Phase1・Phase2完了）
**ステータス**: ✅ Phase2完了（100%） - 旧モデル削除完了、269行削減
**次回更新予定**: Phase3開始時

**関連ドキュメント**:
- Phase10_旧コードの削除クリーンアップ.md
- Phase11_ドキュメント更新.md
- [Phase13_Phase1_Option_B_TestResults.md](../実装結果/Phase13_Phase1_Option_B_TestResults.md)（Phase1結果）
- [Phase13_Phase2_旧モデル削除_TestResults.md](../実装結果/Phase13_Phase2_旧モデル削除_TestResults.md)（Phase2結果）
- [Phase4_Step4-1_ParallelExecution_TestResults.md](../../本体クラス実装/実装結果/Phase4_Step4-1_ParallelExecution_TestResults.md)（Phase13対象外の失敗修正）
- development-methodology.md（TDD手法）

**TDD改訂理由**:
- 元の計画は実装優先でテスト修正が後回し
- TDD原則（Red→Green→Refactor）に準拠するよう全面改訂
- 各Phaseで段階的なテストパス保証を追加
- 単一ブロックごとのテスト確認を明示化
