# Phase7: データ出力処理の修正

## ステータス
✅ **完了** - Phase7実装完了 (2025-11-25)
🔄 **仕様更新** - Phase4 (2025-11-20)仕様変更対応 (2025-11-21)
📝 **文書更新** - プロパティ名・plcModel修正完了 (2025-11-25)
✅ **テスト** - 全12テスト成功 (DataOutputManager 6件 + LoggingManager 6件) (2025-11-25)

### 実装チェックリスト

#### Phase 1: TDD Red Phase（失敗するテストを先に作成）
- ✅ DeviceEntryInfoTests作成完了
- ✅ DataOutputManagerTests作成完了
- ✅ テスト実行（Red確認）- 検出問題を解決し実行成功
- ✅ LoggingManagerTests_Phase7作成完了

#### Phase 2: TDD Green Phase（最小限の実装でテストをパス）
- ✅ DeviceEntryInfo.cs実装完了
- ✅ DataOutputManager.OutputToJson()実装完了
- ✅ DataOutputManager.ConvertValue()実装完了
- ✅ DeviceData.Typeプロパティ実装済み（Phase5で実装済み）
- ✅ テスト実行（Green確認）- 全6テスト成功
- ✅ LoggingManager.LogDataAcquisition()実装完了
- ✅ LoggingManager全メソッド実装完了（6テスト全成功）

#### Phase 3: TDD Refactor Phase（コード品質向上）
- ✅ コメント・ドキュメント整備完了
- ✅ テスト検出問題の解決（andon/Tests/配下へファイル配置）
- ✅ JsonElementの使用方法修正（HasValue → NotEqual比較）
- ✅ TestLoggerヘルパークラス実装

### 実装結果サマリー

| 項目 | ステータス | 備考 |
|-----|-----------|------|
| DeviceEntryInfoクラス | ✅ 完了 | Name, Digitsプロパティ実装 |
| DataOutputManager.OutputToJson() | ✅ 完了 | JSON形式出力実装 |
| DataOutputManager.ConvertValue() | ✅ 完了 | Bit/Word/DWord変換実装 |
| LoggingManager.LogDataAcquisition() | ✅ 完了 | ReadRandom対応ログ実装 |
| LoggingManager.LogFrameSent() | ✅ 完了 | フレーム送信ログ実装 |
| LoggingManager.LogResponseReceived() | ✅ 完了 | レスポンス受信ログ実装 |
| LoggingManager.LogError() | ✅ 完了 | エラーログ実装 |
| ビルド | ✅ 成功 | エラー0件、警告81件 |
| DataOutputManagerTests | ✅ 完了 | 6/6テスト成功 |
| LoggingManagerTests_Phase7 | ✅ 完了 | 6/6テスト成功 |
| 実装結果ドキュメント | ✅ 完了 | Phase7_DataOutput_LoggingManager_TestResults.md作成 |

## 概要
DataOutputManagerとLoggingManagerを修正し、不連続デバイスのデータを正しく出力・ログ記録できるようにします。

**注意**: readコマンド(0x0401)は廃止されました。本システムはread_randomコマンド(0x0403)のみをサポートします。

**Phase4 (2025-11-20)仕様変更対応**:
1. **通信回数の最小化**: 2回送受信 → 1回送受信（全デバイス一括取得）
2. **処理の簡素化**: MergeResponseData()削除、BasicProcessedResponseData型削除
3. **型設計の明確化**: DeviceDataクラス導入、デバイス名キー構造（"M000", "D000", "D002"）
4. **データ構造変更**: Dictionary<DeviceSpecification, ushort> → Dictionary<string, DeviceData>

## 前提条件
- ✅ Phase5完了: ReadRandomレスポンスパース実装済み（Dictionary<string, DeviceData>使用可能）
  - **DeviceData.Typeプロパティ実装済み**（"Bit", "Word", "DWord"）
- ✅ Phase6完了: 設定ファイル構造変更済み（List<DeviceSpecification>使用可能）
- ✅ Phase4完了: 2025-11-20仕様変更適用済み（DeviceDataクラス定義、1回通信での全デバイス一括取得）

