using OfficeOpenXml;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Models;
using Andon.Core.Managers;
using Andon.Core.Constants;

namespace Andon.Infrastructure.Configuration;

/// <summary>
/// Excelファイルから設定を読み込むクラス（Phase2～Phase5実装）
/// PlcConfigurationモデルを使用した統一設計
/// ⚠️ 注意: JSON設定読み込み機能は廃止（appsettings.json完全廃止により不要）
/// Phase 2-5: SettingsValidator統合完了（IPアドレス、ポート、MonitoringIntervalMs検証）
/// </summary>
public class ConfigurationLoaderExcel
{
    private readonly string _baseDirectory;
    private readonly MultiPlcConfigManager? _configManager;
    private readonly SettingsValidator _validator; // Phase 2-5: SettingsValidator統合

    /// <summary>
    /// コンストラクタ（Phase5: DI統合対応）
    /// </summary>
    /// <param name="baseDirectory">Excelファイルを検索するディレクトリ（省略時はAppContext.BaseDirectory）</param>
    /// <param name="configManager">MultiPlcConfigManager（省略可：自動登録が不要な場合）</param>
    public ConfigurationLoaderExcel(string? baseDirectory = null, MultiPlcConfigManager? configManager = null)
    {
        _baseDirectory = baseDirectory ?? AppContext.BaseDirectory;
        _configManager = configManager;
        _validator = new SettingsValidator(); // Phase 2-5: SettingsValidator初期化
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <summary>
    /// 複数のExcelファイルから設定を一括読み込み（Phase5: マネージャー自動登録対応）
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

                // Phase5: 読み込んだ設定をMultiPlcConfigManagerに自動登録
                _configManager?.AddConfiguration(config);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"設定ファイル読み込みエラー: {filePath}: {ex.Message}",
                    ex);
            }
        }

        return configs;
    }

    /// <summary>
    /// 実行フォルダ内の全.xlsxファイルを検索
    /// </summary>
    private List<string> DiscoverExcelFiles()
    {
        var xlsxFiles = Directory.GetFiles(_baseDirectory, "*.xlsx")
            .Where(f => !Path.GetFileName(f).StartsWith("~$")) // 一時ファイル除外
            .Where(f => !IsFileLocked(f)) // ロックファイル除外
            .ToList();

        if (!xlsxFiles.Any())
        {
            throw new ArgumentException(
                $"設定ファイル(.xlsx)が見つかりません: {_baseDirectory}");
        }

        return xlsxFiles;
    }

    /// <summary>
    /// Excelファイルから設定を読み込み
    /// </summary>
    private PlcConfiguration LoadFromExcel(string filePath)
    {
        using var package = new ExcelPackage(new FileInfo(filePath));

        // シート存在確認
        var settingsSheet = package.Workbook.Worksheets["settings"];
        if (settingsSheet == null)
        {
            throw new ArgumentException(
                $"\"settings\"シートが見つかりません: {filePath}");
        }

        var devicesSheet = package.Workbook.Worksheets["データ収集デバイス"];
        if (devicesSheet == null)
        {
            throw new ArgumentException(
                $"\"データ収集デバイス\"シートが見つかりません: {filePath}");
        }

        // 必須項目読み込み
        string ipAddress = ReadCell<string>(settingsSheet, "B8", "PLCのIPアドレス");
        int port = ReadCell<int>(settingsSheet, "B9", "PLCのポート");

        // Phase2: オプション項目読み込み（既定値使用）
        string connectionMethod = ReadOptionalCell<string>(settingsSheet, "B10", DefaultValues.ConnectionMethod);
        string? plcName = settingsSheet.Cells["B15"].Text;

        // PlcId自動生成
        string plcId = $"{ipAddress}_{port}";

        // 設定読み込み
        var config = new PlcConfiguration
        {
            IpAddress = ipAddress,
            Port = port,
            MonitoringIntervalMs = ReadCell<int>(settingsSheet, "B11", "データ取得周期(sec)") * 1000,
            PlcModel = ReadCell<string>(settingsSheet, "B12", "ターゲット名"),
            SavePath = ReadCell<string>(settingsSheet, "B13", "データ保存先パス"),
            // Phase2: 追加プロパティ
            ConnectionMethod = connectionMethod,
            // 将来的な拡張用: 既定値を使用
            FrameVersion = DefaultValues.FrameVersion,
            Timeout = DefaultValues.TimeoutMs,
            IsBinary = DefaultValues.IsBinary,
            PlcId = plcId,
            PlcName = plcName ?? string.Empty,
            SourceExcelFile = filePath,
            Devices = ReadDevices(devicesSheet, filePath)
        };

        // Phase4: 設定検証
        ValidateConfiguration(config);

        return config;
    }

    /// <summary>
    /// セル読み込みヘルパー
    /// </summary>
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
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new ArgumentException(
                $"{itemName}の読み込みに失敗しました（セル{cellAddress}）: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// オプションのセル値を読み込み、空の場合は既定値を返します（Phase2）
    /// </summary>
    private T ReadOptionalCell<T>(ExcelWorksheet sheet, string cellAddress, T defaultValue)
    {
        try
        {
            var cellText = sheet.Cells[cellAddress].Text;

            if (string.IsNullOrWhiteSpace(cellText))
            {
                return defaultValue;
            }

            // 型に応じた変換
            if (typeof(T) == typeof(int))
            {
                if (int.TryParse(cellText, out int intValue))
                {
                    return (T)(object)intValue;
                }
                return defaultValue;
            }
            else if (typeof(T) == typeof(bool))
            {
                // "1"/"0" または "true"/"false" をサポート
                if (cellText == "1" || cellText.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    return (T)(object)true;
                }
                if (cellText == "0" || cellText.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    return (T)(object)false;
                }
                return defaultValue;
            }
            else if (typeof(T) == typeof(string))
            {
                return (T)(object)cellText;
            }

            // その他の型はGetValue<T>()を使用
            var value = sheet.Cells[cellAddress].GetValue<T>();
            return value ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// デバイス情報読み込み
    /// </summary>
    private List<DeviceSpecification> ReadDevices(ExcelWorksheet sheet, string sourceFile)
    {
        var devices = new List<DeviceSpecification>();
        int row = 2; // 1行目はヘッダ、2行目からデータ

        while (true)
        {
            string? itemName = sheet.Cells[$"A{row}"].GetValue<string>();

            // A列が空になったら終了
            if (string.IsNullOrWhiteSpace(itemName))
                break;

            try
            {
                string deviceType = sheet.Cells[$"B{row}"].GetValue<string>() ?? "";

                // デバイス番号の読み込み（文字列または数値に対応）
                int deviceNumber = ParseDeviceNumber(sheet.Cells[$"C{row}"].Value, sourceFile, row);

                int digits = sheet.Cells[$"D{row}"].GetValue<int>();
                string unit = sheet.Cells[$"E{row}"].GetValue<string>() ?? "";

                // Phase3: デバイス情報正規化処理
                var device = NormalizeDevice(itemName, deviceType, deviceNumber, digits, unit);

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

        return devices;
    }

    /// <summary>
    /// デバイス番号をパース（数値または16進数文字列に対応）
    /// </summary>
    /// <param name="cellValue">セルの値（object型）</param>
    /// <param name="sourceFile">読み込み元ファイル名（エラーメッセージ用）</param>
    /// <param name="row">行番号（エラーメッセージ用）</param>
    /// <returns>パースされたデバイス番号</returns>
    /// <exception cref="FormatException">パースに失敗した場合</exception>
    private int ParseDeviceNumber(object? cellValue, string sourceFile, int row)
    {
        if (cellValue == null)
        {
            throw new FormatException(
                $"デバイス番号が空です（{sourceFile}、行{row}）");
        }

        // 数値型の場合はそのまま変換
        if (cellValue is double doubleVal)
        {
            return (int)doubleVal;
        }
        if (cellValue is int intVal)
        {
            return intVal;
        }

        // 文字列型の場合は10進数または16進数としてパース
        if (cellValue is string strVal)
        {
            strVal = strVal.Trim();

            // 10進数としてパース試行
            if (int.TryParse(strVal, out int decimalResult))
            {
                return decimalResult;
            }

            // 16進数としてパース試行（プレフィックス "0x" の有無に対応）
            strVal = strVal.Replace("0x", "").Replace("0X", "");
            if (int.TryParse(strVal, System.Globalization.NumberStyles.HexNumber, null, out int hexResult))
            {
                return hexResult;
            }

            throw new FormatException(
                $"デバイス番号の形式が不正です: '{strVal}'（{sourceFile}、行{row}）");
        }

        throw new FormatException(
            $"デバイス番号の型が不正です: {cellValue.GetType().Name}（{sourceFile}、行{row}）");
    }

    /// <summary>
    /// デバイス情報正規化処理（Phase3実装）
    /// </summary>
    /// <param name="itemName">項目名</param>
    /// <param name="deviceType">デバイスタイプ文字列（Excel B列）</param>
    /// <param name="deviceNumber">デバイス番号（Excel C列）</param>
    /// <param name="digits">桁数（Excel D列）</param>
    /// <param name="unit">単位（Excel E列）</param>
    /// <returns>正規化されたDeviceSpecification</returns>
    /// <exception cref="ArgumentException">未対応のデバイスタイプまたは単位</exception>
    private DeviceSpecification NormalizeDevice(
        string itemName,
        string deviceType,
        int deviceNumber,
        int digits,
        string unit)
    {
        // ① デバイスタイプ検証（大文字変換して検証）
        string deviceTypeUpper = deviceType.ToUpper();
        if (!Core.Constants.DeviceCodeMap.IsValidDeviceType(deviceTypeUpper))
        {
            throw new ArgumentException(
                $"未対応のデバイスタイプ: {deviceType}");
        }

        // ② 単位検証
        string unitLower = unit.ToLower();
        if (unitLower != "bit" && unitLower != "word" && unitLower != "dword")
        {
            throw new ArgumentException(
                $"未対応の単位: {unit}（\"bit\", \"word\", \"dword\"のいずれかを指定）");
        }

        // ③ DeviceCode取得とIsHexAddress判定
        byte deviceCodeByte = Core.Constants.DeviceCodeMap.GetDeviceCode(deviceTypeUpper);
        var deviceCode = (Core.Constants.DeviceCode)deviceCodeByte;
        bool isHex = Core.Constants.DeviceCodeMap.IsHexDevice(deviceTypeUpper);

        // ④ DeviceSpecificationオブジェクト生成
        var device = new DeviceSpecification(deviceCode, deviceNumber, isHexAddress: isHex)
        {
            ItemName = itemName,
            DeviceType = deviceTypeUpper,  // 大文字に正規化
            Digits = digits,
            Unit = unitLower  // 小文字に正規化
        };

        return device;
    }

    /// <summary>
    /// ファイルロック確認
    /// </summary>
    /// <summary>
    /// 設定の妥当性を検証（Phase4実装）
    /// Phase3のNormalizeDevice()で正規化済みの設定を検証
    /// </summary>
    /// <param name="config">検証対象の設定</param>
    /// <exception cref="ArgumentException">設定が不正な場合</exception>
    /// <exception cref="ArgumentOutOfRangeException">値が範囲外の場合</exception>
    private void ValidateConfiguration(PlcConfiguration config)
    {
        // ===== Phase 2-5: SettingsValidator統合 =====
        // 基本設定項目の検証（SettingsValidator使用）
        _validator.ValidateIpAddress(config.IpAddress);
        _validator.ValidatePort(config.Port);
        _validator.ValidateMonitoringIntervalMs(config.MonitoringIntervalMs);

        // 将来拡張: オプション項目の検証
        // _validator.ValidateTimeout(config.Timeout);
        // _validator.ValidateConnectionMethod(config.ConnectionMethod);
        // _validator.ValidateFrameVersion(config.FrameVersion);

        // ===== ConfigurationLoaderExcel固有の検証 =====

        // ③ デバイスリスト検証
        if (config.Devices == null || config.Devices.Count == 0)
        {
            throw new ArgumentException(
                $"デバイスが1つも設定されていません: {config.SourceExcelFile}");
        }

        foreach (var device in config.Devices)
        {
            // Phase3で既にデバイスタイプ・単位検証済みのため、
            // デバイス番号範囲のみ検証
            if (device.DeviceNumber < 0 || device.DeviceNumber > 0xFFFFFF)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(device.DeviceNumber),
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
    }

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
