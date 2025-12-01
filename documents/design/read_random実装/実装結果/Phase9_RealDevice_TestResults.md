# Phase9: 実機テスト結果

## テスト実施情報
- **実施日**: 2025-12-01
- **テスト環境**: PLC実機環境（UDP通信）
- **PLC機種**: 三菱電機 Q00UDECPU相当
- **接続設定**:
  - IP: 172.30.40.15
  - Port: 8192
  - Protocol: UDP
  - Frame: 4E Binary
  - Timeout: 500ms

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
[INFO] PLC communication cycle started - IP:172.30.40.15:8192
[DEBUG] Step3: Connecting to PLC - IP:172.30.40.15, Port:8192, Protocol:UDP, Binary:True
[DEBUG] Step3: UDP connection established
[DEBUG] Step4: Sending frame - Size:25 bytes
[DEBUG] Step4: Frame sent successfully
[DEBUG] Step5: Waiting for response...
[DEBUG] Step5: Response received - Size:17 bytes
[DEBUG] Step6: Parsing received data
[DEBUG] Frame type detected: 4E Binary
[DEBUG] Response subheader: 0xD4 0x00
[DEBUG] Sequence number: 0x0000
[DEBUG] Response parsing - DataLength from header: 2
[DEBUG] End code: 0x0000 (Success)
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

#### 根本原因
`ExecutionOrchestrator.cs` 199行目で空の`ProcessedDeviceRequestInfo`オブジェクトを作成しているため、以下のプロパティが未初期化:

```csharp
// ExecutionOrchestrator.cs:199
var deviceRequestInfo = new ProcessedDeviceRequestInfo();  // ❌ 空のまま

// これにより以下のプロパティがデフォルト値のまま:
// - DeviceType → "" (空文字列)
// - StartAddress → 0
// - Count → 0
// - FrameType → デフォルト値
```

この空の`ProcessedDeviceRequestInfo`が`PlcCommunicationManager.ExtractDeviceValues()`に渡されると、
`switch (requestInfo.DeviceType.ToUpper())`で空文字列に対してマッチするケースがなく、
`default:`ケースで`NotSupportedException`がスローされる。

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

### 短期対策（Phase10実施前の応急対応）

以下のいずれかの方法で緊急対応が可能:

#### 案1: DeviceSpecificationsプロパティの再追加
```csharp
// ProcessedDeviceRequestInfo.cs
public class ProcessedDeviceRequestInfo
{
    // 既存プロパティ
    public string DeviceType { get; set; } = string.Empty;
    public int StartAddress { get; set; }
    public int Count { get; set; }

    // 再追加
    public List<DeviceSpecification>? DeviceSpecifications { get; set; }
}
```

#### 案2: ヘルパーメソッドの追加
```csharp
// ExecutionOrchestrator.cs
public static ProcessedDeviceRequestInfo CreateFromConfig(PlcConfiguration config)
{
    return new ProcessedDeviceRequestInfo
    {
        DeviceType = config.Devices.FirstOrDefault()?.DeviceCode.ToString() ?? "",
        StartAddress = config.Devices.FirstOrDefault()?.Address ?? 0,
        Count = config.Devices.Count,
        FrameType = config.FrameType == "3E" ? FrameType.Frame3E : FrameType.Frame4E,
        DeviceSpecifications = config.Devices
    };
}
```

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
0000: 54 00 00 00 00 00 00 FF FF 03 00 48 00 20 00 03  T..........H. ..
0010: 04 00 00 10 00 48 EE 00 A8                       .....H...
```

### 受信フレーム（17バイト）
```
0000: D4 00 00 00 00 00 00 FF FF 03 00 04 00 00 00 B7  ................
0010: 03                                               .
```

### フレーム解析結果
- **フレームタイプ**: 4E Binary（サブヘッダ: 0xD4 0x00）
- **シーケンス番号**: 0x0000
- **終了コード**: 0x0000（正常）
- **データ長**: 4バイト（ヘッダーフィールド値）
- **実データ長**: 2バイト（終了コード2バイトを除く）
- **デバイスデータ**: `B7 03`（リトルエンディアン → 0x03B7 = 951）

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
**最終更新**: 2025-12-01
