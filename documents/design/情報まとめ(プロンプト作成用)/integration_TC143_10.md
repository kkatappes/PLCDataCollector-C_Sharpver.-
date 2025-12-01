# Integration Step3-6 M100-M107ビット読み出し4パターン統合テスト情報（TC143_10）

## ドキュメント概要

### 目的
このドキュメントは、TC143_10_Step3-6_M100～M107ビット読み出し4パターン統合テストの実装に必要な情報を集約したものです。
**コード作成時に必要となる技術情報のみ**を記載しており、学習資料や説明的な内容は含みません。

### 情報取得元
本ドキュメントの情報は以下のソースから抽出・統合されています：

#### 設計書（andon/documents/design/）
- `クラス・メソッドリスト.md` - クラス・メソッドの一覧と概要
- `クラス設計.md` - 詳細なクラス設計仕様
- `テスト内容.md` - テストケース仕様
- `プロジェクト構造設計.md` - フォルダ構造・プロジェクト構成
- `依存関係.md` - クラス間の依存関係
- `各ステップio.md` - 各ステップのInput/Output詳細

#### 既存テストケース実装情報
- `step3_TC017.md` - TCP接続テスト（Step3）
- `step3_TC021.md` - 送信テスト（Step4）
- `step3_TC025.md` - 受信テスト（Step4）
- `step3_TC027.md` - 切断テスト（Step5）
- `step6_TC029.md` - 基本後処理テスト（Step6-1）
- `step6_TC032.md` - DWord結合テスト（Step6-2）
- `step6_TC037.md` - 3Eフレーム解析テスト（Step6-3）
- `step6_TC038.md` - 4Eフレーム解析テスト（Step6-3）
- `integration_TC121.md` - FullCycle完全実行テスト

#### 実装参考（PySLMPClient）
- `PySLMPClient/pyslmpclient/const.py` - SLMP定数・列挙型定義
- `PySLMPClient/pyslmpclient/__init__.py` - SLMPクライアント実装
- `PySLMPClient/pyslmpclient/util.py` - フレーム作成ユーティリティ
- `PySLMPClient/tests/test_main.py` - テストケース実例

---

## 1. テスト対象機能仕様

### M100～M107ビット読み出し統合テスト
**統合テスト対象**: PlcCommunicationManager全体（Step3-6）
**名前空間**: andon.Core.Managers

#### テストの特徴
- **オフラインテスト**: 実際のPLC機器不要（MockPlcServerを使用）
- **対象デバイス**: M100～M107（8ビット）
- **テストパターン数**: 4パターン（3E/4E × バイナリ/ASCII）
- **検証範囲**: Step3（接続）→Step4（送受信）→Step5（切断）→Step6（データ処理・構造化）

#### 完全サイクル構成
```
Step3: PLC接続処理（ConnectAsync）
  ↓
Step4: PLCリクエスト送信（SendFrameAsync）
  ↓
Step4: PLCデータ受信（ReceiveResponseAsync）
  ↓
Step5: PLC切断処理（DisconnectAsync）
  ↓
Step6-1: 受信データ基本後処理（ProcessReceivedRawData）
  ↓
Step6-2: DWord結合処理（CombineDwordData） ※ビットデバイスのため実質スキップ
  ↓
Step6-3: 構造化データ変換（ParseRawToStructuredData）
```

#### 統合Input
**各パターン共通**:
- 対象デバイス: M100～M107（8ビット）
- デバイス開始アドレス: 100 (0x64)
- デバイス点数: 8

**パターン別設定**:
1. **パターン1: 3Eフレーム × バイナリ**
   - FrameVersion = "3E"
   - IsBinary = true
2. **パターン2: 3Eフレーム × ASCII**
   - FrameVersion = "3E"
   - IsBinary = false
3. **パターン3: 4Eフレーム × バイナリ**
   - FrameVersion = "4E"
   - IsBinary = true
4. **パターン4: 4Eフレーム × ASCII**
   - FrameVersion = "4E"
   - IsBinary = false

#### 統合Output
- StructuredData（最終構造化データ）:
  - StructuredDeviceData（Dictionary<string, StructuredDevice>）
    - M100～M107の8デバイス
  - SlmpFrameInfo（SLMPフレーム解析情報）
  - 統計情報（接続時間、送受信時間、処理時間等）
- ConnectionStats（接続統計情報）:
  - TotalResponseTime（合計応答時間）
  - TotalErrors（合計エラー数）= 0
  - TotalRetries（合計リトライ数）= 0
  - SuccessRate（成功率）= 1.0

#### 機能
- Step3-6の連続実行（4パターン独立実行）
- 各ステップ間のデータ受け渡し検証
- パターン別フレーム形式検証
- ビットデバイスの正確な解析検証
- 統計情報の累積

---

## 2. テストケース仕様（TC143_10）

### TC143_10_Step3-6_M100～M107ビット読み出し4パターン統合テスト
**目的**: M100～M107ビット読み出しの4パターン（3E/4E × バイナリ/ASCII）で、Step3-6の完全サイクルが正常に動作することを統合検証

