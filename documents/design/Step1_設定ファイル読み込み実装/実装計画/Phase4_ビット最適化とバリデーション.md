# Step1 実装計画 - Phase4: ビット最適化とバリデーション

**作成日**: 2025-11-27
**最終更新**: 2025-11-27

## Phase4の目的

ビットデバイスを16点単位でワード化して通信効率を向上させ、設定全体の妥当性を検証する。

---

## Phase3からの引継ぎ事項

### Phase3完了状況

✅ **デバイス情報正規化完了**（2025-11-27完了）
- Excel読み込み～DeviceSpecification生成までの完全実装
- 24種類全デバイスタイプ対応
- Phase1/Phase2との完全統合
- テスト合格率: 100% (19/19テスト)

✅ **ReadDevices()更新完了**
- Phase2の仮実装（DeviceCode.D固定）削除
- NormalizeDevice()統合
- エラーハンドリング継承

✅ **利用可能な実装基盤**
- `DeviceCodeMap`: 24種類デバイスタイプ検証・変換機能（Phase1実装）
- `DeviceSpecification`: SLMP通信用デバイス情報表現（Phase1実装）
- `ConfigurationLoaderExcel`: Excel読み込み・デバイス情報正規化（Phase2/Phase3実装）
- `NormalizeDevice()`: デバイス情報正規化処理（Phase3実装、privateメソッド）

### Phase4で活用可能な機能

| 機能 | 実装Phase | Phase4での活用箇所 |
|------|---------|------------------|
| `DeviceCodeMap.IsValidDeviceType()` | Phase1 | デバイスタイプ検証 |
| `DeviceSpecification` | Phase1 | デバイス情報表現 |
| `ConfigurationLoaderExcel.LoadFromExcel()` | Phase2/Phase3 | 検証処理の統合先 |
| `ReadDevices()` | Phase2/Phase3 | 正規化済みデバイスリスト取得 |

### Phase3で確立された設計判断

- **段階的実装方針**: 各Phaseで最小限の変更、リスク分散
- **privateメソッド活用**: 内部実装の隠蔽、統合テストによる検証
- **Phase1実装の全面活用**: コード重複排除、一貫性保持
- **大文字小文字正規化**: ユーザー入力の柔軟性、内部表現の統一

---

## Phase4の実装範囲

Phase4では以下の2つの機能を実装します：

1. **ビットデバイス最適化処理**（OptimizeBitDevices） - **オプション機能**
2. **設定検証処理**（ValidateConfiguration） - **必須機能**

**注意**: ビット最適化は効果的だが、実装は複雑であるため、Phase4では**最低限のバリデーション機能のみを必須**とし、最適化は将来の拡張として扱う。

---

## 1. 設定検証処理（必須実装）

### 実装クラス: ConfigurationLoaderExcel

**メソッド**: `private void ValidateConfiguration(PlcConfiguration config)`

**目的**: 読み込んだ設定の妥当性を検証し、不正な設定を早期検出

**Phase3で準備された基盤**:
- `config.Devices`: Phase3で正規化済みのDeviceSpecificationリスト
- `device.Code`: Phase3でDeviceCode列挙型に変換済み
- `device.IsHexAddress`: Phase3で10進/16進判定済み
- `device.DeviceType`: Phase3で大文字正規化済み
- `device.Unit`: Phase3で小文字正規化済み

**検証項目**:

#### ① 接続情報検証

```csharp
// IPアドレス形式チェック
if (!IPAddress.TryParse(config.IpAddress, out _))
{
    throw new ArgumentException(
        $"IPアドレスの形式が不正です: {config.IpAddress}");
}

// ポート範囲チェック（1～65535）
if (config.Port < 1 || config.Port > 65535)
{
    throw new ArgumentException(
        $"ポート番号が範囲外です: {config.Port}（1～65535）");
}
```

#### ② データ取得周期検証

