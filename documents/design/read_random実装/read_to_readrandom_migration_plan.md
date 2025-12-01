# Read(0401)からReadRandom(0403)への移行技術仕様書

## 作成日時
2025-11-14

## 概要
andonプロジェクトにおいて、現在のRead(0x0401)コマンドをReadRandom(0x0403)コマンドに置き換えるための技術仕様書です。conmoni_testプロジェクトの実装を参考に、具体的な実装方針を定義します。

---

## 1. コマンドの違い（SLMP仕様書準拠）

### 1.1 Read(0x0401) - 一括読み出し（連続デバイス）

**コマンドコード**: `0x01 0x04` (リトルエンディアン)

**用途**: 連続したデバイスアドレスのデータを一括で読み出す

**SLMP仕様書参照**: pdf2img/page_65.png（5.2 Device(デバイスアクセス)）

**フレーム構造（データ部）**:
```
| バイト | フィールド名 | 内容 | 例 |
|--------|-------------|------|-----|
| 0-1    | コマンド | 0x01 0x04 | 一括読み出し |
| 2-3    | サブコマンド | 0x00 0x00 または 0x00 0x02 | ワード単位/Dword単位 |
| 4      | デバイスコード | 0xA8等 | Dデバイス等 |
| 5-7    | 開始デバイス番号 | 3バイト（LE） | D100 = [0x64, 0x00, 0x00] |
| 8-9    | 読出し点数 | 2バイト（LE） | 10点 = [0x0A, 0x00] |
```

**サブコマンド詳細**（SLMP仕様書 page_65.png）:
- `0x0000`: ワード単位（1ワード=16ビット）
- `0x0002`: Dword単位（1Dword=32ビット）

**データ長**: 10バイト（固定）

**特徴**:
- ✅ シンプルな構造
- ✅ 連続デバイスに最適
- ✅ 1回の通信で最大960点まで読み出し可能
- ❌ 飛び飛びのデバイスには非効率（複数回通信が必要）
- ❌ デバイス種別ごとに別々の通信が必要

**読み出し可能点数制限**（SLMP仕様書 page_66.png）:
- ワードアクセス点数 + ダブルワードアクセス点数 ≦ 960点
- ビットデバイスは16点=1ワード、ワードデバイスは1ワード単位

**例**: D100～D109を読み出す
```
コマンド部: [0x01, 0x04, 0x00, 0x00, 0xA8, 0x64, 0x00, 0x00, 0x0A, 0x00]
```

---

### 1.2 ReadRandom(0x0403) - ランダム読み出し（不連続デバイス）

**コマンドコード**: `0x03 0x04` (リトルエンディアン)

**用途**: 飛び飛びの（不連続な）デバイスアドレスのデータを一度に読み出す

**SLMP仕様書参照**: pdf2img/page_63.png, page_64.png（Entry Monitor Device）

**フレーム構造（データ部）**:
```
| バイト | フィールド名 | 内容 | 例 |
|--------|-------------|------|-----|
| 0-1    | コマンド | 0x03 0x04 | ランダム読み出し |
| 2-3    | サブコマンド | 0x00 0x00 または 0x00 0x02 | ワード単位/Dword単位 |
| 4      | ワードアクセス点数 | 1バイト | 16点 = 0x10 |
| 5      | Dwordアクセス点数 | 1バイト | 0点 = 0x00 |
| 6-     | デバイス指定 | 4バイト×点数 | 後述 |
```

**サブコマンド詳細**（SLMP仕様書 page_63.png）:
- `0x0000`: ワード単位（1ワード=16ビット）
- `0x0002`: Dword単位（1Dword=32ビット）

**デバイス指定（4バイト構造）**:
```
| バイト | フィールド名 | 内容 | 例 |
|--------|-------------|------|-----|
| 0-2    | デバイス番号 | 3バイト（LE） | D100 = [0x64, 0x00, 0x00] |
| 3      | デバイスコード | 1バイト | 0xA8 = Dデバイス |
```

**データ長**: 6 + (4 × 点数) バイト（可変長）

**特徴**:
- ✅ 飛び飛びのデバイスを1回の通信で読み出せる
- ✅ 異なるデバイス種別（D, M, W等）を混在して読み出し可能
- ✅ 通信回数を大幅に削減
- ✅ ワードとDwordを同時に指定可能
- ⚠️ フレームサイズが大きくなる（1点につき4バイト追加）
- ⚠️ デバイス指定部の構築が複雑

**読み出し可能点数制限**（SLMP仕様書 page_64.png）:
- サブコマンド0x0002使用時: ワードアクセス点数 + ダブルワードアクセス点数 ≦ 96点
- サブコマンド0x0000使用時: ワードアクセス点数 + ダブルワードアクセス点数 ≦ 192点

**制約事項**（SLMP仕様書 page_64.png）:
以下のデバイスは指定できません:
- タイマの接点(TS)およびコイル(TC)
- ロングタイマの接点(LTS)、コイル(LTC)、および現在値(LTN)
- 積算タイマの接点(STS)およびコイル(STC)
- ロング積算タイマの接点(LSTS)、コイル(LSTC)、および現在値(LSTN)
- カウンタの接点(CS)およびコイル(CC)
- ロングカウンタの接点(LCS)、コイル(LCC)、および現在値(LCN)

**例**: D100, D105, M200を読み出す（ワード3点）
```
コマンド部:
  [0x03, 0x04, 0x00, 0x00, 0x03, 0x00,  // コマンド+サブコマンド+ワード3点+Dword0点
   0x64, 0x00, 0x00, 0xA8,              // D100（デバイスコード0xA8=D）
   0x69, 0x00, 0x00, 0xA8,              // D105
   0xC8, 0x00, 0x00, 0x90]              // M200（デバイスコード0x90=M）
```

**SLMP仕様書の交信例**（pdf2img/page_71.png）:
```
読み出し内容:
  - ワードデバイス: ブロック1: D0～D3(4点)、ブロック2: W100～W107(8点)
  - ビットデバイス: ブロック1: M0～M31(2点)、ブロック2: M128～M159(2点)、ブロック3: B100～B12F(3点)

ASCII要求データ例（サブコマンド0x0406）:
  0 4 0 6  0 0 0 0  0 2 0 3  ...

  デバイス指定:
  D * 0 0 0 0 0 0 | 0 0 0 4 | W * 0 0 0 1 0 0 | 0 0 0 8 | ...
  (Dデバイス番号0、点数4) (Wデバイス番号100、点数8)
```

---

## 2. conmoni_testの実装分析

### 2.1 送信データ構造（settings_decimal.txt相当）

conmoni_testのハードコードされた送信データ（SEND_DATA配列）を分析:

```csharp
private static readonly int[] SEND_DATA = new int[]
{
    // ========== 3Eフレームヘッダ（変則形式）==========
    84,0,0,0,0,0,0,    // [0-6] サブヘッダ+シーケンス+予約（7バイト）
    255,255,3,0,       // [7-10] ネットワーク+局番+I/O番号（4バイト）
    200,0,             // [11-12] データ長（200バイト=0xC8、動的計算）
    32,0,              // [13-14] 監視タイマ（32=8秒）

    // ========== ReadRandomコマンド部 ==========
    3,4,               // [15-16] コマンド（0x0403=ReadRandom）
    0,0,               // [17-18] サブコマンド（0x0000）
    48,0,              // [19-20] ワード点数（48点=0x30）、Dword点数（0点）

    // ========== デバイス指定部（4バイト×48点=192バイト）==========
    // フォーマット: [デバイス番号3バイト(LE), デバイスコード1バイト]

    // --- Dデバイス（ワード型、10進アドレス）---
    72,238,0,168,      // D61000 (0xEE48): [0x48, 0xEE, 0x00, 0xA8]
    75,238,0,168,      // D61003 (0xEE4B): [0x4B, 0xEE, 0x00, 0xA8]
    82,238,0,168,      // D61010 (0xEE52): [0x52, 0xEE, 0x00, 0xA8]
    92,238,0,168,      // D61020 (0xEE5C): [0x5C, 0xEE, 0x00, 0xA8]

    // --- Wデバイス（ワード型、16進アドレス）---
    170,24,1,168,      // W0x011AA (4522): [0xAA, 0x18, 0x01, 0xA8]
    220,24,1,168,      // W0x011DC (4572): [0xDC, 0x18, 0x01, 0xA8]
    // ... 以下略 ...
};
```

