# TC116_Step3to5_UDP完全サイクル正常動作 修正記録

**修正日**: 2025年11月27日 15:30
**修正者**: Claude Code Assistant
**ステータス**: ✅ 修正完了（ビルドエラー解消後にテスト実行可能）

---

## 1. 問題の概要

### テスト失敗の症状
- **テスト名**: TC116_Step3to5_UDP完全サイクル正常動作
- **失敗箇所**: `Step3_6_IntegrationTests.cs:162`
- **エラー内容**:
  ```
  Assert.True() Failure
  Expected: True
  Actual: False
  ```
- **詳細メッセージ**:
  ```
  M機器応答データ長が不足: 期待>=250, 実際=12, データ=D4001234FFFF
  ```

### 問題の本質
1. MockUdpServerが不完全な4Eフレーム構造のレスポンスを生成していた
2. 受信データが12文字（デフォルトエラーレスポンス）のみだった
3. 期待値は250文字以上（M機器）、4000文字以上（D機器）

---

## 2. 根本原因の詳細分析

### 2.1 MockUdpServerのフレーム構造問題

**問題箇所**: `MockUdpServer.cs:76-116` (CreateMDeviceResponse)

**修正前のコード**:
```csharp
public static string CreateMDeviceResponse()
{
    // 不完全なレスポンス生成（予約フィールド、ヘッダー情報が欠落）
    var responseBuilder = new StringBuilder();
    responseBuilder.Append("D400");    // サブヘッダのみ
    responseBuilder.Append("1234");    // シーケンス番号のみ
    responseBuilder.Append("FFFF");    // 不完全
    return responseBuilder.ToString(); // わずか12文字
}
```

**4Eフレーム仕様（Binary形式）との比較**:

| フィールド | サイズ | 必須 | 修正前 | 修正後 |
|-----------|--------|------|--------|--------|
| サブヘッダ | 2バイト(4文字) | ✅ | ✅ D400 | ✅ D400 |
| シーケンス番号 | 2バイト(4文字) | ✅ | ✅ 1234 | ✅ 1234 |
| 予約 | 2バイト(4文字) | ✅ | ❌ 欠落 | ✅ 0000 |
| ネットワーク番号 | 1バイト(2文字) | ✅ | ❌ 欠落 | ✅ 00 |
| PC番号 | 1バイト(2文字) | ✅ | ❌ 欠落 | ✅ FF |
| I/O番号 | 2バイト(4文字) | ✅ | ❌ 欠落 | ✅ FF03 |
| 局番 | 1バイト(2文字) | ✅ | ❌ 欠落 | ✅ 00 |
| データ長 | 2バイト(4文字) | ✅ | ❌ 欠落 | ✅ 7000 |
| 終了コード | 2バイト(4文字) | ✅ | ⚠️ FFFF(エラー) | ✅ 0000 |
| データ部 | 可変 | ✅ | ❌ 欠落 | ✅ 220文字 |

**合計サイズ**:
- 修正前: 12文字（6バイト）- 不完全なフレーム
- 修正後: 250文字（125バイト）- 完全な4Eフレーム構造

### 2.2 IsBinary設定の不一致問題

**問題箇所**: `Step3_6_IntegrationTests.cs:45`

**根本原因**:
1. TC116テストが`IsBinary=false`（ASCII形式）で送信していた
2. 16進数文字列 "5400..." がASCII文字として送信される
3. バイト表現: 0x35, 0x34, 0x30, 0x30... (ASCII "5", "4", "0", "0"...)
4. MockUdpServerはバイナリとしてConvert.ToHexString()で変換
5. 結果: "353430..." となり、登録された "5400..." とマッチせず
6. デフォルトエラーレスポンス "D4001234FFFF" (12文字)が返却される

