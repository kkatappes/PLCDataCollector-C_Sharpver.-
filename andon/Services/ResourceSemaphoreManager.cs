using Andon.Core.Interfaces;
using Andon.Core.Models;

namespace Andon.Services;

/// <summary>
/// 共有リソース排他制御
/// </summary>
public class ResourceSemaphoreManager : IResourceSemaphoreManager
{
    public SemaphoreSlim LogFileSemaphore { get; }
    public SemaphoreSlim ConfigFileSemaphore { get; }
    public SemaphoreSlim OutputFileSemaphore { get; }

    public ResourceSemaphoreManager()
    {
        LogFileSemaphore = new SemaphoreSlim(1, 1);
        ConfigFileSemaphore = new SemaphoreSlim(3, 3);
        OutputFileSemaphore = new SemaphoreSlim(2, 2);
    }

    public async Task<T> ExecuteWithSemaphoreAsync<T>(
        SemaphoreSlim semaphore,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null)
    {
        var actualTimeout = timeout ?? TimeSpan.FromSeconds(30);
        
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(actualTimeout);
            
            await semaphore.WaitAsync(cts.Token);
            
            try
            {
                return await operation();
            }
            finally
            {
                semaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }

    public SemaphoreSlim GetResourceSemaphore(ResourceType resourceType)
    {
        return resourceType switch
        {
            ResourceType.LogFile => LogFileSemaphore,
            ResourceType.ConfigFile => ConfigFileSemaphore,
            ResourceType.OutputFile => OutputFileSemaphore,
            _ => throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, null)
        };
    }
}
