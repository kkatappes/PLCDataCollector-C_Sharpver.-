# ハードコード置き換え実装計画 - Phase 3: 検証ロジックの実装（TDD）

**作成日**: 2025-11-28
**最終更新**: 2025-11-28
**対象**: andonプロジェクト

---

## Phase 2からの引継ぎ事項

### 完了事項

✅ **Phase 2実装完了**（2025-11-28）
- DefaultValuesクラス実装完了（6個の既定値定義）
- PlcConfiguration拡張完了（7プロパティ追加）
- ConfigurationLoaderExcel拡張完了（ReadOptionalCell<T>()実装）
- B10, B14, B15セル読み込み実装完了
- PlcId自動生成機能実装完了
- ハードコード値完全排除（DefaultValues使用）

✅ **Phase 2テスト結果**
- Phase2新規テスト: 10/10成功
- 既存テスト: 38/38成功（1スキップ）
- 合計: 48/48成功
- TDDサイクル完全準拠（Red-Green-Refactor）

✅ **利用可能な既定値（Phase3で使用可能）**
```csharp
using Andon.Core.Constants;

DefaultValues.ConnectionMethod      // "UDP"
DefaultValues.FrameVersion          // "4E"
DefaultValues.TimeoutMs             // 1000 (ミリ秒)
DefaultValues.TimeoutSlmp           // 4 (SLMP単位)
DefaultValues.IsBinary              // true
DefaultValues.MonitoringIntervalMs  // 1000 (ミリ秒)
```

✅ **PlcConfiguration拡張プロパティ（Phase3で検証対象）**
```csharp
public class PlcConfiguration
{
    // Phase2で追加された検証対象プロパティ
    public string ConnectionMethod { get; set; }  // B10セル（"TCP" or "UDP"）
    public string FrameVersion { get; set; }      // 既定値使用（"3E" or "4E"）
    public int Timeout { get; set; }              // 既定値使用（100～30000ms）
    public bool IsBinary { get; set; }            // 既定値使用（true/false）
    public int MonitoringIntervalMs { get; set; } // B14セル（100～60000ms）
    public string PlcId { get; set; }             // 自動生成（"{IpAddress}_{Port}"）
    public string PlcName { get; set; }           // B15セル（省略時PlcId使用）

    // 既存プロパティ（Phase3で検証対象）
    public string IpAddress { get; set; }         // B8セル（必須）
    public int Port { get; set; }                 // B9セル（1～65535）
    public int DataReadingFrequency { get; set; } // B11セル（既存項目）
    public string PlcModel { get; set; }          // B12セル（既存項目）
    public string SavePath { get; set; }          // B13セル（既存項目）
}
```

### Phase3で実施すること

**検証ロジックの実装**:
1. SettingsValidatorクラスの作成（新規）
2. 各プロパティの検証メソッド実装
3. ConfigurationLoaderExcelへの統合（既存のValidateConfiguration()の拡張または置換）

**検証対象プロパティ**:
- ✅ IpAddress: 形式検証、"0.0.0.0"禁止（Phase2で部分実装済み）
- ✅ Port: 範囲検証 1～65535（Phase2で実装済み）
- 🆕 ConnectionMethod: "TCP" or "UDP"（大文字小文字不問）
- 🆕 FrameVersion: "3E" or "4E"（大文字小文字不問）
- 🆕 Timeout: 範囲検証 100～30000ms
- 🆕 MonitoringIntervalMs: 範囲検証 100～60000ms

**既存の検証ロジックとの関係**:
- Phase2で`ConfigurationLoaderExcel.ValidateConfiguration()`に基本検証が実装済み
- Phase3では専用の`SettingsValidator`クラスに検証ロジックを分離・拡張
- 既存の検証ロジックは保持しつつ、新しい検証項目を追加

### 注意事項

⚠️ **TDDサイクルの厳守**
- Phase2で確立したRed-Green-Refactorサイクルを継続
- テストファースト（Red）→実装（Green）→改善（Refactor）の順序を守る

⚠️ **既存テストへの影響**
- Phase2の既存テスト48個が引き続き全てパスすることを確認
- Phase3の新規検証追加が既存機能を破壊しないことを保証

⚠️ **既定値の活用**
- 検証ロジックでも`DefaultValues`クラスの定数を参照
- ハードコード値を記述しないこと

---

## Phase 3: 検証ロジックの実装（TDD）

**目的**: 設定値の妥当性を検証し、エラーメッセージを統一管理

**⚠️ 重要**: TDDサイクルを厳守してください：
1. **Red**: 失敗するテストを先に書く
2. **Green**: テストを通すための最小限のコードを実装
3. **Refactor**: 動作を保ったままコードを改善

---

### Step 3-1: Red - テストを先に書く