**フレームマッチング失敗のフロー**:
```
TC116テスト (IsBinary=false)
  ↓
"5400..." (16進数文字列)
  ↓
SendFrameAsync() - ASCII変換
  ↓
バイト配列: [0x35, 0x34, 0x30, 0x30, ...] (ASCII)
  ↓
UDP送信
  ↓
MockUdpServer受信
  ↓
Convert.ToHexString() - バイナリとして解釈
  ↓
"353430..." (期待値: "5400...")
  ↓
_responseMap.TryGetValue() - マッチ失敗
  ↓
デフォルトエラーレスポンス "D4001234FFFF" (12文字)
```

### 2.3 PlcCommunicationManager.csの既存コンパイルエラー

**問題箇所**: `PlcCommunicationManager.cs:2740-2750`

**エラー内容**:
1. **Line 2743**: `ProcessedDeviceCount`プロパティが存在しない
   - `ProcessedResponseData`モデルには`ProcessedDeviceCount`プロパティがない
   - Phase5以降の設計では、デバイス数は`BasicProcessedDevices.Count`で取得

2. **Line 2745**: 型の不一致
   - `CombinedDWordDevices`は`List<CombinedDWordDevice>`型を期待
   - 代入しようとした値は`List<ProcessedDevice>`型
   - Phase3.5でDWord結合機能が廃止されたため、空リストで初期化すべき

3. **Line 2750**: `RawDataLength`プロパティが存在しない
   - `ProcessedResponseData`モデルには`RawDataLength`プロパティがない
   - Phase5以降の設計では、生データ長は`BasicProcessedData.TotalDataSizeBytes`で取得

---

## 3. 実施した修正内容

### 修正1: MockUdpServer.cs - M機器応答データの完全実装

**ファイル**: `andon/Tests/TestUtilities/Mocks/MockUdpServer.cs`
**修正行**: 76-116行目
**修正日時**: 2025年11月27日 14:45

**修正後のコード**:
```csharp
/// <summary>
/// M機器応答データ（125バイト → 250文字）を作成
/// 4Eフレーム（Binary形式）の正しい構造に従う
/// </summary>
/// <returns>M機器応答データ（16進数文字列）</returns>
public static string CreateMDeviceResponse()
{
    var responseBuilder = new StringBuilder();

    // 4Eフレーム（Binary形式）応答構造
    // サブヘッダ（2バイト = 4文字）
    responseBuilder.Append("D400");

    // シーケンス番号（2バイト = 4文字）
    responseBuilder.Append("1234");

    // 予約（2バイト = 4文字）
    responseBuilder.Append("0000");

    // ネットワーク番号（1バイト = 2文字）
    responseBuilder.Append("00");

    // PC番号（1バイト = 2文字）
    responseBuilder.Append("FF");

    // I/O番号（2バイト = 4文字、リトルエンディアン）
    responseBuilder.Append("FF03");

    // 局番（1バイト = 2文字）
    responseBuilder.Append("00");

    // データ長（2バイト = 4文字、リトルエンディアン）
    // データ長 = 終了コード(2バイト) + データ部(110バイト) = 112バイト = 0x0070
    responseBuilder.Append("7000");

    // 終了コード（2バイト = 4文字）
    responseBuilder.Append("0000");

    // ヘッダー合計: 4+4+4+2+2+4+2+4+4 = 30文字（15バイト）
    // データ部: 250 - 30 = 220文字（110バイト）

    // M000-M999データ（110バイト = 220文字）
    responseBuilder.Append(new string('0', 220));

    return responseBuilder.ToString(); // 合計250文字（125バイト）
}
```

**フレーム構造の詳細**:
```
位置   フィールド          値      説明
----------------------------------------------
0-3    サブヘッダ         D400    4E Binary応答
4-7    シーケンス番号     1234    要求のエコーバック
8-11   予約              0000    固定値
12-13  ネットワーク番号   00      ネットワーク設定
14-15  PC番号            FF      全局指定
16-19  I/O番号           FF03    リトルエンディアン(0x03FF)
20-21  局番              00      マルチドロップ番号
22-25  データ長          7000    リトルエンディアン(0x0070=112バイト)
26-29  終了コード        0000    正常終了
30-249 データ部          0..0    M000-M999データ(220文字=110バイト)
----------------------------------------------
合計: 250文字 = 125バイト
```

