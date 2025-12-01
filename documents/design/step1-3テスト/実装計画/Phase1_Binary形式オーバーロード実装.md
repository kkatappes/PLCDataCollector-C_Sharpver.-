# Phase 1: Binary形式オーバーロード実装計画

## 目的
TargetDeviceConfig型を受け取る`BuildReadRandomFrameFromConfig`メソッドのオーバーロード実装

## 実装ファイル
- **クラス**: `ConfigToFrameManager` (Core/Managers/ConfigToFrameManager.cs)
- **テストクラス**: `ConfigToFrameManagerTests` (Tests/Unit/Core/Managers/ConfigToFrameManagerTests.cs)

## 注記
- 本文書で扱う型は`TargetDeviceConfig`です
- `PlcConfiguration`はExcel設定読み込み専用の別クラスとして実装されています

---

## Round 1: null検証（異常系）

### Red: テストケース実装
```csharp
[Fact]
public void TC_Step12_004_BuildReadRandomFrameFromConfig_異常系_ConfigNull()
{
    // Arrange
    var frameManager = new ConfigToFrameManager();

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() =>
        frameManager.BuildReadRandomFrameFromConfig((TargetDeviceConfig)null));
}
```

### Green: 最小限の実装
```csharp
public byte[] BuildReadRandomFrameFromConfig(TargetDeviceConfig config)
{
    if (config == null)
        throw new ArgumentNullException(nameof(config));

    return null; // まだ未実装
}
```

### テスト実行
```bash
dotnet test --filter "TC_Step12_004_BuildReadRandomFrameFromConfig_異常系_ConfigNull"
```

### パス確認
✅ テストがパスすることを確認

---

## Round 2: 空リスト検証（異常系）

### Red: テストケース実装
```csharp
[Fact]
public void TC_Step12_003_BuildReadRandomFrameFromConfig_異常系_デバイスリスト空()
{
    // Arrange
    var targetConfig = new TargetDeviceConfig
    {
        Devices = new List<DeviceEntry>()
    };
    var frameManager = new ConfigToFrameManager();

    // Act & Assert
    Assert.Throws<ArgumentException>(() =>
        frameManager.BuildReadRandomFrameFromConfig(targetConfig));
}
```

### Green: Devicesリストの空チェック追加
```csharp
public byte[] BuildReadRandomFrameFromConfig(TargetDeviceConfig config)
{
    if (config == null)
        throw new ArgumentNullException(nameof(config));

    if (config.Devices == null || config.Devices.Count == 0)
        throw new ArgumentException("デバイスリストが空です", nameof(config));

    return null; // まだ未実装
}
```

### テスト実行
```bash
dotnet test --filter "TC_Step12_003_BuildReadRandomFrameFromConfig_異常系_デバイスリスト空"
```

### パス確認
✅ テストがパスすることを確認

---

## Round 3: フレーム構築（正常系）

### Red: テストケース実装
```csharp
[Fact]
public void TC_Step12_001_BuildReadRandomFrameFromConfig_正常系_4Eフレーム_48デバイス()
{
    // Arrange
    var targetConfig = new TargetDeviceConfig
    {
        FrameType = "4E",
        Timeout = 32,
        Devices = new List<DeviceEntry>
        {
            new DeviceEntry
            {
                DeviceType = "M",
                DeviceNumber = 33,
                ItemName = "テスト1",
                Digits = 1,
                Unit = "bit"
            },
            new DeviceEntry
            {
                DeviceType = "D",
                DeviceNumber = 100,
                ItemName = "テスト2",
                Digits = 1,
                Unit = "word"
            }
        }
    };

    var frameManager = new ConfigToFrameManager();

    // Act
    var frame = frameManager.BuildReadRandomFrameFromConfig(targetConfig);

    // Assert
    Assert.NotNull(frame);
    Assert.True(frame.Length > 0);

    // 4Eフレームヘッダ検証
    Assert.Equal(0x54, frame[0]); // サブヘッダ下位
    Assert.Equal(0x00, frame[1]); // サブヘッダ上位

    // コマンド検証 (4Eフレームはオフセット15-16)
    Assert.Equal(0x03, frame[15]); // コマンド下位 (ReadRandom)
    Assert.Equal(0x04, frame[16]); // コマンド上位
}
```

### Green: 完全な実装（SlmpFrameBuilderへの委譲）
```csharp
public byte[] BuildReadRandomFrameFromConfig(TargetDeviceConfig config)
{
    if (config == null)
        throw new ArgumentNullException(nameof(config));

    if (config.Devices == null || config.Devices.Count == 0)
        throw new ArgumentException("デバイスリストが空です", nameof(config));

    // FrameType検証
    if (config.FrameType != "3E" && config.FrameType != "4E")
        throw new ArgumentException($"未対応のフレームタイプ: {config.FrameType}", nameof(config));

    // DeviceEntryをDeviceSpecificationに変換
    var deviceSpecifications = config.Devices
        .Select(d => d.ToDeviceSpecification())
        .ToList();

    byte[] frame = SlmpFrameBuilder.BuildReadRandomRequest(
        deviceSpecifications,
        config.FrameType,
        config.Timeout
    );

    return frame;
}
```

