# PySLMPClient ライブラリ分析レポート

## 分析日時
2025-11-11

## 概要
PySLMPClientは、三菱電機製PLC（Programmable Logic Controller）との通信を実現するPure Python実装のSLMPクライアントライブラリ。バイナリ/ASCII形式、3E/4Eフレーム、TCP/UDP通信に対応し、デバイスの読み書きからシステム操作まで包括的な機能を提供する。

---

## 1. ライブラリ構造

### 主要ファイル構成
```
PySLMPClient/
├── pyslmpclient/
│   ├── __init__.py              # メインクラス（SLMPClient）
│   ├── const.py                 # 定数定義（コマンド、デバイスコード等）
│   └── util.py                  # ユーティリティ関数（フレーム構築等）
├── tests/
│   ├── test_main.py
│   └── test_util.py
├── doc/                         # ドキュメント
├── readme.rst
└── setup.py
```

### アーキテクチャの特徴
- **Pure Python実装**: C拡張なし、プラットフォーム非依存
- **マルチスレッド対応**: 受信処理を別スレッドで非同期実行
- **コンテキストマネージャ対応**: `with`文による自動接続管理
- **シーケンス管理**: 4Eフレームで送受信の対応付け（0-255）
- **受信キュー**: シリアル番号をキーとした辞書で管理

---

## 2. PLC通信機能

### 2.1 通信方式

#### サポートする通信形態
- **トランスポート**: TCP/UDP両対応
- **交信コード**: バイナリ/ASCII両対応
- **フレーム形式**: 3Eフレーム（ST型）/4Eフレーム（MT型）

#### 接続設定（`__init__.py:22-68`）

```python
class SLMPClient(object):
    def __init__(self, addr, port=5000, binary=True, ver=4, tcp=False):
        """
        :param str addr: 接続するPLCのIPアドレス
        :param int port: 接続先のポート番号（デフォルト5000）
        :param bool binary: バイナリコード使用（True）/ASCII（False）
        :param int ver: フレームバージョン 4（4E）or 3（3E）
        :param bool tcp: TCP使用（True）/UDP（False）
        """
```

#### 接続処理（`__init__.py:86-124`）

```python
def open(self):
    """通信の開始"""
    with self.__lock:
        if self.__protocol[2]:  # TCP
            self.__socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        else:  # UDP
            self.__socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.__socket.connect(self.__addr)
        self.__socket.settimeout(1)
        self.__recv_thread.start()  # 受信スレッド起動
```

### 2.2 通信処理フロー

#### 送信処理（`__init__.py:126-163`）

```python
def __cmd_format(self, timeout, cmd, sub_cmd, data):
    """コマンドにヘッダを加え送信"""
    # フレーム構築
    if self.__protocol[0]:  # バイナリ
        make_frame = util.make_binary_frame
    else:  # ASCII
        make_frame = util.make_ascii_frame

    buf = make_frame(
        self.__seq,        # シリアル番号
        self.target,       # 接続先情報
        timeout,           # 監視タイマ
        cmd,               # コマンド
        sub_cmd,           # サブコマンド
        data,              # データ部
        self.__protocol[1] # フレームバージョン
    )
    self.__socket.sendall(buf)

    # シーケンス番号インクリメント（4Eフレームのみ）
    if self.__protocol[1] == 4:
        self.__seq += 1
        return self.__seq - 1
    else:  # 3Eフレーム
        return 0
```

#### 受信処理（`__init__.py:165-255`）

```python
def __recv(self):
    """別スレッドで常時受信"""
    buf = self.__socket.recv(512)

    # フレームタイプ判定
    if buf[0] == ord("D"):  # ASCII
        # ASCIIパース処理
    elif buf[0] in (0xD0, 0xD4):  # Binary
        if buf[0] == 0xD0:  # 3E Binary
            # 3Eバイナリパース
        elif buf[0] == 0xD4:  # 4E Binary
            # 4Eバイナリパース

    # 受信キューに格納
    with self.__lock:
        self.__recv_queue[seq] = (
            network_num, pc_num, io_num,
            m_drop_num, term_code, data
        )
```

#### タイムアウト処理（`__init__.py:279-296`）

```python
def __recv_loop(self, seq: int, timeout: int):
    """応答待ち処理"""
    timeout *= 0.25  # 250msec単位をsec換算
    if timeout == 0:
        timeout = 100  # デフォルト100秒

    start = time.monotonic()
    while seq not in self.__recv_queue.keys():
        end = time.monotonic()
        if end - start > timeout:
            break

    if seq not in self.__recv_queue.keys():
        raise TimeoutError()

    # エンドコードチェック
    end_code = util.EndCode(data[4])
    if end_code != util.EndCode.Success:
        raise util.SLMPCommunicationError(end_code)
```

### 2.3 対応コマンド一覧（`const.py:7-117`）

#### デバイスアクセス系
- `Device_Read (0x0401)`: デバイス一括読み出し
- `Device_Write (0x1401)`: デバイス一括書き込み
- `Device_ReadRandom (0x0403)`: ランダム読み出し
- `Device_WriteRandom (0x1402)`: ランダム書き込み
- `Device_EntryMonitorDevice (0x0801)`: モニタデバイス登録
- `Device_ExecuteMonitor (0x0802)`: モニタ実行
- `Device_ReadBlock (0x0406)`: ブロック読み出し
- `Device_WriteBlock (0x1406)`: ブロック書き込み

#### リモート制御系
- `RemoteControl_RemoteRun (0x1001)`: リモートRUN
- `RemoteControl_RemoteStop (0x1002)`: リモートSTOP
- `RemoteControl_RemotePause (0x1003)`: リモート一時停止
- `RemoteControl_RemoteReset (0x1006)`: リモートリセット
- `RemoteControl_ReadTypeName (0x0101)`: 形名読み出し

#### メモリ操作系
- `Memory_Read (0x0613)`: メモリ読み出し
- `Memory_Write (0x1613)`: メモリ書き込み

#### その他
- `SelfTest (0x0619)`: 通信テスト
- `ClearError_Code (0x1617)`: エラーコードクリア

---

## 3. フレーム構築機能（重要）

### 3.1 フレーム構造

#### バイナリ形式（`util.py:205-245`）

```
┌──────────────────────────────────────────┐
│ ヘッダ部（4Eフレーム: 6byte / 3Eフレーム: 2byte）│
├──────────────────────────────────────────┤
│ 4E: [0x54, 0x00] + シリアル(2) + 予約(2) │
│ 3E: [0x50, 0x00]                         │
├──────────────────────────────────────────┤
│ コマンド部（15byte固定）                  │
├──────────────────────────────────────────┤
│ ネットワーク番号(1)                       │
│ 要求先局番(1)                            │
│ 要求先プロセッサ番号(2)                   │
│ マルチドロップ局番(1)                     │
│ データ長(2) ※リトルエンディアン          │
│ 監視タイマ(2) ※250msec単位               │
│ コマンド(2)                              │
│ サブコマンド(2)                          │
├──────────────────────────────────────────┤
│ データ部（可変長）                        │
└──────────────────────────────────────────┘
```

#### ASCII形式（`util.py:248-287`）

全てASCII文字の16進数表現（2倍のサイズ）
- 例: `0x54` → `"5400"`

### 3.2 バイナリフレーム構築処理（`util.py:205-245`）

