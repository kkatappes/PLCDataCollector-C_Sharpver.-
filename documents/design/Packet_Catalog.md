# 例示的パケットカタログ（Golden Samples）

## デバッグ・相互運用性確認のためのパケット標本集

### 基本フレーム構造

#### 3E Binary フレーム（ST型）

**ワードデバイス読み取り要求**
```
コマンド: ReadWordDevices (DeviceCode.D, startAddress=0, count=3)

hex: 50 00 01 01 01 00 01 0C 00 06 00 01 04 00 00 64 00 00 A8 03 00
     ^^^^^ ^^^^^^^^^ ^^^^^ ^^^^^ ^^^^^ ^^^^^ ^^^^^ ^^^^^ ^^^^^ ^^^^^
     │    │         │    │    │    │    │    │    │    └─ count (3)
     │    │         │    │    │    │    │    │    └─ device code (0xA8 = D) 
     │    │         │    │    │    │    │    └─ start address (100, 3byte)
     │    │         │    │    │    │    └─ sub command (0x0000)
     │    │         │    │    │    └─ command (0x0401)
     │    │         │    │    └─ timer (6 = 1.5sec)
     │    │         │    └─ data length (12 bytes)
     │    │         └─ m_drop (1)
     │    └─ network(1), node(1), io_proc(0x0001)
     └─ sub header (0x5000)

ASCII: "500001010100010C000600010400006400000A8030000"
```

**ワードデバイス読み取り応答**
```
正常応答 (D0=0x1234, D1=0x5678, D2=0x9ABC):

hex: D0 00 01 01 01 00 01 08 00 00 00 34 12 78 56 BC 9A
     ^^^^^ ^^^^^^^^^ ^^^^^ ^^^^^ ^^^^^ ^^^^^ ^^^^^ ^^^^^
     │    │         │    │    │    │    │    └─ D2 (0x9ABC)
     │    │         │    │    │    │    └─ D1 (0x5678) 
     │    │         │    │    │    └─ D0 (0x1234)
     │    │         │    │    └─ end code (0x0000 = Success)
     │    │         │    └─ data length (8 bytes)
     │    │         └─ m_drop (1)
     │    └─ network(1), node(1), io_proc(0x0001)
     └─ response header (0xD000)

ASCII: "D00001010100010800000034125678BC9A"
```

#### 4E Binary フレーム（MT型）

**ビットデバイス読み取り要求**
```
コマンド: ReadBitDevices (DeviceCode.M, startAddress=100, count=8)

hex: 54 00 7B 00 00 00 01 01 01 00 01 0C 00 06 00 01 04 01 00 64 00 00 90 08 00
     ^^^^^ ^^^^^ ^^^^^ ^^^^^^^^^ ^^^^^ ^^^^^ ^^^^^ ^^^^^ ^^^^^ ^^^^^ ^^^^^ ^^^^^
     │    │    │    │         │    │    │    │    │    │    │    └─ count (8)
     │    │    │    │         │    │    │    │    │    │    └─ device code (0x90 = M)
     │    │    │    │         │    │    │    │    │    └─ start address (100, 3byte)
     │    │    │    │         │    │    │    │    └─ sub command (0x0001)
     │    │    │    │         │    │    │    └─ command (0x0401)
     │    │    │    │         │    │    └─ timer (6 = 1.5sec)
     │    │    │    │         │    └─ data length (12 bytes)
     │    │    │    │         └─ m_drop (1)
     │    │    │    └─ network(1), node(1), io_proc(0x0001)
     │    │    └─ reserved (0x0000)
     │    └─ sequence (123)
     └─ sub header (0x5400)

ASCII: "5400007B000001010100010C00060001040100640000900800"
```