## 実装ステップ

### ステップ20: DataOutputManagerの出力形式変更

#### 実装対象
`andon/Core/Managers/DataOutputManager.cs`

**実装方針**:
- read_randomコマンド(0x0403)専用のJSON出力
- 飛び飛びのデバイス（D100, D105, M200等）に対応
- ファイル名に接続情報とタイムスタンプを含む

#### 新しい実装（Phase4 2025-11-20仕様対応 + JSON出力形式）

**重要な変更点**:
- ProcessedResponseData.ProcessedDataプロパティを使用（Dictionary<string, DeviceData>型）
- デバイス名キー構造（"M000", "D000", "D002"）でJSON items配列生成
- ProcessedData内のDeviceData.Valueプロパティでデータ値を取得
- JSON形式で出力（ファイル名: `yyyymmdd_hhmmssSSS_xxx-xxx-x-xx_zzzz.json`）

```csharp
using Andon.Core.Models;
using Andon.Core.Constants;
using System.Text.Json;

namespace Andon.Core.Managers;

/// <summary>
/// Step4: データ出力
/// Phase4 (2025-11-20)仕様変更対応 + JSON出力形式
/// </summary>
public class DataOutputManager
{
    /// <summary>
    /// ReadRandomデータをJSON出力（不連続デバイス対応、Phase4仕様対応）
    /// </summary>
    /// <param name="data">処理済みレスポンスデータ</param>
    /// <param name="outputDirectory">出力ディレクトリパス</param>
    /// <param name="ipAddress">IPアドレス（設定ファイルのConnection.IpAddressから取得）</param>
    /// <param name="port">ポート番号（設定ファイルのConnection.Portから取得）</param>
    /// <param name="deviceConfig">デバイス設定情報（設定ファイルのTargetDevices.Devicesから構築）
    /// キー: デバイス名（"M0", "D100"など）
    /// 値: DeviceEntryInfo（Name=Description, Digits=1）</param>
    public void OutputToJson(
        ProcessedResponseData data,
        string outputDirectory,
        string ipAddress,
        int port,
        Dictionary<string, DeviceEntryInfo> deviceConfig)
    {
        // PLC機種名は現時点では固定値（Phase7実装）
        const string plcModel = "Unknown";

        // ファイル名生成: yyyymmdd_hhmmssSSS_xxx-xxx-x-xx_zzzz.json
        var timestamp = data.ProcessedAt;
        var dateString = timestamp.ToString("yyyyMMdd_HHmmssfff");
        var ipString = ipAddress.Replace(".", "-");
        var fileName = $"{dateString}_{ipString}_{port}.json";
        var filePath = Path.Combine(outputDirectory, fileName);

        // JSON構造構築
        var jsonData = new
        {
            source = new
            {
                plcModel = plcModel,
                ipAddress = ipAddress,
                port = port
            },
            timestamp = new
            {
                local = timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")  // ISO 8601 with timezone
            },
            items = data.ProcessedData.Select(kvp => new
            {
                name = deviceConfig.TryGetValue(kvp.Key, out var config) ? config.Name : kvp.Key,
                device = new
                {
                    code = kvp.Value.Code.ToString(),
                    number = kvp.Value.Address.ToString()
                },
                digits = deviceConfig.TryGetValue(kvp.Key, out var config2) ? config2.Digits : 1,
                unit = kvp.Value.Type.ToLower(),  // Phase5で追加されたTypeプロパティを使用: "Bit" -> "bit", "Word" -> "word", "DWord" -> "dword"
                value = ConvertValue(kvp.Value)
            }).ToArray()
        };

        // JSON出力（インデント付き、読みやすい形式）
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var jsonString = JsonSerializer.Serialize(jsonData, options);
        File.WriteAllText(filePath, jsonString);
    }

    /// <summary>
    /// DeviceDataの値を適切な型に変換
    /// </summary>
    private object ConvertValue(DeviceData deviceData)
    {
        return deviceData.Type.ToLower() switch
        {
            "bit" => deviceData.Value,  // 0 or 1
            "word" => deviceData.Value,  // uint16
            "dword" => deviceData.Value,  // uint32
            _ => deviceData.Value
        };
    }
}

/// <summary>
/// デバイス設定情報（name, digits取得用）
/// 設定ファイル（appsettings.json）のDevicesセクションから取得
/// </summary>
public class DeviceEntryInfo
{
    /// <summary>
    /// センサー名・用途説明（設定ファイルのDescriptionフィールド）
    /// 例: "運転状態フラグ開始", "生産数カウンタ", "エラーカウンタ"
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// データ桁数（将来の拡張用、現在は常に1）
    /// </summary>
    public int Digits { get; set; }
}
```