**重要ポイント**:
1. **デバイス番号のリトルエンディアン変換**:
   - 10進D61000 (0xEE48) → [0x48, 0xEE, 0x00]
   - 16進W0x11AA → [0xAA, 0x18, 0x01]

2. **デバイスコード**:
   - 0xA8 = Dデバイス（データレジスタ）
   - 0x90 = Mデバイス（内部リレー）
   - 0x9C = ZRデバイス（ファイルレジスタ）

3. **ワード点数**:
   - 1バイトで指定（最大255点）
   - conmoni_testでは48点（0x30）

---

### 2.2 フレーム構築ロジック（ConMoni GenerateSettingJson.py相当）

conmoni_testの参考元であるConMoniプロジェクトのフレーム構築ロジック:

#### Step 1: ヘッダ部構築
```python
self.accessPlcSetting["accessPlcSetting"].extend([
    0x54, 0x00,           # サブヘッダ（変則）
    0x00, 0x00,           # シリアル
    0x00, 0x00,           # 予約
    0x00,                 # ネットワーク番号
    0xFF,                 # 局番
    0xFF, 0x03,           # I/O番号（LE）
    0x00,                 # マルチドロップ
    0xFF, 0x03,           # データ長（後で動的計算）
    0x20, 0x00,           # 監視タイマ（8秒）
])
```

#### Step 2: コマンド部構築
```python
self.accessPlcSetting["accessPlcSetting"].extend([
    0x03, 0x04,           # ReadRandomコマンド
    0x00, 0x00,           # サブコマンド
    0x00,                 # ワード点数（後で動的設定）
    0x00                  # Dword点数
])
```

#### Step 3: デバイス指定部構築（10進デバイス）
```python
byte_order = "little"
for index, value in enumerate(_dfDec["デバイス番号"]):
    if isinstance(value, str):
        value = int(value)
    # 3バイトリトルエンディアン変換
    splitHexValue = value.to_bytes(3, byte_order)
    hexToIntValue = [b for b in splitHexValue]

    # デバイス番号3バイト + デバイスコード1バイト
    self.accessPlcSetting["accessPlcSetting"].extend([
        hexToIntValue[0],  # 下位バイト
        hexToIntValue[1],  # 中位バイト
        hexToIntValue[2],  # 上位バイト
        device_code        # デバイスコード（0xA8等）
    ])
```

#### Step 4: デバイス指定部構築（16進デバイス）
```python
# 16進デバイス（X, Y, W等）の処理
_dfHex["デバイス番号"] = _dfHex["デバイス番号"].str.zfill(6)  # 6桁パディング

# 2桁ずつ分割（16進文字列として）
_dfHex["通信用1桁目"] = _dfHex["デバイス番号"].str[4:]     # 下位2桁
_dfHex["通信用2桁目"] = _dfHex["デバイス番号"].str[2:4]   # 中位2桁
_dfHex["通信用3桁目"] = _dfHex["デバイス番号"].str[0:2]   # 上位2桁

# 16進文字列を整数に変換
_dfHex["通信用1桁目"] = _dfHex["通信用1桁目"].apply(lambda x: int(x, 16))
_dfHex["通信用2桁目"] = _dfHex["通信用2桁目"].apply(lambda x: int(x, 16))
_dfHex["通信用3桁目"] = _dfHex["通信用3桁目"].apply(lambda x: int(x, 16))
```

**例**: W0x11AA
```
文字列入力: "11AA"
→ 6桁パディング: "0011AA"
→ 分割: ["00", "11", "AA"]
→ 16進変換: [0x00, 0x11, 0xAA]
→ リトルエンディアン: [0xAA, 0x11, 0x00]
```

#### Step 5: データ長の動的計算
```python
# フレーム全体からデータ長を動的計算（バイト13以降）
numData = len(self.accessPlcSetting["accessPlcSetting"][13:])
hexDevices = str(hex(numData)[2:].zfill(4))

# リトルエンディアンで格納（バイト11-12）
self.accessPlcSetting["accessPlcSetting"][11] = int(hexDevices[2:], 16)  # 下位
self.accessPlcSetting["accessPlcSetting"][12] = int(hexDevices[:2], 16)  # 上位
```

**例**: データ長200バイト（0xC8）
```
numData = 200
hex(200) = "0xc8"
hexDevices = "00c8"
→ バイト11 = int("c8", 16) = 200 (0xC8)
→ バイト12 = int("00", 16) = 0 (0x00)
```

#### Step 6: ワード点数の設定
```python
# ワード型デバイスの合計点数を設定（バイト19）
word_count = len(word_devices)
self.accessPlcSetting["accessPlcSetting"][19] = word_count
```

---

## 3. andonプロジェクトへの実装方針

### 3.1 実装対象クラス

#### 主要クラス:
1. **SlmpFrameBuilder** (`andon/Utilities/SlmpFrameBuilder.cs`)
   - ReadRandomフレーム構築メソッドの実装

2. **DeviceConstants** (`andon/Core/Constants/DeviceConstants.cs`)
   - デバイスコード定義の追加

3. **ConfigToFrameManager** (`andon/Core/Managers/ConfigToFrameManager.cs`)
   - 設定からフレーム構築への統合

---

### 3.2 実装ステップ

#### Phase 1: デバイスコード定義（優先度：最高）

**ファイル**: `andon/Core/Constants/DeviceConstants.cs`

```csharp
namespace Andon.Core.Constants;

/// <summary>
/// SLMPデバイスコード定義
/// </summary>
public enum DeviceCode : byte
{
    // ビットデバイス（16点=1ワード）
    SM = 0x91,   // 特殊リレー
    X = 0x9C,    // 入力
    Y = 0x9D,    // 出力
    M = 0x90,    // 内部リレー
    L = 0x92,    // ラッチリレー
    F = 0x93,    // アナンシエータ
    B = 0xA0,    // リンクリレー

    // ワードデバイス
    SD = 0xA9,   // 特殊レジスタ
    D = 0xA8,    // データレジスタ
    W = 0xB4,    // リンクレジスタ
    R = 0xAF,    // ファイルレジスタ
    ZR = 0xB0,   // ファイルレジスタ（拡張）

    // タイマー
    TN = 0xC2,   // タイマ現在値
    TS = 0xC1,   // タイマ接点
    TC = 0xC0,   // タイマコイル

    // カウンタ
    CN = 0xC5,   // カウンタ現在値
    CS = 0xC4,   // カウンタ接点
    CC = 0xC3,   // カウンタコイル
}

/// <summary>
/// デバイスコード拡張メソッド
/// </summary>
public static class DeviceCodeExtensions
{
    /// <summary>
    /// 16進アドレス表記のデバイスかどうか
    /// </summary>
    private static readonly HashSet<DeviceCode> HexAddressDevices = new()
    {
        DeviceCode.X,
        DeviceCode.Y,
        DeviceCode.B,
        DeviceCode.W,
        DeviceCode.ZR
    };

    /// <summary>
    /// デバイスコードが16進アドレス表記かを判定
    /// </summary>
    public static bool IsHexAddress(this DeviceCode code)
        => HexAddressDevices.Contains(code);

    /// <summary>
    /// デバイスコードがビット型かを判定
    /// </summary>
    private static readonly HashSet<DeviceCode> BitDevices = new()
    {
        DeviceCode.SM,
        DeviceCode.X,
        DeviceCode.Y,
        DeviceCode.M,
        DeviceCode.L,
        DeviceCode.F,
        DeviceCode.B,
        DeviceCode.TS,
        DeviceCode.TC,
        DeviceCode.CS,
        DeviceCode.CC
    };

    /// <summary>
    /// デバイスコードがビット型かを判定
    /// </summary>
    public static bool IsBitDevice(this DeviceCode code)
        => BitDevices.Contains(code);
}
```