### 修正2: MockUdpServer.cs - D機器応答データの完全実装

**ファイル**: `andon/Tests/TestUtilities/Mocks/MockUdpServer.cs`
**修正行**: 118-163行目
**修正日時**: 2025年11月27日 14:45

**修正後のコード**:
```csharp
/// <summary>
/// D機器応答データ（2000バイト → 4000文字）を作成
/// 4Eフレーム（Binary形式）の正しい構造に従う
/// </summary>
/// <returns>D機器応答データ（16進数文字列）</returns>
public static string CreateDDeviceResponse()
{
    var responseBuilder = new StringBuilder();

    // 4Eフレーム（Binary形式）応答構造
    // サブヘッダ（2バイト = 4文字）
    responseBuilder.Append("D400");

    // シーケンス番号（2バイト = 4文字）
    responseBuilder.Append("1234");

    // 予約（2バイト = 4文字）
    responseBuilder.Append("0000");

    // ネットワーク番号（1バイト = 2文字）
    responseBuilder.Append("00");

    // PC番号（1バイト = 2文字）
    responseBuilder.Append("FF");

    // I/O番号（2バイト = 4文字、リトルエンディアン）
    responseBuilder.Append("FF03");

    // 局番（1バイト = 2文字）
    responseBuilder.Append("00");

    // データ長（2バイト = 4文字、リトルエンディアン）
    // データ長 = 終了コード(2バイト) + データ部(1985バイト) = 1987バイト = 0x07C3
    responseBuilder.Append("C307");

    // 終了コード（2バイト = 4文字）
    responseBuilder.Append("0000");

    // ヘッダー合計: 4+4+4+2+2+4+2+4+4 = 30文字（15バイト）
    // データ部: 4000 - 30 = 3970文字（1985バイト）

    // D000-D999データ（1985バイト = 3970文字）
    responseBuilder.Append(new string('1', 3970));

    return responseBuilder.ToString(); // 合計4000文字（2000バイト）
}
```

**フレーム構造の詳細**:
```
位置      フィールド          値      説明
----------------------------------------------
0-3       サブヘッダ         D400    4E Binary応答
4-7       シーケンス番号     1234    要求のエコーバック
8-11      予約              0000    固定値
12-13     ネットワーク番号   00      ネットワーク設定
14-15     PC番号            FF      全局指定
16-19     I/O番号           FF03    リトルエンディアン(0x03FF)
20-21     局番              00      マルチドロップ番号
22-25     データ長          C307    リトルエンディアン(0x07C3=1987バイト)
26-29     終了コード        0000    正常終了
30-3999   データ部          1..1    D000-D999データ(3970文字=1985バイト)
----------------------------------------------
合計: 4000文字 = 2000バイト
```

### 修正3: Step3_6_IntegrationTests.cs - IsBinary設定の修正

**ファイル**: `andon/Tests/Integration/Step3_6_IntegrationTests.cs`
**修正行**: 45行目
**修正日時**: 2025年11月27日 15:00

**修正前**:
```csharp
var connectionConfig = new ConnectionConfig
{
    IpAddress = "127.0.0.1",
    Port = 5000,
    UseTcp = false,                    // UDP使用
    IsBinary = false,                  // ASCII形式 ← 問題箇所
    FrameVersion = FrameVersion.Frame4E
};
```

**修正後**:
```csharp
var connectionConfig = new ConnectionConfig
{
    IpAddress = "127.0.0.1",
    Port = 5000,
    UseTcp = false,                    // UDP使用
    IsBinary = true,                   // Binary形式（16進数文字列をバイナリに変換）
    FrameVersion = FrameVersion.Frame4E
};
```

