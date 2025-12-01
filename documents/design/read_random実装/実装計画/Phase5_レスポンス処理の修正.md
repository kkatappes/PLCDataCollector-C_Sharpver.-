# Phase5: レスポンス処理の修正

## ステータス
✅ **実装完了** - Phase7対応でDeviceData.Typeプロパティ実装完了 (2025-11-25)
✅ **設計完了** - Random READ全デバイス一括取得対応 (2025-11-20)
✅ **設計更新** - Phase6設定ファイル構造変更対応 (2025-11-21)
✅ **テスト完了** - DeviceData.Type全テスト合格（5/5） (2025-11-25)

## 概要
ReadRandom(0x0403)のレスポンスフレームをパースし、デバイス番号とデータ値のマッピングを作成する機能を実装します。

**注意**: READコマンド(0x0401)は廃止されました。本システムはRandom READコマンド(0x0403)のみをサポートします。

**2025-11-20仕様変更**:
- Random READで全デバイス（ビット/ワード/ダブルワード）を一括取得
- 応答統合処理（MergeResponseData）が不要に
- ProcessReceivedRawDataで処理完結

**2025-11-21設計更新（Phase6対応）**:
- DeviceDataクラスの導入
- デバイス名キー構造（"M000", "D000", "D002"）
- TargetDeviceConfig.DevicesがList<DeviceSpecification>型（Phase6で確定）
- Dictionary<string, DeviceData>を返却（Dictionary<DeviceSpecification, ushort>から変更）

## 前提条件
- ✅ Phase1完了: DeviceCode、DeviceSpecification実装済み
- ✅ Phase2完了: SlmpFrameBuilder.BuildReadRandomRequest()実装済み（ビット/ワード/ダブルワード混在対応）
- ✅ Phase4ステップ11完了: ReadRandom送受信テスト実装済み
- ✅ Phase4ステップ12-13完了: PlcCommunicationManagerへの統合（2025-11-18）

## ReadRandomレスポンスフレーム構造

### 4Eフレームレスポンス（memo.md実データ）

```
総バイト数: 111バイト

[ヘッダ部] 13バイト
  バイト0-1:   サブヘッダ（0xD4 0x00）
  バイト2-3:   シーケンス番号（0x00 0x00）
  バイト4-5:   予約（0x00 0x00）
  バイト6:     ネットワーク番号（0x00）
  バイト7:     PC番号（0xFF）
  バイト8-9:   I/O番号（0xFF 0x03、リトルエンディアン）
  バイト10:    マルチドロップ局番（0x00）
  バイト11-12: データ長（0x63 0x00 = 99バイト、リトルエンディアン）

[エンドコード部] 2バイト
  バイト13-14: エンドコード（0x00 0x00 = 正常終了）

[デバイスデータ部] 96バイト（48ワード × 2バイト/ワード）
  バイト15-110: M0-M47のワードデータ（96バイト）
                各ワード = 2バイト（リトルエンディアン）
                例: M0のワード = [0x00, 0x01] = 0x0100 = 256
```

**パース時の注意**（フレーム構築方法.md準拠）:
- ヘッダー部分: 6～14バイト目（9バイト = ネットワーク番号(1) + PC番号(1) + I/O番号(2) + 局番(1) + データ長(2) + 終了コード(2)）
- データ部開始位置: 15バイト目（サブヘッダ2バイト + シーケンス2バイト + 予約2バイト + ヘッダー9バイト）
- 実データ長 = データ長フィールド値 - 2（終了コード分を除く）

### 3Eフレームレスポンス

```
総バイト数: 11 + デバイスデータバイト数

[ヘッダ部] 9バイト
  バイト0-1:   サブヘッダ（0xD0 0x00）
  バイト2:     ネットワーク番号（0x00）
  バイト3:     局番（0xFF）
  バイト4-5:   I/O番号（0xFF 0x03、リトルエンディアン）
  バイト6:     マルチドロップ局番（0x00）
  バイト7-8:   データ長（リトルエンディアン）

[エンドコード部] 2バイト
  バイト9-10:  エンドコード（0x00 0x00 = 正常終了）

[デバイスデータ部] 可変長
  バイト11-:   デバイスデータ（ワード単位、リトルエンディアン）
```

**パース時の注意**（フレーム構築方法.md準拠）:
- データ部開始位置: 11バイト目（サブヘッダ2バイト + ヘッダー9バイト）
- 実データ長 = データ長フィールド値 - 2（終了コード2バイト分を除く）

---

## 実装ステップ

### 【新規】ステップ14-A: DeviceDataクラスの定義 ✅ **完了** (2025-11-25)

#### 実装対象
`andon/Core/Models/DeviceData.cs`（新規作成）

#### 実装内容
**DeviceDataクラス（Phase4仕様変更で導入）**:

```csharp
namespace Andon.Core.Models;

/// <summary>
/// デバイスデータを表現するクラス
/// Phase4仕様変更(2025-11-20)で導入
/// </summary>
public class DeviceData
{
    /// <summary>
    /// デバイス名（"M000", "D000", "D002"等）
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// デバイスコード（M, D, W等）
    /// </summary>
    public DeviceCode Code { get; set; }

    /// <summary>
    /// デバイス番号（アドレス）
    /// </summary>
    public int Address { get; set; }

    /// <summary>
    /// デバイス値（16bit: ワードデバイス、32bit: ダブルワードデバイス）
    /// </summary>
    public uint Value { get; set; }

    /// <summary>
    /// ダブルワードデバイスかどうか
    /// </summary>
    public bool IsDWord { get; set; }

    /// <summary>
    /// 16進アドレス表記かどうか
    /// </summary>
    public bool IsHexAddress { get; set; }

    /// <summary>
    /// デバイス型（"Bit", "Word", "DWord"）
    /// Phase7データ出力で使用（unit値："bit", "word", "dword"への変換に利用）
    /// </summary>
    public string Type { get; set; } = "Word";  // デフォルトはWord

    /// <summary>
    /// DeviceSpecificationから生成
    /// </summary>
    public static DeviceData FromDeviceSpecification(DeviceSpecification device, ushort value)
    {
        return new DeviceData
        {
            DeviceName = device.ToString(),
            Code = device.Code,
            Address = device.DeviceNumber,
            Value = value,
            IsDWord = false,
            IsHexAddress = device.IsHexAddress,
            Type = device.Code.IsBitDevice() ? "Bit" : "Word"
        };
    }

    /// <summary>
    /// ダブルワードデバイスの生成（2ワード分結合）
    /// </summary>
    public static DeviceData FromDWordDevice(DeviceSpecification device, ushort lowerWord, ushort upperWord)
    {
        uint dwordValue = ((uint)upperWord << 16) | lowerWord;
        return new DeviceData
        {
            DeviceName = device.ToString(),
            Code = device.Code,
            Address = device.DeviceNumber,
            Value = dwordValue,
            IsDWord = true,
            IsHexAddress = device.IsHexAddress,
            Type = "DWord"
        };
    }
}

**Phase7対応（2025-11-25実装完了）**:
- ✅ `Type`プロパティを追加（値: "Bit", "Word", "DWord"）
- ✅ FromDeviceSpecification()で自動判定: `device.Code.IsBitDevice() ? "Bit" : "Word"`
- ✅ FromDWordDevice()でType="DWord"設定
- ✅ DataOutputManagerのJSON出力で`unit`フィールド生成時に使用
- ✅ 全テスト合格（5/5テスト）: DeviceDataTests
```

**キー構造の例**:
```
ビットデバイス:   "M000", "M016", "M032"
ワードデバイス:   "D000", "W0x11AA"
DWordデバイス:    "D000" (2ワード分のデータ、D000+D001)
```

#### 変化点
- **Phase5初期設計**: Dictionary<DeviceSpecification, ushort>
- **Phase4仕様変更後**: Dictionary<string, DeviceData>
  - デバイス名キーで管理
  - ビット・ワード・ダブルワード混在対応
  - Phase6で確定したList<DeviceSpecification>型と連携

---

### ステップ14: ReadRandomレスポンスパーサーの追加 ✅ **完了** (2025-11-21)

#### 実装対象
`andon/Utilities/SlmpDataParser.cs`

#### 実装内容
**2025-11-21設計更新（Phase6対応）**:
1. **ParseReadRandomResponse()メソッド**
   - レスポンスフレームバイト配列を受け取る
   - デバイス指定リスト（List<DeviceSpecification>）を受け取る
   - フレームタイプ（3E/4E）を自動判定
   - エンドコードを確認（異常時は例外スロー）
   - デバイスデータ部を順次抽出
   - DeviceDataオブジェクトを生成
   - **Dictionary<string, DeviceData>を返却**（デバイス名キー構造）

2. **ValidateResponseFrame()メソッド**
   - フレーム長検証
   - サブヘッダ検証
   - エンドコード検証
   - 異常時は例外スロー

3. **ExtractDeviceData()メソッド**
   - デバイスデータ部からワード値を抽出
   - リトルエンディアン変換
   - ushort配列を返却

4. **DWordデバイス検出と結合**（Phase4仕様変更対応）
   - 設定ファイルまたはOriginalRequestからDWordデバイスを識別
   - 連続する2ワードを32bit値に結合
   - DeviceData.FromDWordDevice()で生成

#### 実装コード（更新予定）

**注意**: 以下のコードは Phase5初期設計に基づいています。実装時にはDeviceDataクラスを使用した設計に更新されます。

<details>
<summary>Phase5初期設計のコード例（参考）</summary>

