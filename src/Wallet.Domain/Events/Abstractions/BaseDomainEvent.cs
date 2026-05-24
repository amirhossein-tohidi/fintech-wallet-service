namespace Wallet.Domain.Events.Abstractions;

public abstract record BaseDomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}