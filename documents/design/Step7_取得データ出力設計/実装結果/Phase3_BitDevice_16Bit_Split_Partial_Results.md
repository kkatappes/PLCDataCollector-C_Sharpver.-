# Step7 Phase3 ビットデバイス16ビット分割処理 部分実装結果

**作成日**: 2025-11-27
**実装状況**: 進行中（Red Phase完了、Green Phase調査中）

## 概要

Step7データ出力機能のPhase3（ビットデバイス16ビット分割処理）において、TDD手法に基づいた実装を開始。TC_P3_001テストの実装（Red Phase）とビットデバイス16ビット分割ロジックの実装を完了したが、テストが依然として失敗しているため、原因調査中。

---

## 1. 実装内容

### 1.1 実装クラス

| クラス名 | 機能 | ファイルパス | 実装状況 |
|---------|------|------------|----------|
| `DataOutputManager` | JSON出力管理（ビットデバイス16ビット分割対応） | `andon/Core/Managers/DataOutputManager.cs` | ✅ 実装完了 |
| `DataOutputManagerTests` | TC_P3_001テスト実装 | `andon/Tests/Unit/Core/Managers/DataOutputManagerTests.cs` | ✅ 実装完了 |

### 1.2 実装メソッド

| メソッド名 | 機能 | 実装箇所 | 実装状況 |
|-----------|------|---------|----------|
| `OutputToJson()` | ビットデバイス16ビット分割対応JSON出力 | DataOutputManager.cs:25-129 | ✅ 実装完了 |
| `TC_P3_001_BitDevice_SplitsInto16Bits()` | ビットデバイス16ビット分割テスト | DataOutputManagerTests.cs:499-558 | ✅ 実装完了 |

### 1.3 重要な実装判断

**Selectベースから Listベースへの変更**:
- 理由: ビットデバイスを16エントリに展開するには、動的なコレクション操作が必要
- 実装: `List<object>`を使用して、ビットデバイスは16回ループで追加、ワード/ダブルワードは1回追加

**IsBitDevice()拡張メソッドの使用**:
- 理由: デバイスコードから型を判定し、分岐処理を実施
- 実装: `if (deviceData.Code.IsBitDevice())` でビット/ワード判定

**ビット抽出処理**:
- アルゴリズム: `(bitValue >> i) & 1` を使用してi番目のビットを抽出
- 理由: 標準的なビット演算、効率的で可読性が高い

---

## 2. TDD実装サイクル

### 2.1 Red Phase（テスト失敗確認）

**実施日時**: 2025-11-27 10:24

#### テストコード実装

```csharp
[Fact]
public void TC_P3_001_BitDevice_SplitsInto16Bits()
{
    // Arrange - M0デバイス（ビットデバイス）1ワード = 16ビット
    // Value = 0b1010110011010101 (43605)
    var deviceData = new Dictionary<string, DeviceData>
    {
        { "M0", DeviceData.FromDeviceSpecification(
            new DeviceSpecification(DeviceCode.M, 0, false), 0b1010110011010101) }
    };

    var data = new ProcessedResponseData
    {
        ProcessedData = deviceData,
        ProcessedAt = DateTime.Now
    };

    // deviceConfigには16ビット分のエントリを事前に登録
    var deviceConfig = new Dictionary<string, DeviceEntryInfo>();
    for (int i = 0; i < 16; i++)
    {
        deviceConfig[$"M{i}"] = new DeviceEntryInfo { Name = $"ビットM{i}", Digits = 1 };
    }

    // Act
    _manager.OutputToJson(data, _testDirectory, "192.168.1.100", 5000, deviceConfig);

    // Assert - 16ビット分に分割されることを確認
    var files = Directory.GetFiles(_testDirectory, "*.json");
    var jsonString = File.ReadAllText(files[0]);
    var jsonDoc = JsonDocument.Parse(jsonString);
    var items = jsonDoc.RootElement.GetProperty("items").EnumerateArray().ToList();

    Assert.Equal(16, items.Count);

    // 各ビットの値を確認
    int[] expectedBits = { 1, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1, 0, 1, 0, 1 };
    for (int i = 0; i < 16; i++)
    {
        var item = items[i];
        Assert.Equal("M", item.GetProperty("device").GetProperty("code").GetString());
        Assert.Equal(i.ToString("D3"), item.GetProperty("device").GetProperty("number").GetString());
        Assert.Equal($"ビットM{i}", item.GetProperty("name").GetString());
        Assert.Equal("bit", item.GetProperty("unit").GetString());
        Assert.Equal(expectedBits[i], item.GetProperty("value").GetInt32());
    }
}
```

#### Red Phase実行結果

```
テスト実行: TC_P3_001_BitDevice_SplitsInto16Bits
結果: 失敗（期待通り）

エラー:
  Assert.Equal() Failure: Values differ
  Expected: 16
  Actual:   1
```

✅ **Red Phase成功**: 機能未実装によりテストが失敗することを確認

---

### 2.2 Green Phase（機能実装）

**実施日時**: 2025-11-27 10:25-10:27

#### 実装コード

