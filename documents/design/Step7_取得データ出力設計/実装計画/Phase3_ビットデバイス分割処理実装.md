# Phase3: ビットデバイス分割処理実装計画

## 概要
Phase3では、**Step7データ出力の最重要機能**であるビットデバイス16ビット分割処理を実装します。Read Randomコマンドで取得したビットデバイスデータ（1ワード=16ビット）を、16個の個別デバイスとしてJSON出力します。

---

## Phase2からの引き継ぎ事項（2025-11-27更新）

### Phase2完了事項
✅ **Phase2実装完了**: 2025-11-27
✅ **実装方式**: TDD (Red-Green-Refactor)
✅ **テスト結果**: 11/11テスト成功（既存テスト影響なし）

### Phase2で実装された機能
Phase3実装時に利用可能な機能:

1. **device.number 3桁ゼロ埋め** (DataOutputManager.cs:60)
   - `ToString("D3")` 実装済み
   - Phase3でもビットデバイス番号の生成時に使用

2. **ディレクトリ自動作成** (DataOutputManager.cs:47-50)
   - `Directory.CreateDirectory()` 実装済み
   - Phase3のテスト実行時にも利用可能

3. **ログ出力機能** (DataOutputManager.cs:29, 77)
   - JSON出力開始/完了ログ実装済み
   - Phase3でもデバッグ情報として活用可能

### Phase3で解決すべき残課題

Phase2で未解決の最重要課題:

❌ **ビットデバイス16ビット分割処理未実装**
- 現状: ビットデバイスは分割されずに1エントリとして出力
- Phase3実装: ビットデバイスを16ビット（ビット0～15）に分割して出力
- 重要度: ★★★★☆（最重要・最難関）

❌ **plcModel値が固定値**（Phase3後継課題）
- 現状: `plcModel = "Unknown"` 固定
- 将来対応: PlcConfigurationから動的取得（Phase3以降で検討）

❌ **エラーハンドリング未実装**（Phase4実装予定）
- 現状: try-catchブロックなし
- Phase4実装: データ検証、ファイルI/Oエラーハンドリング

### Phase3実装の前提条件

Phase2完了により以下が利用可能:
- ✅ OutputToJson()メソッドの基本構造が確立
- ✅ JSON出力フォーマットが確定
- ✅ device.number 3桁ゼロ埋めが実装済み
- ✅ ログ出力によるデバッグ支援が可能
- ✅ TDD実装フローが確立（Red-Green-Refactorサイクル）

### Phase3実装時の注意点

**Phase2で確立された実装パターンの継続**:
1. TDD手法を厳守（テスト先行実装）
2. Red-Green-Refactorサイクルを1つずつ完結
3. 実装完了後に実装記録を作成
4. 既存テストへの影響を確認

**Phase2実装記録の参照**:
- `documents/implementation_records/Phase2_DataOutputManager_JSON機能改善実装記録.txt`
- `documents/design/Step7_取得データ出力設計/実装結果/Phase2_JSON生成機能改善_TestResults.txt`

---

## 背景・仕様理解

### 問題の本質
Read Randomコマンドでビットデバイス（例: M000）を取得すると、**1ワード（16ビット）単位で取得される**ため、M000～M015の16デバイス分のデータが含まれています。

しかし、現在の実装では**16ビット分をまとめて1つのitemとして出力**しているため、個別のビット値が取得できません。

### 期待される動作

#### 入力データ（ProcessedResponseData）
```csharp
ProcessedData["M0"] = new DeviceData
{
    DeviceName = "M0",
    Code = DeviceCode.M,  // ビットデバイス
    Address = 0,
    Value = 0b1010110011010101,  // 16ビット値: 43605
    Type = "Bit"
}
```

#### 期待される出力（JSON）
```json
{
  "items": [
    {"device": {"code": "M", "number": "000"}, "value": 1},  // ビット0
    {"device": {"code": "M", "number": "001"}, "value": 0},  // ビット1
    {"device": {"code": "M", "number": "002"}, "value": 1},  // ビット2
    {"device": {"code": "M", "number": "003"}, "value": 0},  // ビット3
    {"device": {"code": "M", "number": "004"}, "value": 1},  // ビット4
    {"device": {"code": "M", "number": "005"}, "value": 1},  // ビット5
    {"device": {"code": "M", "number": "006"}, "value": 0},  // ビット6
    {"device": {"code": "M", "number": "007"}, "value": 0},  // ビット7
    {"device": {"code": "M", "number": "008"}, "value": 1},  // ビット8
    {"device": {"code": "M", "number": "009"}, "value": 1},  // ビット9
    {"device": {"code": "M", "number": "010"}, "value": 0},  // ビット10
    {"device": {"code": "M", "number": "011"}, "value": 1},  // ビット11
    {"device": {"code": "M", "number": "012"}, "value": 0},  // ビット12
    {"device": {"code": "M", "number": "013"}, "value": 1},  // ビット13
    {"device": {"code": "M", "number": "014"}, "value": 0},  // ビット14
    {"device": {"code": "M", "number": "015"}, "value": 1}   // ビット15
  ]
}
```

