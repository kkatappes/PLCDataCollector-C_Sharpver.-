# Phase9: 実機テスト結果

## テスト実施情報
- **実施日**: 2025-12-02
- **テスト環境**: PLC実機環境（UDP通信）
- **実行場所**: C:\Users\PPESAdmin\Desktop\x
- **設定ファイル**: ./config/test.json
- **デバイス指定**: D0（1点読み出し）
- **PLC機種**: 三菱電機 Q00UDECPU相当
- **接続設定**:
  - IP: 172.30.40.15
  - Port: 8192
  - Protocol: UDP
  - Frame: 4E Binary
  - Timeout: 1000ms（設定値）
- **監視インターバル**: 2000ms

---

## テスト結果サマリー

### ✅ 成功した項目
1. **ネットワーク接続**: PLCへのUDP接続成功
2. **フレーム送信**: 25バイトのReadRandomフレーム送信成功
3. **フレーム受信**: 17バイトのレスポンスフレーム受信成功
4. **フレーム解析**: 4E Binaryフレームとして正しく解析完了
   - サブヘッダ検証: 0xD4 0x00 ✅
   - シーケンス番号: 0x0000 ✅
   - 終了コード: 0x0000（正常）✅
   - データ長: 2バイト（期待通り）✅

### ❌ 失敗した項目
5. **デバイスデータ抽出**: 受信データからのデバイス値抽出が失敗

---

## 詳細ログ

### 成功ログ（フレーム解析まで）

```
info: Andon.Core.Managers.LoggingManager[0]
      AndonHostedService starting
info: Andon.Core.Managers.LoggingManager[0]
      Starting Step1 initialization
info: Andon.Core.Managers.MultiPlcConfigManager[0]
      設定を追加: test（デバイス数: 1）
[INFO] BitExpansion設定読み込み完了: Enabled=False
info: Andon.Core.Managers.LoggingManager[0]
      Step1 initialization completed
info: Andon.Core.Managers.LoggingManager[0]
      Started monitoring configuration directory: ./config/
[INFO] Starting timer with interval: 2000ms

[INFO] 完全サイクル開始: サーバー=172.30.40.15:8192
[ConnectAsync] === PLC Connection Start ===
[ConnectAsync] Target: 172.30.40.15:8192
[ConnectAsync] Protocol: UDP
[INFO] UDP connection established (verification skipped) - 172.30.40.15:8192
[ConnectAsync] Connected successfully in 8.42ms
[ConnectAsync] === PLC Connection Complete ===
[INFO] Step3完了: 接続成功、所要時間=19ms

[DEBUG] Sending Binary frame, 25 bytes
[DEBUG] First 20 bytes: 0x54 0x00 0x00 0x00 0x00 0x00 0x00 0xFF 0xFF 0x03 0x00 0x0C 0x00 0x04 0x00 0x03 0x04 0x00 0x00 0x01
[SendFrameAsync] Sent 25 bytes in 65.11ms
[SendFrameAsync] === Frame Transmission Complete ===
[INFO] Step4-送信完了: 25バイト、所要時間=89ms

[ReceiveResponseAsync] === Frame Reception Start ===
[ReceiveResponseAsync] Source: 172.30.40.15:8192
[ReceiveResponseAsync] Timeout: 1000ms
[受信] フレーム受信完了 (1.34ms)
[DEBUG] Binary frame detected
[ReceiveResponseAsync] Frame type detected: Frame4E_Binary
[INFO] Step4-受信完了: 17バイト、所要時間=23ms

[INFO] ProcessReceivedRawData開始: データ長=17バイト, デバイス=0, 開始時刻=15:38:06.434
[DEBUG] Binary frame detected
[INFO] フレームタイプ自動判定成功: Frame4E_Binary
[WARNING] 要求フレームタイプ(Frame3E_Binary)と検出フレームタイプ(Frame4E_Binary)が不一致。検出値を優先します。
[DEBUG] SLMPフレーム解析開始: フレーム形式=Frame4E_Binary
[DEBUG] 4Eフレーム解析: シーケンス番号=0x0000, データ長=4, 終了コード=0x0000, デバイスデータ長=2バイト
[DEBUG] Device count validation: DeviceType=, FromHeader=1, FromActualData=1, FromRequest=0
```

### エラーログ（データ抽出時）

