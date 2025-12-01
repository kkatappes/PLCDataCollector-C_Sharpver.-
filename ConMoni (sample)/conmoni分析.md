# ConMoni アプリケーション分析レポート

## 分析日時
2025-11-11

## 概要
ConMoniは三菱電機PLC（SLMP通信）からの自動データ収集システム。Excelで設定を管理し、定期的にPLCからデータを取得してCSV形式で保存する。

---

## 1. アプリケーション構造

### 主要ファイル構成
```
ConMoni/
├── modules/
│   ├── main.py                          # エントリーポイント
│   ├── interface/
│   │   └── IGetPlcData.py              # インターフェース定義
│   └── process/
│       ├── GenerateSettingJson.py      # 設定ファイル生成
│       └── GetPlcData.py               # PLC通信・データ取得
├── settings/
│   ├── excel/                          # Excel設定ファイル格納
│   └── settingJson/                    # 生成されたJSON設定
└── data/                               # 収集データ保存先
```

### アーキテクチャ
- **並列処理**: ProcessPoolExecutorで複数PLCから同時データ収集
- **設定駆動**: Excel → JSON → 実行プロセス
- **スケジューラ**: スレッドベースの定期実行

---

## 2. PLC通信機能

### 2.1 通信方式
- **プロトコル**: SLMP（Seamless Message Protocol）3Eフレーム
- **形式**: バイナリ形式
- **接続方法**: TCP/UDP両対応（設定で選択可能）
- **実装場所**: `GetPlcData.py:280-327`

### 2.2 通信処理フロー

```python
# 要求送信 (287行目)
self.sock.send(bytes(self.settingData["accessPlcSetting"]))

# 応答受信 (290行目)
res = self.sock.recv(self.BUFSIZE)
resRaw = [format(i,'02X') for i in res]
```

### 2.3 接続設定

```python
# 60-69行目
if self.settingData["ConnectionMethod"] == "TCP":
    self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
else:
    self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

self.sock.connect((HOST, PORT))
```

### 2.4 データ収集モード

#### 通常モード (`start.bat` → `getPlcData()`)
- 定時性重視: 2/100秒の誤差
- メモリにデータ蓄積 → 60回分まとめてCSV書き込み
- 実装: `GetPlcData.py:280-410`

#### 省メモリモード (`startCompress.bat` → `getPlcData2()`)
- メモリ消費抑制: 1/10秒の誤差許容
- 都度CSV書き込み（tmpファイル使用）
- 毎時00分にファイル確定
- 実装: `GetPlcData.py:137-278`

---

## 3. フレーム構築機能（重要）

### 3.1 フレーム形式

**使用形式**: 3Eフレーム バイナリ形式（サブヘッダが変則的）

```
┌─────────────────────────────────────┐
│ ヘッダー: なし                        │
├─────────────────────────────────────┤
│ サブヘッダ: 54 00                    │ ← 標準は50 00だが54 00を使用
│ 予約: 00 00 00 00                   │
│ 要求先NW: 00                         │
│ 要求先局番: FF                       │
│ 要求先I/O: FF 03                     │
│ マルチドロップ: 00                    │
│ データ長: XX XX (動的計算)           │
│ 監視タイマ: 20 00                    │
│ コマンド: 03 04 (一括読み出し)        │
│ サブコマンド: 00 00                  │
│ デバイス点数: XX                     │
│ Dword点数: 00                        │
│ デバイス指定: ...                    │
├─────────────────────────────────────┤
│ フッター: なし                        │
└─────────────────────────────────────┘
```

### 3.2 フレーム自動生成処理

**実装場所**: `GenerateSettingJson.py:244-300`

#### 基本フレーム構造（257-266行目）

```python
self.accessPlcSetting["accessPlcSetting"].extend([
    0x54, 0x00,           # 0-1: サブヘッダ ※標準3Eは50 00
    0x00, 0x00,           # 2-3: シリアル（予約）
    0x00, 0x00,           # 4-5: 予約
    0x00,                 # 6: 要求先ネットワーク番号
    0xFF,                 # 7: 要求先局番
    0xFF, 0x03,           # 8-9: 要求先ユニットI/O番号
    0x00,                 # 10: マルチドロップ局番
    0xFF, 0x03,           # 11-12: 要求データ長（後で動的計算）
    0x20, 0x00,           # 13-14: 監視タイマ
    0x03, 0x04,           # 15-16: コマンド（0403 = 一括読み出し）
    0x00, 0x00,           # 17-18: サブコマンド
    0x00,                 # 19: ワード点数（後で動的設定）
    0x00                  # 20: ダブルワード点数（未使用）
])
```

