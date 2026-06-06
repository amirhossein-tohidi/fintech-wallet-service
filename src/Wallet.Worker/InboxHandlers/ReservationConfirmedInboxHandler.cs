using Wallet.Contracts.Enums;
using Wallet.Contracts.IntegrationEvents;

namespace Wallet.Worker.InboxHandlers;

public sealed class ReservationConfirmedInboxHandler(
    ILogger<ReservationConfirmedInboxHandler> logger,
    InboxProjectionWriter projectionWriter)
    : InboxMessageHandler<ReservationConfirmedEvent>(logger)
{
    public override IntegrationEventType EventType => IntegrationEventType.ReservationConfirmed;

    protected override async Task HandleEnvelopeAsync(
        IntegrationEventEnvelope<ReservationConfirmedEvent> envelope,
        CancellationToken ct)
    {
        await projectionWriter.InvalidateWalletBalanceAsync(walletId: envelope.Payload.WalletId, ct: ct);
        await projectionWriter.InvalidateWalletReservationsAsync(walletId: envelope.Payload.WalletId, ct: ct);
        await projectionWriter.InvalidateWalletTransactionsAsync(walletId: envelope.Payload.WalletId, ct: ct);

        logger.LogInformation(
            "Reservation confirmed inbox event consumed. WalletId={WalletId}, ReservationId={ReservationId}",
            envelope.Payload.WalletId,
            envelope.Payload.ReservationId);
    }
}
