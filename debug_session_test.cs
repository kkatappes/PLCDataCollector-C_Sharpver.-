using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SlmpClient.Core;

class Program
{
    static async Task Main(string[] args)
    {
        var testLogPath = Path.Combine(Path.GetTempPath(), $"debug_console_output_{Guid.NewGuid()}.log");
        var mockLogger = new Mock<ILogger<ConsoleOutputManager>>();

        var manager = new ConsoleOutputManager(mockLogger.Object, testLogPath);

        Console.WriteLine($"ログファイルパス: {testLogPath}");

        // テストエントリを作成
        await manager.WriteInfoAsync("デバッグテスト", "Debug");

        // 書き込み完了を待機
        await Task.Delay(200);

        // ファイル内容を確認
        if (File.Exists(testLogPath))
        {
            var content = await File.ReadAllTextAsync(testLogPath);
            Console.WriteLine("=== ファイル内容 ===");
            Console.WriteLine(content);
            Console.WriteLine("==================");

            // SessionIdの存在確認
            if (content.Contains("SessionId"))
            {
                Console.WriteLine("✅ SessionId が見つかりました");
            }
            else
            {
                Console.WriteLine("❌ SessionId が見つかりません");
            }
        }
        else
        {
            Console.WriteLine("❌ ログファイルが作成されませんでした");
        }

        // クリーンアップ
        await manager.DisposeAsync();
        if (File.Exists(testLogPath))
        {
            File.Delete(testLogPath);
        }
    }
}