using Wallet.Contracts.Enums;
using Wallet.Contracts.IntegrationEvents;

namespace Wallet.Worker.InboxHandlers;

public sealed class PromoGrantAddedInboxHandler(
    ILogger<PromoGrantAddedInboxHandler> logger,
    InboxProjectionWriter projectionWriter)
    : InboxMessageHandler<PromoGrantAddedEvent>(logger)
{
    public override IntegrationEventType EventType => IntegrationEventType.PromoGrantAdded;

    protected override async Task HandleEnvelopeAsync(
        IntegrationEventEnvelope<PromoGrantAddedEvent> envelope,
        CancellationToken ct)
    {
        await projectionWriter.InvalidatePromoBalancesAsync(
            walletId: envelope.Payload.WalletId,
            serviceType: envelope.Payload.ServiceType,
            ct: ct);

        logger.LogInformation(
            "Promo grant inbox event consumed. WalletId={WalletId}, PromoId={PromoId}, ServiceType={ServiceType}",
            envelope.Payload.WalletId,
            envelope.Payload.PromoId,
            envelope.Payload.ServiceType);
    }
}
