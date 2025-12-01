# Phase8.5: ProcessedDeviceRequestInfo未初期化エラー恒久対策

## 暫定対策実施状況（2025-12-01完了）

### 実施内容
Phase3.5で削除された`DeviceSpecifications`プロパティを`ProcessedDeviceRequestInfo`に**一時的に再導入**し、ReadRandom(0x0403)コマンドで実機データ取得を可能にした。

### 実施結果
- ✅ **全テストパス**: 新規3テスト + 既存テスト全て合格
- ✅ **ビルド成功**: Main 0 errors, Test 0 errors（84ビルドエラー修正）
- ✅ **実機対応**: `DeviceSpecifications`設定により実機エラー解消
- ✅ **後方互換性**: Read(0x0401)の既存動作を完全維持
- ✅ **TDD厳守**: Red-Green-Refactorサイクル完遂

### テスト実行結果
```
成功!   -失敗:     0、合格:     3、スキップ:     0、合計:     3、期間: 337 ms
```

### 修正ファイル
1. `andon/Core/Models/ProcessedDeviceRequestInfo.cs` - `DeviceSpecifications`プロパティ追加
2. `andon/Core/Controllers/ExecutionOrchestrator.cs` - `DeviceSpecifications`初期化
3. `andon/Core/Managers/PlcCommunicationManager.cs` - `ExtractDeviceValuesFromReadRandom()`追加

### 詳細レポート
📄 `documents/design/read_random実装/実装結果/Phase8_5_暫定対策_TestResults.md`

### 次のステップ
本暫定対策により実機データ取得が可能になったため、Phase12で下記の恒久対策を実施予定。

---

## 概要

### 目的
実機テストで発見された`ProcessedDeviceRequestInfo`未初期化エラーの恒久対策を実施する。

### 背景
- **発見日**: 2025-12-01
- **発見環境**: 実機テスト（PLC: 172.30.40.15:8192, 4Eフレーム, UDP）
- **症状**: `サポートされていないデータ型です: ` エラーが発生し、実機データ取得が完全に不可能
- **暫定対策**: 2025-12-01完了（DeviceSpecificationsプロパティ再導入）

### 重大度
🟡 **Medium** - 暫定対策により実機データ取得は可能（恒久対策でアーキテクチャ改善）

---

## 問題の詳細分析

### 1. 直接的な原因

**ExecutionOrchestrator.cs:199行目**
```csharp
var deviceRequestInfo = new ProcessedDeviceRequestInfo();
```

空の`ProcessedDeviceRequestInfo`を作成しているため、以下のプロパティが未初期化：
- `DeviceType` → `string.Empty`（空文字列）
- `StartAddress` → `0`
- `Count` → `0`
- `FrameType` → デフォルト値

### 2. エラー発生箇所

**PlcCommunicationManager.cs:1919-1941 (ExtractDeviceValues)**
```csharp
switch (requestInfo.DeviceType.ToUpper())
{
    case "D":
        devices.AddRange(ExtractWordDevices(deviceData, requestInfo, processedAt));
        break;
    case "M":
        devices.AddRange(ExtractBitDevices(deviceData, requestInfo, processedAt));
        break;
    default:
        throw new NotSupportedException(string.Format(ErrorMessages.UnsupportedDataType, requestInfo.DeviceType));
}
```

`DeviceType`が空文字列のため、default句に入り`NotSupportedException`がスローされる。

### 3. 根本的な設計上の問題

#### 3.1 コマンド仕様の不一致

| 項目 | Read (0x0401) | ReadRandom (0x0403) |
|------|---------------|---------------------|
| デバイス指定 | 単一デバイス型の連続範囲 | 複数の任意デバイス |
| デバイス型 | 単一（例: D のみ） | 混在可能（例: D, M, X） |
| アドレス | 連続（例: D100-D110） | 任意（例: D100, M200, X10） |
| 現在の設計 | ✅ 対応 | ❌ 不一致 |

#### 3.2 データ構造の不一致

**PlcConfiguration（設定読み込み）**
```csharp
public class PlcConfiguration
{
    // 複数デバイス対応
    public List<DeviceSpecification> Devices { get; set; } = new();
}
```

**ProcessedDeviceRequestInfo（リクエスト情報）**
```csharp
public class ProcessedDeviceRequestInfo
{
    // 単一デバイス型・連続範囲専用
    public string DeviceType { get; set; } = string.Empty;  // 単一デバイス型
    public int StartAddress { get; set; }                   // 開始アドレス
    public int Count { get; set; }                          // 要求デバイス数
}
```

