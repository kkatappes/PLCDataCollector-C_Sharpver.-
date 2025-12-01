# Step2: フレーム構築実装 - 新設計統合仕様

## 設計方針

ConMoniの明確な構造を基本骨格とし、PySLMPClientの優れた機能を統合、andon既実装の型安全性を維持強化した設計。

### 設計の4本柱

1. **ConMoniの明確な構造**を基本骨格とする
2. **PySLMPClientの優れた機能**を追加実装
3. **andon既実装の型安全性**を維持強化
4. **DWord分割機能を完全廃止**してシンプル化

---

## 📊 各実装の特徴分析

### 1. andon現状実装の評価

**✅ 強み:**
- 型安全性（C#の厳格な型システム）
- 入力検証の徹底（null、空リスト、フレームタイプ）
- 明確なクラス分離（ConfigToFrameManager → SlmpFrameBuilder）
- 設定ファイルベースの柔軟性

**⚠️ 弱み:**
- シーケンス番号管理未実装（4Eで固定0x0000）
- DWord分割機能（複雑性増加、今回廃止対象）
- フレーム長の上限検証なし
- ReadRandom対応デバイスチェックなし

---

### 2. ConMoni実装の評価

**✅ 強み:**
- **フレーム構造が非常に明確**（各バイトの意味がコメント付き）
- データ長の動的計算が確実
- リトルエンディアン処理の自動化
- 実機稼働実績あり（信頼性高い）

**⚠️ 弱み:**
- サブヘッダ非標準（0x54使用、標準は0x50）
- シーケンス番号未実装
- 事前生成方式（柔軟性やや低い）
- フレーム検証なし

---

### 3. PySLMPClient実装の評価

**✅ 強み:**
- **シーケンス番号自動管理実装済み**（4Eフレーム対応）
- 3E/4E × Binary/ASCII 完全対応
- データ長計算が明快（`len(data) + 6`）
- **フレーム長上限検証**（8194バイト）
- struct.pack()による洗練されたバイナリ処理

**⚠️ 弱み:**
- 入力検証が弱い（assert文のみ）
- ReadRandom非対応デバイスのチェックなし
- エラーハンドリング簡易

---

## 🎯 新設計の全体構成

### クラス構成図

```
ConfigToFrameManager (既存、軽微な修正)
    ↓ 依存
SlmpFrameBuilder (大幅リファクタリング)
    ↓ 依存
SequenceNumberManager (新規作成)
```

---

## 📝 詳細設計

### 1. ConfigToFrameManager（既存、軽微な修正）

**ファイル:** `andon/Core/Managers/ConfigToFrameManager.cs`

**変更内容:**
- DWord分割処理を完全削除
- ToDeviceSpecification()呼び出しをシンプル化

**実装コード:**

```csharp
public class ConfigToFrameManager : IConfigToFrameManager
{
    public byte[] BuildReadRandomFrameFromConfig(TargetDeviceConfig config)
    {
        // 1. 入力検証（既存のまま）
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (config.Devices == null || config.Devices.Count == 0)
        {
            throw new ArgumentException("デバイスリストが空です", nameof(config));
        }

        if (config.FrameType != "3E" && config.FrameType != "4E")
        {
            throw new ArgumentException(
                $"未対応のフレームタイプ: {config.FrameType}",
                nameof(config));
        }

        // 2. DeviceEntry → DeviceSpecification変換
        // ★ DWord分割処理を完全削除（シンプル化）
        var deviceSpecifications = config.Devices
            .Select(d => d.ToDeviceSpecification())  // ★分割なし
            .ToList();

        // 3. SlmpFrameBuilder呼び出し
        return SlmpFrameBuilder.BuildReadRandomRequest(
            deviceSpecifications,
            config.FrameType,
            config.Timeout
        );
    }
}
```

**採用理由:**
- ConMoniの流れを踏襲（設定→検証→変換→フレーム構築）
- DWord分割廃止によりシンプル化
- 既存の型安全性を維持

---

### 2. SlmpFrameBuilder（大幅リファクタリング）

**ファイル:** `andon/Utilities/SlmpFrameBuilder.cs`

**変更内容:**
- メソッドを機能別に分割
- シーケンス番号管理機能追加
- フレーム検証機能追加
- ReadRandom対応デバイスチェック追加

#### 2-1. クラス全体構造

```csharp
public static class SlmpFrameBuilder
{
    // ★PySLMPClientから採用：シーケンス番号管理
    private static readonly SequenceNumberManager _sequenceManager = new();

    // SLMP最大フレーム長（PySLMPClientから採用）
    private const int MAX_FRAME_LENGTH = 8194;

    // ReadRandom非対応デバイス（PySLMPClientから採用・改善）
    private static readonly DeviceCode[] _unsupportedDevicesForReadRandom = new[]
    {
        DeviceCode.TS,  // タイマ接点
        DeviceCode.TC,  // タイマコイル
        DeviceCode.CS,  // カウンタ接点
        DeviceCode.CC   // カウンタコイル
    };

    // ========== メインメソッド ==========
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

    // 以下、各メソッド詳細...
}
```

#### 2-2. ヘッダ構築メソッド

```csharp
/// <summary>
/// サブヘッダを構築します。
/// ★PySLMPClientのシーケンス番号対応
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

#### 2-3. ネットワーク設定構築メソッド

```csharp
/// <summary>
/// ネットワーク設定部を構築します。
/// ★ConMoniの明確な構造を採用
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

#### 2-4. コマンド部構築メソッド