```python
def make_binary_frame(seq, target, timeout, cmd, sub_cmd, data, ver):
    """バイナリモードのコマンドフレーム作成

    :param int seq: シリアル番号（0-255）
    :param target: 接続先（Target型）
    :param int timeout: 監視タイマ（250msec単位）
    :param cmd: コマンド（SLMPCommand型）
    :param int sub_cmd: サブコマンド
    :param bytes data: データ部
    :param int ver: フレームバージョン（3 or 4）
    :return: 構築されたフレーム
    :rtype: bytes
    """
    # コマンド部を構築
    cmd_text = struct.pack(
        "<BBHBHHHH",
        target.network,      # ネットワーク番号
        target.node,         # 局番
        target.dst_proc,     # プロセッサ番号
        target.m_drop,       # マルチドロップ局番
        len(data) + 6,       # データ長（コマンド+サブコマンド+データ）
        timeout,             # 監視タイマ
        cmd.value,           # コマンド
        sub_cmd,             # サブコマンド
    )

    # ヘッダ追加
    if ver == 4:  # 4Eフレーム
        buf = struct.pack("<HHH", 0x54, seq, 0x00) + cmd_text
    elif ver == 3:  # 3Eフレーム
        buf = b"\x50\x00" + cmd_text

    # データ部追加
    buf += data

    # サイズチェック（8194バイト未満）
    assert len(buf) < 8194, len(buf)
    return buf
```

**重要なポイント**:
- データ長は「コマンド+サブコマンド+データ」の合計（コマンド部=6byte固定）
- リトルエンディアン形式（`<`指定）
- 最大フレームサイズ: 8194バイト

### 3.3 ASCIIフレーム構築処理（`util.py:248-287`）

```python
def make_ascii_frame(seq, target, timeout, cmd, sub_cmd, data, ver):
    """ASCIIモードのコマンドフレーム作成"""
    # 全て16進ASCII文字列で表現
    cmd_text = b"%02X%02X%04X%02X%04X%04X%04X%04X" % (
        target.network,      # 2桁
        target.node,         # 2桁
        target.dst_proc,     # 4桁
        target.m_drop,       # 2桁
        len(data) + 12,      # 4桁（ASCII形式はデータ長も2倍）
        timeout,             # 4桁
        cmd.value,           # 4桁
        sub_cmd,             # 4桁
    )

    if ver == 4:
        buf = b"5400%04X0000" % seq + cmd_text + data
    elif ver == 3:
        buf = b"5000" + cmd_text + data

    return buf
```

### 3.4 接続先情報（Target）（`util.py:132-203`）

```python
class Target(object):
    def __init__(self, network_num=0, node_num=0,
                 dst_proc_num=0, m_drop_num=0):
        """SLMP通信対象の指定

        :param int network_num: ネットワーク番号（0-255）
        :param int node_num: 要求先局番（0-255）
        :param int dst_proc_num: 要求先プロセッサ番号（0-65535）
        :param int m_drop_num: 要求先マルチドロップ局番（0-255）
        """
        self.__network = network_num
        self.__node = node_num
        self.__dst_proc = dst_proc_num
        self.__m_drop = m_drop_num

    # プロパティでバリデーション実装
```

**デフォルト値**:
- ネットワーク番号: 0（自局ネットワーク）
- 局番: 0（自局）
- プロセッサ番号: 0（メインプロセッサ）
- マルチドロップ局番: 0（未使用）

---

## 4. デバイスアクセス機能

### 4.1 デバイスコード定義（`const.py:120-158`）

```python
class DeviceCode(enum.Enum):
    # ビットデバイス
    SM = 0x91   # 特殊リレー
    X = 0x9C    # 入力
    Y = 0x9D    # 出力
    M = 0x90    # 内部リレー
    L = 0x92    # ラッチリレー
    F = 0x93    # アナンシエータ

    # ワードデバイス
    SD = 0xA9   # 特殊レジスタ
    D = 0xA8    # データレジスタ
    W = 0xB4    # リンクレジスタ
    R = 0xAF    # ファイルレジスタ
    ZR = 0xB0   # ファイルレジスタ

    # タイマ
    TS = 0xC1   # タイマ接点
    TC = 0xC0   # タイマコイル
    TN = 0xC2   # タイマ現在値

    # カウンタ
    CS = 0xC4   # カウンタ接点
    CC = 0xC3   # カウンタコイル
    CN = 0xC5   # カウンタ現在値

    # その他
    B = 0xA0    # リンクリレー
    SB = 0xA1   # リンク特殊リレー
    SW = 0xB5   # リンク特殊レジスタ
    DX = 0xA2   # ダイレクト入力
    DY = 0xA3   # ダイレクト出力
    Z = 0xCC    # インデックスレジスタ
```

### 4.2 デバイスアドレス形式（`const.py:162-189`）

#### 16進数表現デバイス（`D_ADDR_16`）
```python
D_ADDR_16 = (
    DeviceCode.X,    # 入力
    DeviceCode.Y,    # 出力
    DeviceCode.B,    # リンクリレー
    DeviceCode.W,    # リンクレジスタ
    DeviceCode.SB,   # リンク特殊リレー
    DeviceCode.SW,   # リンク特殊レジスタ
    DeviceCode.DX,   # ダイレクト入力
    DeviceCode.DY,   # ダイレクト出力
    DeviceCode.ZR,   # ファイルレジスタ
)
```
**扱い**: アドレスを16進数として解釈・変換

#### 4バイトアドレス専用デバイス（`D_ADDR_4BYTE`）
```python
D_ADDR_4BYTE = (
    DeviceCode.LTS,   # 長時間タイマ接点
    DeviceCode.LTC,   # 長時間タイマコイル
    DeviceCode.LTN,   # 長時間タイマ現在値
    DeviceCode.LZ,    # ロングインデックスレジスタ
    DeviceCode.RD,    # 特殊ファイルレジスタ
)
```
**扱い**: アドレス指定に4バイト必要

### 4.3 連続デバイス読み出し

#### ビットデバイス読み出し（`__init__.py:323-345`）

```python
def read_bit_devices(self, device_code, start_num, count, timeout=0):
    """ビットデバイス一括読み出し

    :param device_code: デバイスコード（例: DeviceCode.M）
    :param int start_num: 開始アドレス
    :param int count: 読み出し点数
    :param int timeout: 監視タイマ（250msec単位）
    :return: デバイスの値（Tuple[bool]）
    """
    data = self.__read_devices(
        device_code, start_num, count,
        timeout, 0x0001  # サブコマンド: ビット単位
    )

    # レスポンス処理
    if isinstance(data[5], str):  # ASCII
        ret = tuple(x == "1" for x in data[5])
    else:  # Binary
        ret = tuple(x == 1 for x in util.decode_bcd(list(data[5])))
        if count % 2 == 1:  # 奇数点は最後を切り捨て
            ret = ret[:-1]

    return ret
```

**使用例**:
```python
client = SLMPClient("192.168.1.10", binary=True, ver=3)
with client:
    # M100からM115まで16点読み出し
    values = client.read_bit_devices(DeviceCode.M, 100, 16)
    print(values)  # (True, False, True, ...)
```

#### ワードデバイス読み出し（`__init__.py:347-371`）

```python
def read_word_devices(self, device_code, start_num, count, timeout=0):
    """ワードデバイス一括読み出し

    :return: デバイスの値（array.array[H]）
    """
    data = self.__read_devices(
        device_code, start_num, count,
        timeout, 0x0000  # サブコマンド: ワード単位
    )

    if isinstance(data[5], str):  # ASCII
        ret = array('H', [
            int(data[5][x:][:4], base=16)
            for x in range(0, len(data[5]), 4)
        ])
    else:  # Binary
        ret = array('H', data[5])

    return ret
```

**使用例**:
```python
# D100からD109まで10ワード読み出し
values = client.read_word_devices(DeviceCode.D, 100, 10)
print(values)  # array('H', [1234, 5678, ...])
```

### 4.4 共通読み出し処理（`__init__.py:298-321`）

```python
def __read_devices(self, device_code, start_num, count, timeout, sub_cmd):
    """デバイス読み出し共通処理"""
    cmd = const.SLMPCommand.Device_Read

    if self.__protocol[0]:  # バイナリ
        # アドレス（3バイト）+ デバイスコード（1バイト）+ 点数（2バイト）
        cmd_text = struct.pack("<I", start_num)[:-1]  # 下位3バイト
        cmd_text += struct.pack("<B", device_code.value)
        cmd_text += struct.pack("<H", count)
    else:  # ASCII
        cmd_text = b"%s" % device_code.name.encode("ascii")
        if len(cmd_text) == 1:
            cmd_text += b"*"
        if device_code in const.D_ADDR_16:
            cmd_text += b"%06X%04d" % (start_num, count)
        else:
            cmd_text += b"%06d%04d" % (start_num, count)

    seq = self.__cmd_format(timeout, cmd, sub_cmd, cmd_text)
    data = self.__recv_loop(seq, timeout)
    return data
```