**ビットデバイス読み取り応答**
```
正常応答 (M100-M107 = 10110011):

hex: D4 00 7B 00 00 00 01 01 01 00 01 04 00 00 00 11 0C
     ^^^^^ ^^^^^ ^^^^^ ^^^^^^^^^ ^^^^^ ^^^^^ ^^^^^ ^^^^^
     │    │    │    │         │    │    │    └─ bit data (0x11 0x0C = 10110011)
     │    │    │    │         │    │    └─ end code (0x0000 = Success)
     │    │    │    │         │    └─ data length (4 bytes)
     │    │    │    │         └─ m_drop (1)
     │    │    │    └─ network(1), node(1), io_proc(0x0001)
     │    │    └─ reserved (0x0000)
     │    └─ sequence (123)
     └─ response header (0xD400)

ASCII: "D4007B00000001010100010400000011 0C"
```

## 代表コマンドパケット集

### 1. デバイス書き込み（WriteBitDevices）

```
コマンド: WriteBitDevices (DeviceCode.M, startAddress=200, data=[true,false,true,false])

要求 (3E Binary):
hex: 50 00 01 01 01 00 01 0E 00 06 00 01 14 01 00 C8 00 00 90 04 00 10 00 10 00
     ────────────────────── ────── ────── ────────── ────── ─────── ─────────────
     ヘッダ部分              長さ   タイマ  コマンド    アドレス データ   ビット値

ASCII: "50000101010001 0E000600011401 00C8000090040010001000"

応答 (3E Binary):
hex: D0 00 01 01 01 00 01 02 00 00 00
     ────────────────────── ────── ──────
     ヘッダ部分              長さ   終了コード

ASCII: "D000010101000102000000"
```

### 2. ランダム読み取り（ReadRandomDevices）

```
コマンド: ReadRandomDevices
- wordDevices: [(D,0), (D,10), (TN,0)]  
- dwordDevices: [(D,1000), (CN,0)]

要求 (4E Binary):
hex: 54 00 3C 00 00 00 01 01 01 00 01 1A 00 06 00 03 04 00 00 03 02
     00 00 00 A8 0A 00 00 A8 00 00 00 C2        # Word devices  
     E8 03 00 A8 00 00 00 C5                    # DWord devices
     
     シーケンス=60, データ長=26bytes
     ワード3点: D0, D10, TN0
     ダブルワード2点: D1000, CN0

応答 (4E Binary):  
hex: D4 00 3C 00 00 00 01 01 01 00 01 10 00 00 00
     34 12 78 56 AB CD                          # Word data: 0x1234, 0x5678, 0xCDAB
     12 34 56 78 9A BC DE F0                    # DWord data: 0x78563412, 0xF0DEBC9A
```

### 3. ブロック読み取り（ReadBlock）

```
コマンド: ReadBlock
- wordBlocks: [(D,0,4), (TN,0,2)]
- bitBlocks: [(M,100,16)]

要求 (3E Binary):
hex: 50 00 01 01 01 00 01 17 00 06 00 06 04 00 00 02 01
     00 00 00 A8 04 00                          # D0から4点
     00 00 00 C2 02 00                          # TN0から2点  
     64 00 00 90 10 00                          # M100から16点

応答 (3E Binary):
hex: D0 00 01 01 01 00 01 16 00 00 00
     12 34 56 78 9A BC DE F0                    # D0-D3 (4 words)
     11 22 33 44                                # TN0-TN1 (2 words)
     5A A5 C3 3C                                # M100-M115 (16 bits packed)
```

### 4. モニタ機能（EntryMonitorDevice + ExecuteMonitor）

```
モニタ登録:
要求 (4E Binary):
hex: 54 00 45 00 00 00 01 01 01 00 01 10 00 06 00 01 08 00 00 02 01
     00 00 00 A8 0A 00 00 A8                    # D0, D10
     E8 03 00 A8                                # D1000

応答:
hex: D4 00 45 00 00 00 01 01 01 00 01 02 00 00 00

モニタ実行:
要求 (4E Binary):  
hex: 54 00 46 00 00 00 01 01 01 00 01 06 00 06 00 02 08 00 00

応答 (最新値):
hex: D4 00 46 00 00 00 01 01 01 00 01 0A 00 00 00
     AA BB CC DD                                # D0, D10 (word data)
     11 22 33 44                                # D1000 (dword data)
```

