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
        try
        {
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
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
