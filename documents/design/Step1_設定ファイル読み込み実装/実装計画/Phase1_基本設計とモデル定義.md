# Step1 実装計画 - Phase1: 基本設計とモデル定義

## Phase1の目的

Step1の基盤となるモデルクラスと定数クラスを実装し、後続フェーズの土台を構築する。

---

## 実装対象クラス

### 1. PlcConfiguration（モデルクラス）

**ファイルパス**: `andon/Core/Models/ConfigModels/PlcConfiguration.cs`（新規作成）

**目的**: Excelから読み込んだPLC設定情報を保持

**プロパティ**:
```csharp
public class PlcConfiguration
{
    // Excel "settings"シートから読み込み
    public string IpAddress { get; set; } = string.Empty;           // B8
    public int Port { get; set; }                                    // B9
    public int DataReadingFrequency { get; set; }                    // B11
    public string PlcModel { get; set; } = string.Empty;             // B12
    public string SavePath { get; set; } = string.Empty;             // B13

    // メタ情報
    public string SourceExcelFile { get; set; } = string.Empty;
    public string ConfigurationName => Path.GetFileNameWithoutExtension(SourceExcelFile);

    // Excel "データ収集デバイス"シートから読み込み
    public List<DeviceSpecification> Devices { get; set; } = new();
}
```

### 2. DeviceSpecification（モデルクラス）

**ファイルパス**: `andon/Core/Models/DeviceSpecification.cs`（既存）

**目的**: Excel1行分のデバイス情報を保持

**プロパティ**:
```csharp
public class DeviceSpecification
{
    // Excel "データ収集デバイス"シートから読み込み
    public string ItemName { get; set; } = string.Empty;    // A列: 項目名
    public string DeviceType { get; set; } = string.Empty;  // B列: デバイスコード（M, D, X等）
    public int DeviceNumber { get; set; }                   // C列: デバイス番号（10進）
    public int Digits { get; set; }                         // D列: 桁数
    public string Unit { get; set; } = string.Empty;        // E列: 単位（bit, word, dword）

    // 正規化処理で追加される情報
    public byte DeviceCode { get; set; }                    // SLMPデバイスコード（0x90, 0xA8等）
    public byte[] DeviceBytes { get; set; } = Array.Empty<byte>(); // 3バイトLE表現
    public bool IsHexDevice { get; set; }                   // 16進デバイスフラグ
    public bool IsBitDevice { get; set; }                   // ビットデバイスフラグ
}
```

### 3. DeviceCodeMap（定数・変換クラス）

**ファイルパス**: `andon/Core/Constants/DeviceConstants.cs`（既存、追加実装）

**目的**: 24種類のデバイスコード変換と判定機能を提供

**実装内容**:
```csharp
public static class DeviceCodeMap
{
    private static readonly Dictionary<string, DeviceInfo> _deviceMap = new()
    {
        // ビットデバイス（10進）
        { "SM", new DeviceInfo(0x91, false, true) },  // 特殊リレー
        { "M",  new DeviceInfo(0x90, false, true) },  // 内部リレー
        { "L",  new DeviceInfo(0x92, false, true) },  // ラッチリレー
        { "F",  new DeviceInfo(0x93, false, true) },  // アナンシエータ
        { "V",  new DeviceInfo(0x94, false, true) },  // エッジリレー
        { "TS", new DeviceInfo(0xC1, false, true) },  // タイマ接点
        { "TC", new DeviceInfo(0xC0, false, true) },  // タイマコイル
        { "STS", new DeviceInfo(0xC7, false, true) }, // 積算タイマ接点
        { "STC", new DeviceInfo(0xC6, false, true) }, // 積算タイマコイル
        { "CS", new DeviceInfo(0xC4, false, true) },  // カウンタ接点
        { "CC", new DeviceInfo(0xC3, false, true) },  // カウンタコイル

        // ビットデバイス（16進）
        { "X",  new DeviceInfo(0x9C, true, true) },   // 入力
        { "Y",  new DeviceInfo(0x9D, true, true) },   // 出力
        { "B",  new DeviceInfo(0xA0, true, true) },   // リンクリレー
        { "SB", new DeviceInfo(0xA1, true, true) },   // リンク特殊リレー
        { "DX", new DeviceInfo(0xA2, true, true) },   // ダイレクト入力
        { "DY", new DeviceInfo(0xA3, true, true) },   // ダイレクト出力

        // ワードデバイス（10進）
        { "SD", new DeviceInfo(0xA9, false, false) }, // 特殊レジスタ
        { "D",  new DeviceInfo(0xA8, false, false) }, // データレジスタ
        { "W",  new DeviceInfo(0xB4, false, false) }, // リンクレジスタ
        { "SW", new DeviceInfo(0xB5, false, false) }, // リンク特殊レジスタ
        { "TN", new DeviceInfo(0xC2, false, false) }, // タイマ現在値
        { "STN", new DeviceInfo(0xC8, false, false) },// 積算タイマ現在値
        { "CN", new DeviceInfo(0xC5, false, false) }, // カウンタ現在値
        { "Z",  new DeviceInfo(0xCC, false, false) }, // インデックスレジスタ
        { "R",  new DeviceInfo(0xAF, false, false) }, // ファイルレジスタ
        { "ZR", new DeviceInfo(0xB0, false, false) }  // ファイルレジスタ
    };

    public static byte GetDeviceCode(string deviceType)
    {
        if (!_deviceMap.TryGetValue(deviceType.ToUpper(), out var info))
            throw new ArgumentException($"未対応のデバイスタイプ: {deviceType}");
        return info.Code;
    }

    public static bool IsHexDevice(string deviceType)
        => _deviceMap.TryGetValue(deviceType.ToUpper(), out var info) && info.IsHex;

    public static bool IsBitDevice(string deviceType)
        => _deviceMap.TryGetValue(deviceType.ToUpper(), out var info) && info.IsBit;

    public static bool IsValidDeviceType(string deviceType)
        => _deviceMap.ContainsKey(deviceType.ToUpper());

    private record DeviceInfo(byte Code, bool IsHex, bool IsBit);
}
```