### 5. システム機能（ReadTypeName）

```
型名取得:
要求 (3E Binary):
hex: 50 00 01 01 01 00 01 06 00 06 00 01 01 00 00

応答 (Q02UCPU):
hex: D0 00 01 01 01 00 01 14 00 00 00
     51 30 32 55 43 50 55 20 20 20 20 20 20 20 20 20  # "Q02UCPU         "
     63 02                                            # TypeCode: 0x0263

ASCII応答:
"D00001010100011400000051303255435055202020202020202020206302"
```

### 6. セルフテスト（SelfTest）

```
セルフテスト (データ="ABCDEF"):
要求 (4E Binary):
hex: 54 00 50 00 00 00 01 01 01 00 01 0C 00 06 00 19 06 00 00 06 00
     41 42 43 44 45 46                          # "ABCDEF"

応答:
hex: D4 00 50 00 00 00 01 01 01 00 01 0C 00 00 00 06 00
     41 42 43 44 45 46                          # Echo back "ABCDEF"
```

## エラー応答の標本

### 1. コマンドエラー（WrongCommand）

```
未対応コマンド送信時:
要求: 50 00 01 01 01 00 01 06 00 06 00 FF FF 00 00  # 0xFFFF = 未定義コマンド

応答:
hex: D0 00 01 01 01 00 01 02 00 59 C0
     ────────────────────── ────── ──────
     ヘッダ部分              長さ   エラーコード(0xC059)

ASCII: "D000010101000102005C0"
```

### 2. フォーマットエラー（WrongFormat）

```
不正なデータ長:
要求: 50 00 01 01 01 00 01 FF 00 06 00 01 04 00 00  # データ長不正

応答:  
hex: D0 00 01 01 01 00 01 02 00 5C C0
     エラーコード: 0xC05C (WrongFormat)
```

### 3. ビジー状態（Busy）

```
PLC処理中:
応答:
hex: D0 00 01 01 01 00 01 02 00 E0 CE
     エラーコード: 0xCEE0 (Busy)
```

### 4. タイムアウト（TimeoutError）

```
PLC側タイムアウト:
応答:
hex: D0 00 01 01 01 00 01 02 00 71 CF  
     エラーコード: 0xCF71 (TimeoutError)
```

### 5. デバイス範囲外（ExceedReqLength）

```
要求点数過多:
要求: ReadWordDevices (count=4000) # 上限超過

応答:
hex: D0 00 01 01 01 00 01 02 00 E1 CE
     エラーコード: 0xCEE1 (ExceedReqLength)
```

## テスト用固定パケットファイル

### binary_frames.dat
```
# 3E Binary Frame Samples
# Format: <description>|<hex_data>
Word Read Request D0-D2|5000010101000C000600010400006400000A80300
Word Read Response 1234,5678,9ABC|D00001010100010800000034125678BC9A
Bit Read Request M100-M107|50000101010001 0C00060001040100640000900800
Bit Read Response 10110011|D0000101010001040000001 1 0C
Error Response Wrong Command|D000010101000102005C0
Error Response Busy|D000010101000102000E0CE
```

### ascii_frames.dat  
```
# 3E ASCII Frame Samples
# Format: <description>|<ascii_data>
Word Read Request D0-D2|5000010101000C000600010400006400000A803000
Word Read Response|D00001010100010800000034125678BC9A
Type Name Request|50000101010006000600010100000
Type Name Response Q02UCPU|D00001010100011400000051303255435055202020202020202020206302
```

### error_samples.dat
```
# Error Response Samples
# Format: <end_code>|<description>|<hex_response>
0000|Success|D000010101000102000000
C059|Wrong Command|D000010101000102005C0  
C05C|Wrong Format|D000010101000102005CC0
C061|Wrong Length|D00001010100010200061C0
CEE0|Busy|D000010101000102000E0CE
CEE1|Exceed Request Length|D000010101000102000E1CE
CEE2|Exceed Response Length|D000010101000102000E2CE
CF71|Timeout Error|D00001010100010200071CF
```