```csharp
/// <summary>
/// コマンド部を構築します。
/// ★PySLMPClientの一括処理スタイル
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

#### 2-5. デバイス指定部構築メソッド

```csharp
/// <summary>
/// デバイス指定部を構築します。
/// ★ConMoni方式（各デバイス4バイト）
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
        section.Add((byte)device.DeviceCode);
    }

    return section.ToArray();
}
```

**採用理由:**
- ConMoniの明確な4バイト構造
- リトルエンディアン処理を明示的に記述
- ビットシフトで各バイト抽出

#### 2-6. データ長更新メソッド

```csharp
/// <summary>
/// データ長フィールドを更新します。
/// ★PySLMPClientの明快な計算 + ConMoniの動的更新
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

#### 2-7. 入力検証メソッド

```csharp
/// <summary>
/// 入力パラメータを検証します。
/// ★andon既存 + PySLMPClient要素強化
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

    // 4. ReadRandom対応デバイスチェック（★PySLMPClientから採用・改善）
    foreach (var device in devices)
    {
        if (_unsupportedDevicesForReadRandom.Contains(device.DeviceCode))
        {
            throw new ArgumentException(
                $"ReadRandomコマンドは {device.DeviceCode} デバイスに対応していません。" +
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

#### 2-8. フレーム検証メソッド

```csharp
/// <summary>
/// 完成したフレームを検証します。
/// ★PySLMPClientから採用
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

---

### 3. SequenceNumberManager（新規作成）

**ファイル:** `andon/Core/Managers/SequenceNumberManager.cs`

**実装コード:**

```csharp
namespace Andon.Core.Managers;

/// <summary>
/// シーケンス番号管理クラス
/// ★PySLMPClientから採用：4Eフレーム用シーケンス番号自動管理
/// </summary>
public class SequenceNumberManager
{
    private ushort _sequenceNumber = 0;
    private readonly object _lock = new object();

    /// <summary>
    /// 次のシーケンス番号を取得します。
    /// </summary>
    /// <param name="frameType">フレームタイプ（"3E" or "4E"）</param>
    /// <returns>シーケンス番号（3Eの場合は常に0、4Eの場合は自動インクリメント）</returns>
    public ushort GetNext(string frameType)
    {
        // 3Eフレームでは常に0を返す
        if (frameType == "3E")
        {
            return 0;
        }

        // 4Eフレームでは自動インクリメント
        lock (_lock)
        {
            // ★PySLMPClient方式：0xFF超過時ロールオーバー
            // シーケンス番号は1バイト（0～255）の範囲で管理
            if (_sequenceNumber > 0xFF)
            {
                _sequenceNumber = 0;
            }

            ushort current = _sequenceNumber;
            _sequenceNumber++;
            return current;
        }
    }

    /// <summary>
    /// シーケンス番号をリセットします。
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _sequenceNumber = 0;
        }
    }

    /// <summary>
    /// 現在のシーケンス番号を取得します（テスト用）。
    /// </summary>
    /// <returns>現在のシーケンス番号</returns>
    public ushort GetCurrent()
    {
        lock (_lock)
        {
            return _sequenceNumber;
        }
    }
}
```

**採用理由:**
- PySLMPClientで実装済みの優れた機能
- 4Eフレームでの複数要求並行処理に必須
- スレッドセーフな実装（lockによる排他制御）
- 0xFF超過時の自動ロールオーバー
- テスト容易性（GetCurrent()メソッド）

---

## 🔄 処理フロー（新設計）

```
┌─────────────────────────────────────────────┐
│ ConfigToFrameManager                        │
│ BuildReadRandomFrameFromConfig()            │
├─────────────────────────────────────────────┤
│ 1. 入力検証（null、空、フレームタイプ）      │
│ 2. DeviceEntry → DeviceSpecification変換   │
│    ★DWord分割なし（シンプル化）             │
│ 3. SlmpFrameBuilder呼び出し                │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│ SlmpFrameBuilder.BuildReadRandomRequest()   │
├─────────────────────────────────────────────┤
│ 【1. 入力検証強化】                          │
│   ├─ デバイスリスト存在・点数上限           │
│   ├─ フレームタイプ検証                     │
│   └─ ★ReadRandom対応デバイスチェック       │
│      （PySLMPClientから採用）               │
│                                             │
│ 【2. ヘッダ構築】                            │
│   ├─ ★シーケンス番号取得                   │
│   │  （PySLMPClientから採用）               │
│   ├─ 3E: 0x50 0x00                         │
│   └─ 4E: 0x54 0x00 + seq(2) + 予約(2)     │
│                                             │
│ 【3. ネットワーク設定構築】                  │
│   （ConMoni明確な構造）                     │
│   ├─ ネットワーク番号(1)                    │
│   ├─ 局番(1)                                │
│   ├─ I/O番号(2)                             │
│   ├─ マルチドロップ(1)                      │
│   └─ データ長プレースホルダ(2)              │
│                                             │
│ 【4. コマンド部構築】                        │
│   （PySLMPClient一括処理スタイル）          │
│   ├─ 監視タイマ(2)                          │
│   ├─ コマンド(2): 0x0403                   │
│   ├─ サブコマンド(2): 0x0000                │
│   └─ 点数(2): ワード点数 + 0x00            │
│                                             │
│ 【5. デバイス指定部構築】                    │
│   （ConMoni方式）                           │
│   └─ 各デバイス4バイト                      │
│      （アドレス3バイトLE + コード1バイト）   │
│                                             │
│ 【6. データ長更新】                          │
│   （PySLMPClient計算 + ConMoni動的更新）    │
│   └─ 監視タイマ以降のバイト数を計算・設定   │
│                                             │
│ 【7. フレーム検証】                          │
│   ★PySLMPClientから採用                    │
│   └─ 総バイト数 < 8194チェック             │
└─────────────────────────────────────────────┘
```

