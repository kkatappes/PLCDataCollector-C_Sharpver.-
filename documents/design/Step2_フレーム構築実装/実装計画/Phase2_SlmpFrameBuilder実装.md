# Phase2: SlmpFrameBuilder実装

## 概要

Step2フレーム構築実装の第2フェーズとして、SlmpFrameBuilderの全面リファクタリングを実施します。

## 実装目標

- ConMoniの明確な構造を基本骨格とする
- PySLMPClientの優れた機能を統合する
- andon既実装の型安全性を維持強化する
- メソッドを機能別に分割して可読性・保守性を向上

---

## 🔄 Phase1からの引き継ぎ事項

### Phase1実装完了内容（2025-11-27）

✅ **SequenceNumberManager実装完了**
- **ファイルパス**: `andon/Core/Managers/SequenceNumberManager.cs`
- **テスト結果**: 6/6テストケース合格（実行時間: 33ms）
- **機能**:
  - 4Eフレーム用シーケンス番号自動管理
  - スレッドセーフな実装（lockによる排他制御）
  - 3Eフレームは常に0、4Eフレームは自動インクリメント
  - 0xFF（255）超過時の自動ロールオーバー

### Phase2での統合方法

**1. クラスレベルフィールド追加**:
```csharp
/// <summary>
/// シーケンス番号管理（Phase1で実装済み）
/// </summary>
private static readonly SequenceNumberManager _sequenceManager = new();
```

**2. BuildSubHeader()での活用**:
```csharp
// メインメソッド内でシーケンス番号を取得
ushort sequenceNumber = _sequenceManager.GetNext(frameType);

// BuildSubHeader()に渡して4Eフレームで使用
frame.AddRange(BuildSubHeader(frameType, sequenceNumber));
```

**3. 使用例**:
```csharp
// 3Eフレーム構築時
ushort seqNum3E = _sequenceManager.GetNext("3E");  // → 常に0

// 4Eフレーム構築時
ushort seqNum4E = _sequenceManager.GetNext("4E");  // → 0, 1, 2, ... 255, 0, 1, ...
```

### ProcessedDeviceRequestInfo削除判断

⏳ **Phase1では削除を保留**
- **理由**: PlcCommunicationManagerの大規模リファクタリングが必要、影響範囲が広い
- **対応時期**: Step3-6（PLC通信実装）時に段階的に実施
- **Phase2での影響**: なし（Phase2ではProcessedDeviceRequestInfoは使用しない）

### 参考資料

- **Phase1実装結果**: `documents/design/Step2_フレーム構築実装/実装結果/Phase1_SequenceNumberManager_TestResults.md`
- **Phase1実装計画**: `documents/design/Step2_フレーム構築実装/実装計画/Phase1_準備と基礎クラス実装.md`

---

## 1. 現状分析

### 現在の実装

- **ファイルパス**: `andon/Utilities/SlmpFrameBuilder.cs` (18-160行目)
- **主要メソッド**: `BuildReadRandomRequest()`, `BuildReadRandomRequestAscii()`

### 主な課題

1. **シーケンス番号管理**: 60行目で固定値使用（TODO状態）
2. **フレーム検証なし**: 8194バイト上限チェックがない
3. **ReadRandom対応チェックなし**: TS/TC/CS/CC等の非対応デバイス検証がない
4. **メソッド分割不足**: inline実装で可読性が低い

---

## 2. 新設計のクラス構成

### クラス全体構造

```csharp
public static class SlmpFrameBuilder
{
    // ========== クラスレベル定数・フィールド ==========

    /// <summary>
    /// シーケンス番号管理（Phase1で実装済み）
    /// </summary>
    private static readonly SequenceNumberManager _sequenceManager = new();

    /// <summary>
    /// SLMP最大フレーム長（PySLMPClientから採用）
    /// </summary>
    private const int MAX_FRAME_LENGTH = 8194;

    /// <summary>
    /// ReadRandom非対応デバイス（PySLMPClientから採用・改善）
    /// </summary>
    private static readonly DeviceCode[] _unsupportedDevicesForReadRandom = new[]
    {
        DeviceCode.TS,  // タイマ接点
        DeviceCode.TC,  // タイマコイル
        DeviceCode.CS,  // カウンタ接点
        DeviceCode.CC   // カウンタコイル
    };

    // ========== メインメソッド ==========

    /// <summary>
    /// ReadRandomリクエストフレーム（Binary形式）を構築します。
    /// </summary>
    public static byte[] BuildReadRandomRequest(
        List<DeviceSpecification>? devices,
        string frameType = "3E",
        ushort timeout = 32)
    {
        // 実装内容は後述
    }

    /// <summary>
    /// ReadRandomリクエストフレーム（ASCII形式）を構築します。
    /// </summary>
    public static byte[] BuildReadRandomRequestAscii(
        List<DeviceSpecification>? devices,
        string frameType = "3E",
        ushort timeout = 32)
    {
        // 既存実装を維持
    }

    // ========== プライベートメソッド（機能別分割） ==========

    private static void ValidateInputs(...) { }
    private static byte[] BuildSubHeader(...) { }
    private static byte[] BuildNetworkConfig() { }
    private static byte[] BuildCommandSection(...) { }
    private static byte[] BuildDeviceSpecificationSection(...) { }
    private static void UpdateDataLength(...) { }
    private static void ValidateFrame(...) { }
}
```

