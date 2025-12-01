# Phase3: 設定読み込み統合

## ステータス
✅ **部分完了** - ConfigToFrameManager実装済み（Phase4で実装）、ConfigurationLoader実装済み（Phase6で実装）

🔄 **設計更新** (2025-11-21) - Random READ全デバイス一括取得対応

## 概要
設定ファイル（appsettings.json）からデバイスリストを読み込み、ReadRandomフレームを自動構築する機能を実装します。

**実装状況**:
- ✅ ConfigToFrameManager: Phase4ステップ12で実装完了（Binary/ASCII両対応）
- ✅ ConfigurationLoader: Phase6ステップ18で実装完了
- ✅ TargetDeviceConfig: 既存実装を使用（Phase6で確定）

**2025-11-21設計変更の影響**:
- ✅ Random READで全デバイス一括取得方式採用
- ✅ Phase3で実装されたConfigToFrameManagerは変更不要
- ✅ Phase5でレスポンス処理が簡素化（MergeResponseData削除）
- ✅ Phase5でDeviceDataクラス導入（デバイス名キー構造）

## 前提条件
- ✅ Phase1完了: DeviceCode、DeviceSpecification実装済み
- ✅ Phase2完了: SlmpFrameBuilder.BuildReadRandomRequest()実装済み
- ✅ Phase4完了: ConfigToFrameManager.BuildReadRandomFrameFromConfig()実装済み
- ✅ Phase6完了: ConfigurationLoader.LoadPlcConnectionConfig()実装済み

## 実装ステップ

### ステップ8: ConfigToFrameManagerの実装 ✅ **完了（Phase4で実装）**

#### 実装対象
`andon/Core/Managers/ConfigToFrameManager.cs`

#### 実装内容
**Phase4ステップ12で実装済み（2025-11-18）**:
1. **BuildReadRandomFrameFromConfig()メソッド（Binary形式）**
   - 設定データ（TargetDeviceConfig）を受け取る
   - TargetDeviceConfig.DevicesはList<DeviceSpecification>型で直接使用可能
   - SlmpFrameBuilder.BuildReadRandomRequest()を呼び出し
   - 構築したフレームバイト配列を返却

2. **BuildReadRandomFrameFromConfigAscii()メソッド（ASCII形式）**
   - ASCII形式フレーム構築
   - SlmpFrameBuilder.BuildReadRandomRequestAscii()を呼び出し
   - 構築したフレーム文字列を返却

**重要な設計変更**:
- ❌ ParseDeviceCode()メソッドは不要（DeviceEntryからの変換はConfigurationLoaderで実施）
- ✅ TargetDeviceConfig.DevicesがList<DeviceSpecification>型（Phase6で確定）
- ✅ ConfigurationLoaderがDeviceEntry→DeviceSpecification変換を担当（Phase6実装）

#### 実装状況
- ✅ 実装ファイル: `andon/Core/Managers/ConfigToFrameManager.cs`
- ✅ テストファイル: `andon/Tests/Unit/Core/Managers/ConfigToFrameManagerTests.cs`
- ✅ Binary形式: 5テスト全PASSED
- ✅ ASCII形式: 5テスト全PASSED
- ✅ 実行時間: 44ms（Binary ~17ms + ASCII ~27ms）

詳細は以下を参照:
- `documents/design/read_random実装/実装計画/Phase4_通信マネージャーの修正.md` ステップ12

#### 変化点
- **変更前**: ConfigToFrameManagerは空実装
- **変更後**: ReadRandomフレーム自動構築が完全実装（Phase4で完了）
  - Binary/ASCII両形式対応
  - TargetDeviceConfig.DevicesをList<DeviceSpecification>として直接使用

---

### ステップ9: TargetDeviceConfigモデルの拡張 ✅ **完了（Phase6で確定）**

#### 実装対象
- `andon/Core/Models/ConfigModels/TargetDeviceConfig.cs`
- `andon/Core/Models/ConfigModels/DeviceEntry.cs`（Phase6で実装）

#### 実装内容
**Phase6で確定した設計**:

1. **TargetDeviceConfig**
   - `List<DeviceSpecification> Devices` プロパティ
   - ConfigurationLoader経由で読み込み時に既にDeviceSpecification型
   - ConfigToFrameManagerで直接使用可能

2. **DeviceEntryクラス（Phase6で実装）**
   - 設定ファイルからの読み込み専用の中間型
   - ConfigurationLoader内でDeviceSpecificationに変換
   - アプリケーション内部ではDeviceSpecificationを使用

#### 実装状況

**TargetDeviceConfig.cs（既存）**:
```csharp
public class TargetDeviceConfig
{
    public List<DeviceSpecification> Devices { get; set; } = new();
    public string FrameType { get; set; } = "4E";
    public ushort Timeout { get; set; } = 32;
}
```

**DeviceEntry.cs（Phase6で実装済み）**:
```csharp
public class DeviceEntry
{
    public string DeviceType { get; set; } = string.Empty;
    public int DeviceNumber { get; set; }
    public bool IsHexAddress { get; set; } = false;
    public string? Description { get; set; }

    // DeviceSpecificationに変換
    public DeviceSpecification ToDeviceSpecification() { ... }
}
```

**重要な設計判断**:
- ✅ **TargetDeviceConfig.Devices型**: `List<DeviceSpecification>`（Phase6で確定）
- ✅ **DeviceEntry**: 設定読み込み時の中間型（ConfigurationLoader専用）
- ✅ **変換箇所**: ConfigurationLoader.LoadPlcConnectionConfig()内
- ✅ **アプリケーション内部**: DeviceSpecificationのみ使用

詳細は以下を参照:
- `documents/design/read_random実装/実装計画/Phase6_設定ファイル構造の変更.md`
#### 変化点
- **変更前**: TargetDeviceConfig.DevicesがList<DeviceEntry>型（Phase3初期設計）
- **変更後**: TargetDeviceConfig.DevicesがList<DeviceSpecification>型（Phase6で確定）
  - DeviceEntryは設定読み込み時の中間型として使用
  - ConfigurationLoaderがDeviceEntry→DeviceSpecification変換を担当

---

### ステップ10: ConfigurationLoaderの実装 ✅ **完了（Phase6で実装）**

#### 実装対象
`andon/Infrastructure/Configuration/ConfigurationLoader.cs`

#### 実装内容
**Phase6ステップ18で実装済み（2025-11-21）**:
1. **LoadPlcConnectionConfig()メソッド**
   - appsettings.jsonからDevicesリストを読み込み
   - DeviceEntryオブジェクトとして解析
   - DeviceEntry→DeviceSpecification変換
   - TargetDeviceConfigとして返却

2. **ValidateConfig()メソッド**
   - デバイスリスト空チェック
   - 255点上限チェック
   - フレームタイプ検証（"3E" or "4E"）
   - ReadRandom対応チェック
   - デバイス番号範囲チェック

#### 実装状況
- ✅ 実装ファイル: `andon/Infrastructure/Configuration/ConfigurationLoader.cs`
- ✅ テストファイル: `andon/Tests/Unit/Infrastructure/Configuration/ConfigurationLoaderTests.cs`
- ✅ テスト数: 8テスト全PASSED
- ✅ カバレッジ: 正常系4テスト、異常系4テスト

詳細は以下を参照:
- `documents/design/read_random実装/実装計画/Phase6_設定ファイル構造の変更.md` ステップ18-19

#### 変化点
- **変更前**: ConfigurationLoader未実装（TODO状態）
- **変更後**: Devicesリスト読み込み機能実装完了（Phase6で完了）
  - DeviceEntry→DeviceSpecification変換
  - 厳密なバリデーション

---

### ステップ11: appsettings.jsonの更新 ✅ **完了（Phase6で実装）**

#### 実装対象
`appsettings.json`

#### 実装内容
**Phase6ステップ17で更新済み（2025-11-21）**:
- TargetDevicesをDevicesリスト形式に変更
- 7デバイス登録（M×3, D×3, W×1）
- 16進アドレスデバイス対応（W4522）
- Description追加で可読性向上

