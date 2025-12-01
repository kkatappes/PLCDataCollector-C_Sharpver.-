# Step2: フレーム構築実装フロー

## 概要
andonプロジェクトにおけるStep2（通信フレーム構築）の実装済みフローを記載する。

---

## 実装クラス
- `ConfigToFrameManager` (andon/Core/Managers/ConfigToFrameManager.cs:19-53)
- `SlmpFrameBuilder` (andon/Utilities/SlmpFrameBuilder.cs:18-131)

---

## 処理フロー

```
┌───────────────────────────────────────────┐
│ BuildReadRandomFrameFromConfig()          │
│ (ConfigToFrameManager.cs:19-53)           │
├───────────────────────────────────────────┤
│ 1. null チェック                           │
│    if (config == null)                    │
│        throw ArgumentNullException        │
│                                            │
│ 2. デバイスリスト検証                      │
│    if (Devices == null || Count == 0)     │
│        throw ArgumentException            │
│                                            │
│ 3. フレームタイプ検証                      │
│    if (FrameType != "3E" && != "4E")      │
│        throw ArgumentException            │
│                                            │
│ 4. DeviceEntry → DeviceSpecification 変換 │
│    var deviceSpecifications =             │
│        config.Devices                     │
│            .Select(d => d.ToDeviceSpecification()) │
│            .ToList()                      │
│    ※ DWord対応の場合はここで分割処理実行  │
│                                            │
│ 5. フレーム構築処理呼び出し                │
│    byte[] frame =                         │
│        SlmpFrameBuilder.BuildReadRandomRequest( │
│            deviceSpecifications,          │
│            config.FrameType,              │
│            config.Timeout                 │
│        )                                  │
│                                            │
│ 6. フレーム返却                            │
│    return frame                           │
└───────────────────────────────────────────┘
        ↓
┌───────────────────────────────────────────┐
│ BuildReadRandomRequest()                  │
│ (SlmpFrameBuilder.cs:18-131)              │
├───────────────────────────────────────────┤
│ 1. 入力検証                                │
│    - デバイスリスト存在確認                │
│    - デバイス点数上限チェック (255点以下)  │
│    - フレームタイプ検証 ("3E"/"4E")       │
│                                            │
│ 2. ヘッダ部構築                            │
│    【3Eフレーム】                          │
│    - サブヘッダ: 0x50 0x00               │
│                                            │
│    【4Eフレーム】                          │
│    - サブヘッダ: 0x54 0x00               │
│    - シーケンス番号: 0x00 0x00            │
│      (TODO: 自動インクリメント未実装)     │
│    - 予約: 0x00 0x00                      │
│                                            │
│    【共通部分】                            │
│    - ネットワーク番号: 0x00               │
│    - 局番: 0xFF (自局)                    │
│    - I/O番号: 0xFF 0x03 (LE: 0x03FF)     │
│    - マルチドロップ局番: 0x00             │
│    - データ長: 仮値 (後で確定)            │
│    - 監視タイマ: timeout (LE、250ms単位)  │
│                                            │
│ 3. コマンド部構築                          │
│    - コマンド: 0x03 0x04 (LE: 0x0403)    │
│      ReadRandom: ランダム読出しコマンド    │
│    - サブコマンド: 0x00 0x00 (LE)        │
│      ワード単位読み出し                    │
│    - ワード点数: devices.Count (1バイト)  │
│    - Dword点数: 0x00 (未対応)             │
│                                            │
│ 4. デバイス指定部構築                      │
│    foreach (var device in devices)        │
│    {                                       │
│        // 4バイトデバイス指定:             │
│        // [デバイス番号3バイト(LE),        │
│        //  デバイスコード1バイト]          │
│        frame.AddRange(                     │
│            device.ToDeviceSpecificationBytes() │
│        )                                   │
│    }                                       │
│    例: D100 → 64 00 00 A8                │
│        M10  → 0A 00 00 90                 │
│                                            │
│ 5. データ長確定                            │
│    【3Eフレーム】                          │
│    dataLength = frame.Count               │
│                 - headerSize(2)           │
│                 - fixedSize(9)            │
│    = コマンド部以降のバイト数              │
│                                            │
│    【4Eフレーム】                          │
│    dataLength = frame.Count               │
│                 - headerSize(6)           │
│                 - fixedSize(7)            │
│    = 監視タイマ以降のバイト数              │
│                                            │
│    リトルエンディアンで格納:               │
│    frame[dataLengthPosition] =            │
│        (byte)(dataLength & 0xFF)          │
│    frame[dataLengthPosition + 1] =        │
│        (byte)((dataLength >> 8) & 0xFF)   │
│                                            │
│ 6. フレーム返却                            │
│    return frame.ToArray()                 │
└───────────────────────────────────────────┘
```

