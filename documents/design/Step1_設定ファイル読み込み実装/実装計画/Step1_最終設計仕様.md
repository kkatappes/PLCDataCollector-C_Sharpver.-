# Step1: 設定ファイル読み込み - 最終設計仕様

## 概要

andonプロジェクトにおけるStep1（設定ファイル読み込み）の最終設計仕様。
- **ConMoniの実証済みアルゴリズム採用**（ビット最適化、24種類デバイスコード対応）
- **PySLMPClientの柔軟性取り込み**
- **andonの型安全性・厳格な検証維持**
- **Excel完全駆動設計**（JSON設定完全廃止）
- **通信設定のハードコード化**（memo.md送信フレーム準拠）

## 文書更新履歴

| 日付 | バージョン | 変更内容 |
|------|-----------|---------|
| 2025-11-26 | 1.1 | プロジェクト構造設計準拠に修正：独立クラス名→既存クラス構造への統合 |
| 2025年 | 1.0 | 初版作成（設計確定版） |

---

## 設計方針

### 基本原則

1. **Excel設定のみを使用**
   - JSON設定ファイル（appsettings.json）は完全廃止
   - 実行ファイルと同じフォルダ内の全.xlsxファイルを自動読み込み

2. **通信設定の固定化**
   - IPアドレスとポート番号のみExcelから読み込み
   - その他の通信パラメータは全てハードコード（memo.md送信フレーム準拠）

3. **ConMoniアルゴリズムの完全採用**
   - 24種類デバイスコード対応
   - ビットデバイス16点単位ワード化
   - 10進/16進デバイスの自動判別

4. **出力JSON構造への対応**
   - 設定読み込み仕様.mdの出力JSON構造に準拠
   - Step7での変換を容易にする設計

---

## 処理フロー