### 4. SlmpFixedSettings（定数クラス）

**ファイルパス**: `andon/Core/Constants/SlmpConstants.cs`（既存、追加実装）

**目的**: SLMP通信の固定設定値を提供（memo.md送信フレーム準拠）

**実装内容**:
```csharp
public static class SlmpFixedSettings
{
    // フレーム設定
    public const string FrameVersion = "4E";
    public const string Protocol = "UDP";

    // 通信対象設定（memo.mdフレームから抽出）
    public const byte NetworkNumber = 0x00;
    public const byte StationNumber = 0xFF;
    public const ushort IoNumber = 0x03FF;
    public const byte MultiDropStation = 0x00;

    // タイムアウト設定
    public const ushort MonitorTimer = 0x0020;      // 32 = 8秒
    public const int ReceiveTimeoutMs = 500;

    // コマンド設定
    public const ushort Command = 0x0403;           // ReadRandom
    public const ushort SubCommand = 0x0000;

    // サブヘッダ
    public static readonly byte[] SubHeader_4E = { 0x54, 0x00 };
    public static readonly byte[] Serial = { 0x00, 0x00 };
    public static readonly byte[] Reserved = { 0x00, 0x00 };

    /// <summary>
    /// 固定設定でフレームヘッダを構築
    /// </summary>
    public static byte[] BuildFrameHeader(int dataLength)
    {
        var header = new List<byte>();

        // サブヘッダ (0-1)
        header.AddRange(SubHeader_4E);

        // シリアル (2-3)、予約 (4-5)
        header.AddRange(Serial);
        header.AddRange(Reserved);

        // ネットワーク番号 (6)
        header.Add(NetworkNumber);

        // 局番 (7)
        header.Add(StationNumber);

        // I/O番号 (8-9)
        header.AddRange(BitConverter.GetBytes(IoNumber));

        // マルチドロップ (10)
        header.Add(MultiDropStation);

        // データ長 (11-12)
        header.AddRange(BitConverter.GetBytes((ushort)dataLength));

        // 監視タイマ (13-14)
        header.AddRange(BitConverter.GetBytes(MonitorTimer));

        // コマンド (15-16)
        header.AddRange(BitConverter.GetBytes(Command));

        // サブコマンド (17-18)
        header.AddRange(BitConverter.GetBytes(SubCommand));

        return header.ToArray();
    }
}
```

---

## Excel設定ファイルフォーマット（参考）

### シート1: "settings"

| セル | 設定項目             | 例               |
|------|---------------------|------------------|
| B8   | PLCのIPアドレス      | 172.30.40.15     |
| B9   | PLCのポート          | 8192             |
| B11  | データ取得周期(ms)   | 1000             |
| B12  | デバイス名(PLC識別)  | ライン1-炉A      |
| B13  | データ保存先パス     | C:\data\output   |

### シート2: "データ収集デバイス"

| A列(項目名) | B列(デバイスコード) | C列(デバイス番号) | D列(桁数) | E列(単位) |
|-------------|---------------------|-------------------|-----------|-----------|
| 温度1       | D                   | 60000             | 1         | word      |
| 温度2       | D                   | 60075             | 1         | word      |
| 圧力        | D                   | 60082             | 1         | word      |
| 状態        | M                   | 32                | 1         | bit       |

---

## Phase1の成功条件

- ✅ PlcConfigurationクラスが定義され、必要なプロパティが全て含まれていること
- ✅ DeviceSpecificationクラスが定義され、Excel列と正規化後のプロパティが全て含まれていること
- ✅ DeviceCodeMapクラスで24種類のデバイスコード変換ができること
- ✅ DeviceCodeMapで10進/16進デバイスの判定ができること
- ✅ DeviceCodeMapでビット/ワードデバイスの判定ができること
- ✅ SlmpFixedSettingsで固定通信パラメータが全て定義されていること
- ✅ SlmpFixedSettings.BuildFrameHeader()でフレームヘッダが構築できること