## パケット検証ツール

### C# 実装例
```csharp
public class PacketValidator
{
    public static bool ValidateFrame(byte[] frame, FrameType expectedType)
    {
        if (frame.Length < 11) return false;
        
        // ヘッダ検証
        var header = BinaryPrimitives.ReadUInt16LittleEndian(frame);
        return expectedType switch
        {
            FrameType.Request3E => header == 0x5000,
            FrameType.Response3E => header == 0xD000,
            FrameType.Request4E => header == 0x5400,
            FrameType.Response4E => header == 0xD400,
            _ => false
        };
    }
    
    public static string DumpFrame(byte[] frame)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Frame Length: {frame.Length}");
        sb.AppendLine($"Header: 0x{BinaryPrimitives.ReadUInt16LittleEndian(frame):X4}");
        
        if (frame.Length >= 11)
        {
            sb.AppendLine($"Network: {frame[2]}");
            sb.AppendLine($"Node: {frame[3]}");
            sb.AppendLine($"IO Proc: 0x{BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(4)):X4}");
            sb.AppendLine($"M Drop: {frame[6]}");
            sb.AppendLine($"Data Length: {BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(7))}");
        }
        
        sb.AppendLine($"Hex Dump: {Convert.ToHexString(frame)}");
        return sb.ToString();
    }
}
```

### Python 互換性検証スクリプト
```python
def compare_with_python_output(c_sharp_frame, python_frame):
    """C#実装とPython実装のフレームを比較"""
    if c_sharp_frame == python_frame:
        print("✅ Frame matches exactly")
        return True
    
    # バイト単位での差分表示
    print("❌ Frame mismatch:")
    for i, (c, p) in enumerate(zip(c_sharp_frame, python_frame)):
        if c != p:
            print(f"  Byte {i}: C#=0x{c:02X}, Python=0x{p:02X}")
    
    return False
```

## Wireshark ディスセクタ設定

### SLMP プロトコル定義
```lua
-- SLMP Protocol Dissector for Wireshark
slmp_proto = Proto("SLMP", "SLMP Protocol")

local f_subheader = ProtoField.uint16("slmp.subheader", "Sub Header", base.HEX)
local f_network = ProtoField.uint8("slmp.network", "Network", base.DEC)
local f_node = ProtoField.uint8("slmp.node", "Node", base.DEC)  
local f_command = ProtoField.uint16("slmp.command", "Command", base.HEX)
local f_endcode = ProtoField.uint16("slmp.endcode", "End Code", base.HEX)

slmp_proto.fields = {f_subheader, f_network, f_node, f_command, f_endcode}

function slmp_proto.dissector(buffer, pinfo, tree)
    length = buffer:len()
    if length == 0 then return end
    
    pinfo.cols.protocol = slmp_proto.name
    local subtree = tree:add(slmp_proto, buffer(), "SLMP Protocol Data")
    
    -- Sub Header
    subtree:add_le(f_subheader, buffer(0,2))
    local subheader = buffer(0,2):le_uint()
    
    if subheader == 0x5000 or subheader == 0x5400 then
        pinfo.cols.info:set("SLMP Request")
        -- Parse request frame
        subtree:add(f_network, buffer(2,1))
        subtree:add(f_node, buffer(3,1))
        if length > 15 then
            subtree:add_le(f_command, buffer(15,2))
        end
    elseif subheader == 0xD000 or subheader == 0xD400 then  
        pinfo.cols.info:set("SLMP Response")
        -- Parse response frame
        subtree:add(f_network, buffer(2,1))
        subtree:add(f_node, buffer(3,1))
        if length > 9 then
            subtree:add_le(f_endcode, buffer(9,2))
        end
    end
end

local tcp_port = DissectorTable.get("tcp.port")
tcp_port:add(5000, slmp_proto)
```

これらのパケット標本により、SlmpClientの実装検証と他システムとの相互運用性確認を効率的に行えます。