#### 前提条件
- ConfigToFrameManagerによる設定読み込み完了
- BuildFramesによるSLMPフレーム生成完了（4パターン分）
- MockPlcServerが稼働中（4パターン対応応答データ設定済み）
- ネットワーク到達可能（ローカルホスト）

#### 入力データ

**パターン1: 3Eフレーム × バイナリ**

ConnectionConfig:
```csharp
var config1 = new ConnectionConfig
{
    IpAddress = "127.0.0.1",
    Port = 5001,
    UseTcp = true,
    ConnectionType = "TCP",
    IsBinary = true,
    FrameVersion = FrameVersion.Frame3E
};
```

SLMPフレーム（バイナリ形式、バイト配列）:
```csharp
// 3Eバイナリフレーム構造
// サブヘッダ: 0x5000 (2bytes)
// ネットワーク番号: 0x00 (1byte)
// PC番号: 0xFF (1byte)
// 要求先ユニットI/O番号: 0xFF03 (2bytes)
// 要求先ユニット局番号: 0x00 (1byte)
// コマンド: 0x0401 (2bytes) - デバイス一括読み出し
// サブコマンド: 0x0001 (2bytes) - ビット単位
// 開始デバイス番号: 0x000064 (3bytes) - M100
// デバイスコード: 0x90 (1byte) - M機器
// デバイス点数: 0x0008 (2bytes) - 8点
byte[] frame3EBinary = new byte[]
{
    0x50, 0x00,           // サブヘッダ
    0x00,                 // ネットワーク番号
    0xFF,                 // PC番号
    0xFF, 0x03,           // 要求先ユニットI/O番号
    0x00,                 // 要求先ユニット局番号
    0x01, 0x04,           // READコマンド
    0x01, 0x00,           // サブコマンド（ビット単位）
    0x64, 0x00, 0x00,     // 開始デバイス番号（M100）
    0x90,                 // デバイスコード（M機器）
    0x08, 0x00            // デバイス点数（8点）
};
```

**パターン2: 3Eフレーム × ASCII**

ConnectionConfig:
```csharp
var config2 = new ConnectionConfig
{
    IpAddress = "127.0.0.1",
    Port = 5002,
    UseTcp = true,
    ConnectionType = "TCP",
    IsBinary = false,
    FrameVersion = FrameVersion.Frame3E
};
```

SLMPフレーム（ASCII形式、文字列）:
```csharp
// 3E ASCII フレーム（すべて16進数ASCIIテキスト）
// サブヘッダ: "5000" (4文字)
// ネットワーク番号: "00" (2文字)
// PC番号: "FF" (2文字)
// 要求先ユニットI/O番号: "FF03" (4文字)
// 要求先ユニット局番号: "00" (2文字)
// コマンド: "0401" (4文字)
// サブコマンド: "0001" (4文字)
// 開始デバイス番号: "000064" (6文字)
// デバイスコード: "90" (2文字)
// デバイス点数: "0008" (4文字)
string frame3EAscii = "500000FF03FF0004010001000064900008";
```

**パターン3: 4Eフレーム × バイナリ**

ConnectionConfig:
```csharp
var config3 = new ConnectionConfig
{
    IpAddress = "127.0.0.1",
    Port = 5003,
    UseTcp = true,
    ConnectionType = "TCP",
    IsBinary = true,
    FrameVersion = FrameVersion.Frame4E
};
```

SLMPフレーム（バイナリ形式、バイト配列）:
```csharp
// 4Eバイナリフレーム構造
// サブヘッダ: 0x5400 (2bytes)
// シリアル番号: 0x00001234 (4bytes)
// ネットワーク番号: 0x00 (1byte)
// PC番号: 0xFF (1byte)
// 要求先ユニットI/O番号: 0xFF03 (2bytes)
// 要求先ユニット局番号: 0x00 (1byte)
// コマンド: 0x0401 (2bytes)
// サブコマンド: 0x0001 (2bytes)
// 開始デバイス番号: 0x000064 (3bytes)
// デバイスコード: 0x90 (1byte)
// デバイス点数: 0x0008 (2bytes)
byte[] frame4EBinary = new byte[]
{
    0x54, 0x00,                 // サブヘッダ
    0x34, 0x12, 0x00, 0x00,     // シリアル番号
    0x00,                       // ネットワーク番号
    0xFF,                       // PC番号
    0xFF, 0x03,                 // 要求先ユニットI/O番号
    0x00,                       // 要求先ユニット局番号
    0x01, 0x04,                 // READコマンド
    0x01, 0x00,                 // サブコマンド（ビット単位）
    0x64, 0x00, 0x00,           // 開始デバイス番号（M100）
    0x90,                       // デバイスコード（M機器）
    0x08, 0x00                  // デバイス点数（8点）
};
```

**パターン4: 4Eフレーム × ASCII**

ConnectionConfig:
```csharp
var config4 = new ConnectionConfig
{
    IpAddress = "127.0.0.1",
    Port = 5004,
    UseTcp = true,
    ConnectionType = "TCP",
    IsBinary = false,
    FrameVersion = FrameVersion.Frame4E
};
```