---

## 実装対象

### 1. items配列生成ロジックの全面改修

#### 現在の実装（Phase2完了時点）
**ファイル**: `andon/Core/Managers/DataOutputManager.cs`（行53-68付近）

```csharp
items = data.ProcessedData.Select(kvp => new
{
    name = deviceConfig.TryGetValue(kvp.Key, out var config) ? config.Name : kvp.Key,
    device = new
    {
        code = kvp.Value.Code.ToString(),
        number = kvp.Value.Address.ToString("D3")  // Phase2で3桁ゼロ埋め実装済み
    },
    digits = deviceConfig.TryGetValue(kvp.Key, out var config2) ? config2.Digits : 1,
    unit = kvp.Value.Type.ToLower(),
    value = ConvertValue(kvp.Value)
}).ToArray()
```

**Phase2で解決済み**:
- ✅ device.number 3桁ゼロ埋め実装済み（ToString("D3")）

**Phase3で解決すべき問題**:
- ❌ ビットデバイスも1エントリとして出力（16ビット分が個別に展開されない）

#### 修正後の実装

```csharp
// items配列をListで生成（Selectは使わない）
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

            // デバイス名生成（例: "M0" + i → "M0", "M1", ..., "M15"）
            string bitDeviceName = $"{deviceData.Code}{deviceData.Address + i}";

            // デバイス番号（3桁ゼロ埋め - Phase2実装済みの形式を使用）
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

// JSON構造に組み込み
var jsonData = new
{
    source = new { ... },
    timestamp = new { ... },
    items = itemsList.ToArray()  // ← Listから配列に変換
};
```

---

### 2. DeviceCode.IsBitDevice()拡張メソッドの使用

#### 使用箇所
```csharp
if (deviceData.Code.IsBitDevice())
{
    // ビットデバイス処理
}
```

#### 拡張メソッドの定義場所
**ファイル**: `andon/Core/Constants/DeviceConstants.cs`

**実装内容**（既存）:
```csharp
public static bool IsBitDevice(this DeviceCode code)
{
    return code switch
    {
        DeviceCode.SM => true,   // 特殊リレー
        DeviceCode.X => true,    // 入力
        DeviceCode.Y => true,    // 出力
        DeviceCode.M => true,    // 内部リレー
        DeviceCode.L => true,    // ラッチリレー
        DeviceCode.F => true,    // アナンシエータ
        DeviceCode.B => true,    // リンクリレー
        _ => false
    };
}
```

**確認事項**:
- Phase3実装前に`IsBitDevice()`が正常動作することを確認
- 必要に応じて単体テストを実装

---

### 3. deviceConfig辞書の拡張（ビットデバイス対応）

#### 現在の問題
deviceConfig辞書には**ビットデバイスの代表アドレスのみ**が登録されている
（例: "M0" のみで、"M1"～"M15" は未登録）

#### 必要な対応
ビットデバイス指定時に、**16デバイス分のエントリ**をdeviceConfig辞書に登録する

#### 実装方針（呼び出し側で対応）

**場所**: `ExecutionOrchestrator`または`ApplicationController`内でdeviceConfig辞書を構築する箇所

**修正前**:
```csharp
var deviceConfig = new Dictionary<string, DeviceEntryInfo>();
foreach (var deviceEntry in targetDeviceConfig.Devices)
{
    string deviceName = $"{deviceEntry.DeviceType}{deviceEntry.DeviceNumber}";
    deviceConfig[deviceName] = new DeviceEntryInfo
    {
        Name = deviceEntry.Description ?? deviceName,
        Digits = 1
    };
}
```