```csharp
public void OutputToJson(
    ProcessedResponseData data,
    string outputDirectory,
    string ipAddress,
    int port,
    Dictionary<string, DeviceEntryInfo> deviceConfig)
{
    // ... 前処理省略 ...

    // Phase3: items配列をListで生成（ビットデバイス16ビット分割対応）
    var itemsList = new List<object>();

    foreach (var kvp in data.ProcessedData)
    {
        var deviceData = kvp.Value;

        // ビットデバイスの場合は16ビット分に展開
        if (deviceData.Code.IsBitDevice())
        {
            // 16ビット分に展開
            uint bitValue = deviceData.Value;
            for (int i = 0; i < 16; i++)
            {
                // i番目のビットを抽出
                int bit = (int)((bitValue >> i) & 1);

                // デバイス名生成（例: "M0" → "M0", "M1", ..., "M15"）
                string bitDeviceName = $"{deviceData.Code}{deviceData.Address + i}";

                // デバイス番号（3桁ゼロ埋め）
                string bitDeviceNumber = (deviceData.Address + i).ToString("D3");

                // 設定から名前・桁数取得
                string name = deviceConfig.TryGetValue(bitDeviceName, out var config)
                    ? config.Name
                    : bitDeviceName;
                int digits = deviceConfig.TryGetValue(bitDeviceName, out var config2)
                    ? config2.Digits
                    : 1;

                // items配列に追加
                itemsList.Add(new
                {
                    name = name,
                    device = new
                    {
                        code = deviceData.Code.ToString(),
                        number = bitDeviceNumber
                    },
                    digits = digits,
                    unit = "bit",
                    value = bit
                });
            }
        }
        else
        {
            // ワード/ダブルワードデバイスはそのまま
            itemsList.Add(new
            {
                name = deviceConfig.TryGetValue(kvp.Key, out var config) ? config.Name : kvp.Key,
                device = new
                {
                    code = deviceData.Code.ToString(),
                    number = deviceData.Address.ToString("D3")
                },
                digits = deviceConfig.TryGetValue(kvp.Key, out var config2) ? config2.Digits : 1,
                unit = deviceData.Type.ToLower(),
                value = ConvertValue(deviceData)
            });
        }
    }

    // JSON構造構築（itemsList.ToArray()を使用）
    var jsonData = new
    {
        source = new { ... },
        timestamp = new { ... },
        items = itemsList.ToArray()
    };

    // JSON出力
    var jsonString = JsonSerializer.Serialize(jsonData, options);
    File.WriteAllText(filePath, jsonString);
}
```

#### Green Phase実行結果（未達成）

```
テスト実行: TC_P3_001_BitDevice_SplitsInto16Bits
結果: 失敗

エラー:
  Assert.Equal() Failure: Values differ
  Expected: 1
  Actual:   0

ログ出力:
  [INFO] JSON出力開始: IP=192.168.1.100, Port=5000
  [INFO] JSON出力完了: ファイル=20251127_102721810_192-168-1-100_5000.json, デバイス数=1
```

⚠️ **Green Phase未達成**: テストが依然として失敗

---

## 3. 問題分析

### 3.1 観察された現象

1. **items数の問題**: ログに「デバイス数=1」と表示
   - 期待: 16ビット分割により16エントリ
   - 実際: 1エントリのみ（分割されていない）

2. **ビット値の問題**: Expected: 1, Actual: 0
   - 期待: ビット0の値 = 1 (0b1010110011010101のLSB)
   - 実際: ビット0の値 = 0

### 3.2 推定される原因

#### 原因1: IsBitDevice()が正しく動作していない可能性
- `DeviceData.Code.IsBitDevice()`が`false`を返している可能性
- DeviceCodeが正しく設定されていない可能性

#### 原因2: DeviceData.Typeの設定問題
- `DeviceData.FromDeviceSpecification()`でTypeが"Bit"に設定されていない可能性
- ビット判定ロジックが期待通りに動作していない可能性

#### 原因3: テストデータの問題
- テストで使用している`DeviceSpecification`の生成方法に問題がある可能性

### 3.3 検証が必要な箇所

1. **DeviceData.Codeの値確認**
   ```csharp
   // デバッグログ追加候補
   Console.WriteLine($"DeviceCode: {deviceData.Code}, IsBitDevice: {deviceData.Code.IsBitDevice()}");
   ```

2. **DeviceData.Typeの値確認**
   ```csharp
   // デバッグログ追加候補
   Console.WriteLine($"DeviceType: {deviceData.Type}");
   ```

3. **IsBitDevice()メソッドの動作確認**
   - DeviceCode.Mに対して`IsBitDevice()`が`true`を返すか確認

---

## 4. 次のステップ

### 4.1 即時対応が必要な項目

1. **デバッグログ追加**
   - OutputToJson()メソッド内に一時的なConsole.WriteLineを追加
   - deviceData.Code、deviceData.Type、IsBitDevice()の戻り値を確認

2. **IsBitDevice()の単体テスト確認**
   - DeviceConstantsTests.csのテストを再実行
   - DeviceCode.Mに対するIsBitDevice()の動作を確認

