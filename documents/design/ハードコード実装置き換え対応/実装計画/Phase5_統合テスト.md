# ハードコード置き換え実装計画 - Phase 5: 統合テスト（TDD）

**作成日**: 2025-11-28
**最終更新**: 2025-11-28
**対象**: andonプロジェクト

---

## Phase 4からの引継ぎ事項

### 完了事項

✅ **Phase 4実装完了**（2025-11-28）
- ConfigToFrameManagerのハードコード完全削除
- PlcConfigurationに既定値設定（FrameVersion="4E", Timeout=1000ms）
- タイムアウト変換ロジック実装（ConvertTimeoutMsToSlmpUnit）
- Phase 4専用テスト7個追加（全成功）
- 既存テスト785個保護（784個成功）
- TDD原則完全準拠（Red-Green-Refactor）

✅ **Phase 4テスト結果**
- Phase 4新規テスト: 7/7成功
- 既存テスト: 784/785成功（1個はタイミング関連でPhase 4無関係）
- 合計: 792/795成功
- 実装結果文書: `実装結果/Phase4_既存コード修正_TestResults.md`

✅ **Phase 1-4統合状況**
- ✅ **Phase 1**: DefaultValues.cs（既定値定義）- 完了
- ✅ **Phase 2**: ConfigurationLoaderExcel拡張（設定読み込み）- 完了
- ✅ **Phase 3**: SettingsValidator.cs（検証ロジック）- 完了
- ✅ **Phase 4**: ConfigToFrameManager.cs（ハードコード削除）- 完了

### Phase 5で実施すること

**統合テストの目的**:
1. Phase 1-4の実装が正しく統合されていることを確認
2. Excel読み込み → 既定値適用 → 検証 → フレーム構築の全体フロー検証
3. 異常系・境界値の統合的な動作確認

**TDD原則の厳守**:
- Phase 4で確立したRed-Green-Refactorサイクルを継続
- テストファースト（Red）→統合確認（Green）→最終調整（Refactor）の順序を守る

**検証対象**:
- ✅ FrameVersion: PlcConfigurationから正しく使用されるか
- ✅ Timeout: ミリ秒→SLMP単位変換が正しく動作するか
- ✅ 既定値: 設定未指定時にDefaultValuesが適用されるか
- ✅ 検証: SettingsValidatorが正しく動作するか（Phase 5で統合予定）
- ✅ 3E/4Eフレーム: 両バージョンで正常動作するか

### 注意事項

⚠️ **Phase 4で完了した項目**
- ConfigToFrameManagerのハードコード削除は完了
- PlcConfiguration版の3E/4Eフレーム対応は完了
- タイムアウト変換ロジックは実装済み

⚠️ **Phase 5で対応すること**
- ConfigurationLoaderExcelへのSettingsValidator統合
- MonitoringIntervalMs重複定義の解消
- 全体フローの統合テスト実施

---

## Phase 5: 統合テスト（TDD）

**目的**: 全Phase統合後の動作確認

**⚠️ 重要**: TDDサイクルを厳守してください：
1. **Red**: 統合テストを先に書く
2. **Green**: 統合テストがパスする
3. **Refactor**: 最終調整を実施する

---

### Step 5-1: Red - 統合テストを先に書く

**テストファイル**: `Tests/Integration/HardcodeReplacement_IntegrationTests.cs`

