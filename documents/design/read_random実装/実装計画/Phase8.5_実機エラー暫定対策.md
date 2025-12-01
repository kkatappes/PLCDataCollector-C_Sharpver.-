# Phase8.5: 実機エラー暫定対策（ProcessedDeviceRequestInfo未初期化問題）

**作成日**: 2025-12-01
**対応優先度**: 🔴 Critical
**Phase12根本対策への準備フェーズ**

---

## 1. 問題の概要

### 1.1 発見された問題

**エラー症状**:
```
サポートされていないデータ型です:
```

**発生箇所**:
- `PlcCommunicationManager.ExtractDeviceValues()` (line 1919-1941)
- エラー発生行: line 1938

**発生環境**:
- 実機テスト: PLC 172.30.40.15:8192
- フレームタイプ: 4Eフレーム
- 通信プロトコル: UDP

### 1.2 根本原因

`ExecutionOrchestrator.cs`:199行目で空の`ProcessedDeviceRequestInfo`を作成:

```csharp
var deviceRequestInfo = new ProcessedDeviceRequestInfo();
// ↑ すべてのプロパティがデフォルト値のまま
```

**未初期化プロパティ**:
- `DeviceType`: 空文字列 ("")
- `StartAddress`: 0
- `Count`: 0
- `FrameType`: デフォルト値
- `DeviceSpecifications`: Phase3.5で削除済み（⚠️ 致命的）

### 1.3 アーキテクチャの矛盾

**設計ミスマッチ**:

| 項目 | ReadRandom(0x0403)の仕様 | 現在の`ProcessedDeviceRequestInfo` |
|------|--------------------------|-----------------------------------|
| デバイス指定 | 複数の任意デバイス | 単一`DeviceType` + 連続範囲 |
| デバイス型 | 混在可能（D, M, X混合） | 単一型のみ |
| アドレス | 不連続OK | `StartAddress` + `Count`の連続範囲 |
| 用途 | ReadRandom専用 | 旧Read(0x0401)の設計を流用 |

**PlcConfigurationとの不整合**:
```csharp
// PlcConfiguration: ReadRandomに対応（複数デバイス）
public List<DeviceSpecification> Devices { get; set; }

// ProcessedDeviceRequestInfo: 旧Read設計（単一デバイス型）
public string DeviceType { get; set; }  // ← 矛盾
public int StartAddress { get; set; }   // ← 矛盾
public int Count { get; set; }          // ← 矛盾
```

---

## 2. 暫定対策の方針

### 2.1 選択したアプローチ: Option 1

**DeviceSpecificationsプロパティを再追加**

**理由**:
1. ✅ ReadRandomの本質的な設計に合致（複数デバイス指定）
2. ✅ Phase12根本対策への移行が容易
3. ✅ `PlcConfiguration.Devices`と整合性が取れる
4. ✅ 既存のテストコードとの互換性を維持しやすい

**トレードオフ**:
- ⚠️ Phase3.5で一度削除したプロパティの復活（設計の後退）
- ⚠️ `ExtractDeviceValues()`の大幅な修正が必要
- ✅ しかし、Phase12で専用クラスへの移行がスムーズ

### 2.2 Phase12根本対策への位置づけ

**Phase8.5（暫定対策）**:
```csharp
// ProcessedDeviceRequestInfo（暫定的に拡張）
public List<DeviceSpecification>? DeviceSpecifications { get; set; }
```

**Phase12（根本対策）**:
```csharp
// 新設計: ReadRandomRequestInfo（専用クラス）
public class ReadRandomRequestInfo
{
    public List<DeviceSpecification> Devices { get; set; }
    public FrameType FrameType { get; set; }
    public DateTime RequestedAt { get; set; }
}
```

---

## 3. TDD実装計画

### 3.1 実装方針

**TDDサイクル**: Red → Green → Refactor

**テスト駆動の原則**:
1. ❌ **Red**: 失敗するテストを先に書く
2. ✅ **Green**: 最小限のコードでテストを通す
3. ♻️ **Refactor**: コードを整理・改善