```
[DEBUG] Device count validation: DeviceType=, FromHeader=1, FromActualData=1, FromRequest=0
[ERROR] ProcessReceivedRawData 未サポート機能エラー: サポートされていないデータ型です:
   at Andon.Core.Managers.PlcCommunicationManager.ExtractDeviceValues(Byte[] deviceData, ProcessedDeviceRequestInfo requestInfo, DateTime processedAt)
   at Andon.Core.Managers.PlcCommunicationManager.ProcessReceivedRawData(Byte[] rawData, ProcessedDeviceRequestInfo requestInfo, FrameType frameType)
```

---

## 発見された問題

### 問題1: ProcessedDeviceRequestInfo未初期化エラー

#### 症状
- **エラーメッセージ**: `サポートされていないデータ型です: `
- **発生箇所**: `PlcCommunicationManager.ExtractDeviceValues()`
- **デバッグ出力**: `DeviceType=` (空文字列), `FromHeader=1`, `FromActualData=1`, `FromRequest=0`

#### 現在の実装状況（コード分析結果）

**ExecutionOrchestrator.cs:199-205（Phase8.5暫定対策）**
```csharp
var deviceRequestInfo = new ProcessedDeviceRequestInfo
{
    DeviceSpecifications = config.Devices?.ToList(), // ReadRandom用デバイス指定
    FrameType = config.FrameVersion == "4E" ? FrameType.Frame4E : FrameType.Frame3E,
    RequestedAt = DateTime.UtcNow
};
```

✅ **設定されているプロパティ**:
- `DeviceSpecifications` ← `config.Devices?.ToList()`
- `FrameType`
- `RequestedAt`

❌ **設定されていないプロパティ（デフォルト値のまま）**:
- `DeviceType` → `""` (空文字列)
- `StartAddress` → `0`
- `Count` → `0`

**PlcCommunicationManager.cs:1921-1949（ExtractDeviceValues）**
```csharp
private List<ProcessedDevice> ExtractDeviceValues(byte[] deviceData, ProcessedDeviceRequestInfo requestInfo, DateTime processedAt)
{
    // Phase8.5暫定対策: DeviceSpecificationsが設定されている場合はReadRandom処理
    if (requestInfo.DeviceSpecifications != null && requestInfo.DeviceSpecifications.Any())
    {
        return ExtractDeviceValuesFromReadRandom(deviceData, requestInfo, processedAt); // ← ここが実行されるべき
    }

    // 後方互換性: 既存の処理を維持（DeviceType/StartAddress/Countを使用）
    switch (requestInfo.DeviceType.ToUpper())  // ← 実際にはここが実行された
    {
        case "D": ...
        case "M": ...
        default:
            throw new NotSupportedException(string.Format(ErrorMessages.UnsupportedDataType, requestInfo.DeviceType));
    }
}
```

#### 根本原因

**問題の本質**:
`requestInfo.DeviceSpecifications`が`null`または空であるため、Phase8.5暫定対策のコードパス（1928行目）が実行されず、後方互換性のための既存処理パス（1932行目のswitch文）が実行された。

実機テストでは、ExtractDeviceValues()の1926行目の条件チェック`if (requestInfo.DeviceSpecifications != null && requestInfo.DeviceSpecifications.Any())`で`false`となり、1932行目の`switch (requestInfo.DeviceType.ToUpper())`が実行され、`DeviceType`が空文字列のためdefaultケースで`NotSupportedException`がスローされた。

**矛盾点**:
- ログには「設定を追加: test（デバイス数: 1）」と表示 → `config.Devices.Count == 1`
- しかし、実行時に`DeviceSpecifications`が空またはnull → `config.Devices?.ToList()`の結果が空またはnull

**考えられる原因**:
1. **`config.Devices`が実行時にnullまたは空になっている**
   - ApplicationController.cs:86で`_plcConfigs = configs.ToList()`
   - ExecutionOrchestrator.cs:168で`var config = plcConfigs[i]`
   - この間で何らかの理由で`config.Devices`が変更されている可能性

2. **デバッグログ不足**
   - ExecutionOrchestrator.cs:202で実際に`config.Devices`と`DeviceSpecifications`の値を出力していない
   - そのため、実際の状態が不明

3. **参照渡しの問題**
   - `List<PlcConfiguration>`の要素が参照型のため、他の箇所で変更された可能性

#### 影響範囲
- **重大度**: 🔴 **Critical**
- **影響**: ReadRandom(0x0403)コマンドによる実機データ取得が完全に不可能
- **影響するコマンド**: ReadRandom(0x0403)のみ（旧Read(0x0401)は既に削除済み）