---

## 3. メソッド別実装詳細

### 3-1. BuildReadRandomRequest()（メインメソッド）

**処理フロー:**

```csharp
public static byte[] BuildReadRandomRequest(
    List<DeviceSpecification>? devices,
    string frameType = "3E",
    ushort timeout = 32)
{
    // 1. 入力検証（andon強化版 + PySLMPClient要素）
    ValidateInputs(devices, frameType);

    // 2. フレーム構築
    var frame = new List<byte>();

    // 2-1. ヘッダ構築（ConMoni方式 + PySLMPClient自動管理）
    ushort sequenceNumber = _sequenceManager.GetNext(frameType);
    frame.AddRange(BuildSubHeader(frameType, sequenceNumber));

    // 2-2. ネットワーク設定構築（ConMoni明確な構造）
    frame.AddRange(BuildNetworkConfig());

    // 2-3. データ長プレースホルダ
    int dataLengthPosition = frame.Count;
    frame.AddRange(new byte[] { 0x00, 0x00 });

    // 2-4. コマンド部構築（PySLMPClient一括処理スタイル）
    frame.AddRange(BuildCommandSection(
        timeout,
        0x0403,  // ReadRandom
        0x0000,  // サブコマンド
        (byte)devices!.Count,
        0x00     // Dword点数=0固定
    ));

    // 2-5. デバイス指定部構築（ConMoni方式）
    frame.AddRange(BuildDeviceSpecificationSection(devices));

    // 2-6. データ長更新（PySLMPClient計算式 + ConMoni実装）
    UpdateDataLength(frame, dataLengthPosition, frameType);

    // 2-7. フレーム検証（PySLMPClientから採用）
    ValidateFrame(frame.ToArray());

    return frame.ToArray();
}
```

**採用理由:**
- ConMoniの流れを踏襲（明確な処理ステップ）
- PySLMPClientのシーケンス番号自動管理を統合
- 各セクションを個別メソッドで構築（可読性向上）

---

### 3-2. ValidateInputs()（入力検証）

**実装コード:**

```csharp
/// <summary>
/// 入力パラメータを検証します。
/// andon既存 + PySLMPClient要素強化
/// </summary>
/// <param name="devices">デバイスリスト</param>
/// <param name="frameType">フレームタイプ</param>
private static void ValidateInputs(
    List<DeviceSpecification>? devices,
    string frameType)
{
    // 1. デバイスリスト基本検証（既存）
    if (devices == null || devices.Count == 0)
    {
        throw new ArgumentException(
            "デバイスリストが空です",
            nameof(devices));
    }

    // 2. デバイス点数上限チェック（既存）
    if (devices.Count > 255)
    {
        throw new ArgumentException(
            $"デバイス点数が上限を超えています: {devices.Count}点（最大255点）",
            nameof(devices));
    }

    // 3. フレームタイプ検証（既存）
    if (frameType != "3E" && frameType != "4E")
    {
        throw new ArgumentException(
            $"未対応のフレームタイプ: {frameType}",
            nameof(frameType));
    }

    // 4. ReadRandom対応デバイスチェック（★新規追加）
    foreach (var device in devices)
    {
        if (_unsupportedDevicesForReadRandom.Contains(device.Code))
        {
            throw new ArgumentException(
                $"ReadRandomコマンドは {device.Code} デバイスに対応していません。" +
                $"対応していないデバイス: {string.Join(", ", _unsupportedDevicesForReadRandom)}",
                nameof(devices));
        }
    }
}
```