```csharp
// データ取得周期範囲（1ms～24時間）
if (config.DataReadingFrequency < 1 || config.DataReadingFrequency > 86400000)
{
    throw new ArgumentException(
        $"データ取得周期が範囲外です: {config.DataReadingFrequency}（1～86400000ms）");
}
```

#### ③ デバイスリスト検証

**Phase3との役割分担**:
- **Phase3 (NormalizeDevice())**: デバイスタイプ・単位の妥当性検証（既に実施済み）
- **Phase4 (ValidateConfiguration())**: デバイス番号範囲とリスト全体の検証

```csharp
// 最低1デバイス存在チェック
if (config.Devices == null || config.Devices.Count == 0)
{
    throw new ArgumentException(
        $"デバイスが1つも設定されていません: {config.SourceExcelFile}");
}

// 各デバイスの妥当性チェック
foreach (var device in config.Devices)
{
    // ★Phase3で既にデバイスタイプ・単位検証済みのため、
    // ここでは二重検証を行わず、デバイス番号のみ検証★

    // デバイス番号範囲（0～16777215、3バイト範囲）
    if (device.DeviceNumber < 0 || device.DeviceNumber > 0xFFFFFF)
    {
        throw new ArgumentOutOfRangeException(
            $"デバイス番号が範囲外です: {device.DeviceNumber}（項目名: {device.ItemName}、範囲: 0～16777215）");
    }
}
```

**Phase3との統合ポイント**:
- デバイスタイプ検証: Phase3のNormalizeDevice()で実施済み（不要）
- 単位検証: Phase3のNormalizeDevice()で実施済み（不要）
- デバイス番号範囲: Phase4で実施（新規）
- リスト全体の検証: Phase4で実施（新規）

#### ④ 総点数制限チェック

**ReadRandomコマンドの制約**: 最大255点まで

```csharp
// ワード点数とDword点数の計算
int totalWordPoints = config.Devices
    .Where(d => d.Unit.ToLower() == "word")
    .Sum(d => d.Digits);

int totalDwordPoints = config.Devices
    .Where(d => d.Unit.ToLower() == "dword")
    .Sum(d => d.Digits);

// ビット点数の計算（16点 = 1ワード）
int totalBitPoints = config.Devices
    .Where(d => d.Unit.ToLower() == "bit")
    .Sum(d => d.Digits);

int bitAsWords = (totalBitPoints + 15) / 16; // 切り上げ

// 総点数チェック
int totalPoints = totalWordPoints + (totalDwordPoints * 2) + bitAsWords;

if (totalPoints > 255)
{
    throw new ArgumentException(
        $"デバイス点数が上限を超えています: {totalPoints}点（最大255点）\n" +
        $"  Word: {totalWordPoints}点\n" +
        $"  Dword: {totalDwordPoints}点 (ワード換算: {totalDwordPoints * 2}点)\n" +
        $"  Bit: {totalBitPoints}点 (ワード換算: {bitAsWords}点)\n" +
        $"ファイル: {config.SourceExcelFile}");
}
```

#### ⑤ 出力設定検証

```csharp
// 保存先パス形式チェック
if (string.IsNullOrWhiteSpace(config.SavePath))
{
    throw new ArgumentException(
        $"データ保存先パスが設定されていません: {config.SourceExcelFile}");
}

// パス形式の妥当性チェック（Windowsパス）
try
{
    Path.GetFullPath(config.SavePath);
}
catch (Exception ex)
{
    throw new ArgumentException(
        $"データ保存先パスの形式が不正です: {config.SavePath}",
        ex);
}

// デバイス名チェック
if (string.IsNullOrWhiteSpace(config.PlcModel))
{
    throw new ArgumentException(
        $"デバイス名（PLC識別名）が設定されていません: {config.SourceExcelFile}");
}
```

### ValidateConfiguration()の完全実装

**Phase3との連携**:
- Phase3で正規化済みのデバイス情報を検証
- デバイスタイプ・単位の二重検証を回避

