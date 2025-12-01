# Step1 実装計画 - Phase2: Excel読み込み基礎機能

**作成日**: 2025年11月26日
**実装完了日**: 2025年11月27日
**ステータス**: ✅ 実装完了

## Phase2の目的

Excelファイルを検索・読み込み、基本的な設定情報を取得する機能を実装する。

---

## 実装対象機能

### 1. Excelファイル検索機能

**実装クラス**: ConfigurationLoader（privateヘルパー）

**メソッド**: `private List<string> DiscoverExcelFiles()`

**処理内容**:
```csharp
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
```

**検索仕様**:
- 検索場所: AppContext.BaseDirectory（実行ファイルのフォルダ）
- 検索パターン: *.xlsx
- 除外対象:
  - ~$*.xlsx（Excelの一時ファイル）
  - ロックされているファイル

**エラーハンドリング**:
- .xlsxファイルが1つも見つからない場合 → ArgumentException

### 2. ファイルロック確認機能

**実装クラス**: ConfigurationLoader（privateヘルパー）

**メソッド**: `private static bool IsFileLocked(string filePath)`

**処理内容**:
```csharp
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
```

**用途**:
- Excelが開いているファイルを除外
- 読み込み失敗を事前に防止

### 3. Excelファイル読み込み機能

**実装クラス**: ConfigurationLoader（privateヘルパー）

**メソッド**: `private PlcConfiguration LoadFromExcel(string filePath)`

**使用ライブラリ**: EPPlus（LicenseContext.NonCommercial）

**処理フロー**:
```
1. EPPlusでExcelファイルをオープン
2. "settings"シート存在確認
3. "データ収集デバイス"シート存在確認
4. 各シートから設定情報読み込み
5. PlcConfigurationオブジェクト生成
6. 返却
```

**エラーハンドリング**:
- シートが見つからない場合 → ArgumentException（ファイル名とシート名を明示）
- セルが空の場合 → ArgumentException（セル位置を明示）
- 読み込み失敗時 → ログ出力してthrow

### 4. "settings"シート読み込み

**実装クラス**: ConfigurationLoader（privateヘルパー使用）

**メソッド**: `private T ReadCell<T>(ExcelWorksheet sheet, string cellAddress, string itemName)`

**読み込み項目**（5項目固定）:

| セル | 項目名               | 型     | 検証内容                      |
|------|---------------------|--------|------------------------------|
| B8   | PLCのIPアドレス      | string | IPAddress.TryParse()         |
| B9   | PLCのポート          | int    | 1～65535                     |
| B11  | データ取得周期(ms)   | int    | 1～86400000（24時間）        |
| B12  | デバイス名           | string | 非空文字列                   |
| B13  | データ保存先パス     | string | パス形式チェック             |

**実装例**:
```csharp
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
```

### 5. "データ収集デバイス"シート読み込み（基礎）

**実装クラス**: ConfigurationLoader（privateヘルパー）

**メソッド**: `private List<DeviceSpecification> ReadDevices(ExcelWorksheet sheet, string sourceFile)`

**シート構造**:
- 1行目: ヘッダ行（スキップ）
- 2行目以降: データ行（A列が空白になるまで読み込み）

**列構成**:
| 列 | 項目名         | 型     | 必須 |
|----|---------------|--------|------|
| A  | 項目名         | string | ○    |
| B  | デバイスコード | string | ○    |
| C  | デバイス番号   | int    | ○    |
| D  | 桁数          | int    | ○    |
| E  | 単位          | string | ○    |

**処理フロー**:
```
1. row = 2（2行目から開始）
2. A列の値を読み込み
3. A列が空白 → ループ終了
4. B～E列の値を読み込み
5. DeviceSpecificationオブジェクト生成（Phase3で正規化）
6. リストに追加
7. row++してループ継続
8. デバイス数が0の場合 → ArgumentException
```