---

## 📊 採用機能と理由まとめ

### 1. シーケンス番号自動管理（PySLMPClientから）

**採用理由:**
- 4Eフレームでの複数要求並行処理に必須
- ConMoniでは未実装だった機能
- スレッドセーフ実装で信頼性高い
- 0xFF超過時の自動ロールオーバー

**実装:**
- SequenceNumberManagerクラス新規作成
- lockによるスレッドセーフ制御
- 3Eでは常に0、4Eでは自動インクリメント

---

### 2. フレーム長上限検証（PySLMPClientから）

**採用理由:**
- SLMP最大長8194バイト制約を送信前にチェック
- 不正なフレーム送信を防止
- PySLMPClientで実装済みの優れた機能

**実装:**
- ValidateFrame()メソッド
- 送信直前に実行
- 例外スローでエラー通知

---

### 3. ReadRandom対応デバイスチェック（PySLMPClient課題を改善）

**採用理由:**
- TS/TC/CS/CCなど非対応デバイスの送信を防止
- PySLMPClientでは未実装だった検証
- 実行時エラーを事前検出

**実装:**
- ValidateInputs()内で実施
- DeviceCode列挙型で型安全にチェック
- 詳細なエラーメッセージ

---

### 4. フレーム構造の明確化（ConMoniから）

**採用理由:**
- 各バイトの意味が明確
- デバッグ・保守が容易
- 実機稼働実績あり（信頼性）

**実装:**
- 各セクションを個別メソッド化
  - BuildSubHeader()
  - BuildNetworkConfig()
  - BuildCommandSection()
  - BuildDeviceSpecificationSection()

---

### 5. データ長計算の明確化（PySLMPClient考え方 + ConMoni実装）

**採用理由:**
- PySLMPClientの `len(data) + 6` 計算式が明快
- ConMoniの動的更新方式が確実
- 両者の良いとこ取り

**実装:**
- UpdateDataLength()メソッド
- フレーム完成後に動的計算
- 3E/4Eで計算式調整

---

### 6. コマンド部の一括構築（PySLMPClientから）

**採用理由:**
- struct.pack()的な一括処理で効率的
- C#のBitConverter活用
- 可読性向上

**実装:**
- BuildCommandSection()メソッド
- 監視タイマ～点数指定を一括生成

---

### 7. 既存の型安全性維持（andon既実装）

**採用理由:**
- C#の型システムで実行時エラー防止
- DeviceCode列挙型の活用
- ushort/uint型の厳格な使用

**実装:**
- DeviceSpecificationクラスそのまま活用
- 列挙型による型安全なデバイスコード管理

---

### 8. DWord分割機能の完全廃止

**採用理由:**
- ユーザー要求
- 実装複雑性の大幅削減
- 保守性向上
- テスト容易性向上

**実装:**
- ProcessedDeviceRequestInfo削除
- DWord分割ロジック全削除
- Type=0（ワード）のみ対応

---

## 📋 新旧比較表

| 項目 | andon現状 | ConMoni | PySLMPClient | **新設計** |
|------|-----------|---------|--------------|-----------|
| **シーケンス番号管理** | ❌ 固定0x0000 | ❌ 固定0x0000 | ✅ 自動管理 | ✅ **自動管理** |
| **フレーム長検証** | ❌ なし | ❌ なし | ✅ 8194バイト | ✅ **8194バイト** |
| **ReadRandom対応チェック** | ❌ なし | ❌ なし | ❌ なし | ✅ **あり** |
| **フレーム構造明確性** | 〇 普通 | ✅ 非常に明確 | 〇 普通 | ✅ **非常に明確** |
| **データ長計算** | ✅ 動的 | ✅ 動的 | ✅ 動的 | ✅ **動的（明快化）** |
| **DWord対応** | ⚠️ 分割機能あり | ❌ なし | ✅ あり | ❌ **廃止** |
| **3E/4E対応** | ✅ 両対応 | 4E相当のみ | ✅ 両対応 | ✅ **両対応（標準準拠）** |
| **入力検証** | ✅ 厳格 | ❌ 最小限 | △ assert文 | ✅ **厳格強化** |
| **型安全性** | ✅ 高 | - | - | ✅ **高** |

---

## 📦 影響を受けるファイル

### 修正が必要なファイル:

1. **andon/Core/Managers/ConfigToFrameManager.cs**
   - DWord分割処理削除
   - ToDeviceSpecification()呼び出しをシンプル化

2. **andon/Utilities/SlmpFrameBuilder.cs**
   - 全面リファクタリング
   - メソッド分割（Build*Section系追加）
   - シーケンス番号管理追加
   - フレーム検証追加
   - ReadRandom対応チェック追加

3. **andon/Core/Models/ProcessedDeviceRequestInfo.cs**
   - **削除**（DWord分割廃止に伴い不要）

### 新規作成が必要なファイル:

4. **andon/Core/Managers/SequenceNumberManager.cs**
   - シーケンス番号管理クラス（新規）

### テストファイル:

5. **Tests/Unit/Utilities/SlmpFrameBuilderTests.cs**
   - 全面書き直し
   - シーケンス番号テスト追加
   - フレーム検証テスト追加
   - ReadRandom対応チェックテスト追加

6. **Tests/Unit/Core/Managers/SequenceNumberManagerTests.cs**
   - 新規作成

7. **Tests/Unit/Core/Managers/ConfigToFrameManagerTests.cs**
   - DWord関連テスト削除
   - シンプル化

---

## 🧪 テスト方針