**テストファイル**: `Tests/Unit/Infrastructure/Configuration/SettingsValidatorTests.cs`

```csharp
using Xunit;
using Andon.Infrastructure.Configuration;
using System;

namespace Andon.Tests.Unit.Infrastructure.Configuration
{
    public class SettingsValidatorTests
    {
        private readonly SettingsValidator _validator;

        public SettingsValidatorTests()
        {
            _validator = new SettingsValidator();
        }

        [Theory]
        [InlineData("192.168.1.10")]
        [InlineData("172.30.40.15")]
        [InlineData("10.0.0.1")]
        public void ValidateIpAddress_WhenValidFormat_ShouldNotThrow(string ipAddress)
        {
            // Act & Assert
            var exception = Record.Exception(() => _validator.ValidateIpAddress(ipAddress));
            Assert.Null(exception);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void ValidateIpAddress_WhenEmpty_ShouldThrowArgumentException(string ipAddress)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.ValidateIpAddress(ipAddress));
            Assert.Contains("必須項目 'IPAddress'", exception.Message);
        }

        [Theory]
        [InlineData("999.999.999.999")]
        [InlineData("abc.def.ghi.jkl")]
        [InlineData("192.168.1")]
        public void ValidateIpAddress_WhenInvalidFormat_ShouldThrowArgumentException(string ipAddress)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.ValidateIpAddress(ipAddress));
            Assert.Contains("IPAddressの形式が不正です", exception.Message);
        }

        [Fact]
        public void ValidateIpAddress_When0000_ShouldThrowArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.ValidateIpAddress("0.0.0.0"));
            Assert.Contains("IPAddress '0.0.0.0' は使用できません", exception.Message);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(8192)]
        [InlineData(65535)]
        public void ValidatePort_WhenInRange_ShouldNotThrow(int port)
        {
            // Act & Assert
            var exception = Record.Exception(() => _validator.ValidatePort(port));
            Assert.Null(exception);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(70000)]
        public void ValidatePort_WhenOutOfRange_ShouldThrowArgumentException(int port)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.ValidatePort(port));
            Assert.Contains("Portの値が範囲外です", exception.Message);
        }

        [Theory]
        [InlineData("TCP")]
        [InlineData("UDP")]
        [InlineData("tcp")]
        [InlineData("udp")]
        public void ValidateConnectionMethod_WhenValid_ShouldNotThrow(string connectionMethod)
        {
            // Act & Assert
            var exception = Record.Exception(() => _validator.ValidateConnectionMethod(connectionMethod));
            Assert.Null(exception);
        }

        [Theory]
        [InlineData("HTTP")]
        [InlineData("FTP")]
        [InlineData("")]
        public void ValidateConnectionMethod_WhenInvalid_ShouldThrowArgumentException(string connectionMethod)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.ValidateConnectionMethod(connectionMethod));
            Assert.Contains("ConnectionMethodの値が不正です", exception.Message);
        }

        [Theory]
        [InlineData("3E")]
        [InlineData("4E")]
        [InlineData("3e")]
        [InlineData("4e")]
        public void ValidateFrameVersion_WhenValid_ShouldNotThrow(string frameVersion)
        {
            // Act & Assert
            var exception = Record.Exception(() => _validator.ValidateFrameVersion(frameVersion));
            Assert.Null(exception);
        }

        [Theory]
        [InlineData("5E")]
        [InlineData("2E")]
        [InlineData("")]
        public void ValidateFrameVersion_WhenInvalid_ShouldThrowArgumentException(string frameVersion)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.ValidateFrameVersion(frameVersion));
            Assert.Contains("FrameVersionの値が不正です", exception.Message);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(30000)]
        public void ValidateTimeout_WhenInRange_ShouldNotThrow(int timeoutMs)
        {
            // Act & Assert
            var exception = Record.Exception(() => _validator.ValidateTimeout(timeoutMs));
            Assert.Null(exception);
        }

        [Theory]
        [InlineData(50)]
        [InlineData(40000)]
        public void ValidateTimeout_WhenOutOfRange_ShouldThrowArgumentException(int timeoutMs)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.ValidateTimeout(timeoutMs));
            Assert.Contains("Timeoutの値が範囲外です", exception.Message);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(60000)]
        public void ValidateMonitoringIntervalMs_WhenInRange_ShouldNotThrow(int intervalMs)
        {
            // Act & Assert
            var exception = Record.Exception(() => _validator.ValidateMonitoringIntervalMs(intervalMs));
            Assert.Null(exception);
        }

        [Theory]
        [InlineData(50)]
        [InlineData(70000)]
        public void ValidateMonitoringIntervalMs_WhenOutOfRange_ShouldThrowArgumentException(int intervalMs)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.ValidateMonitoringIntervalMs(intervalMs));
            Assert.Contains("MonitoringIntervalMsの値が範囲外です", exception.Message);
        }
    }
}
```

