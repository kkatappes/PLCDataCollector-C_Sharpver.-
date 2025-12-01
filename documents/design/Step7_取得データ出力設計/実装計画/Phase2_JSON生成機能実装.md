# Phase2: JSON生成機能実装計画

## 概要
Phase2では、既存の`OutputToJson()`メソッドを改善し、設計書に準拠したJSON出力機能を実装します。**ビットデバイス分割処理は含まれません**（Phase3で実装）。

---

## Step1からの引継ぎ事項（2025-11-27更新）

### Step1 Phase5完了により利用可能になった機能

✅ **MultiPlcConfigManager（複数PLC設定管理）**
- **実装ファイル**: `andon/Core/Managers/MultiPlcConfigManager.cs`
- **機能**: 複数のExcelファイルから読み込んだPLC設定を一元管理
- **主要メソッド**:
  - `GetConfiguration(string configName)`: 設定名で取得
  - `GetAllConfigurations()`: 全設定を取得
  - `GetStatistics()`: 統計情報取得
- **Step7での活用方法**:
  - ExecutionOrchestratorから設定名でPLC設定を取得
  - 各PLC設定のSavePathやPlcModel情報を出力に利用可能

✅ **PlcConfiguration（検証済み設定モデル）**
- **実装ファイル**: `andon/Core/Models/ConfigModels/PlcConfiguration.cs`
- **利用可能プロパティ**:
  - `ConfigurationName`: PLC識別名（Excelファイル名由来）
  - `IpAddress`: 接続先IPアドレス
  - `Port`: 接続先ポート番号
  - `PlcModel`: PLC機種名（例: "Q03UDE"）
  - `SavePath`: データ保存先パス
  - `Devices`: デバイス設定リスト（DeviceSpecification）
- **検証状態**: Phase4のValidateConfiguration()で全検証済み
- **Step7での活用**:
  - plcModel固定値"Unknown"の代わりに`config.PlcModel`を使用可能
  - `config.SavePath`を出力ディレクトリとして使用可能

✅ **DeviceSpecification（デバイス指定情報）**
- **実装ファイル**: `andon/Core/Models/DeviceSpecification.cs`
- **利用可能プロパティ**:
  - `ItemName`: デバイス項目名（Excel A列）
  - `DeviceType`: デバイスタイプ（"D", "M", "X"等）
  - `DeviceNumber`: デバイス番号
  - `Digits`: 桁数
  - `Unit`: 単位（"bit", "word", "dword"）
- **Step7での活用**:
  - ItemNameを出力JSON items[].nameに使用
  - Digitsを出力JSON items[].digitsに使用
  - Unitを出力JSON items[].unitに使用

### Phase2実装での活用例

**plcModelの動的取得（L52修正可能）**:
```csharp
// 現在（固定値）
var plcModel = "Unknown";

// Step1完了後（設定から取得可能）
var plcModel = config.PlcModel;  // ExecutionOrchestratorから渡されたconfig
```

**DeviceEntryInfo辞書の構築（Phase3実装時）**:
```csharp
// Step1完了後、PlcConfiguration.Devicesから構築可能
var deviceConfig = config.Devices.ToDictionary(
    d => $"{d.DeviceType}{d.DeviceNumber}",  // キー: "M0", "D100"
    d => new DeviceEntryInfo
    {
        Name = d.ItemName,  // Excel A列の項目名
        Digits = d.Digits   // Excel D列の桁数
    }
);
```

**注意事項**:
- Phase2では引き続き固定値"Unknown"を使用可能（段階的移行）
- Phase3以降でPlcConfigurationとの統合を検討
- ExecutionOrchestratorがMultiPlcConfigManagerから設定を取得し、DataOutputManagerに渡す設計

---

## 実装対象

### 1. device.numberの3桁ゼロ埋めフォーマット

#### 現在の実装（問題あり）
```csharp
// DataOutputManager.cs 行454付近
device = new
{
    code = kvp.Value.Code.ToString(),
    number = kvp.Value.Address.ToString()  // ← 問題: "0", "10", "100"
}
```

