using Wallet.Contracts.Enums;
using Wallet.Contracts.IntegrationEvents;

namespace Wallet.Worker.InboxHandlers;

public sealed class ReservationCancelledInboxHandler(
    ILogger<ReservationCancelledInboxHandler> logger,
    InboxProjectionWriter projectionWriter)
    : InboxMessageHandler<ReservationCancelledEvent>(logger)
{
    public override IntegrationEventType EventType => IntegrationEventType.ReservationCancelled;

    protected override async Task HandleEnvelopeAsync(
        IntegrationEventEnvelope<ReservationCancelledEvent> envelope,
        CancellationToken ct)
    {
        await projectionWriter.InvalidateWalletBalanceAsync(walletId: envelope.Payload.WalletId, ct: ct);
        await projectionWriter.InvalidateWalletReservationsAsync(walletId: envelope.Payload.WalletId, ct: ct);
        await projectionWriter.InvalidateWalletTransactionsAsync(walletId: envelope.Payload.WalletId, ct: ct);

        logger.LogInformation(
            "Reservation cancelled inbox event consumed. WalletId={WalletId}, ReservationId={ReservationId}",
            envelope.Payload.WalletId,
            envelope.Payload.ReservationId);
    }
}