### 3.2 実装ステップ

#### Step 1: モデル修正（TDD: Model Layer）

**📝 テストファイル**: `ProcessedDeviceRequestInfoTests.cs`

**Test Case 1-1**: DeviceSpecificationsプロパティの追加
```csharp
[Fact]
public void DeviceSpecifications_Should_BeNullableList()
{
    // Arrange & Act
    var info = new ProcessedDeviceRequestInfo
    {
        DeviceSpecifications = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100),
            new DeviceSpecification(DeviceCode.M, 200)
        }
    };

    // Assert
    Assert.NotNull(info.DeviceSpecifications);
    Assert.Equal(2, info.DeviceSpecifications.Count);
    Assert.Equal(DeviceCode.D, info.DeviceSpecifications[0].Code);
}
```

**Test Case 1-2**: 後方互換性の確認（nullでも動作）
```csharp
[Fact]
public void DeviceSpecifications_Should_AllowNull_ForBackwardCompatibility()
{
    // Arrange & Act
    var info = new ProcessedDeviceRequestInfo
    {
        DeviceType = "D",
        StartAddress = 100,
        Count = 10
    };

    // Assert
    Assert.Null(info.DeviceSpecifications);
    Assert.Equal("D", info.DeviceType); // 既存プロパティは残す
}
```

**🔨 実装内容**:
```csharp
// ProcessedDeviceRequestInfo.cs
public List<DeviceSpecification>? DeviceSpecifications { get; set; }
```

---

#### Step 2: ExecutionOrchestrator修正（TDD: Controller Layer）

**📝 テストファイル**: `ExecutionOrchestratorTests.cs`

**Test Case 2-1**: DeviceSpecificationsの正しい設定
```csharp
[Fact]
public async Task ExecuteSingleCycleAsync_Should_SetDeviceSpecifications_FromPlcConfiguration()
{
    // Arrange
    var config = new PlcConfiguration
    {
        IpAddress = "172.30.40.15",
        Port = 8192,
        Devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100),
            new DeviceSpecification(DeviceCode.M, 200)
        }
    };

    var mockCommManager = new MockPlcCommunicationManager();
    ProcessedDeviceRequestInfo? capturedRequestInfo = null;

    mockCommManager.OnExecuteFullCycleAsync = (conn, timeout, frame, requestInfo, ct) =>
    {
        capturedRequestInfo = requestInfo;
        return Task.FromResult(new CycleExecutionResult { IsSuccess = true });
    };

    var orchestrator = new ExecutionOrchestrator(
        new[] { config },
        mockCommManager,
        mockLogging,
        mockError,
        mockOutput
    );

    // Act
    await orchestrator.ExecuteSingleCycleAsync(CancellationToken.None);

    // Assert
    Assert.NotNull(capturedRequestInfo);
    Assert.NotNull(capturedRequestInfo.DeviceSpecifications);
    Assert.Equal(2, capturedRequestInfo.DeviceSpecifications.Count);
    Assert.Equal(DeviceCode.D, capturedRequestInfo.DeviceSpecifications[0].Code);
    Assert.Equal(100, capturedRequestInfo.DeviceSpecifications[0].DeviceNumber);
}
```

**🔨 実装内容**:
```csharp
// ExecutionOrchestrator.cs (line 199付近)
var deviceRequestInfo = new ProcessedDeviceRequestInfo
{
    DeviceSpecifications = config.Devices?.ToList(), // ← PlcConfigurationから設定
    FrameType = config.FrameVersion == "4E" ? FrameType.Frame4E : FrameType.Frame3E,
    RequestedAt = DateTime.UtcNow
};
```

---

#### Step 3: ExtractDeviceValues修正（TDD: Service Layer）

**📝 テストファイル**: `PlcCommunicationManagerTests.cs`