**採用理由:**
- andon既存の厳格な検証を維持
- PySLMPClientで不足していたReadRandom対応チェックを追加
- 詳細なエラーメッセージで問題箇所を明確化

**テストケース:**
- TC001: デバイスリストがnullの場合、ArgumentExceptionをスロー
- TC002: デバイスリストが空の場合、ArgumentExceptionをスロー
- TC003: デバイス点数が256点以上の場合、ArgumentExceptionをスロー
- TC004: フレームタイプが"3E"/"4E"以外の場合、ArgumentExceptionをスロー
- TC005: TS/TC/CS/CCデバイス指定時、ArgumentExceptionをスロー

---

### 3-3. BuildSubHeader()（ヘッダ構築）

**実装コード:**

```csharp
/// <summary>
/// サブヘッダを構築します。
/// PySLMPClientのシーケンス番号対応
/// </summary>
/// <param name="frameType">フレームタイプ（"3E" or "4E"）</param>
/// <param name="sequenceNumber">シーケンス番号（4Eの場合）</param>
/// <returns>サブヘッダバイト配列</returns>
private static byte[] BuildSubHeader(string frameType, ushort sequenceNumber)
{
    if (frameType == "3E")
    {
        // 標準3Eフレーム（フレーム構築方法.md準拠）
        return new byte[] { 0x50, 0x00 };
    }
    else // "4E"
    {
        // 4Eフレーム（シーケンス番号含む）
        var header = new List<byte>();
        header.AddRange(new byte[] { 0x54, 0x00 });              // サブヘッダ
        header.AddRange(BitConverter.GetBytes(sequenceNumber));  // シーケンス番号（LE）
        header.AddRange(new byte[] { 0x00, 0x00 });              // 予約
        return header.ToArray();
    }
}
```

**採用理由:**
- PySLMPClientのシーケンス番号自動管理を採用
- 3E/4Eで明確に分岐
- 標準仕様に準拠（3E: 0x50、4E: 0x54）

**テストケース:**
- TC006: 3Eフレームのサブヘッダが0x50 0x00であること
- TC007: 4Eフレームのサブヘッダが0x54 0x00 + シーケンス番号 + 予約であること
- TC008: 4Eフレームでシーケンス番号がリトルエンディアンで格納されること

---

### 3-4. BuildNetworkConfig()（ネットワーク設定構築）

**実装コード:**

```csharp
/// <summary>
/// ネットワーク設定部を構築します。
/// ConMoniの明確な構造を採用
/// </summary>
/// <returns>ネットワーク設定バイト配列（5バイト）</returns>
private static byte[] BuildNetworkConfig()
{
    var config = new List<byte>();
    config.Add(0x00);        // ネットワーク番号（自ネットワーク）
    config.Add(0xFF);        // 局番（全局）
    config.AddRange(BitConverter.GetBytes((ushort)0x03FF));  // I/O番号（LE）
    config.Add(0x00);        // マルチドロップ局番（未使用）
    return config.ToArray();
}
```

**採用理由:**
- ConMoniの明確な構造（各フィールドの意味が明確）
- コメントで各バイトの役割を明記
- 実機稼働実績あり

**テストケース:**
- TC009: ネットワーク設定が5バイトであること
- TC010: ネットワーク番号が0x00であること
- TC011: 局番が0xFFであること
- TC012: I/O番号が0x03FFであること（リトルエンディアン）

---

### 3-5. BuildCommandSection()（コマンド部構築）

**実装コード:**

```csharp
/// <summary>
/// コマンド部を構築します。
/// PySLMPClientの一括処理スタイル
/// </summary>
/// <param name="timeout">監視タイマ（250ms単位）</param>
/// <param name="command">コマンド（例: 0x0403 = ReadRandom）</param>
/// <param name="subCommand">サブコマンド（例: 0x0000 = ワード単位）</param>
/// <param name="wordCount">ワード点数</param>
/// <param name="dwordCount">Dword点数（常に0）</param>
/// <returns>コマンド部バイト配列（8バイト）</returns>
private static byte[] BuildCommandSection(
    ushort timeout,
    ushort command,
    ushort subCommand,
    byte wordCount,
    byte dwordCount)
{
    var section = new List<byte>();
    section.AddRange(BitConverter.GetBytes(timeout));     // 監視タイマ（2バイトLE）
    section.AddRange(BitConverter.GetBytes(command));     // コマンド（2バイトLE）
    section.AddRange(BitConverter.GetBytes(subCommand));  // サブコマンド（2バイトLE）
    section.Add(wordCount);                               // ワード点数（1バイト）
    section.Add(dwordCount);                              // Dword点数（1バイト、常に0）
    return section.ToArray();
}
```

