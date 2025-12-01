# Step1: 設定ファイル読み込み実装フロー

## 概要
andonプロジェクトにおけるStep1（設定ファイル読み込み）の実装済みフローを記載する。

---

## 実装クラス
`ConfigurationLoader` (andon/Infrastructure/Configuration/ConfigurationLoader.cs:22-46)

---

## 処理フロー

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

---

## 入力

### appsettings.json (設定ファイル)
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

### 設定項目の詳細

#### Connection セクション
| 項目 | 型 | 必須 | デフォルト値 | 説明 |
|-----|---|------|------------|------|
| FrameVersion | string | ✅ | "4E" | SLMPフレームタイプ ("3E" or "4E") |

#### Timeouts セクション
| 項目 | 型 | 必須 | デフォルト値 | 説明 |
|-----|---|------|------------|------|
| ReceiveTimeoutMs | int | ✅ | 8000 | 受信タイムアウト (ミリ秒) |

**SLMP形式への変換**:
- 内部変換: `Timeout = ReceiveTimeoutMs / 250`
- 例: 8000ms → 32 (SLMP監視タイマ単位)

#### TargetDevices セクション
| 項目 | 型 | 必須 | 説明 |
|-----|---|------|------|
| Devices | array | ✅ | デバイス指定のリスト |

**DeviceEntry 構造**:
```json
{
  "DeviceName": "D",      // デバイス名 (D, M, W, X, Y等)
  "StartAddress": 100,    // 開始アドレス
  "EndAddress": 200       // 終了アドレス
}
```

---

## 出力

### TargetDeviceConfig オブジェクト
```csharp
public class TargetDeviceConfig
{
    public string FrameType { get; set; }         // "3E" or "4E"
    public ushort Timeout { get; set; }           // 250ms単位 (例: 32 = 8秒)
    public List<DeviceEntry> Devices { get; set; } // デバイスリスト
}
```

### DeviceEntry クラス
```csharp
public class DeviceEntry
{
    public string DeviceName { get; set; }    // "D", "M", "W", etc.
    public int StartAddress { get; set; }     // 開始アドレス
    public int EndAddress { get; set; }       // 終了アドレス
}
```

---

## 成功条件

- ✅ 設定ファイルに記載されている値を正確に取得できること
- ✅ 必要な値が設定されていない場合はエラーを返すこと
- ✅ 不正な値（範囲外、型不一致）を検出してエラーを返すこと
- ✅ デフォルト値が正しく適用されること
- ✅ タイムアウト値がSLMP形式(250ms単位)に正しく変換されること

---

## エラーハンドリング

### ValidateConfig() で検出されるエラー

| エラー条件 | 例外 | メッセージ |
|-----------|------|-----------|
| FrameTypeが"3E"/"4E"以外 | ArgumentException | "未対応のFrameType: {value}" |
| Timeoutが範囲外 (1-65535) | ArgumentException | "Timeoutが範囲外です" |
| Devicesが空またはnull | ArgumentException | "デバイスリストが空です" |
| DeviceNameが空/null | ArgumentException | "DeviceNameが空です" |
| StartAddress > EndAddress | ArgumentException | "アドレス範囲が不正です: Start={start}, End={end}" |
| StartAddress < 0 | ArgumentException | "開始アドレスが負の値です: {start}" |
| EndAddress < 0 | ArgumentException | "終了アドレスが負の値です: {end}" |
| デバイス点数が255を超える | ArgumentException | "デバイス点数が上限を超えています: {count}点（最大255点）" |

### エラー処理の流れ

```
LoadPlcConnectionConfig()
    ↓
設定読み込み
    ↓
ValidateConfig() 呼び出し
    ↓
検証エラー発生?
    ├─ Yes → ArgumentException スロー
    │         └─ 呼び出し元でキャッチ
    └─ No  → 正常な TargetDeviceConfig 返却
```

---

## 実装の詳細

### ConfigurationLoader クラス構造