#### 設定例（実装済み）
```json
{
  "PlcCommunication": {
    "TargetDevices": {
      "Devices": [
        {
          "DeviceType": "M",
          "DeviceNumber": 0,
          "Description": "運転状態フラグ開始"
        },
        {
          "DeviceType": "D",
          "DeviceNumber": 100,
          "Description": "生産数カウンタ"
        },
        {
          "DeviceType": "W",
          "DeviceNumber": 4522,
          "IsHexAddress": true,
          "Description": "通信ステータス（W0x11AA）"
        }
      ]
    }
  }
}
```

#### 変化点
- **変更前**: TargetDevicesで範囲指定（MDeviceRange/DDeviceRange）
- **変更後**: Devicesリスト形式（個別デバイス指定）
  - 飛び飛びのデバイス指定可能
  - デバイス種別混在可能
  - 16進アドレス対応

---

### 元のステップ10: 設定読み込みのテスト作成 ✅ **完了（Phase4/Phase6で実装）**

#### 実装対象
- `andon/Tests/Unit/Core/Managers/ConfigToFrameManagerTests.cs`（Phase4で実装）
- `andon/Tests/Unit/Infrastructure/Configuration/ConfigurationLoaderTests.cs`（Phase6で実装）

#### テスト内容

**ConfigToFrameManagerTests（Phase4実装）**:
- BuildReadRandomFrameFromConfig()テスト（Binary/ASCII）
- 正常系: 4E/3Eフレーム構築
- 異常系: null/空リスト/未対応フレームタイプ

**ConfigurationLoaderTests（Phase6実装）**:
- LoadPlcConnectionConfig()テスト
- 正常系: 通常デバイス、16進デバイス、混在デバイス、デフォルト値
- 異常系: 空リスト、上限超過、不正DeviceType、不正FrameType

#### 実装状況
- ✅ ConfigToFrameManagerTests: 10テスト全PASSED（Phase4）
- ✅ ConfigurationLoaderTests: 8テスト全PASSED（Phase6）
- ✅ 合計: 18テスト全PASSED

**注意**: 以下のPhase3初期設計のテストコード例は古い仕様に基づいています。実際の実装はPhase4/Phase6を参照してください。

<details>
<summary>Phase3初期設計のテストコード例（参考）</summary>

```csharp
// 以下は古い設計に基づくサンプルコードです
// 実際の実装はPhase4/Phase6を参照してください

using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Constants;

namespace Andon.Tests.Unit.Core.Managers;

public class ConfigToFrameManagerTests_OldDesign
{
    // ParseDeviceCode()メソッドのテスト
    // → 実際にはConfigurationLoaderがDeviceEntry→DeviceSpecification変換を担当

    [Theory]
    [InlineData("D", DeviceCode.D)]
    [InlineData("M", DeviceCode.M)]
    public void ParseDeviceCode_ValidDeviceType_ReturnsCorrectCode(string deviceType, DeviceCode expected)
    {
        // このメソッドは実装されていません
        // ConfigurationLoader.LoadPlcConnectionConfig()内で変換が行われます
    }
}
```
</details>

---

## 完了条件
- ✅ ConfigToFrameManager実装完了（Phase4で実装）
  - BuildReadRandomFrameFromConfig()メソッド（Binary/ASCII）
  - 10テスト全PASSED
- ✅ ConfigurationLoader実装完了（Phase6で実装）
  - LoadPlcConnectionConfig()メソッド
  - 8テスト全PASSED
- ✅ TargetDeviceConfig型確定（Phase6で確定）
  - List<DeviceSpecification> Devices
- ✅ DeviceEntry型実装完了（Phase6で実装）
  - ToDeviceSpecification()変換メソッド
- ✅ appsettings.json更新完了（Phase6で実装）
  - Devicesリスト形式

## 次フェーズへの依存関係
- Phase4: ConfigToFrameManager使用（PlcCommunicationManagerとの統合）
- Phase5: ProcessReceivedRawDataでレスポンス処理（2025-11-21設計変更対応）
- Phase6: ConfigurationLoaderで設定読み込み
- Phase8: 統合テストで全フロー検証