#### アーキテクチャ上の根本的問題

この問題の背後には、設計上の根本的な矛盾が存在する:

1. **ReadRandom(0x0403)の仕様**:
   - 複数の任意デバイスを一度に読み出し可能
   - デバイス種別が混在可能（例: D100, M200, X10を同時指定）
   - `PlcConfiguration.Devices`は`List<DeviceSpecification>`（複数デバイス対応）

2. **ProcessedDeviceRequestInfoの設計**:
   - 旧Read(0x0401)コマンド用の設計
   - 単一の`DeviceType`、連続した`StartAddress`、`Count`のみ
   - 複数デバイス種別の混在に対応していない

3. **Phase3.5での変更**:
   - `DeviceSpecifications`プロパティが削除された（2025-11-27）
   - 削除により、複数デバイス情報を保持する手段が失われた

---

## 対策計画

### 短期対策1: デバッグログ追加（最優先）

ExecutionOrchestrator.cs:202の前後に以下のデバッグログを追加して、実際の状態を確認する：

```csharp
// デバッグログ追加
Console.WriteLine($"[DEBUG] Before creating deviceRequestInfo:");
Console.WriteLine($"[DEBUG]   config.Devices is null: {config.Devices == null}");
Console.WriteLine($"[DEBUG]   config.Devices.Count: {config.Devices?.Count ?? -1}");
if (config.Devices != null && config.Devices.Count > 0)
{
    Console.WriteLine($"[DEBUG]   First device: Type={config.Devices[0].DeviceType}, Number={config.Devices[0].DeviceNumber}");
}

var deviceRequestInfo = new ProcessedDeviceRequestInfo
{
    DeviceSpecifications = config.Devices?.ToList(), // ReadRandom用デバイス指定
    FrameType = config.FrameVersion == "4E" ? FrameType.Frame4E : FrameType.Frame3E,
    RequestedAt = DateTime.UtcNow
};

Console.WriteLine($"[DEBUG] After creating deviceRequestInfo:");
Console.WriteLine($"[DEBUG]   DeviceSpecifications is null: {deviceRequestInfo.DeviceSpecifications == null}");
Console.WriteLine($"[DEBUG]   DeviceSpecifications.Count: {deviceRequestInfo.DeviceSpecifications?.Count ?? -1}");
```

このログ出力により、以下が判明する：
- `config.Devices`が実際にnullかどうか
- `config.Devices`が空かどうか
- `DeviceSpecifications`への代入が成功しているかどうか

### 短期対策2: nullガード追加

デバッグログで原因が判明した後、以下のnullガードを追加：

```csharp
var deviceRequestInfo = new ProcessedDeviceRequestInfo
{
    // nullガード: config.Devicesがnullの場合は空リスト
    DeviceSpecifications = config.Devices?.ToList() ?? new List<DeviceSpecification>(),
    FrameType = config.FrameVersion == "4E" ? FrameType.Frame4E : FrameType.Frame3E,
    RequestedAt = DateTime.UtcNow,
    // 後方互換性のために以下も設定（DeviceSpecificationsが空の場合のフォールバック）
    DeviceType = config.Devices?.FirstOrDefault()?.DeviceType ?? "",
    StartAddress = config.Devices?.FirstOrDefault()?.DeviceNumber ?? 0,
    Count = config.Devices?.Count ?? 0
};
```

### 短期対策3: 設定ファイル検証

実機環境の設定ファイル（`C:\Users\PPESAdmin\Desktop\x\config\test.json`）の内容を確認し、Devicesプロパティが正しく定義されているか検証する。

### 長期対策（Phase12: アーキテクチャ再設計）

**Phase12: ProcessedDeviceRequestInfo再設計**として実施予定:

1. **ReadRandom専用情報クラスの導入**:
   ```csharp
   public class ReadRandomRequestInfo
   {
       public List<DeviceSpecification> Devices { get; set; }
       public FrameType FrameType { get; set; }
       public DateTime RequestedAt { get; set; }
   }
   ```

2. **コマンド種別による情報構造の分離**:
   - `IDeviceRequestInfo`インターフェースの導入
   - ReadRandom用/Read用の具象クラス分離
   - コマンドパターンの適用

3. **ExtractDeviceValuesの再設計**:
   - デバイス種別ごとのループ処理
   - 複数デバイス種別の混在対応

詳細は**Phase12実装計画書**を参照のこと。

---

## テスト環境情報