SLMPフレーム（ASCII形式、文字列）:
```csharp
// 4E ASCII フレーム
// サブヘッダ: "5400" (4文字)
// シリアル番号: "12340000" (8文字)
// ネットワーク番号: "00" (2文字)
// PC番号: "FF" (2文字)
// 要求先ユニットI/O番号: "03FF" (4文字)
// 要求先ユニット局番号: "00" (2文字)
// コマンド: "0104" (4文字)
// サブコマンド: "0100" (4文字)
// 開始デバイス番号: "640000" (6文字)
// デバイスコード: "90" (2文字)
// デバイス点数: "0800" (4文字)
string frame4EAscii = "54001234000000FF03FF000104010064000090080000";
```

**ProcessedDeviceRequestInfo（共通）**:
```csharp
var deviceRequestInfo = new ProcessedDeviceRequestInfo
{
    DeviceConfiguration = new Dictionary<string, DeviceInfo>
    {
        { "M", new DeviceInfo
          {
              DeviceCode = "M",
              Start = 100,
              End = 107,
              DataType = DataType.Bit,
              DeviceCount = 8
          }
        }
    },
    DWordSplitRanges = new List<DWordSplitRange>()  // ビットデバイスのため空
};
```

#### 期待出力（各パターン共通）

**StructuredData（最終構造化データ）**:
```csharp
// 構造化データ検証
Assert.NotNull(structuredData);
Assert.NotNull(structuredData.StructuredDeviceData);
Assert.Equal(8, structuredData.StructuredDeviceCount);
Assert.NotNull(structuredData.SlmpFrameInfo);

// SLMPフレーム解析情報検証（パターン別）
// パターン1,2: 3Eフレーム
Assert.Equal("3E", structuredData.SlmpFrameInfo.FrameVersion);
Assert.Equal("5000", structuredData.SlmpFrameInfo.SubHeader);

// パターン3,4: 4Eフレーム
Assert.Equal("4E", structuredData.SlmpFrameInfo.FrameVersion);
Assert.Equal("54001234000000", structuredData.SlmpFrameInfo.SubHeader);

// 共通検証
Assert.Equal(0x0000, structuredData.SlmpFrameInfo.EndCode);
Assert.True(structuredData.SlmpFrameInfo.IsSuccess);

// デバイスデータ検証（M100～M107）
var expectedDevices = new[] { "M100", "M101", "M102", "M103", "M104", "M105", "M106", "M107" };
foreach (var deviceName in expectedDevices)
{
    Assert.Contains(deviceName, structuredData.StructuredDeviceData.Keys);

    var device = structuredData.StructuredDeviceData[deviceName];
    Assert.Equal(deviceName, device.DeviceName);
    Assert.Equal(DeviceDataType.Bit, device.DataType);
    Assert.IsType<bool>(device.Value);
    Assert.NotNull(device.Metadata);
}

// ビット値サンプル検証（テストデータ依存）
Assert.True((bool)structuredData.StructuredDeviceData["M100"].Value);   // ON
Assert.False((bool)structuredData.StructuredDeviceData["M101"].Value);  // OFF
Assert.True((bool)structuredData.StructuredDeviceData["M102"].Value);   // ON
// ... 他のビット検証
```

**ConnectionStats（接続統計情報）**:
```csharp
// 統計情報検証（各パターン）
Assert.NotNull(connectionStats);
Assert.True(connectionStats.TotalResponseTime > TimeSpan.Zero);
Assert.Equal(0, connectionStats.TotalErrors);
Assert.Equal(0, connectionStats.TotalRetries);
Assert.Equal(1.0, connectionStats.SuccessRate);
```

#### 動作フロー成功条件（各パターン共通）

1. **Step3（接続）成功**:
   - ConnectionResponse.Status == ConnectionStatus.Connected
   - Socket != null, Socket.Connected == true
   - RemoteEndPoint == "127.0.0.1:500X" (Xはパターン番号)
   - ConnectedAt != null
   - ConnectionTime > TimeSpan.Zero

2. **Step4（送信）成功**:
   - SendFrameAsync完了（例外なし）
   - バイナリ/ASCII形式に応じた正しい送信データ形式
   - 送信バイト数が期待値と一致

3. **Step4（受信）成功**:
   - ReceiveResponseAsync完了（例外なし）
   - 応答データ受信成功
   - 受信データが正しいSLMP応答形式（3E/4E、バイナリ/ASCII）

4. **Step5（切断）成功**:
   - DisconnectAsync完了（例外なし）
   - Socket.Connected == false
   - 統計情報記録済み

5. **Step6-1（基本後処理）成功**:
   - BasicProcessedResponseData生成成功
   - BasicProcessedData（Dictionary）に8デバイス（M100～M107）格納
   - ProcessedDeviceCount == 8

6. **Step6-2（DWord結合）成功**:
   - ProcessedResponseData生成成功
   - ビットデバイスのためDWord結合は実行されない
   - IsDwordCombined == false
   - DWordCombineCount == 0

7. **Step6-3（構造化変換）成功**:
   - StructuredData生成成功
   - StructuredDeviceData（Dictionary）に8デバイス格納
   - SlmpFrameInfo解析成功（EndCode == 0x0000）
   - StructuredDeviceCount == 8
   - フレームバージョン（3E/4E）正確に識別

