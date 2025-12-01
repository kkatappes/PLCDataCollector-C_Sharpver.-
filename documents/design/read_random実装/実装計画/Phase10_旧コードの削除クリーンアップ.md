# Phase10: 旧コードの削除・クリーンアップ

## ステータス
✅ **ほぼ完了** - 選択肢A（完全削除）が既に実施済み、残りは軽微なクリーンアップのみ

## 概要
Read(0x0401)専用コードの削除判断を行い、プロジェクト方針に基づいてクリーンアップします。

## 前提条件
- ✅ Phase1-9完了: 全機能実装・テスト・実機確認完了
- ✅ ReadRandom(0x0403)が実機環境で正常動作確認済み

---

## 現状分析（2025-11-27時点）

### ✅ 既に完了している削除項目

Phase1-9の実装過程で、Read(0x0401)専用コードは既に削除されています:

1. **SlmpFrameBuilder.BuildReadRequest()メソッド** → ✅ 削除済み
2. **Read(0x0401)用テストコード** → ✅ 削除済み
3. **旧形式設定ファイルサポート（ConvertOldFormatToNewFormat()）** → ✅ 存在せず
4. **旧形式データ出力（OutputReadDataToCsv()）** → ✅ 存在せず
5. **ProcessedResponseDataの旧形式プロパティ** → ✅ 削除済み
   - DeviceCode, StartDeviceNumber, Values, IsReadData等は存在しない
   - 現在はReadRandom専用プロパティのみ（ProcessedData, BitDeviceCount等）

### ⚠️ 残存している0x0401の痕跡

実際の動作には影響しないが、以下の箇所に参照が残存:

1. **PlcCommunicationManager.cs:1109付近**
   - コメント内に検証用フレーム例として0x0401フレームが記載
   - 実装コードには影響なし

2. **統合テストファイル**
   - `Tests/Integration/Step3_6_IntegrationTests.cs`
   - `Tests/Integration/PlcCommunicationManager_IntegrationTests_TC143_10.cs`
   - 過去のテストデータとして0x0401フレームが残存
   - 現在の実行には使用されていない

---

## 実装ステップ

### ステップ29: Read(0x0401)専用コードの削除判断

#### 判断結果: 選択肢A（完全削除）が既に実施済み

**現状**:
- Read(0x0401)の実装コードは既に存在しない
- ReadRandom(0x0403)のみが実装されている
- 選択肢Bを実施する場合は、Read(0x0401)コードの**新規作成**が必要

**結論**:
- 選択肢A（完全削除）が既に完了している
- 残作業は「痕跡のクリーンアップ」のみ

#### ~~選択肢A: 完全削除（ReadRandom(0x0403)のみに統一）~~

**✅ 既に実施済み**

#### ~~選択肢B: 残す（互換性維持、設定で切り替え可能）~~

**注意**: 現状でこの選択肢を実施する場合、Read(0x0401)コードを新規に実装する必要があります。

---

### ステップ30: 残存痕跡のクリーンアップ（軽微な作業）

#### クリーンアップ対象

1. **PlcCommunicationManager.cs:1109付近のコメント**
   - 0x0401フレーム例を削除またはReadRandomフレーム例に更新

2. **統合テストファイル内の0x0401フレーム**
   - `Tests/Integration/Step3_6_IntegrationTests.cs`
   - `Tests/Integration/PlcCommunicationManager_IntegrationTests_TC143_10.cs`
   - 0x0401フレームをReadRandom(0x0403)フレームに更新

#### 実装手順（簡略版）

```bash
# 1. 残存箇所の確認
grep -r "0x0401" andon/Core/
grep -r "54001234000000010401" andon/Tests/

# 2. クリーンアップ実施
# - PlcCommunicationManager.cs: コメント修正
# - テストファイル: 0x0401フレーム → 0x0403フレームに更新

# 3. ビルド確認
dotnet build

# 4. テスト実行
dotnet test

# 5. コミット
git add .
git commit -m "Read(0x0401)痕跡のクリーンアップ完了"
```

#### クリーンアップ後のテスト

```bash
# 全単体テスト実行
dotnet test --filter "FullyQualifiedName~Unit"

# 全統合テスト実行
dotnet test --filter "FullyQualifiedName~Integration"

# ビルド確認
dotnet build andon.sln
```

---

### ~~ステップ31: 選択肢B（切り替え機能実装）の実装~~

**注意**: このステップは現状では実施不要です。選択肢Aが既に完了しているため。

選択肢Bを実施する場合は、Read(0x0401)コードの新規実装が必要になります。以下は参考情報として残します。