---

## 入力

### TargetDeviceConfig (Step1の出力)
```csharp
public class TargetDeviceConfig
{
    public string FrameType { get; set; }         // "3E" or "4E"
    public ushort Timeout { get; set; }           // 250ms単位 (例: 32 = 8秒)
    public List<DeviceEntry> Devices { get; set; } // デバイスリスト
}
```

---

## 中間データ

### DeviceSpecification リスト
```csharp
public class DeviceSpecification
{
    public DeviceCode DeviceCode { get; set; }  // デバイスコード列挙型
    public uint Address { get; set; }           // デバイスアドレス
    public ushort Type { get; set; }            // ワード=0, Dword=1
}
```

### DeviceEntry → DeviceSpecification 変換

```
DeviceEntry (設定ファイル読み込み用)
├─ DeviceName: string ("D", "M", "W", etc.)
├─ StartAddress: int (開始アドレス)
└─ EndAddress: int (終了アドレス)

    ↓ ToDeviceSpecification() 呼び出し
    ↓ (範囲を個別アドレスに展開)

List<DeviceSpecification> (フレーム構築用)
├─ [0] DeviceCode: D (0xA8), Address: 100, Type: 0
├─ [1] DeviceCode: D (0xA8), Address: 101, Type: 0
├─ [2] DeviceCode: D (0xA8), Address: 102, Type: 0
└─ ...
```

**例**: `DeviceEntry { DeviceName="D", StartAddress=100, EndAddress=102 }`
- → `DeviceSpecification { DeviceCode=D, Address=100, Type=0 }`
- → `DeviceSpecification { DeviceCode=D, Address=101, Type=0 }`
- → `DeviceSpecification { DeviceCode=D, Address=102, Type=0 }`

---

## 出力

### byte[] (SLMPフレーム)

ReadRandom(0x0403)コマンドの送信フレーム

---

## 送信フレーム例

### 3Eフレーム (D100, D200, D300 読み出し)
```
50 00                # サブヘッダ (3E)
00                   # ネットワーク番号
FF                   # 局番
FF 03                # I/O番号 (LE)
00                   # マルチドロップ局番
10 00                # データ長 (16バイト、LE)
20 00                # 監視タイマ (32 = 8秒、LE)
03 04                # コマンド (0x0403 ReadRandom、LE)
00 00                # サブコマンド (ワード単位、LE)
03                   # ワード点数 (3点)
00                   # Dword点数 (0点)
64 00 00 A8          # D100 (0x000064, デバイスコード 0xA8)
C8 00 00 A8          # D200 (0x0000C8, デバイスコード 0xA8)
2C 01 00 A8          # D300 (0x00012C, デバイスコード 0xA8)
```

**フレーム長**: 21バイト
**データ長**: 16バイト (監視タイマ以降のバイト数を除く)

### 4Eフレーム (同上)
```
54 00                # サブヘッダ (4E)
00 00                # シーケンス番号
00 00                # 予約
00                   # ネットワーク番号
FF                   # 局番
FF 03                # I/O番号 (LE)
00                   # マルチドロップ局番
10 00                # データ長 (16バイト、LE)
20 00                # 監視タイマ (32 = 8秒、LE)
03 04                # コマンド (0x0403 ReadRandom、LE)
00 00                # サブコマンド (ワード単位、LE)
03                   # ワード点数 (3点)
00                   # Dword点数 (0点)
64 00 00 A8          # D100
C8 00 00 A8          # D200
2C 01 00 A8          # D300
```