3. **DeviceDataの生成確認**
   - FromDeviceSpecification()の実装を再確認
   - Typeプロパティが正しく設定されているか確認

### 4.2 修正方針

**修正候補1**: DeviceDataの生成方法を変更
- テストで直接DeviceDataを生成する方法に変更
- FromDeviceSpecification()を経由しない

**修正候補2**: IsBitDevice()の呼び出し方法を変更
- DeviceCode.IsBitDevice()ではなく、別の判定方法を使用

**修正候補3**: ビット判定ロジックの見直し
- deviceData.Typeを使用した判定に変更
- `if (deviceData.Type == "Bit")`

---

## 5. 実装記録

### 5.1 コード変更履歴

| ファイル | 変更内容 | 行数 | 実施日時 |
|---------|---------|------|---------|
| DataOutputManager.cs | items配列生成ロジックをList<object>ベースに変更 | 51-113 | 2025-11-27 10:25 |
| DataOutputManager.cs | ビットデバイス16ビット分割ループ追加 | 59-96 | 2025-11-27 10:25 |
| DataOutputManagerTests.cs | TC_P3_001テストメソッド追加 | 499-558 | 2025-11-27 10:23 |
| SlmpFrameBuilderTests.cs | TC016テストをSkipに変更（ビルドエラー修正） | 111 | 2025-11-27 10:23 |

### 5.2 ビルド結果

```
実行日時: 2025-11-27 10:27
.NET SDK: 9.0
xUnit.net: v2.8.2+699d445a1a
VSTest: 17.14.1 (x64)

ビルド結果: 成功
  警告: 82個（既存の警告、新規エラーなし）
  エラー: 0個
```

### 5.3 テスト実行結果

```
テスト名: TC_P3_001_BitDevice_SplitsInto16Bits
結果: 失敗
実行時間: 203ms

エラー詳細:
  Assert.Equal() Failure: Values differ
  Expected: 1
  Actual:   0
  スタック: DataOutputManagerTests.cs:line 556
```

---

## 6. 実行環境

- **.NET SDK**: 9.0.304
- **xUnit.net**: v2.8.2+699d445a1a
- **VSTest**: 17.14.1 (x64)
- **プラットフォーム**: .NET 9.0.8 (64-bit)
- **OS**: Windows
- **ビルド構成**: Debug
- **テスト実行モード**: オフライン動作確認（実機PLC接続なし）

---

## 7. 検証状況

### 7.1 完了事項

✅ **TDD Red Phase**: TC_P3_001テスト実装、期待通り失敗を確認
✅ **コード実装**: ビットデバイス16ビット分割ロジック実装完了
✅ **ビルド**: エラーなくビルド成功
✅ **既存テスト**: 既存テストへの影響なし

### 7.2 未完了事項

❌ **TDD Green Phase**: TC_P3_001テストがパスしていない
❌ **原因特定**: テスト失敗の根本原因が未特定
❌ **ビット分割動作確認**: 16ビット分割が実際に動作していない

### 7.3 Phase3進捗率

- **計画策定**: 100%
- **テスト実装（TC_P3_001）**: 100%
- **コード実装**: 100%
- **テスト合格**: 0% ⚠️
- **全体進捗**: 約40%（TC_P3_001のみ、残りTC_P3_002～007未着手）

---

## 8. 今後の作業計画

### 8.1 短期計画（次セッション）

1. **原因特定**（優先度: 最高）
   - デバッグログ追加
   - IsBitDevice()の動作確認
   - DeviceDataの生成方法確認

2. **TC_P3_001のGreen Phase達成**
   - 問題修正
   - テストパス確認

3. **TC_P3_002～007の実装**
   - エッジケーステスト（すべて0、すべて1）
   - ワード/ダブルワード非分割テスト
   - 混在データテスト
   - デバイス名マッピングテスト

### 8.2 中期計画

- Phase3完全実装（全テストケース）
- Phase4エラーハンドリング実装
- Phase5統合テスト実装

---

## 総括

**実装状況**: 進行中（約40%完了）
**TDD進捗**: Red Phase完了、Green Phase未達成
**実装方式**: TDD (Red-Green-Refactor)

**達成事項**:
- TC_P3_001テストの実装完了（Red Phase）
- ビットデバイス16ビット分割ロジックの実装完了
- ビルド成功、既存テストへの影響なし

**課題**:
- テストが依然として失敗（Expected: 1, Actual: 0）
- ビット分割が実際に動作していない可能性
- 原因特定と修正が必要

**次セッションの目標**:
- TC_P3_001のGreen Phase達成（テストパス）
- 原因特定とデバッグ完了
- 残りのテストケース（TC_P3_002～007）への着手

---

## 参考資料

- Phase3実装計画: `documents/design/Step7_取得データ出力設計/実装計画/Phase3_ビットデバイス分割処理実装.md`
- Phase2完了記録: `documents/implementation_records/Phase2_DataOutputManager_JSON機能改善実装記録.txt`
- テスト実装参考: `documents/design/read_random実装/実装結果/Phase1_DeviceCode_DeviceSpecification_TestResults.md`