```csharp
/// <summary>
/// 設定の妥当性を検証（privateヘルパー）
/// Phase3のNormalizeDevice()で正規化済みの設定を検証
/// </summary>
private void ValidateConfiguration(PlcConfiguration config)
{
    // ① 接続情報検証
    if (!IPAddress.TryParse(config.IpAddress, out _))
    {
        throw new ArgumentException(
            $"IPアドレスの形式が不正です: {config.IpAddress}");
    }

    if (config.Port < 1 || config.Port > 65535)
    {
        throw new ArgumentException(
            $"ポート番号が範囲外です: {config.Port}（1～65535）");
    }

    // ② データ取得周期検証
    if (config.DataReadingFrequency < 1 || config.DataReadingFrequency > 86400000)
    {
        throw new ArgumentException(
            $"データ取得周期が範囲外です: {config.DataReadingFrequency}（1～86400000ms）");
    }

    // ③ デバイスリスト検証
    if (config.Devices == null || config.Devices.Count == 0)
    {
        throw new ArgumentException(
            $"デバイスが1つも設定されていません: {config.SourceExcelFile}");
    }

    foreach (var device in config.Devices)
    {
        // ★Phase3で既にデバイスタイプ・単位検証済みのため、
        // デバイス番号範囲のみ検証★

        if (device.DeviceNumber < 0 || device.DeviceNumber > 0xFFFFFF)
        {
            throw new ArgumentOutOfRangeException(
                $"デバイス番号が範囲外です: {device.DeviceNumber}（項目名: {device.ItemName}、範囲: 0～16777215）");
        }
    }

    // ④ 総点数制限チェック
    int totalWordPoints = config.Devices
        .Where(d => d.Unit.ToLower() == "word")
        .Sum(d => d.Digits);

    int totalDwordPoints = config.Devices
        .Where(d => d.Unit.ToLower() == "dword")
        .Sum(d => d.Digits);

    int totalBitPoints = config.Devices
        .Where(d => d.Unit.ToLower() == "bit")
        .Sum(d => d.Digits);

    int bitAsWords = (totalBitPoints + 15) / 16;
    int totalPoints = totalWordPoints + (totalDwordPoints * 2) + bitAsWords;

    if (totalPoints > 255)
    {
        throw new ArgumentException(
            $"デバイス点数が上限を超えています: {totalPoints}点（最大255点）\n" +
            $"  Word: {totalWordPoints}点\n" +
            $"  Dword: {totalDwordPoints}点 (ワード換算: {totalDwordPoints * 2}点)\n" +
            $"  Bit: {totalBitPoints}点 (ワード換算: {bitAsWords}点)\n" +
            $"ファイル: {config.SourceExcelFile}");
    }

    // ⑤ 出力設定検証
    if (string.IsNullOrWhiteSpace(config.SavePath))
    {
        throw new ArgumentException(
            $"データ保存先パスが設定されていません: {config.SourceExcelFile}");
    }

    try
    {
        Path.GetFullPath(config.SavePath);
    }
    catch (Exception ex)
    {
        throw new ArgumentException(
            $"データ保存先パスの形式が不正です: {config.SavePath}",
            ex);
    }

    if (string.IsNullOrWhiteSpace(config.PlcModel))
    {
        throw new ArgumentException(
            $"デバイス名（PLC識別名）が設定されていません: {config.SourceExcelFile}");
    }

    _logger.LogInformation($"設定検証完了: {config.ConfigurationName}");
}
```

### LoadFromExcel()への統合

Phase2で実装したLoadFromExcel()の最後に検証処理を追加：

```csharp
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

        // ★Phase4: 設定検証を追加★
        ValidateConfiguration(config);

        _logger.LogInformation($"設定ファイル読み込み完了: {filePath}");

        return config;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"設定ファイル読み込みエラー: {filePath}");
        throw;
    }
}
```

---

## 2. ビットデバイス最適化処理（オプション実装）