```
┌─────────────────────────────────────────────────────┐
│ Step1: Excel設定ファイル読み込み                     │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│ 1. Excelファイル検索                                 │
│    ConfigurationLoader.DiscoverExcelFiles()          │
│    (privateヘルパーメソッド)                         │
├─────────────────────────────────────────────────────┤
│ 検索場所：                                           │
│   AppContext.BaseDirectory (実行ファイルのフォルダ)  │
│                                                      │
│ 検索パターン：                                       │
│   *.xlsx                                             │
│                                                      │
│ 除外対象：                                           │
│   - ~$*.xlsx (Excelの一時ファイル)                  │
│   - ロックされているファイル                         │
│                                                      │
│ 結果：                                               │
│   List<string> excelFilePaths                       │
│                                                      │
│ エラー処理：                                         │
│   - .xlsxファイルが1つも見つからない場合             │
│     → ArgumentException スロー                       │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│ 2. 各Excelファイルの読み込み                         │
│    ConfigurationLoader.LoadFromExcel()               │
├─────────────────────────────────────────────────────┤
│ 使用ライブラリ：EPPlus (LicenseContext.NonCommercial)│
│                                                      │
│ 各ファイルに対して実行：                             │
│   ① ファイルオープン                                 │
│   ② "settings"シート存在確認                         │
│   ③ "データ収集デバイス"シート存在確認               │
│   ④ 基本設定の読み込み（5項目）                      │
│   ⑤ デバイスリストの読み込み                         │
│   ⑥ PlcConfigurationオブジェクト生成                │
│                                                      │
│ エラー処理：                                         │
│   - シートが見つからない場合                         │
│     → ArgumentException（ファイル名とシート名を明示）│
│   - セルが空の場合                                   │
│     → ArgumentException（セル位置を明示）            │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│ 3. "settings"シート読み込み                          │
│    ConfigurationLoader.ReadSettingsSheet()           │
│    (privateヘルパーメソッド)                         │
├─────────────────────────────────────────────────────┤
│ 【必須項目（5項目のみ）】                            │
│                                                      │
│ セルB8：PLCのIPアドレス                              │
│   型：string                                         │
│   検証：IPAddress.TryParse()                        │
│   例："172.30.40.15"                                │
│                                                      │
│ セルB9：PLCのポート                                  │
│   型：int                                            │
│   範囲：1～65535                                     │
│   例：8192                                           │
│                                                      │
│ セルB11：データ取得周期(ms)                          │
│   型：int                                            │
│   範囲：1～86400000 (24時間)                        │
│   用途：Step7での出力周期制御                        │
│   例：1000                                           │
│                                                      │
│ セルB12：デバイス名（PLC識別名）                     │
│   型：string                                         │
│   検証：非空文字列                                   │
│   用途：出力JSONの"source.plcModel"                 │
│   例："ライン1-炉A"                                 │
│                                                      │
│ セルB13：データ保存先パス                            │
│   型：string                                         │
│   検証：パス形式チェック                             │
│   処理：存在しない場合は自動作成                     │
│   例："C:\\data\\output"                            │
│                                                      │
│ ※通信設定（フレームバージョン、プロトコル等）は      │
│   全てハードコード化されており、Excelからは読まない  │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│ 4. "データ収集デバイス"シート読み込み                │
│    ConfigurationLoader.ReadDevicesSheet()            │
│    (privateヘルパーメソッド)                         │
├─────────────────────────────────────────────────────┤
│ 【シート構造】                                       │
│   1行目：ヘッダ行（スキップ）                        │
│   2行目以降：データ行（空白行まで読み込み）          │
│                                                      │
│ 【列構成】                                           │
│   A列：項目名（string、必須）                        │
│     - 出力JSONの"items[].name"に対応                │
│     - 例："温度1", "圧力", "運転状態"               │
│                                                      │
│   B列：デバイスコード（string、必須）                │
│     - M, D, X, Y, W等（24種類対応）                 │
│     - 出力JSONの"items[].device.code"に対応         │
│     - 例："D", "M", "X"                             │
│                                                      │
│   C列：デバイス番号（int、必須）                     │
│     - 10進数で記載                                   │
│     - 16進デバイス（X,Y等）も10進数で記載            │
│     - 出力JSONの"items[].device.number"に対応       │
│     - 例：60000, 1760, 2304                         │
│                                                      │
│   D列：桁数（int、必須）                             │
│     - 出力JSONの"items[].digits"に対応              │
│     - 例：1, 2, 3                                   │
│                                                      │
│   E列：単位（string、必須）                          │
│     - 値："bit", "word", "dword"のいずれか          │
│     - 出力JSONの"items[].unit"に対応                │
│     - 例："word", "bit"                             │
│                                                      │
│ 【読み込み処理】                                     │
│   - A列が空白になるまで読み込み                      │
│   - 各行をDeviceSpecificationオブジェクトに変換     │
│   - 読み込み時に正規化処理を実行                     │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│ 5. デバイス情報の正規化・変換                        │
│    ConfigurationLoader.NormalizeDevice()             │
│    (privateヘルパーメソッド)                         │
├─────────────────────────────────────────────────────┤
│ ConMoniのアルゴリズムを完全移植                      │
│                                                      │
│ ① デバイスタイプ判定                                 │
│    DeviceCodeMap.IsHexDevice(deviceCode)            │
│    → 10進デバイス：M, D, W, L, V, TN, CN等          │
│    → 16進デバイス：X, Y, B, SB, DX, DY              │
│                                                      │
│ ② デバイスコード取得                                 │
│    DeviceCodeMap.GetDeviceCode(deviceCode)          │
│    → "M" → 0x90                                     │
│    → "D" → 0xA8                                     │
│    → "X" → 0x9C 等（24種類対応）                    │
│                                                      │
│ ③ デバイス番号のバイト変換                           │
│    【10進デバイス】                                  │
│      int → 3バイトリトルエンディアン                 │
│      例：D60000 → 60000 → [0x60, 0xEA, 0x00]       │
│                                                      │
│    【16進デバイス】                                  │
│      int → 16進文字列 → 6桁パディング → 3バイト     │
│      例：X1760 → 1760 → "0006E0" → [0xE0,0x06,0x00]│
│                                                      │
│ ④ ビット/ワード判定                                  │
│    DeviceCodeMap.IsBitDevice(deviceCode)            │
│    → ビットデバイス：M, X, Y, L, B等                │
│    → ワードデバイス：D, W, TN, CN, R等              │
│                                                      │
│ ⑤ DeviceSpecificationオブジェクト生成               │
│    new DeviceSpecification {                        │
│      ItemName = "温度1",                            │
│      DeviceType = "D",                              │
│      DeviceCode = 0xA8,                             │
│      DeviceNumber = 60000,                          │
│      IsHexDevice = false,                           │
│      IsBitDevice = false,                           │
│      DeviceBytes = [0x60, 0xEA, 0x00],             │
│      Digits = 1,                                    │
│      Unit = "word"                                  │
│    }                                                │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│ 6. ビットデバイス最適化処理（オプション）            │
│    ConfigurationLoader.OptimizeBitDevices()          │
│    (privateヘルパーメソッド)                         │
├─────────────────────────────────────────────────────┤
│ ConMoniの16点単位ワード化アルゴリズムを完全移植      │
│                                                      │
│ 【最適化アルゴリズム】                               │
│   入力：ビットデバイスのリスト                       │
│   出力：16点単位でまとめたチャンクリスト             │
│                                                      │
│   処理手順：                                         │
│   ① デバイスタイプでグループ化（M, X, Y等を分離）    │
│   ② 各グループ内でデバイス番号でソート               │
│   ③ 16点単位（0～15, 16～31...）でグループ化        │
│   ④ 各チャンクの先頭デバイスを代表として選択         │
│   ⑤ 未使用ビット位置も保持（null）                   │
│                                                      │
│ 【具体例】                                           │
│   入力：M32, M57074, M57118, M57182                 │
│                                                      │
│   チャンク1：                                        │
│     範囲：M32～M47（16点）                           │
│     代表デバイス：M32                                │
│     使用ビット：[0]=M32のみ、他は未使用              │
│     通信データ：[0x20, 0x00, 0x00, 0x90] + 点数1    │
│                                                      │
│   チャンク2：                                        │
│     範囲：M57074～M57089（16点）                     │
│     代表デバイス：M57074                             │
│     使用ビット：[10]=M57074のみ                     │
│     通信データ：[0xB2, 0xDE, 0x00, 0x90] + 点数1    │
│                                                      │
│   チャンク3：                                        │
│     範囲：M57118～M57133（16点）                     │
│     代表デバイス：M57118                             │
│     使用ビット：[14]=M57118のみ                     │
│     通信データ：[0xDE, 0xDF, 0x00, 0x90] + 点数1    │
│                                                      │
│   チャンク4：                                        │
│     範囲：M57182～M57197（16点）                     │
│     代表デバイス：M57182                             │
│     使用ビット：[14]=M57182のみ                     │
│     通信データ：[0x50, 0xE0, 0x00, 0x90] + 点数1    │
│                                                      │
│ 【効果】                                             │
│   通信回数：4回 → 4回（同じ）                       │
│   ※離れたビットの場合は最適化効果が小さい            │
│                                                      │
│   より効果的な例：M100～M115（連続16点）             │
│   通信回数：16回 → 1回（94%削減）                   │
│   フレームサイズ：64バイト → 4バイト（94%削減）     │
│                                                      │
│ 【重要な制約】                                       │
│   - 同一デバイスタイプ内でのみ最適化                 │
│   - ワードデバイスとは混在不可                       │
│   - ReadRandomコマンド使用（0x0403）                │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│ 7. PlcConfigurationオブジェクト生成                  │
├─────────────────────────────────────────────────────┤
│ public class PlcConfiguration                       │
│ {                                                    │
│     // Excel "settings"シートから読み込んだ設定      │
│     public string IpAddress { get; set; }           │
│     public int Port { get; set; }                   │
│     public int DataReadingFrequency { get; set; }   │
│     public string PlcModel { get; set; }            │
│     public string SavePath { get; set; }            │
│                                                      │
│     // メタ情報                                      │
│     public string SourceExcelFile { get; set; }     │
│     public string ConfigurationName { get; }        │
│                                                      │
│     // Excel "データ収集デバイス"から読み込み        │
│     public List<DeviceSpecification> Devices        │
│                                                      │
│     // ※通信設定は全てSlmpFixedSettingsから取得      │
│     // （FrameVersion, Protocol, NetworkNumber等）   │
│ }                                                    │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│ 8. 設定検証処理                                      │
│    ConfigurationLoader.ValidateConfiguration()       │
│    (privateヘルパーメソッド)                         │
├─────────────────────────────────────────────────────┤
│ 【検証項目】                                         │
│                                                      │
│ ① 接続情報検証                                       │
│    - IPアドレス形式チェック                          │
│    - ポート範囲チェック（1～65535）                  │
│                                                      │
│ ② デバイスリスト検証                                 │
│    - 最低1デバイス存在チェック                       │
│    - デバイスコード妥当性（24種類内）                │
│    - デバイス番号範囲（0～16777215、3バイト範囲）    │
│    - 単位の妥当性（"bit", "word", "dword"）         │
│    - 総点数制限（ReadRandom: 最大255点）             │
│                                                      │
│ ③ 出力設定検証                                       │
│    - 保存先パス形式チェック                          │
│    - データ取得周期範囲（1～86400000ms）             │
│                                                      │
│ 【検証エラー時の動作】                               │
│   - 詳細なエラーメッセージ出力                       │
│   - どのExcelファイルのどのセルでエラーかを明示      │
│   - ValidationException スロー                       │
│   - アプリケーション起動中止                         │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│ 9. 複数設定の統合                                    │
│    MultiPlcConfigManager                             │
├─────────────────────────────────────────────────────┤
│ 【処理内容】                                         │
│   - 複数のPlcConfigurationをリストとして保持         │
│   - 各設定に一意のID割り当て                         │
│   - 設定ごとの実行状態管理                           │
│                                                      │
│ 【データ構造】                                       │
│   Dictionary<string, PlcConfiguration> _configs     │
│   key: ConfigurationName (Excelファイル名)          │
│   value: PlcConfiguration                           │
│                                                      │
│ 【機能】                                             │
│   - GetConfiguration(name): 名前で設定取得           │
│   - GetAllConfigurations(): 全設定取得               │
│   - GetConfigurationCount(): 設定数取得              │
└─────────────────────────────────────────────────────┘
                        ↓
                   Step2へ渡す
```