#### 修正後
```csharp
device = new
{
    code = kvp.Value.Code.ToString(),
    number = kvp.Value.Address.ToString("D3")  // ← 修正: "000", "010", "100"
}
```

#### 期待される出力
```json
{
  "device": {
    "code": "M",
    "number": "000"  // ← 3桁ゼロ埋め
  }
}
```

---

### 2. plcModel値の動的取得（オプション）

#### 現在の実装
```csharp
// DataOutputManager.cs 行40付近
var plcModel = "Unknown";  // ← 固定値
```

#### 修正方針
**Phase2では固定値のままでOK**（将来拡張として設定から取得可能にする）

**将来実装の方針**:
1. `ConnectionConfig`に`PlcModel`プロパティを追加
2. `appsettings.json`に`PlcModel`設定を追加
3. `OutputToJson()`のパラメータに`string plcModel`を追加

#### 設定例（将来実装）
```json
{
  "Connection": {
    "IpAddress": "192.168.1.10",
    "Port": 5007,
    "PlcModel": "Q00UJCPU"  // ← 追加
  }
}
```

---

### 3. ディレクトリ存在確認・作成処理

#### 現在の実装状況
ディレクトリが存在しない場合のエラーハンドリングなし

#### 追加実装
```csharp
// DataOutputManager.OutputToJson() メソッド内
// ファイル書き込み前に追加
if (!Directory.Exists(outputDirectory))
{
    Directory.CreateDirectory(outputDirectory);
}
```

#### 実装位置
`OutputToJson()`メソッドの**ファイル名生成後、JSON生成前**

---

### 4. ログ出力の追加

#### 追加するログ
1. **出力開始ログ**（情報レベル）
2. **出力完了ログ**（情報レベル）
3. **エラーログ**（エラーレベル、Phase4で実装）

#### 実装内容

```csharp
// OutputToJson() メソッドの先頭に追加
Console.WriteLine($"[INFO] JSON出力開始: IP={ipAddress}, Port={port}");

// ファイル書き込み後に追加
Console.WriteLine($"[INFO] JSON出力完了: ファイル={fileName}, デバイス数={data.ProcessedData.Count}");
```

**注意**: Phase2では簡易的なConsole.WriteLineを使用。将来的にはILoggingManagerを使用。

---

### 5. JSON構造の整理（リファクタリング）

#### 現在の実装
items配列の生成が複雑（行448-459）

#### 改善方針
**Phase2では大きな変更なし**。Phase3でビットデバイス分割処理実装時に大幅に改修します。

---

## 実装手順

### Step 1: device.numberの3桁ゼロ埋め修正

1. `andon/Core/Managers/DataOutputManager.cs`を開く
2. 行454付近の`number`プロパティを修正
   ```csharp
   number = kvp.Value.Address.ToString("D3")
   ```

### Step 2: ディレクトリ存在確認・作成処理の追加

1. `OutputToJson()`メソッド内、ファイル名生成後に追加
   ```csharp
   var filePath = Path.Combine(outputDirectory, fileName);

   // ディレクトリ存在確認・作成
   if (!Directory.Exists(outputDirectory))
   {
       Directory.CreateDirectory(outputDirectory);
   }
   ```

### Step 3: ログ出力の追加

1. `OutputToJson()`メソッドの先頭に追加
   ```csharp
   Console.WriteLine($"[INFO] JSON出力開始: IP={ipAddress}, Port={port}");
   ```

2. ファイル書き込み後に追加
   ```csharp
   File.WriteAllText(filePath, jsonString);
   Console.WriteLine($"[INFO] JSON出力完了: ファイル={fileName}, デバイス数={data.ProcessedData.Count}");
   ```

### Step 4: ビルド・動作確認

1. `dotnet build`を実行
2. コンパイルエラーがないことを確認

### Step 5: 手動テスト