<details>
<summary>参考: 選択肢B実装案（クリックで展開）</summary>

#### 実装内容

1. **appsettings.jsonに切り替えフラグを追加**

```json
{
  "PlcConnection": {
    "FrameVersion": "3E",
    "Timeout": 8000,
    "UseReadRandom": true,  // ← 追加

    // ReadRandom用設定
    "Devices": [
      {
        "DeviceType": "D",
        "DeviceNumber": 100
      }
    ],

    // Read用設定（UseReadRandom=falseの場合）
    "StartDevice": "D100",
    "DeviceCount": 10
  }
}
```

2. **ConfigurationLoaderの拡張**

```csharp
public class ConfigurationLoader
{
    public TargetDeviceConfig LoadPlcConnectionConfig()
    {
        var config = new TargetDeviceConfig();

        // 基本設定
        config.FrameVersion = _configuration["PlcConnection:FrameVersion"] ?? "3E";
        config.Timeout = int.Parse(_configuration["PlcConnection:Timeout"] ?? "8000");
        config.UseReadRandom = bool.Parse(_configuration["PlcConnection:UseReadRandom"] ?? "true");

        if (config.UseReadRandom)
        {
            // ReadRandom用設定読み込み
            config.Devices = LoadDevicesList();
        }
        else
        {
            // Read用設定読み込み
            config.StartDevice = _configuration["PlcConnection:StartDevice"];
            config.DeviceCount = int.Parse(_configuration["PlcConnection:DeviceCount"] ?? "0");
        }

        return config;
    }
}
```

3. **PlcCommunicationManagerの拡張**

```csharp
public class PlcCommunicationManager
{
    private readonly TargetDeviceConfig _config;

    public async Task<ProcessedResponseData> ReadDevicesAsync()
    {
        if (_config.UseReadRandom)
        {
            // ReadRandom(0x0403)実行
            return await ReadDevicesWithReadRandomAsync();
        }
        else
        {
            // Read(0x0401)実行（旧方式）
            return await ReadDevicesWithReadAsync();
        }
    }

    private async Task<ProcessedResponseData> ReadDevicesWithReadRandomAsync()
    {
        var sendFrame = _frameManager.BuildReadRandomFrameFromConfig(_config);
        await SendFrameAsync(sendFrame);
        var responseFrame = await ReceiveResponseAsync();
        var parsedData = SlmpDataParser.ParseReadRandomResponse(responseFrame, _config.Devices);
        return new ProcessedResponseData(parsedData);
    }

    private async Task<ProcessedResponseData> ReadDevicesWithReadAsync()
    {
        var sendFrame = _frameManager.BuildReadRequestFromConfig(_config);
        await SendFrameAsync(sendFrame);
        var responseFrame = await ReceiveResponseAsync();
        var parsedData = SlmpDataParser.ParseReadResponse(responseFrame);
        return new ProcessedResponseData(parsedData);
    }
}
```

4. **TargetDeviceConfigの拡張**

```csharp
public class TargetDeviceConfig
{
    public string FrameVersion { get; set; } = "3E";
    public int Timeout { get; set; } = 8000;

    /// <summary>
    /// ReadRandom(0x0403)を使用するか（デフォルト: true）
    /// </summary>
    public bool UseReadRandom { get; set; } = true;

    // ReadRandom用設定
    public List<DeviceEntry> Devices { get; set; } = new();

    // Read用設定（UseReadRandom=falseの場合）
    public string? StartDevice { get; set; }
    public int? DeviceCount { get; set; }
}
```

</details>

---

## 完了条件

### 現在の状況（選択肢A完了済み）
- ✅ Read(0x0401)専用コード完全削除（Phase1-9で実施済み）
- ✅ 全テストパス（ReadRandomのみ）
- ✅ ビルド成功
- ⏳ 残存痕跡のクリーンアップ（軽微な作業）

### ~~選択肢B（切り替え機能）の場合~~
実施不要（選択肢Aが既に完了しているため）

## 次フェーズへの依存関係
- Phase11（ドキュメント更新）で、削除・変更内容をドキュメントに反映します

## リスク管理
| リスク | 影響 | 対策 |
|--------|------|------|
| **コメント・テスト更新時のミス** | 低 | ・動作に影響しない箇所のみ<br>・テスト実行で確認 |
| ~~**誤削除**~~ | なし | 削除対象コードは既に存在しない |
| ~~**互換性喪失（選択肢A）**~~ | なし | 既に実施済み |

---

**作成日**: 2025-11-18
**更新日**: 2025-11-27（現状反映）
**元ドキュメント**: read_to_readrandom_migration_plan.md
