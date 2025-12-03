using OfficeOpenXml;

namespace Andon.Tests.TestUtilities.TestData.SampleConfigurations;

/// <summary>
/// テスト用Excelファイルを生成するヘルパークラス
/// </summary>
public static class TestExcelFileCreator
{
    /// <summary>
    /// 有効な設定ファイルを作成
    /// </summary>
    public static void CreateValidConfigFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "settings"シート作成
        var settingsSheet = package.Workbook.Worksheets.Add("settings");

        // ヘッダー設定
        settingsSheet.Cells["A8"].Value = "PLCのIPアドレス";
        settingsSheet.Cells["B8"].Value = "172.30.40.15";

        settingsSheet.Cells["A9"].Value = "PLCのポート";
        settingsSheet.Cells["B9"].Value = 8192;

        settingsSheet.Cells["A11"].Value = "データ取得周期(ms)";
        settingsSheet.Cells["B11"].Value = 1;

        settingsSheet.Cells["A12"].Value = "デバイス名";
        settingsSheet.Cells["B12"].Value = "テスト用PLC";

        settingsSheet.Cells["A13"].Value = "データ保存先パス";
        settingsSheet.Cells["B13"].Value = @"C:\data\output";

        // "データ収集デバイス"シート作成
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");

        // ヘッダー行
        devicesSheet.Cells["A1"].Value = "項目名";
        devicesSheet.Cells["B1"].Value = "デバイスコード";
        devicesSheet.Cells["C1"].Value = "デバイス番号";
        devicesSheet.Cells["D1"].Value = "桁数";
        devicesSheet.Cells["E1"].Value = "単位";

        // データ行
        devicesSheet.Cells["A2"].Value = "温度1";
        devicesSheet.Cells["B2"].Value = "D";
        devicesSheet.Cells["C2"].Value = 60000;
        devicesSheet.Cells["D2"].Value = 1;
        devicesSheet.Cells["E2"].Value = "word";

        devicesSheet.Cells["A3"].Value = "温度2";
        devicesSheet.Cells["B3"].Value = "D";
        devicesSheet.Cells["C3"].Value = 60075;
        devicesSheet.Cells["D3"].Value = 1;
        devicesSheet.Cells["E3"].Value = "word";

        devicesSheet.Cells["A4"].Value = "圧力";
        devicesSheet.Cells["B4"].Value = "D";
        devicesSheet.Cells["C4"].Value = 60082;
        devicesSheet.Cells["D4"].Value = 1;
        devicesSheet.Cells["E4"].Value = "word";

        // ファイル保存
        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    /// <summary>
    /// settingsシートが欠落したファイルを作成
    /// </summary>
    public static void CreateMissingSettingsSheetFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "データ収集デバイス"シートのみ作成
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");
        devicesSheet.Cells["A1"].Value = "項目名";
        devicesSheet.Cells["A2"].Value = "テストデバイス";

        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    /// <summary>
    /// データ収集デバイスシートが欠落したファイルを作成
    /// </summary>
    public static void CreateMissingDevicesSheetFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "settings"シートのみ作成
        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "172.30.40.15";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = "テスト用PLC";
        settingsSheet.Cells["B13"].Value = @"C:\data\output";

        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    /// <summary>
    /// 空セルを含むファイルを作成
    /// </summary>
    public static void CreateEmptyCellsFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "settings"シート（B8が空）
        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = ""; // 空文字
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = "テスト用PLC";
        settingsSheet.Cells["B13"].Value = @"C:\data\output";

        // "データ収集デバイス"シート
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");
        devicesSheet.Cells["A1"].Value = "項目名";

        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    /// <summary>
    /// デバイスが0件のファイルを作成
    /// </summary>
    public static void CreateNoDevicesFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "settings"シート
        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "172.30.40.15";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = "テスト用PLC";
        settingsSheet.Cells["B13"].Value = @"C:\data\output";