1. テストデータを用意
2. `OutputToJson()`を実行
3. 出力されたJSONファイルを確認
   - device.numberが3桁ゼロ埋めされているか
   - outputディレクトリが自動作成されるか
   - ログが正常に出力されるか

---

## テスト要件（TDD必須）

### Phase2の単体テスト（必須実装）

**重要**: TDD手法に従い、Phase2では**テストを先に実装**してから機能実装を行います。

#### TDD実装サイクル（具体的手順）

各テストケースごとに以下のサイクルを1つずつ実施します:

1. **Red（テスト実装）**:
   - 1つのテストメソッドを実装
   - `dotnet test`を実行し、テストが失敗することを確認
   - 失敗理由が「機能未実装」であることを確認

2. **Green（機能実装）**:
   - テストをパスさせるための最小限の機能を実装
   - `dotnet test`を実行し、テストがパスすることを確認
   - 他の既存テストもパスしていることを確認

3. **Refactor（リファクタリング）**:
   - コードを整理・改善（必要に応じて）
   - `dotnet test`を再実行し、すべてのテストがパスすることを確認

4. **次のテストケースへ**: 上記1-3を繰り返す

**注意**: 全テストメソッドを一度に実装してから機能実装するのではなく、**1つずつRed-Green-Refactorサイクルを回す**ことが重要です。

#### テスト用モックデータ

Phase2で使用するモックデータの作成例:

```csharp
// device.numberテスト用モックデータ
private ProcessedResponseData CreateMockData_Address0()
{
    return new ProcessedResponseData
    {
        ProcessedData = new Dictionary<string, DeviceData>
        {
            ["M0"] = new DeviceData
            {
                DeviceName = "M0",
                Code = DeviceCode.M,
                Address = 0,  // ← TC_P2_001用
                Value = 1,
                Type = "Bit"
            }
        },
        ProcessedAt = DateTime.Now,
        IsSuccess = true
    };
}

// ディレクトリ作成テスト用（存在しないパス）
private string GetNonExistentTestDirectory()
{
    return Path.Combine(Path.GetTempPath(), $"andon_test_{Guid.NewGuid()}");
}
```

#### テストケース（必須）
1. **TC_P2_001**: device.numberが3桁ゼロ埋めされること
   - 入力: Address=0
   - 期待値: number="000"

2. **TC_P2_002**: device.numberが3桁ゼロ埋めされること（2桁）
   - 入力: Address=10
   - 期待値: number="010"

3. **TC_P2_003**: device.numberが3桁ゼロ埋めされること（3桁）
   - 入力: Address=100
   - 期待値: number="100"

4. **TC_P2_004**: 存在しないディレクトリが自動作成されること
   - 入力: outputDirectory="./test_output_new"
   - 期待値: ディレクトリが作成され、ファイルが出力される

5. **TC_P2_005**: ログ出力が正常に行われること
   - 入力: 任意のデータ
   - 期待値: "[INFO] JSON出力開始" と "[INFO] JSON出力完了" が出力される

---

## 完了条件

### 必須項目（TDD厳守）
- [x] **単体テスト（TC_P2_001～TC_P2_005）が実装されている** ✅ 完了 (2025-11-27)
- [x] **すべてのテストがパスする** ✅ 完了 (5/5テスト成功)
- [x] device.numberが3桁ゼロ埋めフォーマットで出力される ✅ 完了 (ToString("D3")実装)
- [x] 存在しないディレクトリが自動作成される ✅ 完了 (Directory.CreateDirectory()実装)
- [x] JSON出力開始・完了のログが出力される ✅ 完了 (Console.WriteLine()実装)
- [x] `dotnet build`が成功する ✅ 完了

### 完了条件の確認順序
1. ✅ テスト実装（Red）- 4/5テストが失敗することを確認
2. ✅ 機能実装（Green）- 全5テストが成功
3. ✅ テスト実行・パス確認 - 全11テスト成功（既存テスト影響なし）
4. ✅ リファクタリング（必要に応じて）- 不要と判断
5. ✅ 最終テスト実行・パス確認 - 完了