**Phase4仕様変更の反映箇所**:
1. `data.DeviceValueMap` → `data.ProcessedData`（Phase5で定義されたプロパティ名）
2. `data.Timestamp` → `data.ProcessedAt`（ProcessedResponseData既存プロパティ使用）
3. `data.ProcessedData.Values` → `data.ProcessedData.Values.Select(d => d.Value)`（DeviceDataクラス経由で値取得）
4. CSV出力 → JSON出力（ファイル名にIPアドレス・ポート情報含む）

#### JSON出力例（新形式）

**ファイル名**: `20251125_103045123_172-30-40-15_8192.json`

```json
{
  "source": {
    "plcModel": "Unknown",
    "ipAddress": "172.30.40.15",
    "port": 8192
  },
  "timestamp": {
    "local": "2025-11-25T10:30:45.123+09:00"
  },
  "items": [
    {
      "name": "運転状態フラグ開始",
      "device": {
        "code": "M",
        "number": "0"
      },
      "digits": 1,
      "unit": "bit",
      "value": 1
    },
    {
      "name": "運転状態フラグ",
      "device": {
        "code": "M",
        "number": "16"
      },
      "digits": 1,
      "unit": "bit",
      "value": 0
    },
    {
      "name": "エラーフラグ",
      "device": {
        "code": "M",
        "number": "100"
      },
      "digits": 1,
      "unit": "bit",
      "value": 0
    },
    {
      "name": "生産数カウンタ開始",
      "device": {
        "code": "D",
        "number": "0"
      },
      "digits": 1,
      "unit": "word",
      "value": 1500
    },
    {
      "name": "生産数カウンタ",
      "device": {
        "code": "D",
        "number": "100"
      },
      "digits": 1,
      "unit": "word",
      "value": 256
    },
    {
      "name": "エラーカウンタ",
      "device": {
        "code": "D",
        "number": "105"
      },
      "digits": 1,
      "unit": "word",
      "value": 5
    },
    {
      "name": "通信ステータス（W0x11AA）",
      "device": {
        "code": "W",
        "number": "4522"
      },
      "digits": 1,
      "unit": "word",
      "value": 4522
    }
  ]
}
```

**注意**: `name`フィールドは設定ファイル（appsettings.json）の`Description`フィールドから取得されます。

**特徴**:
- ✅ JSON形式で構造化された出力
- ✅ ファイル名にタイムスタンプ・IPアドレス・ポート情報を含む
- ✅ ISO 8601形式のタイムゾーン付きタイムスタンプ
- ✅ デバイスごとにname, device, digits, unit, valueを含む
- ✅ 飛び飛びのデバイスに対応
- ✅ 1ファイル = 1回の取得結果

#### 変化点
- **変更前**: read_randomコマンド未実装のため、出力機能なし
- **変更後**: 指定したデバイスのみJSON形式で出力（D100, D105, M200...）

---

### ステップ21: LoggingManagerのログフォーマット変更

#### 実装対象
`andon/Core/Managers/LoggingManager.cs`

**実装方針**:
- read_randomコマンド(0x0403)専用のログ出力
- デバイス点数と代表デバイスをログ記録

#### 新しい実装（Phase4 2025-11-20仕様対応）

**重要な変更点**:
- ProcessedResponseData.ProcessedDataプロパティを使用（Dictionary<string, DeviceData>型）
- デバイス名キー構造（"M000", "D000", "D002"）でログ出力
- DeviceData.Typeプロパティでデバイス種別を識別可能