**Test Case 3-1**: ReadRandomレスポンスの正しい処理
```csharp
[Fact]
public void ExtractDeviceValues_Should_ProcessReadRandomResponse_WithMultipleDevices()
{
    // Arrange
    var responseData = new byte[]
    {
        0x96, 0x00,  // D100 = 150 (LE)
        0x01, 0x00,  // M200 = 1 (word形式、下位バイトが1)
    };

    var requestInfo = new ProcessedDeviceRequestInfo
    {
        DeviceSpecifications = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100),
            new DeviceSpecification(DeviceCode.M, 200)
        },
        FrameType = FrameType.Frame4E,
        RequestedAt = DateTime.UtcNow
    };

    var manager = new PlcCommunicationManager(mockLogging, mockError, mockResource);

    // Act
    var result = manager.ExtractDeviceValues(responseData, requestInfo, DateTime.UtcNow);

    // Assert
    Assert.Equal(2, result.Count);

    Assert.Equal("D", result[0].DeviceType);
    Assert.Equal(100, result[0].Address);
    Assert.Equal(150, result[0].Value);

    Assert.Equal("M", result[1].DeviceType);
    Assert.Equal(200, result[1].Address);
    Assert.Equal(1, result[1].Value);
}
```

**Test Case 3-2**: DeviceSpecificationsがnullの場合の後方互換性
```csharp
[Fact]
public void ExtractDeviceValues_Should_FallbackToLegacyMode_WhenDeviceSpecificationsIsNull()
{
    // Arrange
    var responseData = new byte[]
    {
        0x96, 0x00,  // D100 = 150
        0x97, 0x00   // D101 = 151
    };

    var requestInfo = new ProcessedDeviceRequestInfo
    {
        DeviceSpecifications = null, // ← nullの場合
        DeviceType = "D",            // ← 既存プロパティを使用
        StartAddress = 100,
        Count = 2,
        FrameType = FrameType.Frame3E,
        RequestedAt = DateTime.UtcNow
    };

    var manager = new PlcCommunicationManager(mockLogging, mockError, mockResource);

    // Act
    var result = manager.ExtractDeviceValues(responseData, requestInfo, DateTime.UtcNow);

    // Assert
    Assert.Equal(2, result.Count);
    Assert.Equal("D", result[0].DeviceType);
    Assert.Equal(100, result[0].Address);
}
```

**🔨 実装内容**:
```csharp
// PlcCommunicationManager.cs (line 1919-1941)
private List<ProcessedDevice> ExtractDeviceValues(
    byte[] deviceData,
    ProcessedDeviceRequestInfo requestInfo,
    DateTime processedAt)
{
    var devices = new List<ProcessedDevice>();

    // Phase8.5暫定対策: DeviceSpecificationsが設定されている場合はReadRandom処理
    if (requestInfo.DeviceSpecifications != null && requestInfo.DeviceSpecifications.Any())
    {
        return ExtractDeviceValuesFromReadRandom(deviceData, requestInfo, processedAt);
    }

    // 後方互換性: 既存の処理を維持（DeviceType/StartAddress/Countを使用）
    switch (requestInfo.DeviceType.ToUpper())
    {
        case "D":
            devices.AddRange(ExtractWordDevices(deviceData, requestInfo, processedAt));
            break;

        case "M":
            devices.AddRange(ExtractBitDevices(deviceData, requestInfo, processedAt));
            break;

        default:
            throw new NotSupportedException(
                string.Format(ErrorMessages.UnsupportedDataType, requestInfo.DeviceType));
    }

    return devices;
}
```

**新規ヘルパーメソッド**:
```csharp
/// <summary>
/// ReadRandomレスポンスからデバイス値を抽出（Phase8.5暫定実装）
/// </summary>
private List<ProcessedDevice> ExtractDeviceValuesFromReadRandom(
    byte[] deviceData,
    ProcessedDeviceRequestInfo requestInfo,
    DateTime processedAt)
{
    var devices = new List<ProcessedDevice>();
    int offset = 0;

    foreach (var spec in requestInfo.DeviceSpecifications!)
    {
        if (offset + 2 > deviceData.Length)
        {
            throw new InvalidOperationException(
                $"レスポンスデータが不足しています: offset={offset}, dataLength={deviceData.Length}");
        }

        // 2バイト（1ワード）ずつ処理（ReadRandomの仕様）
        int value = BitConverter.ToUInt16(deviceData, offset);

        devices.Add(new ProcessedDevice
        {
            DeviceType = spec.DeviceType,
            Address = spec.DeviceNumber,
            Value = value,
            ProcessedAt = processedAt,
            RawBytes = deviceData.Skip(offset).Take(2).ToArray()
        });

        offset += 2; // 次のデバイスへ
    }

    return devices;
}
```

