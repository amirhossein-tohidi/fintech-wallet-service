using Wallet.Contracts.Enums;
using Wallet.Contracts.Events;

namespace Wallet.Worker.InboxHandlers;

public sealed class ReservationCreatedInboxHandler(
    ILogger<ReservationCreatedInboxHandler> logger,
    InboxProjectionWriter projectionWriter)
    : InboxMessageHandler<ReservationCreatedEvent>(logger)
{
    public override IntegrationEventType EventType => IntegrationEventType.ReservationCreated;

    protected override async Task HandleEnvelopeAsync(
        IntegrationEventEnvelope<ReservationCreatedEvent> envelope,
        CancellationToken ct)
    {
        await projectionWriter.InvalidateWalletBalanceAsync(walletId: envelope.Payload.WalletId, ct: ct);
        await projectionWriter.InvalidateWalletReservationsAsync(walletId: envelope.Payload.WalletId, ct: ct);

        logger.LogInformation(
            "Reservation created inbox event consumed. WalletId={WalletId}, ReservationId={ReservationId}",
            envelope.Payload.WalletId,
            envelope.Payload.ReservationId);
    }
}