        // "データ収集デバイス"シート（ヘッダーのみ、データなし）
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");
        devicesSheet.Cells["A1"].Value = "項目名";
        devicesSheet.Cells["B1"].Value = "デバイスコード";
        devicesSheet.Cells["C1"].Value = "デバイス番号";
        devicesSheet.Cells["D1"].Value = "桁数";
        devicesSheet.Cells["E1"].Value = "単位";
        // A2以降は空（デバイスなし）

        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    // ========== Phase3: デバイス正規化テスト用ファイル ==========

    /// <summary>
    /// 10進ビットデバイス(M)を含むファイルを作成
    /// </summary>
    public static void CreateBitDeviceFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "settings"シート
        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "172.30.40.15";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = "テスト用PLC";
        settingsSheet.Cells["B13"].Value = @"C:\data\output";

        // "データ収集デバイス"シート
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");
        devicesSheet.Cells["A1"].Value = "項目名";
        devicesSheet.Cells["B1"].Value = "デバイスコード";
        devicesSheet.Cells["C1"].Value = "デバイス番号";
        devicesSheet.Cells["D1"].Value = "桁数";
        devicesSheet.Cells["E1"].Value = "単位";

        // M32, bit
        devicesSheet.Cells["A2"].Value = "状態";
        devicesSheet.Cells["B2"].Value = "M";
        devicesSheet.Cells["C2"].Value = 32;
        devicesSheet.Cells["D2"].Value = 1;
        devicesSheet.Cells["E2"].Value = "bit";

        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    /// <summary>
    /// 16進ビットデバイス(X)を含むファイルを作成
    /// </summary>
    public static void CreateHexDeviceFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "settings"シート
        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "172.30.40.15";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = "テスト用PLC";
        settingsSheet.Cells["B13"].Value = @"C:\data\output";

        // "データ収集デバイス"シート
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");
        devicesSheet.Cells["A1"].Value = "項目名";
        devicesSheet.Cells["B1"].Value = "デバイスコード";
        devicesSheet.Cells["C1"].Value = "デバイス番号";
        devicesSheet.Cells["D1"].Value = "桁数";
        devicesSheet.Cells["E1"].Value = "単位";

        // X1760, bit (Excelには10進数で記載)
        devicesSheet.Cells["A2"].Value = "入力";
        devicesSheet.Cells["B2"].Value = "X";
        devicesSheet.Cells["C2"].Value = 1760;
        devicesSheet.Cells["D2"].Value = 1;
        devicesSheet.Cells["E2"].Value = "bit";

        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    /// <summary>
    /// 大文字小文字混在のデバイスタイプを含むファイルを作成
    /// </summary>
    public static void CreateMixedCaseFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "settings"シート
        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "172.30.40.15";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = "テスト用PLC";
        settingsSheet.Cells["B13"].Value = @"C:\data\output";

        // "データ収集デバイス"シート
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");
        devicesSheet.Cells["A1"].Value = "項目名";
        devicesSheet.Cells["B1"].Value = "デバイスコード";
        devicesSheet.Cells["C1"].Value = "デバイス番号";
        devicesSheet.Cells["D1"].Value = "桁数";
        devicesSheet.Cells["E1"].Value = "単位";

        // "d" (小文字)
        devicesSheet.Cells["A2"].Value = "温度";
        devicesSheet.Cells["B2"].Value = "d";
        devicesSheet.Cells["C2"].Value = 100;
        devicesSheet.Cells["D2"].Value = 1;
        devicesSheet.Cells["E2"].Value = "word";

        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    /// <summary>
    /// 未対応デバイスタイプ(ZZ)を含むファイルを作成
    /// </summary>
    public static void CreateInvalidDeviceTypeFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "settings"シート
        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "172.30.40.15";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = "テスト用PLC";
        settingsSheet.Cells["B13"].Value = @"C:\data\output";

        // "データ収集デバイス"シート
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");
        devicesSheet.Cells["A1"].Value = "項目名";
        devicesSheet.Cells["B1"].Value = "デバイスコード";
        devicesSheet.Cells["C1"].Value = "デバイス番号";
        devicesSheet.Cells["D1"].Value = "桁数";
        devicesSheet.Cells["E1"].Value = "単位";