**問題点**:
- `PlcConfiguration.Devices`: `List<DeviceSpecification>` → 複数デバイス、任意アドレス対応
- `ProcessedDeviceRequestInfo`: 単一`DeviceType`/連続アドレス → Read(0x0401)専用設計
- ReadRandom(0x0403)では、複数のデバイス型が混在可能（例: D100, M200, X10）
- この構造的な不一致により、ReadRandomの情報を`ProcessedDeviceRequestInfo`で表現できない

---

## 設計方針

### 1. アーキテクチャ方針

#### オプション1: ReadRandom専用クラス新規作成（推奨）
- **メリット**:
  - 責務が明確
  - 既存コードへの影響最小
  - ReadRandomの仕様に完全対応
- **デメリット**:
  - 新規クラスの追加
  - インターフェース/メソッドシグネチャの拡張が必要

#### オプション2: ProcessedDeviceRequestInfo拡張
- **メリット**:
  - クラス数の増加なし
- **デメリット**:
  - 責務の混在（Read用 vs ReadRandom用）
  - 既存テストへの影響大
  - 下位互換性の考慮が必要

**選択**: オプション1（ReadRandom専用クラス新規作成）

### 2. 新規クラス設計

#### ReadRandomRequestInfo（新規）

```csharp
/// <summary>
/// ReadRandom(0x0403)コマンド用リクエスト情報
/// </summary>
public class ReadRandomRequestInfo
{
    /// <summary>
    /// 読み出し対象デバイス仕様リスト
    /// </summary>
    public List<DeviceSpecification> DeviceSpecifications { get; set; } = new();

    /// <summary>
    /// フレーム型
    /// </summary>
    public FrameType FrameType { get; set; }

    /// <summary>
    /// 要求日時
    /// </summary>
    public DateTime RequestedAt { get; set; }

    /// <summary>
    /// 解析設定（TC037構造化処理用）
    /// </summary>
    public ParseConfiguration? ParseConfiguration { get; set; }

    /// <summary>
    /// PlcConfiguration全体への参照（設定情報アクセス用）
    /// </summary>
    public PlcConfiguration? SourceConfiguration { get; set; }
}
```

### 3. メソッドシグネチャの変更

#### 3.1 ExecuteFullCycleAsync（修正）

**現在**:
```csharp
public async Task<FullCycleExecutionResult> ExecuteFullCycleAsync(
    ConnectionConfig connectionConfig,
    TimeoutConfig timeoutConfig,
    byte[] sendFrame,
    ProcessedDeviceRequestInfo processedRequestInfo,  // ← Read用
    CancellationToken cancellationToken = default)
```

**修正後**:
```csharp
public async Task<FullCycleExecutionResult> ExecuteFullCycleAsync(
    ConnectionConfig connectionConfig,
    TimeoutConfig timeoutConfig,
    byte[] sendFrame,
    ReadRandomRequestInfo readRandomRequestInfo,  // ← ReadRandom用
    CancellationToken cancellationToken = default)
```

#### 3.2 ExtractDeviceValues（修正）

**現在**:
```csharp
private List<ProcessedDevice> ExtractDeviceValues(
    byte[] deviceData,
    ProcessedDeviceRequestInfo requestInfo,  // ← Read用
    DateTime processedAt)
```

**修正後**:
```csharp
private List<ProcessedDevice> ExtractDeviceValues(
    byte[] deviceData,
    ReadRandomRequestInfo requestInfo,  // ← ReadRandom用
    DateTime processedAt)
```