---

#### Phase 2: デバイス指定データ構造（優先度：最高）

**ファイル**: `andon/Core/Models/DeviceSpecification.cs`（新規作成）

```csharp
namespace Andon.Core.Models;

/// <summary>
/// デバイス指定情報（ReadRandom用）
/// </summary>
public class DeviceSpecification
{
    /// <summary>
    /// デバイスコード
    /// </summary>
    public DeviceCode Code { get; set; }

    /// <summary>
    /// デバイス番号（10進表記）
    /// </summary>
    /// <remarks>
    /// 16進デバイス（X, Y等）も10進で格納
    /// 例: W0x11AA → 4522（10進）
    /// </remarks>
    public int DeviceNumber { get; set; }

    /// <summary>
    /// デバイス番号が16進表記かどうか
    /// </summary>
    public bool IsHexAddress { get; set; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public DeviceSpecification(DeviceCode code, int deviceNumber, bool isHexAddress = false)
    {
        Code = code;
        DeviceNumber = deviceNumber;
        IsHexAddress = isHexAddress;
    }

    /// <summary>
    /// 16進デバイス番号から生成（例: "11AA" → 0x11AA）
    /// </summary>
    public static DeviceSpecification FromHexString(DeviceCode code, string hexString)
    {
        int deviceNumber = Convert.ToInt32(hexString, 16);
        return new DeviceSpecification(code, deviceNumber, isHexAddress: true);
    }

    /// <summary>
    /// デバイス番号を3バイト配列に変換（リトルエンディアン）
    /// </summary>
    public byte[] ToDeviceNumberBytes()
    {
        return new byte[]
        {
            (byte)(DeviceNumber & 0xFF),           // 下位バイト
            (byte)((DeviceNumber >> 8) & 0xFF),    // 中位バイト
            (byte)((DeviceNumber >> 16) & 0xFF)    // 上位バイト
        };
    }

    /// <summary>
    /// 4バイトデバイス指定配列に変換（ReadRandom用）
    /// </summary>
    public byte[] ToDeviceSpecificationBytes()
    {
        var result = new byte[4];
        var deviceNumberBytes = ToDeviceNumberBytes();

        // デバイス番号（3バイト）
        Array.Copy(deviceNumberBytes, 0, result, 0, 3);

        // デバイスコード（1バイト）
        result[3] = (byte)Code;

        return result;
    }

    /// <summary>
    /// デバッグ用文字列表現
    /// </summary>
    public override string ToString()
    {
        if (IsHexAddress)
        {
            return $"{Code}0x{DeviceNumber:X}";
        }
        else
        {
            return $"{Code}{DeviceNumber}";
        }
    }
}
```

---

#### Phase 3: ReadRandomフレーム構築メソッド（優先度：最高）

**ファイル**: `andon/Utilities/SlmpFrameBuilder.cs`

```csharp
using Andon.Core.Constants;
using Andon.Core.Models;

namespace Andon.Utilities;

/// <summary>
/// SLMPフレーム構築ユーティリティ
/// </summary>
public class SlmpFrameBuilder
{
    /// <summary>
    /// ReadRandom(0x0403)要求フレームを構築
    /// </summary>
    /// <param name="devices">読み出すデバイスのリスト</param>
    /// <param name="frameType">フレームタイプ（3E/4E）</param>
    /// <param name="timeout">監視タイマ（250ms単位、デフォルト8秒=32）</param>
    /// <returns>送信用バイト配列</returns>
    public static byte[] BuildReadRandomRequest(
        List<DeviceSpecification> devices,
        string frameType = "3E",
        ushort timeout = 32)
    {
        if (devices == null || devices.Count == 0)
        {
            throw new ArgumentException("デバイスリストが空です", nameof(devices));
        }

        if (devices.Count > 255)
        {
            throw new ArgumentException($"デバイス点数が上限を超えています: {devices.Count}点（最大255点）", nameof(devices));
        }

        var frame = new List<byte>();

        // ========================================
        // 1. ヘッダ部構築
        // ========================================
        if (frameType == "3E")
        {
            // 標準3Eフレーム
            frame.AddRange(new byte[] { 0x50, 0x00 });  // サブヘッダ
        }
        else if (frameType == "4E")
        {
            // 標準4Eフレーム
            frame.AddRange(new byte[] { 0x54, 0x00 });  // サブヘッダ
            frame.AddRange(new byte[] { 0x00, 0x00 });  // シーケンス番号（TODO: 管理機能実装）
            frame.AddRange(new byte[] { 0x00, 0x00 });  // 予約
        }
        else
        {
            throw new ArgumentException($"未対応のフレームタイプ: {frameType}", nameof(frameType));
        }

        // ネットワーク番号・局番・I/O番号・マルチドロップ
        frame.Add(0x00);                                // ネットワーク番号
        frame.Add(0xFF);                                // 局番（自局）
        frame.AddRange(BitConverter.GetBytes((ushort)0x03FF));  // I/O番号（LE）
        frame.Add(0x00);                                // マルチドロップ局番

        // データ長（仮値、後で確定）
        int dataLengthPosition = frame.Count;
        frame.AddRange(new byte[] { 0x00, 0x00 });

        // 監視タイマ（250ms単位）
        frame.AddRange(BitConverter.GetBytes(timeout));

        // ========================================
        // 2. コマンド部構築
        // ========================================
        // コマンド: 0x0403 (ReadRandom)
        frame.AddRange(BitConverter.GetBytes((ushort)0x0403));

        // サブコマンド: 0x0000（固定）
        frame.AddRange(BitConverter.GetBytes((ushort)0x0000));

        // ワード点数（1バイト）
        byte wordCount = (byte)devices.Count;
        frame.Add(wordCount);

        // Dword点数（1バイト、現在未対応）
        frame.Add(0x00);

        // ========================================
        // 3. デバイス指定部構築
        // ========================================
        foreach (var device in devices)
        {
            // 4バイトデバイス指定: [デバイス番号3バイト(LE), デバイスコード1バイト]
            frame.AddRange(device.ToDeviceSpecificationBytes());
        }

        // ========================================
        // 4. データ長確定
        // ========================================
        // データ長 = コマンド部以降のバイト数
        int headerSize = frameType == "3E" ? 2 : 6;
        int dataLength = frame.Count - headerSize - 9;  // ヘッダ（2 or 6）+ 固定部（9）を除く

        // リトルエンディアンで格納
        frame[dataLengthPosition] = (byte)(dataLength & 0xFF);
        frame[dataLengthPosition + 1] = (byte)((dataLength >> 8) & 0xFF);

        return frame.ToArray();
    }

    /// <summary>
    /// 旧Read(0x0401)要求フレームを構築（互換性維持用）
    /// </summary>
    /// <param name="deviceCode">デバイスコード</param>
    /// <param name="startDeviceNumber">開始デバイス番号</param>
    /// <param name="readCount">読み出し点数</param>
    /// <param name="frameType">フレームタイプ（3E/4E）</param>
    /// <param name="timeout">監視タイマ（250ms単位）</param>
    /// <returns>送信用バイト配列</returns>
    public static byte[] BuildReadRequest(
        DeviceCode deviceCode,
        int startDeviceNumber,
        ushort readCount,
        string frameType = "3E",
        ushort timeout = 32)
    {
        var frame = new List<byte>();

        // ヘッダ部（ReadRandomと同じ）
        if (frameType == "3E")
        {
            frame.AddRange(new byte[] { 0x50, 0x00 });
        }
        else if (frameType == "4E")
        {
            frame.AddRange(new byte[] { 0x54, 0x00, 0x00, 0x00, 0x00, 0x00 });
        }

        frame.Add(0x00);
        frame.Add(0xFF);
        frame.AddRange(BitConverter.GetBytes((ushort)0x03FF));
        frame.Add(0x00);

        // データ長（固定10バイト）
        frame.AddRange(BitConverter.GetBytes((ushort)10));

        // 監視タイマ
        frame.AddRange(BitConverter.GetBytes(timeout));

        // コマンド: 0x0401 (Read)
        frame.AddRange(BitConverter.GetBytes((ushort)0x0401));

        // サブコマンド: 0x0000（ワード単位）
        frame.AddRange(BitConverter.GetBytes((ushort)0x0000));

        // デバイスコード
        frame.Add((byte)deviceCode);

        // 開始デバイス番号（3バイト、LE）
        frame.Add((byte)(startDeviceNumber & 0xFF));
        frame.Add((byte)((startDeviceNumber >> 8) & 0xFF));
        frame.Add((byte)((startDeviceNumber >> 16) & 0xFF));

        // 読み出し点数（2バイト、LE）
        frame.AddRange(BitConverter.GetBytes(readCount));

        return frame.ToArray();
    }
}
```