```csharp
using Microsoft.Extensions.Logging;
using Andon.Core.Models;

namespace Andon.Core.Managers;

/// <summary>
/// Step6: ログ出力
/// Phase4 (2025-11-20)仕様変更対応
/// </summary>
public class LoggingManager
{
    private readonly ILogger<LoggingManager> _logger;

    public LoggingManager(ILogger<LoggingManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// データ取得のログ記録（Phase4仕様対応）
    /// </summary>
    public void LogDataAcquisition(ProcessedResponseData data)
    {
        // Phase4仕様: Dictionary<string, DeviceData>型のProcessedDataプロパティ使用
        var deviceList = string.Join(", ", data.ProcessedData.Keys.Take(5));
        int deviceCount = data.ProcessedData.Count;

        if (deviceCount <= 5)
        {
            _logger.LogInformation(
                $"[ReadRandom] {deviceCount}点取得: {deviceList}"
            );
        }
        else
        {
            _logger.LogInformation(
                $"[ReadRandom] {deviceCount}点取得: {deviceList}... （他{deviceCount - 5}点）"
            );
        }
    }

    /// <summary>
    /// フレーム送信のログ記録
    /// </summary>
    public void LogFrameSent(byte[] frame, string commandType)
    {
        _logger.LogDebug(
            $"[送信] {commandType}フレーム: {frame.Length}バイト"
        );
    }

    /// <summary>
    /// レスポンス受信のログ記録
    /// </summary>
    public void LogResponseReceived(byte[] response)
    {
        _logger.LogDebug(
            $"[受信] レスポンス: {response.Length}バイト"
        );
    }

    /// <summary>
    /// エラーのログ記録
    /// </summary>
    public void LogError(Exception ex, string context)
    {
        _logger.LogError(ex, $"[エラー] {context}: {ex.Message}");
    }
}
```

**Phase4仕様変更の反映箇所**:
1. `data.DeviceValueMap.Keys` → `data.ProcessedData.Keys`（デバイス名キー構造）
2. `data.DeviceValueMap.Count` → `data.ProcessedData.Count`（Dictionary<string, DeviceData>型）

**追加情報（Phase5実装、Phase7使用）**:
- DeviceDataクラスには`Type`プロパティ（"Bit", "Word", "DWord"）が含まれる（Phase5で追加）
- DataOutputManagerのJSON出力で`unit`フィールド生成時に使用（`.ToLower()`で小文字化）
- LoggingManagerでは将来的にデバイス種別ごとのログ分類が可能（例: "48点取得: ビット16点、ワード24点、ダブルワード8点"）

#### ログ出力例（新形式）

```
[2025-11-18 10:15:30] [Info] [ReadRandom] 48点取得: D61000, D61003, D61010, W0x11AA, W0x11DC... （他43点）
[2025-11-18 10:15:30] [Debug] [送信] ReadRandomフレーム: 213バイト
[2025-11-18 10:15:30] [Debug] [受信] レスポンス: 111バイト
```

**特徴**:
- ✅ ReadRandom使用を明示
- ✅ デバイス点数と代表デバイスをログ記録
- ✅ デバイス数が多い場合は省略表示
- ✅ フレームサイズもログ記録

#### 変化点
- **変更前**: read_randomコマンド未実装のため、ログ出力機能なし
- **変更後**: "[ReadRandom] 48点取得: D61000, D61003, D61010, W0x11AA, W0x11DC... （他43点）"

---

### ステップ22: データ出力のテスト更新

#### 実装対象
`andon/Tests/Unit/Core/Managers/DataOutputManagerTests.cs`

#### テスト内容

1. **OutputToJson()の基本テスト**
   - ReadRandomデータのJSON出力テスト（不連続デバイス）
   - JSON構造の検証（source, timestamp, items）
   - ファイル名生成の検証

2. **JSON形式の検証テスト**
   - items配列の各要素の検証（name, device, digits, unit, value）
   - タイムスタンプフォーマットの検証（ISO 8601）
   - デバイスコード・アドレスの正確性検証

#### テストコード（サンプル）（Phase4 2025-11-20仕様対応 + JSON出力）