```csharp
using Andon.Core.Constants;
using Andon.Core.Models;

namespace Andon.Utilities;

/// <summary>
/// SLMPレスポンスデータパーサー（Phase5初期設計版）
/// 実装時にはDeviceDataクラス対応に更新されます
/// </summary>
public static class SlmpDataParser_Phase5InitialDesign
{
    /// <summary>
    /// ReadRandom(0x0403)レスポンスをパース（旧設計）
    /// 実装時にはDictionary<string, DeviceData>を返却するように変更
    /// </summary>
    /// <param name="responseFrame">レスポンスフレームバイト配列</param>
    /// <param name="devices">送信時に使用したデバイス指定リスト</param>
    /// <returns>デバイス番号とデータ値のマッピング（旧設計）</returns>
    public static Dictionary<DeviceSpecification, ushort> ParseReadRandomResponse_OldDesign(
        byte[] responseFrame,
        List<DeviceSpecification> devices)
    {
        if (responseFrame == null || responseFrame.Length == 0)
        {
            throw new ArgumentException("レスポンスフレームが空です", nameof(responseFrame));
        }

        if (devices == null || devices.Count == 0)
        {
            throw new ArgumentException("デバイスリストが空です", nameof(devices));
        }

        // フレームタイプ判定（3E or 4E）
        bool is4EFrame = responseFrame[0] == 0xD4 && responseFrame[1] == 0x00;
        bool is3EFrame = responseFrame[0] == 0xD0 && responseFrame[1] == 0x00;

        if (!is3EFrame && !is4EFrame)
        {
            throw new InvalidOperationException(
                $"未対応のフレームタイプです: サブヘッダ=0x{responseFrame[0]:X2}{responseFrame[1]:X2}"
            );
        }

        // フレーム検証
        ValidateResponseFrame(responseFrame, is4EFrame);

        // デバイスデータ部の開始位置
        int dataStartIndex = is4EFrame ? 15 : 11;

        // デバイスデータ部の期待サイズ（ワード数 × 2バイト/ワード）
        int expectedDataSize = devices.Count * 2;
        int actualDataSize = responseFrame.Length - dataStartIndex;

        if (actualDataSize < expectedDataSize)
        {
            throw new InvalidOperationException(
                $"デバイスデータ部のサイズが不足しています: 期待{expectedDataSize}バイト、実際{actualDataSize}バイト"
            );
        }

        // デバイスデータ抽出
        var deviceDataMap = new Dictionary<DeviceSpecification, ushort>();

        for (int i = 0; i < devices.Count; i++)
        {
            int dataIndex = dataStartIndex + (i * 2);
            ushort value = BitConverter.ToUInt16(responseFrame, dataIndex);
            deviceDataMap[devices[i]] = value;
        }

        return deviceDataMap;
    }

    /// <summary>
    /// レスポンスフレームの検証
    /// </summary>
    private static void ValidateResponseFrame(byte[] responseFrame, bool is4EFrame)
    {
        // 最小フレーム長検証（4E: サブヘッダ2 + シーケンス2 + 予約2 + ヘッダ9 = 15バイト、3E: サブヘッダ2 + ヘッダ9 = 11バイト）
        int minLength = is4EFrame ? 15 : 11;
        if (responseFrame.Length < minLength)
        {
            throw new InvalidOperationException(
                $"レスポンスフレーム長が不足しています: 最小{minLength}バイト、実際{responseFrame.Length}バイト"
            );
        }

        // エンドコード検証（4Eフレーム: バイト13-14、3Eフレーム: バイト9-10）
        int endCodeIndex = is4EFrame ? 13 : 9;

        // エンドコードは常に2バイト
        ushort endCode = BitConverter.ToUInt16(responseFrame, endCodeIndex);

        if (endCode != 0x0000)
        {
            // エンドコード異常（エラー応答）
            throw new InvalidOperationException(
                $"PLCからエラー応答を受信しました: エンドコード=0x{endCode:X4}"
            );
        }
    }

}
```
</details>

**実装時の更新内容**:
- ParseReadRandomResponse()の戻り値を`Dictionary<string, DeviceData>`に変更
- DeviceData.FromDeviceSpecification()でデバイスデータ生成
- DWordデバイス検出と結合ロジック追加
- デバイス名キー生成（DeviceSpecification.ToString()使用）

#### 変化点
- **Phase5初期設計**: Dictionary<DeviceSpecification, ushort>を返却
- **Phase4仕様変更後**: Dictionary<string, DeviceData>を返却
  - デバイス名キー構造
  - ビット・ワード・ダブルワード混在対応
  - Phase6で確定したList<DeviceSpecification>型と連携

---

### ステップ15: ProcessedResponseDataの構造拡張（2025-11-21更新）

#### 📋 実装戦略: 段階的クリーン移行

**戦略コンセプト**:
1. 「破綻しない」: Phase5～7で新旧構造を共存、既存コード無修正
2. 「不要なコードは削除」: Phase7完了時点で旧構造への依存ゼロ化 → Phase10で物理削除
3. ConMoni/PySLMPClient分析結果の統合

**新旧構造の共存期間**:
- **Phase5～7**: 新旧両構造を共存（破綻防止）
  - 新構造: `DeviceData` (デバイス名キー構造、Dictionary<string, DeviceData>)
  - 旧構造: `BasicProcessedDevices` / `CombinedDWordDevices` (リスト構造)
  - 互換性維持: 旧プロパティは動的変換で実装（get専用、Obsolete属性付き）