8. **統計情報累積成功**:
   - 接続時間、送受信時間、処理時間が正確に記録
   - TotalResponseTime == Step3時間 + Step4送受信時間 + Step6処理時間
   - SuccessRate == 1.0（全ステップ成功）

---

## 3. パターン別詳細仕様

### パターン1: 3Eフレーム × バイナリ

#### フレーム構造詳細
```
3Eバイナリフレーム（リクエスト）: 21バイト
バイト位置 | 内容 | 値（16進） | 説明
----------|------|-----------|-----
0-1       | サブヘッダ | 50 00 | 3Eフレーム識別
2         | ネットワーク番号 | 00 | ローカルネットワーク
3         | PC番号 | FF | 要求先PC
4-5       | 要求先ユニットI/O番号 | FF 03 | CPU直結
6         | 要求先ユニット局番号 | 00 | 局番号0
7-8       | コマンド | 01 04 | デバイス一括読み出し
9-10      | サブコマンド | 01 00 | ビット単位読み出し
11-13     | 開始デバイス番号 | 64 00 00 | M100（リトルエンディアン）
14        | デバイスコード | 90 | M機器
15-16     | デバイス点数 | 08 00 | 8点（リトルエンディアン）
```

#### 応答フレーム構造
```
3Eバイナリフレーム（応答）: 11バイト + データ1バイト = 12バイト
バイト位置 | 内容 | 値（16進） | 説明
----------|------|-----------|-----
0-1       | サブヘッダ | D0 00 | 3E応答
2-5       | データ長 | 02 00 00 00 | 2バイト（終了コード）
6-7       | 終了コード | 00 00 | 正常終了
8         | データ | XX | M100～M107のビット値（1バイト）
```

データバイト構造（8ビット）:
```
ビット位置 | デバイス | サンプル値
----------|---------|----------
bit0      | M100    | 1 (ON)
bit1      | M101    | 0 (OFF)
bit2      | M102    | 1 (ON)
bit3      | M103    | 0 (OFF)
bit4      | M104    | 1 (ON)
bit5      | M105    | 1 (ON)
bit6      | M106    | 0 (OFF)
bit7      | M107    | 1 (ON)

サンプル値 = 0xB5 (10110101)
```

---

### パターン2: 3Eフレーム × ASCII

#### フレーム構造詳細
```
3E ASCII フレーム（リクエスト）: 34文字（17バイト相当）
文字位置 | 内容 | 値 | 説明
--------|------|-----|-----
0-3     | サブヘッダ | 5000 | 3Eフレーム識別
4-5     | ネットワーク番号 | 00 | ローカルネットワーク
6-7     | PC番号 | FF | 要求先PC
8-11    | 要求先ユニットI/O番号 | 03FF | CPU直結
12-13   | 要求先ユニット局番号 | 00 | 局番号0
14-17   | コマンド | 0104 | デバイス一括読み出し
18-21   | サブコマンド | 0100 | ビット単位読み出し
22-27   | 開始デバイス番号 | 640000 | M100
28-29   | デバイスコード | 90 | M機器
30-33   | デバイス点数 | 0800 | 8点

実際のフレーム文字列: "500000FF03FF0004010001640000900800"
```

#### 応答フレーム構造
```
3E ASCII フレーム（応答）: 18文字（9バイト相当）+ データ2文字
文字位置 | 内容 | 値 | 説明
--------|------|-----|-----
0-3     | サブヘッダ | D000 | 3E応答
4-11    | データ長 | 02000000 | 2バイト
12-15   | 終了コード | 0000 | 正常終了
16-17   | データ | B5 | M100～M107ビット値（16進文字列）

実際の応答文字列: "D00002000000B5"
```

---

### パターン3: 4Eフレーム × バイナリ

#### フレーム構造詳細
```
4Eバイナリフレーム（リクエスト）: 25バイト
バイト位置 | 内容 | 値（16進） | 説明
----------|------|-----------|-----
0-1       | サブヘッダ | 54 00 | 4Eフレーム識別
2-5       | シリアル番号 | 34 12 00 00 | 0x00001234
6         | ネットワーク番号 | 00 | ローカルネットワーク
7         | PC番号 | FF | 要求先PC
8-9       | 要求先ユニットI/O番号 | FF 03 | CPU直結
10        | 要求先ユニット局番号 | 00 | 局番号0
11-12     | コマンド | 01 04 | デバイス一括読み出し
13-14     | サブコマンド | 01 00 | ビット単位読み出し
15-17     | 開始デバイス番号 | 64 00 00 | M100
18        | デバイスコード | 90 | M機器
19-20     | デバイス点数 | 08 00 | 8点
```

#### 応答フレーム構造
```
4Eバイナリフレーム（応答）: 15バイト + データ1バイト = 16バイト
バイト位置 | 内容 | 値（16進） | 説明
----------|------|-----------|-----
0-1       | サブヘッダ | D4 00 | 4E応答
2-5       | シリアル番号 | 34 12 00 00 | リクエストと同一
6-9       | データ長 | 02 00 00 00 | 2バイト
10-11     | 終了コード | 00 00 | 正常終了
12        | データ | XX | M100～M107のビット値
```

---

### パターン4: 4Eフレーム × ASCII

