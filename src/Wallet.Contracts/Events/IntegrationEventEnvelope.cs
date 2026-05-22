using Wallet.Contracts.Enums;

namespace Wallet.Contracts.Events;

public sealed record IntegrationEventEnvelope<TPayload>(
    Guid Id,
    IntegrationEventType Type,
    DateTime OccurredOn,
    TPayload Payload);