---

## Excel設定ファイルフォーマット

### シート1: "settings"

```
┌──────┬─────────────────────┬──────────────────┐
│ セル │ 設定項目             │ 例               │
├──────┼─────────────────────┼──────────────────┤
│ B8   │ PLCのIPアドレス      │ 172.30.40.15     │
│ B9   │ PLCのポート          │ 8192             │
│ B11  │ データ取得周期(ms)   │ 1000             │
│ B12  │ デバイス名(PLC識別)  │ ライン1-炉A      │
│ B13  │ データ保存先パス     │ C:\data\output   │
└──────┴─────────────────────┴──────────────────┘

注意：
- B8, B9のみ通信に使用（他の通信設定は全てハードコード）
- B11: Step7での出力周期制御に使用
- B12: 出力JSONの"source.plcModel"に対応
- B13: 出力ファイルの保存先パス
```

### シート2: "データ収集デバイス"

```
┌────┬──────────┬──────────┬──────┬──────────┐
│ A  │    B     │    C     │  D   │    E     │
├────┼──────────┼──────────┼──────┼──────────┤
│項目名│デバイスコード│デバイス番号│桁数│  単位   │ ← ヘッダ行(1行目)
├────┼──────────┼──────────┼──────┼──────────┤
│温度1│   D      │  60000   │  1   │  word    │ ← データ行(2行目～)
│温度2│   D      │  60075   │  1   │  word    │
│圧力 │   D      │  60082   │  1   │  word    │
│状態 │   M      │   32     │  1   │  bit     │
│入力 │   X      │  1760    │  1   │  bit     │
│出力 │   Y      │  2304    │  1   │  bit     │
└────┴──────────┴──────────┴──────┴──────────┘

注意：
- A列（項目名）: 出力JSONの"items[].name"に対応
- B列（デバイスコード）: M, D, X, Y等（出力JSONの"items[].device.code"）
- C列（デバイス番号）: 10進数で記載（16進デバイスも10進数で記載）
- D列（桁数）: 出力JSONの"items[].digits"に対応
- E列（単位）: "bit", "word", "dword"（出力JSONの"items[].unit"）
- ヘッダ行は1行目、データは2行目から空白行まで
```

---

## ハードコード化する通信設定