#### データ長の動的計算（291-294行目）

```python
numData = len(self.accessPlcSetting["accessPlcSetting"][13:])
hexDevices = str(hex(numData)[2:].zfill(4))
self.accessPlcSetting["accessPlcSetting"][11] = int(hexDevices[2:],16)  # 下位バイト
self.accessPlcSetting["accessPlcSetting"][12] = int(hexDevices[:2],16)  # 上位バイト
```

#### デバイス点数の設定（299行目）

```python
self.accessPlcSetting["accessPlcSetting"][19] = self.deviceCounter
```

### 3.3 デバイスコード自動変換

**実装場所**: `GenerateSettingJson.py:376-403`

Excelのデバイス名（例：D100, M0, Y40）をSLMPデバイスコードに自動変換：

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

### 3.4 デバイス番号変換処理

**実装場所**: `GenerateSettingJson.py:336-363`

#### 10進デバイスの変換（338-348行目）

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

**例**: D100（10進）の変換
- 10進: 100
- 3バイトリトルエンディアン: `64 00 00`
- 通信用: [0x64, 0x00, 0x00]

#### 16進デバイスの変換（353-362行目）

```python
_dfHex = df[~df["デバイス"].str.contains('SM|SD|M|L|V|D|T|...')]
_dfHex["デバイス番号"] = _dfHex["デバイス番号"].str.zfill(6)  # 6桁にゼロパディング
_dfHex["通信用1桁目"] = _dfHex["デバイス番号"].str[4:]     # 下位2桁
_dfHex["通信用2桁目"] = _dfHex["デバイス番号"].str[2:4]   # 中位2桁
_dfHex["通信用3桁目"] = _dfHex["デバイス番号"].str[0:2]   # 上位2桁
```

**例**: Y40（16進）の変換
- 16進文字列: "40"
- 6桁パディング: "000040"
- 通信用: [0x40, 0x00, 0x00]

### 3.5 ビットデバイスの特殊処理（重要）

**実装場所**: `GenerateSettingJson.py:89-143`

#### 処理概要
ビットデバイス（M, X, Yなど）を**16点単位でワードにまとめる**処理。

#### アルゴリズム（102-115行目）

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

#### デバイス名リスト生成（118-130行目）

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

#### 通信用データへの変換（135-141行目）

```python
resultHeadDeviceNumbers = []
for deviceNumber in headDeviceNumber:
    tmpHeadDeviceNumbers = []
    tmpHeadDeviceNumbers.append(int(df["通信用1桁目"][df["デバイス番号"] == deviceNumber].iloc[0]))
    tmpHeadDeviceNumbers.append(int(df["通信用2桁目"][df["デバイス番号"] == deviceNumber].iloc[0]))
    tmpHeadDeviceNumbers.append(int(df["通信用3桁目"][df["デバイス番号"] == deviceNumber].iloc[0]))
    tmpHeadDeviceNumbers.append(int(df["通信用4桁目"][df["デバイス番号"] == deviceNumber].iloc[0]))
    resultHeadDeviceNumbers.append(tmpHeadDeviceNumbers)
```

#### 具体例

**入力**: M100, M101, M102, M110, M111, M115, M116, M117

**処理結果**:
```
チャンク1: [M100-M115] → 先頭M100から1ワード読み出し
チャンク2: [M116-M131] → 先頭M116から1ワード読み出し

フレームに追加されるデータ:
- M100: [0x64, 0x00, 0x00, 0x90]  # 100 = 0x64, デバイスコード0x90
- M116: [0x74, 0x00, 0x00, 0x90]  # 116 = 0x74, デバイスコード0x90
```

### 3.6 ワードデバイスの処理

**実装場所**: `GenerateSettingJson.py:156-200`

#### 処理フロー（160-191行目）