**フレーム長**: 25バイト
**データ長**: 16バイト (監視タイマ以降のバイト数)

---

## データ変換の詳細

### デバイスコード変換例

| デバイス名 | DeviceCode列挙値 | バイト値 | 説明 |
|-----------|-----------------|---------|------|
| D         | DeviceCode.D    | 0xA8    | データレジスタ |
| M         | DeviceCode.M    | 0x90    | 内部リレー |
| W         | DeviceCode.W    | 0xB4    | リンクレジスタ |
| X         | DeviceCode.X    | 0x9C    | 入力デバイス |
| Y         | DeviceCode.Y    | 0x9D    | 出力デバイス |
| B         | DeviceCode.B    | 0xA0    | リンクリレー |
| L         | DeviceCode.L    | 0x92    | ラッチリレー |

### バイト配列生成 (ToDeviceSpecificationBytes)

```
DeviceSpecification: D100
├─ DeviceCode: 0xA8
└─ Address: 100 (0x000064)

    ↓ ToDeviceSpecificationBytes()

byte[4]: { 0x64, 0x00, 0x00, 0xA8 }
         └─────────────┘  └──┘
         Address (LE, 3バイト) DeviceCode (1バイト)
```

**リトルエンディアン変換**:
- Address: 100 (10進数)
  - → 0x000064 (16進数)
  - → `{ 0x64, 0x00, 0x00 }` (LE)

---

## 成功条件

- ✅ Step1で取得した値から正しい内容のフレームを生成できること
- ✅ 3E/4E両フレームタイプに対応すること
- ✅ SLMP仕様書に準拠したバイト配列を生成すること
- ✅ デバイス番号がリトルエンディアンで正しく格納されること
- ✅ データ長が動的に計算され正しく設定されること
- ✅ 不正な入力に対してArgumentExceptionをスローすること

---

## エラーハンドリング

### ConfigToFrameManager.BuildReadRandomFrameFromConfig()
| エラー条件 | 例外 | メッセージ |
|-----------|------|-----------|
| config が null | ArgumentNullException | "Value cannot be null. (Parameter 'config')" |
| Devices が null または空 | ArgumentException | "デバイスリストが空です" |
| FrameType が "3E"/"4E" 以外 | ArgumentException | "未対応のフレームタイプ: {frameType}" |

### SlmpFrameBuilder.BuildReadRandomRequest()
| エラー条件 | 例外 | メッセージ |
|-----------|------|-----------|
| devices が null または空 | ArgumentException | "デバイスリストが空です" |
| デバイス点数 > 255 | ArgumentException | "デバイス点数が上限を超えています: {count}点（最大255点）" |
| frameType が "3E"/"4E" 以外 | ArgumentException | "未対応のフレームタイプ: {frameType}" |

---

## フレーム構造の詳細

### 3Eフレーム構造

| オフセット | 長さ | 項目 | 値 (例) | 説明 |
|-----------|------|------|---------|------|
| 0 | 2 | サブヘッダ | 0x50 0x00 | 3Eフレーム識別子 |
| 2 | 1 | ネットワーク番号 | 0x00 | 自ネットワーク |
| 3 | 1 | 局番 | 0xFF | 自局 |
| 4 | 2 | I/O番号 | 0xFF 0x03 (LE) | 要求先ユニット |
| 6 | 1 | マルチドロップ局番 | 0x00 | 未使用 |
| 7 | 2 | データ長 | 可変 (LE) | コマンド部以降のバイト数 |
| 9 | 2 | 監視タイマ | 可変 (LE) | タイムアウト時間(250ms単位) |
| 11 | 2 | コマンド | 0x03 0x04 (LE) | ReadRandom |
| 13 | 2 | サブコマンド | 0x00 0x00 (LE) | ワード単位 |
| 15 | 1 | ワード点数 | 可変 | 読み出しワード数 |
| 16 | 1 | Dword点数 | 0x00 | 未対応 |
| 17 | n×4 | デバイス指定 | 可変 | 各4バイト (アドレス3+コード1) |

**データ長計算式**:
```
dataLength = (ワード点数 + Dword点数) × 4 + 6
           = コマンド(2) + サブコマンド(2) + 点数(2) + デバイス指定(4×n)
```

