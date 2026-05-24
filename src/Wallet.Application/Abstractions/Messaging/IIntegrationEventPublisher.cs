using Wallet.Contracts.Enums;

namespace Wallet.Application.Abstractions.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(
        IntegrationEventType eventType,
        string payload,
        CancellationToken ct = default);
}