---

#### Phase 4: 設定ファイルからのデバイスリスト構築（優先度：高）

**ファイル**: `andon/Core/Managers/ConfigToFrameManager.cs`

```csharp
using Andon.Core.Constants;
using Andon.Core.Models;
using Andon.Utilities;

namespace Andon.Core.Managers;

/// <summary>
/// Step1-2: 設定読み込み・フレーム構築
/// </summary>
public class ConfigToFrameManager
{
    /// <summary>
    /// 設定からReadRandomフレームを構築
    /// </summary>
    /// <param name="config">設定データ</param>
    /// <returns>送信用バイト配列</returns>
    public byte[] BuildReadRandomFrameFromConfig(TargetDeviceConfig config)
    {
        // 設定からデバイスリストを構築
        var devices = new List<DeviceSpecification>();

        // 設定ファイルの各デバイス定義を解析
        // （TODO: 実際の設定構造に応じて実装）
        foreach (var deviceEntry in config.Devices)
        {
            var deviceCode = ParseDeviceCode(deviceEntry.DeviceType);
            var deviceNumber = deviceEntry.DeviceNumber;
            var isHex = deviceCode.IsHexAddress();

            devices.Add(new DeviceSpecification(deviceCode, deviceNumber, isHex));
        }

        // フレーム構築
        return SlmpFrameBuilder.BuildReadRandomRequest(
            devices,
            frameType: config.FrameVersion,
            timeout: (ushort)(config.Timeout / 250)  // msを250ms単位に変換
        );
    }

    /// <summary>
    /// デバイス種別文字列をDeviceCodeに変換
    /// </summary>
    private DeviceCode ParseDeviceCode(string deviceType)
    {
        return deviceType.ToUpper() switch
        {
            "D" => DeviceCode.D,
            "M" => DeviceCode.M,
            "W" => DeviceCode.W,
            "X" => DeviceCode.X,
            "Y" => DeviceCode.Y,
            "B" => DeviceCode.B,
            "ZR" => DeviceCode.ZR,
            "R" => DeviceCode.R,
            _ => throw new ArgumentException($"未対応のデバイス種別: {deviceType}")
        };
    }
}
```

---

## 4. テスト実装計画

### 4.1 単体テスト（Unit Tests）

**ファイル**: `andon/Tests/Unit/Utilities/SlmpFrameBuilderTests.cs`（新規作成）

```csharp
using Xunit;
using Andon.Utilities;
using Andon.Core.Constants;
using Andon.Core.Models;

namespace Andon.Tests.Unit.Utilities;

public class SlmpFrameBuilderTests
{
    [Fact]
    public void BuildReadRandomRequest_ValidDevices_ReturnsCorrectFrame()
    {
        // Arrange
        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100),     // D100
            new DeviceSpecification(DeviceCode.D, 105),     // D105
            new DeviceSpecification(DeviceCode.M, 200)      // M200
        };

        // Act
        var frame = SlmpFrameBuilder.BuildReadRandomRequest(devices, "3E", timeout: 32);

        // Assert
        Assert.NotNull(frame);
        Assert.True(frame.Length > 0);

        // サブヘッダ確認（3Eフレーム）
        Assert.Equal(0x50, frame[0]);
        Assert.Equal(0x00, frame[1]);

        // コマンド確認（0x0403 = ReadRandom）
        Assert.Equal(0x03, frame[15]);
        Assert.Equal(0x04, frame[16]);

        // ワード点数確認（3点）
        Assert.Equal(3, frame[19]);
    }

    [Fact]
    public void DeviceSpecification_ToDeviceSpecificationBytes_D100_ReturnsCorrectBytes()
    {
        // Arrange
        var device = new DeviceSpecification(DeviceCode.D, 100);

        // Act
        var bytes = device.ToDeviceSpecificationBytes();

        // Assert
        Assert.Equal(4, bytes.Length);
        Assert.Equal(0x64, bytes[0]);   // 100 = 0x64（下位）
        Assert.Equal(0x00, bytes[1]);   // 中位
        Assert.Equal(0x00, bytes[2]);   // 上位
        Assert.Equal(0xA8, bytes[3]);   // Dデバイスコード
    }

    [Fact]
    public void DeviceSpecification_FromHexString_W11AA_ReturnsCorrectDevice()
    {
        // Arrange & Act
        var device = DeviceSpecification.FromHexString(DeviceCode.W, "11AA");

        // Assert
        Assert.Equal(DeviceCode.W, device.Code);
        Assert.Equal(0x11AA, device.DeviceNumber);
        Assert.True(device.IsHexAddress);

        var bytes = device.ToDeviceSpecificationBytes();
        Assert.Equal(0xAA, bytes[0]);   // 下位
        Assert.Equal(0x11, bytes[1]);   // 中位
        Assert.Equal(0x00, bytes[2]);   // 上位
        Assert.Equal(0xB4, bytes[3]);   // Wデバイスコード
    }

    [Theory]
    [InlineData(DeviceCode.X, true)]
    [InlineData(DeviceCode.Y, true)]
    [InlineData(DeviceCode.W, true)]
    [InlineData(DeviceCode.D, false)]
    [InlineData(DeviceCode.M, false)]
    public void DeviceCodeExtensions_IsHexAddress_ReturnsExpectedValue(DeviceCode code, bool expected)
    {
        // Act
        var result = code.IsHexAddress();

        // Assert
        Assert.Equal(expected, result);
    }
}
```

---

### 4.2 統合テスト（Integration Tests）

**ファイル**: `andon/Tests/Integration/ReadRandomIntegrationTests.cs`（新規作成）

```csharp
using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Constants;
using Andon.Utilities;

namespace Andon.Tests.Integration;

public class ReadRandomIntegrationTests
{
    [Fact]
    public void BuildAndParseReadRandomFrame_RoundTrip_Success()
    {
        // Arrange
        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 61000),  // conmoni_test相当
            new DeviceSpecification(DeviceCode.D, 61003),
            new DeviceSpecification(DeviceCode.D, 61010)
        };

        // Act - フレーム構築
        var frame = SlmpFrameBuilder.BuildReadRandomRequest(devices, "3E", timeout: 32);

        // Assert - フレーム検証
        Assert.NotNull(frame);

        // サブヘッダ
        Assert.Equal(0x50, frame[0]);
        Assert.Equal(0x00, frame[1]);

        // ネットワーク番号・局番
        Assert.Equal(0x00, frame[2]);
        Assert.Equal(0xFF, frame[3]);

        // コマンド（0x0403）
        ushort command = (ushort)(frame[15] | (frame[16] << 8));
        Assert.Equal(0x0403, command);

        // ワード点数
        Assert.Equal(3, frame[19]);

        // デバイス指定部（D61000）
        Assert.Equal(0x48, frame[21]);  // 61000 = 0xEE48 → 下位0x48
        Assert.Equal(0xEE, frame[22]);  // 中位0xEE
        Assert.Equal(0x00, frame[23]);  // 上位0x00
        Assert.Equal(0xA8, frame[24]);  // Dデバイスコード
    }

    [Fact(Skip = "実機テスト用（PLC接続環境が必要）")]
    public async Task ReadRandom_RealDevice_Success()
    {
        // このテストはPLC実機環境でのみ実行
        // （実装は実機テスト時に追加）
    }
}
```