**修正の効果**:
- `IsBinary=true`により、16進数文字列 "5400..." がバイナリバイト配列に正しく変換される
- バイト配列: [0x54, 0x00, 0x12, 0x34, ...] (正しいバイナリ表現)
- MockUdpServerで`Convert.ToHexString()`実行時: "5400..." と一致
- 登録された応答データが正しく返却される

### 修正4: PlcCommunicationManager.cs - 既存コンパイルエラーの解消

**ファイル**: `andon/Core/Managers/PlcCommunicationManager.cs`
**修正行**: 2740-2750行目
**修正日時**: 2025年11月27日 15:15

**修正前（エラーコード）**:
```csharp
fullCycleResult.ProcessedData = new ProcessedResponseData
{
    IsSuccess = fullCycleResult.BasicProcessedData.IsSuccess,
    BasicProcessedDevices = fullCycleResult.BasicProcessedData.ProcessedDevices,
    CombinedDWordDevices = fullCycleResult.BasicProcessedData.ProcessedDevices, // ❌ 型エラー
    ProcessedAt = DateTime.UtcNow,
    ProcessingTimeMs = fullCycleResult.BasicProcessedData.ProcessingTimeMs,
    Errors = fullCycleResult.BasicProcessedData.Errors,
    Warnings = fullCycleResult.BasicProcessedData.Warnings,
    ProcessedDeviceCount = fullCycleResult.BasicProcessedData.ProcessedDeviceCount, // ❌ 存在しない
    RawDataLength = fullCycleResult.BasicProcessedData.RawDataLength // ❌ 存在しない
};
```

**修正後**:
```csharp
// Phase3.5: DWord結合処理を廃止し、BasicProcessedData → ProcessedDataの変換処理に置き換え
// ReadRandomコマンドでは各デバイスを個別に指定するため、DWord結合は不要
fullCycleResult.ProcessedData = new ProcessedResponseData
{
    IsSuccess = fullCycleResult.BasicProcessedData.IsSuccess,
    BasicProcessedDevices = fullCycleResult.BasicProcessedData.ProcessedDevices,
    CombinedDWordDevices = new List<CombinedDWordDevice>(), // 空リスト（DWord結合機能廃止）
    ProcessedAt = DateTime.UtcNow,
    ProcessingTimeMs = fullCycleResult.BasicProcessedData.ProcessingTimeMs,
    Errors = fullCycleResult.BasicProcessedData.Errors,
    Warnings = fullCycleResult.BasicProcessedData.Warnings
    // RawDataLengthプロパティは存在しないため削除
};
```

**エラー解消の詳細**:

1. **ProcessedDeviceCount削除**:
   - `ProcessedResponseData`にこのプロパティは存在しない
   - デバイス数は`BasicProcessedDevices.Count`で取得可能
   - 不要な参照のため削除

2. **CombinedDWordDevices型修正**:
   - 期待される型: `List<CombinedDWordDevice>`
   - 誤った値: `fullCycleResult.BasicProcessedData.ProcessedDevices` (型: `List<ProcessedDevice>`)
   - 正しい値: `new List<CombinedDWordDevice>()` (空リスト)
   - Phase3.5でDWord結合機能が廃止されたため、空リストで初期化

3. **RawDataLength削除**:
   - `ProcessedResponseData`にこのプロパティは存在しない
   - 生データ長は`BasicProcessedData.TotalDataSizeBytes`で取得可能
   - 不要な参照のため削除

---

## 4. テスト実行結果

### 現状
⚠️ **修正完了したが、別問題（DWord関連エラー27件）によりビルド失敗**

### ビルドエラーの詳細
```
エラー CS0246: 型または名前空間の名前 'DWordCombineInfo' が見つかりませんでした
(using ディレクティブまたはアセンブリ参照が指定されていることを確認してください。)

エラー箇所:
- PlcCommunicationManager_IntegrationTests_TC143_10.cs (複数箇所)
- Step3_6_IntegrationTests.cs (複数箇所)
- PlcCommunicationManagerTests_ParseRawToStructuredData.cs (複数箇所)

合計: 27件のコンパイルエラー
```