**データ部構造（バイナリ）**:
```
┌─────────────────────────────┐
│ 開始アドレス（3byte, LE）    │
│ デバイスコード（1byte）      │
│ 読み出し点数（2byte, LE）    │
└─────────────────────────────┘
```

### 4.5 連続デバイス書き込み

#### ビットデバイス書き込み（`__init__.py:429-440`）

```python
def write_bit_devices(self, dc2, start_num, data, timeout=0):
    """ビットデバイス一括書き込み

    :param data: 書き込むデータ（List[int]）
    """
    self.__write_devices(dc2, start_num, data, timeout, 0x01)
```

#### ワードデバイス書き込み（`__init__.py:442-453`）

```python
def write_word_devices(self, dc2, start_num, data, timeout=0):
    """ワードデバイス一括書き込み

    :param data: 書き込むデータ（List[int]）
    """
    self.__write_devices(dc2, start_num, data, timeout, 0x00)
```

#### 共通書き込み処理（`__init__.py:373-427`）

```python
def __write_devices(self, dc2, start_num, data, timeout, sub_cmd):
    """デバイス書き込み共通処理"""
    cmd = const.SLMPCommand.Device_Write

    if self.__protocol[0]:  # Binary
        buf = struct.pack("<I", start_num)[:-1]
        buf += struct.pack("<BH", dc2.value, len(data))

        if sub_cmd & 0x01:  # ビット
            # 2点ずつBCDパック
            for i in range(0, len(data), 2):
                tmp = data[i:][:2]
                if len(tmp) == 2:
                    if tmp[0]:
                        buf += b"\x11" if tmp[1] else b"\x10"
                    else:
                        buf += b"\x01" if tmp[1] else b"\x00"
                else:
                    buf += b"\x10" if tmp[0] else b"\x00"
        else:  # ワード
            buf += array('H', data).tobytes()
    else:  # ASCII
        # ASCII形式の書き込みデータ構築
        ...

    self.__cmd_format(timeout, cmd, sub_cmd, buf)
```

### 4.6 ランダムデバイスアクセス

#### ランダム読み出し（`__init__.py:493-534`）

```python
def read_random_devices(self, word_list, dword_list, timeout=0):
    """連続していないデバイスの読み出し

    :param word_list: ワードデバイスリスト
      [(DeviceCode, address), ...]
    :param dword_list: ダブルワードデバイスリスト
      [(DeviceCode, address), ...]
    :return: (ワードデータ, ダブルワードデータ)
      (List[bytes], List[bytes])
    """
    cmd = const.SLMPCommand.Device_ReadRandom
    buf = self.__format_device_list(word_list, dword_list)
    seq = self.__cmd_format(timeout, cmd, 0x0000, buf)
    data = self.__recv_loop(seq, timeout)

    # レスポンスからワード/ダブルワードデータを抽出
    buf = data[5]
    if isinstance(buf, str):  # ASCII
        # ASCII処理
        ...
    else:  # Binary
        split_pos = len(word_list) * 2
        dword_data, word_data = util.extracts_word_dword_data(
            buf, split_pos
        )

    return word_data, dword_data
```

**使用例**:
```python
# 不連続なデバイスを一度に読み出し
word_list = [
    (DeviceCode.D, 100),
    (DeviceCode.D, 200),
    (DeviceCode.D, 500)
]
dword_list = [
    (DeviceCode.D, 1000),  # D1000-D1001を4バイトで読み出し
]
word_data, dword_data = client.read_random_devices(word_list, dword_list)
```

#### デバイスリストフォーマット（`__init__.py:455-491`）

```python
def __format_device_list(self, word_list, dword_list):
    """デバイスリストを要求電文にフォーマット"""
    if self.__protocol[0]:  # Binary
        buf = struct.pack("<BB", len(word_list), len(dword_list))
        for dc, addr in word_list:
            buf += struct.pack("<I", addr)[:-1]  # 3バイト
            buf += struct.pack("<B", dc.value)
        for dc, addr in dword_list:
            buf += struct.pack("<I", addr)[:-1]
            buf += struct.pack("<B", dc.value)
    else:  # ASCII
        buf = b"%02X%02X" % (len(word_list), len(dword_list))
        for dc, addr in word_list:
            buf += dc.name.encode("ascii")
            if len(dc.name) == 1:
                buf += b"*"
            if dc in const.D_ADDR_16:
                buf += b"%06X" % addr
            else:
                buf += b"%06d" % addr
        # dword_list処理...
    return buf
```

#### ランダム書き込み（`__init__.py:561-603`）

```python
def write_random_word_devices(self, word_list, dword_list, timeout=0):
    """連続していないワードデバイスへの書き込み

    :param word_list: [(DeviceCode, address, bytes), ...]
    :param dword_list: [(DeviceCode, address, bytes), ...]
    """
    if self.__protocol[0]:  # Binary
        buf = struct.pack("<BB", len(word_list), len(dword_list))
        for v in word_list:
            buf += struct.pack("<I", v[1])[:-1]  # アドレス
            buf += struct.pack("<B", v[0].value)  # デバイスコード
            byte_buf = v[2][:]
            while len(byte_buf) < 2:
                byte_buf += b"\x00"
            buf += byte_buf[:2]  # データ（2バイト）
        for v in dword_list:
            buf += struct.pack("<I", v[1])[:-1]
            buf += struct.pack("<B", v[0].value)
            byte_buf = v[2][:]
            while len(byte_buf) < 4:
                byte_buf += b"\x00"
            buf += byte_buf[:4]  # データ（4バイト）

    self.__cmd_format(timeout, const.SLMPCommand.Device_WriteRandom, 0x0, buf)
```

### 4.7 モニタ機能

#### モニタデバイス登録（`__init__.py:605-624`）

```python
def entry_monitor_device(self, word_list, dword_list, timeout=0):
    """モニタするデバイスを登録（最大192点）

    :param word_list: ワードデバイスリスト
    :param dword_list: ダブルワードデバイスリスト
    """
    cmd = const.SLMPCommand.Device_EntryMonitorDevice
    assert 1 < len(word_list) + len(dword_list) <= 192

    buf = self.__format_device_list(word_list, dword_list)
    self.__cmd_format(timeout, cmd, 0x0000, buf)

    # モニタ登録情報を保持
    with self.__lock:
        self.__monitor_device_num = (len(word_list), len(dword_list))
```

#### モニタ実行（`__init__.py:626-667`）

```python
def execute_monitor(self, timeout=0):
    """登録したモニタデバイスの値を一括取得

    :return: (ワードデータ, ダブルワードデータ)
    """
    cmd = const.SLMPCommand.Device_ExecuteMonitor

    if self.__monitor_device_num[0] == 0 and self.__monitor_device_num[1] == 0:
        raise RuntimeError("モニタデバイス未登録")

    seq = self.__cmd_format(timeout, cmd, 0x00, b"")
    data = self.__recv_loop(seq, timeout)

    # レスポンスデータを登録情報に基づいて分割
    split_pos = self.__monitor_device_num[0] * 2
    dword_data, word_data = util.extracts_word_dword_data(
        data[5], split_pos
    )

    return word_data, dword_data
```

**使用例**:
```python
# 定期的に同じデバイスを読み出す場合に効率的
word_list = [(DeviceCode.D, 100), (DeviceCode.D, 101), ...]
dword_list = [(DeviceCode.D, 200), ...]

client.entry_monitor_device(word_list, dword_list)

# 定期実行
while True:
    word_data, dword_data = client.execute_monitor()
    # データ処理...
    time.sleep(1)
```