---

## 5. 移行手順

### 5.1 段階的移行ロードマップ

#### Step 1: 基礎実装（1週間） ✅ **完了 (2025-11-14)**
- [x] DeviceCode列挙型の実装
- [x] DeviceCodeExtensionsの実装
  - [x] IsHexAddress()メソッド
  - [x] IsBitDevice()メソッド
  - [x] IsReadRandomSupported()メソッド
- [x] DeviceSpecificationクラスの実装
  - [x] ToDeviceNumberBytes()メソッド
  - [x] ToDeviceSpecificationBytes()メソッド
  - [x] FromHexString()静的メソッド
  - [x] ValidateForReadRandom()メソッド
  - [x] Equals()とGetHashCode()オーバーライド
- [x] 単体テスト作成・実行（78テスト、100%成功）

**成果物**: デバイス定義の型安全な実装 ✅
**詳細**: `Phase1_DeviceCode_DeviceSpecification_TestResults.md`

#### Step 2: フレーム構築実装（1週間） ✅ **完了 (2025-11-14)**
- [x] SlmpFrameBuilder.BuildReadRandomRequestの実装（134行）
  - [x] 3E/4Eフレーム両対応
  - [x] ヘッダ部構築
  - [x] コマンド部構築（0x0403 + サブコマンド）
  - [x] デバイス指定部構築（4バイト×点数）
  - [x] 入力検証（空リスト、上限超過、不正フレームタイプ）
- [x] データ長自動計算の実装
  - [x] 3Eフレーム用計算ロジック
  - [x] 4Eフレーム用計算ロジック
- [x] 統合テスト作成・実行（21テスト、100%成功）
  - [x] conmoni_test互換性テスト（213バイト完全一致）
  - [x] 異常系テスト
  - [x] データ長動的計算テスト

**成果物**: ReadRandomフレーム構築機能 ✅
**詳細**: `Phase2_SlmpFrameBuilder_TestResults.md`

#### Step 3: 設定統合（1週間）
- [ ] ConfigToFrameManagerの実装
- [ ] 設定ファイルからのデバイスリスト構築
- [ ] エンドツーエンドテスト

**成果物**: 設定ファイルからのフレーム自動構築

#### Step 4: 実機テスト（1週間）
- [ ] テストプログラム作成（PlcRealDeviceTest相当）
- [ ] 実機でのReadRandomテスト
- [ ] パフォーマンス測定
- [ ] バグ修正

**成果物**: 実機動作確認済み実装

#### Step 5: 既存コード置き換え（1週間）
- [ ] 既存Read(0x0401)コードの特定
- [ ] ReadRandom(0x0403)への段階的置き換え
- [ ] 回帰テスト
- [ ] ドキュメント更新

**成果物**: 完全移行

**進捗サマリー**:
- ✅ Step 1完了: 78テスト全パス
- ✅ Step 2完了: 21テスト全パス、conmoni_test互換性確認
- 🔄 Step 3以降: 未着手
- **累計**: 99テスト全パス、2/5ステップ完了（40%）

---

### 5.2 リスク管理

| リスク | 影響 | 対策 |
|--------|------|------|
| **PLC互換性問題** | 高 | ・実機テストフェーズを設ける<br>・旧Read(0x0401)との並行運用 |
| **フレーム構築バグ** | 高 | ・単体テスト網羅率90%以上<br>・既知の正解データ（conmoni_test）との比較検証 |
| **パフォーマンス劣化** | 中 | ・ベンチマークテストの実施<br>・最適化実装（バイト配列操作の効率化） |
| **設定ファイル互換性** | 中 | ・既存設定の解析<br>・移行ツールの提供 |
| **デバイスコード不足** | 低 | ・必要に応じてDeviceCodeに追加 |

---

## 6. コード例: conmoni_testとの対応関係

### 6.1 conmoni_testのハードコードデータ

```csharp
// conmoni_test/PlcSingleTest.cs
private static readonly int[] SEND_DATA = new int[]
{
    84,0,0,0,0,0,0,255,255,3,0,200,0,32,0,  // ヘッダ部
    3,4,0,0,48,0,                            // コマンド部
    72,238,0,168,  // D61000
    75,238,0,168,  // D61003
    // ...
};
```

### 6.2 andon実装での等価コード

```csharp
// andon/Utilities/SlmpFrameBuilder.cs
var devices = new List<DeviceSpecification>
{
    new DeviceSpecification(DeviceCode.D, 61000),  // D61000
    new DeviceSpecification(DeviceCode.D, 61003),  // D61003
    // ...
};

var frame = SlmpFrameBuilder.BuildReadRandomRequest(devices, "3E", timeout: 32);

// frame配列はconmoni_testのSEND_DATAと同じ内容になる
```

**検証コード**:
```csharp
[Fact]
public void BuildReadRandomRequest_MatchesConMoniTestData()
{
    // conmoni_testの既知データ
    byte[] expected = new byte[]
    {
        0x54, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  // ヘッダ
        0xFF, 0xFF, 0x03, 0x00,
        0xC8, 0x00,  // データ長200バイト
        0x20, 0x00,  // タイマ32
        0x03, 0x04,  // ReadRandomコマンド
        0x00, 0x00,  // サブコマンド
        0x30, 0x00,  // 48点
        0x48, 0xEE, 0x00, 0xA8,  // D61000
        // ...
    };

    // andon実装で構築
    var devices = new List<DeviceSpecification>
    {
        new DeviceSpecification(DeviceCode.D, 61000),
        // ... (48点分)
    };
    var actual = SlmpFrameBuilder.BuildReadRandomRequest(devices, "3E", timeout: 32);

    // バイト単位比較
    Assert.Equal(expected.Length, actual.Length);
    for (int i = 0; i < expected.Length; i++)
    {
        Assert.Equal(expected[i], actual[i]);
    }
}
```

---

## 7. まとめ

### 7.1 技術的変更点

| 項目 | 旧Read(0x0401) | 新ReadRandom(0x0403) |
|------|---------------|---------------------|
| **コマンドコード** | 0x01 0x04 | 0x03 0x04 |
| **デバイス指定** | 連続デバイスのみ | 飛び飛びOK |
| **フレーム長** | 固定（ヘッダ+10バイト） | 可変（ヘッダ+6+4×点数） |
| **通信回数** | デバイス群ごとに複数回 | 1回で完結 |
| **実装複雑度** | 低 | 中 |
| **効率** | 低（連続デバイス以外） | 高 |

### 7.2 実装の優先順位

1. **最優先**: DeviceCode定義・DeviceSpecificationクラス
2. **高**: BuildReadRandomRequestメソッド
3. **中**: ConfigToFrameManager統合
4. **低**: 旧コードの完全置き換え

### 7.3 参考リソース

- **conmoni_testコード**: `C:\Users\1010821\Desktop\python\andon\conmoni_test\PlcSingleTest.cs`
- **ConMoniフレーム構築**: `GenerateSettingJson.py` (Python実装)
- **既存パース処理**: `andon/Utilities/SlmpDataParser.cs`
- **SLMP仕様**: `documents/design/フレーム構築方法.md`

