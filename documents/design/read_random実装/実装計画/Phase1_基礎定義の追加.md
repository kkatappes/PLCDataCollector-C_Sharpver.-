# Phase1: 基礎定義の追加（既存コードへの追加のみ）

## ステータス
✅ **完了** (2025-11-14)

## 概要
ReadRandom(0x0403)コマンド実装の基礎となるデバイス定義とデータ構造を追加。既存コードへの影響なし。

**主要な設計変更点**:
- **通信回数の最小化**: 2回送受信 → 1回送受信（全デバイスを単一ReadRandomフレームで一括取得）
- **処理の簡素化**: MergeResponseData()メソッド削除、BasicProcessedResponseData型削除、ProcessReceivedRawDataで処理完結
- **型設計の明確化**: デバイス名キー構造（例: "M000", "D000", "D002"）、DWordDeviceCountはOriginalRequestから算出
- **ビットデバイス対応の簡素化**: 16点=1ワード換算ロジックが不要に、ビット・ワード・ダブルワード混在指定が可能

## 実装対象

### 1. DeviceCode列挙型の実装
**ファイル**: `andon/Core/Constants/DeviceConstants.cs`（新規作成）

```csharp
namespace Andon.Core.Constants;

/// <summary>
/// SLMPデバイスコード定義
/// ReadRandom(0x0403)ではビット・ワード・ダブルワード混在指定が可能
/// </summary>
public enum DeviceCode : byte
{
    // ビットデバイス
    // 【重要】ReadRandomで指定した場合、開始番号から強制的に16点が読み取られる
    // 例: M10を指定 → M10～M25の16点が取得される（データ長: 2バイト = 1ワード）
    SM = 0x91,   // 特殊リレー
    X = 0x9C,    // 入力
    Y = 0x9D,    // 出力
    M = 0x90,    // 内部リレー
    L = 0x92,    // ラッチリレー
    F = 0x93,    // アナンシエータ
    B = 0xA0,    // リンクリレー

    // ワードデバイス（1点 = 2バイト）
    SD = 0xA9,   // 特殊レジスタ
    D = 0xA8,    // データレジスタ
    W = 0xB4,    // リンクレジスタ
    R = 0xAF,    // ファイルレジスタ
    ZR = 0xB0,   // ファイルレジスタ（拡張）

    // タイマー
    TN = 0xC2,   // タイマ現在値
    TS = 0xC1,   // タイマ接点（ReadRandom非対応）
    TC = 0xC0,   // タイマコイル（ReadRandom非対応）

    // カウンタ
    CN = 0xC5,   // カウンタ現在値
    CS = 0xC4,   // カウンタ接点（ReadRandom非対応）
    CC = 0xC3,   // カウンタコイル（ReadRandom非対応）
}
```

### 2. DeviceCodeExtensions拡張メソッドの実装
**ファイル**: `andon/Core/Constants/DeviceConstants.cs`（同一ファイル内）

```csharp
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
        DeviceCode.X, DeviceCode.Y, DeviceCode.B,
        DeviceCode.W, DeviceCode.ZR
    };

    /// <summary>
    /// デバイスコードが16進アドレス表記かを判定
    /// </summary>
    public static bool IsHexAddress(this DeviceCode code)
        => HexAddressDevices.Contains(code);

    /// <summary>
    /// ビット型デバイスかを判定
    /// </summary>
    private static readonly HashSet<DeviceCode> BitDevices = new()
    {
        DeviceCode.SM, DeviceCode.X, DeviceCode.Y,
        DeviceCode.M, DeviceCode.L, DeviceCode.F, DeviceCode.B,
        DeviceCode.TS, DeviceCode.TC, DeviceCode.CS, DeviceCode.CC
    };

    public static bool IsBitDevice(this DeviceCode code)
        => BitDevices.Contains(code);

    /// <summary>
    /// ReadRandom対応デバイスかを判定
    /// （SLMP仕様書page_64.png制約事項準拠）
    /// </summary>
    private static readonly HashSet<DeviceCode> ReadRandomUnsupported = new()
    {
        DeviceCode.TS, DeviceCode.TC,  // タイマ接点/コイル
        DeviceCode.CS, DeviceCode.CC   // カウンタ接点/コイル
    };

    public static bool IsReadRandomSupported(this DeviceCode code)
        => !ReadRandomUnsupported.Contains(code);
}
```

### 3. DeviceSpecificationクラスの実装
**ファイル**: `andon/Core/Models/DeviceSpecification.cs`（新規作成）