### 4.8 ブロックアクセス

#### ブロック読み出し（`__init__.py:669-733`）

```python
def read_block(self, word_list, bit_list, timeout=0):
    """複数のデバイスブロックを一括読み出し

    :param word_list: [(DeviceCode, address, count), ...]
    :param bit_list: [(DeviceCode, address, count), ...]
    :return: (ワードデータリスト, ビットデータリスト)
      (List[List[bytes]], List[List[bool]])
    """
    cmd = const.SLMPCommand.Device_ReadBlock

    if self.__protocol[0]:  # Binary
        buf = struct.pack("<BB", len(word_list), len(bit_list))
        for dc, addr, num in word_list + bit_list:
            buf += struct.pack("<I", addr)[:-1]
            buf += struct.pack("<BH", dc.value, num)

    seq = self.__cmd_format(timeout, cmd, 0x00, buf)
    data = self.__recv_loop(seq, timeout)

    # レスポンスを各ブロックに分割
    word_data = []
    bit_data = []
    for dc1, addr1, num1 in word_list:
        tmp_buf = []
        for _ in range(num1):
            d1 = bytes_buf.pop()
            d2 = bytes_buf.pop()
            tmp_buf.append(bytes([d1, d2]))
        word_data.append(tmp_buf)

    for dc1, addr1, num1 in bit_list:
        tmp_buf = []
        for _ in range(num1):
            d1 = bytes_buf.pop()
            d2 = bytes_buf.pop()
            tmp_buf.extend(util.unpack_bits([d1, d2]))
        bit_data.append(tmp_buf)

    return word_data, bit_data
```

#### ブロック書き込み（`__init__.py:735-797`）

```python
def write_block(self, word_list, bit_list, timeout=0):
    """複数のデバイスブロックへの一括書き込み

    :param word_list: [(DeviceCode, address, count, data), ...]
    :param bit_list: [(DeviceCode, address, count, data), ...]
    """
    if len(word_list) + len(bit_list) > 120:
        raise RuntimeError("書き込みブロック数超過")

    if self.__protocol[0]:  # Binary
        buf = struct.pack("<BB", len(word_list), len(bit_list))
        for dc, addr, num, w_data in word_list:
            buf += struct.pack("<I", addr)[:-1]
            buf += struct.pack("<BH", dc.value, num)
            assert len(w_data) == num
            for v in w_data:
                buf += struct.pack("<H", v)

        for dc, addr, num, w_data in bit_list:
            buf += struct.pack("<I", addr)[:-1]
            buf += struct.pack("<BH", dc.value, num)
            assert len(w_data) == num * 16  # ビットは16点単位
            p_data = util.pack_bits(w_data)
            for v in p_data:
                buf += struct.pack("<B", v)

    self.__cmd_format(timeout, const.SLMPCommand.Device_WriteBlock, 0x00, buf)
```

---

## 5. ビット処理ユーティリティ

### 5.1 ビット展開（`util.py:60-84`）

```python
def unpack_bits(data):
    """LSBから順に格納されているビット列を配列に展開

    [<M107...M100>, <M115...M108>] →
    [<M100>, ..., <M107>, <M108>, ..., <M115>]

    :param data: パック済みのデータ列（List[int]）
    :return: 1項目1デバイスとした配列（List[int]）
    """
    data = np.asarray(data, dtype=np.uint8)

    # 2次元配列化（1バイトごとに行を分ける）
    byte_array2d = data.reshape((data.size, 1))

    # ビット展開
    byte_array2d_bin = np.unpackbits(byte_array2d, axis=1)

    # 列方向反転（LSB first）して1次元化
    return list(byte_array2d_bin[:, ::-1].flatten())
```

**例**:
```
入力: [0x05, 0x03]
      = [00000101, 00000011]

展開: [1,0,1,0,0,0,0,0, 1,1,0,0,0,0,0,0]
       ↑               ↑
      bit0            bit8
```

### 5.2 ビットパック（`util.py:87-110`）

```python
def pack_bits(data):
    """ビットデータ配列をLSBから順にパック

    [<M100>, ..., <M107>, <M108>, ..., <M115>] →
    [<M107...M100>, <M115...M108>]

    :param data: デバイスごとのビットデータ（List[int]）
    :return: パックした配列（List[int]）
    """
    data = np.asarray(data, dtype=np.uint8)

    # 8の倍数にゼロパディング
    size8 = -(-data.size // 8) * 8
    byte_array_bin = np.zeros(size8, "u1")
    byte_array_bin[:data.size] = data

    # 2次元配列化（8ビットごと）
    byte_array2d_bin = byte_array_bin.reshape((size8 // 8, 8))

    # ビットパック（列方向反転してからパック）
    return list(np.packbits(byte_array2d_bin[:, ::-1]))
```

### 5.3 BCDエンコード/デコード

#### エンコード（`util.py:13-35`）

```python
def encode_bcd(data):
    """4bit BCD配列にエンコード

    [1,2,3,4,...] → [0x12,0x34,...]

    :param data: エンコード対象（List[int]）
    :return: 4bit毎にパックされた結果（List[int]）
    """
    data = np.asarray(data, dtype=np.uint8)
    bin_array_h = (data[::2] & 0x0F) << 4   # 偶数インデックス（上位4bit）
    bin_array_l = data[1::2] & 0x0F          # 奇数インデックス（下位4bit）
    bin_array = np.zeros_like(bin_array_h)

    if data.size % 2 == 0:  # 偶数個
        bin_array = bin_array_h | bin_array_l
    else:  # 奇数個（最後を0埋め）
        bin_array[:-1] = bin_array_h[:-1] | bin_array_l
        bin_array[-1] = bin_array_h[-1]

    return list(bin_array)
```

#### デコード（`util.py:38-57`）

```python
def decode_bcd(data):
    """4bit BCD配列をデコード

    [0x12,0x34,...] → [1,2,3,4,...]

    :param data: 4bitにパックされたデータ列（List[int]）
    :return: デコード結果（List[int]）
    """
    data = np.asarray(data, dtype=np.uint8)
    bin_array_h = (data >> 4) & 0x0F  # 上位4bit
    bin_array_l = data & 0x0F          # 下位4bit

    bin_array = np.empty(data.size * 2, "u1")
    bin_array[::2] = bin_array_h   # 偶数インデックスに上位
    bin_array[1::2] = bin_array_l  # 奇数インデックスに下位

    return list(bin_array)
```

### 5.4 ワード/ダブルワードデータ分割（`util.py:307-331`）

```python
def extracts_word_dword_data(buf, split_pos):
    """2バイトデータ列と4バイトデータ列を切り分ける

    :param bytes buf: 切り分け元のバイナリデータ
    :param int split_pos: 2バイトデータの総バイト数
    :return: (ダブルワードデータ, ワードデータ)
    :rtype: (List[bytes], List[bytes])
    """
    word_data = []
    dword_data = []

    word_buf = bytearray(buf[:split_pos])
    dword_buf = bytearray(buf[split_pos:])

    word_buf.reverse()
    dword_buf.reverse()

    while word_buf:
        d1 = word_buf.pop()
        d2 = word_buf.pop()
        word_data.append(bytes([d1, d2]))

    while dword_buf:
        d1 = dword_buf.pop()
        d2 = dword_buf.pop()
        d3 = dword_buf.pop()
        d4 = dword_buf.pop()
        dword_data.append(bytes([d1, d2, d3, d4]))

    return dword_data, word_data
```

---

## 6. システム機能

### 6.1 形名読み出し（`__init__.py:799-818`）