---

## 8. 実装チェックリスト（段階的変更手順）

### 【フェーズ1: 基礎定義の追加】（既存コードへの追加のみ） ✅ **完了 (2025-11-14)**

#### ステップ1: DeviceCode列挙型の追加 ✅
- [x] `andon/Core/Constants/DeviceConstants.cs`を新規作成
- [x] DeviceCode列挙型を定義（SM, X, Y, M, D, W, R, ZR等）
- [x] 変化点: デバイスコードがハードコード数値から型安全な列挙型に

#### ステップ2: DeviceCodeExtensions拡張メソッドの追加 ✅
- [x] DeviceCodeExtensionsクラスを同ファイル内に作成
- [x] IsHexAddress()メソッドを実装（X, Y, W等の16進デバイス判定）
- [x] IsBitDevice()メソッドを実装（M, X, Y等のビットデバイス判定）
- [x] IsReadRandomSupported()メソッドを実装（ReadRandom対応判定）
- [x] 変化点: デバイス種別判定ロジックが一元化

#### ステップ3: DeviceSpecificationクラスの追加 ✅
- [x] `andon/Core/Models/DeviceSpecification.cs`を新規作成
- [x] プロパティ定義（Code, DeviceNumber, IsHexAddress）
- [x] ToDeviceNumberBytes()メソッド実装（3バイトLE変換）
- [x] ToDeviceSpecificationBytes()メソッド実装（4バイト変換）
- [x] FromHexString()静的メソッド実装（16進文字列対応）
- [x] ToString()オーバーライド（デバッグ用）
- [x] Equals()とGetHashCode()オーバーライド（コレクション対応）
- [x] ValidateForReadRandom()メソッド実装（事前検証）
- [x] ValidateDeviceNumberRange()メソッド実装（範囲検証）
- [x] 変化点: デバイス指定情報がオブジェクト化、バイト変換ロジックをカプセル化

#### ステップ4: 基礎定義の単体テスト作成 ✅
- [x] `andon/Tests/Unit/Core/Constants/DeviceConstantsTests.cs`を新規作成
- [x] DeviceCodeExtensions.IsHexAddress()のテスト（10テスト）
- [x] DeviceCodeExtensions.IsBitDevice()のテスト（17テスト）
- [x] DeviceCodeExtensions.IsReadRandomSupported()のテスト（11テスト）
- [x] 複合条件テスト（5テスト）、SLMP仕様書準拠テスト（7テスト）
- [x] `andon/Tests/Unit/Core/Models/DeviceSpecificationTests.cs`を新規作成
- [x] ToDeviceNumberBytes()のテスト（D100, D61000等）
- [x] FromHexString()のテスト（W0x11AA等）
- [x] conmoni_test統合テスト（3テスト）
- [x] テスト実行・全パス確認（78テスト、100%成功率）
- [x] conmoni_testとの完全互換性検証完了

**完了条件**: 全単体テストがパスし、型安全なデバイス定義が使用可能 ✅

**実績**:
- 実装クラス: 2クラス（DeviceCode+Extensions、DeviceSpecification）
- テスト数: 78テスト（DeviceConstantsTests 50、DeviceSpecificationTests 28）
- 成功率: 100% (78/78)
- conmoni_test互換性: 100%（バイト単位完全一致）
- 詳細レポート: `documents/design/read_random実装/Phase1_DeviceCode_DeviceSpecification_TestResults.md`

---

### 【フェーズ2: フレーム構築機能の追加】（既存コードへの追加のみ） ✅ **完了 (2025-11-14)**

#### ステップ5: ReadRandomフレーム構築メソッドの実装 ✅
- [x] `andon/Utilities/SlmpFrameBuilder.cs`を開く（現在は空実装）
- [x] BuildReadRandomRequest()静的メソッドを実装
  - [x] ヘッダ部構築（3E/4E対応）
  - [x] コマンド部構築（0x0403 + サブコマンド）
  - [x] ワード点数・Dword点数設定
  - [x] デバイス指定部構築（4バイト×点数）
  - [x] データ長自動計算・確定
- [x] 変化点: 空だったクラスにReadRandom(0x0403)フレーム構築ロジックを追加

#### ステップ6: 旧Readフレーム構築メソッドの実装（互換性維持用）
- [ ] BuildReadRequest()静的メソッドを実装（0x0401用）
- [ ] 変化点: 旧Read(0x0401)フレーム構築も実装（段階的移行のため）
- ⚠️ **Note**: Phase3以降で必要に応じて実装（現時点では不要）

#### ステップ7: フレーム構築の単体テスト作成 ✅
- [x] `andon/Tests/Unit/Utilities/SlmpFrameBuilderTests.cs`を新規作成
- [x] BuildReadRandomRequest()の基本テスト
  - [x] ヘッダ検証（3Eフレーム: 0x50 0x00、4Eフレーム: 0x54 0x00）
  - [x] コマンド検証（0x03 0x04）
  - [x] ワード点数検証
  - [x] デバイス指定バイト検証（D100, D61000, W0x0118AA等）
- [x] conmoni_testとの互換性テスト
  - [x] 48デバイスのフレーム構築テスト（213バイト）
  - [x] バイト配列完全一致テスト（D61000, W0x0118AA）
  - [x] データ長自動計算テスト（1, 10, 48, 100デバイス）
- [x] 異常系テスト
  - [x] 空デバイスリスト
  - [x] null デバイスリスト
  - [x] 256デバイス（上限超過）
  - [x] 未対応フレームタイプ
- [x] タイムアウト設定テスト（1, 32, 120, 240）
- [x] テスト実行・全パス確認（21テスト、100%成功率）

**完了条件**: conmoni_testと同一のフレームバイト配列が構築可能 ✅

**実績**:
- 実装メソッド: BuildReadRandomRequest() (134行)
- テスト数: 21テスト（全パス、100%成功率）
- conmoni_test互換性: ✅ 213バイトフレーム構築成功
- 3E/4Eフレーム両対応: ✅
- データ長自動計算: ✅
- TDD手法適用: ✅ Red→Green→Refactorサイクル完遂

---

### 【フェーズ3: 設定読み込み統合】（既存コードの拡張） ⚠️ **後回し**

> **注**: このフェーズはPlcCommunicationManagerへの統合後に実施予定。
> 現在はフェーズ4（通信マネージャーの修正）を優先。

#### ステップ8: ConfigToFrameManagerの実装
- [ ] `andon/Core/Managers/ConfigToFrameManager.cs`を開く（現在は空実装）
- [ ] BuildReadRandomFrameFromConfig()メソッドを実装
  - [ ] 設定からデバイスリストを構築
  - [ ] SlmpFrameBuilder.BuildReadRandomRequest()を呼び出し
- [ ] ParseDeviceCode()メソッドを実装
  - [ ] 文字列（"D", "M"等）をDeviceCodeに変換
- [ ] 変化点: 設定ファイルからReadRandomフレーム自動構築が可能に

#### ステップ9: TargetDeviceConfigモデルの拡張
- [ ] `andon/Core/Models/ConfigModels/TargetDeviceConfig.cs`を開く
- [ ] Devicesリストプロパティを追加
- [ ] DeviceEntryクラスを追加（DeviceType, DeviceNumber）
- [ ] 変化点: 設定ファイルでデバイスリスト指定が可能に

#### ステップ10: 設定読み込みのテスト作成
- [ ] `andon/Tests/Unit/Core/Managers/ConfigToFrameManagerTests.cs`を新規作成
- [ ] BuildReadRandomFrameFromConfig()のテスト
- [ ] ParseDeviceCode()のテスト
- [ ] テスト実行・パス確認

**完了条件**: 設定ファイルからReadRandomフレームが自動構築可能

---

### 【フェーズ4: 通信マネージャーの修正】（既存コードの変更開始） 🔄 **進行中**