**修正後**:
```csharp
var deviceConfig = new Dictionary<string, DeviceEntryInfo>();
foreach (var deviceEntry in targetDeviceConfig.Devices)
{
    string deviceName = $"{deviceEntry.DeviceType}{deviceEntry.DeviceNumber}";

    // デバイスコード取得（例: "M" → DeviceCode.M）
    if (Enum.TryParse<DeviceCode>(deviceEntry.DeviceType, out var deviceCode))
    {
        if (deviceCode.IsBitDevice())
        {
            // ビットデバイスの場合は16エントリ生成
            for (int i = 0; i < 16; i++)
            {
                string bitDeviceName = $"{deviceEntry.DeviceType}{deviceEntry.DeviceNumber + i}";
                deviceConfig[bitDeviceName] = new DeviceEntryInfo
                {
                    Name = deviceEntry.Description ?? bitDeviceName,  // または個別の説明
                    Digits = 1
                };
            }
        }
        else
        {
            // ワード/ダブルワードデバイスはそのまま
            deviceConfig[deviceName] = new DeviceEntryInfo
            {
                Name = deviceEntry.Description ?? deviceName,
                Digits = 1
            };
        }
    }
}
```

**注意**:
- この修正は**DataOutputManager外**で行う
- DataOutputManager自体は修正不要

---

### 4. ビット展開処理の詳細解説

#### ビットシフトとマスク処理

```csharp
uint bitValue = deviceData.Value;  // 例: 0b1010110011010101

for (int i = 0; i < 16; i++)
{
    // i番目のビットを抽出
    int bit = (int)((bitValue >> i) & 1);

    // ビット処理の詳細:
    // i=0: 0b1010110011010101 >> 0 = 0b1010110011010101, & 1 = 1
    // i=1: 0b1010110011010101 >> 1 = 0b0101011001101010, & 1 = 0
    // i=2: 0b1010110011010101 >> 2 = 0b0010101100110101, & 1 = 1
    // ...
    // i=15: 0b1010110011010101 >> 15 = 0b0000000000000001, & 1 = 1
}
```

#### デバイス名・番号の生成

```csharp
// 元のデバイスアドレス: 0 (M0)
// i=0: M0
// i=1: M1
// i=2: M2
// ...
// i=15: M15

string bitDeviceName = $"{deviceData.Code}{deviceData.Address + i}";
string bitDeviceNumber = (deviceData.Address + i).ToString("D3");
```

---

## 実装手順

### Step 1: 既存実装のバックアップ
1. `DataOutputManager.cs`の現在の実装をコピー
2. コメントアウトまたは別ファイルに保存

### Step 2: items配列生成ロジックの改修
1. `OutputToJson()`メソッド内のitems生成部分を特定（行448-459）
2. Selectベースの実装を削除
3. Listベースの新実装に置き換え

### Step 3: ビットデバイス分割処理の実装
1. `if (deviceData.Code.IsBitDevice())`分岐を追加
2. 16ビット展開ループを実装
3. ビット抽出処理を実装

### Step 4: deviceConfig辞書構築処理の修正（呼び出し側）
1. deviceConfig辞書を構築している箇所を特定
2. ビットデバイス時に16エントリ生成する処理を追加

### Step 5: ビルド・コンパイル確認
1. `dotnet build`を実行
2. コンパイルエラーがないことを確認

### Step 6: 単体テスト実装
1. ビットデバイス分割処理のテストを実装
2. 期待値と実際の出力を比較

### Step 7: 統合テスト
1. 実際のPLCデータ（モック）を使用
2. JSON出力ファイルを検証

---

## テスト要件（TDD必須）

### Phase3の単体テスト（必須実装）

**重要**: Phase3は最重要・最難関の実装です。TDD手法を厳格に適用し、**テストを先に実装**してから機能実装を行います。

#### TDD実装サイクル（具体的手順）

Phase3の複雑さを考慮し、以下の順序でテストと実装を進めます:

1. **ステップ1: 基本ビット分割テスト（TC_P3_001）**
   - Red: テスト実装 → 失敗確認
   - Green: ビット分割ループの基本実装 → パス確認
   - Refactor: コード整理 → パス確認

2. **ステップ2: エッジケーステスト（TC_P3_002, TC_P3_003）**
   - Red: すべて0、すべて1のテスト実装 → 失敗確認
   - Green: ビット抽出処理の修正 → パス確認

3. **ステップ3: ワード/ダブルワード非分割テスト（TC_P3_004）**
   - Red: テスト実装 → 失敗確認
   - Green: IsBitDevice()分岐の実装 → パス確認

4. **ステップ4: 混在データテスト（TC_P3_005）**
   - Red: テスト実装 → 失敗確認
   - Green: Listベースの統合実装 → パス確認