**重要な変更点**:
- ProcessedResponseDataの構築時にProcessedDataプロパティ（Dictionary<string, DeviceData>型）を使用
- DeviceData.FromDeviceSpecification()でDeviceDataオブジェクト生成
- デバイス名キー構造（"D100", "D105", "M200"）でテスト検証
- JSON形式での出力検証

```csharp
using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Constants;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

namespace Andon.Tests.Unit.Core.Managers;

public class DataOutputManagerTests : IDisposable
{
    private readonly DataOutputManager _manager;
    private readonly string _testDirectory;

    public DataOutputManagerTests()
    {
        _manager = new DataOutputManager();
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void OutputToJson_ReadRandomData_OutputsCorrectJson()
    {
        // Arrange - Phase4仕様: Dictionary<string, DeviceData>を使用
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "D100", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 100), 256) },
            { "D105", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 105), 512) },
            { "M200", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.M, 200), 1) }
        };

        var timestamp = new DateTime(2025, 11, 25, 10, 30, 45, 123);
        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = timestamp
        };

        // 設定ファイルから取得した情報を使用
        var deviceConfig = new Dictionary<string, DeviceEntryInfo>
        {
            { "D100", new DeviceEntryInfo { Name = "生産数カウンタ", Digits = 1 } },
            { "D105", new DeviceEntryInfo { Name = "エラーカウンタ", Digits = 1 } },
            { "M0", new DeviceEntryInfo { Name = "運転状態フラグ開始", Digits = 1 } }
        };

        // Act
        _manager.OutputToJson(
            data,
            _testDirectory,
            "192.168.1.100",
            5000,
            deviceConfig);

        // Assert - ファイル名検証
        var files = Directory.GetFiles(_testDirectory, "*.json");
        Assert.Single(files);

        var fileName = Path.GetFileName(files[0]);
        Assert.Matches(@"^\d{8}_\d{9}_192-168-1-100_5000\.json$", fileName);

        // Assert - JSON内容検証
        var jsonString = File.ReadAllText(files[0]);
        var jsonDoc = JsonDocument.Parse(jsonString);
        var root = jsonDoc.RootElement;

        // source検証
        Assert.Equal("Unknown", root.GetProperty("source").GetProperty("plcModel").GetString());
        Assert.Equal("192.168.1.100", root.GetProperty("source").GetProperty("ipAddress").GetString());
        Assert.Equal(5000, root.GetProperty("source").GetProperty("port").GetInt32());

        // timestamp検証
        var timestampStr = root.GetProperty("timestamp").GetProperty("local").GetString();
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}[+-]\d{2}:\d{2}$", timestampStr);

        // items検証
        var items = root.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(3, items.Count);

        // D100検証
        var d100 = items.FirstOrDefault(i => i.GetProperty("device").GetProperty("code").GetString() == "D" &&
                                             i.GetProperty("device").GetProperty("number").GetString() == "100");
        Assert.NotNull(d100);
        Assert.Equal("運転モード", d100.Value.GetProperty("name").GetString());
        Assert.Equal(1, d100.Value.GetProperty("digits").GetInt32());
        Assert.Equal("word", d100.Value.GetProperty("unit").GetString());
        Assert.Equal(256, d100.Value.GetProperty("value").GetUInt32());
    }

    [Fact]
    public void OutputToJson_MultipleWrites_CreatesMultipleFiles()
    {
        // Arrange - Phase4仕様対応
        var deviceData1 = new Dictionary<string, DeviceData>
        {
            { "D100", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 100), 256) }
        };

        var deviceData2 = new Dictionary<string, DeviceData>
        {
            { "D100", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 100), 257) }
        };

        var data1 = new ProcessedResponseData
        {
            ProcessedData = deviceData1,
            ProcessedAt = DateTime.Now
        };

        var data2 = new ProcessedResponseData
        {
            ProcessedData = deviceData2,
            ProcessedAt = DateTime.Now.AddSeconds(1)
        };

        var deviceConfig = new Dictionary<string, DeviceEntryInfo>
        {
            { "D100", new DeviceEntryInfo { Name = "テストデバイス", Digits = 1 } }
        };

        // Act
        _manager.OutputToJson(data1, _testDirectory, "192.168.1.100", 5000, deviceConfig);
        _manager.OutputToJson(data2, _testDirectory, "192.168.1.100", 5000, deviceConfig);

        // Assert - 2ファイル作成されることを確認
        var files = Directory.GetFiles(_testDirectory, "*.json");
        Assert.Equal(2, files.Length);

        // 各ファイルの値を検証
        var jsonStrings = files.Select(f => File.ReadAllText(f)).ToList();
        var values = jsonStrings.Select(json =>
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("items")[0].GetProperty("value").GetUInt32();
        }).OrderBy(v => v).ToList();

        Assert.Equal(256u, values[0]);
        Assert.Equal(257u, values[1]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
```