## 2025-11-21設計変更の詳細と影響分析

### 設計変更の概要

**主な変更ポイント**:
1. **通信回数の最小化**: 2回送受信 → 1回送受信
   - Random READ(0x0403)コマンドで全デバイス（ビット・ワード・ダブルワード混在）を一括取得
   - READコマンド(0x0401)の廃止

2. **処理の簡素化**: MergeResponseData()メソッド削除
   - BasicProcessedResponseData型削除
   - ProcessReceivedRawDataで処理完結

3. **型設計の明確化**: DeviceDataクラスの導入
   - デバイス名キー構造（"M000", "D000", "D002"）
   - Dictionary<string, DeviceData>型でデータ管理
   - DWordDeviceCountはOriginalRequestから算出

4. **ビットデバイス対応の簡素化**:
   - 16点=1ワード換算ロジックが不要に
   - ビット・ワード・ダブルワード混在指定が可能に
   - Random READコマンドの仕様により、PLCが自動的に適切な形式で返す

### Phase3への影響

#### ✅ 影響なし（実装変更不要）

**ConfigToFrameManager（Phase4ステップ12で実装済み）**:
- BuildReadRandomFrameFromConfig()メソッドは変更不要
- 既にList<DeviceSpecification>型を受け取り、Random READフレームを構築
- ビット・ワード・ダブルワード混在に対応済み

**ConfigurationLoader（Phase6ステップ18で実装済み）**:
- LoadPlcConnectionConfig()メソッドは変更不要
- 既にDeviceEntry→DeviceSpecification変換を実装
- 飛び飛びのデバイス指定に対応済み

**TargetDeviceConfig（Phase6で確定）**:
- List<DeviceSpecification> Devices プロパティは変更不要
- 既にDeviceSpecification型でビット・ワード・ダブルワード混在対応

#### 📝 Phase5以降での対応（Phase3では不要）

**レスポンス処理（Phase5で実装予定）**:
1. **DeviceDataクラスの導入** (Phase5 ステップ14-A)
   - デバイス名キー構造（"M000", "D000"等）
   - ビット・ワード・ダブルワード混在対応
   - Dictionary<string, DeviceData>型でデータ管理

2. **ProcessedResponseDataの拡張** (Phase5 ステップ15)
   - DeviceDataプロパティ追加（新構造）
   - BasicProcessedDevices/CombinedDWordDevices（旧構造、Phase10削除予定）
   - 段階的クリーン移行戦略（Phase5～7で共存）

3. **MergeResponseData()メソッドの削除**
   - Random READ一括取得により不要化
   - ProcessReceivedRawDataで処理完結

### 設計変更のメリット

**1. 通信効率の向上**:
- 通信回数: 2回 → 1回（50%削減）
- ネットワーク負荷軽減
- 処理時間短縮

**2. コードの簡素化**:
- MergeResponseData()削除により処理フロー単純化
- BasicProcessedResponseData型削除によりデータ構造統一
- 保守性向上

**3. 型安全性の向上**:
- DeviceDataクラスによる明確な型定義
- デバイス名キーによる直感的なアクセス
- DWord対応の明示化（IsDWordプロパティ）

**4. 拡張性の向上**:
- ビット・ワード・ダブルワード混在が容易
- 新しいデバイスタイプの追加が簡単
- 設定ファイルの柔軟性向上

### 互換性の維持

**Phase3実装との互換性**:
- ✅ ConfigToFrameManager: 既存実装をそのまま使用可能
- ✅ ConfigurationLoader: 既存実装をそのまま使用可能
- ✅ TargetDeviceConfig: 既存の型定義をそのまま使用可能

**段階的移行戦略（Phase5～Phase10）**:
- Phase5～7: 新旧構造の共存（既存コード無修正）
- Phase7: 旧構造への依存ゼロ化
- Phase10: 旧構造の物理削除

### 参照ドキュメント