**実行**: テストを実行 → **失敗することを確認**（Redステップ完了）

---

### Step 3-2: Green - 最小限の実装

**実装ファイル**: `andon/Infrastructure/Configuration/SettingsValidator.cs`

```csharp
using System;
using System.Linq;
using System.Net;

namespace Andon.Infrastructure.Configuration
{
    public class SettingsValidator
    {
        public void ValidateIpAddress(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentException("必須項目 'IPAddress' が設定ファイルに存在しません。");

            if (!IPAddress.TryParse(ipAddress, out var parsedIp))
                throw new ArgumentException($"IPAddressの形式が不正です: '{ipAddress}'");

            if (parsedIp.ToString() == "0.0.0.0")
                throw new ArgumentException("IPAddress '0.0.0.0' は使用できません。");
        }

        public void ValidatePort(int port)
        {
            if (port < 1 || port > 65535)
                throw new ArgumentException($"Portの値が範囲外です: {port} (許可範囲: 1～65535)");
        }

        public void ValidateConnectionMethod(string connectionMethod)
        {
            var validMethods = new[] { "TCP", "UDP" };
            if (!validMethods.Contains(connectionMethod.ToUpper()))
                throw new ArgumentException($"ConnectionMethodの値が不正です: '{connectionMethod}' (許可値: TCP, UDP)");
        }

        public void ValidateFrameVersion(string frameVersion)
        {
            var validVersions = new[] { "3E", "4E" };
            if (!validVersions.Contains(frameVersion.ToUpper()))
                throw new ArgumentException($"FrameVersionの値が不正です: '{frameVersion}' (許可値: 3E, 4E)");
        }

        public void ValidateTimeout(int timeoutMs)
        {
            if (timeoutMs < 100 || timeoutMs > 30000)
                throw new ArgumentException($"Timeoutの値が範囲外です: {timeoutMs} (推奨範囲: 100～30000)");
        }

        public void ValidateMonitoringIntervalMs(int intervalMs)
        {
            if (intervalMs < 100 || intervalMs > 60000)
                throw new ArgumentException($"MonitoringIntervalMsの値が範囲外です: {intervalMs} (推奨範囲: 100～60000)");
        }
    }
}
```

**実行**: テストを実行 → **成功することを確認**（Greenステップ完了）

---

### Step 3-3: Refactor - リファクタリング

- エラーメッセージの定数化
- 検証ルールの共通化
- コードの可読性向上

**実行**: テストを実行 → **引き続き成功することを確認**（Refactorステップ完了）

---

### 成功条件

- [x] 失敗するテストを先に書いた（Red）
- [x] テストを通す最小実装を行った（Green）
- [x] リファクタリングを実施した（Refactor）
- [x] 全テストがパス
- [x] ビルドが成功

---

## Phase 3: 実装状況

**実装状況**: ✅ **実装完了**（2025-11-28）

**TDD実装チェック**:
- [x] Red: 失敗するテストを先に書いた（40テスト作成、ビルドエラー14個確認）
- [x] Green: テストを通す最小実装を行った（全40テスト成功）
- [x] Refactor: リファクタリングを実施した（定数化・XMLコメント・region追加、全40テスト継続成功）

**実装完了事項**:
- ✅ `SettingsValidator.cs` 実装完了（6つの検証メソッド）
- ✅ `SettingsValidatorTests.cs` 作成完了（40テスト、全成功）
- ✅ エラーメッセージ統一（Phase 0設計書準拠）
- ✅ 既存テスト保護（既存778テスト全て成功維持）

**検証メソッド**:
1. `ValidateIpAddress()` - IPv4形式、"0.0.0.0"禁止、オクテット4つ必須
2. `ValidatePort()` - 1～65535範囲
3. `ValidateConnectionMethod()` - "TCP"/"UDP"、大文字小文字不問
4. `ValidateFrameVersion()` - "3E"/"4E"、大文字小文字不問
5. `ValidateTimeout()` - 100～30000ms範囲
6. `ValidateMonitoringIntervalMs()` - 100～60000ms範囲

**テスト結果**:
- Phase 3新規テスト: 40/40成功
- 既存テスト: 778/778成功（2スキップ）
- 合計: 818/818成功
- 実装結果文書: `実装結果/Phase3_検証ロジック実装_TestResults.md`

**Phase 4への引き継ぎ**:
- ✅ 検証ロジック実装完了、ConfigurationLoaderExcel統合準備完了
- ✅ エラーメッセージ統一完了、Phase 0設計書準拠確認完了

---

**以上**