- **Phase7完了時点**: 旧構造への依存をゼロ化
  - DataOutputManager: 新構造のみ使用
  - LoggingManager: 新構造のみ使用
- **Phase10**: 旧構造を完全削除（Obsolete属性付きプロパティ・メソッド削除）

#### 実装対象
`andon/Core/Models/ProcessedResponseData.cs`

#### 実装内容（2025-11-21更新）

**新構造（Phase5～Phase10以降で使用）**:
1. **DeviceDataプロパティの定義**
   - `Dictionary<string, DeviceData> DeviceData { get; set; }`
   - デバイス名キー構造（例: "M0", "D100", "W0x11AA"）
   - ビット・ワード・ダブルワード混在データ対応

2. **統計情報の自動計算**
   - TotalProcessedDevices: DeviceData.Count
   - BitDeviceCount: DeviceData.Values.Count(d => d.Code.IsBitDevice())
   - WordDeviceCount: DeviceData.Values.Count(d => !d.Code.IsBitDevice() && !d.IsDWord)
   - DWordDeviceCount: DeviceData.Values.Count(d => d.IsDWord)

3. **ユーティリティメソッド**
   - GetDeviceValue(string deviceName): uint?
   - GetBitDevices(): List<string>
   - GetWordDevices(): List<string>
   - GetDWordDevices(): List<string>

**旧構造（Phase5～Phase10で維持、Phase10で削除予定）**:
1. **BasicProcessedDevicesプロパティ**
   - `[Obsolete("Phase10で削除予定。DeviceDataプロパティを使用してください。")]`
   - `List<ProcessedDevice> BasicProcessedDevices { get; }`（get専用、動的変換）
   - ConMoni互換性維持: ビット展開、変換係数対応

2. **CombinedDWordDevicesプロパティ**
   - `[Obsolete("Phase10で削除予定。DeviceDataプロパティを使用してください。")]`
   - `List<CombinedDWordDevice> CombinedDWordDevices { get; }`（get専用、動的変換）

3. **変換メソッド（Phase10で削除予定）**
   - ConvertToProcessedDevices(): DeviceData → ProcessedDevice変換
   - ConvertToCombinedDWordDevices(): DeviceData → CombinedDWordDevice変換
   - ExpandWordToBits(): ワード値をビット配列に展開（ConMoni方式: LSB first）

**ConMoni/PySLMPClient準拠機能の統合**:
1. **ビット展開**: ProcessedDeviceで実装（ConMoni方式）
2. **変換係数**: DeviceData.ConversionFactorプロパティ（Phase6で設定ファイルから取得）
3. **変換後値**: DeviceData.ConvertedValue = Value * ConversionFactor
4. **DWord明示指定**: DeviceSpecification.AccessMode列挙型（Phase6で追加）

#### 実装コード（サンプル）（2025-11-21更新）

**注意**: DeviceDataクラスはStep14-Aで既に実装済み（`andon/Core/Models/DeviceData.cs`）
**Phase6対応**: DeviceData.ConversionFactor, DeviceSpecification.AccessMode追加予定

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Andon.Core.Constants;

namespace Andon.Core.Models;

/// <summary>
/// レスポンス処理結果
/// Phase5実装: 新旧構造の共存期（Phase10で旧構造削除予定）
/// </summary>
public class ProcessedResponseData
{
    // ========================================
    // 新構造（Phase5～Phase10以降で使用）
    // ========================================

    /// <summary>
    /// デバイスデータマップ（デバイス名キー構造）
    /// Phase7: DataOutputManager/LoggingManagerで使用
    /// </summary>
    public Dictionary<string, DeviceData> DeviceData { get; set; } = new();