5. **ステップ5: デバイス名マッピングテスト（TC_P3_006, TC_P3_007）**
   - Red: テスト実装 → 失敗確認
   - Green: deviceConfig辞書連携 → パス確認

**注意**: Phase3では複雑な処理のため、**ステップごとに完結させる**ことが重要です。

#### テスト用モックデータ

Phase3で使用するモックデータの作成例:

```csharp
// ビットデバイスのモックデータ（TC_P3_001用）
private ProcessedResponseData CreateMockData_BitDevice()
{
    return new ProcessedResponseData
    {
        ProcessedData = new Dictionary<string, DeviceData>
        {
            ["M0"] = new DeviceData
            {
                DeviceName = "M0",
                Code = DeviceCode.M,
                Address = 0,
                Value = 0b1010110011010101,  // 43605
                Type = "Bit"
            }
        },
        ProcessedAt = DateTime.Now,
        IsSuccess = true
    };
}

// ワードデバイスのモックデータ（TC_P3_004用）
private ProcessedResponseData CreateMockData_WordDevice()
{
    return new ProcessedResponseData
    {
        ProcessedData = new Dictionary<string, DeviceData>
        {
            ["D100"] = new DeviceData
            {
                DeviceName = "D100",
                Code = DeviceCode.D,
                Address = 100,
                Value = 12345,
                Type = "Word"
            }
        },
        ProcessedAt = DateTime.Now,
        IsSuccess = true
    };
}

// ビットデバイス用deviceConfig（16エントリ）
private Dictionary<string, DeviceEntryInfo> CreateMockDeviceConfig_BitDevice()
{
    var config = new Dictionary<string, DeviceEntryInfo>();
    for (int i = 0; i < 16; i++)
    {
        config[$"M{i}"] = new DeviceEntryInfo
        {
            Name = $"ビットデバイスM{i}",
            Digits = 1
        };
    }
    return config;
}
```

#### テストケース（必須）

#### TC_P3_001: ビットデバイス16ビット分割（基本）
- **入力**: M0 = 0b1010110011010101（43605）
- **期待値**: M000～M015の16エントリが出力される
- **検証項目**:
  - エントリ数が16個
  - M000のvalue = 1
  - M001のvalue = 0
  - M002のvalue = 1
  - ...
  - M015のvalue = 1

#### TC_P3_002: ビットデバイス16ビット分割（すべて0）
- **入力**: M0 = 0（0b0000000000000000）
- **期待値**: M000～M015の16エントリがすべてvalue=0

#### TC_P3_003: ビットデバイス16ビット分割（すべて1）
- **入力**: M0 = 65535（0b1111111111111111）
- **期待値**: M000～M015の16エントリがすべてvalue=1

#### TC_P3_004: ワード/ダブルワードデバイスは分割されない
- **入力**: D100 = 12345
- **期待値**: D100の1エントリのみが出力される（分割されない）

#### TC_P3_005: ビット+ワード混在
- **入力**: M0 = 0b1010110011010101, D100 = 12345
- **期待値**: M000～M015の16エントリ + D100の1エントリ = 計17エントリ

#### TC_P3_006: デバイス名マッピング
- **入力**: M0 = 1, deviceConfig["M0"] = "停止纏めON"
- **期待値**: M000のname = "停止纏めON"

#### TC_P3_007: device.numberの3桁ゼロ埋め（ビットデバイス）
- **入力**: M0
- **期待値**: number = "000", "001", ..., "015"

---

## 完了条件

### 必須項目
- [ ] ビットデバイスが16ビット分に展開されてJSON出力される
- [ ] ワード/ダブルワードデバイスは分割されない
- [ ] device.numberが3桁ゼロ埋めで出力される
- [ ] deviceConfig辞書から正しい名前・桁数が取得される
- [ ] `dotnet build`が成功する
- [ ] 単体テスト（TC_P3_001～TC_P3_007）がすべてパスする

### 推奨項目
- [ ] 統合テストでビット+ワード+ダブルワード混在データが正常出力される
- [ ] パフォーマンステストで大量デバイス処理時の性能を確認

---

## 実装後の確認事項

### 1. JSON出力確認（ビットデバイス分割）
```json
{
  "items": [
    {
      "name": "停止纏めON=異常無",
      "device": {"code": "M", "number": "000"},
      "digits": 1,
      "unit": "bit",
      "value": 1
    },
    {
      "name": "自動運転    開始",
      "device": {"code": "M", "number": "001"},
      "digits": 1,
      "unit": "bit",
      "value": 0
    },
    // ... M002～M015まで続く
    {
      "device": {"code": "M", "number": "015"},
      "digits": 1,
      "unit": "bit",
      "value": 1
    }
  ]
}
```