**Phase2での実装範囲**:
- Excelからの値読み込みのみ
- 正規化処理（デバイスコード変換等）はPhase3で実装

**実装例**:
```csharp
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

            // Phase2: とりあえず読み込みのみ（Phase3で正規化）
            var device = new DeviceSpecification
            {
                ItemName = itemName,
                DeviceType = deviceCode,
                DeviceNumber = deviceNumber,
                Digits = digits,
                Unit = unit
            };

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
```

### 6. 複数設定ファイル一括読み込み

**実装クラス**: ConfigurationLoader

**メソッド**: `public List<PlcConfiguration> LoadAllPlcConnectionConfigs()`

**処理フロー**:
```
1. DiscoverExcelFiles()で全.xlsxファイルを検索
2. 各ファイルに対してLoadFromExcel()を実行
3. 成功した設定をリストに追加
4. エラー発生時はログ出力してthrow
5. 全設定のリストを返却
```

**実装例**:
```csharp
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
```

---

## ConfigurationLoaderクラスの全体構造（Phase2時点）

**ファイルパス**: `andon/Infrastructure/Configuration/ConfigurationLoader.cs`

```csharp
using OfficeOpenXml;
using Microsoft.Extensions.Logging;

namespace Andon.Infrastructure.Configuration
{
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
            // 実装内容は上記参照
        }

        /// <summary>
        /// 実行フォルダ内の全.xlsxファイルを検索（privateヘルパー）
        /// </summary>
        private List<string> DiscoverExcelFiles()
        {
            // 実装内容は上記参照
        }

        /// <summary>
        /// Excelファイルから設定を読み込み（privateヘルパー）
        /// </summary>
        private PlcConfiguration LoadFromExcel(string filePath)
        {
            // 実装内容は上記参照
        }

        /// <summary>
        /// セル読み込みヘルパー（privateヘルパー）
        /// </summary>
        private T ReadCell<T>(ExcelWorksheet sheet, string cellAddress, string itemName)
        {
            // 実装内容は上記参照
        }

        /// <summary>
        /// デバイス情報読み込み（privateヘルパー）
        /// </summary>
        private List<DeviceSpecification> ReadDevices(
            ExcelWorksheet sheet, string sourceFile)
        {
            // 実装内容は上記参照（Phase2は正規化なし）
        }

        /// <summary>
        /// ファイルロック確認（privateヘルパー）
        /// </summary>
        private static bool IsFileLocked(string filePath)
        {
            // 実装内容は上記参照
        }
    }
}
```

---

## EPPlusライブラリの導入

### NuGetパッケージ追加

```bash
dotnet add package EPPlus --version 6.2.0
```

### ライセンス設定

```csharp
// ConfigurationLoaderのコンストラクタまたはLoadFromExcel()の最初で設定
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
```

**注意**: 商用利用の場合はライセンス購入が必要

---

## Phase2の成功条件

- ✅ 実行フォルダ内の全.xlsxファイルを検出できること
- ✅ 一時ファイル（~$*.xlsx）を除外できること
- ✅ ロックされているファイルを除外できること
- ✅ .xlsxファイルが見つからない場合にエラーを返すこと
- ✅ Excelファイルを正常にオープンできること
- ✅ "settings"シートの5項目を正確に読み込めること
- ✅ "データ収集デバイス"シートの全行を読み込めること
- ✅ 複数のExcelファイルを同時に読み込めること
- ✅ エラー発生時に適切なメッセージを表示すること

---

## Phase2のテスト計画

### Excelファイル検索のテスト

1. **正常系**
   - 実行フォルダに.xlsxファイルが1つある場合
   - 実行フォルダに.xlsxファイルが複数ある場合

2. **除外処理テスト**
   - ~$で始まるファイルが除外されること
   - ロックされているファイルが除外されること

3. **異常系**
   - .xlsxファイルが1つも存在しない場合 → ArgumentException

### Excel読み込みのテスト

