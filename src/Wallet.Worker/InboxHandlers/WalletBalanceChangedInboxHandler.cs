using Wallet.Contracts.Enums;
using Wallet.Contracts.IntegrationEvents;

namespace Wallet.Worker.InboxHandlers;

public sealed class WalletBalanceChangedInboxHandler(
    ILogger<WalletBalanceChangedInboxHandler> logger,
    InboxProjectionWriter projectionWriter)
    : InboxMessageHandler<WalletBalanceChangedEvent>(logger)
{
    public override IntegrationEventType EventType => IntegrationEventType.WalletBalanceChanged;

    protected override async Task HandleEnvelopeAsync(
        IntegrationEventEnvelope<WalletBalanceChangedEvent> envelope,
        CancellationToken ct)
    {
        await projectionWriter.UpsertWalletBalanceAsync(envelope: envelope, ct: ct);

        logger.LogInformation(
            "Wallet balance inbox projection updated. WalletId={WalletId}, NewBalance={NewBalance}",
            envelope.Payload.WalletId,
            envelope.Payload.NewBalance);
    }
}