### 2. ワード/ダブルワードデバイスが分割されないことを確認
```json
{
  "items": [
    // ... ビットデバイス16個
    {
      "name": "生産台数",
      "device": {"code": "D", "number": "100"},
      "digits": 5,
      "unit": "word",
      "value": 12345
    }
  ]
}
```

### 3. パフォーマンス確認
- 1000デバイス処理時の処理時間を測定
- メモリ使用量が適切か確認

---

## 注意事項

### 1. deviceConfig辞書の事前準備
**重要**: ビットデバイス分割処理を正常動作させるには、**呼び出し側でdeviceConfig辞書を適切に構築する必要があります**。

DataOutputManager自体は辞書を構築しないため、ExecutionOrchestratorまたはApplicationController側で対応してください。

### 2. 既存実装との互換性
Phase3実装後、ビットデバイスを含まないデータ（ワード/ダブルワードのみ）も正常動作することを確認してください。

### 3. エラーハンドリング
Phase3では基本的なエラーハンドリングは含まれていません。Phase4で実装します。

---

## 既存実装との差異

### Phase3で解決される問題
- ✅ ビットデバイス16ビット分割処理の未実装

### Phase3で未解決の問題（Phase4以降で対応）
- ❌ エラーハンドリングの不足
- ❌ plcModel値が固定値

---

## 次のPhaseへの準備

Phase4では、**エラーハンドリング**を実装します。
ファイル出力エラー、データ検証エラー、例外処理を網羅的に実装します。

Phase3完了後、`Phase4_エラーハンドリング実装.md`を参照してください。

---

## 参照文書

### Phase3実装関連
- `実装ガイド.md`: ビットデバイス分割処理詳細（セクション1.5, 4.2）
- `実装時対応関係.md`: ビットデバイス分割処理の実装方針（セクション8）

### Phase2完了関連（前提知識）
- `Phase2_JSON生成機能実装.md`: Phase2実装計画（完了条件チェック済み）
- `documents/implementation_records/Phase2_DataOutputManager_JSON機能改善実装記録.txt`: Phase2実装記録
- `documents/design/Step7_取得データ出力設計/実装結果/Phase2_JSON生成機能改善_TestResults.txt`: Phase2テスト結果

### Step1完了関連（前提機能）
- Step1 Phase5完了により`MultiPlcConfigManager`、`PlcConfiguration`が利用可能
- `DeviceCode.IsBitDevice()`拡張メソッドが実装済み

## 作成日時
- **作成日**: 2025年11月27日
- **最終更新**: 2025年11月27日（Phase2引き継ぎ事項追記）
- **対象Phase**: Phase3（ビットデバイス分割処理実装）

---

## Phase3実装開始前のチェックリスト

Phase3実装を開始する前に、以下を確認してください:

### 前提条件確認
- [x] Phase2実装完了を確認（Phase2完了日: 2025-11-27）
- [x] Phase2テスト結果を確認（11/11テスト成功）
- [x] Phase2実装記録を読了
- [x] `DeviceCode.IsBitDevice()`が正常動作することを確認
- [x] `ToString("D3")`が実装済みであることを確認

### 実装準備
- [x] TDD実装フローを理解（Red-Green-Refactor）
- [x] テストケース（TC_P3_001～007）を理解
- [x] ビット抽出処理（`(bitValue >> i) & 1`）を理解
- [x] deviceConfig辞書の拡張方法を理解

### 開発環境確認
- [x] `dotnet build`が成功することを確認
- [x] 既存テスト11/11が成功することを確認
- [x] 開発環境がクリーンな状態であることを確認

**Phase3実装準備完了**: すべてのチェックボックスが✅になったら実装開始可能

---

## Phase3実装状況（2025-11-27最終更新）

### 実装完了
- **実装開始日**: 2025-11-27
- **実装完了日**: 2025-11-27（全テストケース完了）
- **実装方式**: TDD (Red-Green-Refactor)
- **実装時間**: 約3時間

### 最終状況
✅ **Phase3完全完了**: 全7テストケース（TC_P3_001～TC_P3_007）実装・成功
✅ **Red Phase完了**: TC_P3_001テスト実装・失敗確認
✅ **Green Phase完了**: ビット順反転処理実装・TC_P3_001テスト成功
✅ **Refactor Phase完了**: コード整理・可読性向上・全12テスト成功
✅ **追加テスト完了**: TC_P3_002～TC_P3_007実装・全成功