### 1. SequenceNumberManagerTests

**テストケース:**
- 初期値が0であること
- 3Eフレームでは常に0を返すこと
- 4Eフレームで呼び出すたびにインクリメントされること
- 0xFF超過時に0にリセットされること（ロールオーバー）
- スレッドセーフであること（並行呼び出しテスト）
- Reset()で0に戻ること

### 2. SlmpFrameBuilderTests

**テストケース:**

#### 入力検証系:
- デバイスリストがnullの場合、ArgumentExceptionをスローすること
- デバイスリストが空の場合、ArgumentExceptionをスローすること
- デバイス点数が256点以上の場合、ArgumentExceptionをスローすること
- フレームタイプが"3E"/"4E"以外の場合、ArgumentExceptionをスローすること
- TS/TC/CS/CCデバイス指定時、ArgumentExceptionをスローすること

#### フレーム構築系:
- 3Eフレームが正しく構築されること（サブヘッダ0x50 0x00）
- 4Eフレームが正しく構築されること（サブヘッダ0x54 0x00 + シーケンス番号）
- データ長が正しく計算されること
- デバイス指定部が正しく構築されること（リトルエンディアン）
- 監視タイマが正しく設定されること
- コマンドが0x0403であること
- ワード点数が正しく設定されること
- Dword点数が0であること

#### フレーム検証系:
- フレーム長が8194バイトを超える場合、InvalidOperationExceptionをスローすること
- フレーム長が0の場合、InvalidOperationExceptionをスローすること

#### シーケンス番号系:
- 4Eフレームで連続呼び出し時、シーケンス番号がインクリメントされること
- 3Eフレームでシーケンス番号が常に0であること

### 3. ConfigToFrameManagerTests

**テストケース:**
- DWord関連テスト削除
- 基本的な入力検証テスト維持
- ToDeviceSpecification()が正しく呼ばれること

---

## 📊 実装フレーム例

### 3Eフレーム（D100, D200, M10 読み出し）

```
【ヘッダ部】
50 00           # サブヘッダ（3E Binary）

【ネットワーク設定】
00              # ネットワーク番号
FF              # 局番
FF 03           # I/O番号（LE: 0x03FF）
00              # マルチドロップ局番

【データ長】
14 00           # データ長（20バイト、LE）

【コマンド部】
20 00           # 監視タイマ（32 = 8秒、LE）
03 04           # コマンド（0x0403 ReadRandom、LE）
00 00           # サブコマンド（0x0000、LE）
03              # ワード点数（3点）
00              # Dword点数（0点）

【デバイス指定部】
64 00 00 A8     # D100（0x000064、コード0xA8）
C8 00 00 A8     # D200（0x0000C8、コード0xA8）
0A 00 00 90     # M10（0x00000A、コード0x90）

合計: 32バイト
```

### 4Eフレーム（同上、シーケンス番号1）

```
【ヘッダ部】
54 00           # サブヘッダ（4E Binary）
01 00           # シーケンス番号（1、LE）
00 00           # 予約

【ネットワーク設定】
00              # ネットワーク番号
FF              # 局番
FF 03           # I/O番号（LE: 0x03FF）
00              # マルチドロップ局番

【データ長】
14 00           # データ長（20バイト、LE）

【コマンド部】
20 00           # 監視タイマ（32 = 8秒、LE）
03 04           # コマンド（0x0403 ReadRandom、LE）
00 00           # サブコマンド（0x0000、LE）
03              # ワード点数（3点）
00              # Dword点数（0点）

【デバイス指定部】
64 00 00 A8     # D100
C8 00 00 A8     # D200
0A 00 00 90     # M10

合計: 36バイト
```

---

## ✅ 実装チェックリスト

### Phase 1: 準備
- [ ] ProcessedDeviceRequestInfo.cs削除
- [ ] SequenceNumberManager.cs新規作成
- [ ] SequenceNumberManagerTests.cs新規作成

### Phase 2: SlmpFrameBuilder実装
- [ ] BuildSubHeader()実装
- [ ] BuildNetworkConfig()実装
- [ ] BuildCommandSection()実装
- [ ] BuildDeviceSpecificationSection()実装
- [ ] UpdateDataLength()実装
- [ ] ValidateInputs()実装（ReadRandomチェック追加）
- [ ] ValidateFrame()実装
- [ ] BuildReadRandomRequest()統合

### Phase 3: ConfigToFrameManager修正
- [ ] DWord分割処理削除
- [ ] ToDeviceSpecification()シンプル化

### Phase 4: テスト実装
- [ ] SequenceNumberManagerTests実装
- [ ] SlmpFrameBuilderTests全面書き直し
- [ ] ConfigToFrameManagerTests修正

### Phase 5: 統合テスト
- [ ] 3Eフレーム構築テスト
- [ ] 4Eフレーム構築テスト
- [ ] シーケンス番号動作確認
- [ ] フレーム検証動作確認
- [ ] ReadRandom対応チェック動作確認

---

## 🎓 まとめ

この新設計により、以下を達成:

1. **ConMoniの明確な構造**を基本骨格として採用
2. **PySLMPClientの優れた機能**（シーケンス番号管理、フレーム検証）を統合
3. **andon既実装の型安全性**を維持強化
4. **DWord分割機能を完全廃止**してシンプル化

結果として、**保守性**、**信頼性**、**拡張性**すべてが向上した設計となっています。

---

## 📚 参考資料