memo.mdの送信フレームから抽出した固定値：

```csharp
/// <summary>
/// SLMP通信設定（固定値）
/// memo.mdの送信フレーム仕様に基づく
/// </summary>
public static class SlmpFixedSettings
{
    // フレーム設定（固定）
    public const string FrameVersion = "4E";        // 4Eフレーム固定
    public const string Protocol = "UDP";           // UDP固定

    // 通信対象設定（memo.mdフレームから抽出、固定）
    public const byte NetworkNumber = 0x00;         // ネットワーク番号: 0
    public const byte StationNumber = 0xFF;         // PC番号: 255 (全局指定)
    public const ushort IoNumber = 0x03FF;          // I/O番号: 1023
    public const byte MultiDropStation = 0x00;      // マルチドロップ局番: 0

    // タイムアウト設定（固定）
    public const ushort MonitorTimer = 0x0020;      // 監視タイマ: 32 (8秒)
    public const int ReceiveTimeoutMs = 500;        // 受信タイムアウト: 500ms

    // コマンド設定（固定）
    public const ushort Command = 0x0403;           // ReadRandom (ランダム読出)
    public const ushort SubCommand = 0x0000;        // サブコマンド

    // サブヘッダ（固定）
    public static readonly byte[] SubHeader_4E = { 0x54, 0x00 };
    public static readonly byte[] Serial = { 0x00, 0x00 };
    public static readonly byte[] Reserved = { 0x00, 0x00 };
}
```

### 送信フレーム構造（memo.md準拠）

```
| Idx | 長さ | 名称               | 値（固定）           |
|-----|------|--------------------|---------------------|
| 0   | 2    | サブヘッダ         | 0x54 0x00          |
| 2   | 2    | シリアル           | 0x00 0x00          |
| 4   | 2    | 予約               | 0x00 0x00          |
| 6   | 1    | ネットワーク番号   | 0x00               |
| 7   | 1    | 局番               | 0xFF               |
| 8   | 2    | I/O番号            | 0xFF 0x03 (LE)     |
| 10  | 1    | マルチドロップ     | 0x00               |
| 11  | 2    | データ長           | 動的計算            |
| 13  | 2    | 監視タイマ         | 0x20 0x00 (LE)     |
| 15  | 2    | コマンド           | 0x03 0x04 (LE)     |
| 17  | 2    | サブコマンド       | 0x00 0x00          |
| 19  | 1    | ワード点数         | 動的計算            |
| 20  | 1    | Dword点数          | 0x00               |
| 21~ | n*4  | デバイス指定       | 動的生成            |
```

---

## デバイスコードマップ（24種類完全対応）

ConMoniの変換テーブルを完全移植：

| デバイス名 | デバイスコード | 進数表記 | ビット/ワード | 用途 |
|-----------|---------------|---------|--------------|------|
| SM | 0x91 | 10進 | ビット | 特殊リレー |
| SD | 0xA9 | 10進 | ワード | 特殊レジスタ |
| X | 0x9C | 16進 | ビット | 入力 |
| Y | 0x9D | 16進 | ビット | 出力 |
| M | 0x90 | 10進 | ビット | 内部リレー |
| L | 0x92 | 10進 | ビット | ラッチリレー |
| F | 0x93 | 10進 | ビット | アナンシエータ |
| V | 0x94 | 10進 | ビット | エッジリレー |
| B | 0xA0 | 16進 | ビット | リンクリレー |
| D | 0xA8 | 10進 | ワード | データレジスタ |
| W | 0xB4 | 10進 | ワード | リンクレジスタ |
| TS | 0xC1 | 10進 | ビット | タイマ接点 |
| TC | 0xC0 | 10進 | ビット | タイマコイル |
| TN | 0xC2 | 10進 | ワード | タイマ現在値 |
| STS | 0xC7 | 10進 | ビット | 積算タイマ接点 |
| STC | 0xC6 | 10進 | ビット | 積算タイマコイル |
| STN | 0xC8 | 10進 | ワード | 積算タイマ現在値 |
| CS | 0xC4 | 10進 | ビット | カウンタ接点 |
| CC | 0xC3 | 10進 | ビット | カウンタコイル |
| CN | 0xC5 | 10進 | ワード | カウンタ現在値 |
| SB | 0xA1 | 16進 | ビット | リンク特殊リレー |
| SW | 0xB5 | 10進 | ワード | リンク特殊レジスタ |
| DX | 0xA2 | 16進 | ビット | ダイレクト入力 |
| DY | 0xA3 | 16進 | ビット | ダイレクト出力 |
| Z | 0xCC | 10進 | ワード | インデックスレジスタ |
| R | 0xAF | 10進 | ワード | ファイルレジスタ |
| ZR | 0xB0 | 10進 | ワード | ファイルレジスタ |

---

## デバイス番号変換アルゴリズム

### 10進デバイスの変換

```
対象デバイス: M, D, W, L, V, TN, CN等

変換方法: int → 3バイトリトルエンディアン

例1: D60000 (10進)
  60000 (dec) = 0xEA60 (hex)
  3バイトLE: [0x60, 0xEA, 0x00]

例2: M57074 (10進)
  57074 (dec) = 0xDEB2 (hex)
  3バイトLE: [0xB2, 0xDE, 0x00]

例3: D500 (10進)
  500 (dec) = 0x01F4 (hex)
  3バイトLE: [0xF4, 0x01, 0x00]
```

### 16進デバイスの変換