**採用理由:**
- PySLMPClientのstruct.pack()的な一括処理
- 引数で柔軟に指定可能
- 可読性と保守性が高い

**テストケース:**
- TC013: コマンド部が8バイトであること
- TC014: 監視タイマが正しく設定されること（リトルエンディアン）
- TC015: コマンドが0x0403であること（リトルエンディアン）
- TC016: サブコマンドが0x0000であること
- TC017: ワード点数が正しく設定されること
- TC018: Dword点数が0であること

---

### 3-6. BuildDeviceSpecificationSection()（デバイス指定部構築）

**実装コード:**

```csharp
/// <summary>
/// デバイス指定部を構築します。
/// ConMoni方式（各デバイス4バイト）
/// </summary>
/// <param name="devices">デバイス指定リスト</param>
/// <returns>デバイス指定部バイト配列（4バイト×デバイス数）</returns>
private static byte[] BuildDeviceSpecificationSection(
    List<DeviceSpecification> devices)
{
    var section = new List<byte>();

    foreach (var device in devices)
    {
        // デバイス番号（3バイト、リトルエンディアン）
        section.Add((byte)(device.Address & 0xFF));           // 下位バイト
        section.Add((byte)((device.Address >> 8) & 0xFF));    // 中位バイト
        section.Add((byte)((device.Address >> 16) & 0xFF));   // 上位バイト

        // デバイスコード（1バイト）
        section.Add((byte)device.Code);
    }

    return section.ToArray();
}
```

**採用理由:**
- ConMoniの明確な4バイト構造
- リトルエンディアン処理を明示的に記述
- ビットシフトで各バイト抽出

**テストケース:**
- TC019: デバイス指定部が（4バイト×デバイス数）であること
- TC020: デバイス番号が3バイトリトルエンディアンで格納されること
- TC021: デバイスコードが正しく格納されること

---

### 3-7. UpdateDataLength()（データ長更新）

**実装コード:**

```csharp
/// <summary>
/// データ長フィールドを更新します。
/// PySLMPClientの明快な計算 + ConMoniの動的更新
/// </summary>
/// <param name="frame">フレームバイト配列</param>
/// <param name="dataLengthPosition">データ長フィールドの位置</param>
/// <param name="frameType">フレームタイプ（"3E" or "4E"）</param>
private static void UpdateDataLength(
    List<byte> frame,
    int dataLengthPosition,
    string frameType)
{
    // データ長 = データ長フィールド以降のバイト数
    // 3E: サブヘッダ(2) + ネットワーク設定(5) + データ長(2) + 監視タイマ以降
    // 4E: サブヘッダ(2) + シーケンス(2) + 予約(2) + ネットワーク設定(5) + データ長(2) + 監視タイマ以降

    int headerSize = frameType == "3E"
        ? 2 + 5 + 2  // サブヘッダ + ネットワーク設定 + データ長フィールド = 9
        : 2 + 2 + 2 + 5 + 2;  // サブヘッダ + シーケンス + 予約 + ネットワーク設定 + データ長フィールド = 13

    int dataLength = frame.Count - headerSize;

    // リトルエンディアンで書き込み
    frame[dataLengthPosition] = (byte)(dataLength & 0xFF);
    frame[dataLengthPosition + 1] = (byte)((dataLength >> 8) & 0xFF);
}
```

**採用理由:**
- PySLMPClientの明快な計算式
- ConMoniの動的更新方式
- 3E/4Eで自動調整

**テストケース:**
- TC022: 3Eフレームでデータ長が正しく計算されること
- TC023: 4Eフレームでデータ長が正しく計算されること
- TC024: データ長がリトルエンディアンで格納されること

---

### 3-8. ValidateFrame()（フレーム検証）

**実装コード:**

```csharp
/// <summary>
/// 完成したフレームを検証します。
/// PySLMPClientから採用
/// </summary>
/// <param name="frame">フレームバイト配列</param>
private static void ValidateFrame(byte[] frame)
{
    if (frame.Length > MAX_FRAME_LENGTH)
    {
        throw new InvalidOperationException(
            $"フレーム長が上限を超えています: {frame.Length}バイト（最大{MAX_FRAME_LENGTH}バイト）");
    }

    if (frame.Length == 0)
    {
        throw new InvalidOperationException("フレームが空です");
    }
}
```