#### ステップ11: ReadRandomフレーム送受信テストの実装 ✅ **完了 (2025-11-14)**
- [x] `andon/Tests/TestUtilities/Mocks/MockPlcServer.cs`にSetM000ToM999ReadResponse()を実装
  - [x] memo.md実データ(111バイト)から正確な4Eフレーム応答データを構築
  - [x] バリデーション機能追加(222文字=111バイト検証)
  - [x] デバッグ出力機能追加
- [x] TC021テスト: ReadRandom送信フレームテスト実装
  - [x] `TC021_SendFrameAsync_ReadRandom_正常送信_213バイト` - PASSED
  - [x] 送信フレーム長検証: 213バイト(426文字)
  - [x] SlmpFrameBuilder.BuildReadRandomRequest()統合
- [x] TC025テスト: ReadRandom受信フレームテスト実装
  - [x] `TC025_ReceiveResponseAsync_ReadRandom_正常受信_111バイト` - PASSED
  - [x] 受信フレーム長検証: 111バイト(222文字)
  - [x] 4Eフレーム構造解析検証(ヘッダ15バイト + デバイスデータ96バイト)
- [x] TC021_TC025統合テスト実装
  - [x] `TC021_TC025統合_ReadRandom送受信_正常動作` - PASSED
  - [x] 送信→受信の一連フロー検証
  - [x] MockPlcServerとの統合動作確認
- [x] 全テスト実行・全パス確認(Exit code 0)

**実績**:
- 修正ファイル: MockPlcServer.cs (SetM000ToM999ReadResponse)
- テスト数: 3テスト(TC021, TC025, 統合) - 全PASSED
- フレーム検証: 送信213バイト、受信111バイト - 両方正確
- memo.md実データ互換性: ✅ 完全一致

**変化点**:
- **変更前**: MockPlcServerに応答データなし、ReadRandomテスト未実装
- **変更後**: ReadRandom(0x0403)の送受信テストが完全動作

#### ステップ12: PlcCommunicationManagerのフレーム構築呼び出し変更
- [ ] `andon/Core/Managers/PlcCommunicationManager.cs`を開く
- [ ] ハードコードされたフレームバイト配列を特定
- [ ] ConfigToFrameManager.BuildReadRandomFrameFromConfig()の呼び出しに置き換え
- [ ] 変化点:
  - **変更前**: ハードコードされたバイト配列
  - **変更後**: ビルダーパターンで動的構築

#### ステップ13: データ取得ループの変更
- [ ] `andon/Core/Managers/PlcCommunicationManager.cs`または`ExecutionOrchestrator.cs`を開く
- [ ] 複数回のRead(0x0401)ループを特定
- [ ] 1回のReadRandom(0x0403)呼び出しに変更
- [ ] 変化点:
  - **変更前**: 複数回通信ループ
  - **変更後**: 1回の通信で完結

**完了条件**: 通信マネージャーがReadRandomを使用してフレーム送信可能 (ステップ11完了、12-13進行中)

---

### 【フェーズ5: レスポンス処理の修正】（既存コードの変更）

#### ステップ14: ReadRandomレスポンスパーサーの追加
- [ ] `andon/Utilities/SlmpDataParser.cs`を開く
- [ ] ParseReadRandomResponse()メソッドを追加
  - [ ] デバイス指定順にデータを抽出
  - [ ] デバイス番号とデータ値のマッピング作成
- [ ] 変化点:
  - **変更前**: Read(0x0401)の連続データのみ対応
  - **変更後**: ReadRandom(0x0403)の不連続データにも対応

#### ステップ15: ProcessedResponseDataの構造拡張
- [ ] `andon/Core/Models/ProcessedResponseData.cs`を開く
- [ ] DeviceValueMapプロパティを追加（Dictionary<DeviceSpecification, object>）
- [ ] 既存の連続デバイス形式は互換性のため残す
- [ ] 変化点:
  - **変更前**: 連続したデバイス番号範囲で管理
  - **変更後**: デバイス指定リストとデータ値のマッピングで管理

#### ステップ16: レスポンス処理のテスト作成
- [ ] `andon/Tests/Unit/Utilities/SlmpDataParserTests.cs`を開く
- [ ] ParseReadRandomResponse()のテスト追加
- [ ] 不連続デバイスのパーステスト
- [ ] テスト実行・パス確認

**完了条件**: ReadRandomレスポンスが正しくパース可能

---

### 【フェーズ6: 設定ファイル構造の変更】（設定の変更） ⚠️ **後回し**

> **注**: このフェーズはPlcCommunicationManagerへの統合とレスポンス処理実装後に実施予定。
> 現在はフェーズ4（通信マネージャーの修正）とフェーズ5（レスポンス処理）を優先。

#### ステップ17: appsettings.jsonの更新
- [ ] `appsettings.json`を開く
- [ ] デバイス指定方式を変更
  - **変更前**: `"StartDevice": "D100", "DeviceCount": 10`
  - **変更後**: `"Devices": [{"Type": "D", "Number": 100}, ...]`
- [ ] 既存設定は`_old`として残す（ロールバック用）
- [ ] 変化点: 範囲指定からリスト指定に変更

#### ステップ18: ConfigurationLoaderの修正
- [ ] `andon/Infrastructure/Configuration/ConfigurationLoader.cs`を開く
- [ ] 新しいDevicesリスト形式の読み込みロジックを追加
- [ ] 旧形式（StartDevice/DeviceCount）のフォールバック処理を追加
- [ ] 変化点: 両形式に対応（後方互換性維持）

#### ステップ19: 設定ファイルのバリデーションテスト
- [ ] `andon/Tests/Unit/Infrastructure/Configuration/ConfigurationLoaderTests.cs`を更新
- [ ] 新形式の設定読み込みテスト
- [ ] 旧形式のフォールバックテスト
- [ ] テスト実行・パス確認

**完了条件**: 新旧両方の設定形式が読み込み可能

---

### 【フェーズ7: データ出力処理の修正】（既存コードの変更） ⚠️ **後回し**

> **注**: このフェーズはPlcCommunicationManagerへの統合とレスポンス処理実装後に実施予定。
> 現在はフェーズ4（通信マネージャーの修正）とフェーズ5（レスポンス処理）を優先。

#### ステップ20: DataOutputManagerの出力形式変更
- [ ] `andon/Core/Managers/DataOutputManager.cs`を開く
- [ ] CSV出力ロジックを修正
  - **変更前**: 連続したデバイス値を出力（D100, D101, D102...）
  - **変更後**: 指定したデバイスのみ出力（D100, D105, M200...）
- [ ] ヘッダー行の動的生成（デバイス指定リストから）
- [ ] 変化点: 不連続デバイスに対応した出力形式

#### ステップ21: LoggingManagerのログフォーマット変更
- [ ] `andon/Core/Managers/LoggingManager.cs`を開く
- [ ] ログメッセージを更新
  - **変更前**: "Read 10 devices from D100"
  - **変更後**: "ReadRandom 3 devices: D100, D105, M200"
- [ ] 変化点: ReadRandom使用を明示したログ

#### ステップ22: データ出力のテスト更新
- [ ] `andon/Tests/Unit/Core/Managers/DataOutputManagerTests.cs`を更新
- [ ] 不連続デバイス出力のテスト追加
- [ ] テスト実行・パス確認

**完了条件**: 不連続デバイスのデータが正しく出力可能

---

### 【フェーズ8: 統合テストの追加・修正】（テストの追加）

#### ステップ23: ReadRandom統合テストの作成
- [ ] `andon/Tests/Integration/ReadRandomIntegrationTests.cs`を新規作成
- [ ] フレーム構築→送信→レスポンスパースの一連テスト
- [ ] conmoni_testとのバイト配列互換性テスト
- [ ] テスト実行・パス確認