### エラーの原因
- Phase3.5でDWord結合機能が完全に廃止された
- `DWordCombineInfo`クラスが削除された
- 既存のテストコードが未更新のまま残っている
- これらはTC116修正とは無関係な既存の問題

### TC116修正の検証状況
✅ **コード修正完了**:
- MockUdpServer: 4Eフレーム構造の完全実装
- Step3_6_IntegrationTests: IsBinary設定修正
- PlcCommunicationManager: 既存コンパイルエラー解消

⚠️ **テスト実行未確認**:
- DWord関連のビルドエラー27件により、テスト実行不可
- ビルドエラー解消後にTC116テストの成功を検証可能

---

## 5. 技術的な学び

### 5.1 SLMP 4Eフレーム構造の理解

**完全な4Eフレーム（Binary形式）の構造**:
```
┌─────────────────────────────────────────────────────────────┐
│ 4E Binary Response Frame Structure (125 bytes total)       │
├─────────────────────────────────────────────────────────────┤
│ Field Name          │ Size    │ Example │ Description       │
├─────────────────────┼─────────┼─────────┼──────────────────┤
│ Sub-header          │ 2 bytes │ D4 00   │ 4E Binary marker │
│ Sequence Number     │ 2 bytes │ 12 34   │ Request echo     │
│ Reserved            │ 2 bytes │ 00 00   │ Fixed value      │
│ Network Number      │ 1 byte  │ 00      │ Network setting  │
│ PC Number           │ 1 byte  │ FF      │ All stations     │
│ I/O Number          │ 2 bytes │ FF 03   │ Little-endian    │
│ Station Number      │ 1 byte  │ 00      │ Multi-drop       │
│ Data Length         │ 2 bytes │ 70 00   │ 112 bytes (LE)   │
│ End Code            │ 2 bytes │ 00 00   │ Normal end       │
│ Device Data         │ 110 B   │ ...     │ M000-M999 data   │
└─────────────────────┴─────────┴─────────┴──────────────────┘
Header: 15 bytes (30 hex chars)
Data: 110 bytes (220 hex chars)
Total: 125 bytes (250 hex chars)
```

**重要なポイント**:
1. **データ長フィールド**: 終了コード以降のバイト数（終了コード含む）
2. **リトルエンディアン**: I/O番号、データ長など2バイトフィールドは下位バイトが先
3. **予約フィールド**: 必須フィールドで、省略すると不完全なフレームになる

### 5.2 IsBinaryフラグの動作理解

**SendFrameAsync()の処理フロー**:

```
IsBinary = true の場合:
"5400..." (hex string)
    ↓ ConvertHexStringToBytes()
[0x54, 0x00, 0x12, 0x34, ...] (binary bytes)
    ↓ Socket.SendAsync()
UDP/TCP送信
    ↓ MockUdpServer受信
[0x54, 0x00, 0x12, 0x34, ...]
    ↓ Convert.ToHexString()
"5400..." ✅ マッチング成功

IsBinary = false の場合:
"5400..." (hex string)
    ↓ Encoding.ASCII.GetBytes()
[0x35, 0x34, 0x30, 0x30, ...] (ASCII bytes)
    ↓ Socket.SendAsync()
UDP/TCP送信
    ↓ MockUdpServer受信
[0x35, 0x34, 0x30, 0x30, ...]
    ↓ Convert.ToHexString()
"353430..." ❌ マッチング失敗
```

**教訓**:
- Binary形式のSLMP通信では必ず`IsBinary=true`を設定
- ASCII形式のSLMP通信では`IsBinary=false`だが、フレーム文字列自体がASCII文字列である必要がある
- テストでバイナリフレームを使用する場合は、IsBinary設定に注意

### 5.3 Phase3.5設計変更の影響