**関連設計文書**:
- `documents/design/read_random実装/実装計画/Phase5_レスポンス処理の修正.md`
  - DeviceDataクラス定義（ステップ14-A）
  - ProcessedResponseData拡張（ステップ15）
  - 段階的クリーン移行戦略

- `documents/design/フレーム構築関係/フレーム構築方法.md`
  - Random READ要求フレーム構造
  - Random READ応答フレーム構造
  - パース処理の重要ポイント

- `documents/design/クラス設計.md`
  - DeviceDataクラス詳細設計
  - ProcessedResponseData詳細設計
  - ConMoni/PySLMPClient準拠機能

### 実装スケジュール

**Phase3（完了済み）**:
- ✅ ConfigToFrameManager実装（Phase4ステップ12）
- ✅ ConfigurationLoader実装（Phase6ステップ18）
- ✅ TargetDeviceConfig型確定（Phase6）

**Phase5（未着手）**:
- ⏳ DeviceDataクラス実装（ステップ14-A）
- ⏳ SlmpDataParser.ParseReadRandomResponse()実装（ステップ14）
- ⏳ ProcessedResponseData拡張（ステップ15）
- ⏳ レスポンス処理テスト（ステップ16）

**Phase7（未着手）**:
- ⏳ DataOutputManager更新（新構造DeviceData使用）
- ⏳ LoggingManager更新（新構造DeviceData使用）

**Phase10（未着手）**:
- ⏳ 旧構造削除（BasicProcessedDevices/CombinedDWordDevices）
- ⏳ Obsolete属性付きプロパティ・メソッド削除

---

## Phase3の実装経緯まとめ

Phase3は当初、設定読み込みとフレーム構築の統合を計画していましたが、実際の実装は以下のように分散されました:

1. **ConfigToFrameManager**: Phase4ステップ12で実装（2025-11-18）
   - Binary/ASCII両形式対応
   - TargetDeviceConfig.DevicesをList<DeviceSpecification>として直接使用

2. **ConfigurationLoader**: Phase6ステップ18で実装（2025-11-21）
   - DeviceEntry→DeviceSpecification変換
   - 厳密なバリデーション

3. **TargetDeviceConfig設計確定**: Phase6で確定（2025-11-21）
   - List<DeviceSpecification> Devices型
   - DeviceEntryは設定読み込み時の中間型

**設計の進化**:
- **Phase3初期設計**: TargetDeviceConfig.DevicesがList<DeviceEntry>型
- **Phase4/Phase6実装**: TargetDeviceConfig.DevicesがList<DeviceSpecification>型
  - ConfigurationLoaderがDeviceEntry→DeviceSpecification変換を担当
  - アプリケーション内部はDeviceSpecificationのみ使用

---

**作成日**: 2025-11-14
**最終更新**: 2025-11-21（2025-11-21設計変更の影響分析追加、Phase5以降との整合性確認）
**元ドキュメント**: read_to_readrandom_migration_plan.md

---

## 参考: Phase3初期設計との相違点

<details>
<summary>Phase3初期設計のテストコード例（古い仕様、参考用）</summary>

以下は Phase3初期設計時のテストコード例です。実際の実装では:
- ParseDeviceCode()メソッドは削除（ConfigurationLoaderで変換）
- TargetDeviceConfig.DevicesがList<DeviceSpecification>型に変更

```csharp
// 以下は古い設計に基づくサンプルコードです
// 実際の実装はPhase4/Phase6を参照してください

public class ConfigToFrameManagerTests_Phase3InitialDesign
{
    [Fact]
    public void BuildReadRandomFrameFromConfig_ValidDevices_ReturnsCorrectFrame()
    {
        // Phase3初期設計ではTargetDeviceConfig.DevicesがList<DeviceEntry>型だった
        var config = new TargetDeviceConfig
        {
            Devices = new List<DeviceEntry>
            {
                new DeviceEntry { DeviceType = "D", DeviceNumber = 100 }
            }
        };

        // 実際の実装ではDeviceEntryはConfigurationLoaderで変換される
        // ConfigToFrameManagerはList<DeviceSpecification>を受け取る
    }
}
}
```
</details>
