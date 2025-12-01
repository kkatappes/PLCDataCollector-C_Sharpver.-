# ハードコード置き換え実装計画 - Phase 2: 設定読み込みロジックの実装（TDD）

**作成日**: 2025-11-28
**最終更新**: 2025-11-28
**対象**: andonプロジェクト

---

## Phase 1からの引継ぎ事項

### 完了事項

✅ **DefaultValuesクラス実装完了**
- ファイルパス: `andon/Core/Constants/DefaultValues.cs`
- テストファイル: `andon/Tests/Unit/Core/Constants/DefaultValuesTests.cs`
- 全6個のテスト成功、ビルドエラー0個

✅ **定義された既定値（Phase2で使用可能）**
```csharp
using Andon.Core.Constants;

DefaultValues.ConnectionMethod      // "UDP"
DefaultValues.FrameVersion          // "4E"
DefaultValues.TimeoutMs             // 1000 (ミリ秒)
DefaultValues.TimeoutSlmp           // 4 (SLMP単位)
DefaultValues.IsBinary              // true
DefaultValues.MonitoringIntervalMs  // 1000 (ミリ秒)
```

✅ **TDD実装プロセスの確立**
- Red-Green-Refactorサイクルの実践完了
- 全テストケースが成功している状態で引継ぎ
- XMLドキュメントコメント完備

### Phase2で実施すること

**DefaultValuesの活用**:
1. ConfigurationLoaderExcelで`DefaultValues`をインポート
2. `GetOptionalValue`メソッドで既定値として使用
3. 設定ファイルに値がない場合のフォールバック処理

**使用例**:
```csharp
using Andon.Core.Constants;

// B10セルから読み込み、空の場合はDefaultValues.ConnectionMethodを使用
string connectionMethod = GetOptionalValue(settingsSheet, "B10", DefaultValues.ConnectionMethod);

// B11セルから読み込み、空の場合はDefaultValues.FrameVersionを使用
string frameVersion = GetOptionalValue(settingsSheet, "B11", DefaultValues.FrameVersion);

// B12セルから読み込み、空の場合はDefaultValues.TimeoutMsを使用
int timeoutMs = ParseIntOrDefault(settingsSheet.Cells["B12"].Text, DefaultValues.TimeoutMs);

// B13セルから読み込み、空の場合はDefaultValues.IsBinaryを使用
bool isBinary = ParseBoolOrDefault(settingsSheet.Cells["B13"].Text, DefaultValues.IsBinary);

// B14セルから読み込み、空の場合はDefaultValues.MonitoringIntervalMsを使用
int monitoringIntervalMs = ParseIntOrDefault(settingsSheet.Cells["B14"].Text, DefaultValues.MonitoringIntervalMs);
```

### 注意事項

⚠️ **ハードコード値の禁止**
- Phase2実装時は、直接値（"UDP", "4E", 1000等）を記述しないこと
- 必ず`DefaultValues`クラスの定数を参照すること
- これによりPhase1で定義した既定値の一元管理が実現される

⚠️ **TDDサイクルの厳守**
- Phase1で確立したRed-Green-Refactorサイクルを継続
- テストファースト（Red）→実装（Green）→改善（Refactor）の順序を守る

---

## Phase 2: 設定読み込みロジックの実装（TDD）

**目的**: ConfigurationLoaderExcelを拡張し、B10-B15セルから追加項目を読み込む

**⚠️ 重要**: TDDサイクルを厳守してください：
1. **Red**: 失敗するテストを先に書く
2. **Green**: テストを通すための最小限のコードを実装
3. **Refactor**: 動作を保ったままコードを改善

---

### Step 2-1: Red - テストを先に書く

**テストファイル**: `Tests/Unit/Infrastructure/Configuration/ConfigurationLoaderExcelTests.cs`