---

#### Step 4: 統合テスト（TDD: Integration Layer）

**📝 テストファイル**: `Step3_6_IntegrationTests.cs`

**Test Case 4-1**: 実機データでの動作確認（モック使用）
```csharp
[Fact]
public async Task FullCycle_Should_ProcessReadRandomResponse_WithRealWorldData()
{
    // Arrange - 実機レスポンスデータ（Phase9で取得）
    var realResponseFrame = new byte[]
    {
        0xD4, 0x00, 0x04, 0x00, 0x00, 0x00,        // サブヘッダ + シーケンス + 予約
        0x00, 0xFF, 0xFF, 0x03, 0x00,              // ネットワーク情報
        0x04, 0x00,                                 // データ長 = 4
        0x00, 0x00,                                 // 終了コード = 正常
        0x21, 0x05                                  // D100 = 1313 (0x0521 LE)
    };

    var config = new PlcConfiguration
    {
        IpAddress = "172.30.40.15",
        Port = 8192,
        FrameVersion = "4E",
        IsBinary = true,
        Devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100)
        }
    };

    var mockNetwork = new MockUdpClient();
    mockNetwork.SetNextReceiveData(realResponseFrame);

    // Act
    var result = await manager.ExecuteFullCycleAsync(
        connectionConfig,
        timeoutConfig,
        requestFrame,
        deviceRequestInfo,
        CancellationToken.None
    );

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.ProcessedData);
    Assert.Single(result.ProcessedData.Devices);
    Assert.Equal("D", result.ProcessedData.Devices[0].DeviceType);
    Assert.Equal(100, result.ProcessedData.Devices[0].Address);
    Assert.Equal(1313, result.ProcessedData.Devices[0].Value);
}
```

---

## 4. テスト実行計画

### 4.1 単体テスト（ユニットテスト）

**実行順序**:
```bash
# Step 1: モデルレイヤー
dotnet test --filter "FullyQualifiedName~ProcessedDeviceRequestInfoTests"

# Step 2: コントローラーレイヤー
dotnet test --filter "FullyQualifiedName~ExecutionOrchestratorTests"

# Step 3: サービスレイヤー
dotnet test --filter "FullyQualifiedName~PlcCommunicationManagerTests.ExtractDeviceValues"
```

**期待結果**: 全テストパス（Green）

### 4.2 統合テスト

```bash
# Step 4: 統合テスト
dotnet test --filter "FullyQualifiedName~Step3_6_IntegrationTests"
```

### 4.3 実機テスト（Phase9再実行）

**テスト環境**:
- PLC: 172.30.40.15:8192
- フレーム: 4Eフレーム（Binary）
- プロトコル: UDP

**実行手順**:
```bash
# ビルド
dotnet build -c Release

# 実機テスト実行
cd publish
.\andon.exe --config=実機設定.xlsx
```

**成功基準**:
- ✅ `サポートされていないデータ型です:` エラーが発生しない
- ✅ デバイス値が正しく取得できる
- ✅ ログに正常なデバイス値が出力される

---

## 5. 実装チェックリスト

### 5.1 コード変更

- [ ] `ProcessedDeviceRequestInfo.cs`: DeviceSpecificationsプロパティ追加
- [ ] `ExecutionOrchestrator.cs`: DeviceSpecifications設定処理追加
- [ ] `PlcCommunicationManager.cs`: ExtractDeviceValues修正
- [ ] `PlcCommunicationManager.cs`: ExtractDeviceValuesFromReadRandom追加

### 5.2 テストコード