**実装方針**:
```csharp
private List<ProcessedDevice> ExtractDeviceValues(byte[] deviceData, ReadRandomRequestInfo requestInfo, DateTime processedAt)
{
    var devices = new List<ProcessedDevice>();
    int offset = 0;

    // ReadRandomでは各デバイスを個別に解析
    foreach (var deviceSpec in requestInfo.DeviceSpecifications)
    {
        // デバイス型に応じた処理
        switch (deviceSpec.Unit.ToLower())
        {
            case "word":
                // 2バイト読み出し
                if (offset + 2 <= deviceData.Length)
                {
                    var wordValue = BitConverter.ToUInt16(deviceData, offset);
                    devices.Add(new ProcessedDevice
                    {
                        DeviceType = deviceSpec.DeviceType,
                        DeviceNumber = deviceSpec.DeviceNumber,
                        Value = wordValue,
                        ProcessedAt = processedAt
                    });
                    offset += 2;
                }
                break;

            case "bit":
                // 2バイト読み出し（ビット型もワード単位で返される）
                if (offset + 2 <= deviceData.Length)
                {
                    var bitValue = BitConverter.ToUInt16(deviceData, offset);
                    devices.Add(new ProcessedDevice
                    {
                        DeviceType = deviceSpec.DeviceType,
                        DeviceNumber = deviceSpec.DeviceNumber,
                        Value = bitValue & 0x01,  // 最下位ビットのみ
                        ProcessedAt = processedAt
                    });
                    offset += 2;
                }
                break;

            case "dword":
                // 4バイト読み出し
                if (offset + 4 <= deviceData.Length)
                {
                    var dwordValue = BitConverter.ToUInt32(deviceData, offset);
                    devices.Add(new ProcessedDevice
                    {
                        DeviceType = deviceSpec.DeviceType,
                        DeviceNumber = deviceSpec.DeviceNumber,
                        Value = dwordValue,
                        ProcessedAt = processedAt
                    });
                    offset += 4;
                }
                break;

            default:
                throw new NotSupportedException($"サポートされていない単位です: {deviceSpec.Unit}");
        }
    }

    return devices;
}
```

---

## 実装計画（TDD準拠）

### TDD実施方針

各Phaseで以下のTDDサイクルを厳守：

1. **🔴 Red**: 失敗するテストを先に書く
2. **🟢 Green**: テストをパスする最小限の実装
3. **🔵 Refactor**: コードをリファクタリング
4. **✅ Verify**: 全テストが依然としてパスすることを確認

---

### Phase 8.5.1: ReadRandomRequestInfo実装（TDD）

#### ステップ1: 🔴 Red - テスト作成

**作業内容**:
1. `ReadRandomRequestInfoTests.cs`を作成
2. 以下のテストケースを実装（全て失敗することを確認）:
   - `Constructor_デフォルト値_正しく初期化される()`
   - `DeviceSpecifications_複数デバイス_設定可能()`
   - `FrameType_設定_取得可能()`
   - `RequestedAt_設定_取得可能()`
   - `ParseConfiguration_Null許容_設定可能()`
   - `SourceConfiguration_Null許容_設定可能()`

**確認**:
```bash
dotnet test --filter "FullyQualifiedName~ReadRandomRequestInfoTests"
```
→ 全テスト失敗（クラスが存在しないため）

#### ステップ2: 🟢 Green - 最小実装

**作業内容**:
1. `ReadRandomRequestInfo.cs`を作成
2. テストをパスする最小限の実装:
   - プロパティのみ実装
   - ロジックなし

**確認**:
```bash
dotnet test --filter "FullyQualifiedName~ReadRandomRequestInfoTests"
```
→ 全テストパス

#### ステップ3: 🔵 Refactor - リファクタリング

**作業内容**:
1. XMLドキュメントコメント追加
2. プロパティの初期化方法見直し
3. コードスタイル統一

**確認**:
```bash
dotnet test --filter "FullyQualifiedName~ReadRandomRequestInfoTests"
```
→ 全テスト依然としてパス

**成果物**:
- `Tests/Unit/Core/Models/ReadRandomRequestInfoTests.cs` ✅
- `andon/Core/Models/ReadRandomRequestInfo.cs` ✅

**期待結果**:
- ✅ TDDサイクル完了
- ✅ 単体テスト全てパス
- ✅ コードカバレッジ100%

---

### Phase 8.5.2: ExecutionOrchestrator修正（TDD）

#### ステップ1: 🔴 Red - テスト作成

**作業内容**:
1. `ExecutionOrchestratorTests.cs`に新規テスト追加:
   - `ExecuteCycleAsync_PlcConfiguration_ReadRandomRequestInfo生成()`
   - `ExecuteCycleAsync_ReadRandomRequestInfo_正しく初期化()`
   - `ExecuteCycleAsync_空のProcessedDeviceRequestInfo_作成されない()`
2. テスト実行 → 失敗確認（現在は空のインスタンスを作成している）

**確認**:
```bash
dotnet test --filter "FullyQualifiedName~ExecutionOrchestratorTests"
```
→ 新規テスト失敗

#### ステップ2: 🟢 Green - 最小実装

**作業内容**:
1. `ExecutionOrchestrator.cs`:199行目を修正:
```csharp
// 修正前
var deviceRequestInfo = new ProcessedDeviceRequestInfo();

// 修正後
var readRandomRequestInfo = new ReadRandomRequestInfo
{
    DeviceSpecifications = config.Devices,
    FrameType = config.IsBinary ? FrameType.Frame4E : FrameType.Frame4E,
    RequestedAt = DateTime.UtcNow,
    SourceConfiguration = config
};
```
2. `ExecuteFullCycleAsync()`呼び出し箇所を修正