#### フレーム構造詳細
```
4E ASCII フレーム（リクエスト）: 42文字（21バイト相当）
文字位置 | 内容 | 値 | 説明
--------|------|-----|-----
0-3     | サブヘッダ | 5400 | 4Eフレーム識別
4-11    | シリアル番号 | 12340000 | 0x00001234
12-13   | ネットワーク番号 | 00 | ローカルネットワーク
14-15   | PC番号 | FF | 要求先PC
16-19   | 要求先ユニットI/O番号 | 03FF | CPU直結
20-21   | 要求先ユニット局番号 | 00 | 局番号0
22-25   | コマンド | 0104 | デバイス一括読み出し
26-29   | サブコマンド | 0100 | ビット単位読み出し
30-35   | 開始デバイス番号 | 640000 | M100
36-37   | デバイスコード | 90 | M機器
38-41   | デバイス点数 | 0800 | 8点

実際のフレーム文字列: "54001234000000FF03FF0001040100640000900800"
```

#### 応答フレーム構造
```
4E ASCII フレーム（応答）: 26文字（13バイト相当）+ データ2文字
文字位置 | 内容 | 値 | 説明
--------|------|-----|-----
0-3     | サブヘッダ | D400 | 4E応答
4-11    | シリアル番号 | 12340000 | リクエストと同一
12-19   | データ長 | 02000000 | 2バイト
20-23   | 終了コード | 0000 | 正常終了
24-25   | データ | B5 | M100～M107ビット値

実際の応答文字列: "D4001234000002000000B5"
```

---

## 4. 統合テスト実装構造

### Arrange（準備）

#### 1. MockPlcServer準備（4パターン対応）

```csharp
// パターン1用サーバー（3E × バイナリ）
var mockServer1 = new MockPlcServer("127.0.0.1", 5001, isBinary: true, frameVersion: "3E");
byte[] response1 = new byte[] { 0xD0, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0xB5 };
mockServer1.SetBinaryResponse(frame3EBinary, response1);
mockServer1.Start();

// パターン2用サーバー（3E × ASCII）
var mockServer2 = new MockPlcServer("127.0.0.1", 5002, isBinary: false, frameVersion: "3E");
mockServer2.SetAsciiResponse(frame3EAscii, "D00002000000B5");
mockServer2.Start();

// パターン3用サーバー（4E × バイナリ）
var mockServer3 = new MockPlcServer("127.0.0.1", 5003, isBinary: true, frameVersion: "4E");
byte[] response3 = new byte[] { 0xD4, 0x00, 0x34, 0x12, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0xB5 };
mockServer3.SetBinaryResponse(frame4EBinary, response3);
mockServer3.Start();

// パターン4用サーバー（4E × ASCII）
var mockServer4 = new MockPlcServer("127.0.0.1", 5004, isBinary: false, frameVersion: "4E");
mockServer4.SetAsciiResponse(frame4EAscii, "D4001234000002000000B5");
mockServer4.Start();
```

#### 2. 設定準備（各パターン）

```csharp
// タイムアウト設定（共通）
var timeoutConfig = new TimeoutConfig
{
    ConnectTimeoutMs = 5000,
    SendTimeoutMs = 3000,
    ReceiveTimeoutMs = 3000
};

// ProcessedDeviceRequestInfo準備（共通）
var deviceRequestInfo = new ProcessedDeviceRequestInfo
{
    DeviceConfiguration = new Dictionary<string, DeviceInfo>
    {
        { "M", new DeviceInfo
          {
              DeviceCode = "M",
              Start = 100,
              End = 107,
              DataType = DataType.Bit,
              DeviceCount = 8
          }
        }
    },
    DWordSplitRanges = new List<DWordSplitRange>()
};

// PlcCommunicationManager初期化（各パターン用に4つ）
var manager1 = new PlcCommunicationManager(loggingManager, errorHandler, resourceManager);
var manager2 = new PlcCommunicationManager(loggingManager, errorHandler, resourceManager);
var manager3 = new PlcCommunicationManager(loggingManager, errorHandler, resourceManager);
var manager4 = new PlcCommunicationManager(loggingManager, errorHandler, resourceManager);
```

---

### Act（実行）

#### パターン1実行（3E × バイナリ）

```csharp
// Step3: 接続
var connectionResponse1 = await manager1.ConnectAsync(config1, timeoutConfig);

// Step4: 送信
await manager1.SendFrameAsync(frame3EBinary);

// Step4: 受信
var response1 = await manager1.ReceiveResponseAsync(timeoutConfig);

// Step5: 切断
await manager1.DisconnectAsync();

// Step6-1: 基本後処理
var basicProcessed1 = await manager1.ProcessReceivedRawData(response1, deviceRequestInfo);

// Step6-2: DWord結合（ビットデバイスのためスキップ）
var processed1 = await manager1.CombineDwordData(basicProcessed1, deviceRequestInfo);

// Step6-3: 構造化変換
var structured1 = await manager1.ParseRawToStructuredData(processed1, deviceRequestInfo);

// 統計情報取得
var stats1 = manager1.GetConnectionStats();
```

#### パターン2実行（3E × ASCII）

```csharp
// （パターン1と同様の流れ、config2, frame3EAscii, manager2を使用）
```

