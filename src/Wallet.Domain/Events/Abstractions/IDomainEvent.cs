using MediatR;

namespace Wallet.Domain.Events.Abstractions;

public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}