# andon Step1,2 実装処理フロー

## 概要
andonプロジェクトにおけるStep1（設定ファイル読み込み）とStep2（フレーム構築）の実装済みフローを記載する。

---

## Step1: 設定ファイル読み込み

### 実装クラス
`ConfigurationLoader` (andon/Infrastructure/Configuration/ConfigurationLoader.cs:22-46)

### 処理フロー

```
┌───────────────────────────────────────────┐
│ LoadPlcConnectionConfig()                 │
│ (ConfigurationLoader.cs:22-46)            │
├───────────────────────────────────────────┤
│ 1. 設定インスタンス作成                    │
│    var config = new TargetDeviceConfig()  │
│                                            │
│ 2. 基本設定の読み込み                      │
│    - PlcCommunication:Connection セクション │
│    - FrameVersion: "3E" or "4E"           │
│      (デフォルト: "4E")                    │
│                                            │
│ 3. タイムアウト設定の読み込み              │
│    - PlcCommunication:Timeouts セクション  │
│    - ReceiveTimeoutMs を読み込み           │
│      (デフォルト: 8000ms = 8秒)           │
│    - SLMP形式に変換: ms / 250             │
│      例: 8000ms → 32 (250ms単位)         │
│                                            │
│ 4. デバイスリストの読み込み                │
│    - PlcCommunication:TargetDevices:Devices │
│      セクション                            │
│    - JSON配列を List<DeviceEntry> に変換  │
│      [{                                    │
│        "DeviceName": "D",                  │
│        "StartAddress": 100,                │
│        "EndAddress": 200                   │
│      }, ...]                               │
│    - セクションが存在しない場合は空リスト  │
│                                            │
│ 5. 設定検証                                │
│    ValidateConfig(config)                 │
│    - FrameType: "3E"/"4E" のみ許可        │
│    - Timeout: 1～65535 範囲チェック       │
│    - Devices: 少なくとも1つ必要           │
│    - デバイス名検証: 空白/null禁止         │
│    - アドレス検証: 開始≤終了、0以上       │
│    - 点数検証: 255点以下                   │
│                                            │
│ 6. 設定オブジェクト返却                    │
│    return config                          │
└───────────────────────────────────────────┘
```

### 入力
- **appsettings.json** (設定ファイル)
  ```json
  {
    "PlcCommunication": {
      "Connection": {
        "FrameVersion": "4E"
      },
      "Timeouts": {
        "ReceiveTimeoutMs": 8000
      },
      "TargetDevices": {
        "Devices": [
          {
            "DeviceName": "D",
            "StartAddress": 100,
            "EndAddress": 200
          }
        ]
      }
    }
  }
  ```

### 出力
- **TargetDeviceConfig** オブジェクト
  - FrameType: string ("3E" or "4E")
  - Timeout: ushort (250ms単位)
  - Devices: List<DeviceEntry>

### 成功条件
- ✅ 設定ファイルに記載されている値を正確に取得できること
- ✅ 必要な値が設定されていない場合はエラーを返すこと
- ✅ 不正な値（範囲外、型不一致）を検出してエラーを返すこと

---

## Step2: 通信フレーム構築

### 実装クラス
- `ConfigToFrameManager` (andon/Core/Managers/ConfigToFrameManager.cs:19-53)
- `SlmpFrameBuilder` (andon/Utilities/SlmpFrameBuilder.cs:18-131)

### 処理フロー

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

### 入力
- **TargetDeviceConfig** (Step1の出力)
  - FrameType: "3E" or "4E"
  - Timeout: ushort (250ms単位)
  - Devices: List<DeviceEntry>

### 中間データ
- **DeviceSpecification** リスト
  - DeviceCode: DeviceCode (列挙型)
  - Address: uint (デバイスアドレス)
  - Type: ushort (ワード/Dword識別用)

### 出力
- **byte[]** (SLMPフレーム)

### 送信フレーム例