#### パターン3実行（4E × バイナリ）

```csharp
// （パターン1と同様の流れ、config3, frame4EBinary, manager3を使用）
```

#### パターン4実行（4E × ASCII）

```csharp
// （パターン1と同様の流れ、config4, frame4EAscii, manager4を使用）
```

---

### Assert（検証）

#### パターン共通検証関数

```csharp
private void AssertFullCycleSuccess(
    ConnectionResponse connectionResponse,
    BasicProcessedResponseData basicProcessed,
    ProcessedResponseData processed,
    StructuredData structured,
    ConnectionStats stats,
    string expectedFrameVersion,
    string expectedSubHeader,
    string testPatternName)
{
    // Step3検証
    Assert.Equal(ConnectionStatus.Connected, connectionResponse.Status,
        $"{testPatternName}: 接続失敗");
    Assert.NotNull(connectionResponse.Socket);
    Assert.True(connectionResponse.Socket.Connected);

    // Step6-1検証
    Assert.NotNull(basicProcessed);
    Assert.Equal(8, basicProcessed.ProcessedDeviceCount,
        $"{testPatternName}: 処理デバイス数不一致");
    Assert.False(basicProcessed.HasError,
        $"{testPatternName}: 基本後処理エラー発生");

    // Step6-2検証（ビットデバイスのためDWord結合なし）
    Assert.NotNull(processed);
    Assert.False(processed.IsDwordCombined,
        $"{testPatternName}: DWord結合が誤って実行された");
    Assert.Equal(0, processed.DWordCombineCount);

    // Step6-3検証
    Assert.NotNull(structured);
    Assert.Equal(8, structured.StructuredDeviceCount,
        $"{testPatternName}: 構造化デバイス数不一致");

    // SLMPフレーム情報検証
    Assert.Equal(expectedFrameVersion, structured.SlmpFrameInfo.FrameVersion,
        $"{testPatternName}: フレームバージョン不一致");
    Assert.Equal(expectedSubHeader, structured.SlmpFrameInfo.SubHeader,
        $"{testPatternName}: サブヘッダ不一致");
    Assert.Equal(0x0000, structured.SlmpFrameInfo.EndCode,
        $"{testPatternName}: 終了コードが正常でない");
    Assert.True(structured.SlmpFrameInfo.IsSuccess,
        $"{testPatternName}: フレーム解析失敗");

    // デバイスデータ検証
    var expectedDevices = new[] { "M100", "M101", "M102", "M103", "M104", "M105", "M106", "M107" };
    foreach (var deviceName in expectedDevices)
    {
        Assert.Contains(deviceName, structured.StructuredDeviceData.Keys,
            $"{testPatternName}: デバイス{deviceName}が見つからない");

        var device = structured.StructuredDeviceData[deviceName];
        Assert.Equal(DeviceDataType.Bit, device.DataType,
            $"{testPatternName}: {deviceName}のデータ型不一致");
        Assert.IsType<bool>(device.Value,
            $"{testPatternName}: {deviceName}の値がbool型でない");
    }

    // ビット値検証（0xB5 = 10110101）
    Assert.True((bool)structured.StructuredDeviceData["M100"].Value);   // bit0 = 1
    Assert.False((bool)structured.StructuredDeviceData["M101"].Value);  // bit1 = 0
    Assert.True((bool)structured.StructuredDeviceData["M102"].Value);   // bit2 = 1
    Assert.False((bool)structured.StructuredDeviceData["M103"].Value);  // bit3 = 0
    Assert.True((bool)structured.StructuredDeviceData["M104"].Value);   // bit4 = 1
    Assert.True((bool)structured.StructuredDeviceData["M105"].Value);   // bit5 = 1
    Assert.False((bool)structured.StructuredDeviceData["M106"].Value);  // bit6 = 0
    Assert.True((bool)structured.StructuredDeviceData["M107"].Value);   // bit7 = 1

    // 統計情報検証
    Assert.NotNull(stats);
    Assert.True(stats.TotalResponseTime > TimeSpan.Zero,
        $"{testPatternName}: 応答時間が記録されていない");
    Assert.Equal(0, stats.TotalErrors,
        $"{testPatternName}: エラーが発生した");
    Assert.Equal(1.0, stats.SuccessRate,
        $"{testPatternName}: 成功率が100%でない");
}
```

#### パターン別検証実行

```csharp
// パターン1検証
AssertFullCycleSuccess(
    connectionResponse1, basicProcessed1, processed1, structured1, stats1,
    expectedFrameVersion: "3E",
    expectedSubHeader: "5000",
    testPatternName: "パターン1（3E×バイナリ）"
);

// パターン2検証
AssertFullCycleSuccess(
    connectionResponse2, basicProcessed2, processed2, structured2, stats2,
    expectedFrameVersion: "3E",
    expectedSubHeader: "5000",
    testPatternName: "パターン2（3E×ASCII）"
);

// パターン3検証
AssertFullCycleSuccess(
    connectionResponse3, basicProcessed3, processed3, structured3, stats3,
    expectedFrameVersion: "4E",
    expectedSubHeader: "54001234000000",
    testPatternName: "パターン3（4E×バイナリ）"
);

// パターン4検証
AssertFullCycleSuccess(
    connectionResponse4, basicProcessed4, processed4, structured4, stats4,
    expectedFrameVersion: "4E",
    expectedSubHeader: "54001234000000",
    testPatternName: "パターン4（4E×ASCII）"
);
```