```python
wordDeviceList = ["FD", "SD", "D", "W", "TN", "STN", "CN", "SW", "Z", "R", "ZR"]
for device in wordDeviceList:
    if df[df["デバイス"] == device].empty:
        continue
    for index in range(0, len(df[df["デバイス"] == device])):
        _df = df[df["デバイス"] == device].reset_index(drop=True)

        # 通信用デバイス登録（4バイト）
        self.accessPlcSetting["accessPlcSetting"].append(int(_df["通信用1桁目"][index]))
        self.accessPlcSetting["accessPlcSetting"].append(int(_df["通信用2桁目"][index]))
        self.accessPlcSetting["accessPlcSetting"].append(int(_df["通信用3桁目"][index]))
        self.accessPlcSetting["accessPlcSetting"].append(int(_df["通信用4桁目"][index]))

        # ヘッダ情報（CSVカラム名）
        self.accessPlcSetting["accessDeviceName"].append(str(_df["項目名"][index]))

        # 桁数変換係数
        self.accessPlcSetting["accessDeviceDigit"].append(float(_df["桁数"][index]))

        # ビットフラグ（ワードなので0）
        self.accessPlcSetting["accessBitDataLoc"].append(0)

        self.deviceCounter += 1
```

### 3.7 実際のフレーム生成例

**JSON設定ファイルから**:

```json
{
  "accessPlcSetting": [
    84, 0, 0, 0, 0, 0,        // サブヘッダ（0x54 0x00）
    0, 255, 255, 3, 0,        // NW設定（0x00 0xFF 0xFF 0x03 0x00）
    72, 0,                    // データ長72バイト（0x48 0x00）
    32, 0,                    // 監視タイマ（0x20 0x00）
    3, 4,                     // コマンド0x0403
    0, 0,                     // サブコマンド
    16, 0,                    // 16ワード読み出し

    // デバイス指定（4バイト×16デバイス）
    244, 1, 0, 168,           // D500: 0xF4 0x01 0x00 0xA8
    246, 1, 0, 168,           // D502: 0xF6 0x01 0x00 0xA8
    248, 1, 0, 168,           // D504
    249, 1, 0, 168,           // D505
    250, 1, 0, 168,           // D506
    252, 1, 0, 168,           // D508
    254, 1, 0, 168,           // D510
    0, 2, 0, 168,             // D512: 0x00 0x02 = 512
    67, 6, 0, 144,            // M1603: 0x43 0x06 = 1603, 0x90 = M
    175, 6, 0, 144,           // M1711
    198, 4, 0, 144,           // M1222
    176, 4, 0, 144,           // M1200
    192, 4, 0, 144,           // M1216
    230, 5, 0, 144,           // M1510
    48, 1, 0, 157,            // Y304: 0x30 0x01 = 0x130, 0x9D = Y
    96, 16, 0, 157            // Y4192: 0x60 0x10 = 0x1060
  ],
  "accessDeviceName": [
    "DATETIME", "ブランク",
    "#1露点温度現在値", "#1酸素濃度現在値", "圧力現在値", ...
  ]
}
```

---

## 4. データ処理フロー

### 4.1 レスポンスデータのパース

**実装場所**: `GetPlcData.py:290-327`

```python
# 応答受信
res = self.sock.recv(self.BUFSIZE)
resRaw = [format(i,'02X') for i in res]

# 302-307行目: データ値の抽出
for x in range(0, self.settingData["accessPlcSetting"][19]+1):
    tmpDeviceData = int(''.join(resRaw[13+2*x:15+2*x][::-1]), 16)
    tmpData.append(tmpDeviceData)
```

**レスポンス構造**:
```
バイト0-10: ヘッダ情報
バイト11-12: データ長
バイト13-: デバイスデータ（2バイト×点数）
```

### 4.2 ビットデータの展開

**実装場所**: `GetPlcData.py:311-323`

```python
calcTempData = np.array(tmpData) * self.digitControl
final_result = []
for r, flag in zip(calcTempData, self.settingData["accessBitDataLoc"]):
    if flag == 1:  # ビットデバイスの場合
        # 16ビットに展開
        binary = format(r.astype(np.uint16), '016b')
        binary = binary[::-1]  # ビット順反転
        binary_list = list(map(int, binary))
        final_result.extend(binary_list)
    else:  # ワードデバイスの場合
        final_result.append(r)
```

**例**: ワード値 `0x0105` の展開
```
16進: 0x0105
2進: 0000000100000101
反転: 10100000010000000
リスト: [1,0,1,0,0,0,0,0,0,1,0,0,0,0,0,0]
        ↑           ↑
       bit0        bit8
```

### 4.3 不連続データ処理

**実装場所**: `GetPlcData.py:243-251`