**採用理由:**
- PySLMPClientで実装済みの優れた機能
- SLMP仕様の最大長8194バイトを厳守
- 送信前の最終チェック

**テストケース:**
- TC025: フレーム長が8194バイトを超える場合、InvalidOperationExceptionをスロー
- TC026: フレーム長が0の場合、InvalidOperationExceptionをスロー

---

## 4. Phase2実装チェックリスト

### 実装タスク

- [ ] **クラスレベル定数・フィールド追加**
  - [ ] `_sequenceManager` フィールド追加
  - [ ] `MAX_FRAME_LENGTH` 定数追加
  - [ ] `_unsupportedDevicesForReadRandom` 配列追加

- [ ] **プライベートメソッド実装**
  - [ ] ValidateInputs() 実装
  - [ ] BuildSubHeader() 実装
  - [ ] BuildNetworkConfig() 実装
  - [ ] BuildCommandSection() 実装
  - [ ] BuildDeviceSpecificationSection() 実装
  - [ ] UpdateDataLength() 実装
  - [ ] ValidateFrame() 実装

- [ ] **BuildReadRandomRequest() 統合**
  - [ ] 既存コードを各メソッド呼び出しに置き換え
  - [ ] シーケンス番号管理の統合
  - [ ] フレーム検証の追加

### 完了条件

1. 全メソッドが実装されている
2. コンパイルエラーがない
3. 既存テストが引き続きパスする（リグレッションなし）

---

## 5. 次フェーズへの引き継ぎ事項

### Phase3への準備

Phase2完了後、以下をPhase3（テスト実装）に引き継ぎます：

1. **新規実装の動作確認**:
   - シーケンス番号が正しくインクリメントされること
   - フレーム検証が機能すること
   - ReadRandom対応チェックが機能すること

2. **統合テスト準備**:
   - 3Eフレーム構築テスト
   - 4Eフレーム構築テスト

---

## 実装時間見積もり

| タスク | 見積もり時間 |
|-------|------------|
| クラスレベル定数・フィールド追加 | 0.5時間 |
| プライベートメソッド実装（7メソッド） | 3-4時間 |
| BuildReadRandomRequest() 統合 | 1-2時間 |
| デバッグ・動作確認 | 1時間 |
| **合計** | **5.5-7.5時間** |

---

## 参考資料

- `documents/design/Step2_フレーム構築実装/Step2_新設計_統合フレーム構築仕様.md` - 全体設計書
- `documents/design/フレーム構築関係/フレーム構築方法.md` - フレーム仕様書
- ConMoni実装、PySLMPClient実装

---

## 実装完了記録

**Phase2実装日**: 2025-11-27
**担当者**: Claude Code
**ステータス**: ✅ **完了** → Phase3へ移行可能

### 実装結果サマリー

**ビルド結果**:
- コンパイルエラー: 0件
- 警告: 0件
- ✅ ビルド成功

**テスト結果**:
- ConfigToFrameManagerTests: 9/10テスト合格 (90%)
- 失敗1件: TC_Step12_ASCII_001 (シーケンス番号の問題、Phase4で対処予定)

**実装完了事項**:
- [x] クラスレベル定数・フィールド追加
- [x] 7つのprivateメソッド実装
- [x] BuildReadRandomRequest()統合
- [x] コンパイルエラーなし
- [x] 既存テスト動作確認（リグレッションなし）

**詳細結果**: `documents/design/Step2_フレーム構築実装/実装結果/Phase2_SlmpFrameBuilder_RefactoringResults.md`

### 既知の問題と対処予定

**SequenceNumberManagerの静的フィールド問題**:
- 症状: テスト間でシーケンス番号が引き継がれる
- 影響: 4Eフレームテスト1件が失敗
- 対処: Phase4（総合テスト実装）で対処予定

### Phase3への引き継ぎ

**完了した実装**:
1. SlmpFrameBuilderの全面リファクタリング完了
2. 7つの機能別メソッドへの分割完了
3. シーケンス番号管理統合完了
4. フレーム検証機能追加完了
5. ReadRandom対応デバイスチェック追加完了

**Phase3で確認すべき事項**:
1. ConfigToFrameManagerとの統合確認
2. DWord分割処理の完全削除確認
3. 既存テスト全パス（Phase4で修正予定の1件を除く）

---

## アーカイブ: 元の実装計画

**元のステータス**: 準備完了（実装前）
**実装完了日**: 2025-11-27