- `documents/design/フレーム構築関係/フレーム構築方法.md` - フレーム仕様書（正）
- `documents/design/Step2_フレーム構築実装/andon_Step2現状実装フロー.md` - andon現状実装
- `documents/design/Step2_フレーム構築実装/ConMoni_Step2処理フロー.md` - ConMoni実装
- `documents/design/Step2_フレーム構築実装/PySLMPClient_Step2処理フロー.md` - PySLMPClient実装
- SLMP仕様書 - 公式プロトコル仕様

---

## 🔍 現状確認と推奨対応（2025-11-26更新）

### 実装状況確認結果

設計書と実装コードの整合性を確認した結果、以下の対応が必要です。

#### ✅ 1. DeviceEntry → DeviceSpecification 変換（実装済み）

**状況**: Phase6で正しく実装済み

**実装箇所**:
- `andon/Core/Models/ConfigModels/DeviceEntry.cs`: `ToDeviceSpecification()`メソッド実装
- `andon/Core/Managers/ConfigToFrameManager.cs`: 44行目・92行目で変換処理使用

**動作フロー**:
```csharp
// 設定ファイルから読み込み
appsettings.json (Devices配列)
    ↓
DeviceEntry (設定読み込み用中間型)
    ↓ .ToDeviceSpecification()
DeviceSpecification (フレーム構築用)
    ↓
SlmpFrameBuilder.BuildReadRandomRequest()
```

**対応**: 不要（設計通り実装済み）

---

#### ❌ 2. SequenceNumberManager（未実装）

**状況**: 未実装（TODO状態）

**問題箇所**:
- `andon/Utilities/SlmpFrameBuilder.cs`: 60行目
  ```csharp
  frame.AddRange(new byte[] { 0x00, 0x00 });  // シーケンス番号（TODO: 管理機能実装）
  ```
- ファイル `andon/Core/Managers/SequenceNumberManager.cs` が存在しない

**影響**:
- 4Eフレームでシーケンス番号が固定`0x00 0x00`
- 複数要求を並行処理する際、要求と応答の対応付けができない
- 現状は単一要求のみの動作

**推奨対応**: **最優先で実装が必要**

**実装内容**:
```csharp
namespace Andon.Core.Managers;

/// <summary>
/// シーケンス番号管理クラス
/// PySLMPClientから採用：4Eフレーム用シーケンス番号自動管理
/// </summary>
public class SequenceNumberManager
{
    private ushort _sequenceNumber = 0;
    private readonly object _lock = new object();

    /// <summary>
    /// 次のシーケンス番号を取得
    /// </summary>
    /// <param name="frameType">フレームタイプ（"3E" or "4E"）</param>
    /// <returns>シーケンス番号（3Eは常に0、4Eは自動インクリメント）</returns>
    public ushort GetNext(string frameType)
    {
        if (frameType == "3E") return 0;

        lock (_lock)
        {
            if (_sequenceNumber > 0xFF) _sequenceNumber = 0;
            return _sequenceNumber++;
        }
    }

    public void Reset()
    {
        lock (_lock) { _sequenceNumber = 0; }
    }

    public ushort GetCurrent()
    {
        lock (_lock) { return _sequenceNumber; }
    }
}
```

**SlmpFrameBuilder修正箇所**:
```csharp
public static class SlmpFrameBuilder
{
    private static readonly SequenceNumberManager _sequenceManager = new();

    public static byte[] BuildReadRandomRequest(...)
    {
        // ...
        if (frameType == "4E")
        {
            frame.AddRange(new byte[] { 0x54, 0x00 });
            ushort seqNum = _sequenceManager.GetNext(frameType);
            frame.AddRange(BitConverter.GetBytes(seqNum));  // シーケンス番号（自動管理）
            frame.AddRange(new byte[] { 0x00, 0x00 });
        }
        // ...
    }
}
```

---

#### ⚠️ 3. DWord分割機能の残存（部分的廃止必要）

**状況**: テスト用途のみに限定されているが、本番コードに残存

**問題箇所**:
- `andon/Core/Models/ProcessedDeviceRequestInfo.cs`:
  - コメント: "TC029テスト実装で使用、TC037での構造化処理にも利用"
  - 44行目: `DWordCombineTargets`プロパティが存在

- `andon/Core/Managers/PlcCommunicationManager.cs`:
  - 881行目: `ProcessReceivedRawData()`の引数
  - 1134行目: `CombineDwordData()`の引数
  - 2089, 2116, 2154, 2223, 2345, 2751行目: 各メソッドで使用

**設計方針との相違**:
- 設計書: "DWord分割機能を完全廃止"
- 実装: PlcCommunicationManagerで依然として使用

**推奨対応**: **段階的な削除**

**Phase 1: 影響範囲調査**
```bash
# ProcessedDeviceRequestInfoの使用箇所を確認
grep -r "ProcessedDeviceRequestInfo" andon/Core/ andon/Tests/
```

**Phase 2: 代替案の検討**
- ReadRandom方式では`List<DeviceSpecification>`のみでデバイス指定完結
- `ProcessedDeviceRequestInfo`の必要性を再評価
- テストコードでのみ使用する場合、Testフォルダに移動

**Phase 3: リファクタリング**
```csharp
// Before
public async Task<BasicProcessedResponseData> ProcessReceivedRawData(
    byte[] rawData,
    ProcessedDeviceRequestInfo processedRequestInfo,  // 削除候補
    CancellationToken cancellationToken = default)

// After
public async Task<BasicProcessedResponseData> ProcessReceivedRawData(
    byte[] rawData,
    List<DeviceSpecification> devices,  // シンプル化
    FrameType frameType,
    CancellationToken cancellationToken = default)
```

---

### 優先順位と実装計画