```csharp
public class ConfigurationLoader
{
    private readonly IConfiguration _configuration;

    public ConfigurationLoader(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // 設定ファイルからPLC接続設定を読み込む
    public TargetDeviceConfig LoadPlcConnectionConfig()
    {
        // 1. 設定インスタンス作成
        var config = new TargetDeviceConfig();

        // 2. 基本設定の読み込み
        var connectionSection = _configuration.GetSection("PlcCommunication:Connection");
        config.FrameType = connectionSection["FrameVersion"] ?? "4E";

        // 3. タイムアウト設定の読み込み
        var timeoutsSection = _configuration.GetSection("PlcCommunication:Timeouts");
        var receiveTimeoutMs = int.Parse(timeoutsSection["ReceiveTimeoutMs"] ?? "8000");
        config.Timeout = (ushort)(receiveTimeoutMs / 250);

        // 4. デバイスリストの読み込み
        var devicesSection = _configuration.GetSection("PlcCommunication:TargetDevices:Devices");
        if (devicesSection.Exists() && devicesSection.GetChildren().Any())
        {
            config.Devices = devicesSection.Get<List<DeviceEntry>>() ?? new List<DeviceEntry>();
        }

        // 5. 設定検証
        ValidateConfig(config);

        // 6. 設定返却
        return config;
    }

    // 設定内容の検証
    private void ValidateConfig(TargetDeviceConfig config)
    {
        // FrameType検証
        if (config.FrameType != "3E" && config.FrameType != "4E")
        {
            throw new ArgumentException($"未対応のFrameType: {config.FrameType}");
        }

        // Timeout検証
        if (config.Timeout < 1 || config.Timeout > 65535)
        {
            throw new ArgumentException("Timeoutが範囲外です");
        }

        // Devices検証
        if (config.Devices == null || config.Devices.Count == 0)
        {
            throw new ArgumentException("デバイスリストが空です");
        }

        // 各デバイスの検証
        int totalDeviceCount = 0;
        foreach (var device in config.Devices)
        {
            // デバイス名検証
            if (string.IsNullOrWhiteSpace(device.DeviceName))
            {
                throw new ArgumentException("DeviceNameが空です");
            }

            // アドレス範囲検証
            if (device.StartAddress < 0)
            {
                throw new ArgumentException($"開始アドレスが負の値です: {device.StartAddress}");
            }

            if (device.EndAddress < 0)
            {
                throw new ArgumentException($"終了アドレスが負の値です: {device.EndAddress}");
            }

            if (device.StartAddress > device.EndAddress)
            {
                throw new ArgumentException(
                    $"アドレス範囲が不正です: Start={device.StartAddress}, End={device.EndAddress}");
            }

            // 点数計算
            int deviceCount = device.EndAddress - device.StartAddress + 1;
            totalDeviceCount += deviceCount;
        }

        // 総点数検証 (ReadRandomコマンドの制約: 最大255点)
        if (totalDeviceCount > 255)
        {
            throw new ArgumentException(
                $"デバイス点数が上限を超えています: {totalDeviceCount}点（最大255点）");
        }
    }
}
```

---

## 設定例

### 基本設定例
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
          "EndAddress": 110
        }
      ]
    }
  }
}
```

### 複数デバイス設定例
```json
{
  "PlcCommunication": {
    "Connection": {
      "FrameVersion": "3E"
    },
    "Timeouts": {
      "ReceiveTimeoutMs": 10000
    },
    "TargetDevices": {
      "Devices": [
        {
          "DeviceName": "D",
          "StartAddress": 100,
          "EndAddress": 150
        },
        {
          "DeviceName": "M",
          "StartAddress": 0,
          "EndAddress": 100
        },
        {
          "DeviceName": "W",
          "StartAddress": 0,
          "EndAddress": 50
        }
      ]
    }
  }
}
```

**注意**: 全デバイス合計で255点以下になるように設定すること

---

## 特徴

### ✅ 実装済み機能
- **IConfiguration統合**: ASP.NET Core標準のConfiguration APIを使用
- **デフォルト値対応**: 設定が欠けている場合も安全に動作
- **厳格な検証**: SLMP仕様書に準拠した制約チェック
- **型安全な読み込み**: JSON → C#オブジェクトの自動変換
- **タイムアウト自動変換**: ミリ秒 → SLMP形式(250ms単位)の自動変換
- **エラーメッセージ充実**: 不正な設定を具体的に通知

### ⚠️ 制約事項
- **点数上限**: ReadRandomコマンドの制約により最大255点
- **固定パラメータ**: ネットワーク番号、局番、I/O番号は固定値
- **単一PLC**: 複数PLC同時接続は未対応（MultiConfigManager使用で対応可能）

---

## 次のステップ

Step1で読み込んだ `TargetDeviceConfig` は、Step2のフレーム構築処理に渡されます。

```
Step1: LoadPlcConnectionConfig()
    ↓ (TargetDeviceConfig)
Step2: BuildReadRandomFrameFromConfig()
    ↓ (byte[])
Step3: PLC接続・送信
```

---

## 参考資料
- ConfigurationLoader.cs: andon/Infrastructure/Configuration/ConfigurationLoader.cs
- TargetDeviceConfig.cs: andon/Core/Models/ConfigModels/TargetDeviceConfig.cs
- DeviceEntry.cs: andon/Core/Models/ConfigModels/DeviceEntry.cs
- appsettings.json: プロジェクトルートの設定ファイル
- ASP.NET Core Configuration: https://learn.microsoft.com/ja-jp/aspnet/core/fundamentals/configuration/
