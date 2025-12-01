using Microsoft.Extensions.Hosting;
using Andon.Core.Interfaces;

namespace Andon.Services;

/// <summary>
/// バックグラウンド実行サービス（継続実行モード）
/// </summary>
public class AndonHostedService : BackgroundService
{
    private readonly IApplicationController _controller;
    private readonly ILoggingManager _loggingManager;

    public AndonHostedService(
        IApplicationController controller,
        ILoggingManager loggingManager)
    {
        _controller = controller;
        _loggingManager = loggingManager;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await _loggingManager.LogInfo("AndonHostedService starting");
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _controller.StartAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // 正常なキャンセルは無視
        }
        catch (Exception ex)
        {
            await _loggingManager.LogError(ex, "ExecuteAsync failed");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _loggingManager.LogInfo("AndonHostedService stopping");
        await _controller.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