        // 未対応デバイスタイプ "ZZ"
        devicesSheet.Cells["A2"].Value = "テスト";
        devicesSheet.Cells["B2"].Value = "ZZ";
        devicesSheet.Cells["C2"].Value = 100;
        devicesSheet.Cells["D2"].Value = 1;
        devicesSheet.Cells["E2"].Value = "word";

        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    /// <summary>
    /// 未対応単位(byte)を含むファイルを作成
    /// </summary>
    public static void CreateInvalidUnitFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "settings"シート
        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "172.30.40.15";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = "テスト用PLC";
        settingsSheet.Cells["B13"].Value = @"C:\data\output";

        // "データ収集デバイス"シート
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");
        devicesSheet.Cells["A1"].Value = "項目名";
        devicesSheet.Cells["B1"].Value = "デバイスコード";
        devicesSheet.Cells["C1"].Value = "デバイス番号";
        devicesSheet.Cells["D1"].Value = "桁数";
        devicesSheet.Cells["E1"].Value = "単位";

        // 未対応単位 "byte"
        devicesSheet.Cells["A2"].Value = "テスト";
        devicesSheet.Cells["B2"].Value = "D";
        devicesSheet.Cells["C2"].Value = 100;
        devicesSheet.Cells["D2"].Value = 1;
        devicesSheet.Cells["E2"].Value = "byte";

        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    /// <summary>
    /// 24種類全デバイスタイプを含むファイルを作成
    /// </summary>
    public static void CreateAllDeviceTypesFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "settings"シート
        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "172.30.40.15";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = "テスト用PLC";
        settingsSheet.Cells["B13"].Value = @"C:\data\output";

        // "データ収集デバイス"シート
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");
        devicesSheet.Cells["A1"].Value = "項目名";
        devicesSheet.Cells["B1"].Value = "デバイスコード";
        devicesSheet.Cells["C1"].Value = "デバイス番号";
        devicesSheet.Cells["D1"].Value = "桁数";
        devicesSheet.Cells["E1"].Value = "単位";

        // 24種類のデバイスタイプ
        string[] deviceTypes = { "SM", "M", "L", "F", "V", "TS", "TC", "STS", "STC", "CS", "CC",
                                  "X", "Y", "B", "SB", "DX", "DY",
                                  "SD", "D", "W", "SW", "TN", "STN", "CN" };

        for (int i = 0; i < deviceTypes.Length; i++)
        {
            int row = i + 2;
            devicesSheet.Cells[$"A{row}"].Value = $"デバイス{i + 1}";
            devicesSheet.Cells[$"B{row}"].Value = deviceTypes[i];
            devicesSheet.Cells[$"C{row}"].Value = 100 + i;
            devicesSheet.Cells[$"D{row}"].Value = 1;
            devicesSheet.Cells[$"E{row}"].Value = "word";
        }

        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    // ========== Phase4: 設定検証テスト用ファイル ==========

    /// <summary>
    /// 不正なIPアドレスを含むファイルを作成
    /// </summary>
    public static void CreateInvalidIpAddressFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "settings"シート
        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "invalid-ip-address"; // 不正なIPアドレス
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = "テスト用PLC";
        settingsSheet.Cells["B13"].Value = @"C:\data\output";

        // "データ収集デバイス"シート
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");
        devicesSheet.Cells["A1"].Value = "項目名";
        devicesSheet.Cells["B1"].Value = "デバイスコード";
        devicesSheet.Cells["C1"].Value = "デバイス番号";
        devicesSheet.Cells["D1"].Value = "桁数";
        devicesSheet.Cells["E1"].Value = "単位";

        devicesSheet.Cells["A2"].Value = "温度";
        devicesSheet.Cells["B2"].Value = "D";
        devicesSheet.Cells["C2"].Value = 100;
        devicesSheet.Cells["D2"].Value = 1;
        devicesSheet.Cells["E2"].Value = "word";

        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    /// <summary>
    /// 範囲外のポート番号を含むファイルを作成（下限）
    /// </summary>
    public static void CreateInvalidPortLowFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "settings"シート
        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "172.30.40.15";
        settingsSheet.Cells["B9"].Value = 0; // 範囲外（下限）
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = "テスト用PLC";
        settingsSheet.Cells["B13"].Value = @"C:\data\output";

