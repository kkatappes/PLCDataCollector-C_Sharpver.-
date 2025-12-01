using Andon.Core.Interfaces;
using Andon.Core.Models;

namespace Andon.Services;

/// <summary>
/// 階層的例外ハンドリング
/// </summary>
public class AsyncExceptionHandler : IAsyncExceptionHandler
{
    private readonly ILoggingManager _logger;
    private readonly IErrorHandler _errorHandler;

    public AsyncExceptionHandler(ILoggingManager logger, IErrorHandler errorHandler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
    }

    public async Task<AsyncOperationResult<T>> HandleCriticalOperationAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var result = new AsyncOperationResult<T>
        {
            StartTime = DateTime.Now
        };

        try
        {
            var data = await operation();
            result.IsSuccess = true;
            result.Data = data;
            result.EndTime = DateTime.Now;
            return result;
        }
        catch (OperationCanceledException ex)
        {
            result.IsSuccess = false;
            result.Exception = ex;
            result.EndTime = DateTime.Now;
            _logger.LogError(ex, $"Operation '{operationName}' was canceled");
            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.Exception = ex;
            result.EndTime = DateTime.Now;
            result.FailedStep = operationName;
            
            _logger.LogError(ex, $"Critical operation '{operationName}' failed: {ex.Message}");
            
            return result;
        }
    }

    public async Task<GeneralOperationResult> HandleGeneralOperationsAsync(
        IEnumerable<Func<Task>> operations,
        string groupName,
        CancellationToken cancellationToken = default)
    {
        var result = new GeneralOperationResult();
        var startTime = DateTime.Now;
        var operationList = operations.ToList();
        var operationIndex = 0;

        foreach (var operation in operationList)
        {
            try
            {
                await operation();
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.FailedOperations.Add($"Operation_{operationIndex}");
                result.Exceptions.Add(ex);
            }
            operationIndex++;
        }

        result.TotalExecutionTime = DateTime.Now - startTime;
        
        if (result.FailureCount > 0)
        {
            _logger.LogError(
                result.Exceptions.FirstOrDefault(),
                $"General operations group '{groupName}' completed with {result.FailureCount} failures");
        }

        return result;
    }
}