**Phase4仕様変更の反映箇所**:
1. `Dictionary<DeviceSpecification, ushort>` → `Dictionary<string, DeviceData>`
2. ProcessedResponseData.ProcessedDataプロパティを使用
3. デバイス名キー構造（"D100", "D105", "M200"）を使用
4. `DeviceData.FromDeviceSpecification()`でDeviceDataオブジェクト生成
5. CSV出力テスト → JSON出力テスト（ファイル名検証、JSON構造検証）
6. 複数回出力は追記モードではなく個別ファイル生成を検証
7. plcModelは固定値"Unknown"を使用

---

## 完了条件
- ✅ DataOutputManager.OutputToJson()実装完了（ReadRandom対応、JSON形式） - 2025-11-25完了
- ✅ DeviceEntryInfoクラス定義完了 - 2025-11-25完了
- ✅ LoggingManager.LogDataAcquisition()実装完了（ReadRandom対応） - 2025-11-25完了
- ✅ LoggingManager全メソッド実装完了（LogFrameSent, LogResponseReceived, LogError） - 2025-11-25完了
- ✅ DataOutputManagerTests全テストパス（JSON出力検証、6/6成功） - 2025-11-25完了
- ✅ LoggingManagerTests_Phase7全テストパス（6/6成功） - 2025-11-25完了
- ✅ 不連続デバイスのデータが正しくJSON形式で出力可能 - 2025-11-25確認完了
- ✅ ファイル名が仕様通りに生成される（yyyymmdd_hhmmssSSS_xxx-xxx-x-xx_zzzz.json） - 2025-11-25確認完了
- ✅ Phase4仕様変更対応（Dictionary<string, DeviceData>）完了 - 2025-11-25確認完了
- ✅ 実装結果ドキュメント作成完了（Phase7_DataOutput_LoggingManager_TestResults.md） - 2025-11-25完了

## 次フェーズへの依存関係
- Phase8（統合テストの追加・修正）で、JSON出力を含む一連フローをテストします

## リスク管理
| リスク | 影響 | 対策 |
|--------|------|------|
| **ログの可読性低下** | 低 | ・デバイス数が多い場合は省略表示<br>・デバッグログレベルで詳細出力 |
| **JSONファイル数の増加** | 低 | ・ファイルローテーション検討<br>・古いファイルの自動削除機能検討 |
| **ファイル名の重複** | 中 | ・ミリ秒単位のタイムスタンプ使用<br>・同一ミリ秒での複数出力は発生しない想定 |
| **タイムゾーン処理** | 低 | ・ISO 8601形式でタイムゾーン情報を含める<br>・ローカルタイムゾーンで出力 |

---

## Phase4 (2025-11-20)仕様変更対応のまとめ

### 📝 変更された型・メソッド

#### 削除された型・メソッド
```csharp
// 削除: BasicProcessedResponseData型
/*
public class BasicProcessedResponseData
{
    public Dictionary<int, ushort> Data { get; set; }
    public int DWordDeviceCount { get; set; }
}
*/

// 削除: MergeResponseData()メソッド
/*
private Dictionary<int, ushort> MergeResponseData(
    Dictionary<int, ushort> data1,
    Dictionary<int, ushort> data2)
{
    var merged = new Dictionary<int, ushort>(data1);
    foreach (var kvp in data2)
    {
        merged.Add(kvp.Key, kvp.Value);
    }
    return merged;
}
*/
```