**注意**: この機能は複雑であるため、**Phase4では実装をスキップし、Phase5で余力があれば実装**することを推奨します。

### 概要

**目的**: ビットデバイスを16点単位でまとめてワード読み出しすることで、通信回数とフレームサイズを削減

**効果**:
- 連続した16点のビットデバイス: 16回 → 1回（94%削減）
- 離れたビットデバイス: 最適化効果は小さい

### アルゴリズム（ConMoni準拠）

```
入力: ビットデバイスのリスト（M32, M57074, M57118, M57182）

処理手順:
1. デバイスタイプでグループ化（M, X, Y等を分離）
2. 各グループ内でデバイス番号でソート
3. 16点単位（0～15, 16～31...）でグループ化
   - 範囲 = (deviceNumber / 16) * 16 ～ (deviceNumber / 16) * 16 + 15
4. 各チャンクの先頭デバイスを代表として選択
5. 未使用ビット位置も保持（null）

出力: 16点単位でまとめたチャンクリスト
```

### 具体例

**入力**: M32, M57074, M57118, M57182

**処理結果**:

| チャンク | 範囲 | 代表デバイス | 使用ビット位置 | 通信データ |
|---------|------|-------------|--------------|-----------|
| 1 | M32～M47 | M32 | [0]=M32 | [0x20, 0x00, 0x00, 0x90] + 1点 |
| 2 | M57074～M57089 | M57074 | [10]=M57074 | [0xB2, 0xDE, 0x00, 0x90] + 1点 |
| 3 | M57118～M57133 | M57118 | [14]=M57118 | [0xDE, 0xDF, 0x00, 0x90] + 1点 |
| 4 | M57182～M57197 | M57182 | [14]=M57182 | [0x50, 0xE0, 0x00, 0x90] + 1点 |

**効果**: 通信回数は4回で変わらず（離れたビットのため）

**より効果的な例**: M100～M115（連続16点）
- 最適化前: 16回の通信（各ビット1回ずつ）
- 最適化後: 1回の通信（16点まとめて）
- 削減率: 94%

### OptimizeBitDevices()の実装スケルトン（参考）

```csharp
/// <summary>
/// ビットデバイス最適化（オプション機能）
/// 16点単位でワード化してフレーム数削減
/// </summary>
private List<DeviceSpecification> OptimizeBitDevices(List<DeviceSpecification> devices)
{
    // ビットデバイスのみ抽出
    var bitDevices = devices
        .Where(d => d.IsBitDevice)
        .ToList();

    // ワードデバイスはそのまま
    var wordDevices = devices
        .Where(d => !d.IsBitDevice)
        .ToList();

    if (bitDevices.Count == 0)
        return devices; // 最適化不要

    // デバイスタイプでグループ化（M, X, Y等）
    var groupedByType = bitDevices
        .GroupBy(d => d.DeviceType)
        .ToList();

    var optimizedDevices = new List<DeviceSpecification>();

    foreach (var group in groupedByType)
    {
        // デバイス番号でソート
        var sorted = group.OrderBy(d => d.DeviceNumber).ToList();

        // 16点単位でチャンク化
        var chunks = sorted
            .GroupBy(d => (d.DeviceNumber / 16))
            .ToList();

        foreach (var chunk in chunks)
        {
            // 各チャンクの先頭デバイスを代表として選択
            int baseDeviceNumber = (int)chunk.Key * 16;
            var representative = chunk.First();

            // 代表デバイスのDeviceNumberを16点の先頭に調整
            representative.DeviceNumber = baseDeviceNumber;
            representative.DeviceBytes = ConvertDeviceNumberToBytes(
                baseDeviceNumber,
                representative.IsHexDevice);

            optimizedDevices.Add(representative);
        }
    }

    // ワードデバイスと結合
    optimizedDevices.AddRange(wordDevices);

    _logger.LogInformation(
        $"ビット最適化: {bitDevices.Count}デバイス → {optimizedDevices.Count(d => d.IsBitDevice)}チャンク");

    return optimizedDevices;
}
```