        // "データ収集デバイス"シート
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");
        devicesSheet.Cells["A1"].Value = "項目名";
        devicesSheet.Cells["B1"].Value = "デバイスコード";
        devicesSheet.Cells["C1"].Value = "デバイス番号";
        devicesSheet.Cells["D1"].Value = "桁数";
        devicesSheet.Cells["E1"].Value = "単位";

        devicesSheet.Cells["A2"].Value = "温度";
        devicesSheet.Cells["B2"].Value = "D";
        devicesSheet.Cells["C2"].Value = 100;
        devicesSheet.Cells["D2"].Value = 1;
        devicesSheet.Cells["E2"].Value = "word";

        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    /// <summary>
    /// 範囲外のポート番号を含むファイルを作成（上限）
    /// </summary>
    public static void CreateInvalidPortHighFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "settings"シート
        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "172.30.40.15";
        settingsSheet.Cells["B9"].Value = 70000; // 範囲外（上限）
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = "テスト用PLC";
        settingsSheet.Cells["B13"].Value = @"C:\data\output";

        // "データ収集デバイス"シート
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");
        devicesSheet.Cells["A1"].Value = "項目名";
        devicesSheet.Cells["B1"].Value = "デバイスコード";
        devicesSheet.Cells["C1"].Value = "デバイス番号";
        devicesSheet.Cells["D1"].Value = "桁数";
        devicesSheet.Cells["E1"].Value = "単位";

        devicesSheet.Cells["A2"].Value = "温度";
        devicesSheet.Cells["B2"].Value = "D";
        devicesSheet.Cells["C2"].Value = 100;
        devicesSheet.Cells["D2"].Value = 1;
        devicesSheet.Cells["E2"].Value = "word";

        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    /// <summary>
    /// 範囲外のデータ取得周期を含むファイルを作成（下限）
    /// </summary>
    public static void CreateInvalidFrequencyLowFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "settings"シート
        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "172.30.40.15";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B11"].Value = 0; // 範囲外（下限）
        settingsSheet.Cells["B12"].Value = "テスト用PLC";
        settingsSheet.Cells["B13"].Value = @"C:\data\output";

        // "データ収集デバイス"シート
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");
        devicesSheet.Cells["A1"].Value = "項目名";
        devicesSheet.Cells["B1"].Value = "デバイスコード";
        devicesSheet.Cells["C1"].Value = "デバイス番号";
        devicesSheet.Cells["D1"].Value = "桁数";
        devicesSheet.Cells["E1"].Value = "単位";

        devicesSheet.Cells["A2"].Value = "温度";
        devicesSheet.Cells["B2"].Value = "D";
        devicesSheet.Cells["C2"].Value = 100;
        devicesSheet.Cells["D2"].Value = 1;
        devicesSheet.Cells["E2"].Value = "word";

        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    /// <summary>
    /// 範囲外のデータ取得周期を含むファイルを作成（上限）
    /// </summary>
    public static void CreateInvalidFrequencyHighFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "settings"シート
        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "172.30.40.15";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B11"].Value = 90000000; // 範囲外（上限）
        settingsSheet.Cells["B12"].Value = "テスト用PLC";
        settingsSheet.Cells["B13"].Value = @"C:\data\output";

        // "データ収集デバイス"シート
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");
        devicesSheet.Cells["A1"].Value = "項目名";
        devicesSheet.Cells["B1"].Value = "デバイスコード";
        devicesSheet.Cells["C1"].Value = "デバイス番号";
        devicesSheet.Cells["D1"].Value = "桁数";
        devicesSheet.Cells["E1"].Value = "単位";

        devicesSheet.Cells["A2"].Value = "温度";
        devicesSheet.Cells["B2"].Value = "D";
        devicesSheet.Cells["C2"].Value = 100;
        devicesSheet.Cells["D2"].Value = 1;
        devicesSheet.Cells["E2"].Value = "word";

        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    /// <summary>
    /// 範囲外のデバイス番号を含むファイルを作成
    /// </summary>
    public static void CreateInvalidDeviceNumberFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "settings"シート
        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "172.30.40.15";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = "テスト用PLC";
        settingsSheet.Cells["B13"].Value = @"C:\data\output";