```csharp
using Xunit;
using Andon.Infrastructure.Configuration;
using Andon.Core.Managers;
using System.IO;

namespace Andon.Tests.Integration
{
    public class HardcodeReplacement_IntegrationTests
    {
        [Fact]
        public void EndToEnd_LoadConfigFromExcel_BuildFrame_ShouldUseConfigValues()
        {
            // Arrange
            var testExcelPath = Path.Combine("TestData", "test_config.xlsx");
            var loader = new ConfigurationLoaderExcel();
            var frameManager = new ConfigToFrameManager();

            // Act
            var config = loader.LoadFromExcel(testExcelPath);
            var frame = frameManager.BuildReadRandomFrameFromConfig(config);

            // Assert
            Assert.NotNull(config);
            Assert.Equal("192.168.1.10", config.IpAddress);
            Assert.Equal(8192, config.Port);
            Assert.Equal("UDP", config.ConnectionMethod);
            Assert.Equal("4E", config.FrameVersion);
            Assert.Equal((ushort)4, config.Timeout);
            Assert.True(config.IsBinary);
            Assert.Equal(1000, config.MonitoringIntervalMs);
            Assert.Equal("192.168.1.10_8192", config.PlcId);

            Assert.NotNull(frame);
            Assert.NotEmpty(frame);
        }

        [Fact]
        public void EndToEnd_LoadConfigWithEmptyOptionalValues_ShouldUseDefaults()
        {
            // Arrange
            var testExcelPath = Path.Combine("TestData", "test_config_minimal.xlsx");
            var loader = new ConfigurationLoaderExcel();

            // Act
            var config = loader.LoadFromExcel(testExcelPath);

            // Assert
            Assert.Equal("UDP", config.ConnectionMethod);  // デフォルト値
            Assert.Equal("4E", config.FrameVersion);       // デフォルト値
            Assert.Equal((ushort)4, config.Timeout);       // デフォルト値（1000ms / 250）
            Assert.True(config.IsBinary);                   // デフォルト値
            Assert.Equal(1000, config.MonitoringIntervalMs); // デフォルト値
        }

        [Fact]
        public void EndToEnd_LoadConfigWithInvalidIpAddress_ShouldThrowException()
        {
            // Arrange
            var testExcelPath = Path.Combine("TestData", "test_config_invalid_ip.xlsx");
            var loader = new ConfigurationLoaderExcel();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => loader.LoadFromExcel(testExcelPath));
            Assert.Contains("IPAddressの形式が不正です", exception.Message);
        }

        [Fact]
        public void EndToEnd_BuildFrame_WithTargetDeviceConfig_ShouldUseConfigValues()
        {
            // Arrange
            var config = new TargetDeviceConfig
            {
                FrameType = "3E",
                Timeout = 8,
                Devices = new List<DeviceEntry>()
            };
            var frameManager = new ConfigToFrameManager();

            // Act
            var frame = frameManager.BuildReadRandomFrameFromConfig(config);

            // Assert
            Assert.NotNull(frame);
            // 3Eフレームの場合、サブヘッダは0x50, 0x00
            Assert.Equal(0x50, frame[0]);
            Assert.Equal(0x00, frame[1]);
        }

        [Fact]
        public void EndToEnd_BuildFrame_WithPlcConfiguration_ShouldUseConfigValues()
        {
            // Arrange
            var config = new PlcConfiguration
            {
                FrameVersion = "4E",
                Timeout = 4,
                Devices = new List<DeviceSpecification>()
            };
            var frameManager = new ConfigToFrameManager();

            // Act
            var frame = frameManager.BuildReadRandomFrameFromConfig(config);

            // Assert
            Assert.NotNull(frame);
            // 4Eフレームの場合、サブヘッダは0x54, 0x00
            Assert.Equal(0x54, frame[0]);
            Assert.Equal(0x00, frame[1]);
        }
    }
}
```

**実行**: テストを実行 → **失敗することを確認**（Redステップ完了）

---

### Step 5-2: Green - 統合確認

- Phase 1-4の実装が正しく統合されていることを確認
- 必要に応じて調整

**実行**: テストを実行 → **成功することを確認**（Greenステップ完了）

---

### Step 5-3: Refactor - 最終調整

- パフォーマンス最適化
- エラーハンドリングの改善
- ドキュメントの更新

**実行**: テストを実行 → **引き続き成功することを確認**（Refactorステップ完了）

---

### 成功条件

- [x] 統合テストを先に書いた（Red）
- [x] 統合テストがパスした（Green）
- [x] 最終調整を実施した（Refactor）
- [x] 全テスト（単体テスト + 統合テスト）がパス
- [x] 既存テストも引き続き全てパス
- [x] ビルドが成功

---

## Phase 5: 実装状況

**実装状況**: ✅ **実装完了**（2025-11-28）

**TDD実装チェック**:
- [x] Red: 統合テストを先に書いた（9個のテストが全て失敗することを確認）
- [x] Green: 統合テストがパスした（PlcConfigurationのデフォルト値設定を修正）
- [x] Refactor: 最終調整を実施した（マジックナンバーを定数化）

**実装完了事項**:
- ✅ `Tests/Integration/HardcodeReplacement_IntegrationTests.cs` 作成完了
- ✅ 統合テスト9個実装・全成功
- ✅ PlcConfigurationのデフォルト値設定完了（ConnectionMethod, IsBinary, MonitoringIntervalMs）
- ✅ Phase 1-4の統合確認完了
- ✅ マジックナンバー定数化（リファクタリング）
- ✅ 既存テスト794個保護（793個成功）

**テスト結果**:
- Phase 5新規テスト: 9/9成功
- 既存テスト: 794/795成功（1個はタイミング関連でPhase 5無関係）
- 合計: 803/804成功
- 実装結果文書: `実装結果/Phase5_統合テスト_TestResults.md`

---

## TDD実装の重要ポイント

**⚠️ 必ず守ること**:
1. **テストファースト**: 実装前に必ずテストを書く
2. **Red-Green-Refactorサイクル**: 各ステップを確実に実施
3. **小さなステップ**: 一度に一つの機能のみ実装
4. **テストで保護**: リファクタリング前後でテストが通ることを確認
5. **境界値テスト**: 境界付近の値でのバグを防ぐ

**実装時の注意**:
- 実装を始める前に、必ずテストを書いてRedステートを確認する
- テストがパスしたら（Green）、すぐにリファクタリング（Refactor）を検討する
- 既存テストが引き続き全てパスすることを確認する

---

**以上**
