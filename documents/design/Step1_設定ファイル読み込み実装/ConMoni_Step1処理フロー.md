# ConMoni (sample) - Step1: 設定ファイル読み込み処理フロー

## 処理概要

**範囲**: アプリケーション起動時
**動作**: Excel設定ファイルからJSON設定ファイルを生成
**実装場所**: `GenerateSettingJson.py` + `GetPlcData.py` 初期化処理

---

## 全体処理フロー

```
┌──────────────────────────────────────────────────────────────┐
│ 1. アプリケーション起動                                       │
│    main.py:18-20                                             │
├──────────────────────────────────────────────────────────────┤
│ GenerateSettingJson.GenerateSetting().main()                 │
│    - Excel設定ファイル一覧取得                                 │
│    - settings/excel/*.xlsx を読み込み                         │
│                                                               │
│ 処理内容:                                                     │
│    - Excelファイルから「settings」シート読み込み              │
│    - Excelファイルから「データ収集デバイス」シート読み込み     │
│    - 既存JSONファイルを削除（設定リセット）                    │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 2. デバイス番号変換処理                                       │
│    GenerateSettingJson.py:336-363                            │
├──────────────────────────────────────────────────────────────┤
│ 【10進デバイス変換】(338-348行目)                             │
│    対象デバイス: SM, SD, M, L, V, D, T系, C系等               │
│    変換方法: int → 3バイトリトルエンディアン                   │
│                                                               │
│    例: D100 (10進)                                            │
│        100 → [0x64, 0x00, 0x00]                             │
│                                                               │
│ 【16進デバイス変換】(353-362行目)                             │
│    対象デバイス: X, Y, B, SB, DX, DY                          │
│    変換方法: 16進数文字列 → 6桁ゼロパディング → 2桁ずつ分割   │
│                                                               │
│    例: Y40 (16進)                                             │
│        "40" → "000040" → [0x40, 0x00, 0x00]                 │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 3. デバイスコード変換                                         │
│    GenerateSettingJson.py:376-403                            │
├──────────────────────────────────────────────────────────────┤
│ デバイス名 → SLMPデバイスコードへ自動変換                      │
│                                                               │
│ 主要なデバイスコード:                                         │
│    M  → 0x90 (内部リレー)                                    │
│    D  → 0xA8 (データレジスタ)                                │
│    X  → 0x9C (入力)                                          │
│    Y  → 0x9D (出力)                                          │
│    W  → 0xB4 (リンクレジスタ)                                │
│                                                               │
│ ※全24種類のデバイスコード対応                                 │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 4. ビットデバイス処理（16点単位ワード化）                      │
│    GenerateSettingJson.py:89-143                             │
├──────────────────────────────────────────────────────────────┤
│ 【チャンク処理】(102-115行目)                                 │
│    - ビットデバイスを16点単位でグループ化                      │
│    - 同一ワード内（N～N+15）のビットをまとめる                 │
│                                                               │
│    例: M100, M101, M102, M110, M116                          │
│        → チャンク1: [M100-M115] (先頭M100)                   │
│        → チャンク2: [M116-M131] (先頭M116)                   │
│                                                               │
│ 【デバイス名リスト生成】(118-130行目)                         │
│    - 16点分のデバイス名を配列化                               │
│    - 未使用ビットは空文字 "" として保持                        │
│                                                               │
│    例: チャンク1のリスト                                      │
│        ["M100名", "M101名", "", ..., "M110名", ..., ""]     │
│        ↑ 16要素の配列                                         │
│                                                               │
│ 【通信用データ変換】(135-141行目)                             │
│    - 先頭デバイス番号を通信用4バイトに変換                     │
│    - [下位3バイト(リトルエンディアン), デバイスコード1バイト]  │
│                                                               │
│    例: M100 (0x90 = Mのデバイスコード)                       │
│        [0x64, 0x00, 0x00, 0x90]                             │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 5. ワードデバイス処理                                         │
│    GenerateSettingJson.py:156-200                            │
├──────────────────────────────────────────────────────────────┤
│ 対象デバイス: D, W, TN, STN, CN, Z, R, ZR等                  │
│                                                               │
│ 各デバイスごとに:                                             │
│    1. 通信用4バイトデータ追加                                 │
│       [下位3バイト(LE), デバイスコード1バイト]                │
│                                                               │
│    2. CSVヘッダ情報追加                                       │
│       accessDeviceName[] にデバイス項目名                     │
│                                                               │
│    3. 桁数変換係数追加                                        │
│       accessDeviceDigit[] に変換係数（例: 0.1, 1, 10）       │
│                                                               │
│    4. ビットフラグ設定                                        │
│       accessBitDataLoc[] に 0 (ワードは非ビット)             │
│                                                               │
│    5. デバイスカウンタ +1                                     │
│                                                               │
│ 例: D500, D502, D504                                          │
│    → [0xF4, 0x01, 0x00, 0xA8] (D500)                        │
│    → [0xF6, 0x01, 0x00, 0xA8] (D502)                        │
│    → [0xF8, 0x01, 0x00, 0xA8] (D504)                        │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 6. JSON設定ファイル生成                                       │
│    GenerateSettingJson.py:302-311                            │
├──────────────────────────────────────────────────────────────┤
│ 出力先: settings/settingJson/                                 │
│ ファイル名形式:                                               │
│    {Line}-{ProcessName}-{EquipmentShortName}-freq_{周期}_setting.json │
│                                                               │
│ JSON構造:                                                     │
│    {                                                          │
│        "accessPlcSetting": [84, 0, 0, ...],  // 通信フレーム  │
│        "accessDeviceName": [...],             // CSVヘッダ    │
│        "accessDeviceDigit": [...],            // 桁数変換     │
│        "accessBitDataLoc": [...],             // ビット判定   │
│        "IPAddress": "172.30.40.15",                           │
│        "Port": 8192,                                          │
│        "ConnectionMethod": "UDP",                             │
│        "DataReadingFrequency": 100,           // 周期(ms)     │
│        "ContinuousData": 0,                   // 0=不連続     │
│        "EcoMemory": 0,                        // 省メモリ     │
│        ... (その他のExcel設定値)                              │
│    }                                                          │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 7. GetPlcDataインスタンス生成                                 │
│    main.py:21-25                                             │
├──────────────────────────────────────────────────────────────┤
│ 処理内容:                                                     │
│    - settingJson/*.json ファイル一覧取得                      │
│    - 各JSONファイルごとに GetPlcData インスタンス生成          │
│    - instances = [GetPlcData(file) for file in targetFiles]  │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 8. GetPlcData初期化処理                                       │
│    GetPlcData.py:20-69                                       │
├──────────────────────────────────────────────────────────────┤
│ 【JSON設定読み込み】(26-27行目)                               │
│    with open(targetFilePath, "r") as f:                      │
│        self.settingData = json.load(f)                       │
│                                                               │
│ 【内部変数初期化】(28-49行目)                                 │
│    - baseSettingPath: データ保存先パス                        │
│    - globalCounter: CSV書き込み回数カウント                   │
│    - digitControl: 桁数変換係数配列                           │
│    - ContinuousData: 連続/不連続モードフラグ                  │
│    - previousData: 前回値保持（不連続モード用）               │
│    - writeData: 書き込み用データバッファ                      │
│    - ecoMemory: 省メモリモードフラグ                          │
│                                                               │
│ 【ソケット作成・接続】(54-69行目)                             │
│    1. JSON設定から接続情報取得                                │
│       - IPAddress                                             │
│       - Port                                                  │
│       - ConnectionMethod (TCP/UDP)                            │
│                                                               │
│    2. ソケット種別判定                                        │
│       if ConnectionMethod == "TCP":                           │
│           socket(AF_INET, SOCK_STREAM)                        │
│       else:                                                   │
│           socket(AF_INET, SOCK_DGRAM)                         │
│                                                               │
│    3. PLC接続実行                                             │
│       self.sock.connect((HOST, PORT))                         │
│                                                               │
│    4. エラーハンドリング                                      │
│       try-except: 接続失敗時 statusCode=1 設定               │
│                                                               │
│ 【タイムアウト設定】                                          │
│    - BUFSIZE = 4096 (受信バッファサイズ)                      │
│    - デフォルトタイムアウト: 2秒                              │
└──────────────────────────────────────────────────────────────┘
```

