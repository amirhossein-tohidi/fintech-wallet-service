namespace Wallet.Infrastructure.Resilience;

public sealed class CircuitBreakerOptions
{
    public const string SectionName = "CircuitBreaker";

    public int FailureThreshold { get; set; } = 5;
    public int BreakDurationSeconds { get; set; } = 30;
}