---

## Phase1のテスト計画

### DeviceCodeMapのテスト

1. **デバイスコード取得テスト**
   - 全24種類のデバイスで正しいコードを返すこと
   - 未対応デバイスでArgumentExceptionをスローすること

2. **デバイス判定テスト**
   - 10進デバイス（M, D, W等）でIsHexDevice() == falseとなること
   - 16進デバイス（X, Y, B等）でIsHexDevice() == trueとなること
   - ビットデバイスでIsBitDevice() == trueとなること
   - ワードデバイスでIsBitDevice() == falseとなること

3. **検証テスト**
   - 有効なデバイスタイプでIsValidDeviceType() == trueとなること
   - 無効なデバイスタイプでIsValidDeviceType() == falseとなること

### SlmpFixedSettingsのテスト

1. **固定値確認テスト**
   - FrameVersion == "4E"
   - Protocol == "UDP"
   - NetworkNumber == 0x00
   - StationNumber == 0xFF
   - IoNumber == 0x03FF
   - MonitorTimer == 0x0020
   - Command == 0x0403

2. **フレームヘッダ構築テスト**
   - BuildFrameHeader(72)で正しいヘッダが生成されること
   - ヘッダ長が19バイトであること
   - データ長フィールド（11-12バイト目）が正しく設定されること

---

## Phase1の実装手順

1. **PlcConfigurationクラス作成**
   - ファイル作成: `andon/Core/Models/ConfigModels/PlcConfiguration.cs`
   - プロパティ定義
   - ConfigurationName計算プロパティ実装

2. **DeviceSpecificationクラス確認・更新**
   - 既存ファイル確認: `andon/Core/Models/DeviceSpecification.cs`
   - 不足プロパティ追加（DeviceBytes, IsHexDevice, IsBitDevice等）

3. **DeviceCodeMap実装**
   - ファイル更新: `andon/Core/Constants/DeviceConstants.cs`
   - 24種類のデバイスマップ定義
   - 各種判定メソッド実装

4. **SlmpFixedSettings実装**
   - ファイル更新: `andon/Core/Constants/SlmpConstants.cs`
   - 固定パラメータ定義
   - BuildFrameHeader()メソッド実装

5. **単体テスト作成**
   - `Tests/Unit/Core/Constants/DeviceConstantsTests.cs` - DeviceCodeMapテスト
   - `Tests/Unit/Core/Constants/SlmpConstantsTests.cs` - SlmpFixedSettingsテスト

6. **テスト実行・検証**
   - 全テストがパスすることを確認

---

## Phase1完了後の状態

- 基本的なモデルクラスが定義され、他のPhaseで使用可能になる
- デバイスコード変換ロジックが利用可能になる
- 固定通信パラメータが定義され、フレーム構築の準備が整う
- Phase2（Excel読み込み基礎機能）の実装に進む準備が完了

---

## 次のPhase

**Phase2: Excel読み込み基礎機能**
- EPPlusライブラリ導入
- Excelファイル検索機能実装
- "settings"シート読み込み実装
- "データ収集デバイス"シート読み込み実装（基礎部分）

---

## Phase1実装完了報告

**実装完了日**: 2025-11-27

### 実装結果サマリー

✅ **実装完了**: DeviceCodeMap、SlmpFixedSettings
✅ **テスト完了**: 89テスト全合格（DeviceCodeMap: 65、SlmpFixedSettings: 24）
✅ **実装方式**: TDD (Red-Green-Refactor)

### 実装内容

**DeviceCodeMap** (`Core/Constants/DeviceConstants.cs`):
- 27種類全SLMPデバイスタイプ対応
- GetDeviceCode(): デバイスタイプ文字列→デバイスコード変換
- IsHexDevice(): 16進/10進アドレス表記判定
- IsBitDevice(): ビット/ワード型デバイス判定
- IsValidDeviceType(): デバイスタイプ有効性検証

**SlmpFixedSettings** (`Core/Constants/SlmpConstants.cs`):
- memo.md送信フレーム仕様準拠の固定パラメータ定義
- BuildFrameHeader(): 19バイトSLMPフレームヘッダ構築
- 全パラメータのリトルエンディアン対応

### テスト結果

```
テスト合計: 89
成功: 89
失敗: 0
成功率: 100%
```

**詳細結果**: `実装結果/Phase1_基本設計とモデル定義_TestResults.txt` 参照

### Phase1で実装しなかった項目

以下は意図的にPhase2以降で実装:
- ❌ PlcConfigurationクラス → Phase2で実装予定
- ❌ DeviceEntryクラス → Phase2で実装予定

理由: これらのクラスはExcel読み込み機能と密接に関連しているため、Phase2（Excel読み込み基礎機能）で一緒に実装する方が効率的と判断。

### Phase2への準備状況

✅ **準備完了**: デバイスコード変換機能が利用可能
✅ **準備完了**: 固定通信パラメータが定義済み
✅ **準備完了**: Phase2の実装に必要な基盤が整備済み
