using Wallet.Contracts.Enums;
using Wallet.Contracts.IntegrationEvents;

namespace Wallet.Worker.InboxHandlers;

public sealed class ReservationExpiredInboxHandler(
    ILogger<ReservationExpiredInboxHandler> logger,
    InboxProjectionWriter projectionWriter)
    : InboxMessageHandler<ReservationExpiredEvent>(logger)
{
    public override IntegrationEventType EventType => IntegrationEventType.ReservationExpired;

    protected override async Task HandleEnvelopeAsync(
        IntegrationEventEnvelope<ReservationExpiredEvent> envelope,
        CancellationToken ct)
    {
        await projectionWriter.InvalidateWalletBalanceAsync(walletId: envelope.Payload.WalletId, ct: ct);
        await projectionWriter.InvalidateWalletReservationsAsync(walletId: envelope.Payload.WalletId, ct: ct);
        await projectionWriter.InvalidateWalletTransactionsAsync(walletId: envelope.Payload.WalletId, ct: ct);

        logger.LogInformation(
            "Reservation expired inbox event consumed. WalletId={WalletId}, ReservationId={ReservationId}",
            envelope.Payload.WalletId,
            envelope.Payload.ReservationId);
    }
}