```
対象デバイス: X, Y, B, SB, DX, DY

変換方法: int → 16進文字列 → 6桁パディング → 3バイト

例1: X1760 (16進表記)
  1760 (dec) = 0x06E0 (hex)
  6桁パディング: "0006E0"
  3バイト: [0xE0, 0x06, 0x00]

例2: Y2304 (16進表記)
  2304 (dec) = 0x0900 (hex)
  6桁パディング: "000900"
  3バイト: [0x00, 0x09, 0x00]

例3: Y40 (16進表記)
  40 (dec) = 0x0028 (hex)
  6桁パディング: "000028"
  3バイト: [0x28, 0x00, 0x00]

注意：Excelでは10進数で記載されているため、
      プログラム内で16進デバイスとして扱う際に変換
```

---

## 主要クラス設計

### 1. ConfigurationLoader（Excel読み込み機能統合版）

```csharp
/// <summary>
/// 設定ファイル読み込み（Excel形式対応）
/// Infrastructure/Configuration/ConfigurationLoader.cs
/// </summary>
public class ConfigurationLoader
{
    private readonly ILogger<ConfigurationLoader> _logger;

    public ConfigurationLoader(ILogger<ConfigurationLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 複数のExcelファイルから設定を一括読み込み
    /// </summary>
    public List<PlcConfiguration> LoadAllPlcConnectionConfigs()
    {
        var excelFiles = DiscoverExcelFiles();
        var configs = new List<PlcConfiguration>();

        foreach (var filePath in excelFiles)
        {
            try
            {
                var config = LoadFromExcel(filePath);
                configs.Add(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"設定ファイル読み込みエラー: {filePath}");
                throw;
            }
        }

        return configs;
    }

    /// <summary>
    /// 実行フォルダ内の全.xlsxファイルを検索（privateヘルパー）
    /// </summary>
    private List<string> DiscoverExcelFiles()
    {
        string baseDirectory = AppContext.BaseDirectory;
        var xlsxFiles = Directory.GetFiles(baseDirectory, "*.xlsx")
            .Where(f => !Path.GetFileName(f).StartsWith("~$")) // 一時ファイル除外
            .Where(f => !IsFileLocked(f)) // ロックファイル除外
            .ToList();

        if (!xlsxFiles.Any())
        {
            throw new ArgumentException(
                $"設定ファイル(.xlsx)が見つかりません: {baseDirectory}");
        }

        return xlsxFiles;
    }

    /// <summary>
    /// Excelファイルから設定を読み込み（privateヘルパー）
    /// </summary>
    private PlcConfiguration LoadFromExcel(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        try
        {
            using var package = new ExcelPackage(new FileInfo(filePath));

            // シート存在確認
            var settingsSheet = package.Workbook.Worksheets["settings"];
            var devicesSheet = package.Workbook.Worksheets["データ収集デバイス"];

            if (settingsSheet == null)
                throw new ArgumentException(
                    $"'settings'シートが見つかりません: {filePath}");

            if (devicesSheet == null)
                throw new ArgumentException(
                    $"'データ収集デバイス'シートが見つかりません: {filePath}");

            // 設定読み込み
            var config = new PlcConfiguration
            {
                SourceExcelFile = filePath,
                IpAddress = ReadCell<string>(settingsSheet, "B8", "PLCのIPアドレス"),
                Port = ReadCell<int>(settingsSheet, "B9", "PLCのポート"),
                DataReadingFrequency = ReadCell<int>(settingsSheet, "B11", "データ取得周期"),
                PlcModel = ReadCell<string>(settingsSheet, "B12", "デバイス名"),
                SavePath = ReadCell<string>(settingsSheet, "B13", "データ保存先パス"),
                Devices = ReadDevices(devicesSheet, filePath)
            };

            _logger.LogInformation($"設定ファイル読み込み完了: {filePath}");

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"設定ファイル読み込みエラー: {filePath}");
            throw;
        }
    }

    private T ReadCell<T>(ExcelWorksheet sheet, string cellAddress, string itemName)
    {
        try
        {
            var value = sheet.Cells[cellAddress].GetValue<T>();

            if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
            {
                throw new ArgumentException(
                    $"{itemName}が設定されていません（セル{cellAddress}）");
            }

            return value;
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                $"{itemName}の読み込みに失敗しました（セル{cellAddress}）: {ex.Message}",
                ex);
        }
    }

    private List<DeviceSpecification> ReadDevices(
        ExcelWorksheet sheet, string sourceFile)
    {
        var devices = new List<DeviceSpecification>();
        int row = 2; // 1行目はヘッダ、2行目からデータ

        while (true)
        {
            string itemName = sheet.Cells[$"A{row}"].GetValue<string>();

            // A列が空になったら終了
            if (string.IsNullOrWhiteSpace(itemName))
                break;

            try
            {
                string deviceCode = sheet.Cells[$"B{row}"].GetValue<string>();
                int deviceNumber = sheet.Cells[$"C{row}"].GetValue<int>();
                int digits = sheet.Cells[$"D{row}"].GetValue<int>();
                string unit = sheet.Cells[$"E{row}"].GetValue<string>();

                // デバイス情報の正規化
                var device = NormalizeDevice(
                    itemName, deviceCode, deviceNumber, digits, unit);

                devices.Add(device);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"デバイス情報の読み込みに失敗（{sourceFile}、行{row}）: {ex.Message}",
                    ex);
            }

            row++;
        }

        if (devices.Count == 0)
        {
            throw new ArgumentException(
                $"デバイスが1つも設定されていません: {sourceFile}");
        }

        _logger.LogInformation($"デバイス数: {devices.Count}個");

        return devices;
    }

    /// <summary>
    /// デバイス情報の正規化（privateヘルパー）
    /// </summary>
    private DeviceSpecification NormalizeDevice(
        string itemName,
        string deviceCode,
        int deviceNumber,
        int digits,
        string unit)
    {
        // デバイスコード検証
        if (!DeviceCodeMap.IsValidDeviceType(deviceCode))
        {
            throw new ArgumentException(
                $"未対応のデバイスタイプ: {deviceCode}");
        }

        // 単位検証
        string unitLower = unit.ToLower();
        if (unitLower != "bit" && unitLower != "word" && unitLower != "dword")
        {
            throw new ArgumentException(
                $"未対応の単位: {unit}（\"bit\", \"word\", \"dword\"のいずれかを指定）");
        }

        byte deviceCodeByte = DeviceCodeMap.GetDeviceCode(deviceCode);
        bool isHex = DeviceCodeMap.IsHexDevice(deviceCode);
        bool isBit = DeviceCodeMap.IsBitDevice(deviceCode);
        byte[] deviceBytes = ConvertDeviceNumberToBytes(deviceNumber, isHex);

        return new DeviceSpecification
        {
            ItemName = itemName,
            DeviceType = deviceCode.ToUpper(),
            DeviceNumber = deviceNumber,
            Digits = digits,
            Unit = unitLower,
            DeviceCode = deviceCodeByte,
            DeviceBytes = deviceBytes,
            IsHexDevice = isHex,
            IsBitDevice = isBit
        };
    }

    /// <summary>
    /// デバイス番号を3バイトのリトルエンディアンに変換（privateヘルパー）
    /// </summary>
    private byte[] ConvertDeviceNumberToBytes(int deviceNumber, bool isHexDevice)
    {
        if (deviceNumber < 0 || deviceNumber > 0xFFFFFF)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deviceNumber),
                $"デバイス番号が範囲外です: {deviceNumber}（0～16777215）");
        }

        if (isHexDevice)
        {
            // 16進デバイス（X, Y等）
            string hexString = deviceNumber.ToString("X6");
            return new byte[]
            {
                Convert.ToByte(hexString.Substring(4, 2), 16), // 下位
                Convert.ToByte(hexString.Substring(2, 2), 16), // 中位
                Convert.ToByte(hexString.Substring(0, 2), 16)  // 上位
            };
        }
        else
        {
            // 10進デバイス（M, D等）
            return new byte[]
            {
                (byte)(deviceNumber & 0xFF),
                (byte)((deviceNumber >> 8) & 0xFF),
                (byte)((deviceNumber >> 16) & 0xFF)
            };
        }
    }

    /// <summary>
    /// ファイルロック確認（privateヘルパー）
    /// </summary>
    private static bool IsFileLocked(string filePath)
    {
        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }
}
```