```python
if self.ContinuousData == 0:  # 不連続モード時
    if len(final_result) != len(self.previousConvertedRawData):
        self.previousConvertedRawData = [None] * len(final_result)
    tempResult = [None] * len(final_result)
    for index in range(len(final_result)):
        if final_result[index] != self.previousConvertedRawData[index]:
            tempResult[index] = final_result[index]  # 変化があった値のみ
    self.previousConvertedRawData = final_result
    final_result = tempResult
```

**効果**: 値が変化していないデータは `None` となり、CSV上では空セルとなる。

### 4.4 CSV出力

**実装場所**: `GetPlcData.py:256-278`

```python
# タイムスタンプ追加
final_result.insert(0, dt.datetime.now().strftime("%F %T.%f"))

# ヘッダー生成（初回のみ）
if not os.path.exists(tmpCsvPath):
    with open(tmpCsvPath, "a", encoding="utf-8", newline='') as f:
        writer = csv.writer(f)
        headerItems = []
        for item in self.settingData["accessDeviceName"]:
            if isinstance(item, list):  # ビットデバイスは16要素のリスト
                headerItems.extend(item)
            else:
                headerItems.append(item)
        # 空ヘッダーを"BlankColumn_N"に置換
        for index, replaceItem in enumerate(headerItems):
            if replaceItem == "":
                headerItems[index] = "BlankColumn_" + str(index)
        writer.writerow(headerItems)

# データ書き込み
with open(tmpCsvPath, "a", encoding="utf-8", newline='') as f:
    writer = csv.writer(f)
    writer.writerow(final_result)
```

---

## 5. 全体データフロー

```
┌─────────────────────────────────────────────────────────┐
│ 1. Excel設定ファイル作成（ユーザー）                      │
│    - settings/excel/*.xlsx                               │
│    - シート: settings, データ収集デバイス                 │
└─────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────┐
│ 2. GenerateSettingJson.py                               │
│    - Excelファイル読み込み                                │
│    - デバイス番号変換（10進/16進 → 3バイト）              │
│    - デバイスコード変換（D → 0xA8等）                     │
│    - ビットデバイス16点単位処理                           │
│    - フレーム自動構築                                     │
│    - JSON設定ファイル生成                                 │
└─────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────┐
│ 3. main.py                                              │
│    - JSON設定ファイル一覧取得                             │
│    - GetPlcDataインスタンス生成（設定数分）               │
│    - ProcessPoolExecutorで並列実行                       │
└─────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────┐
│ 4. GetPlcData.py                                        │
│    ┌──────────────────────────────────────────────┐    │
│    │ 4-1. 初期化                                   │    │
│    │   - JSON設定読み込み                          │    │
│    │   - Socket作成・接続（TCP/UDP）               │    │
│    └──────────────────────────────────────────────┘    │
│                      ↓                                  │
│    ┌──────────────────────────────────────────────┐    │
│    │ 4-2. スケジューラ起動                         │    │
│    │   - 指定周期でgetPlcData()実行                │    │
│    │   - スレッドベース                            │    │
│    └──────────────────────────────────────────────┘    │
│                      ↓                                  │
│    ┌──────────────────────────────────────────────┐    │
│    │ 4-3. データ取得（各周期ごと）                  │    │
│    │   - フレーム送信                              │    │
│    │   - レスポンス受信                            │    │
│    │   - データ抽出（バイト13以降）                 │    │
│    │   - ビット展開処理                            │    │
│    │   - 不連続データ処理（オプション）             │    │
│    └──────────────────────────────────────────────┘    │
│                      ↓                                  │
│    ┌──────────────────────────────────────────────┐    │
│    │ 4-4. CSV出力                                  │    │
│    │   通常モード: 60回分蓄積 → まとめて書き込み    │    │
│    │   省メモリモード: 都度書き込み                 │    │
│    └──────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────┐
│ 5. データ保存                                            │
│    - data/設備名-周期/*.csv（通常モード）                 │
│    - data/tmp_*.csv → OneDrive（省メモリモード）         │
└─────────────────────────────────────────────────────────┘
```

---

## 6. andonプロジェクト（C#）への適用推奨事項

### 6.1 採用すべき設計

#### ✅ フレーム自動構築ロジック
- `GenerateSettingJson.py:257-300` の構造をそのまま移植
- デバイスコード変換テーブルを `Dictionary<string, byte>` で実装
- データ長・デバイス点数の動的計算処理

#### ✅ ビットデバイス処理アルゴリズム
- 16点単位のワード化処理（`compressBitDevice:89-143`）
- C#の `BitArray` または `BitConverter` を活用