**DWord結合機能の廃止理由**:
- ReadRandomコマンド(0x0403)では各デバイスを個別に指定可能
- 32ビット値が必要な場合、下位ワードと上位ワードを別々に指定
- DWord結合処理が不要になり、コードが単純化された

**影響範囲**:
1. **削除されたクラス**:
   - `DWordCombineInfo`
   - `CombinedDWordDevice`（一部残存）

2. **変更されたモデル**:
   - `ProcessedResponseData`: `CombinedDWordDevices`は空リストで初期化
   - `ProcessedDeviceRequestInfo`: `DWordCombineTargets`は空リストで初期化

3. **修正が必要な箇所**:
   - 既存テストコード（27箇所）
   - DWord結合を前提としたテストロジック

---

## 6. 残存課題

### 課題1: DWord関連コンパイルエラー（27件）

**影響ファイル**:
1. `PlcCommunicationManager_IntegrationTests_TC143_10.cs`
2. `Step3_6_IntegrationTests.cs` (TC119, TC121など)
3. `PlcCommunicationManagerTests_ParseRawToStructuredData.cs`

**エラー内容**:
- `DWordCombineInfo`型が見つからない
- `DWordCombineTargets`プロパティが存在しない
- DWord結合を前提としたテストロジックが動作しない

**解決方法**:
1. **短期対応**: 該当テストコードから`DWordCombineInfo`参照を削除
2. **中期対応**: DWord結合を前提としたテストロジックをReadRandom方式に書き換え
3. **長期対応**: Phase3.5設計に完全準拠したテストスイート整備

**優先度**: 🔴 高（TC116テスト実行の前提条件）

### 課題2: TC116テスト実行検証

**現状**: 修正完了だが実行未確認

**検証手順**:
1. DWord関連エラー27件を解消
2. ビルド成功を確認
3. TC116テスト単体実行
4. テスト結果確認（期待: Pass）

**期待される成功条件**:
- M機器レスポンス: 250文字以上受信
- D機器レスポンス: 4000文字以上受信
- 全てのAssertがPass
- UDP完全サイクル（接続→送信→受信→切断）が正常完了

---

## 7. まとめ

### 実施した修正（3箇所）

1. **MockUdpServer.cs** (Lines 75-163)
   - M機器応答: 12文字 → 250文字（完全な4Eフレーム）
   - D機器応答: 12文字 → 4000文字（完全な4Eフレーム）
   - 予約フィールド、ヘッダー情報を完全実装

2. **Step3_6_IntegrationTests.cs** (Line 45)
   - `IsBinary = false` → `IsBinary = true`
   - 16進数文字列を正しくバイナリに変換

3. **PlcCommunicationManager.cs** (Lines 2740-2750)
   - 存在しないプロパティ参照を削除
   - `CombinedDWordDevices`を空リストで初期化

### 成果

✅ **TC116修正完了**:
- 4Eフレーム構造の完全実装
- IsBinary設定の修正
- 既存コンパイルエラーの解消

⚠️ **検証保留**:
- DWord関連エラー27件により実行未確認
- ビルドエラー解消後に検証可能

### 次のステップ

1. **即座に必要**: DWord関連エラー27件の解消
2. **その後**: TC116テスト実行検証
3. **最終確認**: UDP完全サイクルの動作確認

---

**参照文書**:
- `C:\Users\1010821\Desktop\python\andon\documents\design\フレーム構築関係\フレーム構築方法.md`
- `C:\Users\1010821\Desktop\python\andon\memo.md`
- `C:\Users\1010821\Desktop\python\andon\documents\design\フレーム構築関係\既存コード修正\既存コード修正_テスト失敗30件分析結果.md`

**関連ファイル**:
- `andon/Tests/TestUtilities/Mocks/MockUdpServer.cs`
- `andon/Tests/Integration/Step3_6_IntegrationTests.cs`
- `andon/Core/Managers/PlcCommunicationManager.cs`