**Phase4での扱い**: 実装は見送り、**Phase5で検討**

---

## Phase4の成功条件

### 必須（バリデーション）

- ✅ IPアドレス形式を検証できること
- ✅ ポート範囲（1～65535）を検証できること
- ✅ データ取得周期範囲を検証できること
- ✅ デバイスが最低1つ存在することを検証できること
- ✅ 各デバイスのデバイスコードが24種類内であることを検証できること
- ✅ 各デバイスのデバイス番号が範囲内であることを検証できること
- ✅ 各デバイスの単位が妥当であることを検証できること
- ✅ 総点数が255点以内であることを検証できること
- ✅ 保存先パスが妥当であることを検証できること

### オプション（ビット最適化）

- ⏸️ Phase4ではスキップ、Phase5で検討

---

## Phase4のテスト計画

### Phase3との統合を考慮したテスト設計

**Phase3のテスト資産活用**:
- `TestExcelFileCreator`: Phase3で実装済みのExcelファイル生成メソッド
- Phase3の19テストケース: Phase4実装後も全継続動作を保証

**Phase4の新規テスト**:
- ValidateConfiguration()の検証項目（①～⑤）
- Phase3で検証済みの項目は除外（デバイスタイプ、単位）

### ValidateConfiguration()のテスト

#### 1. 正常系テスト

```csharp
// 正常な設定で例外がスローされないこと
// Phase3のCreateValidConfigFile()を活用
var config = CreateValidConfig();
Assert.DoesNotThrow(() => loader.ValidateConfiguration(config));
```

#### 2. 接続情報異常系テスト

```csharp
// 不正なIPアドレス
var config = CreateValidConfig();
config.IpAddress = "192.168.1";
Assert.Throws<ArgumentException>(() => loader.ValidateConfiguration(config));

// ポート範囲外（下限）
config = CreateValidConfig();
config.Port = 0;
Assert.Throws<ArgumentException>(() => loader.ValidateConfiguration(config));

// ポート範囲外（上限）
config = CreateValidConfig();
config.Port = 70000;
Assert.Throws<ArgumentException>(() => loader.ValidateConfiguration(config));
```

#### 3. データ取得周期異常系テスト

```csharp
// 下限未満
var config = CreateValidConfig();
config.DataReadingFrequency = 0;
Assert.Throws<ArgumentException>(() => loader.ValidateConfiguration(config));

// 上限超過
config = CreateValidConfig();
config.DataReadingFrequency = 90000000;
Assert.Throws<ArgumentException>(() => loader.ValidateConfiguration(config));
```

#### 4. デバイスリスト異常系テスト

**Phase3との役割分担**:
- 未対応デバイスタイプ・単位: Phase3のNormalizeDevice()でテスト済み（Phase4ではテスト不要）
- デバイス番号範囲外: Phase4で新規テスト

```csharp
// デバイスが0件
var config = CreateValidConfig();
config.Devices.Clear();
Assert.Throws<ArgumentException>(() => loader.ValidateConfiguration(config));

// デバイス番号範囲外（Phase4新規テスト）
config = CreateValidConfig();
config.Devices[0].DeviceNumber = 20000000;
Assert.Throws<ArgumentOutOfRangeException>(() => loader.ValidateConfiguration(config));

// ★Phase3で検証済みのため削除★
// - 未対応デバイスタイプ（Phase3のテストで実施済み）
// - 未対応単位（Phase3のテストで実施済み）
```

#### 5. 総点数制限テスト

```csharp
// 総点数が255点を超える場合
var config = CreateValidConfig();
for (int i = 0; i < 300; i++)
{
    config.Devices.Add(new DeviceSpecification
    {
        ItemName = $"Device{i}",
        DeviceType = "D",
        DeviceNumber = i,
        Digits = 1,
        Unit = "word"
    });
}
Assert.Throws<ArgumentException>(() => loader.ValidateConfiguration(config));
```

