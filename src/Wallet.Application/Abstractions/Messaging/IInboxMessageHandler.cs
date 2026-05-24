using Wallet.Contracts.Enums;

namespace Wallet.Application.Abstractions.Messaging;

public interface IInboxMessageHandler
{
    IntegrationEventType EventType { get; }

    Task HandleAsync(string payload, CancellationToken ct);
}
