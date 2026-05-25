using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Wallet.Infrastructure.Resilience;

public sealed class CircuitBreakerState(
    IOptions<CircuitBreakerOptions> options,
    ILogger<CircuitBreakerState> logger)
{
    private readonly CircuitBreakerOptions _options = options.Value;
    private int _failureCount;
    private long _openedAtTicks;

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
        var openedAtTicks = Volatile.Read(ref _openedAtTicks);
        if (openedAtTicks == 0)
        {
            return;
        }

        var breakDuration = TimeSpan.FromSeconds(_options.BreakDurationSeconds);
        var openedAt = new DateTime(openedAtTicks, DateTimeKind.Utc);
        if (DateTime.UtcNow - openedAt >= breakDuration)
        {
            if (Interlocked.CompareExchange(ref _openedAtTicks, value: 0, comparand: openedAtTicks) == openedAtTicks)
            {
                Interlocked.Exchange(ref _failureCount, value: 0);

                logger.LogInformation(
                    "Circuit breaker for {DependencyName} is half-open.",
                    dependencyName);
            }

            return;
        }

        throw new InvalidOperationException(
            $"Circuit breaker is open for dependency '{dependencyName}'.");
    }

    private void RecordSuccess()
    {
        Interlocked.Exchange(ref _failureCount, value: 0);
        Interlocked.Exchange(ref _openedAtTicks, value: 0);
    }

    private void RecordFailure(string dependencyName, Exception exception)
    {
        var failureCount = Interlocked.Increment(ref _failureCount);
        if (failureCount < _options.FailureThreshold)
        {
            return;
        }

        var openedAtTicks = DateTime.UtcNow.Ticks;
        if (Interlocked.CompareExchange(ref _openedAtTicks, value: openedAtTicks, comparand: 0) != 0)
        {
            return;
        }

        logger.LogWarning(
            exception,
            "Circuit breaker opened for {DependencyName} after {FailureCount} consecutive failures.",
            dependencyName,
            failureCount);
    }
}