### 2. DeviceCodeMap

```csharp
/// <summary>
/// デバイスコード変換マップ（ConMoni 24種類完全対応）
/// </summary>
public static class DeviceCodeMap
{
    private static readonly Dictionary<string, DeviceInfo> _deviceMap = new()
    {
        // ビットデバイス（10進）
        { "SM", new DeviceInfo(0x91, false, true) },  // 特殊リレー
        { "M",  new DeviceInfo(0x90, false, true) },  // 内部リレー
        { "L",  new DeviceInfo(0x92, false, true) },  // ラッチリレー
        { "F",  new DeviceInfo(0x93, false, true) },  // アナンシエータ
        { "V",  new DeviceInfo(0x94, false, true) },  // エッジリレー
        { "TS", new DeviceInfo(0xC1, false, true) },  // タイマ接点
        { "TC", new DeviceInfo(0xC0, false, true) },  // タイマコイル
        { "STS", new DeviceInfo(0xC7, false, true) }, // 積算タイマ接点
        { "STC", new DeviceInfo(0xC6, false, true) }, // 積算タイマコイル
        { "CS", new DeviceInfo(0xC4, false, true) },  // カウンタ接点
        { "CC", new DeviceInfo(0xC3, false, true) },  // カウンタコイル

        // ビットデバイス（16進）
        { "X",  new DeviceInfo(0x9C, true, true) },   // 入力
        { "Y",  new DeviceInfo(0x9D, true, true) },   // 出力
        { "B",  new DeviceInfo(0xA0, true, true) },   // リンクリレー
        { "SB", new DeviceInfo(0xA1, true, true) },   // リンク特殊リレー
        { "DX", new DeviceInfo(0xA2, true, true) },   // ダイレクト入力
        { "DY", new DeviceInfo(0xA3, true, true) },   // ダイレクト出力

        // ワードデバイス（10進）
        { "SD", new DeviceInfo(0xA9, false, false) }, // 特殊レジスタ
        { "D",  new DeviceInfo(0xA8, false, false) }, // データレジスタ
        { "W",  new DeviceInfo(0xB4, false, false) }, // リンクレジスタ
        { "SW", new DeviceInfo(0xB5, false, false) }, // リンク特殊レジスタ
        { "TN", new DeviceInfo(0xC2, false, false) }, // タイマ現在値
        { "STN", new DeviceInfo(0xC8, false, false) },// 積算タイマ現在値
        { "CN", new DeviceInfo(0xC5, false, false) }, // カウンタ現在値
        { "Z",  new DeviceInfo(0xCC, false, false) }, // インデックスレジスタ
        { "R",  new DeviceInfo(0xAF, false, false) }, // ファイルレジスタ
        { "ZR", new DeviceInfo(0xB0, false, false) }  // ファイルレジスタ
    };

    public static byte GetDeviceCode(string deviceType)
    {
        if (!_deviceMap.TryGetValue(deviceType.ToUpper(), out var info))
            throw new ArgumentException($"未対応のデバイスタイプ: {deviceType}");
        return info.Code;
    }

    public static bool IsHexDevice(string deviceType)
        => _deviceMap.TryGetValue(deviceType.ToUpper(), out var info) && info.IsHex;

    public static bool IsBitDevice(string deviceType)
        => _deviceMap.TryGetValue(deviceType.ToUpper(), out var info) && info.IsBit;

    public static bool IsValidDeviceType(string deviceType)
        => _deviceMap.ContainsKey(deviceType.ToUpper());

    private record DeviceInfo(byte Code, bool IsHex, bool IsBit);
}
```