**確認**:
```bash
dotnet test --filter "FullyQualifiedName~ExecutionOrchestratorTests"
```
→ 全テストパス

#### ステップ3: 🔵 Refactor - リファクタリング

**作業内容**:
1. `ReadRandomRequestInfo`生成ロジックをprivateメソッドに抽出:
```csharp
private ReadRandomRequestInfo CreateReadRandomRequestInfo(PlcConfiguration config)
{
    return new ReadRandomRequestInfo
    {
        DeviceSpecifications = config.Devices,
        FrameType = config.IsBinary ? FrameType.Frame4E : FrameType.Frame4E,
        RequestedAt = DateTime.UtcNow,
        SourceConfiguration = config
    };
}
```
2. エラーハンドリング追加

**確認**:
```bash
dotnet test --filter "FullyQualifiedName~ExecutionOrchestratorTests"
```
→ 全テスト依然としてパス

**成果物**:
- `Tests/Unit/Core/Controllers/ExecutionOrchestratorTests.cs` ✅（修正）
- `andon/Core/Controllers/ExecutionOrchestrator.cs` ✅（修正）

**期待結果**:
- ✅ TDDサイクル完了
- ✅ 空のインスタンス生成が解消
- ✅ 既存テスト全てパス

---

### Phase 8.5.3: PlcCommunicationManager修正（TDD）

#### ステップ1: 🔴 Red - テスト作成

**作業内容**:
1. `PlcCommunicationManagerTests.cs`に新規テスト追加:
   - `ExtractDeviceValues_単一Word型_正しく解析()`
   - `ExtractDeviceValues_単一Bit型_正しく解析()`
   - `ExtractDeviceValues_単一DWord型_4バイト読み出し()`
   - `ExtractDeviceValues_複数デバイス型混在_正しく解析()`
   - `ExtractDeviceValues_未対応Unit_NotSupportedException()`
2. テスト実行 → 失敗確認（現在はProcessedDeviceRequestInfoを使用）

**確認**:
```bash
dotnet test --filter "FullyQualifiedName~PlcCommunicationManagerTests.ExtractDeviceValues"
```
→ 全テスト失敗

#### ステップ2: 🟢 Green - 最小実装

**作業内容**:
1. `ExecuteFullCycleAsync()`シグネチャ変更:
```csharp
// 修正前
public async Task<FullCycleExecutionResult> ExecuteFullCycleAsync(
    ConnectionConfig connectionConfig,
    TimeoutConfig timeoutConfig,
    byte[] sendFrame,
    ProcessedDeviceRequestInfo processedRequestInfo,
    CancellationToken cancellationToken = default)

// 修正後
public async Task<FullCycleExecutionResult> ExecuteFullCycleAsync(
    ConnectionConfig connectionConfig,
    TimeoutConfig timeoutConfig,
    byte[] sendFrame,
    ReadRandomRequestInfo readRandomRequestInfo,
    CancellationToken cancellationToken = default)
```

2. `ExtractDeviceValues()`実装変更:
```csharp
private List<ProcessedDevice> ExtractDeviceValues(byte[] deviceData, ReadRandomRequestInfo requestInfo, DateTime processedAt)
{
    var devices = new List<ProcessedDevice>();
    int offset = 0;

    foreach (var deviceSpec in requestInfo.DeviceSpecifications)
    {
        switch (deviceSpec.Unit.ToLower())
        {
            case "word":
                if (offset + 2 <= deviceData.Length)
                {
                    var wordValue = BitConverter.ToUInt16(deviceData, offset);
                    devices.Add(new ProcessedDevice
                    {
                        DeviceType = deviceSpec.DeviceType,
                        DeviceNumber = deviceSpec.DeviceNumber,
                        Value = wordValue,
                        ProcessedAt = processedAt
                    });
                    offset += 2;
                }
                break;

            case "bit":
                if (offset + 2 <= deviceData.Length)
                {
                    var bitValue = BitConverter.ToUInt16(deviceData, offset);
                    devices.Add(new ProcessedDevice
                    {
                        DeviceType = deviceSpec.DeviceType,
                        DeviceNumber = deviceSpec.DeviceNumber,
                        Value = bitValue & 0x01,
                        ProcessedAt = processedAt
                    });
                    offset += 2;
                }
                break;

            case "dword":
                if (offset + 4 <= deviceData.Length)
                {
                    var dwordValue = BitConverter.ToUInt32(deviceData, offset);
                    devices.Add(new ProcessedDevice
                    {
                        DeviceType = deviceSpec.DeviceType,
                        DeviceNumber = deviceSpec.DeviceNumber,
                        Value = dwordValue,
                        ProcessedAt = processedAt
                    });
                    offset += 4;
                }
                break;

            default:
                throw new NotSupportedException($"サポートされていない単位です: {deviceSpec.Unit}");
        }
    }

    return devices;
}
```

