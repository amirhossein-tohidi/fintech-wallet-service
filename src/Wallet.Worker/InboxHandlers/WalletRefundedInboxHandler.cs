using Wallet.Contracts.Enums;
using Wallet.Contracts.Events;

namespace Wallet.Worker.InboxHandlers;

public sealed class WalletRefundedInboxHandler(
    ILogger<WalletRefundedInboxHandler> logger,
    InboxProjectionWriter projectionWriter)
    : InboxMessageHandler<WalletRefundedEvent>(logger)
{
    public override IntegrationEventType EventType => IntegrationEventType.WalletRefunded;

    protected override async Task HandleEnvelopeAsync(
        IntegrationEventEnvelope<WalletRefundedEvent> envelope,
        CancellationToken ct)
    {
        await projectionWriter.InvalidateWalletBalanceAsync(walletId: envelope.Payload.WalletId, ct: ct);
        await projectionWriter.InvalidateWalletTransactionsAsync(walletId: envelope.Payload.WalletId, ct: ct);

        logger.LogInformation(
            "Wallet refund inbox event consumed. WalletId={WalletId}, Amount={Amount}",
            envelope.Payload.WalletId,
            envelope.Payload.Amount);
    }
}
