# Phase 2: ビット展開機能 実装仕様書

作成日: 2025-01-17
優先度: 🟡 高優先
対象プロジェクト: andon (C#)
参照元: 受信データ解析_実装方針決定.md

---

## 1. Phase 2 概要

### 1.1 目標

ConMoni互換のビット展開機能を実装し、ワードデバイス値を16ビット配列に展開する機能を提供する。

### 1.2 実装範囲

- **2-1**: ビット展開ユーティリティクラス
- **2-2**: 変換係数対応（digitControl互換）
- **2-3**: ProcessedDeviceクラスへの統合

### 1.3 予想工数

**合計**: 6-8時間
- ビット展開ロジック: 4-5時間
- 変換係数対応: 2-3時間

---

## 2. ConMoniのビット展開機能分析

### 2.1 ConMoniの実装

ConMoniの`getPlcData()`メソッドで実装されているビット展開処理:

```python
# デバイス値抽出後
calcTempData = np.array(tmpData) * self.digitControl  # 変換係数適用

final_result = []
for r, flag in zip(calcTempData, self.settingData["accessBitDataLoc"]):
    if flag == 1:  # ビットデバイスの場合
        binary = format(r.astype(np.uint16), '016b')  # 16ビット文字列化
        binary = binary[::-1]  # 文字列反転 (LSB first化)
        binary_list = list(map(int, binary))
        final_result.extend(binary_list)
    else:  # ワードデバイスの場合
        final_result.append(r)
```

### 2.2 重要な仕様

#### 2.2.1 ビット順序: LSB first

ConMoniではビット順序を反転（`binary[::-1]`）してLSB firstにしている:

```
ワード値: 0x0003 (10進: 3)
↓
2進数: 0000 0000 0000 0011
↓ 反転（LSB first）
ビット配列: [1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
             ↑  ↑
           bit0 bit1
```

この順序はPLCのビットデバイス仕様と一致（例: M0が配列の先頭）。

#### 2.2.2 変換係数（digitControl）

ConMoniでは各デバイスに対して変換係数を適用:

```python
self.digitControl = [1.0, 0.1, 10.0, ...]  # 設定ファイルから読み込み
calcTempData = np.array(tmpData) * self.digitControl
```

**用途例**:
- 温度センサー: 0.1倍（1点 = 0.1℃）
- 圧力センサー: 10倍（1点 = 10Pa）

#### 2.2.3 選択的ビット展開

`accessBitDataLoc`フラグで各デバイスをビット展開するか判定:

```python
accessBitDataLoc = [0, 1, 0, 1, ...]  # 0=ワード値、1=ビット展開
```

---

## 3. 実装詳細

### 3.1 ビット展開ユーティリティクラス

#### 3.1.1 基本ビット展開メソッド

```csharp
/// <summary>
/// ビット展開ユーティリティ（ConMoni互換）
/// </summary>
public static class BitExpansionUtility
{
    /// <summary>
    /// ワード値を16ビット配列に展開（LSB first）
    /// ConMoniの binary[::-1] ロジックを再現
    /// </summary>
    /// <param name="wordValue">16ビットワード値</param>
    /// <returns>ビット配列（[0]=bit0, [15]=bit15）</returns>
    public static bool[] ExpandWordToBits(ushort wordValue)
    {
        var bits = new bool[16];
        for (int i = 0; i < 16; i++)
        {
            // ビットマスクで各ビットを抽出（LSB first）
            bits[i] = (wordValue & (1 << i)) != 0;
        }
        return bits;
    }

    /// <summary>
    /// ワード値を16ビット配列に展開（int版オーバーロード）
    /// </summary>
    public static bool[] ExpandWordToBits(int wordValue)
    {
        // 下位16ビットのみ使用
        return ExpandWordToBits((ushort)(wordValue & 0xFFFF));
    }

    /// <summary>
    /// 複数ワードを一括ビット展開
    /// </summary>
    /// <param name="wordValues">ワード値配列</param>
    /// <returns>ビット配列（各ワード16ビット × ワード数）</returns>
    public static bool[] ExpandMultipleWordsToBits(ushort[] wordValues)
    {
        var allBits = new List<bool>(wordValues.Length * 16);
        foreach (var word in wordValues)
        {
            allBits.AddRange(ExpandWordToBits(word));
        }
        return allBits.ToArray();
    }
}
```

#### 3.1.2 選択的ビット展開メソッド

ConMoniの`accessBitDataLoc`互換機能:

```csharp
/// <summary>
/// 選択的ビット展開（ConMoniの accessBitDataLoc 互換）
/// </summary>
/// <param name="wordValues">ワード値配列</param>
/// <param name="bitExpansionMask">ビット展開フラグ配列（true=展開、false=ワード値のまま）</param>
/// <param name="conversionFactors">変換係数配列（nullの場合は1.0）</param>
/// <returns>混合データリスト（boolまたはdouble）</returns>
public static List<object> ExpandWithSelectionMask(
    ushort[] wordValues,
    bool[] bitExpansionMask,
    double[]? conversionFactors = null)
{
    // 配列長チェック
    if (wordValues.Length != bitExpansionMask.Length)
    {
        throw new ArgumentException(
            $"Array length mismatch: wordValues={wordValues.Length}, bitExpansionMask={bitExpansionMask.Length}");
    }

    if (conversionFactors != null && conversionFactors.Length != wordValues.Length)
    {
        throw new ArgumentException(
            $"Array length mismatch: wordValues={wordValues.Length}, conversionFactors={conversionFactors.Length}");
    }

    var result = new List<object>();

    for (int i = 0; i < wordValues.Length; i++)
    {
        // 変換係数適用（ConMoniの digitControl 互換）
        double convertedValue = wordValues[i];
        if (conversionFactors != null && i < conversionFactors.Length)
        {
            convertedValue = wordValues[i] * conversionFactors[i];
        }

        if (bitExpansionMask[i])
        {
            // ビット展開モード
            var bits = ExpandWordToBits((ushort)convertedValue);
            foreach (var bit in bits)
            {
                result.Add(bit);
            }
        }
        else
        {
            // ワード値モード
            result.Add(convertedValue);
        }
    }

    return result;
}
```

#### 3.1.3 型安全版の選択的ビット展開

objectリストではなく、専用のクラスを返すバージョン:

```csharp
/// <summary>
/// 展開結果データ（型安全版）
/// </summary>
public class ExpandedDeviceValue
{
    /// <summary>値のタイプ</summary>
    public enum ValueType { Word, Bit }

    /// <summary>値のタイプ</summary>
    public ValueType Type { get; init; }

    /// <summary>ワード値（Type=Wordの場合）</summary>
    public double? WordValue { get; init; }

    /// <summary>ビット値（Type=Bitの場合）</summary>
    public bool? BitValue { get; init; }

    /// <summary>元のインデックス</summary>
    public int SourceIndex { get; init; }

    /// <summary>ビット位置（Type=Bitの場合、0-15）</summary>
    public int? BitPosition { get; init; }

    public override string ToString()
    {
        return Type switch
        {
            ValueType.Word => $"Word[{SourceIndex}]: {WordValue}",
            ValueType.Bit => $"Bit[{SourceIndex}][{BitPosition}]: {BitValue}",
            _ => "Unknown"
        };
    }
}

/// <summary>
/// 選択的ビット展開（型安全版）
/// </summary>
public static List<ExpandedDeviceValue> ExpandWithSelectionMaskTypeSafe(
    ushort[] wordValues,
    bool[] bitExpansionMask,
    double[]? conversionFactors = null)
{
    // 配列長チェック（省略: 上記と同じ）

    var result = new List<ExpandedDeviceValue>();

    for (int i = 0; i < wordValues.Length; i++)
    {
        double convertedValue = wordValues[i];
        if (conversionFactors != null && i < conversionFactors.Length)
        {
            convertedValue = wordValues[i] * conversionFactors[i];
        }

        if (bitExpansionMask[i])
        {
            // ビット展開モード
            var bits = ExpandWordToBits((ushort)convertedValue);
            for (int bitPos = 0; bitPos < bits.Length; bitPos++)
            {
                result.Add(new ExpandedDeviceValue
                {
                    Type = ExpandedDeviceValue.ValueType.Bit,
                    BitValue = bits[bitPos],
                    SourceIndex = i,
                    BitPosition = bitPos
                });
            }
        }
        else
        {
            // ワード値モード
            result.Add(new ExpandedDeviceValue
            {
                Type = ExpandedDeviceValue.ValueType.Word,
                WordValue = convertedValue,
                SourceIndex = i
            });
        }
    }

    return result;
}
```

---

### 3.2 変換係数対応

#### 3.2.1 設定ファイル拡張

appsettings.jsonに変換係数設定を追加:

```json
{
  "PlcCommunication": {
    "DataProcessing": {
      // ビット展開設定（ConMoni互換）
      "BitExpansion": {
        "Enabled": true,

        // デバイスごとの展開フラグ
        // true: ビット展開、false: ワード値のまま
        "SelectionMask": [false, true, false, true],

        // 変換係数（digitControl互換）
        // 各デバイス値に乗算される係数
        "ConversionFactors": [1.0, 0.1, 10.0, 1.0]
      }
    }
  }
}
```

#### 3.2.2 設定クラス

```csharp
/// <summary>
/// ビット展開設定（ConMoni互換）
/// </summary>
public class BitExpansionSettings
{
    /// <summary>ビット展開機能の有効/無効</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// デバイスごとのビット展開フラグ
    /// true: 16ビット展開、false: ワード値のまま
    /// </summary>
    public bool[] SelectionMask { get; set; } = Array.Empty<bool>();

    /// <summary>
    /// 変換係数配列（ConMoniの digitControl 互換）
    /// 各デバイス値に乗算される係数（デフォルト: 1.0）
    /// </summary>
    public double[] ConversionFactors { get; set; } = Array.Empty<double>();

    /// <summary>
    /// 設定の妥当性検証
    /// </summary>
    public void Validate()
    {
        if (!Enabled)
            return;

        if (SelectionMask.Length == 0)
        {
            throw new InvalidOperationException(
                "BitExpansion is enabled but SelectionMask is empty");
        }

        if (ConversionFactors.Length > 0 &&
            ConversionFactors.Length != SelectionMask.Length)
        {
            throw new InvalidOperationException(
                $"ConversionFactors length ({ConversionFactors.Length}) " +
                $"must match SelectionMask length ({SelectionMask.Length})");
        }
    }
}
```

---

### 3.3 ProcessedDeviceクラスへの統合

#### 3.3.1 ProcessedDeviceクラスの拡張

既存のProcessedDeviceクラスにビット展開情報を追加:

```csharp
/// <summary>
/// 処理済みデバイスデータ（ビット展開対応版）
/// </summary>
public class ProcessedDevice
{
    /// <summary>デバイス名（例: "D100", "M0"）</summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>ワード値（元の値）</summary>
    public ushort RawValue { get; set; }

    /// <summary>変換係数適用後の値</summary>
    public double ConvertedValue { get; set; }

    /// <summary>変換係数</summary>
    public double ConversionFactor { get; set; } = 1.0;

    /// <summary>ビット展開するかどうか</summary>
    public bool IsBitExpanded { get; set; } = false;

    /// <summary>
    /// 展開されたビット配列（IsBitExpanded=trueの場合）
    /// [0]=bit0, [15]=bit15（LSB first）
    /// </summary>
    public bool[]? ExpandedBits { get; set; }

    /// <summary>データ型</summary>
    public string DataType { get; set; } = "Word";

    /// <summary>読み取り時刻</summary>
    public DateTime ReadAt { get; set; }

    /// <summary>
    /// ビット値を名前付きで取得
    /// </summary>
    /// <param name="bitPosition">ビット位置（0-15）</param>
    /// <returns>ビット値とビット名</returns>
    public (bool Value, string BitName) GetBit(int bitPosition)
    {
        if (!IsBitExpanded || ExpandedBits == null)
        {
            throw new InvalidOperationException("Device is not bit-expanded");
        }

        if (bitPosition < 0 || bitPosition >= 16)
        {
            throw new ArgumentOutOfRangeException(nameof(bitPosition), "Bit position must be 0-15");
        }

        string bitName = $"{DeviceName}.{bitPosition}";
        return (ExpandedBits[bitPosition], bitName);
    }

    public override string ToString()
    {
        if (IsBitExpanded && ExpandedBits != null)
        {
            string bitsStr = string.Join("", ExpandedBits.Select(b => b ? "1" : "0"));
            return $"{DeviceName}: Raw={RawValue:X4}, Bits=[{bitsStr}]";
        }
        else
        {
            return $"{DeviceName}: Value={ConvertedValue} (Raw={RawValue}, Factor={ConversionFactor})";
        }
    }
}
```

#### 3.3.2 ビット展開処理の統合

ProcessReceivedRawData()の後処理として追加:

```csharp
/// <summary>
/// デバイス値にビット展開を適用
/// </summary>
private List<ProcessedDevice> ApplyBitExpansion(
    List<ProcessedDevice> devices,
    BitExpansionSettings settings)
{
    // ビット展開が無効な場合はそのまま返却
    if (!settings.Enabled)
    {
        _logger.LogDebug("Bit expansion is disabled");
        return devices;
    }

    // 設定検証
    settings.Validate();

    // デバイス数と設定の長さチェック
    if (devices.Count != settings.SelectionMask.Length)
    {
        _logger.LogWarning(
            $"Device count ({devices.Count}) does not match SelectionMask length ({settings.SelectionMask.Length}). " +
            $"Bit expansion will be skipped.");
        return devices;
    }

    _logger.LogDebug($"Applying bit expansion to {devices.Count} devices");

    for (int i = 0; i < devices.Count; i++)
    {
        var device = devices[i];

        // 変換係数適用
        if (settings.ConversionFactors.Length > 0)
        {
            device.ConversionFactor = settings.ConversionFactors[i];
            device.ConvertedValue = device.RawValue * device.ConversionFactor;
        }
        else
        {
            device.ConvertedValue = device.RawValue;
        }

        // ビット展開フラグ確認
        if (settings.SelectionMask[i])
        {
            device.IsBitExpanded = true;
            device.ExpandedBits = BitExpansionUtility.ExpandWordToBits(device.RawValue);
            device.DataType = "Bits";

            _logger.LogDebug(
                $"Device {device.DeviceName}: Expanded to bits (Raw=0x{device.RawValue:X4})");
        }
        else
        {
            device.IsBitExpanded = false;
            device.ExpandedBits = null;

            _logger.LogDebug(
                $"Device {device.DeviceName}: Kept as word (Value={device.ConvertedValue}, Factor={device.ConversionFactor})");
        }
    }

    return devices;
}
```

#### 3.3.3 ProcessReceivedRawData()への統合

既存のメソッドの最後に追加:

```csharp
// Step-7 ビット展開適用（Phase 2追加機能）
if (_bitExpansionSettings.Enabled)
{
    result.ProcessedDevices = ApplyBitExpansion(
        result.ProcessedDevices,
        _bitExpansionSettings);
}
```

---

## 4. テスト計画

### 4.1 単体テスト

#### 4.1.1 ExpandWordToBits() テスト

**テストケース**:

| No | 入力値 | 期待ビット配列（LSB first） | 説明 |
|----|-------|---------------------------|------|
| 1 | 0x0000 | [0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0] | 全ビット0 |
| 2 | 0xFFFF | [1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1] | 全ビット1 |
| 3 | 0x0001 | [1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0] | bit0のみ1 |
| 4 | 0x8000 | [0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1] | bit15のみ1 |
| 5 | 0x0003 | [1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0] | bit0,1が1 |
| 6 | 0x00AA | [0,1,0,1,0,1,0,1,0,0,0,0,0,0,0,0] | 0xAA = 10101010 |

#### 4.1.2 ExpandWithSelectionMask() テスト

**テストケース**:

```csharp
// 入力
ushort[] words = [0x0003, 0x00FF, 0x0001, 0x0002];
bool[] mask = [false, true, false, true];
double[] factors = [1.0, 1.0, 0.1, 10.0];

// 期待出力
// words[0]=3, mask=false, factor=1.0 → 3.0（ワード値）
// words[1]=0xFF, mask=true, factor=1.0 → 16ビット展開
// words[2]=1, mask=false, factor=0.1 → 0.1（ワード値）
// words[3]=2, mask=true, factor=10.0 → 20を16ビット展開
```

#### 4.1.3 ApplyBitExpansion() テスト

**テストシナリオ**:
1. ビット展開無効時: そのまま返却
2. SelectionMask長不一致: 警告ログ + スキップ
3. 正常系: 各デバイスに正しく展開適用

### 4.2 統合テスト

#### 4.2.1 ConMoni互換性テスト

**目的**: ConMoniと同じ入力で同じ出力を得る

**準備**:
1. ConMoniの設定ファイルから`digitControl`と`accessBitDataLoc`を取得
2. 同じ設定をandonに適用
3. 同じPLCデータで両方を実行

**検証**:
- ビット展開されたデバイスの順序が一致
- 各ビット値が一致（LSB first順序）
- 変換係数適用後の値が一致

#### 4.2.2 実機データ再生テスト

**テストデータ**:
```
デバイス値: [0x0003, 0x00FF, 0x0001]
SelectionMask: [false, true, false]
ConversionFactors: [1.0, 1.0, 0.1]
```

**期待結果**:
```
ProcessedDevices[0]: Value=3.0, IsBitExpanded=false
ProcessedDevices[1]: IsBitExpanded=true, ExpandedBits=[1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0]
ProcessedDevices[2]: Value=0.1, IsBitExpanded=false
```

---

## 5. 実装手順

### 5.1 推奨実装順序

1. **BitExpansionUtilityクラス作成**（1時間）
   - ExpandWordToBits()実装
   - ExpandMultipleWordsToBits()実装

2. **単体テスト（基本ビット展開）**（1時間）
   - 各種ワード値でテスト
   - LSB first順序の確認

3. **選択的ビット展開メソッド実装**（1.5時間）
   - ExpandWithSelectionMask()実装
   - 変換係数適用ロジック

4. **設定クラス実装**（30分）
   - BitExpansionSettings作成
   - appsettings.json更新

5. **ProcessedDeviceクラス拡張**（1時間）
   - ビット展開フィールド追加
   - GetBit()メソッド追加

6. **ApplyBitExpansion()実装**（1時間）
   - ProcessReceivedRawData()に統合
   - ログ出力追加

7. **単体テスト（統合版）**（1-2時間）
   - 全機能の動作確認
   - エッジケースのテスト

**合計**: 6-8時間

### 5.2 実装時の注意点

#### 5.2.1 LSB first順序の重要性

PLCのビットデバイス仕様に合わせ、必ずLSB firstで展開:

```
M0 → ExpandedBits[0]
M1 → ExpandedBits[1]
...
M15 → ExpandedBits[15]
```

#### 5.2.2 変換係数の適用タイミング

**正しい順序**:
1. ワード値取得
2. 変換係数適用
3. ビット展開（変換後の値を展開）

**誤った順序**:
1. ワード値取得
2. ビット展開
3. 変換係数適用 ← ビット値に係数は適用不可

#### 5.2.3 設定配列長の検証

SelectionMaskとConversionFactorsの長さが一致しない場合は例外:

```csharp
if (conversionFactors != null &&
    conversionFactors.Length != wordValues.Length)
{
    throw new ArgumentException("Array length mismatch");
}
```

---

## 6. Phase 2 完了基準

### 6.1 機能要件

- ✅ ワード値を16ビット配列に展開（LSB first）
- ✅ 選択的ビット展開（マスク指定）
- ✅ 変換係数適用（ConMoni互換）
- ✅ ProcessedDeviceクラスへの統合
- ✅ 設定ファイルでの制御

### 6.2 品質要件

- ✅ 全単体テストがパス
- ✅ ConMoni互換性テストで同等の結果
- ✅ LSB first順序の正確性確認
- ✅ エラーケースで適切な例外・警告

### 6.3 ドキュメント要件

- ✅ コード内コメント（ビット順序の説明）
- ✅ テスト結果レポート
- ✅ ConMoniとの互換性確認レポート
- ✅ 実装記録の作成

---

## 7. Phase 2 後の次ステップ

Phase 2完了後は以下に進む:

1. **Phase 3: 検証機能強化** → デバイス点数検証、エラーコードマッピング
2. **実機テスト** → ビット展開機能の実機確認
3. **パフォーマンス測定** → ビット展開による処理時間への影響確認

---

## 8. 参考: ConMoniとの対応表

| ConMoni機能 | andon実装 | 備考 |
|------------|----------|------|
| `binary = format(r, '016b')` | `ExpandWordToBits(ushort)` | 16ビット文字列化 |
| `binary[::-1]` | ビット演算でLSB first | 文字列反転の代わりにビットシフト |
| `self.digitControl` | `ConversionFactors` | 設定ファイルから読み込み |
| `accessBitDataLoc` | `SelectionMask` | bool配列で管理 |
| `final_result.extend()` | `ProcessedDevice.ExpandedBits` | 16要素のbool配列 |
| `final_result.append(r)` | `ProcessedDevice.ConvertedValue` | double値で管理 |

---

**文書作成者**: Claude Code
**参照元**: 受信データ解析_実装方針決定.md, ConMoni/modules/process/GetPlcData.py