### PLC接続情報
- **IP Address**: 172.30.40.15
- **Port**: 8192
- **Protocol**: UDP
- **Frame Type**: 4E Binary
- **Timeout**: 500ms

### 送信フレーム（25バイト）
```
[送信] データサイズ: 25バイト
[送信] 生データ (HEX 1行):
  54 00 00 00 00 00 00 FF FF 03 00 0C 00 04 00 03 04 00 00 01 00 00 00 00 90

[送信] HEXダンプ:
  0000: 54 00 00 00 00 00 00 FF FF 03 00 0C 00 04 00 03  T...............
  0010: 04 00 00 01 00 00 00 00 90                       .........
```

**フレーム構造分析**:
- サブヘッダ: 0x54 0x00（4Eフレーム）
- シーケンス番号: 0x0000
- ネットワーク番号: 0x00
- PC番号: 0xFF
- I/O番号: 0xFF 0x03（LE: 0x03FF）
- マルチドロップ: 0x00
- データ長: 0x0C 0x00（LE: 12バイト）
- 監視タイマ: 0x04 0x00（LE: 4 = 1秒）
- コマンド: 0x03 0x04（LE: 0x0403 = ReadRandom）
- サブコマンド: 0x00 0x00
- ワード点数: 0x01（1点）
- Dword点数: 0x00（0点）
- デバイスコード: 0x90（D）
- デバイスアドレス: 0x00 0x00 0x00（LE: 0 = D0）

### 受信フレーム（17バイト）- 1回目（シーケンス: 0x0000）
```
[受信] データサイズ: 17バイト
[受信] 生データ (HEX 1行):
  D4 00 00 00 00 00 00 FF FF 03 00 04 00 00 00 21 05

[受信] HEXダンプ:
  0000: D4 00 00 00 00 00 00 FF FF 03 00 04 00 00 00 21  ...............!
  0010: 05                                               .
```

### 受信フレーム（17バイト）- 2回目（シーケンス: 0x0001）
```
[受信] データサイズ: 17バイト
[受信] 生データ (HEX 1行):
  D4 00 01 00 00 00 00 FF FF 03 00 04 00 00 00 21 05

[受信] HEXダンプ:
  0000: D4 00 01 00 00 00 00 FF FF 03 00 04 00 00 00 21  ...............!
  0010: 05                                               .
```

### フレーム解析結果
- **フレームタイプ**: 4E Binary（サブヘッダ: 0xD4 0x00）
- **シーケンス番号**: 0x0000（1回目）、0x0001（2回目）
- **終了コード**: 0x0000（正常）
- **データ長**: 4バイト（ヘッダーフィールド値）
- **実データ長**: 2バイト（終了コード2バイトを除く）
- **デバイスデータ**: `21 05`（リトルエンディアン → 0x0521 = 1313）

**受信詳細解析出力**:
```
[解析] 受信データ詳細解析開始
[解析] フレームタイプ: 4Eフレーム (Binary)
[解析] 終了コード: 0x0000 (正常終了)
[解析] デバイスデータ長: 2バイト
[解析] デバイスデータ（HEX）:
  0000: 21 05                                            !.
[解析] デバイス値抽出（ワード型、リトルエンディアン）:
  D  0: 0x0521 ( 1313) [Byte: 0x21 0x05]
[解析] 受信データ詳細解析完了
```

---

## 次のアクション

### 優先度1: 🔴 Phase12実装（最優先）
ProcessedDeviceRequestInfoの再設計を実施し、ReadRandomコマンドに適合したアーキテクチャに変更する。

**理由**: 現在のアーキテクチャでは実機データ取得が完全に不可能。Phase10（旧コード削除）を実施する前に、Phase12で動作するアーキテクチャを確立する必要がある。

### 優先度2: Phase10実施延期
Phase12完了後にPhase10（旧Read(0x0401)コード削除・クリーンアップ）を実施する。

**理由**: 現在のコードでは実機動作しないため、旧コード削除を急ぐ必要性が低い。まずは動作するシステムの確立を優先する。

---

## 参考資料

- **実装計画**: `documents/design/read_random実装/実装計画/Phase9_実機テスト.md`
- **根本原因分析**: 同上の「実機テストで発見された問題」セクション
- **Phase12計画**: `documents/design/read_random実装/実装計画/Phase12_ProcessedDeviceRequestInfo再設計.md`（作成予定）

---

**作成日**: 2025-12-01
**テスト担当**: Claude Code
**最終更新**: 2025-12-02（実機テスト実施、詳細ログ追加）