---

## デバイスコード変換テーブル

ConMoniで実装されているデバイスコード変換テーブル（全24種類）:

| デバイス名 | デバイスコード | 進数表記 | 用途 |
|-----------|---------------|---------|------|
| SM | 0x91 | 10進 | 特殊リレー |
| SD | 0xA9 | 10進 | 特殊レジスタ |
| X | 0x9C | 16進 | 入力 |
| Y | 0x9D | 16進 | 出力 |
| M | 0x90 | 10進 | 内部リレー |
| L | 0x92 | 10進 | ラッチリレー |
| F | 0x93 | 10進 | アナンシエータ |
| V | 0x94 | 10進 | エッジリレー |
| B | 0xA0 | 16進 | リンクリレー |
| D | 0xA8 | 10進 | データレジスタ |
| W | 0xB4 | 10進 | リンクレジスタ |
| TS | 0xC1 | 10進 | タイマ接点 |
| TC | 0xC0 | 10進 | タイマコイル |
| TN | 0xC2 | 10進 | タイマ現在値 |
| STS | 0xC7 | 10進 | 積算タイマ接点 |
| STC | 0xC6 | 10進 | 積算タイマコイル |
| STN | 0xC8 | 10進 | 積算タイマ現在値 |
| CS | 0xC4 | 10進 | カウンタ接点 |
| CC | 0xC3 | 10進 | カウンタコイル |
| CN | 0xC5 | 10進 | カウンタ現在値 |
| SB | 0xA1 | 16進 | リンク特殊リレー |
| SW | 0xB5 | 10進 | リンク特殊レジスタ |
| DX | 0xA2 | 16進 | ダイレクト入力 |
| DY | 0xA3 | 16進 | ダイレクト出力 |
| Z | 0xCC | 10進 | インデックスレジスタ |
| R | 0xAF | 10進 | ファイルレジスタ |
| ZR | 0xB0 | 10進 | ファイルレジスタ |