    public DateTime ProcessedAt { get; set; }
    public long ProcessingTimeMs { get; set; }
    public bool IsSuccess { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public FrameType FrameType { get; set; } = FrameType.Frame3E;

    // ========================================
    // 旧構造（Phase5～Phase10で維持、Phase10で削除）
    // ========================================

    /// <summary>
    /// 旧構造：ビット・ワードデバイスリスト
    /// Phase10で削除予定
    /// Phase7完了時点で使用箇所ゼロにする
    /// </summary>
    [Obsolete("Phase10で削除予定。DeviceDataプロパティを使用してください。")]
    public List<ProcessedDevice> BasicProcessedDevices
    {
        get => ConvertToProcessedDevices();
        set => throw new NotSupportedException("読み取り専用プロパティです");
    }

    /// <summary>
    /// 旧構造：DWordデバイスリスト
    /// Phase10で削除予定
    /// Phase7完了時点で使用箇所ゼロにする
    /// </summary>
    [Obsolete("Phase10で削除予定。DeviceDataプロパティを使用してください。")]
    public List<CombinedDWordDevice> CombinedDWordDevices
    {
        get => ConvertToCombinedDWordDevices();
        set => throw new NotSupportedException("読み取り専用プロパティです");
    }

    // ========================================
    // 変換メソッド（Phase10で削除予定）
    // ========================================

    /// <summary>
    /// DeviceData → ProcessedDevice変換
    /// ConMoni互換性維持：ビット展開、変換係数対応
    /// Phase10で削除予定
    /// </summary>
    [Obsolete("Phase10で削除予定")]
    private List<ProcessedDevice> ConvertToProcessedDevices()
    {
        var result = new List<ProcessedDevice>();

        foreach (var kvp in DeviceData.Where(kv => !kv.Value.IsDWord))
        {
            var deviceData = kvp.Value;
            var processed = new ProcessedDevice
            {
                DeviceName = deviceData.DeviceName,
                RawValue = (ushort)deviceData.Value,
                ConversionFactor = deviceData.ConversionFactor,
                // ConvertedValue自動計算（ProcessedDevice内のプロパティ）
            };

            // ビット展開（ConMoni方式）
            if (deviceData.Code.IsBitDevice())
            {
                processed.IsBitExpanded = true;
                processed.ExpandedBits = ExpandWordToBits((ushort)deviceData.Value);
            }

            result.Add(processed);
        }

        return result;
    }

    /// <summary>
    /// DeviceData → CombinedDWordDevice変換
    /// Phase10で削除予定
    /// </summary>
    [Obsolete("Phase10で削除予定")]
    private List<CombinedDWordDevice> ConvertToCombinedDWordDevices()
    {
        return DeviceData
            .Where(kv => kv.Value.IsDWord)
            .Select(kv => new CombinedDWordDevice
            {
                DeviceName = kv.Key,
                CombinedValue = kv.Value.Value,
                LowerWord = (ushort)(kv.Value.Value & 0xFFFF),
                UpperWord = (ushort)(kv.Value.Value >> 16)
            })
            .ToList();
    }

    /// <summary>
    /// ワード値をビット配列に展開（ConMoni方式：LSB first）
    /// Phase10で削除予定
    /// </summary>
    [Obsolete("Phase10で削除予定")]
    private bool[] ExpandWordToBits(ushort value)
    {
        var bits = new bool[16];
        for (int i = 0; i < 16; i++)
        {
            bits[i] = ((value >> i) & 1) == 1;
        }
        return bits;
    }

    // ========================================
    // 統計情報（新構造ベース）
    // ========================================

    public int TotalProcessedDevices => DeviceData.Count;
    public int BitDeviceCount => DeviceData.Values.Count(d => d.Code.IsBitDevice());
    public int WordDeviceCount => DeviceData.Values.Count(d => !d.Code.IsBitDevice() && !d.IsDWord);
    public int DWordDeviceCount => DeviceData.Values.Count(d => d.IsDWord);

    // ========================================
    // ユーティリティメソッド（新構造ベース）
    // ========================================

    public uint? GetDeviceValue(string deviceName)
    {
        return DeviceData.TryGetValue(deviceName, out var device) ? device.Value : null;
    }

    public List<string> GetBitDevices()
    {
        return DeviceData.Where(kv => kv.Value.Code.IsBitDevice()).Select(kv => kv.Key).ToList();
    }

    public List<string> GetWordDevices()
    {
        return DeviceData.Where(kv => !kv.Value.Code.IsBitDevice() && !kv.Value.IsDWord)
                         .Select(kv => kv.Key).ToList();
    }

    public List<string> GetDWordDevices()
    {
        return DeviceData.Where(kv => kv.Value.IsDWord).Select(kv => kv.Key).ToList();
    }
}
```

#### DeviceData.csの拡張（Phase6対応予定）

**Phase6で追加予定**:

```csharp
namespace Andon.Core.Models;

/// <summary>
/// デバイスデータ（Phase5実装）
/// Phase6拡張: ConMoni準拠機能（変換係数）
/// </summary>
public class DeviceData
{
    // ========================================
    // 基本処理結果
    // ========================================

    /// <summary>
    /// 元の受信生データ（16進数文字列）
    /// </summary>
    public string OriginalRawData { get; set; } = string.Empty;

    /// <summary>
    /// 処理済みデータ（デバイス名キー構造）
    /// キー例: "M0", "D100", "W0x11AA"
    /// 値: DeviceData（DeviceName, Code, Address, Value, IsDWord, IsHexAddress）
    /// </summary>
    public Dictionary<string, DeviceData> ProcessedData { get; set; } = new();

    /// <summary>
    /// 処理完了時刻
    /// </summary>
    public DateTime ProcessedAt { get; set; }

    /// <summary>
    /// 処理時間（ミリ秒）
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    // ========================================
    // エラー情報
    // ========================================

    /// <summary>
    /// 処理成功フラグ
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// エラー情報
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 警告情報
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    // ========================================
    // 統計情報（自動計算）
    // ========================================

    /// <summary>
    /// 処理済みデバイス総数
    /// </summary>
    public int TotalProcessedDevices => ProcessedData.Count;

    /// <summary>
    /// ビットデバイス数（DeviceCode.IsBitDevice()で判定）
    /// </summary>
    public int BitDeviceCount => ProcessedData.Values.Count(d => d.Code.IsBitDevice());