```python
def read_type_name(self, timeout=0):
    """アクセス先ユニットの形名と形名コードを読み出す

    :return: (形名, 形名コード)
    :rtype: (str, const.TypeCode)
    """
    seq = self.__cmd_format(
        timeout,
        const.SLMPCommand.RemoteControl_ReadTypeName,
        0x00,
        b""
    )
    data = self.__recv_loop(seq, timeout)
    buf = data[5]

    if isinstance(buf, str):  # ASCII
        return buf[:16].strip(), const.TypeCode(int(buf[16:], base=16))
    else:  # Binary
        (code,) = struct.unpack("<H", buf[16:])
        return buf[:16].decode("ascii").strip(), const.TypeCode(code)
```

**使用例**:
```python
type_name, type_code = client.read_type_name()
print(f"PLC型式: {type_name}")  # 例: "Q03UDCPU"
print(f"型式コード: {type_code}")  # 例: TypeCode.Q03UDCPU (0x268)
```

### 6.2 通信テスト（`__init__.py:820-850`）

```python
def self_test(self, data=None, timeout=0):
    """通信が正常に行えているかテスト

    :param str data: 通信テストで送る文字列（16進数のみ）
    :param int timeout: タイムアウト（250msec単位）
    :return: 正常に通信できているかどうか
    :rtype: bool
    """
    if data is None:
        data = time.strftime("%Y%m%d%H%M%S")  # デフォルト: 日時

    assert int(data, base=16), data  # 16進数チェック
    assert len(data) < 960, data

    if self.__protocol[0]:  # Binary
        body = struct.pack("<H", len(data))
    else:  # ASCII
        body = b"%04X" % len(data)
    body += data.encode("ascii")

    seq = self.__cmd_format(timeout, const.SLMPCommand.SelfTest, 0x00, body)
    ret = self.__recv_loop(seq, timeout)
    buf = ret[5]

    if isinstance(buf, str):
        return int(buf[:4], base=16) == len(data) and buf[4:] == data
    else:
        (length,) = struct.unpack("<H", buf[:2])
        body = buf[2:]
        return length == len(data) and body == data.encode("ascii")
```

**動作**: 送信したデータがそのまま返ってくることを確認（エコーバック）

### 6.3 エラークリア（`__init__.py:852-858`）

```python
def clear_error(self, timeout=0):
    """PLCのエラーをクリア"""
    self.__cmd_format(timeout, const.SLMPCommand.ClearError, 0x00, b"")
```

### 6.4 メモリ直接アクセス

#### メモリ読み出し（`__init__.py:887-930`）

```python
def memory_read(self, addr, length, timeout=0):
    """自局のメモリを読み取る

    :param int addr: 先頭アドレス
    :param int length: ワード長（最大480）
    :param int timeout: タイムアウト
    :return: 読みだしたデータ（List[bytes]）
    """
    assert 0 < length <= 480, length
    assert self.target.network == 0, self.target
    assert self.target.node == 0xFF, self.target

    if self.__protocol[0]:  # Binary
        buf = struct.pack("<IH", addr, length)
    else:  # ASCII
        buf = b"%08X%04X" % (addr, length)

    seq = self.__cmd_format(
        timeout,
        const.SLMPCommand.Memory_Read,
        0x00,
        buf
    )
    ret = self.__recv_loop(seq, timeout)
    buf = ret[5]

    # レスポンスをワードデータのリストに変換
    ret = []
    if isinstance(buf, str):
        buf = list(buf)
        buf.reverse()
        while buf:
            d1 = buf.pop()
            d2 = buf.pop()
            d3 = buf.pop()
            d4 = buf.pop()
            ret.append(bytes([int(d3+d4, base=16), int(d1+d2, base=16)]))
    else:
        buf = list(buf)
        buf.reverse()
        while buf:
            d1 = buf.pop()
            d2 = buf.pop()
            ret.append(bytes([d1, d2]))

    return ret
```

#### メモリ書き込み（`__init__.py:932-954`）

```python
def memory_write(self, addr, data, timeout=0):
    """自局のメモリに書き込む

    :param int addr: 先頭アドレス
    :param data: 書き込みデータ（List[bytes]）
    :param int timeout: タイムアウト
    """
    assert 0 < len(data) <= 480, len(data)
    assert self.target.network == 0, self.target
    assert self.target.node == 0xFF, self.target

    if self.__protocol[0]:  # Binary
        buf = struct.pack("<IH", addr, len(data))
        for v in data:
            buf += v
    else:  # ASCII
        buf = b"%08X%04X" % (addr, len(data))
        for v in data:
            buf += b"%02X%02X" % (v[1], v[0])

    self.__cmd_format(timeout, const.SLMPCommand.Memory_Write, 0x00, buf)
```

**注意**: メモリ直接アクセスは自局（network=0, node=0xFF）のみ

---

## 7. エラーハンドリング

### 7.1 エンドコード定義（`const.py:308-339`）

```python
class EndCode(enum.Enum):
    Success = 0x00                    # 正常終了
    WrongCommand = 0xC059             # コマンドエラー
    WrongFormat = 0xC05C              # フォーマットエラー
    WrongLength = 0xC061              # データ長エラー
    Busy = 0xCEE0                     # ビジー状態
    ExceedReqLength = 0xCEE1          # 要求データ長超過
    ExceedRespLength = 0xCEE2         # 応答データ長超過
    ServerNotFound = 0xCF10           # サーバー未検出
    WrongConfigItem = 0xCF20          # 設定項目エラー
    PrmIDNotFound = 0xCF30            # パラメータIDエラー
    NotStartExclusiveWrite = 0xCF31   # 排他書き込み未開始
    RelayFailure = 0xCF70             # 中継エラー
    TimeoutError = 0xCF71             # タイムアウト

    # CANアプリケーションエラー
    CANAppNotPermittedRead = 0xCCC7   # 読み出し不可
    CANAppWriteOnly = 0xCCC8          # 書き込み専用
    CANAppReadOnly = 0xCCC9           # 読み出し専用
    CANAppUndefinedObjectAccess = 0xCCCA  # 未定義オブジェクト

    # その他
    OtherNetworkError = 0xCF00        # ネットワークエラー
    DataFragmentShortage = 0xCF40     # データフラグメント不足
    DataFragmentDup = 0xCF41          # データフラグメント重複
    DataFragmentLost = 0xCF43         # データフラグメント喪失
    DataFragmentNotSupport = 0xCF44   # データフラグメント非対応
```

### 7.2 例外クラス（`util.py:334-344`）

```python
class SLMPError(Exception):
    """SLMP関連エラーの基底クラス"""
    pass

class SLMPCommunicationError(SLMPError):
    """通信エラー"""
    def __init__(self, cause):
        """
        :param EndCode cause: PLCより報告されるエラー
        """
        self.cause = cause
```

### 7.3 エラーチェック処理（`__init__.py:293-296`）

```python
def __recv_loop(self, seq: int, timeout: int):
    """応答受信とエラーチェック"""
    # ... 応答待ち処理 ...

    # エンドコードチェック
    end_code = util.EndCode(data[4])
    if end_code != util.EndCode.Success:
        raise util.SLMPCommunicationError(end_code)

    return data
```

### 7.4 エラーハンドリング例

```python
try:
    with SLMPClient("192.168.1.10", binary=True, ver=3) as client:
        values = client.read_word_devices(DeviceCode.D, 100, 10)

except util.SLMPCommunicationError as e:
    print(f"通信エラー: {e.cause}")
    if e.cause == util.EndCode.Busy:
        print("PLCがビジー状態です")
    elif e.cause == util.EndCode.TimeoutError:
        print("タイムアウトが発生しました")

except TimeoutError:
    print("応答タイムアウト")

except ConnectionError:
    print("接続エラー")
```

---

## 8. 対応PLC一覧（`const.py:192-260`）

### Qシリーズ
- **Q00J系**: Q00JCPU (0x250), Q00CPU (0x251), Q01CPU (0x252)
- **Q02-06H系**: Q02CPU (0x41), Q06HCPU (0x42), Q12HCPU (0x43), Q25HCPU (0x44)
- **Q12-25PRH系**: Q12PRHCPU (0x4B), Q25PRHCPU (0x4C)
- **Q00-01U系**: Q00UJCPU (0x260), Q00UCPU (0x261), Q01UCPU (0x262), Q02UCPU (0x263)
- **Q03-26UD系**: Q03UDCPU (0x268), Q04UDHCPU (0x269), Q06UDHCPU (0x26A),
  Q10UDHCPU (0x266), Q13UDHCPU (0x26B), Q20UDHCPU (0x267), Q26UDHCPU (0x26C)