#### ✅ デバイス番号変換処理
- 10進デバイス: `BitConverter.GetBytes()` でリトルエンディアン変換
- 16進デバイス: 文字列パース → バイト配列変換

### 6.2 改善推奨点

#### ⚠️ サブヘッダの標準化
- ConMoni: `0x54 0x00`（4Eフレーム相当）
- **推奨**: `0x50 0x00`（標準3Eバイナリ）を優先実装
- 接続テストで問題があれば `0x54 0x00` も試行

#### ⚠️ エラーハンドリング強化
- ConMoniは接続失敗時 `statusCode=1` のみ
- **推奨**: 詳細なエラーコード・リトライ機構の実装

#### ⚠️ 応答フレーム検証
- ConMoniはエラーコードチェックなし
- **推奨**: 応答ヘッダのエンドコード確認（正常: `0x0000`）

### 6.3 C#実装のクラス構成案

```csharp
// フレーム構築
public class SlmpFrameBuilder
{
    public byte[] BuildReadRequest(List<DeviceSpec> devices)
    public static Dictionary<string, byte> DeviceCodeMap { get; }
    private byte[] ConvertDeviceNumber(int number, bool isHex)
}

// デバイス指定
public class DeviceSpec
{
    public string DeviceType { get; set; }  // "D", "M", "X"等
    public int DeviceNumber { get; set; }
    public bool IsBitDevice { get; set; }
}

// ビット処理
public class BitDeviceProcessor
{
    public List<int> GroupBitDevicesIntoWords(List<DeviceSpec> bitDevices)
    public List<bool> ExpandWordToBits(ushort wordValue)
}

// 通信
public class SlmpCommunicator
{
    public async Task<byte[]> SendRequestAsync(byte[] frame)
    public ResponseData ParseResponse(byte[] response)
}
```

---

## 7. 重要な技術的知見

### 7.1 バイトオーダー
- **デバイス番号**: リトルエンディアン（下位バイト先）
- **例**: D500 (0x01F4) → `[0xF4, 0x01, 0x00]`

### 7.2 ビット順序
- **ワード内ビット順**: 下位ビットが先（LSB first）
- **例**: 0x0105 → bit0=1, bit2=1, bit8=1

### 7.3 データ長計算
- **計算対象**: バイト13以降の全データ
- **公式**: `len(frame[13:])`
- **格納位置**: バイト11-12（リトルエンディアン）

### 7.4 ビットデバイスの扱い
- **読み出し単位**: 16点（1ワード）
- **先頭指定**: グループの最小デバイス番号
- **例**: M100-M115 → M100を指定して1ワード読み出し

---

## 8. テスト実施時の注意点

### PLC接続テスト推奨手順

1. **標準フレームでテスト**
   - サブヘッダ: `0x50 0x00`
   - 最小構成（Dデバイス1点のみ）

2. **変則フレームでテスト**（失敗時）
   - サブヘッダ: `0x54 0x00`

3. **デバイス種類を段階的に追加**
   - ワードデバイス → ビットデバイス → 混在

4. **エラー応答の確認**
   - 応答バイト9-10のエンドコード確認
   - `0x0000` = 正常、それ以外 = エラー

---

## 9. 参照情報

### ファイルパス
- ConMoniサンプル: `C:\Users\1010821\Desktop\python\andon\ConMoni (sample)\`
- 主要実装: `ConMoni\modules\process\GetPlcData.py`
- フレーム構築: `ConMoni\modules\process\GenerateSettingJson.py`

### 関連ドキュメント
- フレーム構築仕様: `documents\フレーム構築内容.md`
- SLMP仕様書: 三菱電機公式ドキュメント参照

---

## 10. まとめ

### ConMoniの優れた点
- ✅ Excel駆動の設定管理（現場での変更容易性）
- ✅ フレーム完全自動構築（手動フレーム作成不要）
- ✅ ビットデバイスの最適化処理（16点単位ワード化）
- ✅ 並列処理対応（複数PLC同時接続）
- ✅ 省メモリモード実装（リソース制約環境対応）

### andonプロジェクトへの移植価値
- **高**: フレーム構築ロジック（そのまま移植可能）
- **高**: デバイスコード変換テーブル
- **高**: ビット処理アルゴリズム
- **中**: 並列処理アーキテクチャ（C#のTaskで再設計）
- **低**: CSV出力処理（andonは独自実装）

---

**分析完了**