### テスト実行
```bash
dotnet test --filter "TC_Step12_001_BuildReadRandomFrameFromConfig_正常系_4Eフレーム"
```

### パス確認
✅ テストがパスすることを確認

---

## Round 4: Binary形式全体テスト

### 全テスト実行
```bash
dotnet test --filter "BuildReadRandomFrameFromConfig"
```

### パス確認
✅ Round 1-3の全てのテストがパスすることを確認

### Refactor
必要に応じてコード改善（今回は不要と判断）

---

## Phase 1完了条件
- [x] Round 1: null検証テストがパス ✅ (2025-11-27)
- [x] Round 2: 空リスト検証テストがパス ✅ (2025-11-27)
- [x] Round 3: 正常系テストがパス ✅ (2025-11-27)
- [x] Round 4: Binary形式全テストがパス ✅ (2025-11-27)

## 実装完了
**完了日**: 2025-11-27
**実装方式**: TDD (Red-Green-Refactor)
**テスト結果**: 12/12成功（100%）

詳細結果: `documents/design/step1-3テスト/実装結果/Phase1_Binary形式オーバーロード_TestResults.md`

## 次のステップ
Phase 1完了後、Phase 2（ASCII形式オーバーロード実装）に進む
※ ASCII形式オーバーロードは既に実装済み（BuildReadRandomFrameFromConfigAscii）

---

## Phase 1補完: PlcConfiguration用Binary形式オーバーロード実装

### 実装背景
Phase1では`TargetDeviceConfig`型用のBinary形式オーバーロードを実装したが、Excel読み込み機能（`ConfigurationLoaderExcel`）との統合のため、`PlcConfiguration`型用のBinary形式オーバーロードも必要と判明。

### 実装日
**完了日**: 2025-11-28

### 実装内容

#### 実装メソッド（ConfigToFrameManager.cs: 137-154行）
```csharp
/// <summary>
/// PlcConfigurationからReadRandomフレームを構築（Binary形式、Excel読み込み用）
/// </summary>
public byte[] BuildReadRandomFrameFromConfig(PlcConfiguration config)
{
    if (config == null)
        throw new ArgumentNullException(nameof(config));

    if (config.Devices == null || config.Devices.Count == 0)
        throw new ArgumentException("デバイスリストが空です", nameof(config));

    // PlcConfiguration.Devices は既に DeviceSpecification型のリスト
    byte[] frame = SlmpFrameBuilder.BuildReadRandomRequest(
        config.Devices,
        frameType: "4E",  // 固定値
        timeout: 32       // 固定値
    );

    return frame;
}
```

#### テストケース（ConfigToFrameManagerTests.cs: 536-609行）
- **Round 1**: `TC_Step12_Binary_004_PlcConfig` - null検証（異常系）
- **Round 2**: `TC_Step12_Binary_003_PlcConfig` - 空リスト検証（異常系）
- **Round 3**: `TC_Step12_Binary_001_PlcConfig` - 正常系（4Eフレーム構築）

### 実装の特徴
1. **Phase2 ASCII版と同じ設計パターン**を採用
2. **PlcConfiguration.Devices**は既に`DeviceSpecification`型なので変換不要
3. **固定値**: frameType="4E", timeout=32（Excel読み込み仕様に準拠）
4. **TDD手法**: Red-Green-Refactorサイクルに従って実装

### 完了条件
- [x] Round 1: PlcConfig null検証テスト実装 ✅ (2025-11-28)
- [x] Round 2: PlcConfig 空リスト検証テスト実装 ✅ (2025-11-28)
- [x] Round 3: PlcConfig 正常系テスト実装 ✅ (2025-11-28)
- [x] ビルド成功確認 ✅ (2025-11-28)
- [x] DeviceSpecification修正（DeviceTypeプロパティ初期化） ✅ (2025-11-28)

### 詳細結果
`documents/design/step1-3テスト/実装結果/Phase1_PlcConfig_Binary形式オーバーロード実装_TestResults.md`

**実装結果サマリー**:
- Phase1補完テスト: 3/3成功（100%）
- ConfigToFrameManagerTests全体: 18/18成功（100%）
- DeviceSpecification修正: コンストラクタでDeviceTypeプロパティ初期化追加

### 全実装状況まとめ

| メソッド | 型 | 形式 | 実装日 | 状態 | テスト数 |
|---------|-----|------|--------|------|---------|
| BuildReadRandomFrameFromConfig | TargetDeviceConfig | Binary | 2025-11-27 | ✅ 完了 | 6 |
| BuildReadRandomFrameFromConfigAscii | TargetDeviceConfig | ASCII | 2025-11-27 | ✅ 完了 | 6 |
| BuildReadRandomFrameFromConfig | PlcConfiguration | Binary | 2025-11-28 | ✅ 完了 | 3 |
| BuildReadRandomFrameFromConfigAscii | PlcConfiguration | ASCII | 2025-11-28 | ✅ 完了 | 3 |
| **合計** | - | - | - | **✅ 完了** | **18** |

**Phase1完全完了**: 2025-11-28
**テスト実行結果**: 18/18成功（100%）
**次のステップ**: Phase3統合テスト実装・実行 ✅ 完了（2025-11-28）