| 優先度 | 項目 | 状況 | 工数目安 | 影響範囲 |
|-------|------|------|---------|---------|
| **最優先** | SequenceNumberManager実装 | ❌ 未実装 | 2-3時間 | SlmpFrameBuilder、テスト |
| 高 | ProcessedDeviceRequestInfo削減 | ⚠️ 部分残存 | 4-6時間 | PlcCommunicationManager、テスト全体 |
| 低 | DeviceEntry変換 | ✅ 完了 | 0時間 | なし |

### 実装手順（推奨）

#### Step 1: SequenceNumberManager実装（最優先）
1. `andon/Core/Managers/SequenceNumberManager.cs`新規作成
2. `SlmpFrameBuilder.cs`修正（60行目のTODO解消）
3. 単体テスト作成（`Tests/Unit/Core/Managers/SequenceNumberManagerTests.cs`）
4. 統合テスト確認（4Eフレームでシーケンス番号検証）

#### Step 2: ProcessedDeviceRequestInfo削減（段階的）
1. 影響範囲調査（grep実行）
2. PlcCommunicationManagerのメソッドシグネチャ見直し
3. テストコードへの移動検討
4. 段階的リファクタリング実施

---

### 設計書との整合性評価

| 項目 | 設計書 | 実装状況 | 整合性 |
|-----|-------|---------|-------|
| シーケンス番号自動管理 | ✅ 必須 | ❌ 未実装 | ❌ 不整合 |
| DWord分割機能廃止 | ✅ 完全廃止 | ⚠️ 部分残存 | ⚠️ 部分不整合 |
| DeviceEntry変換 | ✅ Phase6実装 | ✅ 実装済み | ✅ 整合 |
| ReadRandom対応 | ✅ 0x0403 | ✅ 実装済み | ✅ 整合 |
| フレーム検証 | ✅ 8194バイト | ✅ 実装済み | ✅ 整合 |

**総合評価**: 概ね設計に準拠しているが、**SequenceNumberManager未実装が最大の課題**

---

### 次のアクション

1. **即座に対応**: SequenceNumberManager実装
2. **計画的に対応**: ProcessedDeviceRequestInfo削減
3. **確認完了**: DeviceEntry変換機能の動作確認

**更新日**: 2025-11-27
**確認者**: 設計書整合性チェック実施 + 実装対応情報追加

---

## 📦 実装コード対応マッピング（2025-11-27追加）

### 実装ファイルパスと現状

| 設計書の項目 | 実装ファイルパス | 実装状況 |
|------------|----------------|---------|
| **ConfigToFrameManager** | `andon/Core/Managers/ConfigToFrameManager.cs` | ✅ 実装済み（19-102行目） |
| **SlmpFrameBuilder** | `andon/Utilities/SlmpFrameBuilder.cs` | ⚠️ 部分実装（18-160行目） |
| **SequenceNumberManager** | `andon/Core/Managers/SequenceNumberManager.cs` | ❌ 未実装（ファイル未作成） |
| **DeviceEntry** | `andon/Core/Models/ConfigModels/DeviceEntry.cs` | ✅ 実装済み（8-47行目） |
| **DeviceSpecification** | `andon/Core/Models/DeviceSpecification.cs` | ✅ 実装済み（8-194行目） |
| **DeviceCode** | `andon/Core/Constants/DeviceConstants.cs` | ✅ 実装済み（6-33行目） |
| **TargetDeviceConfig** | `andon/Core/Models/ConfigModels/TargetDeviceConfig.cs` | ✅ 実装済み（6-23行目） |
| **ProcessedDeviceRequestInfo** | `andon/Core/Models/ProcessedDeviceRequestInfo.cs` | ⚠️ 削減対象（1-46行目）|

### クラス・メソッド実装対応表

#### 1. ConfigToFrameManager

**ファイル:** `andon/Core/Managers/ConfigToFrameManager.cs`

| メソッド | 行番号 | 実装状況 | 備考 |
|---------|-------|---------|------|
| BuildReadRandomFrameFromConfig() | 19-53 | ✅ 実装済み | DWord分割処理は既に削除済み |
| BuildReadRandomFrameFromConfigAscii() | 67-101 | ✅ 実装済み | ASCII形式対応 |

**関連モデル:**
- **TargetDeviceConfig**: Properties: `Devices` (List<DeviceEntry>), `FrameType` (string), `Timeout` (ushort)
- **DeviceEntry.ToDeviceSpecification()**: 35-46行目（Phase6実装済み）

**現在の処理フロー:**
```csharp
// 44-46行目: DWord分割なしのシンプルな変換
var deviceSpecifications = config.Devices
    .Select(d => d.ToDeviceSpecification())
    .ToList();
```

#### 2. SlmpFrameBuilder

**ファイル:** `andon/Utilities/SlmpFrameBuilder.cs`

| メソッド/機能 | 行番号 | 実装状況 | 必要な対応 |
|-------------|-------|---------|-----------|
| BuildReadRandomRequest() | 18-131 | ⚠️ 部分実装 | シーケンス番号管理、フレーム検証、ReadRandomチェック追加 |
| BuildReadRandomRequestAscii() | 148-159 | ✅ 実装済み | - |
| 入力検証 | 27-44 | ✅ 実装済み | ReadRandom対応デバイスチェック追加が必要 |
| ヘッダ構築 | 51-62 | ⚠️ 部分実装 | 60行目: シーケンス番号固定値使用（TODO） |
| ネットワーク設定 | 64-68 | ✅ 実装済み | - |
| データ長プレースホルダ | 71-72 | ✅ 実装済み | - |
| コマンド部構築 | 75-80 | ✅ 実装済み | - |
| デバイス指定部構築 | 83-103 | ✅ 実装済み | - |
| データ長更新 | 106-127 | ✅ 実装済み | - |
| フレーム検証 | - | ❌ 未実装 | MAX_FRAME_LENGTH(8194)チェック追加が必要 |