---

## デバイス番号変換の詳細

### 10進デバイスの変換処理

**実装コード** (GenerateSettingJson.py:338-348):

```python
_dfDec = df[df["デバイス"].str.contains('SM|SD|M|L|V|D|T|TS|TC|TN|...')]
byte_order = "little"
for index, value in enumerate(_dfDec["デバイス番号"]):
    if isinstance(value, str):
        value = int(value)
    splitHexValue = value.to_bytes(3, byte_order)  # 3バイトのリトルエンディアン
    hexToIntValue = [b for b in splitHexValue]
    _dfDec["通信用1桁目"][index] = hexToIntValue[0]
    _dfDec["通信用2桁目"][index] = hexToIntValue[1]
    _dfDec["通信用3桁目"][index] = hexToIntValue[2]
```

**変換例**:
- D100 (10進): 100 → 0x64 → リトルエンディアン3バイト → `[0x64, 0x00, 0x00]`
- D500 (10進): 500 → 0x1F4 → リトルエンディアン3バイト → `[0xF4, 0x01, 0x00]`
- M1603 (10進): 1603 → 0x643 → リトルエンディアン3バイト → `[0x43, 0x06, 0x00]`

### 16進デバイスの変換処理

**実装コード** (GenerateSettingJson.py:353-362):

```python
_dfHex = df[~df["デバイス"].str.contains('SM|SD|M|L|V|D|T|...')]
_dfHex["デバイス番号"] = _dfHex["デバイス番号"].str.zfill(6)  # 6桁にゼロパディング
_dfHex["通信用1桁目"] = _dfHex["デバイス番号"].str[4:]     # 下位2桁
_dfHex["通信用2桁目"] = _dfHex["デバイス番号"].str[2:4]   # 中位2桁
_dfHex["通信用3桁目"] = _dfHex["デバイス番号"].str[0:2]   # 上位2桁
```

**変換例**:
- Y40 (16進): "40" → "000040" → `["40", "00", "00"]` → `[0x40, 0x00, 0x00]`
- Y304 (16進): "130" → "000130" → `["30", "01", "00"]` → `[0x30, 0x01, 0x00]`
- Y4192 (16進): "1060" → "001060" → `["60", "10", "00"]` → `[0x60, 0x10, 0x00]`

---

## ビットデバイス最適化処理の詳細

### アルゴリズム概要

ビットデバイス（M, X, Yなど）を**16点単位でワードにまとめる**処理。これにより通信効率が向上。

### チャンク処理アルゴリズム

**実装コード** (GenerateSettingJson.py:102-115):

```python
df = df.reset_index()
chunks = []
start = df["デバイス番号"][0]
chunk = [start]

for num in df["デバイス番号"][1:]:
    num = int(num)
    if start <= num < start + 16:
        chunk.append(num)  # 同じワード内（16点以内）
    else:
        chunks.append(chunk)  # 新しいワード開始
        start = num
        chunk = [start]
chunks.append(chunk)
```

### 具体例

**入力**: M100, M101, M102, M110, M116, M117

**処理結果**:
```
チャンク1: [M100, M101, M102, M110] (範囲: M100～M115)
    → 先頭M100から1ワード読み出し
    → 通信データ: [0x64, 0x00, 0x00, 0x90]

チャンク2: [M116, M117] (範囲: M116～M131)
    → 先頭M116から1ワード読み出し
    → 通信データ: [0x74, 0x00, 0x00, 0x90]
```