### 4Eフレーム構造

| オフセット | 長さ | 項目 | 値 (例) | 説明 |
|-----------|------|------|---------|------|
| 0 | 2 | サブヘッダ | 0x54 0x00 | 4Eフレーム識別子 |
| 2 | 2 | シーケンス番号 | 0x00 0x00 (LE) | 要求-応答対応付け |
| 4 | 2 | 予約 | 0x00 0x00 | 固定値 |
| 6 | 1 | ネットワーク番号 | 0x00 | 自ネットワーク |
| 7 | 1 | 局番 | 0xFF | 自局 |
| 8 | 2 | I/O番号 | 0xFF 0x03 (LE) | 要求先ユニット |
| 10 | 1 | マルチドロップ局番 | 0x00 | 未使用 |
| 11 | 2 | データ長 | 可変 (LE) | 監視タイマ以降のバイト数 |
| 13 | 2 | 監視タイマ | 可変 (LE) | タイムアウト時間(250ms単位) |
| 15 | 2 | コマンド | 0x03 0x04 (LE) | ReadRandom |
| 17 | 2 | サブコマンド | 0x00 0x00 (LE) | ワード単位 |
| 19 | 1 | ワード点数 | 可変 | 読み出しワード数 |
| 20 | 1 | Dword点数 | 0x00 | 未対応 |
| 21 | n×4 | デバイス指定 | 可変 | 各4バイト (アドレス3+コード1) |

**データ長計算式**:
```
dataLength = 監視タイマ(2) + コマンド(2) + サブコマンド(2) + 点数(2) + デバイス指定(4×n)
```

---

## 実装の詳細

### ConfigToFrameManager クラス

```csharp
public class ConfigToFrameManager
{
    public byte[] BuildReadRandomFrameFromConfig(TargetDeviceConfig config)
    {
        // 1. null チェック
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        // 2. デバイスリスト検証
        if (config.Devices == null || config.Devices.Count == 0)
        {
            throw new ArgumentException("デバイスリストが空です", nameof(config));
        }

        // 3. フレームタイプ検証
        if (config.FrameType != "3E" && config.FrameType != "4E")
        {
            throw new ArgumentException(
                $"未対応のフレームタイプ: {config.FrameType}",
                nameof(config));
        }

        // 4. DeviceEntry → DeviceSpecification 変換
        var deviceSpecifications = config.Devices
            .Select(d => d.ToDeviceSpecification())
            .ToList();

        // 5. フレーム構築処理呼び出し
        byte[] frame = SlmpFrameBuilder.BuildReadRandomRequest(
            deviceSpecifications,
            config.FrameType,
            config.Timeout
        );

        // 6. フレーム返却
        return frame;
    }
}
```

### SlmpFrameBuilder クラス (抜粋)

```csharp
public static class SlmpFrameBuilder
{
    public static byte[] BuildReadRandomRequest(
        List<DeviceSpecification>? devices,
        string frameType = "3E",
        ushort timeout = 32)
    {
        // 入力検証
        if (devices == null || devices.Count == 0)
        {
            throw new ArgumentException("デバイスリストが空です", nameof(devices));
        }

        if (devices.Count > 255)
        {
            throw new ArgumentException(
                $"デバイス点数が上限を超えています: {devices.Count}点（最大255点）",
                nameof(devices));
        }

        var frame = new List<byte>();

        // ヘッダ部構築
        if (frameType == "3E")
        {
            frame.AddRange(new byte[] { 0x50, 0x00 });
        }
        else // "4E"
        {
            frame.AddRange(new byte[] { 0x54, 0x00 });
            frame.AddRange(new byte[] { 0x00, 0x00 }); // シーケンス番号
            frame.AddRange(new byte[] { 0x00, 0x00 }); // 予約
        }

        // 共通ヘッダ
        frame.Add(0x00);  // ネットワーク番号
        frame.Add(0xFF);  // 局番
        frame.AddRange(BitConverter.GetBytes((ushort)0x03FF));  // I/O番号
        frame.Add(0x00);  // マルチドロップ局番

        // データ長(仮)
        int dataLengthPosition = frame.Count;
        frame.AddRange(new byte[] { 0x00, 0x00 });

        // 監視タイマ
        frame.AddRange(BitConverter.GetBytes(timeout));

        // コマンド部
        frame.AddRange(BitConverter.GetBytes((ushort)0x0403));  // ReadRandom
        frame.AddRange(BitConverter.GetBytes((ushort)0x0000));  // サブコマンド
        frame.Add((byte)devices.Count);  // ワード点数
        frame.Add(0x00);                  // Dword点数

        // デバイス指定部
        foreach (var device in devices)
        {
            frame.AddRange(device.ToDeviceSpecificationBytes());
        }

        // データ長確定
        int dataLength;
        if (frameType == "3E")
        {
            dataLength = frame.Count - 2 - 9;
        }
        else
        {
            dataLength = frame.Count - 6 - 7;
        }

        frame[dataLengthPosition] = (byte)(dataLength & 0xFF);
        frame[dataLengthPosition + 1] = (byte)((dataLength >> 8) & 0xFF);

        return frame.ToArray();
    }
}
```