- **QUD-V系**: Q03UDVCPU (0x366), Q04UDVCPU (0x367), Q06UDVCPU (0x368),
  Q13UDVCPU (0x36A), Q26UDVCPU (0x36C)
- **Q50-100UDEH系**: Q50UDEHCPU (0x26D), Q100UDEHCPU (0x26E)
- **QS系**: QS001CPU (0x230)

### Lシリーズ
- **L02系**: L02SCPU (0x543), L02CPU (0x541)
- **L06-26系**: L06CPU (0x544), L26CPU (0x545), L26CPU_BT (0x542)
- **L04-16H系**: L04HCPU (0x48C0), L08HCPU (0x48C1), L16HCPU (0x48C2)

### Rシリーズ
- **R00-02系**: R00CPU (0x48A0), R01CPU (0x48A1), R02CPU (0x48A2)
- **R04-120系**: R04CPU (0x4800), R08CPU (0x4801), R16CPU (0x4802),
  R32CPU (0x4803), R120CPU (0x4804)
- **REN系**: R04ENCPU (0x4805), R08ENCPU (0x4806), R16ENCPU (0x4807),
  R32ENCPU (0x4808), R120ENCPU (0x4809)
- **RP系**: R08PCPU (0x4841), R16PCPU (0x4842), R32PCPU (0x4843), R120PCPU (0x4844)
- **RPSF系**: R08PSFCPU (0x4851), R16PSFCPU (0x4852), R32PSFCPU (0x4853), R120PSFCPU (0x4854)
- **RSF系**: R08SFCPU (0x4891), R16SFCPU (0x4892), R32SFCPU (0x4893), R120SFCPU (0x4894)
- **R12CC-V系**: R12CCPU_V (0x4820)

### その他
- **MI系**: MI5122-VW (0x4E01)
- **RJ系**: RJ72GF15-T2 (0x4860), RJ72GF15-T2-D1 (0x4861), RJ72GF15-T2-D2 (0x4862)
- **LJ系**: LJ72GF15-T2 (0x0641)
- **NZ系**: NZ2GF-ETB (0x0642)

---

## 9. 実装推奨事項（andonプロジェクトへの適用）

### 9.1 採用すべき設計パターン

#### ✅ フレーム構築の柔軟性
```csharp
// PySLMPClientの柔軟な設計を参考
public class SlmpFrameBuilder
{
    private bool _isBinary;
    private int _frameVersion;  // 3 or 4

    public byte[] BuildReadRequest(
        DeviceCode deviceCode,
        int startAddress,
        int count,
        int subCommand)
    {
        if (_isBinary)
            return BuildBinaryFrame(...);
        else
            return BuildAsciiFrame(...);
    }
}
```

#### ✅ デバイスコード体系
```csharp
// Enumベースのデバイスコード管理
public enum DeviceCode : byte
{
    SM = 0x91,  // 特殊リレー
    X = 0x9C,   // 入力
    Y = 0x9D,   // 出力
    M = 0x90,   // 内部リレー
    D = 0xA8,   // データレジスタ
    // ...
}

// 16進数表現デバイスの識別
public static class DeviceCodeExtensions
{
    private static readonly HashSet<DeviceCode> HexAddressDevices = new()
    {
        DeviceCode.X, DeviceCode.Y, DeviceCode.B,
        DeviceCode.W, DeviceCode.ZR
    };

    public static bool IsHexAddress(this DeviceCode code)
        => HexAddressDevices.Contains(code);
}
```

#### ✅ マルチスレッド受信処理
```csharp
// バックグラウンドで受信処理
public class SlmpClient : IDisposable
{
    private readonly ConcurrentDictionary<int, ResponseData> _receiveQueue;
    private readonly CancellationTokenSource _cancellationToken;
    private Task _receiveTask;

    public void Open()
    {
        _receiveTask = Task.Run(ReceiveWorkerAsync, _cancellationToken.Token);
    }

    private async Task ReceiveWorkerAsync()
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            try
            {
                var response = await ReceiveAsync();
                _receiveQueue[response.SequenceNumber] = response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "受信エラー");
            }
        }
    }
}
```

#### ✅ シーケンス管理（4Eフレーム）
```csharp
public class SequenceManager
{
    private int _currentSequence = 0;
    private readonly object _lock = new();

    public int GetNextSequence()
    {
        lock (_lock)
        {
            if (_currentSequence > 255)
                _currentSequence = 0;
            return _currentSequence++;
        }
    }
}
```

#### ✅ ビット処理ユーティリティ
```csharp
public static class BitProcessor
{
    // ワード値からビット配列に展開（LSB first）
    public static bool[] UnpackBits(ushort wordValue)
    {
        var bits = new bool[16];
        for (int i = 0; i < 16; i++)
        {
            bits[i] = ((wordValue >> i) & 1) == 1;
        }
        return bits;
    }

    // ビット配列からワード値にパック
    public static ushort PackBits(bool[] bits)
    {
        ushort result = 0;
        for (int i = 0; i < Math.Min(16, bits.Length); i++)
        {
            if (bits[i])
                result |= (ushort)(1 << i);
        }
        return result;
    }
}
```

### 9.2 改善推奨点

#### ⚠️ タイムアウト処理の強化
PySLMPClientは単純なループ待機だが、C#では非同期処理を活用:

```csharp
public async Task<ResponseData> WaitResponseAsync(
    int sequenceNumber,
    TimeSpan timeout)
{
    using var cts = new CancellationTokenSource(timeout);

    while (!cts.Token.IsCancellationRequested)
    {
        if (_receiveQueue.TryRemove(sequenceNumber, out var response))
        {
            // エンドコードチェック
            if (response.EndCode != 0x0000)
                throw new SlmpCommunicationException(response.EndCode);

            return response;
        }

        await Task.Delay(10, cts.Token);
    }

    throw new TimeoutException($"Sequence {sequenceNumber} timeout");
}
```

#### ⚠️ エラー詳細情報の保持
```csharp
public class SlmpCommunicationException : Exception
{
    public ushort EndCode { get; }
    public string EndCodeDescription { get; }

    public SlmpCommunicationException(ushort endCode)
        : base($"SLMP通信エラー: 0x{endCode:X4} ({GetDescription(endCode)})")
    {
        EndCode = endCode;
        EndCodeDescription = GetDescription(endCode);
    }

    private static string GetDescription(ushort code)
    {
        return code switch
        {
            0x0000 => "正常",
            0xC059 => "コマンドエラー",
            0xC05C => "フォーマットエラー",
            0xC061 => "データ長エラー",
            0xCEE0 => "ビジー状態",
            0xCF71 => "タイムアウト",
            _ => "不明なエラー"
        };
    }
}
```

#### ⚠️ ロギングの充実
```csharp
public class SlmpClient
{
    private readonly ILogger<SlmpClient> _logger;

    private async Task<byte[]> SendCommandAsync(byte[] frame)
    {
        // 送信ログ（16進ダンプ）
        _logger.LogDebug("送信: {Frame}",
            BitConverter.ToString(frame).Replace("-", " "));

        await _socket.SendAsync(frame, SocketFlags.None);

        var response = await ReceiveAsync();

        // 受信ログ
        _logger.LogDebug("受信: {Response}",
            BitConverter.ToString(response).Replace("-", " "));

        return response;
    }
}
```

### 9.3 C#実装のクラス構成案