**効果**:
- 個別に6点読み出し → 2ワードにまとめて読み出し
- 通信回数削減: 6回 → 2回
- フレームサイズ削減: 24バイト → 8バイト

### デバイス名リスト生成

**実装コード** (GenerateSettingJson.py:118-130):

```python
headDeviceNumber = []
deviceNameList = []
for x in chunks:
    deviceNames = []
    # 16点分のデバイス名を生成
    for y in range(0, 16):
        try:
            deviceNames.append(df["項目名"][df["デバイス番号"]==x[0]+y].iloc[0])
        except:
            deviceNames.append("")  # 未使用ビットは空文字
    deviceNameList.append(deviceNames)
    headDeviceNumber.append(x[0])
```

**出力例**:
```
チャンク1 (M100):
    deviceNames = [
        "運転中",      # M100
        "異常発生",    # M101
        "温度警報",    # M102
        "",            # M103 (未使用)
        ...
        "原料不足",    # M110
        "",            # M111 (未使用)
        ...
        ""             # M115 (未使用)
    ]
```

---

## JSON設定ファイル出力例

実際のConMoni設定ファイルの構造:

```json
{
  "accessPlcSetting": [84, 0, 0, 0, 0, 0, ...],
  "accessDeviceName": [
    "DATETIME",
    "ブランク",
    ["運転中", "異常発生", "温度警報", "", ...],
    "#1露点温度現在値",
    "#1酸素濃度現在値",
    ...
  ],
  "accessDeviceDigit": [1, 0.1, 1, 10, ...],
  "accessBitDataLoc": [0, 1, 0, 0, ...],
  "accessCombinationDataGroup": [0, 0, 0, ...],

  "IPAddress": "172.30.40.15",
  "Port": 8192,
  "ConnectionMethod": "UDP",
  "DataReadingFrequency": 100,
  "ContinuousData": 0,
  "EcoMemory": 0,

  "Works": "工場A",
  "Line": "ライン1",
  "ProcessName": "加熱工程",
  "EquipmentShortName": "炉A",
  "CsvName": "line1-furnace-a.csv"
}
```

**フィールド説明**:
- `accessPlcSetting`: SLMP通信フレーム（バイト配列）
- `accessDeviceName`: CSVヘッダ情報（ビットデバイスは16要素配列）
- `accessDeviceDigit`: 桁数変換係数（受信値に乗算）
- `accessBitDataLoc`: ビットフラグ（1=ビットデバイス, 0=ワード）
- `accessCombinationDataGroup`: データ結合グループ（Dword対応用、現在未使用）

---

## andonプロジェクトとの対比

### ConMoniの特徴

**✅ 採用すべき点:**

1. **デバイスコード変換テーブル**
   - 24種類のデバイスに対応
   - andonでは `Dictionary<string, byte>` で実装可能

2. **ビットデバイス最適化アルゴリズム**
   - 16点単位ワード化による通信効率向上
   - そのまま移植可能（高価値）

3. **リトルエンディアン自動変換**
   - `int.to_bytes(3, "little")` による自動変換
   - andonでは `BitConverter.GetBytes()` で実現

4. **設定駆動アーキテクチャ**
   - Excel → JSON → 実行
   - 現場でのデバイス変更が容易

**⚠️ 改善すべき点:**

1. **エラー検証不足**
   - フレーム生成時の検証なし
   - 不正な設定でもそのまま処理
   - **andon推奨**: 設定読み込み時の多段階検証

2. **接続エラーハンドリング簡易**
   - statusCode=1 のみで詳細不明
   - **andon推奨**: 詳細なエラー分類・リトライ機構

3. **型安全性**
   - Pythonの動的型付け
   - **andon優位**: C#の静的型付けによる安全性

### andonでの実装状況

**Step1: 設定読み込み（実装済み）**

実装場所: `ConfigurationLoader.cs`

```csharp
public TargetDeviceConfig LoadPlcConnectionConfig()
{
    var config = new TargetDeviceConfig();

    // 基本設定の読み込み
    var connectionSection = _configuration.GetSection("PlcCommunication:Connection");
    config.FrameType = connectionSection["FrameVersion"] ?? "4E";

    // Devicesリストの読み込み
    var devicesSection = _configuration.GetSection("PlcCommunication:TargetDevices:Devices");
    var deviceEntries = devicesSection.Get<List<DeviceEntry>>() ?? new List<DeviceEntry>();

    // DeviceEntry → DeviceSpecificationに変換
    config.Devices = deviceEntries
        .Select(entry => entry.ToDeviceSpecification())
        .ToList();

    // 検証
    ValidateConfig(config);

    return config;
}
```