---

## 特徴

### ✅ 実装済み機能
- **動的フレーム構築**: 設定内容に応じてリアルタイムでフレーム生成
- **3E/4E両対応**: フレームタイプを引数で切り替え可能
- **入力検証の徹底**: 不正なパラメータを送信前に検出
- **リトルエンディアン処理**: BitConverterで明示的に処理
- **データ長自動計算**: デバイス数に応じて動的に計算
- **SLMP仕様書準拠**: 標準SLMP仕様に忠実な実装

### ⚠️ 未実装/制約事項
- **シーケンス番号管理**: 4Eフレームで固定値0x0000使用（自動インクリメント未実装）
- **Dword読み出し**: ワード読み出しのみ対応（Dword点数は常に0）
- **ASCII形式**: Binary形式のみ対応（BuildReadRandomRequestAscii は別メソッドで実装済み）

---

## ConMoni/PySLMPClientとの比較

### 実装方式の違い

| 項目 | andon (C#) | ConMoni (Python) | PySLMPClient (Python) |
|-----|-----------|-----------------|----------------------|
| **フレーム構築方式** | ✅ 動的構築 | 固定バイト配列 | 動的構築 |
| **対応フレーム形式** | 3E/4E Binary | 4E Binary固定 | 3E/4E × Binary/ASCII |
| **デバイスリスト** | 設定ファイル → 動的変換 | JSON固定値 | プログラム引数 |
| **データ長計算** | ✅ 動的計算 | 固定値 | 動的計算 |
| **入力検証** | ✅ 厳格 | なし | 標準的 |
| **シーケンス番号** | ⚠️ 固定 (TODO) | 固定 | ✅ 自動インクリメント |
| **保守性** | ✅ 高 (構造明確) | 低 (ブラックボックス) | 高 (ライブラリ) |

### andon の強み
- **設定ファイルベース**: appsettings.jsonで柔軟に設定変更可能
- **型安全性**: C#の型システムで誤った値を事前検出
- **厳格な検証**: SLMP制約（点数上限、デバイスコード制約）を送信前にチェック
- **明確な構造**: フレーム構築処理がコードとして可視化

---

## 次のステップ

Step2で構築した `byte[]` フレームは、Step3のPLC通信処理に渡されます。

```
Step2: BuildReadRandomFrameFromConfig()
    ↓ (byte[])
Step3: ConnectAsync() → SendRequestAsync()
    ↓ (送信完了)
Step4: ReceiveResponseAsync()
    ↓ (byte[] 受信データ)
Step6: ParseRawToStructuredData()
```

---

## 参考資料
- ConfigToFrameManager.cs: andon/Core/Managers/ConfigToFrameManager.cs
- SlmpFrameBuilder.cs: andon/Utilities/SlmpFrameBuilder.cs
- DeviceSpecification.cs: andon/Core/Models/DeviceSpecification.cs
- DeviceEntry.cs: andon/Core/Models/ConfigModels/DeviceEntry.cs
- SLMP仕様書: フレーム構造、コマンド定義
- documents/design/read_random実装/フレーム送信機能比較.md