1. **正常系**
   - 正しいフォーマットのExcelファイルを読み込めること
   - 複数のExcelファイルを連続で読み込めること

2. **シート存在確認テスト**
   - "settings"シートが存在しない場合 → ArgumentException
   - "データ収集デバイス"シートが存在しない場合 → ArgumentException

3. **セル読み込みテスト**
   - 各セルから正しい型の値を読み込めること
   - 空セルの場合 → ArgumentException
   - 型変換エラーの場合 → ArgumentException

### デバイス情報読み込みのテスト

1. **正常系**
   - 複数行のデバイス情報を読み込めること
   - A列が空白になった時点で読み込みを終了すること

2. **異常系**
   - デバイスが1つも設定されていない場合 → ArgumentException
   - セルの値が不正な場合 → ArgumentException（行番号を明示）

---

## テストデータ準備

### テスト用Excelファイル作成

**ファイル名**: `TestUtilities/TestData/SampleConfigurations/valid_config.xlsx`

**"settings"シート**:
| セル | 値              |
|------|----------------|
| B8   | 172.30.40.15   |
| B9   | 8192           |
| B11  | 1000           |
| B12  | テスト用PLC    |
| B13  | C:\data\output |

**"データ収集デバイス"シート**:
| A    | B  | C     | D | E    |
|------|----|-------|---|------|
| 項目名 | デバイスコード | デバイス番号 | 桁数 | 単位 |
| 温度1 | D  | 60000 | 1 | word |
| 温度2 | D  | 60075 | 1 | word |
| 圧力  | D  | 60082 | 1 | word |

### エラーケース用Excelファイル

1. `missing_settings_sheet.xlsx` - "settings"シートなし
2. `missing_devices_sheet.xlsx` - "データ収集デバイス"シートなし
3. `empty_cells.xlsx` - 必須セルが空
4. `no_devices.xlsx` - デバイスが0件

---

## Phase2の実装手順

1. **EPPlusライブラリ導入**
   - NuGetパッケージ追加
   - ライセンス設定確認

2. **ConfigurationLoaderクラス作成**
   - ファイル作成: `andon/Infrastructure/Configuration/ConfigurationLoader.cs`
   - コンストラクタ実装（ILogger注入）

3. **DiscoverExcelFiles()実装**
   - ファイル検索ロジック
   - 除外処理（一時ファイル、ロックファイル）

4. **IsFileLocked()実装**
   - ファイルロック確認ロジック

5. **ReadCell<T>()実装**
   - ジェネリック型でのセル読み込み
   - エラーハンドリング

6. **LoadFromExcel()実装**
   - Excelファイルオープン
   - シート存在確認
   - "settings"シート読み込み（ReadCell使用）
   - "データ収集デバイス"シート読み込み（ReadDevices呼び出し）

7. **ReadDevices()実装**
   - ループ処理でデバイス情報読み込み
   - DeviceSpecificationオブジェクト生成（正規化なし）

8. **LoadAllPlcConnectionConfigs()実装**
   - 複数ファイル一括読み込みロジック
   - エラーハンドリング

9. **テスト用Excelファイル作成**
   - 正常系テストデータ
   - 異常系テストデータ

10. **単体テスト作成**
    - `Tests/Unit/Infrastructure/Configuration/ConfigurationLoaderTests.cs`
    - 各メソッドのテストケース実装

11. **テスト実行・検証**
    - 全テストがパスすることを確認

---

## Phase2完了後の状態

- Excelファイルから基本的な設定情報を読み込める
- 複数のExcelファイルを同時に処理できる
- 適切なエラーハンドリングが実装されている
- Phase3（デバイス情報変換と正規化）の実装に進む準備が完了

---

## 次のPhase

**Phase3: デバイス情報変換と正規化**
- NormalizeDevice()実装
- ConvertDeviceNumberToBytes()実装
- デバイスコード変換処理
- 10進/16進デバイス変換処理