3. 関連メソッド修正:
   - `ProcessReceivedRawData()`
   - `ParseRawToStructuredData()`

**確認**:
```bash
dotnet test --filter "FullyQualifiedName~PlcCommunicationManagerTests.ExtractDeviceValues"
```
→ 全テストパス

```bash
dotnet test --filter "FullyQualifiedName~PlcCommunicationManagerTests"
```
→ 既存テスト全てパス

#### ステップ3: 🔵 Refactor - リファクタリング

**作業内容**:
1. 重複コードの抽出（offset管理、バイト読み出し）
2. エラーハンドリング強化
3. ログ出力追加

**確認**:
```bash
dotnet test --filter "FullyQualifiedName~PlcCommunicationManagerTests"
```
→ 全テスト依然としてパス

**成果物**:
- `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs` ✅（修正）
- `andon/Core/Managers/PlcCommunicationManager.cs` ✅（修正）

**期待結果**:
- ✅ TDDサイクル完了
- ✅ ReadRandomコマンドで複数デバイス型対応
- ✅ `NotSupportedException`エラー解消
- ✅ 既存テスト全てパス

---

### Phase 8.5.4: IPlcCommunicationManager修正（TDD）

#### ステップ1: 🔴 Red - テスト作成

**作業内容**:
1. `MockPlcCommunicationManagerTests.cs`を作成
2. Mock実装がインターフェースに準拠することを確認するテスト追加

**確認**:
```bash
dotnet test --filter "FullyQualifiedName~MockPlcCommunicationManagerTests"
```
→ テスト失敗（インターフェース不一致）

#### ステップ2: 🟢 Green - 最小実装

**作業内容**:
1. `IPlcCommunicationManager.cs`のインターフェース定義変更
2. `MockPlcCommunicationManager.cs`のMock実装修正

**確認**:
```bash
dotnet test --filter "FullyQualifiedName~MockPlcCommunicationManagerTests"
```
→ 全テストパス

#### ステップ3: 🔵 Refactor - リファクタリング

**作業内容**:
1. XMLドキュメントコメント更新
2. Mock実装の柔軟性向上

**確認**:
```bash
dotnet test --filter "FullyQualifiedName~MockPlcCommunicationManagerTests"
```
→ 全テスト依然としてパス

**成果物**:
- `andon/Core/Interfaces/IPlcCommunicationManager.cs` ✅（修正）
- `Tests/TestUtilities/Mocks/MockPlcCommunicationManager.cs` ✅（修正）
- `Tests/Unit/Mocks/MockPlcCommunicationManagerTests.cs` ✅（新規）

**期待結果**:
- ✅ TDDサイクル完了
- ✅ インターフェース整合性確保
- ✅ Mock実装がテストで使用可能

---

### Phase 8.5.5: 統合テスト（TDD）

#### ステップ1: 🔴 Red - 統合テスト作成

**作業内容**:
1. `Phase8_5_IntegrationTests.cs`を作成
2. 以下のテストケースを実装:
   - `TC8_5_1_単一デバイス型_D_のみ_成功()`
   - `TC8_5_2_複数デバイス型混在_D_M_成功()`
   - `TC8_5_3_全デバイス型混在_D_M_X_Y_W_成功()`
   - `TC8_5_4_DWord型デバイス_4バイト読み出し_成功()`
   - `TC8_5_5_エラーケース_未対応Unit_NotSupportedException()`

**確認**:
```bash
dotnet test --filter "FullyQualifiedName~Phase8_5_IntegrationTests"
```
→ テスト失敗確認（統合が未完了のため）

#### ステップ2: 🟢 Green - 統合修正

**作業内容**:
1. 各コンポーネント間の統合を確認
2. 必要に応じて微調整

**確認**:
```bash
dotnet test --filter "FullyQualifiedName~Phase8_5_IntegrationTests"
```
→ 全テストパス

#### ステップ3: 🔵 Refactor - 統合最適化