#### 6. 出力設定異常系テスト

```csharp
// 保存先パスが空
var config = CreateValidConfig();
config.SavePath = "";
Assert.Throws<ArgumentException>(() => loader.ValidateConfiguration(config));

// 不正なパス形式
config = CreateValidConfig();
config.SavePath = "C:\\invalid<path>\\data";
Assert.Throws<ArgumentException>(() => loader.ValidateConfiguration(config));

// デバイス名が空
config = CreateValidConfig();
config.PlcModel = "";
Assert.Throws<ArgumentException>(() => loader.ValidateConfiguration(config));
```

---

## Phase4の実装手順

### Phase3からの継続実装の注意点

**Phase3で確立された方針を継承**:
- 段階的実装（最小限の変更）
- privateメソッド活用（ValidateConfiguration()）
- Phase1/Phase3実装の全面活用（DeviceCodeMap、正規化済みデータ）
- 既存テストの継続動作保証

### 実装ステップ

1. **ValidateConfiguration()実装**
   - ConfigurationLoaderExcelにprivateメソッド追加
   - 各検証ロジック実装（①～⑤）
   - Phase3で正規化済みのため、デバイスタイプ・単位の二重検証を回避
   - ログ出力追加

2. **LoadFromExcel()更新**
   - Phase3実装（デバイス正規化後）の後にValidateConfiguration()呼び出しを追加
   - 変更は1行追加のみ（Phase3方針継承）

3. **エラーテスト用Excelファイル作成**（TestExcelFileCreatorに追加）
   - 不正なIPアドレス用ファイル
   - 範囲外のポート番号用ファイル
   - デバイス点数超過（300点）用ファイル
   - Phase3の6メソッドに加えて、Phase4で3メソッド追加

4. **単体テスト作成**
   - ValidateConfiguration()の正常系テスト（Phase3のCreateValidConfigFile()活用）
   - 各検証項目の異常系テスト
   - エラーメッセージ内容の検証
   - Phase3で検証済みの項目（デバイスタイプ、単位）は除外

5. **Phase3テストの継続動作確認**
   - Phase3の19テストケースが引き続き全成功することを確認
   - Phase4実装がPhase3機能に影響しないことを確認

6. **統合テスト更新**
   - 不正なExcelファイルでエラーが発生することを確認

7. **テスト実行・検証**
   - Phase3テスト19件 + Phase4新規テスト約10件 = 合計約29件
   - 全テストがパスすることを確認

---

## Phase4完了後の状態

### 機能完成状況

✅ **Excel読み込み～検証完了**（Phase1～Phase4統合）
- Phase1: DeviceCodeMap、DeviceSpecification（24種類対応）
- Phase2: Excel読み込み基盤
- Phase3: デバイス情報正規化（NormalizeDevice()）
- Phase4: 設定全体検証（ValidateConfiguration()）

✅ **設定妥当性検証機能**
- 読み込んだ設定の妥当性が完全に検証される
- 不正な設定を早期検出できる
- 適切なエラーメッセージでユーザーに通知できる
- Phase3で正規化済みのため、二重検証を回避

✅ **テスト完全性**
- Phase1～Phase4の全テストケースが継続動作
- 設定読み込みから検証までの完全な統合テスト
- エラーハンドリングの完全なカバレッジ

### Phase5への準備状況

✅ **Phase5（複数設定管理と統合）の実装に進む準備が完了**
- 単一設定ファイルの読み込み・検証が安定稼働
- 複数設定ファイルの一元管理基盤が整備済み
- Phase1～Phase4の全機能が統合され、安定性確保

---

## 次のPhase

**Phase5: 複数設定管理と統合**
- MultiPlcConfigManagerクラス実装
- 設定の一元管理機能
- （余力があれば）ビット最適化機能の実装