#### ステップ24: 既存統合テストの修正
- [ ] `andon/Tests/Integration/Step1_2_IntegrationTests.cs`を開く
- [ ] Read(0x0401)前提のテストをReadRandom(0x0403)用に更新
- [ ] `andon/Tests/Integration/Step3_6_IntegrationTests.cs`を開く
- [ ] エンドツーエンドテストの更新
- [ ] テスト実行・パス確認

#### ステップ25: エラーハンドリング統合テストの更新
- [ ] `andon/Tests/Integration/ErrorHandling_IntegrationTests.cs`を開く
- [ ] ReadRandom用のエラーケーステスト追加
  - [ ] 点数上限超過（192点以上）
  - [ ] 無効なデバイスコード
  - [ ] 制約違反デバイス（TS, TC等）
- [ ] テスト実行・パス確認

**完了条件**: 全統合テストがパス

---

### 【フェーズ9: 実機テスト】（実機環境でのテスト）

#### ステップ26: 実機テストプログラムの作成
- [ ] `PlcRealDeviceTest/Program.cs`を開く
- [ ] ReadRandom(0x0403)を使用したテストコードを追加
- [ ] conmoni_testと同じデバイス指定でテスト
- [ ] 変化点: 実機での動作確認

#### ステップ27: 実機での動作確認
- [ ] PLC実機環境でテスト実行
- [ ] フレーム送信確認
- [ ] レスポンス受信確認
- [ ] データ値の正確性確認
- [ ] パフォーマンス測定（通信時間）
- [ ] エラーケースのテスト

#### ステップ28: バグ修正・調整
- [ ] 実機テストで発見された問題を修正
- [ ] 再テスト・パス確認

**完了条件**: 実機環境でReadRandomが正常動作

---

### 【フェーズ10: 旧コードの削除・クリーンアップ】（既存コードの削除）

#### ステップ29: Read(0x0401)専用コードの削除判断
- [ ] プロジェクトチームで方針決定
  - [ ] **選択肢A**: 完全削除（ReadRandom(0x0403)のみに統一）
  - [ ] **選択肢B**: 残す（互換性維持、設定で切り替え可能）

#### ステップ30（選択肢A選択時）: 旧コードの完全削除
- [ ] BuildReadRequest()メソッドの削除
- [ ] Read(0x0401)用テストコードの削除
- [ ] 旧形式設定ファイルサポートの削除
- [ ] ハードコードされたバイト配列の削除
- [ ] 全テスト実行・パス確認

#### ステップ31（選択肢B選択時）: 切り替え機能の実装
- [ ] appsettings.jsonに`UseReadRandom`フラグを追加
- [ ] 実行時にRead(0x0401)とReadRandom(0x0403)を切り替え可能に
- [ ] 両方式のテスト維持
- [ ] 全テスト実行・パス確認

**完了条件**: プロジェクト方針に基づいてクリーンアップ完了

---

### 【フェーズ11: ドキュメント更新】

#### ステップ32: 設計書の更新
- [ ] `documents/design/クラス設計.md`を更新
- [ ] `documents/design/フレーム構築方法.md`を更新
- [ ] `documents/design/各ステップio.md`を更新

#### ステップ33: 実装記録の作成
- [ ] `documents/implementation_records/method_records/`に記録作成
  - [ ] BuildReadRandomRequest実装記録
  - [ ] DeviceSpecification実装記録
  - [ ] 各フェーズの判断根拠記録
- [ ] `documents/implementation_records/progress_notes/`に日次記録作成

#### ステップ34: README・運用ガイドの更新
- [ ] プロジェクトREADMEの更新（新機能説明）
- [ ] 設定ファイルサンプルの更新
- [ ] トラブルシューティングガイドの追加

**完了条件**: 全ドキュメントが最新状態

---

## 9. 進捗管理

### 全体進捗
- [x] **フェーズ1: 基礎定義の追加（ステップ1-4）** ✅ 完了 (2025-11-14)
  - 実装: DeviceCode列挙型、DeviceCodeExtensions、DeviceSpecification
  - テスト: 78テスト全パス（100%成功率）
  - レポート: `Phase1_DeviceCode_DeviceSpecification_TestResults.md`
- [x] **フェーズ2: フレーム構築機能の追加（ステップ5-7）** ✅ 完了 (2025-11-14)
  - 実装: SlmpFrameBuilder.BuildReadRandomRequest()（134行）
  - テスト: 21テスト全パス（100%成功率）
  - conmoni_test互換性: 213バイトフレーム完全一致
  - レポート: `Phase2_SlmpFrameBuilder_TestResults.md`
- [ ] フェーズ3: 設定読み込み統合（ステップ8-10） ⚠️ **後回し**
- [x] **フェーズ4: 通信マネージャーの修正（ステップ11-13）** 🔄 **ステップ11完了 (2025-11-14)**
  - ステップ11完了: MockPlcServer応答データ実装、TC021/TC025/統合テスト全PASSED
  - 実装: SetM000ToM999ReadResponse() (222文字=111バイト)
  - テスト: TC021(送信213バイト), TC025(受信111バイト), 統合テスト - 全PASSED
  - memo.md実データ互換性: ✅ 完全一致
  - ステップ12-13: 未着手（PlcCommunicationManagerの実装統合）
- [ ] フェーズ5: レスポンス処理の修正（ステップ14-16）
- [ ] フェーズ6: 設定ファイル構造の変更（ステップ17-19）
- [ ] フェーズ7: データ出力処理の修正（ステップ20-22）
- [ ] フェーズ8: 統合テストの追加・修正（ステップ23-25）
- [ ] フェーズ9: 実機テスト（ステップ26-28）
- [ ] フェーズ10: 旧コードの削除・クリーンアップ（ステップ29-31）
- [ ] フェーズ11: ドキュメント更新（ステップ32-34）

### 各フェーズの完了条件
1. **フェーズ1完了**: 全単体テストがパス、型安全なデバイス定義が使用可能 ✅ **達成**
2. **フェーズ2完了**: conmoni_testと同一のフレームバイト配列が構築可能 ✅ **達成**
3. **フェーズ3完了**: 設定ファイルからReadRandomフレームが自動構築可能
4. **フェーズ4完了**: 通信マネージャーがReadRandomを使用してフレーム送信可能
5. **フェーズ5完了**: ReadRandomレスポンスが正しくパース可能
6. **フェーズ6完了**: 新旧両方の設定形式が読み込み可能
7. **フェーズ7完了**: 不連続デバイスのデータが正しく出力可能
8. **フェーズ8完了**: 全統合テストがパス
9. **フェーズ9完了**: 実機環境でReadRandomが正常動作
10. **フェーズ10完了**: プロジェクト方針に基づいてクリーンアップ完了
11. **フェーズ11完了**: 全ドキュメントが最新状態

### 推定所要時間と実績
- フェーズ1: 2日（推定） → ✅ **完了** (2025-11-14)
- フェーズ2: 2日（推定） → ✅ **完了** (2025-11-14)
- フェーズ3: 2日（推定） → ⚠️ **後回し** (Phase4完了後に実施)
- フェーズ4: 1日（推定） → 🔄 **進行中** (ステップ11完了 2025-11-14、残りステップ12-13)
- フェーズ5: 2日（推定）
- フェーズ6: 1日（推定） → ⚠️ **後回し** (Phase4/5完了後に実施)
- フェーズ7: 1日（推定） → ⚠️ **後回し** (Phase4/5完了後に実施)
- フェーズ8: 2日（推定）
- フェーズ9: 2日（推定、実機環境依存）
- フェーズ10: 1日（推定）
- フェーズ11: 1日（推定）

**合計**: 約17日（3.5週間）
**進捗**: 2.33/11フェーズ完了（21%）、実績2.33フェーズ/推定4.5日分
**今回の作業**: Phase4ステップ11完了 - ReadRandom送受信テスト実装、全テストPASSED

---

**作成日**: 2025-11-14
**作成者**: Claude Code
**承認**: （実装前にレビュー必須）