---

## 5. エラーハンドリング

### TC143_10では発生しないエラー
本テストは正常系のみをテストするため、以下のエラーは発生しない：
- Step3接続エラー
- Step4送受信エラー
- Step6データ処理エラー

### パターン別エラー発生可能性（他テストケースで検証）
- **バイナリ/ASCII形式不一致エラー**: 設定とフレーム形式が一致しない場合
- **フレームバージョン不一致エラー**: 3E/4Eの指定とフレーム内容が一致しない場合
- **ビット境界エラー**: デバイス点数が8の倍数でない場合のビット抽出エラー

---

## 6. テスト実装方針（TDD）

### 開発手法
- C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.mdに記載のTDD手法を使用

### テストファイル配置
- **ファイル名**: PlcCommunicationManager_IntegrationTests_TC143_10.cs
- **配置先**: Tests/Integration/
- **名前空間**: andon.Tests.Integration

### テスト実装順序
1. **TC017-TC027**: Step3-5の単体テスト（完了済み）
2. **TC029-TC037**: Step6の単体テスト（完了済み）
3. **TC121**: FullCycle完全実行テスト（完了済み）
4. **TC143_10**: M100～M107ビット読み出し4パターン統合テスト（本テスト）

### モック・スタブ使用

**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockPlcServer**: TCP対応PLCシミュレータ（4パターン対応）
  - バイナリ/ASCII形式対応
  - 3E/4Eフレーム対応
  - ビットデバイス応答機能
  - 複数ポート同時起動機能（5001-5004）
- **MockLoggingManager**: ログ出力モック
- **MockErrorHandler**: エラーハンドリングモック
- **MockResourceManager**: リソース管理モック

#### 使用するスタブ
- **ConnectionConfigStubs**: 接続設定スタブ（4パターン分）
- **TimeoutConfigStubs**: タイムアウト設定スタブ
- **SlmpFrameStubs**: SLMPフレームスタブ（3E/4E × バイナリ/ASCII）
- **PlcResponseStubs**: PLC応答データスタブ（ビットデータ0xB5）
- **StructuredDataValidator**: 構造化データ検証ヘルパー

---

## 7. 依存クラス・設定

### FrameVersion列挙型

```csharp
public enum FrameVersion
{
    Frame3E,
    Frame4E
}
```

### ConnectionConfig（接続設定）

```csharp
public class ConnectionConfig
{
    public string IpAddress { get; set; }
    public int Port { get; set; }
    public bool UseTcp { get; set; }
    public string ConnectionType { get; set; }
    public bool IsBinary { get; set; }
    public FrameVersion FrameVersion { get; set; }
}
```

### DeviceInfo（デバイス情報）

```csharp
public class DeviceInfo
{
    public string DeviceCode { get; set; }
    public int Start { get; set; }
    public int End { get; set; }
    public DataType DataType { get; set; }
    public int DeviceCount { get; set; }
}

public enum DataType
{
    Bit,
    Word,
    DWord
}
```

### StructuredDevice（構造化デバイスデータ）

```csharp
public class StructuredDevice
{
    public string DeviceName { get; set; }
    public DeviceDataType DataType { get; set; }
    public object Value { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}

public enum DeviceDataType
{
    Bit,
    Word,
    DWord
}
```

### SlmpFrameAnalysis（SLMPフレーム解析情報）

```csharp
public class SlmpFrameAnalysis
{
    public string FrameVersion { get; set; }
    public string SubHeader { get; set; }
    public int EndCode { get; set; }
    public bool IsSuccess { get; set; }
    public string ErrorDescription { get; set; }
}
```

---

## 8. ログ出力要件

### LoggingManager連携

**パターン別ログ出力**:
- **テスト開始ログ**: パターン番号、フレームバージョン、バイナリ/ASCII形式
- **各ステップログ**: Step3-6の各ステップ開始/完了
- **パターン完了ログ**: 統計情報、デバイス数、ビット値サンプル
- **全パターン完了ログ**: 4パターンの総合結果

### ログ出力例

```
[Information] TC143_10開始: M100～M107ビット読み出し4パターン統合テスト

[Information] パターン1開始: 3Eフレーム × バイナリ, Port=5001
[Debug] Step3開始: TCP接続処理
[Information] Step3完了: 接続時間=80ms
[Debug] Step4開始: フレーム送信（バイナリ21バイト）
[Information] Step4完了: 送信成功
[Debug] Step4開始: 応答受信
[Information] Step4完了: 受信成功（12バイト）
[Debug] Step5開始: 切断処理
[Information] Step5完了: 切断時間=30ms
[Debug] Step6-1開始: 基本後処理（ビットデバイス）
[Information] Step6-1完了: 8デバイス処理完了
[Debug] Step6-2開始: DWord結合処理
[Information] Step6-2完了: ビットデバイスのためスキップ
[Debug] Step6-3開始: 構造化変換処理
[Information] Step6-3完了: 8デバイス構造化完了（3Eフレーム）
[Information] パターン1完了: 成功率=100%, 応答時間=150ms
[Debug] ビット値: M100=ON, M101=OFF, M102=ON, M103=OFF, M104=ON, M105=ON, M106=OFF, M107=ON

[Information] パターン2開始: 3Eフレーム × ASCII, Port=5002
...

[Information] パターン3開始: 4Eフレーム × バイナリ, Port=5003
...

[Information] パターン4開始: 4Eフレーム × ASCII, Port=5004
...

[Information] TC143_10完了: 全4パターン成功, 総実行時間=680ms
```