#### 3Eフレーム (D100, D200, D300 読み出し)
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

#### 4Eフレーム (同上)
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

### 成功条件
- ✅ Step1で取得した値から正しい内容のフレームを生成できること
- ✅ 3E/4E両フレームタイプに対応すること
- ✅ SLMP仕様書に準拠したバイト配列を生成すること
- ✅ デバイス番号がリトルエンディアンで正しく格納されること
- ✅ データ長が動的に計算され正しく設定されること
- ✅ 不正な入力に対してArgumentExceptionをスローすること

---

## データ変換の詳細

### DeviceEntry → DeviceSpecification 変換

```
DeviceEntry (設定ファイル読み込み用)
├─ DeviceName: string ("D", "M", "W", etc.)
├─ StartAddress: int (開始アドレス)
└─ EndAddress: int (終了アドレス)

    ↓ ToDeviceSpecification() 呼び出し

DeviceSpecification (フレーム構築用)
├─ DeviceCode: DeviceCode (列挙型: 0xA8=D, 0x90=M, etc.)
├─ Address: uint (個別デバイスアドレス)
└─ Type: ushort (ワード=0, Dword=1)
```

### デバイスコード変換例

| デバイス名 | DeviceCode列挙値 | バイト値 |
|-----------|-----------------|---------|
| D         | DeviceCode.D    | 0xA8    |
| M         | DeviceCode.M    | 0x90    |
| W         | DeviceCode.W    | 0xB4    |
| X         | DeviceCode.X    | 0x9C    |
| Y         | DeviceCode.Y    | 0x9D    |

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

## エラーハンドリング

### ConfigurationLoader.LoadPlcConnectionConfig()
| エラー条件 | 例外 | メッセージ |
|-----------|------|-----------|
| FrameTypeが"3E"/"4E"以外 | ArgumentException | "未対応のFrameType: {value}" |
| Timeoutが範囲外 (1-65535) | ArgumentException | "Timeoutが範囲外です" |
| Devicesが空 | ArgumentException | "デバイスリストが空です" |
| DeviceNameが空/null | ArgumentException | "DeviceNameが空です" |
| StartAddress > EndAddress | ArgumentException | "アドレス範囲が不正です" |
| 点数が255を超える | ArgumentException | "デバイス点数が上限を超えています" |

### SlmpFrameBuilder.BuildReadRandomRequest()
| エラー条件 | 例外 | メッセージ |
|-----------|------|-----------|
| devices が null または空 | ArgumentException | "デバイスリストが空です" |
| デバイス点数 > 255 | ArgumentException | "デバイス点数が上限を超えています: {count}点（最大255点）" |
| frameType が "3E"/"4E" 以外 | ArgumentException | "未対応のフレームタイプ: {frameType}" |

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

## まとめ

### Step1: 設定ファイル読み込み
- **役割**: appsettings.jsonから通信設定を読み込み、検証を行う
- **出力**: TargetDeviceConfig オブジェクト
- **実装場所**: ConfigurationLoader.LoadPlcConnectionConfig()

### Step2: フレーム構築
- **役割**: Step1の設定からSLMPフレーム(ReadRandom)を動的に生成
- **出力**: byte[] (送信可能なSLMPフレーム)
- **実装場所**:
  - ConfigToFrameManager.BuildReadRandomFrameFromConfig()
  - SlmpFrameBuilder.BuildReadRandomRequest()

### 実装の完成度
- ✅ **基本機能**: 完全実装済み
- ✅ **入力検証**: SLMP仕様準拠の厳格なチェック
- ✅ **3E/4E対応**: 両フレームタイプに対応
- ⚠️ **拡張機能**: シーケンス番号管理、Dword対応は今後の課題

---

## 参考資料
- SLMP仕様書: フレーム構造、コマンド定義
- ConMoni (Python): 固定バイト配列方式の実装例
- PySLMPClient: 動的フレーム構築の参考実装
- documents/design/read_random実装/フレーム送信機能比較.md