```csharp
// 名前空間構成
namespace Andon.Slmp
{
    // 定数・列挙型
    public enum DeviceCode : byte { ... }
    public enum SlmpCommand : ushort { ... }
    public enum EndCode : ushort { ... }

    // コア機能
    public class SlmpClient : IDisposable
    {
        public async Task OpenAsync();
        public void Close();
        public async Task<ushort[]> ReadWordDevicesAsync(...);
        public async Task<bool[]> ReadBitDevicesAsync(...);
        public async Task WriteWordDevicesAsync(...);
        public async Task WriteBitDevicesAsync(...);
    }

    // フレーム構築
    public class SlmpFrameBuilder
    {
        public byte[] BuildReadRequest(...);
        public byte[] BuildWriteRequest(...);
        private byte[] BuildBinaryFrame(...);
        private byte[] BuildAsciiFrame(...);
    }

    // フレーム解析
    public class SlmpFrameParser
    {
        public ResponseData ParseBinaryResponse(byte[] data);
        public ResponseData ParseAsciiResponse(byte[] data);
    }

    // ユーティリティ
    public static class BitProcessor
    {
        public static bool[] UnpackBits(ushort wordValue);
        public static ushort PackBits(bool[] bits);
    }

    // データ型
    public record struct Target(byte Network, byte Node,
                                ushort DstProc, byte MultiDrop);
    public record struct ResponseData(int Sequence, ushort EndCode,
                                      byte[] Data);

    // 例外
    public class SlmpException : Exception { }
    public class SlmpCommunicationException : SlmpException
    {
        public ushort EndCode { get; }
    }
}
```

---

## 10. 重要な技術的知見

### 10.1 バイトオーダー

#### デバイス番号
- **形式**: リトルエンディアン
- **サイズ**: 3バイト
- **例**: D500 (10進: 500, 16進: 0x01F4)
  ```
  バイト配列: [0xF4, 0x01, 0x00]
             ↑下位  ↑中位  ↑上位
  ```

#### ワードデータ
- **形式**: リトルエンディアン
- **サイズ**: 2バイト
- **例**: 0x1234 → `[0x34, 0x12]`

### 10.2 ビット順序

#### ワード内ビット配置
- **順序**: LSB first（下位ビットが先）
- **例**: 0x0105 (0b0000000100000101)
  ```
  bit15 ... bit8  bit7 ... bit0
    0         1     0       1

  配列展開: [1,0,1,0,0,0,0,0, 1,0,0,0,0,0,0,0]
            ↑bit0           ↑bit8
  ```

### 10.3 データ長計算

#### 要求電文のデータ長
- **対象**: コマンド(2) + サブコマンド(2) + データ部の合計
- **公式**: `len(frame[コマンド以降])`
- **格納**: バイト11-12（リトルエンディアン）

**例**:
```
コマンド部（6byte）:
  [cmd(2)] [sub_cmd(2)] [device_addr(3)] [device_code(1)] [count(2)]
  = 2 + 2 + 6 = 10byte

データ長フィールド値: 10 (0x000A)
バイト配列: [0x0A, 0x00]
```

### 10.4 フレームサイズ制限

- **最大フレームサイズ**: 8194バイト
- **内訳**:
  - ヘッダ: 6byte (4E) or 2byte (3E)
  - コマンド部: 15byte
  - データ部: 最大8173byte (4E) or 8177byte (3E)

### 10.5 ビットデバイスの扱い

#### 読み出し単位
- **基本単位**: 1ビット単位で指定可能
- **転送単位**: 16点単位（2バイト）に丸められる
- **例**: M100を3点読み出し → 実際は16点（M100-M115）読み出し

#### BCDパック
- **ビット書き込み時**: 2点ずつBCDパック
  ```
  [bit0, bit1] → 1バイト
  0,0 → 0x00
  1,0 → 0x10
  0,1 → 0x01
  1,1 → 0x11
  ```

### 10.6 監視タイマ

- **単位**: 250msec
- **範囲**: 0-65535 (0-16383.75秒)
- **0指定時**: タイムアウトなし（推奨しない）
- **推奨値**: 32 (8秒) 程度

### 10.7 デバイス番号変換

#### 10進数デバイス（M, D等）
```python
# Python例
number = 100
bytes_array = number.to_bytes(3, 'little')  # [0x64, 0x00, 0x00]
```

```csharp
// C#例
int number = 100;
byte[] bytes = new byte[3];
bytes[0] = (byte)(number & 0xFF);
bytes[1] = (byte)((number >> 8) & 0xFF);
bytes[2] = (byte)((number >> 16) & 0xFF);
```

#### 16進数デバイス（X, Y等）
```python
# Python例
number_hex = "40"  # Y40
number_int = int(number_hex, 16)  # 0x40 = 64
bytes_array = number_int.to_bytes(3, 'little')  # [0x40, 0x00, 0x00]
```

```csharp
// C#例
string numberHex = "40";
int numberInt = Convert.ToInt32(numberHex, 16);  // 64
byte[] bytes = new byte[3];
bytes[0] = (byte)(numberInt & 0xFF);
bytes[1] = (byte)((numberInt >> 8) & 0xFF);
bytes[2] = (byte)((numberInt >> 16) & 0xFF);
```

---

## 11. テスト実施時の推奨手順

### ステップ1: 最小構成での接続テスト

```python
# 最も単純な構成
client = SLMPClient(
    addr="192.168.1.10",
    port=5000,
    binary=True,   # バイナリ形式
    ver=3,         # 3Eフレーム
    tcp=True       # TCP接続
)

with client:
    # 通信テスト
    result = client.self_test()
    print(f"通信テスト: {'成功' if result else '失敗'}")

    # 形名読み出し
    type_name, type_code = client.read_type_name()
    print(f"PLC型式: {type_name} ({type_code})")
```

### ステップ2: 単一デバイス読み出し

```python
with client:
    # Dデバイス1点読み出し
    values = client.read_word_devices(DeviceCode.D, 100, 1)
    print(f"D100: {values[0]}")

    # Mデバイス16点読み出し
    bits = client.read_bit_devices(DeviceCode.M, 0, 16)
    print(f"M0-M15: {bits}")
```

### ステップ3: 複数デバイス・混在テスト

```python
with client:
    # ワード10点
    words = client.read_word_devices(DeviceCode.D, 100, 10)

    # ビット32点
    bits = client.read_bit_devices(DeviceCode.M, 100, 32)

    # ランダムアクセス
    word_list = [(DeviceCode.D, 100), (DeviceCode.D, 200)]
    dword_list = [(DeviceCode.D, 1000)]
    word_data, dword_data = client.read_random_devices(word_list, dword_list)
```

### ステップ4: 書き込みテスト

```python
with client:
    # ワード書き込み
    client.write_word_devices(DeviceCode.D, 100, [1234, 5678])

    # ビット書き込み
    client.write_bit_devices(DeviceCode.M, 100, [1, 0, 1, 1])
```

### ステップ5: エラーハンドリングテスト

```python
try:
    with client:
        # 存在しないデバイスへのアクセス
        values = client.read_word_devices(DeviceCode.D, 999999, 1)

except util.SLMPCommunicationError as e:
    print(f"エラーコード: 0x{e.cause.value:04X}")

except TimeoutError:
    print("タイムアウト発生")
```

### デバッグ用ログ出力

```python
import logging

# ロギング設定
logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s [%(levelname)s] %(name)s: %(message)s'
)

client = SLMPClient("192.168.1.10", binary=True, ver=3, tcp=True)
client.logger.setLevel(logging.DEBUG)

with client:
    values = client.read_word_devices(DeviceCode.D, 100, 10)
```

---

## 12. andonプロジェクトとの差異分析

### 12.1 PySLMPClientが優れている点

#### ✅ 柔軟な通信形態対応
- バイナリ/ASCII、3E/4E、TCP/UDPの全組み合わせに対応
- andon: バイナリ・3Eフレーム・UDPのみ（現状）

#### ✅ 豊富なデバイスアクセス方法
- 連続、ランダム、ブロック、モニタの4方式
- andon: 連続読み出しのみ（現状）