---

## 9. テスト実装チェックリスト

### TC143_10実装前
- [ ] 単体テスト完了確認（TC017-TC037）
- [ ] TC121完了確認
- [ ] MockPlcServer拡張（バイナリ/ASCII、3E/4E対応）
- [ ] PlcResponseStubs作成（ビットデータ0xB5）
- [ ] SlmpFrameStubs作成（4パターン分）

### TC143_10実装中
- [ ] Arrange: MockPlcServer起動（4ポート）
- [ ] Arrange: 応答データ設定（4パターン分）
- [ ] Arrange: ConnectionConfig準備（4パターン分）
- [ ] Arrange: SLMPフレーム準備（4パターン分）
- [ ] Arrange: ProcessedDeviceRequestInfo準備
- [ ] Act: パターン1実行（3E×バイナリ）
- [ ] Act: パターン2実行（3E×ASCII）
- [ ] Act: パターン3実行（4E×バイナリ）
- [ ] Act: パターン4実行（4E×ASCII）
- [ ] Assert: パターン1検証（8デバイス、ビット値、3Eフレーム）
- [ ] Assert: パターン2検証（8デバイス、ビット値、3Eフレーム）
- [ ] Assert: パターン3検証（8デバイス、ビット値、4Eフレーム）
- [ ] Assert: パターン4検証（8デバイス、ビット値、4Eフレーム）

### TC143_10実装後
- [ ] テスト実行・Green確認（4パターン全て）
- [ ] リファクタリング実施
- [ ] パターン別ログ出力確認
- [ ] C:\Users\1010821\Desktop\python\andon\documents\design\チェックリスト\step3to6_test情報まとめ.mdにチェック

---

## 10. 参考情報

### 完全サイクル処理時間（目安）

**パターン1実行時間**:
- Step3（接続）: 50-100ms
- Step4（送信）: 10-30ms
- Step4（受信）: 30-80ms
- Step5（切断）: 10-30ms
- Step6-1（基本後処理）: 5-15ms
- Step6-2（DWord結合）: 1-3ms（スキップ）
- Step6-3（構造化変換）: 5-15ms
- **パターン1合計**: 111-273ms（通常150ms）

**4パターン総実行時間**: 450-1100ms（通常600ms）

### テストデータサンプル

**配置先**: Tests/TestUtilities/TestData/

- TC143_10_Pattern1_3E_Binary_Request.bin: 3Eバイナリリクエストフレーム
- TC143_10_Pattern1_3E_Binary_Response.bin: 3Eバイナリ応答フレーム（0xB5）
- TC143_10_Pattern2_3E_Ascii_Request.txt: 3E ASCIIリクエストフレーム
- TC143_10_Pattern2_3E_Ascii_Response.txt: 3E ASCII応答フレーム
- TC143_10_Pattern3_4E_Binary_Request.bin: 4Eバイナリリクエストフレーム
- TC143_10_Pattern3_4E_Binary_Response.bin: 4Eバイナリ応答フレーム
- TC143_10_Pattern4_4E_Ascii_Request.txt: 4E ASCIIリクエストフレーム
- TC143_10_Pattern4_4E_Ascii_Response.txt: 4E ASCII応答フレーム
- TC143_10_Expected_StructuredData.json: 期待出力サンプル（8デバイス分）

### ビットデバイスデータ構造

**テストデータ（0xB5 = 10110101）**:
```
バイト値: 0xB5
2進数: 10110101
ビット配置（LSBファースト）:
  bit0 (M100): 1 → true
  bit1 (M101): 0 → false
  bit2 (M102): 1 → true
  bit3 (M103): 0 → false
  bit4 (M104): 1 → true
  bit5 (M105): 1 → true
  bit6 (M106): 0 → false
  bit7 (M107): 1 → true
```

### SLMP仕様参考

**3Eフレーム**:
- サブヘッダ: 0x5000（バイナリ）、"5000"（ASCII）
- 応答サブヘッダ: 0xD000（バイナリ）、"D000"（ASCII）
- 最小フレーム長: 21バイト（バイナリ）、42文字（ASCII）

**4Eフレーム**:
- サブヘッダ: 0x5400（バイナリ）、"5400"（ASCII）
- 応答サブヘッダ: 0xD400（バイナリ）、"D400"（ASCII）
- 最小フレーム長: 25バイト（バイナリ）、50文字（ASCII）
- シリアル番号: 4バイト（リトルエンディアン）

---

以上が TC143_10_Step3-6_M100～M107ビット読み出し4パターン統合テスト実装に必要な情報です。