### 3. PlcConfiguration

```csharp
/// <summary>
/// PLC設定（Excelから読み込んだ情報のみ）
/// 通信設定は全てSlmpFixedSettingsを使用
/// </summary>
public class PlcConfiguration
{
    // Excel "settings"シートから読み込み
    public string IpAddress { get; set; } = string.Empty;           // B8
    public int Port { get; set; }                                    // B9
    public int DataReadingFrequency { get; set; }                    // B11
    public string PlcModel { get; set; } = string.Empty;             // B12
    public string SavePath { get; set; } = string.Empty;             // B13

    // メタ情報
    public string SourceExcelFile { get; set; } = string.Empty;
    public string ConfigurationName => Path.GetFileNameWithoutExtension(SourceExcelFile);

    // Excel "データ収集デバイス"シートから読み込み
    public List<DeviceSpecification> Devices { get; set; } = new();
}
```

### 4. DeviceSpecification

```csharp
/// <summary>
/// デバイス指定情報（Excel1行分）
/// Core/Models/DeviceSpecification.cs
/// </summary>
public class DeviceSpecification
{
    // Excel "データ収集デバイス"シートから読み込み
    public string ItemName { get; set; } = string.Empty;    // A列: 項目名
    public string DeviceType { get; set; } = string.Empty;  // B列: デバイスコード（M, D, X等）
    public int DeviceNumber { get; set; }                   // C列: デバイス番号（10進）
    public int Digits { get; set; }                         // D列: 桁数
    public string Unit { get; set; } = string.Empty;        // E列: 単位（bit, word, dword）

    // 正規化処理で追加される情報
    public byte DeviceCode { get; set; }                    // SLMPデバイスコード（0x90, 0xA8等）
    public byte[] DeviceBytes { get; set; } = Array.Empty<byte>(); // 3バイトLE表現
    public bool IsHexDevice { get; set; }                   // 16進デバイスフラグ
    public bool IsBitDevice { get; set; }                   // ビットデバイスフラグ
}
```

### 5. SlmpFixedSettings

```csharp
/// <summary>
/// SLMP通信設定（全て固定値、memo.md送信フレーム準拠）
/// Core/Constants/SlmpConstants.cs 内に配置
/// </summary>
public static class SlmpFixedSettings
{
    // フレーム設定
    public const string FrameVersion = "4E";
    public const string Protocol = "UDP";

    // 通信対象設定（memo.mdフレームから抽出）
    public const byte NetworkNumber = 0x00;
    public const byte StationNumber = 0xFF;
    public const ushort IoNumber = 0x03FF;
    public const byte MultiDropStation = 0x00;

    // タイムアウト設定
    public const ushort MonitorTimer = 0x0020;      // 32 = 8秒
    public const int ReceiveTimeoutMs = 500;

    // コマンド設定
    public const ushort Command = 0x0403;           // ReadRandom
    public const ushort SubCommand = 0x0000;

    // サブヘッダ
    public static readonly byte[] SubHeader_4E = { 0x54, 0x00 };
    public static readonly byte[] Serial = { 0x00, 0x00 };
    public static readonly byte[] Reserved = { 0x00, 0x00 };

    /// <summary>
    /// 固定設定でフレームヘッダを構築
    /// </summary>
    public static byte[] BuildFrameHeader(int dataLength)
    {
        var header = new List<byte>();

        // サブヘッダ (0-1)
        header.AddRange(SubHeader_4E);

        // シリアル (2-3)、予約 (4-5)
        header.AddRange(Serial);
        header.AddRange(Reserved);

        // ネットワーク番号 (6)
        header.Add(NetworkNumber);

        // 局番 (7)
        header.Add(StationNumber);

        // I/O番号 (8-9)
        header.AddRange(BitConverter.GetBytes(IoNumber));

        // マルチドロップ (10)
        header.Add(MultiDropStation);

        // データ長 (11-12)
        header.AddRange(BitConverter.GetBytes((ushort)dataLength));

        // 監視タイマ (13-14)
        header.AddRange(BitConverter.GetBytes(MonitorTimer));

        // コマンド (15-16)
        header.AddRange(BitConverter.GetBytes(Command));

        // サブコマンド (17-18)
        header.AddRange(BitConverter.GetBytes(SubCommand));

        return header.ToArray();
    }
}
```

---

## 出力JSON構造との対応

Step7で出力するJSON構造（設定読み込み仕様.md準拠）：

```json
{
  "source": {
    "plcModel": "← PlcConfiguration.PlcModel",
    "ipAddress": "← PlcConfiguration.IpAddress",
    "port": "← PlcConfiguration.Port"
  },
  "timestamp": {
    "local": "← 実行時に生成（ISO 8601形式）"
  },
  "items": [
    {
      "name": "← DeviceSpecification.ItemName",
      "device": {
        "code": "← DeviceSpecification.DeviceType",
        "number": "← DeviceSpecification.DeviceNumber（文字列化）"
      },
      "digits": "← DeviceSpecification.Digits",
      "unit": "← DeviceSpecification.Unit",
      "value": "← 実行時に取得したPLC値"
    }
  ]
}
```

