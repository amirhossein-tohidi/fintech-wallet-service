using Wallet.Contracts.Enums;
using Wallet.Contracts.Events;

namespace Wallet.Worker.InboxHandlers;

public sealed class PromoConsumedInboxHandler(
    ILogger<PromoConsumedInboxHandler> logger,
    InboxProjectionWriter projectionWriter)
    : InboxMessageHandler<PromoConsumedEvent>(logger)
{
    public override IntegrationEventType EventType => IntegrationEventType.PromoConsumed;

    protected override async Task HandleEnvelopeAsync(
        IntegrationEventEnvelope<PromoConsumedEvent> envelope,
        CancellationToken ct)
    {
        await projectionWriter.InvalidatePromoBalancesAsync(
            walletId: envelope.Payload.WalletId,
            serviceType: envelope.Payload.ServiceType,
            ct: ct);

        logger.LogInformation(
            "Promo consumed inbox event consumed. WalletId={WalletId}, ServiceType={ServiceType}, Amount={Amount}",
            envelope.Payload.WalletId,
            envelope.Payload.ServiceType,
            envelope.Payload.Amount);
    }
}