```csharp
namespace Andon.Core.Models;

/// <summary>
/// デバイス指定情報（ReadRandom用）
/// 単一のReadRandomフレームで全デバイスを一括取得するための基本単位
/// 取得後はデバイス名キー（例: "M000", "D000", "D002"）でマッピング
/// </summary>
public class DeviceSpecification
{
    public DeviceCode Code { get; set; }
    public int DeviceNumber { get; set; }
    public bool IsHexAddress { get; set; }

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
        Array.Copy(deviceNumberBytes, 0, result, 0, 3);
        result[3] = (byte)Code;
        return result;
    }

    /// <summary>
    /// ReadRandom用バリデーション
    /// </summary>
    public void ValidateForReadRandom()
    {
        if (!Code.IsReadRandomSupported())
            throw new ArgumentException(
                $"デバイス{Code}はReadRandomコマンドで使用できません（SLMP仕様書制約）");

        ValidateDeviceNumberRange();
    }

    private void ValidateDeviceNumberRange()
    {
        if (DeviceNumber < 0 || DeviceNumber > 0xFFFFFF)
            throw new ArgumentOutOfRangeException(
                nameof(DeviceNumber),
                $"デバイス番号は0～16777215の範囲で指定してください: {DeviceNumber}");
    }

    public override string ToString()
    {
        return IsHexAddress ? $"{Code}0x{DeviceNumber:X}" : $"{Code}{DeviceNumber}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not DeviceSpecification other) return false;
        return Code == other.Code && DeviceNumber == other.DeviceNumber;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Code, DeviceNumber);
    }
}
```

## テスト実装

### 1. DeviceConstantsTests
**ファイル**: `andon/Tests/Unit/Core/Constants/DeviceConstantsTests.cs`（新規作成）

**テスト項目**:
- IsHexAddress() テスト（10テスト）
- IsBitDevice() テスト（17テスト）
- IsReadRandomSupported() テスト（11テスト）
- 複合条件テスト（5テスト）
- SLMP仕様書準拠テスト（7テスト）

**合計**: 50テスト

### 2. DeviceSpecificationTests
**ファイル**: `andon/Tests/Unit/Core/Models/DeviceSpecificationTests.cs`（新規作成）

**テスト項目**:
- ToDeviceNumberBytes() テスト（D100, D61000等、8テスト）
- ToDeviceSpecificationBytes() テスト（4バイト変換、5テスト）
- FromHexString() テスト（W0x11AA等、3テスト）
- ValidateForReadRandom() テスト（6テスト）
- Equals/GetHashCode() テスト（3テスト）
- conmoni_test統合テスト（3テスト）

**合計**: 28テスト

## 完了条件
- [x] DeviceCode列挙型の実装
- [x] DeviceCodeExtensions拡張メソッドの実装
- [x] DeviceSpecificationクラスの実装
- [x] 単体テスト作成・実行（78テスト全パス、100%成功率）
- [x] conmoni_testとの互換性検証完了

## 実績
- **実装クラス**: 2クラス
  - DeviceCode + DeviceCodeExtensions
  - DeviceSpecification
- **テスト数**: 78テスト（全パス、100%成功率）
- **conmoni_test互換性**: 100%（バイト単位完全一致）
- **詳細レポート**: `Phase1_DeviceCode_DeviceSpecification_TestResults.md`

## 変化点
- **変更前**: デバイスコードがハードコード数値
- **変更後**: 型安全な列挙型とオブジェクト指向実装

## 重要な設計判断

### 1. ReadRandom通信方式の採用理由
**対象**: 全体設計

**判断根拠**:
- **旧方式**: Read(0x0401)を複数回実行（ビットデバイス用1回+ワードデバイス用1回）
- **新方式**: ReadRandom(0x0403)を1回実行（全デバイスを単一フレームで一括取得）
- **効果**: 通信回数削減、応答データ結合処理(MergeResponseData)が不要に、型構造が簡素化

**4Eフレーム構造** (CLAUDE.md準拠):
```
| Idx | 長さ | 名称 | 内容(例) |
|-----|------|------|----------|
| 0 | 2 | サブヘッダ | 54 00（4E相当、固定） |
| 2 | 2 | シリアル | 00 00（予約） |
| 4 | 2 | 予約 | 00 00 |
| 6 | 1 | ネットワーク番号 | 00 |
| 7 | 1 | 局番 | FF（全局） |
| 8 | 2 | I/O番号 | FF 03（LE） |
| 10 | 1 | マルチドロップ | 00 |
| 11 | 2 | データ長 | 48 00（72バイト、LE） |
| 13 | 2 | 監視タイマ | 20 00（32=8秒、LE） |
| 15 | 2 | コマンド | 03 04（ランダム読出、LE） |
| 17 | 2 | サブコマンド | 00 00 |
| 19 | 1 | ワード点数 | 10（16点） |
| 20 | 1 | Dword点数 | 00（0点） |
| 21- | 4×点数 | デバイス指定 | [デバイス番号3byte(LE), デバイスコード1byte] |
```

### 2. リトルエンディアン変換ロジック
**実装箇所**: `DeviceSpecification.ToDeviceNumberBytes()`