---

## 成功条件

- ✅ 実行フォルダ内の全.xlsxファイルを自動検出できること
- ✅ Excelの"settings"シートから5項目を正確に読み込めること
- ✅ Excelの"データ収集デバイス"シートから全デバイス情報を読み込めること
- ✅ デバイスコード24種類全てに対応できること
- ✅ 10進/16進デバイスを正しく判別・変換できること
- ✅ ビットデバイスを16点単位でワード化できること
- ✅ 不正な設定値を検出してエラーを返すこと
- ✅ 複数のExcelファイルを同時に管理できること
- ✅ 通信設定が全てmemo.md送信フレームと一致すること

---

## エラーハンドリング

### Excel読み込み時のエラー

| エラー条件 | 例外 | メッセージ例 |
|-----------|------|------------|
| .xlsxファイルが見つからない | ArgumentException | "設定ファイル(.xlsx)が見つかりません: C:\app" |
| シートが存在しない | ArgumentException | "'settings'シートが見つかりません: config.xlsx" |
| セルが空 | ArgumentException | "PLCのIPアドレスが設定されていません（セルB8）" |
| 型変換エラー | ArgumentException | "PLCのポートの読み込みに失敗しました（セルB9）" |
| デバイス情報読み込みエラー | ArgumentException | "デバイス情報の読み込みに失敗（config.xlsx、行3）" |

### 設定検証時のエラー

| エラー条件 | 例外 | メッセージ例 |
|-----------|------|------------|
| 未対応デバイスタイプ | ArgumentException | "未対応のデバイスタイプ: Z" |
| 未対応単位 | ArgumentException | "未対応の単位: byte（\"bit\", \"word\", \"dword\"のいずれか）" |
| デバイス番号範囲外 | ArgumentOutOfRangeException | "デバイス番号が範囲外です: 20000000（0～16777215）" |
| 総点数超過 | ArgumentException | "デバイス点数が上限を超えています: 300点（最大255点）" |
| IPアドレス不正 | ArgumentException | "IPアドレスの形式が不正です: 192.168.1" |
| ポート範囲外 | ArgumentException | "ポート番号が範囲外です: 70000（1～65535）" |

---

## 実装の特徴

### ✅ 採用した設計

1. **ConMoniの実証済みアルゴリズム**
   - 24種類デバイスコード完全対応
   - ビット最適化（16点単位ワード化）
   - 10進/16進デバイスの自動判別

2. **通信設定の固定化**
   - memo.md送信フレームと完全一致
   - IPアドレスとポート以外は全てハードコード
   - 設定ミスの可能性を排除

3. **Excel完全駆動設計**
   - JSON設定完全廃止
   - 現場エンジニアが直接編集可能
   - 複数PLCの同時監視対応

4. **出力JSON構造への対応**
   - DeviceSpecificationが出力構造と1対1対応
   - Step7での変換処理が単純化

### ⚠️ 制約事項

- **点数上限**: ReadRandomコマンドの制約により最大255点
- **フレームバージョン固定**: 4Eフレーム固定（3E未対応）
- **プロトコル固定**: UDP固定（TCP未対応）
- **Excel必須**: 少なくとも1つの.xlsxファイルが必要

---

## 次のステップ

Step1で読み込んだ `PlcConfiguration` は、Step2のフレーム構築処理に渡されます。

```
Step1: Excel読み込み
    ↓ (PlcConfiguration)
Step2: フレーム構築
    ↓ (byte[])
Step3: PLC接続・送信
    ↓ (byte[])
Step6: 応答解析
    ↓ (デバイス値)
Step7: JSON出力
```

---

## 参考資料

- **設計元**: ConMoni (sample) - Step1処理フロー
- **フレーム仕様**: memo.md - PLC単一通信テストプログラム
- **出力仕様**: 設定読み込み仕様.md
- **デバイスコード**: ConMoniデバイスコード変換テーブル（24種類）

---

## 実装チェックリスト

### 主要クラス実装
- [ ] ConfigurationLoader クラス実装（Excel読み込み機能統合）
  - [ ] DiscoverExcelFiles() privateヘルパー実装
  - [ ] LoadFromExcel() privateヘルパー実装
  - [ ] ReadSettingsSheet() privateヘルパー実装
  - [ ] ReadDevicesSheet() privateヘルパー実装
  - [ ] NormalizeDevice() privateヘルパー実装
  - [ ] ConvertDeviceNumberToBytes() privateヘルパー実装
  - [ ] OptimizeBitDevices() privateヘルパー実装（オプション）
  - [ ] ValidateConfiguration() privateヘルパー実装
  - [ ] IsFileLocked() privateヘルパー実装
- [ ] DeviceCodeMap クラス実装（24種類対応）
- [ ] PlcConfiguration モデルクラス実装
- [ ] DeviceSpecification モデルクラス実装
- [ ] SlmpFixedSettings クラス実装（Core/Constants/SlmpConstants.cs内）
- [ ] MultiPlcConfigManager クラス実装

### テスト実装
- [ ] ConfigurationLoader 単体テスト実装
- [ ] DeviceCodeMap 単体テスト実装
- [ ] 統合テスト実装（Excel読み込み～検証まで）

### 依存関係・環境整備
- [ ] EPPlusライブラリ追加（NuGet）
- [ ] Excelサンプルファイル作成

---

**最終更新**: 2025年11月26日（プロジェクト構造設計準拠版）