#### ✅ システム機能
- 形名読み出し、通信テスト、エラークリア等
- andon: データ取得機能に特化

#### ✅ エラーハンドリング
- 40種類以上のエンドコード定義
- andon: 基本的なエラー処理のみ

### 12.2 andonプロジェクトが優れている点

#### ✅ C#/.NET実装
- 型安全性、パフォーマンス、保守性
- PySLMPClient: Pythonの動的型付け

#### ✅ ロギング機能
- Serilog統合、構造化ログ、ファイル/コンソール出力
- PySLMPClient: 基本的なloggingモジュール

#### ✅ 設定管理
- JSON設定ファイル、環境別設定
- PySLMPClient: コード内設定

#### ✅ ビジネスロジック統合
- データ変換、CSV出力、エラー通知
- PySLMPClient: 通信ライブラリとしての機能のみ

### 12.3 移植優先度

| 機能 | 優先度 | 理由 |
|------|--------|------|
| 3E/4Eフレーム対応 | 高 | PLC環境の柔軟性向上 |
| TCP対応 | 高 | 信頼性の高い通信 |
| ランダムアクセス | 中 | 不連続デバイス取得の効率化 |
| モニタ機能 | 中 | 定期読み出しの効率化 |
| ASCII形式対応 | 低 | デバッグ用途のみ |
| ブロックアクセス | 低 | 連続アクセスで代替可能 |

---

## 13. 実装例：C#への移植サンプル

### 13.1 基本的な読み出し処理

```csharp
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public class SlmpClient : IDisposable
{
    private readonly Socket _socket;
    private readonly IPEndPoint _endpoint;
    private readonly bool _isBinary;
    private readonly int _frameVersion;
    private int _sequence = 0;

    public SlmpClient(string ipAddress, int port = 5000,
                     bool binary = true, int frameVersion = 3)
    {
        _endpoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        _isBinary = binary;
        _frameVersion = frameVersion;
        _socket = new Socket(AddressFamily.InterNetwork,
                            SocketType.Dgram, ProtocolType.Udp);
    }

    public async Task<ushort[]> ReadWordDevicesAsync(
        DeviceCode deviceCode,
        int startAddress,
        int count,
        int timeout = 0)
    {
        // フレーム構築
        var frame = BuildReadWordDeviceFrame(deviceCode, startAddress, count, timeout);

        // 送信
        await _socket.SendToAsync(frame, SocketFlags.None, _endpoint);

        // 受信
        var buffer = new byte[1024];
        var received = await _socket.ReceiveAsync(buffer, SocketFlags.None);

        // パース
        return ParseWordDeviceResponse(buffer, received, count);
    }

    private byte[] BuildReadWordDeviceFrame(
        DeviceCode deviceCode,
        int startAddress,
        int count,
        int timeout)
    {
        var frame = new List<byte>();

        // ヘッダ（3Eフレーム）
        frame.AddRange(new byte[] { 0x50, 0x00 });

        // コマンド部
        frame.Add(0x00);  // ネットワーク番号
        frame.Add(0xFF);  // 局番
        frame.AddRange(BitConverter.GetBytes((ushort)0x03FF));  // I/O番号（LE）
        frame.Add(0x00);  // マルチドロップ

        // データ長（後で設定）
        int dataLengthPos = frame.Count;
        frame.AddRange(new byte[] { 0x00, 0x00 });

        // 監視タイマ
        frame.AddRange(BitConverter.GetBytes((ushort)timeout));

        // コマンド（Device_Read: 0x0401）
        frame.AddRange(BitConverter.GetBytes((ushort)0x0401));

        // サブコマンド（ワード単位: 0x0000）
        frame.AddRange(BitConverter.GetBytes((ushort)0x0000));

        // デバイスアドレス（3バイト、LE）
        frame.Add((byte)(startAddress & 0xFF));
        frame.Add((byte)((startAddress >> 8) & 0xFF));
        frame.Add((byte)((startAddress >> 16) & 0xFF));

        // デバイスコード
        frame.Add((byte)deviceCode);

        // 点数
        frame.AddRange(BitConverter.GetBytes((ushort)count));

        // データ長を計算して設定
        int dataLength = frame.Count - dataLengthPos - 2 + 2;  // コマンド以降
        frame[dataLengthPos] = (byte)(dataLength & 0xFF);
        frame[dataLengthPos + 1] = (byte)((dataLength >> 8) & 0xFF);

        return frame.ToArray();
    }

    private ushort[] ParseWordDeviceResponse(byte[] buffer, int length, int count)
    {
        // レスポンスヘッダチェック（簡略版）
        if (buffer[0] != 0xD0 || buffer[1] != 0x00)
            throw new Exception("Invalid response header");

        // エンドコードチェック（バイト9-10）
        ushort endCode = BitConverter.ToUInt16(buffer, 9);
        if (endCode != 0x0000)
            throw new SlmpCommunicationException(endCode);

        // データ部抽出（バイト11以降）
        var result = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = BitConverter.ToUInt16(buffer, 11 + i * 2);
        }

        return result;
    }

    public void Dispose()
    {
        _socket?.Dispose();
    }
}

// 使用例
public async Task Example()
{
    using var client = new SlmpClient("192.168.1.10", binary: true, frameVersion: 3);

    var values = await client.ReadWordDevicesAsync(DeviceCode.D, 100, 10);

    foreach (var value in values)
    {
        Console.WriteLine($"Value: {value}");
    }
}
```

---

## 14. まとめ

### PySLMPClientの優れた点

- ✅ **Pure Python実装**: プラットフォーム非依存、導入容易
- ✅ **完全なSLMP対応**: バイナリ/ASCII、3E/4E、TCP/UDPの全組み合わせ
- ✅ **豊富なデバイスアクセス方法**: 連続、ランダム、ブロック、モニタ
- ✅ **非同期受信処理**: マルチスレッドで効率的な通信
- ✅ **包括的なエラーハンドリング**: 40種類以上のエンドコード対応
- ✅ **システム機能**: 形名読み出し、通信テスト、メモリアクセス
- ✅ **ビット処理ユーティリティ**: pack/unpack、BCD変換

### andonプロジェクトへの移植価値

#### 高優先度（即時移植推奨）
- **フレーム構築ロジック**: バイナリ/ASCIIの両対応
- **デバイスコード体系**: Enumベースの定義
- **エンドコード処理**: 詳細なエラー情報
- **ビット処理**: pack/unpackアルゴリズム

#### 中優先度（段階的導入）
- **4Eフレーム対応**: シーケンス管理機能
- **TCP対応**: 信頼性の高い通信
- **ランダムアクセス**: 不連続デバイス取得
- **モニタ機能**: 定期読み出し効率化

#### 低優先度（必要に応じて）
- **ASCII形式**: デバッグ用途
- **ブロックアクセス**: 連続アクセスで代替可
- **メモリ直接アクセス**: 特殊用途

### C#実装時の推奨アプローチ

1. **段階的実装**: 3Eバイナリ → 4Eバイナリ → ASCII形式の順
2. **非同期処理**: async/awaitベースの設計
3. **ロギング強化**: 送受信フレームの16進ダンプ
4. **型安全性**: Enumと構造体の活用
5. **テスト駆動**: 各機能の単体テスト実装

---

## 15. 参照情報

### ファイルパス
- **PySLMPClient**: `C:\Users\1010821\Desktop\python\andon\PySLMPClient\`
- **メインクラス**: `pyslmpclient\__init__.py`
- **定数定義**: `pyslmpclient\const.py`
- **ユーティリティ**: `pyslmpclient\util.py`

### 関連ドキュメント
- **SLMP仕様書**: 三菱電機公式ドキュメント
- **GitHub**: https://github.com/（PySLMPClientのリポジトリ）

### 検証環境
- **Python**: 3.x
- **依存ライブラリ**: numpy（ビット処理用）
- **対応PLC**: Qシリーズ、Lシリーズ、Rシリーズ

---

**分析完了**