    /// <summary>
    /// ワードデバイス数（非ビット、非DWord）
    /// </summary>
    public int WordDeviceCount => ProcessedData.Values.Count(d => !d.Code.IsBitDevice() && !d.IsDWord);

    /// <summary>
    /// ダブルワードデバイス数
    /// </summary>
    public int DWordDeviceCount => ProcessedData.Values.Count(d => d.IsDWord);

    /// <summary>
    /// フレームタイプ（4Eフレーム解析対応）
    /// </summary>
    public FrameType FrameType { get; set; } = FrameType.Frame3E;

    // ========================================
    // ユーティリティメソッド
    // ========================================

    /// <summary>
    /// デバイス名から値を取得
    /// </summary>
    public uint? GetDeviceValue(string deviceName)
    {
        return ProcessedData.TryGetValue(deviceName, out var data) ? data.Value : null;
    }

    /// <summary>
    /// ビットデバイス一覧を取得
    /// </summary>
    public List<string> GetBitDevices()
    {
        return ProcessedData.Where(kv => kv.Value.Code.IsBitDevice()).Select(kv => kv.Key).ToList();
    }

    /// <summary>
    /// ワードデバイス一覧を取得
    /// </summary>
    public List<string> GetWordDevices()
    {
        return ProcessedData.Where(kv => !kv.Value.Code.IsBitDevice() && !kv.Value.IsDWord).Select(kv => kv.Key).ToList();
    }

