using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Andon.Services;

namespace Andon;

/// <summary>
/// アプリケーションエントリーポイント・Host構築
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Phase 4-4 Green: GracefulShutdownHandler統合
        var shutdownCts = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("\nShutdown signal received (Ctrl+C)...");
            e.Cancel = true; // デフォルトの終了を防止
            shutdownCts.Cancel();
        };

        try
        {
            var host = CreateHostBuilder(args).Build();

            // HostedServiceとして実行
            var runTask = host.RunAsync(shutdownCts.Token);

            // シャットダウンシグナルを待機
            await runTask;

            // Phase 4-4 Green: GracefulShutdownHandlerを使用して終了処理
            var shutdownHandler = host.Services.GetRequiredService<Services.GracefulShutdownHandler>();
            var controller = host.Services.GetRequiredService<Core.Interfaces.IApplicationController>();

            var shutdownResult = await shutdownHandler.ExecuteGracefulShutdown(
                controller,
                TimeSpan.FromSeconds(30));

            if (!shutdownResult.Success)
            {
                Console.WriteLine($"Warning: Graceful shutdown completed with errors: {shutdownResult.ErrorMessage}");
            }

            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Application cancelled by user");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Application failed: {ex.Message}");
            return 1;
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                DependencyInjectionConfigurator.Configure(services, hostContext.Configuration);
                services.AddHostedService<AndonHostedService>();
            });
}