        // "データ収集デバイス"シート
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");
        devicesSheet.Cells["A1"].Value = "項目名";
        devicesSheet.Cells["B1"].Value = "デバイスコード";
        devicesSheet.Cells["C1"].Value = "デバイス番号";
        devicesSheet.Cells["D1"].Value = "桁数";
        devicesSheet.Cells["E1"].Value = "単位";

        devicesSheet.Cells["A2"].Value = "温度";
        devicesSheet.Cells["B2"].Value = "D";
        devicesSheet.Cells["C2"].Value = 20000000; // 範囲外（3バイト最大値超過）
        devicesSheet.Cells["D2"].Value = 1;
        devicesSheet.Cells["E2"].Value = "word";

        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    /// <summary>
    /// 総点数が255点を超えるファイルを作成
    /// </summary>
    public static void CreateExceedTotalPointsFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "settings"シート
        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "172.30.40.15";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = "テスト用PLC";
        settingsSheet.Cells["B13"].Value = @"C:\data\output";

        // "データ収集デバイス"シート（300点のワードデバイス）
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");
        devicesSheet.Cells["A1"].Value = "項目名";
        devicesSheet.Cells["B1"].Value = "デバイスコード";
        devicesSheet.Cells["C1"].Value = "デバイス番号";
        devicesSheet.Cells["D1"].Value = "桁数";
        devicesSheet.Cells["E1"].Value = "単位";

        for (int i = 0; i < 300; i++)
        {
            int row = i + 2;
            devicesSheet.Cells[$"A{row}"].Value = $"デバイス{i}";
            devicesSheet.Cells[$"B{row}"].Value = "D";
            devicesSheet.Cells[$"C{row}"].Value = 1000 + i;
            devicesSheet.Cells[$"D{row}"].Value = 1;
            devicesSheet.Cells[$"E{row}"].Value = "word";
        }

        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    /// <summary>
    /// 空の保存先パスを含むファイルを作成
    /// </summary>
    public static void CreateEmptySavePathFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "settings"シート
        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "172.30.40.15";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = "テスト用PLC";
        settingsSheet.Cells["B13"].Value = ""; // 空の保存先パス

        // "データ収集デバイス"シート
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");
        devicesSheet.Cells["A1"].Value = "項目名";
        devicesSheet.Cells["B1"].Value = "デバイスコード";
        devicesSheet.Cells["C1"].Value = "デバイス番号";
        devicesSheet.Cells["D1"].Value = "桁数";
        devicesSheet.Cells["E1"].Value = "単位";

        devicesSheet.Cells["A2"].Value = "温度";
        devicesSheet.Cells["B2"].Value = "D";
        devicesSheet.Cells["C2"].Value = 100;
        devicesSheet.Cells["D2"].Value = 1;
        devicesSheet.Cells["E2"].Value = "word";

        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    /// <summary>
    /// 不正なパス形式を含むファイルを作成
    /// </summary>
    public static void CreateInvalidPathFormatFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "settings"シート
        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "172.30.40.15";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = "テスト用PLC";
        settingsSheet.Cells["B13"].Value = @"C:\invalid<path>\data"; // 不正なパス形式

        // "データ収集デバイス"シート
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");
        devicesSheet.Cells["A1"].Value = "項目名";
        devicesSheet.Cells["B1"].Value = "デバイスコード";
        devicesSheet.Cells["C1"].Value = "デバイス番号";
        devicesSheet.Cells["D1"].Value = "桁数";
        devicesSheet.Cells["E1"].Value = "単位";

        devicesSheet.Cells["A2"].Value = "温度";
        devicesSheet.Cells["B2"].Value = "D";
        devicesSheet.Cells["C2"].Value = 100;
        devicesSheet.Cells["D2"].Value = 1;
        devicesSheet.Cells["E2"].Value = "word";

        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    /// <summary>
    /// 空のデバイス名を含むファイルを作成
    /// </summary>
    public static void CreateEmptyPlcModelFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // "settings"シート
        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "172.30.40.15";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = ""; // 空のデバイス名
        settingsSheet.Cells["B13"].Value = @"C:\data\output";