```csharp
using Xunit;
using Andon.Infrastructure.Configuration;
using Moq;

namespace Andon.Tests.Unit.Infrastructure.Configuration
{
    public class ConfigurationLoaderExcelTests
    {
        [Fact]
        public void LoadFromExcel_WhenConnectionMethodIsEmpty_ShouldUseDefaultUDP()
        {
            // Arrange
            var mockExcelData = CreateMockExcelData(connectionMethod: "");
            var loader = new ConfigurationLoaderExcel();

            // Act
            var config = loader.LoadFromExcel(mockExcelData);

            // Assert
            Assert.Equal("UDP", config.ConnectionMethod);
        }

        [Fact]
        public void LoadFromExcel_WhenFrameVersionIsEmpty_ShouldUseDefault4E()
        {
            // Arrange
            var mockExcelData = CreateMockExcelData(frameVersion: "");
            var loader = new ConfigurationLoaderExcel();

            // Act
            var config = loader.LoadFromExcel(mockExcelData);

            // Assert
            Assert.Equal("4E", config.FrameVersion);
        }

        [Fact]
        public void LoadFromExcel_WhenTimeoutIsEmpty_ShouldUseDefault1000Ms()
        {
            // Arrange
            var mockExcelData = CreateMockExcelData(timeout: "");
            var loader = new ConfigurationLoaderExcel();

            // Act
            var config = loader.LoadFromExcel(mockExcelData);

            // Assert
            Assert.Equal(1000, config.Timeout * 250); // SLMP単位からミリ秒に変換
        }

        [Fact]
        public void LoadFromExcel_WhenIsBinaryIsEmpty_ShouldUseDefaultTrue()
        {
            // Arrange
            var mockExcelData = CreateMockExcelData(isBinary: "");
            var loader = new ConfigurationLoaderExcel();

            // Act
            var config = loader.LoadFromExcel(mockExcelData);

            // Assert
            Assert.True(config.IsBinary);
        }

        [Fact]
        public void LoadFromExcel_WhenMonitoringIntervalMsIsEmpty_ShouldUseDefault1000Ms()
        {
            // Arrange
            var mockExcelData = CreateMockExcelData(monitoringIntervalMs: "");
            var loader = new ConfigurationLoaderExcel();

            // Act
            var config = loader.LoadFromExcel(mockExcelData);

            // Assert
            Assert.Equal(1000, config.MonitoringIntervalMs);
        }

        [Fact]
        public void LoadFromExcel_ShouldGeneratePlcIdFromIpAndPort()
        {
            // Arrange
            var mockExcelData = CreateMockExcelData(ipAddress: "192.168.1.10", port: "8192");
            var loader = new ConfigurationLoaderExcel();

            // Act
            var config = loader.LoadFromExcel(mockExcelData);

            // Assert
            Assert.Equal("192.168.1.10_8192", config.PlcId);
        }

        [Fact]
        public void LoadFromExcel_WhenPlcNameIsEmpty_ShouldUsePlcId()
        {
            // Arrange
            var mockExcelData = CreateMockExcelData(ipAddress: "192.168.1.10", port: "8192", plcName: "");
            var loader = new ConfigurationLoaderExcel();

            // Act
            var config = loader.LoadFromExcel(mockExcelData);

            // Assert
            Assert.Equal(config.PlcId, config.PlcName);
        }

        [Fact]
        public void ParseIntOrDefault_WhenValueIsEmpty_ShouldReturnDefault()
        {
            // Arrange
            var loader = new ConfigurationLoaderExcel();

            // Act
            var result = loader.ParseIntOrDefault("", 1000);

            // Assert
            Assert.Equal(1000, result);
        }

        [Fact]
        public void ParseBoolOrDefault_WhenValueIs1_ShouldReturnTrue()
        {
            // Arrange
            var loader = new ConfigurationLoaderExcel();

            // Act
            var result = loader.ParseBoolOrDefault("1", false);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ParseBoolOrDefault_WhenValueIs0_ShouldReturnFalse()
        {
            // Arrange
            var loader = new ConfigurationLoaderExcel();

            // Act
            var result = loader.ParseBoolOrDefault("0", true);

            // Assert
            Assert.False(result);
        }

        private MockExcelData CreateMockExcelData(
            string ipAddress = "192.168.1.10",
            string port = "8192",
            string connectionMethod = "UDP",
            string frameVersion = "4E",
            string timeout = "1000",
            string isBinary = "true",
            string monitoringIntervalMs = "1000",
            string plcName = "TestPLC")
        {
            // モックExcelデータを作成
            return new MockExcelData
            {
                ["B8"] = ipAddress,
                ["B9"] = port,
                ["B10"] = connectionMethod,
                ["B11"] = frameVersion,
                ["B12"] = timeout,
                ["B13"] = isBinary,
                ["B14"] = monitoringIntervalMs,
                ["B15"] = plcName
            };
        }
    }
}
```

**実行**: テストを実行 → **失敗することを確認**（Redステップ完了）

---

### Step 2-2: Green - 最小限の実装

**実装ファイル**: `andon/Infrastructure/Configuration/ConfigurationLoaderExcel.cs`