**Phase2完了日**: 2025-11-27
**実装方式**: TDD (Red-Green-Refactor)

---

## 実装後の確認事項

### 1. JSON出力確認
```json
{
  "source": {
    "plcModel": "Unknown",
    "ipAddress": "192.168.1.10",
    "port": 5007
  },
  "timestamp": {
    "local": "2025-11-27T12:34:56.789+09:00"
  },
  "items": [
    {
      "name": "停止纏めON=異常無",
      "device": {
        "code": "M",
        "number": "000"  // ← 3桁ゼロ埋めされていることを確認
      },
      "digits": 1,
      "unit": "bit",
      "value": 43605
    }
  ]
}
```

### 2. ディレクトリ作成確認
- `./output` ディレクトリが存在しない状態で実行
- ディレクトリが自動作成されることを確認

### 3. ログ出力確認
```
[INFO] JSON出力開始: IP=192.168.1.10, Port=5007
[INFO] JSON出力完了: ファイル=20251127_123456789_192-168-1-10_5007.json, デバイス数=1
```

---

## 既存実装との差異

### Phase2で解決される問題
- ✅ device.numberの3桁ゼロ埋め不足
- ✅ ディレクトリ存在確認・作成処理の欠如
- ✅ ログ出力の不足

### Phase2で未解決の問題（Phase3以降で対応）
- ❌ ビットデバイス16ビット分割処理の未実装
- ❌ エラーハンドリングの不足
- ❌ plcModel値が固定値

---

## 次のPhaseへの準備

Phase3では、**ビットデバイス16ビット分割処理**を実装します。
これは最も重要な機能であり、Phase2完了後すぐに着手してください。

Phase2完了後、`Phase3_ビットデバイス分割処理実装.md`を参照してください。

---

## 参照文書
- `実装ガイド.md`: 全体的な実装仕様（セクション2, 4, 6）
- `実装時対応関係.md`: device.number問題、ログ出力設計

## 作成日時
- **作成日**: 2025年11月27日
- **対象Phase**: Phase2（JSON生成機能実装）

---

## 実装完了サマリー（2025-11-27追記）

### 実装結果
✅ **Phase2実装完了**: 2025-11-27
✅ **実装方式**: TDD (Red-Green-Refactor)
✅ **テスト結果**: 11/11テスト成功（既存テスト影響なし）

### 実装内容
1. **device.number 3桁ゼロ埋め** (andon/Core/Managers/DataOutputManager.cs:60)
   - `kvp.Value.Address.ToString("D3")` 実装
   - "0" → "000", "10" → "010", "100" → "100"

2. **ディレクトリ自動作成** (andon/Core/Managers/DataOutputManager.cs:47-50)
   - `Directory.CreateDirectory(outputDirectory)` 実装
   - 存在しないディレクトリを自動作成

3. **ログ出力機能** (andon/Core/Managers/DataOutputManager.cs:29, 77)
   - `Console.WriteLine("[INFO] JSON出力開始...")` 実装
   - `Console.WriteLine("[INFO] JSON出力完了...")` 実装

### テスト結果詳細
| テストカテゴリ | テスト数 | 成功 | 失敗 |
|---------------|----------|------|------|
| Phase2新規テスト（TC_P2_001～005） | 5 | 5 | 0 |
| 既存テスト | 6 | 6 | 0 |
| **合計** | **11** | **11** | **0** |

### 実装記録
- **実装記録**: `documents/implementation_records/Phase2_DataOutputManager_JSON機能改善実装記録.txt`
- **テスト結果**: `documents/design/Step7_取得データ出力設計/実装結果/Phase2_JSON生成機能改善_TestResults.txt`

### Phase3への引き継ぎ
次Phase: **Phase3（ビットデバイス16ビット分割処理実装）**
- 難易度: ★★★★☆（最重要・最難関）
- 推奨期間: 3～5日
- 参照文書: `Phase3_ビットデバイス分割処理実装.md`