        // "データ収集デバイス"シート
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");
        devicesSheet.Cells["A1"].Value = "項目名";
        devicesSheet.Cells["B1"].Value = "デバイスコード";
        devicesSheet.Cells["C1"].Value = "デバイス番号";
        devicesSheet.Cells["D1"].Value = "桁数";
        devicesSheet.Cells["E1"].Value = "単位";

        devicesSheet.Cells["A2"].Value = "温度";
        devicesSheet.Cells["B2"].Value = "D";
        devicesSheet.Cells["C2"].Value = 100;
        devicesSheet.Cells["D2"].Value = 1;
        devicesSheet.Cells["E2"].Value = "word";

        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Phase2: DefaultValues使用テスト用ファイル作成メソッド
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// ConnectionMethodが空のファイルを作成（既定値UDP使用テスト用）
    /// </summary>
    public static void CreatePhase2EmptyConnectionMethodFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();

        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "192.168.1.10";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B10"].Value = ""; // 空 → 既定値UDP
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = "TestPLC";
        settingsSheet.Cells["B13"].Value = @"C:\data";

        CreateMinimalDevicesSheet(package);
        package.SaveAs(new FileInfo(filePath));
    }

    /// <summary>
    /// FrameVersionが空のファイルを作成（既定値4E使用テスト用）
    /// </summary>
    public static void CreatePhase2EmptyFrameVersionFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();

        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "192.168.1.10";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B10"].Value = "UDP";
        settingsSheet.Cells["B11"].Value = 1; // B11は必須（DataReadingFrequency用）
        settingsSheet.Cells["B12"].Value = "TestPLC"; // B12は必須（PlcModel用）
        settingsSheet.Cells["B13"].Value = @"C:\data"; // B13は必須（SavePath用）
        // FrameVersionは将来的な拡張用（現在は内部で既定値4Eが設定される）

        CreateMinimalDevicesSheet(package);
        package.SaveAs(new FileInfo(filePath));
    }

    /// <summary>
    /// Timeoutが空のファイルを作成（既定値1000ms使用テスト用）
    /// </summary>
    public static void CreatePhase2EmptyTimeoutFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();

        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "192.168.1.10";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B10"].Value = "UDP";
        settingsSheet.Cells["B11"].Value = 1; // B11は必須（DataReadingFrequency用）
        settingsSheet.Cells["B12"].Value = "TestPLC"; // B12は必須（PlcModel用）
        settingsSheet.Cells["B13"].Value = @"C:\data"; // B13は必須（SavePath用）
        // Timeoutは将来的な拡張用（現在は内部で既定値1000msが設定される）

        CreateMinimalDevicesSheet(package);
        package.SaveAs(new FileInfo(filePath));
    }

    /// <summary>
    /// IsBinaryが空のファイルを作成（既定値true使用テスト用）
    /// </summary>
    public static void CreatePhase2EmptyIsBinaryFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();

        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "192.168.1.10";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B10"].Value = "UDP";
        settingsSheet.Cells["B11"].Value = 1; // B11は必須（DataReadingFrequency用）
        settingsSheet.Cells["B12"].Value = "TestPLC"; // B12は必須（PlcModel用）
        settingsSheet.Cells["B13"].Value = @"C:\data"; // B13は必須（SavePath用）
        // IsBinaryは将来的な拡張用（現在は内部で既定値trueが設定される）

        CreateMinimalDevicesSheet(package);
        package.SaveAs(new FileInfo(filePath));
    }

    /// <summary>
    /// MonitoringIntervalMsが空のファイルを作成（既定値1000ms使用テスト用）
    /// </summary>
    public static void CreatePhase2EmptyMonitoringIntervalFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();

        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "192.168.1.10";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B10"].Value = "UDP";
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = "TestPLC";
        settingsSheet.Cells["B13"].Value = @"C:\data";
        settingsSheet.Cells["B14"].Value = ""; // 空 → 既定値1000ms

        CreateMinimalDevicesSheet(package);
        package.SaveAs(new FileInfo(filePath));
    }

    /// <summary>
    /// PlcId自動生成テスト用ファイルを作成
    /// </summary>
    public static void CreatePhase2PlcIdGenerationFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();

        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "192.168.1.10";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B10"].Value = "UDP";
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = "TestPLC";
        settingsSheet.Cells["B13"].Value = @"C:\data";

        CreateMinimalDevicesSheet(package);
        package.SaveAs(new FileInfo(filePath));
    }

    /// <summary>
    /// PlcNameが空のファイルを作成（PlcId使用テスト用）
    /// </summary>
    public static void CreatePhase2EmptyPlcNameFile(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();

        var settingsSheet = package.Workbook.Worksheets.Add("settings");
        settingsSheet.Cells["B8"].Value = "192.168.1.10";
        settingsSheet.Cells["B9"].Value = 8192;
        settingsSheet.Cells["B10"].Value = "UDP";
        settingsSheet.Cells["B11"].Value = 1;
        settingsSheet.Cells["B12"].Value = "TestPLC";
        settingsSheet.Cells["B13"].Value = @"C:\data";
        settingsSheet.Cells["B14"].Value = 1000;
        settingsSheet.Cells["B15"].Value = ""; // 空 → PlcIdを使用

        CreateMinimalDevicesSheet(package);
        package.SaveAs(new FileInfo(filePath));
    }

    /// <summary>
    /// IsBinary=1のファイルを作成（true変換テスト用）
    /// </summary>
    public static void CreatePhase2IsBinary1File(string filePath)
{
    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    using var package = new ExcelPackage();

    var settingsSheet = package.Workbook.Worksheets.Add("settings");
    settingsSheet.Cells["B8"].Value = "192.168.1.10";
    settingsSheet.Cells["B9"].Value = 8192;
    settingsSheet.Cells["B10"].Value = "UDP";
    settingsSheet.Cells["B11"].Value = 1; // B11は必須（DataReadingFrequency用）
    settingsSheet.Cells["B12"].Value = "TestPLC"; // B12は必須（PlcModel用）
    settingsSheet.Cells["B13"].Value = @"C:\data"; // B13は必須（SavePath用）
    // IsBinary=1は将来的な拡張用（現在は内部で"1"→trueに変換される）

    CreateMinimalDevicesSheet(package);
    package.SaveAs(new FileInfo(filePath));
}

    /// <summary>
    /// IsBinary=0のファイルを作成（false変換テスト用）
    /// </summary>
    public static void CreatePhase2IsBinary0File(string filePath)
{
    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    using var package = new ExcelPackage();

    var settingsSheet = package.Workbook.Worksheets.Add("settings");
    settingsSheet.Cells["B8"].Value = "192.168.1.10";
    settingsSheet.Cells["B9"].Value = 8192;
    settingsSheet.Cells["B10"].Value = "UDP";
    settingsSheet.Cells["B11"].Value = 1; // B11は必須（DataReadingFrequency用）
    settingsSheet.Cells["B12"].Value = "TestPLC"; // B12は必須（PlcModel用）
    settingsSheet.Cells["B13"].Value = @"C:\data"; // B13は必須（SavePath用）
    // IsBinary=0は将来的な拡張用（現在は内部で"0"→falseに変換される）

    CreateMinimalDevicesSheet(package);
    package.SaveAs(new FileInfo(filePath));
}

    /// <summary>
    /// 最小限のデバイスシートを作成（Phase2テスト用ヘルパー）
    /// </summary>
    private static void CreateMinimalDevicesSheet(ExcelPackage package)
    {
        var devicesSheet = package.Workbook.Worksheets.Add("データ収集デバイス");
        devicesSheet.Cells["A1"].Value = "項目名";
        devicesSheet.Cells["B1"].Value = "デバイスコード";
        devicesSheet.Cells["C1"].Value = "デバイス番号";
        devicesSheet.Cells["D1"].Value = "桁数";
        devicesSheet.Cells["E1"].Value = "単位";

        devicesSheet.Cells["A2"].Value = "テスト";
        devicesSheet.Cells["B2"].Value = "D";
        devicesSheet.Cells["C2"].Value = 100;
        devicesSheet.Cells["D2"].Value = 1;
        devicesSheet.Cells["E2"].Value = "word";
    }
}