    /// <summary>
    /// ダブルワードデバイス一覧を取得
    /// </summary>
    public List<string> GetDWordDevices()
    {
        return ProcessedData.Where(kv => kv.Value.IsDWord).Select(kv => kv.Key).ToList();
    }
}
```

    public string DeviceName { get; set; } = string.Empty;
    public DeviceCode Code { get; set; }
    public int Address { get; set; }
    public uint Value { get; set; }
    public bool IsDWord { get; set; }
    public bool IsHexAddress { get; set; }

    // ========================================
    // Phase6拡張: ConMoni準拠機能
    // ========================================

    /// <summary>
    /// 変換係数（Phase6: appsettings.jsonから取得）
    /// ConMoni: accessDeviceDigit準拠
    /// </summary>
    public double ConversionFactor { get; set; } = 1.0;

    /// <summary>
    /// 変換後の値
    /// ConMoni方式: Value * ConversionFactor
    /// </summary>
    public double ConvertedValue => Value * ConversionFactor;

    // ファクトリメソッドも変換係数対応に更新...
}
```

#### DeviceSpecification.csの拡張（Phase6対応予定）

**Phase6で追加予定**:

```csharp
namespace Andon.Core.Models;

public class DeviceSpecification
{
    public DeviceCode Code { get; set; }
    public int Address { get; set; }
    public bool IsHexAddress { get; set; }

    // ========================================
    // Phase6拡張: ConMoni/PySLMPClient準拠機能
    // ========================================

    /// <summary>
    /// 変換係数（Phase6: appsettings.jsonから取得）
    /// ConMoni: accessDeviceDigit準拠
    /// </summary>
    public double ConversionFactor { get; set; } = 1.0;

    /// <summary>
    /// アクセスモード（Phase6: DWord対応）
    /// PySLMPClient準拠: 明示的指定
    /// </summary>
    public AccessMode AccessMode { get; set; } = AccessMode.Word;

    // 既存メソッド...
}

/// <summary>
/// デバイスアクセスモード（Phase6: PySLMPClient準拠）
/// </summary>
public enum AccessMode
{
    Word,   // 16ビット（デフォルト）
    DWord   // 32ビット（PySLMPClient: dword_points）
}
```

#### appsettings.json の最終形（Phase6対応予定）

```json
{
  "PlcConnection": {
    "FrameVersion": "4E",
    "Timeout": 8000,
    "Devices": [
      {
        "DeviceType": "D",
        "DeviceNumber": 100,
        "AccessMode": "Word",
        "ConversionFactor": 0.1,
        "Description": "温度センサー（0.1℃単位）"
      },
      {
        "DeviceType": "D",
        "DeviceNumber": 200,
        "AccessMode": "DWord",
        "ConversionFactor": 1.0,
        "Description": "累積カウンタ（32ビット）"
      },
      {
        "DeviceType": "M",
        "DeviceNumber": 0,
        "AccessMode": "Word",
        "ConversionFactor": 1.0,
        "Description": "運転状態（ビット展開される）"
      }
    ]
  }
}
```

#### フェーズごとの移行計画

**Phase5（現在）**:
- 新構造: DeviceData導入、ProcessedResponseData.DeviceDataプロパティ
- 旧構造: BasicProcessedDevices/CombinedDWordDevices（動的変換）
- 状態: 共存、既存コード無修正

**Phase6**:
- 設定ファイル拡張: ConversionFactor, AccessMode追加
- DeviceSpecification拡張: 新フィールド対応
- 状態: 共存継続、既存コード無修正

**Phase7**:
- DataOutputManager: 新構造(DeviceData)のみ使用
- LoggingManager: 新構造(DeviceData)のみ使用
- 状態: 旧プロパティへの実質的依存ゼロ

**Phase8**:
- 統合テスト: 新構造のみでテスト
- 状態: 旧プロパティ使用ゼロ確認

**Phase10**:
- 削除対象:
  - BasicProcessedDevices/CombinedDWordDevices プロパティ
  - ConvertToProcessedDevices() メソッド
  - ConvertToCombinedDWordDevices() メソッド
  - ExpandWordToBits() メソッド
  - ProcessedDevice/CombinedDWordDevice クラス（判断待ち）
- 判断保留:
  - ProcessedDevice: ビット展開機能が他で必要なら残す
  - CombinedDWordDevice: DWord処理が他で必要なら残す

#### この戦略の利点

**1. 破綻しない**:
- ✅ Phase5～7: 既存コード無修正で動作
- ✅ 旧プロパティは動的変換で互換性維持
- ✅ ビルドエラーゼロ

**2. 不要なコードは削除**:
- ✅ Phase7完了時点で旧構造への依存ゼロ
- ✅ Phase10で旧構造を完全削除
- ✅ Obsolete属性で削除対象を明示

**3. ConMoni/PySLMPClient準拠**:
- ✅ ビット展開: ProcessedDeviceで実装（ConMoni方式）
- ✅ 変換係数: 設定ファイル管理（ConMoni方式）
- ✅ DWord指定: 明示的指定（PySLMPClient方式）

**4. 計画的移行**:
- ✅ 各Phaseで明確な責務
- ✅ Phase7で実質的に新構造へ移行完了
- ✅ Phase10で物理的に旧構造削除

#### 変化点（2025-11-21更新）
- **Phase5初期設計（2025-11-20）**:
  - DeviceDataクラス: Value (object), Type (string)
  - OriginalRequestプロパティ必須

- **Phase5実装版（2025-11-21）: 段階的クリーン移行戦略**:
  - DeviceDataクラス: Step14-Aで実装済み（DeviceName, Code, Address, Value (uint), IsDWord, IsHexAddress）
  - ProcessedResponseData.DeviceData: Dictionary<string, DeviceData>型（新構造）
  - 旧構造: BasicProcessedDevices/CombinedDWordDevices（Obsolete属性、動的変換、Phase10削除予定）
  - 統計情報はDeviceDataのプロパティから自動計算
  - ConMoni/PySLMPClient準拠機能統合（変換係数、ビット展開、DWord明示指定）
  - Phase5～7で共存、Phase7で旧構造依存ゼロ化、Phase10で物理削除

---

### ステップ16: レスポンス処理のテスト作成

#### 実装対象
`andon/Tests/Unit/Utilities/SlmpDataParserTests.cs`

#### テスト内容
1. **ParseReadRandomResponse()の基本テスト**
   - 正常系: 3Eフレームレスポンスのパース
   - 正常系: 4Eフレームレスポンスのパース
   - デバイス数とデータ数の整合性検証
   - リトルエンディアン変換の正確性検証

2. **異常系テスト**
   - 空のレスポンスフレーム
   - 不正なサブヘッダ
   - エンドコード異常（PLC側エラー）
   - データ部のサイズ不足
   - デバイスリストとデータ数の不一致

3. **memo.md実データテスト**
   - memo.mdの実データ（111バイト）をパース
   - 48デバイスのデータ値を正確に抽出
   - M0ワード値 = 0x0100（256）の検証

#### テストコード（サンプル）

```csharp
using Xunit;
using Andon.Utilities;
using Andon.Core.Constants;
using Andon.Core.Models;

namespace Andon.Tests.Unit.Utilities;

public class SlmpDataParserTests
{
    [Fact]
    public void ParseReadRandomResponse_4EFrame_ValidResponse_ReturnsCorrectData()
    {
        // Arrange
        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.M, 0),
            new DeviceSpecification(DeviceCode.M, 16),
            new DeviceSpecification(DeviceCode.M, 32)
        };

        // 4Eフレーム応答（サブヘッダ2 + シーケンス2 + 予約2 + ネットワーク等7 + データ長2 = 15バイト + エンドコード2バイト + データ6バイト = 23バイト）
        byte[] responseFrame = new byte[]
        {
            // サブヘッダ2バイト
            0xD4, 0x00,
            // シーケンス番号2バイト
            0x00, 0x00,
            // 予約2バイト
            0x00, 0x00,
            // ネットワーク番号1バイト
            0x00,
            // PC番号1バイト
            0xFF,
            // I/O番号2バイト（LE）
            0xFF, 0x03,
            // マルチドロップ局番1バイト
            0x00,
            // データ長2バイト（LE: 8バイト = エンドコード2 + データ6）
            0x08, 0x00,
            // エンドコード2バイト（正常）
            0x00, 0x00,
            // デバイスデータ6バイト（3ワード × 2バイト）
            0x01, 0x00,  // M0 = 0x0001
            0x02, 0x00,  // M16 = 0x0002
            0x03, 0x00   // M32 = 0x0003
        };

        // Act
        var result = SlmpDataParser.ParseReadRandomResponse(responseFrame, devices);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(0x0001, result[devices[0]]);
        Assert.Equal(0x0002, result[devices[1]]);
        Assert.Equal(0x0003, result[devices[2]]);
    }

    [Fact]
    public void ParseReadRandomResponse_3EFrame_ValidResponse_ReturnsCorrectData()
    {
        // Arrange
        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100),
            new DeviceSpecification(DeviceCode.D, 200)
        };

        // 3Eフレーム応答（サブヘッダ2 + ネットワーク等7 = 9バイト + エンドコード2バイト + データ4バイト = 15バイト）
        byte[] responseFrame = new byte[]
        {
            // サブヘッダ2バイト
            0xD0, 0x00,
            // ネットワーク番号1バイト
            0x00,
            // PC番号1バイト
            0xFF,
            // I/O番号2バイト（LE）
            0xFF, 0x03,
            // マルチドロップ局番1バイト
            0x00,
            // データ長2バイト（LE: 6バイト = エンドコード2 + データ4）
            0x06, 0x00,
            // エンドコード2バイト（正常）
            0x00, 0x00,
            // デバイスデータ4バイト（2ワード × 2バイト）
            0x64, 0x00,  // D100 = 0x0064 = 100
            0xC8, 0x00   // D200 = 0x00C8 = 200
        };

        // Act
        var result = SlmpDataParser.ParseReadRandomResponse(responseFrame, devices);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(0x0064, result[devices[0]]);
        Assert.Equal(0x00C8, result[devices[1]]);
    }

    [Fact]
    public void ParseReadRandomResponse_ErrorEndCode_ThrowsException()
    {
        // Arrange
        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100)
        };

        // エラーレスポンス（エンドコード = 0xC051 = デバイス範囲エラー）
        byte[] responseFrame = new byte[]
        {
            // サブヘッダ2バイト
            0xD0, 0x00,
            // ネットワーク番号1バイト
            0x00,
            // PC番号1バイト
            0xFF,
            // I/O番号2バイト（LE）
            0xFF, 0x03,
            // マルチドロップ局番1バイト
            0x00,
            // データ長2バイト（LE: 2バイト = エンドコードのみ）
            0x02, 0x00,
            // エンドコード2バイト（エラー）
            0x51, 0xC0  // エンドコード: 0xC051（エラー）
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => SlmpDataParser.ParseReadRandomResponse(responseFrame, devices)
        );
        Assert.Contains("エラー応答を受信しました", ex.Message);
        Assert.Contains("0xC051", ex.Message);
    }

    [Fact]
    public void ParseReadRandomResponse_MemoMdRealData_ReturnsCorrectData()
    {
        // Arrange: memo.mdの実データ（M0-M47の48ワード）
        var devices = new List<DeviceSpecification>();
        for (int i = 0; i < 48; i++)
        {
            devices.Add(new DeviceSpecification(DeviceCode.M, i * 16));
        }

        // memo.md実データ（111バイト）
        string hexResponse =
            "D4000000000000FF03000000006300002000" +
            "00000000" +
            "0001000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000" +
            "000000000000000000000000";

        byte[] responseFrame = new byte[hexResponse.Length / 2];
        for (int i = 0; i < hexResponse.Length; i += 2)
        {
            responseFrame[i / 2] = Convert.ToByte(hexResponse.Substring(i, 2), 16);
        }

        // Act
        var result = SlmpDataParser.ParseReadRandomResponse(responseFrame, devices);

        // Assert
        Assert.Equal(48, result.Count);
        Assert.Equal(0x0100, result[devices[0]]);  // M0 = 0x0100 = 256
        Assert.Equal(0x0000, result[devices[1]]);  // M16 = 0x0000 = 0
    }
}
```

---

## 完了条件
- ✅ SlmpDataParser.ParseReadRandomResponse()実装完了
- ✅ ProcessedResponseData.DeviceValueMap実装完了
- ✅ SlmpDataParserTests全テストパス
- ✅ memo.md実データのパーステスト成功
- ✅ ReadRandomレスポンスが正しくパース可能

## 次フェーズへの依存関係
- Phase6（設定ファイル構造の変更）で、新しいデバイス指定形式に対応します
- Phase7（データ出力処理の修正）で、不連続デバイスのCSV出力に対応します

## リスク管理
| リスク | 影響 | 対策 |
|--------|------|------|
| **エンドコード解析ミス** | 高 | ・SLMP仕様書に基づく厳密な検証<br>・実機テストデータでの検証 |
| **リトルエンディアン変換ミス** | 高 | ・BitConverter.ToUInt16()の使用<br>・単体テストでの徹底検証 |
| **フレームタイプ判定ミス** | 中 | ・3E/4E両フレームの単体テスト |

---

**作成日**: 2025-11-18
**元ドキュメント**: read_to_readrandom_migration_plan.md
