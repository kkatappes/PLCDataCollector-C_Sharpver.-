namespace Andon.Core.Interfaces;

/// <summary>
/// ログ機能インターフェース
/// </summary>
public interface ILoggingManager
{
    Task LogInfo(string message);
    Task LogWarning(string message);
    Task LogError(Exception? ex, string message);
    Task LogDebug(string message);
    Task CloseAndFlushAsync();
}