**重要な実装箇所:**

**60行目（シーケンス番号部分）:**
```csharp
frame.AddRange(new byte[] { 0x00, 0x00 });  // シーケンス番号（TODO: 管理機能実装）
```
→ **対応必要:** `_sequenceManager.GetNext(frameType)` 呼び出しに変更

**必要な追加実装:**
```csharp
// クラスレベルに追加
private static readonly SequenceNumberManager _sequenceManager = new();
private const int MAX_FRAME_LENGTH = 8194;
private static readonly DeviceCode[] _unsupportedDevicesForReadRandom = new[]
{
    DeviceCode.TS, DeviceCode.TC, DeviceCode.CS, DeviceCode.CC
};
```

#### 3. DeviceSpecification（既存、活用可能）

**ファイル:** `andon/Core/Models/DeviceSpecification.cs`

| メソッド | 行番号 | 実装状況 | 活用方法 |
|---------|-------|---------|---------|
| ValidateForReadRandom() | 169-176 | ✅ 実装済み | SlmpFrameBuilderのReadRandomチェックで活用可能 |
| ToDeviceSpecificationBytes() | 115-127 | ✅ 実装済み | フレーム構築で使用中 |
| ValidateDeviceNumberRange() | 182-193 | ✅ 実装済み | 入力検証で使用可能 |

**Properties:**
- `DeviceType` (string): デバイスタイプ（"D", "M", "X", "Y"等）
- `DeviceNumber` (int): デバイス番号
- `Code` (DeviceCode): デバイスコード列挙型
- `IsHexAddress` (bool): 16進数アドレスフラグ

#### 4. DeviceCode列挙型（既存）

**ファイル:** `andon/Core/Constants/DeviceConstants.cs`

```csharp
public enum DeviceCode : byte
{
    // ビットデバイス（16点=1ワード）
    SM = 0x91, X = 0x9C, Y = 0x9D, M = 0x90, L = 0x92, F = 0x93, B = 0xA0,

    // ワードデバイス
    SD = 0xA9, D = 0xA8, W = 0xB4, R = 0xAF, ZR = 0xB0,

    // タイマー（ReadRandom制約あり）
    TN = 0xC2,
    TS = 0xC1,  // ReadRandom非対応
    TC = 0xC0,  // ReadRandom非対応

    // カウンタ
    CN = 0xC5,
    CS = 0xC4,  // ReadRandom非対応
    CC = 0xC3   // ReadRandom非対応
}
```

### ProcessedDeviceRequestInfoの使用状況（削減対象）

**ファイル:** `andon/Core/Models/ProcessedDeviceRequestInfo.cs` (1-46行目)

**主要な使用箇所:**

| 使用箇所 | ファイルパス | メソッド/箇所 |
|---------|------------|-------------|
| インターフェース定義 | `andon/Core/Interfaces/IPlcCommunicationManager.cs` | ProcessReceivedRawData (49行目), CombineDwordData (62行目), ParseRawToStructuredData (75行目) |
| 実装 | `andon/Core/Managers/PlcCommunicationManager.cs` | ProcessReceivedRawData (880行目), CombineDwordData (1133行目), ParseRawToStructuredData (2222行目), ExecuteFullCycleAsync (2750行目) |
| privateメソッド | `andon/Core/Managers/PlcCommunicationManager.cs` | ExtractDeviceValues (2088行目), ExtractWordDevices (2115行目), ExtractBitDevices (2153行目), CreateFrameInfo (2344行目) |
| 統合テスト | `andon/Tests/Integration/Step3_6_IntegrationTests.cs` | TC119 (326, 422行目), TC121 (555行目), TC123 (1020, 1110, 1204, 1304行目) |
| 単体テスト | `andon/Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs` | TC029 (596行目), TC032 (977行目), TC037 (704行目), TC038 (841行目), TC118 (1128行目), その他多数 |

**DWordCombineTargetsプロパティ（44行目）:**
```csharp
public List<DWordCombineInfo> DWordCombineTargets { get; set; } = new();
```
→ DWord分割機能に関連（削減対象だが影響範囲が広い）

### テストファイル対応

| テスト対象 | テストファイルパス | 実装状況 |
|-----------|------------------|---------|
| ConfigToFrameManager | `Tests/Unit/Core/Managers/ConfigToFrameManagerTests.cs` | ✅ 実装済み |
| SlmpFrameBuilder | `Tests/Unit/Utilities/SlmpFrameBuilderTests.cs` | ⚠️ 追加テストが必要 |
| SequenceNumberManager | `Tests/Unit/Core/Managers/SequenceNumberManagerTests.cs` | ❌ 未作成 |

**追加が必要なテストケース:**

**SequenceNumberManagerTests（新規）:**
- 初期値が0であること
- 3Eフレームでは常に0を返すこと
- 4Eフレームで呼び出すたびにインクリメントされること
- 0xFF超過時にリセットされること（ロールオーバー）
- スレッドセーフであること（並行呼び出しテスト）
- Reset()で0に戻ること

**SlmpFrameBuilderTests（追加）:**
- 4Eフレームでシーケンス番号がインクリメントされること
- フレーム長が8194バイトを超える場合にInvalidOperationExceptionをスローすること
- TS/TC/CS/CCデバイス指定時にArgumentExceptionをスローすること

### 実装優先度マトリクス

