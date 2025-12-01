# JSONファイルからソケット通信への変換について

## 概要
このプログラムは、JSONファイルから設定情報を読み取り、PLC機器とのソケット通信を行うアプリケーションです。

## JSONから読み取られるキー

JSONファイルから以下のキーが読み取られます：

### 1. `AccessPlcSetting` (List<int>)
- **説明**: PLCアクセス用のコマンドデータ
- **型**: 整数のリスト
- **用途**: ソケット通信でPLCに送信するバイトデータに変換される
- **例**: `[1, 2, 3, 255, 0]`

### 2. `IPAddress` (string)
- **説明**: 接続先のIPアドレス
- **型**: 文字列
- **用途**: ソケット接続の宛先を指定
- **例**: `"192.168.1.100"`

### 3. `Port` (int)
- **説明**: 接続ポート番号
- **型**: 整数
- **用途**: ソケット接続のポート番号を指定
- **例**: `502`

### 4. `ConnectionMethod` (string)
- **説明**: 接続方式
- **型**: 文字列
- **有効値**: `"TCP"` または `"UDP"`
- **用途**: ソケットの種類とプロトコルを決定

### 5. `Key` (string)
- **説明**: データベース保存時の識別キー
- **型**: 文字列
- **用途**: 受信データをデータベースに保存する際の識別子
- **例**: `"PLC001"`

## JSONからソケットリクエストへの変換プロセス

### 1. JSONファイルの読み込み
```csharp
// dataディレクトリ内のすべてのJSONファイルを取得
_targetFiles = Directory.GetFiles(dataDir, "*.json");

// 各JSONファイルを読み込んで設定データに変換
foreach (var path in _targetFiles)
{
    var data = JsonConvert.DeserializeObject<SettingData>(File.ReadAllText(path));
    _targetList.Add(data);
}
```

### 2. ソケット接続の設定
- `ConnectionMethod`の値に基づいてソケットタイプを決定
  - `"TCP"` → `SocketType.Stream` + `ProtocolType.Tcp`
  - `"UDP"` → `SocketType.Dgram` + `ProtocolType.Udp`

### 3. コマンドデータの変換と送信
**重要な変換処理**：
```csharp
// AccessPlcSetting (List<int>) をbyte配列に変換
var accessPlcSetting = settingData.AccessPlcSetting.Select(i => (byte)i).ToArray();
// PLCにコマンドデータを送信
sock.Send(accessPlcSetting);
```

### 4. 通信フロー
1. JSONから`IPAddress`と`Port`を使ってソケット接続を確立
2. `AccessPlcSetting`をバイト配列に変換してPLCに送信
3. PLCからの応答データを受信
4. 受信データを`Key`と共にデータベースに保存

## JSONファイルの例

```json
{
  "AccessPlcSetting": [1, 3, 0, 0, 0, 10],
  "IPAddress": "192.168.1.100",
  "Port": 502,
  "ConnectionMethod": "TCP",
  "Key": "PLC_LINE_001"
}
```

## データベース保存

受信したデータは以下の形式でPostgreSQLデータベースに保存されます：
- `c_datetime`: データ取得時刻
- `key`: JSONの`Key`値
- `value`: PLCから受信したバイトデータ

## 注意点

- `AccessPlcSetting`の各整数値は0-255の範囲でなければならない（byte変換のため）
- 接続タイムアウトは3ミリ秒に設定されている
- データベース接続エラー時は自動的に再接続を試行する