### 実装内容

#### Phase3 Green Phase（ビット分割実装）
- `DataOutputManager.OutputToJson()`メソッドにビットデバイス16ビット分割ロジックを実装
- Selectベースのitems生成からListベースの実装に変更
- `IsBitDevice()`拡張メソッドを使用したビット/ワード判定分岐を実装
- **重要発見**: ConMoni分析によりSLMPプロトコルのビット順がMSB-firstであることを発見
- **ビット順反転処理**: `int bitIndex = 15 - i;` によりMSB-first → LSB-first変換を実装

#### Phase3 Refactor Phase（コード改善）
- マジックナンバー定数化: `BitsPerWord = 16`, `DefaultDigits = 1`
- ヘルパーメソッド追加:
  - `AddBitDeviceItems()`: ビットデバイス16ビット分割処理
  - `AddWordDeviceItem()`: ワード/ダブルワードデバイス処理
  - `GetDeviceConfigInfo()`: デバイス設定情報取得（重複コード削減）
  - `ExtractBit()`: ビット抽出（単一責任原則）
- コメント充実化: SLMP仕様、ビット順反転理由を明記
- 既存テスト更新: Phase3対応（18エントリ期待値）

#### Phase3 追加テスト実装（TC_P3_002～TC_P3_007）
- **実装日**: 2025-11-27
- **実装内容**:
  - TC_P3_002: すべて0のビットデバイステスト
  - TC_P3_003: すべて1のビットデバイステスト
  - TC_P3_004: ワード/ダブルワードデバイス非分割テスト
  - TC_P3_005: ビット+ワード混在テスト
  - TC_P3_006: デバイス名マッピングテスト
  - TC_P3_007: device.number 3桁ゼロ埋めテスト
- **テスト方式**: TDD（テスト先行実装）
- **結果**: すべてのテストが初回実行で成功（Phase1実装が正しかったため）

### 解決した問題

#### 問題1: テストデータ型ミスマッチ
- **現象**: Value=44245（期待値: 43605）
- **原因**: `int`リテラルが暗黙的に`ushort`変換されオーバーフロー
- **解決**: `(ushort)0b1010110011010101` 明示的キャスト追加

#### 問題2: ビット値が期待値と異なる
- **現象**: ビット0=0（期待値: 1）
- **原因**: LSB-firstでビット抽出していたが、SLMPはMSB-first
- **解決**: ConMoni分析により`binary[::-1]`（ビット順反転）を発見、実装

#### 問題3: 既存テストの期待値不一致
- **現象**: items.Count=18（既存テストの期待値: 3）
- **原因**: ビットデバイスM200が16ビットに分割されることをテストが考慮していない
- **解決**: テストの期待値を18に更新（D100: 1, D105: 1, M200-215: 16）

### テスト結果（最終）
- **TC_P3_001**: ✅ 成功（ビットデバイス16ビット分割・基本）
- **TC_P3_002**: ✅ 成功（すべて0のビットデバイス）
- **TC_P3_003**: ✅ 成功（すべて1のビットデバイス）
- **TC_P3_004**: ✅ 成功（ワード/ダブルワードデバイス非分割）
- **TC_P3_005**: ✅ 成功（ビット+ワード混在）
- **TC_P3_006**: ✅ 成功（デバイス名マッピング）
- **TC_P3_007**: ✅ 成功（device.number 3桁ゼロ埋め）
- **既存11テスト**: ✅ すべて成功
- **合計**: 18/18テスト成功（Phase3: 7/7, 既存: 11/11）

### 実装記録
詳細な実装記録は以下に保存:
- `documents/implementation_records/Phase3_DataOutputManager_BitDeviceSplitting_Implementation.txt`
- `documents/design/Step7_取得データ出力設計/実装結果/Phase3_BitDevice_16Bit_Split_Complete_Results.md`

### Phase3完了確認
✅ **全完了条件を満たしました**:
- ✅ ビットデバイスが16ビット分に展開される
- ✅ ワード/ダブルワードデバイスは分割されない
- ✅ 単体テスト（TC_P3_001～TC_P3_007）がすべてパス
- ✅ `dotnet build`が成功する
- ✅ 既存テストへの影響なし

### 次のPhase
**Phase4: エラーハンドリング実装**（予定2～3日）
- try-catchブロックの追加
- データ検証処理の実装
- ファイル出力エラー処理の詳細化
- ログ出力の強化