| 優先度 | 項目 | ファイルパス | 実装箇所 | 工数目安 | 影響範囲 |
|-------|------|------------|---------|---------|---------|
| 🔴 **最優先** | SequenceNumberManager新規作成 | `andon/Core/Managers/SequenceNumberManager.cs` | 新規ファイル | 2-3時間 | SlmpFrameBuilder |
| 🔴 **最優先** | シーケンス番号管理統合 | `andon/Utilities/SlmpFrameBuilder.cs` | 60行目修正 | 1時間 | 4Eフレーム生成 |
| 🔴 **最優先** | フレーム検証機能追加 | `andon/Utilities/SlmpFrameBuilder.cs` | ValidateFrame()追加 | 1時間 | 全フレーム生成 |
| 🟡 **高優先** | ReadRandom対応チェック | `andon/Utilities/SlmpFrameBuilder.cs` | ValidateInputs()拡張 | 2時間 | 入力検証 |
| 🟡 **高優先** | メソッド分割リファクタリング | `andon/Utilities/SlmpFrameBuilder.cs` | 全体構造 | 4-6時間 | 可読性・保守性 |
| 🟢 **低優先** | ProcessedDeviceRequestInfo削減 | 複数ファイル | PlcCommunicationManager等 | 8-12時間 | 広範囲 |

### 設計書と実装のギャップサマリー

| 項目 | 設計書 | 実装状況 | ギャップ | 対応アクション |
|------|-------|---------|---------|--------------|
| DeviceEntry変換 | Phase6実装 | ✅ 完了 | なし | 変更不要 |
| DWord分割廃止 | 完全削除 | ⚠️ ConfigToFrameManagerでは達成、PlcCommunicationManagerで残存 | ProcessedDeviceRequestInfo削減が必要 | 段階的削減計画 |
| シーケンス番号管理 | 4Eフレーム自動管理 | ❌ 未実装（固定0x0000） | SequenceNumberManager未作成 | 新規実装必須 |
| フレーム検証 | 8194バイト上限 | ❌ 未実装 | ValidateFrame()なし | 追加実装必須 |
| ReadRandomチェック | 非対応デバイス検証 | ❌ 未実装 | 検証ロジックなし | 追加実装推奨（DeviceSpecification.ValidateForReadRandom活用可能） |
| メソッド分割 | 機能別メソッド化 | ❌ 未実施（inline実装） | Build*Section系メソッドなし | リファクタリング推奨 |

### 活用可能な既存実装

以下の実装は既に完了しており、設計書の意図通りに活用可能です:

1. **DeviceEntry.ToDeviceSpecification()** (35-46行目)
   - 用途: 設定ファイルからフレーム構築用モデルへの変換
   - 状態: Phase6で実装済み、正常動作中

2. **DeviceSpecification.ValidateForReadRandom()** (169-176行目)
   - 用途: ReadRandom対応デバイスチェック
   - 状態: 実装済みだが現在未使用、SlmpFrameBuilderで活用可能

3. **DeviceCode列挙型** (6-33行目)
   - 用途: 型安全なデバイスコード管理
   - 状態: 完全実装済み、ReadRandom非対応デバイス識別に使用可能

4. **DeviceSpecification.ToDeviceSpecificationBytes()** (115-127行目)
   - 用途: デバイス指定部のバイト配列生成
   - 状態: 実装済み、フレーム構築で使用中

### 次のステップ（具体的なアクション）

#### Step 1: SequenceNumberManager実装（最優先）
```
1. ファイル作成: andon/Core/Managers/SequenceNumberManager.cs
2. 実装内容:
   - private ushort _sequenceNumber = 0
   - private readonly object _lock = new object()
   - public ushort GetNext(string frameType)
   - public void Reset()
   - public ushort GetCurrent()
3. テスト作成: Tests/Unit/Core/Managers/SequenceNumberManagerTests.cs
```

#### Step 2: SlmpFrameBuilder修正（60行目）
```csharp
// Before (現在の実装)
frame.AddRange(new byte[] { 0x00, 0x00 });  // シーケンス番号（TODO: 管理機能実装）

// After (修正後)
ushort seqNum = _sequenceManager.GetNext(frameType);
frame.AddRange(BitConverter.GetBytes(seqNum));
```

#### Step 3: フレーム検証機能追加
```csharp
// SlmpFrameBuilder.csに追加
private const int MAX_FRAME_LENGTH = 8194;

private static void ValidateFrame(byte[] frame)
{
    if (frame.Length > MAX_FRAME_LENGTH)
    {
        throw new InvalidOperationException(
            $"フレーム長が上限を超えています: {frame.Length}バイト（最大{MAX_FRAME_LENGTH}バイト）");
    }
}

// BuildReadRandomRequest()の最後に追加
ValidateFrame(frame.ToArray());
```

#### Step 4: ReadRandom対応デバイスチェック（推奨）
```csharp
// SlmpFrameBuilder.csに追加
private static readonly DeviceCode[] _unsupportedDevicesForReadRandom = new[]
{
    DeviceCode.TS, DeviceCode.TC, DeviceCode.CS, DeviceCode.CC
};

// ValidateInputs()に追加（既存の検証後）
foreach (var device in devices)
{
    // Option 1: 既存メソッド活用
    device.ValidateForReadRandom();

    // Option 2: 直接チェック
    if (_unsupportedDevicesForReadRandom.Contains(device.Code))
    {
        throw new ArgumentException(
            $"ReadRandomコマンドは {device.Code} デバイスに対応していません。",
            nameof(devices));
    }
}
```

---

**実装対応情報追加日**: 2025-11-27
**対応者**: 実装コードマッピング作成
**目的**: 設計書と実装コードの対応関係を明確化し、実装タスクを具体化