- [ ] `ProcessedDeviceRequestInfoTests.cs`: 新規テストケース追加
- [ ] `ExecutionOrchestratorTests.cs`: DeviceSpecifications設定テスト追加
- [ ] `PlcCommunicationManagerTests.cs`: ExtractDeviceValuesテスト修正
- [ ] `Step3_6_IntegrationTests.cs`: 実機データテスト追加

### 5.3 検証

- [ ] 全ユニットテストパス
- [ ] 全統合テストパス
- [ ] 実機テストで正常動作確認
- [ ] Phase9ドキュメント更新

---

## 6. Phase12への移行準備

### 6.1 今回の暫定対策で準備できること

**✅ データ構造の整理**:
- `DeviceSpecifications`ベースの処理フロー確立
- ReadRandomレスポンス処理のロジック確立

**✅ テストコードの資産化**:
- ReadRandom専用のテストケース作成
- Phase12で再利用可能なテストパターン

### 6.2 Phase12で実施すべきこと

**専用クラスの設計**:
```csharp
// 新設計案
public class ReadRandomRequestInfo
{
    public List<DeviceSpecification> Devices { get; }
    public FrameType FrameType { get; }
    public DateTime RequestedAt { get; }
}

public class ReadRequestInfo  // 旧Read(0x0401)用
{
    public string DeviceType { get; }
    public int StartAddress { get; }
    public int Count { get; }
}
```

**インターフェース分離**:
- ReadRandom専用の処理メソッド
- Read専用の処理メソッド
- コマンド種別に応じた適切な型チェック

---

## 7. リスクと制約事項

### 7.1 リスク

**技術的リスク**:
- ⚠️ Phase3.5で削除したプロパティの復活（設計の後退感）
- ⚠️ 後方互換性維持のため複雑度が増加

**軽減策**:
- ✅ Phase12での抜本的な設計見直しを明記
- ✅ 暫定対策であることをコメントで明示
- ✅ 既存テストの互換性を維持

### 7.2 制約事項

**Phase8.5の制約**:
- 🔒 `ProcessedDeviceRequestInfo`の構造は変更しない（プロパティ追加のみ）
- 🔒 既存のDeviceType/StartAddress/Countプロパティは削除しない
- 🔒 後方互換性を完全に維持

**Phase12への持ち越し**:
- 📌 専用クラスへの分離
- 📌 コマンド種別ごとの型安全性向上
- 📌 不要なプロパティの削除

---

## 8. 完了条件

### 8.1 機能要件

- ✅ 実機テストで `サポートされていないデータ型です:` エラーが発生しない
- ✅ ReadRandomコマンドでデバイス値が正しく取得できる
- ✅ 複数デバイス指定が正しく動作する

### 8.2 品質要件

- ✅ 全ユニットテストがパスする
- ✅ 全統合テストがパスする
- ✅ コードカバレッジ80%以上を維持

### 8.3 ドキュメント要件

- ✅ Phase8.5ドキュメント作成完了
- ✅ Phase9ドキュメント更新（暫定対策の適用記録）
- ✅ Phase12への移行計画作成

---

## 9. 参考情報

### 9.1 関連ファイル

- `andon/Core/Models/ProcessedDeviceRequestInfo.cs`
- `andon/Core/Controllers/ExecutionOrchestrator.cs`
- `andon/Core/Managers/PlcCommunicationManager.cs`
- `documents/design/read_random実装/実装計画/Phase9_実機テスト.md`

### 9.2 関連コマンド

```bash
# ReadRandomコマンド: 0x0403
# SLMP仕様書: page_64.png参照

# 4Eフレーム Binary応答例:
# D4 00 04 00 00 00 00 FF FF 03 00 04 00 00 00 21 05
# ↑                                         ↑
# サブヘッダ                                 データ部（D100=0x0521）
```

### 9.3 TDD参考資料

- `documents/development_methodology/development-methodology.md`
- Red-Green-Refactorサイクルの実践
- モック/スタブを活用したオフラインテスト

---

**Phase8.5完了後の次ステップ**: Phase9実機テスト再実行 → Phase12根本対策へ
