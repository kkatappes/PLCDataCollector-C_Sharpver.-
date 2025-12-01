# ハードコード置き換え実装計画 - Phase 1: 既定値の定義（TDD）

**作成日**: 2025-11-28
**対象**: andonプロジェクト

---

## Phase 1: 既定値の定義（TDD）

**目的**: 既定値を定数として定義し、コード全体で統一的に使用

**⚠️ 重要**: 本プロジェクトはTDD（Test-Driven Development）を採用しています。以下のサイクルを厳守してください：
1. **Red**: 失敗するテストを先に書く
2. **Green**: テストを通すための最小限のコードを実装
3. **Refactor**: 動作を保ったままコードを改善

---

### Step 1-1: Red - テストを先に書く

**テストファイル**: `Tests/Unit/Core/Constants/DefaultValuesTests.cs`

```csharp
using Xunit;
using Andon.Core.Constants;

namespace Andon.Tests.Unit.Core.Constants
{
    public class DefaultValuesTests
    {
        [Fact]
        public void ConnectionMethod_ShouldBeUDP()
        {
            Assert.Equal("UDP", DefaultValues.ConnectionMethod);
        }

        [Fact]
        public void FrameVersion_ShouldBe4E()
        {
            Assert.Equal("4E", DefaultValues.FrameVersion);
        }

        [Fact]
        public void TimeoutMs_ShouldBe1000()
        {
            Assert.Equal(1000, DefaultValues.TimeoutMs);
        }

        [Fact]
        public void TimeoutSlmp_ShouldBe4()
        {
            Assert.Equal((ushort)4, DefaultValues.TimeoutSlmp);
        }

        [Fact]
        public void IsBinary_ShouldBeTrue()
        {
            Assert.True(DefaultValues.IsBinary);
        }

        [Fact]
        public void MonitoringIntervalMs_ShouldBe1000()
        {
            Assert.Equal(1000, DefaultValues.MonitoringIntervalMs);
        }
    }
}
```

**実行**: テストを実行 → **失敗することを確認**（Redステップ完了）

---

### Step 1-2: Green - 最小限の実装

**実装ファイル**: `andon/Core/Constants/DefaultValues.cs`

```csharp
namespace Andon.Core.Constants
{
    public static class DefaultValues
    {
        // 接続設定
        public const string ConnectionMethod = "UDP";

        // SLMP設定
        public const string FrameVersion = "4E";
        public const int TimeoutMs = 1000;  // ミリ秒
        public const ushort TimeoutSlmp = 4;  // SLMP単位（1000ms / 250）
        public const bool IsBinary = true;

        // データ処理設定
        public const int MonitoringIntervalMs = 1000;
    }
}
```

**実行**: テストを実行 → **成功することを確認**（Greenステップ完了）

---

### Step 1-3: Refactor - リファクタリング

- コメントの追加・整理
- 定数のグルーピング確認
- 命名規則の統一

**実行**: テストを実行 → **引き続き成功することを確認**（Refactorステップ完了）

---

### 成功条件

- [x] 失敗するテストを先に書いた（Red）
- [x] テストを通す最小実装を行った（Green）
- [x] リファクタリングを実施した（Refactor）
- [x] 全テストがパス
- [x] ビルドが成功

---

## Phase 1: 実装状況

**実装状況**: ✅ **実装完了**（2025-11-28）

**TDD実装チェック**:
- [x] Red: 失敗するテストを先に書いた（6個のコンパイルエラーを確認）
- [x] Green: テストを通す最小実装を行った（全6個のテスト成功）
- [x] Refactor: リファクタリングを実施した（XMLドキュメントコメント・セクション区切り追加、テスト継続成功）

**実装完了事項**:
- ✅ `andon/Core/Constants/DefaultValues.cs` 作成完了
- ✅ `andon/Tests/Unit/Core/Constants/DefaultValuesTests.cs` 作成完了
- ✅ 全6個のテストが成功
- ✅ ビルドエラー: 0個
- ✅ andonプロジェクト全体のビルド成功

**定義された既定値**:
- `ConnectionMethod`: "UDP"
- `FrameVersion`: "4E"
- `TimeoutMs`: 1000 (ミリ秒)
- `TimeoutSlmp`: 4 (SLMP単位)
- `IsBinary`: true (Binary形式)
- `MonitoringIntervalMs`: 1000 (ミリ秒)

**実装結果詳細**:
- 詳細なテスト結果は `documents/design/ハードコード実装置き換え対応/実装結果/Phase1_既定値の定義_TestResults.md` を参照

---

**以上**