```csharp
// GetRequiredValue / GetOptionalValue メソッドを追加
private T GetRequiredValue<T>(ExcelWorksheet sheet, string cellAddress, string itemName)
{
    var value = sheet.Cells[cellAddress].Text;
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new ArgumentException($"必須項目 '{itemName}' が設定ファイルに存在しません。");
    }
    return (T)Convert.ChangeType(value, typeof(T));
}

private T GetOptionalValue<T>(ExcelWorksheet sheet, string cellAddress, T defaultValue)
{
    var value = sheet.Cells[cellAddress].Text;
    if (string.IsNullOrWhiteSpace(value))
        return defaultValue;

    try
    {
        return (T)Convert.ChangeType(value, typeof(T));
    }
    catch
    {
        return defaultValue;
    }
}

// ヘルパーメソッド追加
public int ParseIntOrDefault(string value, int defaultValue)
{
    if (string.IsNullOrWhiteSpace(value))
        return defaultValue;

    return int.TryParse(value, out int result) ? result : defaultValue;
}

public bool ParseBoolOrDefault(string value, bool defaultValue)
{
    if (string.IsNullOrWhiteSpace(value))
        return defaultValue;

    if (bool.TryParse(value, out bool result))
        return result;

    // "1"/"0" 形式のサポート
    if (value == "1" || value.ToLower() == "true")
        return true;
    if (value == "0" || value.ToLower() == "false")
        return false;

    return defaultValue;
}

// LoadFromExcel メソッドを拡張
private PlcConfiguration LoadFromExcel(string filePath)
{
    // ... 既存コード ...

    // 【新規追加】追加項目の読み込み
    string connectionMethod = GetOptionalValue(settingsSheet, "B10", DefaultValues.ConnectionMethod);
    string frameVersion = GetOptionalValue(settingsSheet, "B11", DefaultValues.FrameVersion);
    int timeoutMs = ParseIntOrDefault(settingsSheet.Cells["B12"].Text, DefaultValues.TimeoutMs);
    bool isBinary = ParseBoolOrDefault(settingsSheet.Cells["B13"].Text, DefaultValues.IsBinary);
    int monitoringIntervalMs = ParseIntOrDefault(settingsSheet.Cells["B14"].Text, DefaultValues.MonitoringIntervalMs);
    string? plcName = settingsSheet.Cells["B15"].Text;

    // PlcIdの自動生成
    string plcId = $"{ipAddress}_{port}";

    var config = new PlcConfiguration
    {
        IpAddress = ipAddress,
        Port = port,
        ConnectionMethod = connectionMethod,
        FrameVersion = frameVersion,
        Timeout = (ushort)(timeoutMs / 250),  // SLMP単位に変換
        IsBinary = isBinary,
        MonitoringIntervalMs = monitoringIntervalMs,
        PlcId = plcId,
        PlcName = string.IsNullOrWhiteSpace(plcName) ? plcId : plcName,
        Devices = ReadDevices(devicesSheet, filePath)
    };

    return config;
}
```

**実行**: テストを実行 → **成功することを確認**（Greenステップ完了）

---

### Step 2-3: Refactor - リファクタリング

- 重複コードの削除
- メソッドの分離
- エラーハンドリングの改善

**実行**: テストを実行 → **引き続き成功することを確認**（Refactorステップ完了）

---

### 成功条件

- [x] 失敗するテストを先に書いた（Red）
- [x] テストを通す最小実装を行った（Green）
- [x] リファクタリングを実施した（Refactor）
- [x] 全テストがパス
- [x] ビルドが成功

---

## Phase 2: 実装状況

**実装状況**: ✅ **実装完了**（2025-11-28）

**TDD実装チェック**:
- [x] Red: 失敗するテストを先に書いた（Phase2テスト10個追加 → 7失敗、3成功）
- [x] Green: テストを通す最小実装を行った（全10個のテスト成功）
- [x] Refactor: リファクタリングを実施した（既存テスト38個も全て合格維持）

**実装完了事項**:
- ✅ PlcConfigurationに拡張プロパティ追加（7プロパティ）
- ✅ ReadOptionalCell<T>()ヘルパーメソッド実装
- ✅ B10セル（ConnectionMethod）読み込み実装
- ✅ B14セル（MonitoringIntervalMs）読み込み実装
- ✅ B15セル（PlcName）読み込み実装
- ✅ PlcId自動生成機能実装
- ✅ DefaultValues定数使用（ハードコード排除）
- ✅ "1"/"0"形式のブール値変換サポート

**テスト結果**:
- Phase2新規テスト: 10/10成功
- 既存テスト: 38/38成功（1スキップ）
- TDDサイクル完全準拠

**将来的な拡張用**:
- FrameVersion, Timeout, IsBinaryは現在既定値固定
- 理由: B11-B13セルが既存項目で使用中
- 将来: 専用セルが追加された際に即座に対応可能

**詳細結果**:
- 実装結果文書: `documents/design/ハードコード実装置き換え対応/実装結果/Phase2_設定読み込み実装_TestResults.md`

---

**以上**
