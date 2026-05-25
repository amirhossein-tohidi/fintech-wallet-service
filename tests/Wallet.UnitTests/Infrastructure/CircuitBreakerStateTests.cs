using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Wallet.Infrastructure.Resilience;

namespace Wallet.UnitTests.Infrastructure;

public sealed class CircuitBreakerStateTests
{
    [Fact]
    public async Task ExecuteAsync_WhenCircuitIsClosed_ExecutesOperation()
    {
        var circuitBreaker = CreateCircuitBreaker();
        var executed = false;

        await circuitBreaker.ExecuteAsync(
            dependencyName: "kafka",
            operation: () =>
            {
                executed = true;
                return Task.CompletedTask;
            });

        Assert.True(executed);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFailureThresholdIsReached_OpensCircuitAndFailsFast()
    {
        var circuitBreaker = CreateCircuitBreaker(failureThreshold: 2);

        await Assert.ThrowsAsync<InvalidOperationException>(() => FailAsync(circuitBreaker));
        await Assert.ThrowsAsync<InvalidOperationException>(() => FailAsync(circuitBreaker));

        var executed = false;
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            circuitBreaker.ExecuteAsync(
                dependencyName: "kafka",
                operation: () =>
                {
                    executed = true;
                    return Task.CompletedTask;
                }));

        Assert.False(executed);
        Assert.Contains("Circuit breaker is open", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOperationSucceeds_ResetsFailureCount()
    {
        var circuitBreaker = CreateCircuitBreaker(failureThreshold: 2);

        await Assert.ThrowsAsync<InvalidOperationException>(() => FailAsync(circuitBreaker));

        await circuitBreaker.ExecuteAsync(
            dependencyName: "kafka",
            operation: () => Task.CompletedTask);

        await Assert.ThrowsAsync<InvalidOperationException>(() => FailAsync(circuitBreaker));

        var executed = false;
        await circuitBreaker.ExecuteAsync(
            dependencyName: "kafka",
            operation: () =>
            {
                executed = true;
                return Task.CompletedTask;
            });

        Assert.True(executed);
    }

    [Fact]
    public async Task ExecuteAsync_WhenBreakDurationElapsed_AllowsNextOperation()
    {
        var circuitBreaker = CreateCircuitBreaker(
            failureThreshold: 1,
            breakDurationSeconds: 0);

        await Assert.ThrowsAsync<InvalidOperationException>(() => FailAsync(circuitBreaker));

        var executed = false;
        await circuitBreaker.ExecuteAsync(
            dependencyName: "kafka",
            operation: () =>
            {
                executed = true;
                return Task.CompletedTask;
            });

        Assert.True(executed);
    }

    private static CircuitBreakerState CreateCircuitBreaker(
        int failureThreshold = 3,
        int breakDurationSeconds = 30)
    {
        return new CircuitBreakerState(
            options: Options.Create(new CircuitBreakerOptions
            {
                FailureThreshold = failureThreshold,
                BreakDurationSeconds = breakDurationSeconds
            }),
            logger: NullLogger<CircuitBreakerState>.Instance);
    }

    private static Task FailAsync(CircuitBreakerState circuitBreaker)
    {
        return circuitBreaker.ExecuteAsync(
            dependencyName: "kafka",
            operation: () => throw new InvalidOperationException("Kafka is unavailable."));
    }
}
