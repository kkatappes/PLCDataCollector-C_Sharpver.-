using Andon.Core.Interfaces;

namespace Andon.Services;

/// <summary>
/// キャンセレーション制御
/// </summary>
public class CancellationCoordinator : ICancellationCoordinator
{
    private readonly ILoggingManager _logger;

    public CancellationCoordinator(ILoggingManager logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public CancellationTokenSource CreateHierarchicalToken(
        CancellationToken parentToken,
        TimeSpan? timeout = null)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(parentToken);

        if (timeout.HasValue)
        {
            cts.CancelAfter(timeout.Value);
        }

        return cts;
    }

    public CancellationTokenRegistration RegisterCancellationCallback(
        CancellationToken token,
        Func<Task> callback,
        string callbackName)
    {
        return token.Register(() =>
        {
            try
            {
                // Execute async callback synchronously in cancellation context
                callback().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Cancellation callback '{callbackName}' failed: {ex.Message}");
            }
        });
    }
}