**作業内容**:
1. テストデータの共通化
2. テストヘルパーメソッドの抽出

**確認**:
```bash
dotnet test --filter "FullyQualifiedName~Phase8_5_IntegrationTests"
```
→ 全テスト依然としてパス

**成果物**:
- `Tests/Integration/Phase8_5_IntegrationTests.cs` ✅

**期待結果**:
- ✅ TDDサイクル完了
- ✅ 全統合テストパス
- ✅ 実機データ取得が正常動作（モック環境）

---

### 最終検証（Phase 8.5完了時）

#### 全テスト実行

```bash
# 単体テスト
dotnet test --filter "FullyQualifiedName~ReadRandomRequestInfoTests"
dotnet test --filter "FullyQualifiedName~ExecutionOrchestratorTests"
dotnet test --filter "FullyQualifiedName~PlcCommunicationManagerTests"
dotnet test --filter "FullyQualifiedName~MockPlcCommunicationManagerTests"

# 統合テスト
dotnet test --filter "FullyQualifiedName~Phase8_5_IntegrationTests"

# 全テスト
dotnet test
```

#### 成功基準

- [ ] **全単体テストパス**: Phase 8.5.1～8.5.4の全テストが成功
- [ ] **全統合テストパス**: Phase 8.5.5の全テストが成功
- [ ] **既存テストパス**: Phase 8.5以前の全テストが引き続き成功
- [ ] **コードカバレッジ**: 新規コードのカバレッジ80%以上
- [ ] **ビルド成功**: `dotnet build`が警告なしで成功

---

### TDD実施時の注意事項

#### 1. テストファースト厳守
- **絶対に**実装コードを先に書かない
- テストが失敗することを確認してから実装開始

#### 2. 最小限の実装
- テストをパスする最小限のコードのみ実装
- 将来の拡張を考慮した過剰な実装は避ける

#### 3. リファクタリングの安全性
- リファクタリング前後でテストが全てパスすることを確認
- テストコードもリファクタリング対象

#### 4. 継続的なテスト実行
- コード変更の度にテスト実行
- CI/CDパイプラインでの自動テスト

#### 5. テストの独立性
- 各テストは独立して実行可能
- テスト間の依存関係を作らない

---

### TDDサイクル確認チェックリスト

各Phase完了時に以下を確認：

#### Phase 8.5.1
- [ ] 🔴 Red: テスト作成完了、全テスト失敗確認
- [ ] 🟢 Green: 最小実装完了、全テストパス
- [ ] 🔵 Refactor: リファクタリング完了、全テスト依然としてパス
- [ ] ✅ Verify: 最終確認、コードカバレッジ確認

#### Phase 8.5.2
- [ ] 🔴 Red: テスト作成完了、新規テスト失敗確認
- [ ] 🟢 Green: 最小実装完了、全テストパス
- [ ] 🔵 Refactor: リファクタリング完了、全テスト依然としてパス
- [ ] ✅ Verify: 最終確認、既存テストパス確認

#### Phase 8.5.3
- [ ] 🔴 Red: テスト作成完了、新規テスト失敗確認
- [ ] 🟢 Green: 最小実装完了、全テストパス
- [ ] 🔵 Refactor: リファクタリング完了、全テスト依然としてパス
- [ ] ✅ Verify: 最終確認、既存テストパス確認

#### Phase 8.5.4
- [ ] 🔴 Red: テスト作成完了、新規テスト失敗確認
- [ ] 🟢 Green: 最小実装完了、全テストパス
- [ ] 🔵 Refactor: リファクタリング完了、全テスト依然としてパス
- [ ] ✅ Verify: 最終確認、Mock動作確認

#### Phase 8.5.5
- [ ] 🔴 Red: 統合テスト作成完了、テスト失敗確認
- [ ] 🟢 Green: 統合修正完了、全テストパス
- [ ] 🔵 Refactor: 統合最適化完了、全テスト依然としてパス
- [ ] ✅ Verify: 最終確認、エンドツーエンドテスト成功

---

## テスト計画

### 1. 単体テスト

