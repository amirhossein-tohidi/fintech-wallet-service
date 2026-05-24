using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Wallet.Infrastructure.Resilience;

public sealed class CircuitBreakerState(
    IOptions<CircuitBreakerOptions> options,
    ILogger<CircuitBreakerState> logger)
{
    private readonly CircuitBreakerOptions _options = options.Value;
    private readonly object _syncRoot = new();
    private int _failureCount;
    private DateTime? _openedAt;

    public async Task ExecuteAsync(
        string dependencyName,
        Func<Task> operation,
        CancellationToken ct = default)
    {
        ThrowIfOpen(dependencyName);

        try
        {
            await operation();
            RecordSuccess();
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            RecordFailure(dependencyName, ex);
            throw;
        }
    }

    private void ThrowIfOpen(string dependencyName)
    {
        lock (_syncRoot)
        {
            if (_openedAt == null)
            {
                return;
            }

            var breakDuration = TimeSpan.FromSeconds(_options.BreakDurationSeconds);
            if (DateTime.UtcNow - _openedAt >= breakDuration)
            {
                logger.LogInformation(
                    "Circuit breaker for {DependencyName} is half-open.",
                    dependencyName);

                _openedAt = null;
                _failureCount = 0;
                return;
            }

            throw new InvalidOperationException(
                $"Circuit breaker is open for dependency '{dependencyName}'.");
        }
    }

    private void RecordSuccess()
    {
        lock (_syncRoot)
        {
            _failureCount = 0;
            _openedAt = null;
        }
    }

    private void RecordFailure(string dependencyName, Exception exception)
    {
        lock (_syncRoot)
        {
            _failureCount++;

            if (_failureCount < _options.FailureThreshold)
            {
                return;
            }

            _openedAt = DateTime.UtcNow;

            logger.LogWarning(
                exception,
                "Circuit breaker opened for {DependencyName} after {FailureCount} consecutive failures.",
                dependencyName,
                _failureCount);
        }
    }
}