**判断根拠**:
- SLMP仕様書でデバイス番号は3バイトリトルエンディアン形式
- BitConverter.GetBytes()を使わずビットシフトで実装
- 理由: 3バイト変換はBitConverterが標準サポートしていないため

**例**:
```
D61000 (0xEE48) → [0x48, 0xEE, 0x00]
```

### 3. 16進デバイスの扱い
**実装箇所**: `DeviceSpecification.FromHexString()`

**判断根拠**:
- X, Y, W, B, ZRデバイスは16進表記が標準
- 内部では10進整数として統一管理
- 16進文字列→整数変換はConvert.ToInt32(hexString, 16)を使用

**例**:
```
W"11AA" → DeviceNumber = 4522 (10進)
```

### 4. ReadRandom制約事項の実装
**実装箇所**: `DeviceCodeExtensions.IsReadRandomSupported()`

**判断根拠**:
- SLMP仕様書page_64.pngの制約事項に準拠
- タイマ/カウンタの接点・コイルはReadRandom不可
- ValidateForReadRandom()で事前検証

### 6. ビットデバイスの読み取り動作仕様
**対象**: ReadRandomコマンドにおけるビットデバイスの取り扱い

**重要な仕様**:
1. **強制16点読み取り**:
   - ビットデバイスを指定した場合、指定した開始番号から**強制的に16点**が読み取られる
   - 例: M10を指定 → M10～M25の16点が取得される
   - データ長: 16点 = 2バイト（1ワード）

2. **要求フレームの点数カウント**:
   - ワード点数フィールド（19バイト目）: ワードデバイス + ビットデバイスの合計点数
   - ビットデバイス1指定 = 1ワード相当としてカウント
   - 例: ワード3点 + ビット2指定 = ワード点数フィールドに「5」を指定

3. **応答データの構造**:
   - ビットデバイス: 2バイト（16ビット）で返される
   - ワードデバイス: 2バイト
   - Dwordデバイス: 4バイト（Dword点数フィールドに別途カウント）

4. **実装への影響**:
   - フレーム構築時: ビットデバイスをワード点数にカウント
   - 応答パース時: ビットデバイスは2バイト読み取り、16ビット分解析
   - データマッピング: 指定したビットデバイス1つにつき16点分のデータを保持

### 5. デバイス名キー構造の採用
**対象**: 受信データのマッピング

**判断根拠**:
- ReadRandom応答は指定順にデータが返される
- デバイス名キー（例: "M000", "D000", "D002"）でDictionary化することで、データアクセスが簡素化
- DWordDeviceCountは送信時のOriginalRequestから算出可能
- 従来のBasicProcessedResponseData型（開始番号+点数）は不要に

## 参考資料
- SLMP仕様書: `documents/design/pdf2img/page_63.png`, `page_64.png`
- フレーム構築方法: `documents/design/フレーム構築関係/フレーム構築方法.md` (CLAUDE.md準拠)
- conmoni_testコード: `conmoni_test/PlcSingleTest.cs`
- 既存パース処理: `andon/Utilities/SlmpDataParser.cs`

---

## 設計変更サマリー

### 通信方式の変更
| 項目 | 旧方式 (Read 0x0401) | 新方式 (ReadRandom 0x0403) |
|------|---------------------|---------------------------|
| **通信回数** | 2回（ビット用+ワード用） | 1回（全デバイス一括） |
| **フレーム構築** | 連続デバイス指定 | 不連続デバイス個別指定 |
| **応答処理** | MergeResponseData()で結合 | ProcessReceivedRawDataで完結 |
| **型構造** | BasicProcessedResponseData使用 | デバイス名キー(Dictionary)使用 |

### データ構造の変更
| 項目 | 旧方式 | 新方式 |
|------|--------|--------|
| **デバイス指定** | 開始番号+点数 | List<DeviceSpecification> |
| **応答データ** | 連続バイト配列 | Dictionary<"M000", value> |
| **DWord情報** | 個別プロパティ | OriginalRequestから算出 |
| **ビットデバイス** | 16点=1ワード換算必要 | 個別指定可能、換算不要 |

### 削除される処理
- ❌ `MergeResponseData()`メソッド
- ❌ `BasicProcessedResponseData`型
- ❌ ビットデバイスの16点=1ワード換算ロジック
- ❌ 複数回通信のループ処理

### 追加される処理
- ✅ `DeviceSpecification`クラス（デバイス指定情報）
- ✅ デバイス名キー生成（"M000", "D000"等）
- ✅ ReadRandomフレーム構築（SlmpFrameBuilder）
- ✅ 単一フレームでの一括取得処理

---

**作成日**: 2025-11-14
**更新日**: 2025-11-21 (ビットデバイスの読み取り動作仕様を明記)
**ステータス**: ✅ 完了
**次フェーズ**: Phase2 - フレーム構築機能の追加