#### ReadRandomRequestInfoTests
```csharp
[Fact]
public void Constructor_デフォルト値_正しく初期化される()
{
    // Arrange & Act
    var requestInfo = new ReadRandomRequestInfo();

    // Assert
    Assert.NotNull(requestInfo.DeviceSpecifications);
    Assert.Empty(requestInfo.DeviceSpecifications);
    Assert.Equal(default(DateTime), requestInfo.RequestedAt);
    Assert.Null(requestInfo.ParseConfiguration);
    Assert.Null(requestInfo.SourceConfiguration);
}

[Fact]
public void DeviceSpecifications_複数デバイス_設定可能()
{
    // Arrange
    var devices = new List<DeviceSpecification>
    {
        new DeviceSpecification(DeviceCode.D, 100),
        new DeviceSpecification(DeviceCode.M, 200),
        new DeviceSpecification(DeviceCode.X, 0x10)
    };

    // Act
    var requestInfo = new ReadRandomRequestInfo
    {
        DeviceSpecifications = devices
    };

    // Assert
    Assert.Equal(3, requestInfo.DeviceSpecifications.Count);
}
```

#### ExecutionOrchestratorTests（修正）
```csharp
[Fact]
public async Task ExecuteCycleAsync_PlcConfiguration_ReadRandomRequestInfo生成()
{
    // Arrange
    var config = new PlcConfiguration
    {
        Devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100) { Unit = "word" },
            new DeviceSpecification(DeviceCode.M, 200) { Unit = "bit" }
        }
    };

    // Act
    // ExecuteCycleAsync内でReadRandomRequestInfo生成を確認

    // Assert
    // ReadRandomRequestInfoが正しく生成されることを確認
}
```

#### PlcCommunicationManagerTests（修正）
```csharp
[Fact]
public async Task ExtractDeviceValues_複数デバイス型_正しく解析()
{
    // Arrange
    var requestInfo = new ReadRandomRequestInfo
    {
        DeviceSpecifications = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100) { Unit = "word" },
            new DeviceSpecification(DeviceCode.M, 200) { Unit = "bit" }
        }
    };

    var deviceData = new byte[] { 0x12, 0x34, 0x01, 0x00 };  // D100=0x3412, M200=1

    // Act
    var devices = ExtractDeviceValues(deviceData, requestInfo, DateTime.UtcNow);

    // Assert
    Assert.Equal(2, devices.Count);
    Assert.Equal("D", devices[0].DeviceType);
    Assert.Equal(100, devices[0].DeviceNumber);
    Assert.Equal(0x3412, devices[0].Value);
    Assert.Equal("M", devices[1].DeviceType);
    Assert.Equal(200, devices[1].DeviceNumber);
    Assert.Equal(1, devices[1].Value);
}

[Fact]
public async Task ExtractDeviceValues_DWord型_4バイト読み出し()
{
    // Arrange
    var requestInfo = new ReadRandomRequestInfo
    {
        DeviceSpecifications = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100) { Unit = "dword" }
        }
    };

    var deviceData = new byte[] { 0x12, 0x34, 0x56, 0x78 };  // 0x78563412

    // Act
    var devices = ExtractDeviceValues(deviceData, requestInfo, DateTime.UtcNow);

    // Assert
    Assert.Single(devices);
    Assert.Equal(0x78563412u, devices[0].Value);
}
```

### 2. 統合テスト

#### Phase8_5_IntegrationTests
```csharp
[Fact]
public async Task FullCycle_ReadRandom_複数デバイス型混在_成功()
{
    // Arrange
    var config = CreateTestConfiguration();
    var orchestrator = CreateOrchestrator();

    // Act
    var result = await orchestrator.ExecuteCycleAsync(config, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.ProcessedData);
    Assert.True(result.ProcessedData.BasicProcessedDevices.Count > 0);
}

[Fact]
public async Task FullCycle_ReadRandom_エラーケース_適切なエラー()
{
    // Arrange
    var config = CreateInvalidConfiguration();  // 未対応Unit指定

    // Act & Assert
    await Assert.ThrowsAsync<NotSupportedException>(() =>
        orchestrator.ExecuteCycleAsync(config, CancellationToken.None));
}
```

---

## 影響範囲

### 直接影響を受けるクラス

| クラス | 影響内容 | 対応 |
|--------|---------|------|
| `ExecutionOrchestrator` | `ReadRandomRequestInfo`生成に変更 | 修正 |
| `PlcCommunicationManager` | メソッドシグネチャ変更 | 修正 |
| `IPlcCommunicationManager` | インターフェース定義変更 | 修正 |
| `MockPlcCommunicationManager` | Mock実装変更 | 修正 |
| 各種テストクラス | テストコード修正 | 修正 |

### 間接影響を受けるクラス

| クラス | 影響内容 | 対応 |
|--------|---------|------|
| `ProcessedDeviceRequestInfo` | 使用箇所の確認 | Read用として残す |
| `DataOutputManager` | 影響なし | 変更なし |
| `ConfigToFrameManager` | 影響なし | 変更なし |