**ConMoniとの比較**:

| 項目 | ConMoni | andon |
|-----|---------|-------|
| 設定ソース | Excel | appsettings.json |
| 中間ファイル | JSON | なし（直接オブジェクト化） |
| 型安全性 | 低（動的型） | 高（静的型） |
| エラー検証 | 最小限 | 多段階検証 |
| デバイスコード変換 | 実行時変換 | 列挙型定義（DeviceCode enum） |

---

## andon実装への適用推奨

### 1. デバイスコード変換テーブルの実装

**推奨実装** (C#):

```csharp
public static class DeviceCodeMap
{
    public static readonly Dictionary<string, byte> Map = new()
    {
        { "SM", 0x91 },
        { "SD", 0xA9 },
        { "X", 0x9C },
        { "Y", 0x9D },
        { "M", 0x90 },
        { "L", 0x92 },
        { "D", 0xA8 },
        { "W", 0xB4 },
        // ... 全24種類
    };

    public static byte GetDeviceCode(string deviceType)
    {
        if (!Map.TryGetValue(deviceType, out byte code))
        {
            throw new ArgumentException($"未対応のデバイスタイプ: {deviceType}");
        }
        return code;
    }
}
```

### 2. ビットデバイス最適化処理の実装

**推奨実装** (C#):

```csharp
public static List<DeviceSpecification> OptimizeBitDevices(List<DeviceSpecification> bitDevices)
{
    var result = new List<DeviceSpecification>();
    var sorted = bitDevices.OrderBy(d => d.DeviceNumber).ToList();

    int start = sorted[0].DeviceNumber;
    var chunk = new List<DeviceSpecification> { sorted[0] };

    foreach (var device in sorted.Skip(1))
    {
        if (device.DeviceNumber < start + 16)
        {
            chunk.Add(device);
        }
        else
        {
            // チャンク完成 → 先頭デバイスのみ登録
            result.Add(chunk[0]);
            start = device.DeviceNumber;
            chunk = new List<DeviceSpecification> { device };
        }
    }
    result.Add(chunk[0]); // 最後のチャンク

    return result;
}
```

### 3. デバイス番号のリトルエンディアン変換

**推奨実装** (C#):

```csharp
public static byte[] ConvertDeviceNumberToBytes(int deviceNumber)
{
    // 3バイトのリトルエンディアン変換
    byte[] bytes = new byte[3];
    bytes[0] = (byte)(deviceNumber & 0xFF);        // 下位バイト
    bytes[1] = (byte)((deviceNumber >> 8) & 0xFF); // 中位バイト
    bytes[2] = (byte)((deviceNumber >> 16) & 0xFF); // 上位バイト
    return bytes;
}
```

---

## まとめ

### ConMoni Step1処理の要約

```
Excel設定ファイル
    ↓
Excelシート読み込み
    ↓
デバイス番号変換
    - 10進デバイス: int → 3バイトLE
    - 16進デバイス: 文字列 → 3バイトLE
    ↓
デバイスコード変換
    - デバイス名 → SLMPコード（24種類対応）
    ↓
ビット最適化処理
    - 16点単位ワード化
    - デバイス名リスト生成
    ↓
ワードデバイス処理
    - 各デバイスを個別登録
    - CSVヘッダ情報生成
    ↓
JSON設定ファイル出力
    - accessPlcSetting (フレームデータ)
    - accessDeviceName (ヘッダ情報)
    - accessDeviceDigit (変換係数)
    - accessBitDataLoc (ビット判定)
    - 接続情報 (IP, Port, 周期等)
    ↓
GetPlcDataインスタンス生成
    ↓
JSON設定読み込み
    ↓
ソケット作成・PLC接続
    ↓
Step2以降へ
```

### 重要な技術的知見

1. **リトルエンディアン処理**
   - デバイス番号は必ず3バイトのリトルエンディアン
   - 例: 500 (0x1F4) → [0xF4, 0x01, 0x00]

2. **ビットデバイスの扱い**
   - 読み出し単位: 16点（1ワード）
   - 先頭指定: グループの最小デバイス番号
   - 通信効率向上のため必須

3. **デバイスコードの重要性**
   - 各デバイスタイプに固有のコードが存在
   - M=0x90, D=0xA8, Y=0x9D等
   - 誤ったコードは通信エラーの原因

4. **設定駆動の利点**
   - プログラム変更なしでデバイス変更可能
   - 現場でのメンテナンス性向上
   - andonでもappsettings.jsonで同等実現可能
