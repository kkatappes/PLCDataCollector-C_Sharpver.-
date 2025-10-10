using System;
using System.IO;
using Microsoft.Extensions.Configuration;

class TestConfig
{
    static void Main()
    {
        Console.WriteLine("=== 設定値読み込みテスト ===");

        // 設定ファイル読み込み
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var plcSettings = config.GetSection("PlcConnection");

        // 安全な設定読み込み（GetConfigValueSafe相当）
        var port = GetConfigValueSafe(plcSettings, "Port", 8192);
        var useTcp = GetConfigValueSafe(plcSettings, "UseTcp", false);
        var isBinary = GetConfigValueSafe(plcSettings, "IsBinary", false);

        Console.WriteLine($"Port: {port} (期待値: 8192)");
        Console.WriteLine($"UseTcp: {useTcp} (期待値: False)");
        Console.WriteLine($"IsBinary: {isBinary} (期待値: False - ASCII通信)");

        // タイムアウト設定
        var timeoutSettings = config.GetSection("TimeoutSettings");
        var receiveTimeout = GetConfigValueSafe(timeoutSettings, "ReceiveTimeoutMs", 3000);
        var connectTimeout = GetConfigValueSafe(timeoutSettings, "ConnectTimeoutMs", 10000);

        Console.WriteLine($"ReceiveTimeoutMs: {receiveTimeout} (期待値: 3000)");
        Console.WriteLine($"ConnectTimeoutMs: {connectTimeout} (期待値: 10000)");

        Console.WriteLine("\n✅ 設定値テスト完了");
    }

    static T GetConfigValueSafe<T>(IConfigurationSection config, string key, T defaultValue)
    {
        var valueStr = config[key];
        if (string.IsNullOrEmpty(valueStr))
            return defaultValue;

        try
        {
            return (T)Convert.ChangeType(valueStr, typeof(T));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ 設定値変換エラー: {key} = '{valueStr}' -> {typeof(T).Name}, デフォルト値 {defaultValue} を使用: {ex.Message}");
            return defaultValue;
        }
    }
}