---

## リスクと対策

### リスク1: 既存テストの大規模修正
- **リスク**: テストコードの修正範囲が広範囲
- **対策**:
  - Phase 8.5.2で段階的に修正
  - 各Phase毎にテスト実行・確認
  - CI/CDパイプラインでの自動テスト

### リスク2: 実機テストでの予期しない動作
- **リスク**: モック環境と実機環境での動作差異
- **対策**:
  - Phase 8.5.5での徹底的な統合テスト
  - 実機テストは別途Phase9で実施
  - ログ出力の強化

### リスク3: ProcessedDeviceRequestInfo の既存用途
- **リスク**: Read(0x0401)用として使われている箇所が残っている可能性
- **対策**:
  - コードベース全体でのGrep検索
  - 使用箇所の特定と影響分析
  - 必要に応じて両対応の設計

---

## 成功基準

### Phase 8.5.1
- [ ] `ReadRandomRequestInfo.cs`作成完了
- [ ] 単体テスト全てパス

### Phase 8.5.2
- [ ] `ExecutionOrchestrator.cs`修正完了
- [ ] 空のインスタンス生成が解消
- [ ] 既存テスト全てパス

### Phase 8.5.3
- [ ] `PlcCommunicationManager.cs`修正完了
- [ ] `NotSupportedException`エラー解消
- [ ] 既存テスト全てパス

### Phase 8.5.4
- [ ] `IPlcCommunicationManager.cs`修正完了
- [ ] Mock実装修正完了

### Phase 8.5.5
- [ ] 統合テスト全てパス
- [ ] 実機データ取得が正常動作（モック環境）

---

## スケジュール（TDD準拠）

| Phase | 作業内容 | TDDステップ | 見積もり |
|-------|---------|------------|---------|
| 8.5.1 | ReadRandomRequestInfo実装 | 🔴Red → 🟢Green → 🔵Refactor → ✅Verify | 1ステップ |
| 8.5.2 | ExecutionOrchestrator修正 | 🔴Red → 🟢Green → 🔵Refactor → ✅Verify | 1ステップ |
| 8.5.3 | PlcCommunicationManager修正 | 🔴Red → 🟢Green → 🔵Refactor → ✅Verify | 2ステップ |
| 8.5.4 | Interface/Mock修正 | 🔴Red → 🟢Green → 🔵Refactor → ✅Verify | 1ステップ |
| 8.5.5 | 統合テスト | 🔴Red → 🟢Green → 🔵Refactor → ✅Verify | 1ステップ |
| **合計** | | | **6ステップ** |

### 各ステップの詳細時間

| フェーズ | Red | Green | Refactor | Verify | 合計 |
|---------|-----|-------|----------|--------|------|
| 8.5.1 | テスト作成 | 最小実装 | リファクタ | 検証 | 1ステップ |
| 8.5.2 | テスト作成 | 最小実装 | リファクタ | 検証 | 1ステップ |
| 8.5.3 | テスト作成 | 最小実装 | リファクタ | 検証 | 2ステップ |
| 8.5.4 | テスト作成 | 最小実装 | リファクタ | 検証 | 1ステップ |
| 8.5.5 | テスト作成 | 統合修正 | 最適化 | E2Eテスト | 1ステップ |

**注意**:
- 各ステップは、TDDサイクル（Red-Green-Refactor-Verify）を完全に完了してから次へ進む
- テストが失敗することを確認してから実装を開始
- 実装後は必ずリファクタリングを実施

---

## 次のステップ

Phase 8.5完了後:
1. **Phase 9**: 実機テスト実施
2. **Phase 10**: パフォーマンス最適化
3. **Phase 11**: エラーハンドリング強化
4. **Phase 12**: ProcessedDeviceRequestInfo 完全廃止検討

---

## 参考資料

### 関連ドキュメント
- `documents/design/read_random実装/`
- `documents/design/エラーハンドリング.md`
- `CLAUDE.md` (プロジェクト構造)

### 関連Issue
- ProcessedDeviceRequestInfo未初期化エラー（2025-12-01発見）

### SLMP仕様書
- ReadRandom(0x0403): SLMP仕様書 page_64.png
- Read(0x0401): SLMP仕様書 (該当ページ)

---

## 変更履歴

| 日付 | バージョン | 変更内容 | 担当 |
|------|-----------|---------|------|
| 2025-12-01 | 1.0 | 初版作成 | Claude Code |