#### 新しく導入された型

**DeviceDataクラス**（Phase5で定義）:
```csharp
/// <summary>
/// デバイスデータを表現するクラス
/// Phase4仕様変更(2025-11-20)で導入
/// </summary>
public class DeviceData
{
    public string DeviceName { get; set; }  // "M000", "D000", "D002"等
    public DeviceCode Code { get; set; }
    public int Address { get; set; }
    public uint Value { get; set; }
    public bool IsDWord { get; set; }
    public bool IsHexAddress { get; set; }
    public string Type { get; set; }  // "Bit", "Word", "DWord"
}
```

**ProcessedResponseDataの構造変更**:
```csharp
// 変更前（Phase5初期設計）
public class ProcessedResponseData
{
    public Dictionary<DeviceSpecification, ushort> DeviceValueMap { get; set; }
}

// 変更後（Phase4仕様対応）
public class ProcessedResponseData
{
    public Dictionary<string, DeviceData> ProcessedData { get; set; }
    public DateTime ProcessedAt { get; set; }
    // BasicProcessedDevices, CombinedDWordDevicesは削除
}
```

### 🔄 Phase7での対応内容

| 項目 | 変更前 | 変更後 (Phase4対応 + JSON出力) |
|-----|--------|-------------------|
| **データ構造** | `Dictionary<DeviceSpecification, ushort>` | `Dictionary<string, DeviceData>` |
| **プロパティ名** | `DeviceValueMap` | `ProcessedData` |
| **キー構造** | DeviceSpecificationオブジェクト | デバイス名文字列 ("M000", "D000") |
| **値取得** | `data.DeviceValueMap.Values` | `data.ProcessedData.Values.Select(d => d.Value)` |
| **通信回数** | 2回（M用 + D用） | 1回（全デバイス一括） |
| **応答統合** | MergeResponseData()必要 | 不要（1回で完結） |
| **出力形式** | CSV（追記モード） | JSON（ファイル単位） |
| **ファイル名** | 固定名 | タイムスタンプ+接続情報 |
| **plcModel** | 設定ファイルから取得 | 固定値"Unknown" |

### ✅ 対応済み箇所

1. **DataOutputManager.OutputToJson()**:
   - `data.ProcessedData.Keys`でデバイス名キー取得
   - `data.ProcessedData.Values.Select(d => d.Value)`でデータ値取得
   - JSON形式で構造化された出力
   - ファイル名に接続情報とタイムスタンプを含む
   - plcModelは固定値"Unknown"を使用

2. **LoggingManager.LogDataAcquisition()**:
   - `data.ProcessedData.Keys.Take(5)`でデバイス名リスト取得
   - `data.ProcessedData.Count`でデバイス総数取得

3. **DataOutputManagerTests**:
   - ProcessedResponseData.ProcessedDataプロパティを使用
   - `Dictionary<string, DeviceData>`でテストデータ構築
   - `DeviceData.FromDeviceSpecification()`でDeviceDataオブジェクト生成
   - JSON構造の検証（source, timestamp, items）
   - ファイル名生成の検証
   - plcModelが"Unknown"であることを検証

### 📋 Phase5への依存関係

Phase7の実装には以下のPhase5での実装が前提条件:
1. **DeviceDataクラスの定義** (`andon/Core/Models/DeviceData.cs`)
2. **ProcessedResponseDataの更新** (`DeviceData`プロパティ追加)
3. **SlmpDataParser.ParseReadRandomResponse()** (Dictionary<string, DeviceData>を返却)

### 🔗 関連ドキュメント

- Phase4: `documents/design/read_random実装/実装計画/Phase4_通信マネージャーの修正.md`
- Phase5: `documents/design/read_random実装/実装計画/Phase5_レスポンス処理の修正.md`
- フレーム構築: `documents/design/フレーム構築関係/フレーム構築方法.md`

---

**作成日**: 2025-11-18
**元ドキュメント**: read_to_readrandom_migration_plan.md
**最終更新**: 2025-11-25 (JSON出力形式対応)
