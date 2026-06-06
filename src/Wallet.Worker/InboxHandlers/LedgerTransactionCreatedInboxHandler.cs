using Wallet.Contracts.IntegrationEvents;
using Wallet.Contracts.Enums;

namespace Wallet.Worker.InboxHandlers;

public sealed class LedgerTransactionCreatedInboxHandler(
    ILogger<LedgerTransactionCreatedInboxHandler> logger,
    InboxProjectionWriter projectionWriter)
    : InboxMessageHandler<LedgerTransactionCreatedEvent>(logger)
{
    public override IntegrationEventType EventType => IntegrationEventType.LedgerTransactionCreated;

    protected override async Task HandleEnvelopeAsync(
        IntegrationEventEnvelope<LedgerTransactionCreatedEvent> envelope,
        CancellationToken ct)
    {
        await projectionWriter.InvalidateWalletTransactionsAsync(
            walletId: envelope.Payload.WalletId,
            ct: ct);

        logger.LogInformation(
            "Ledger transaction inbox event consumed. WalletId={WalletId}, TransactionId={TransactionId}, ServiceType={ServiceType}, Type={TransactionType}",
            envelope.Payload.WalletId,
            envelope.Payload.TransactionId,
            envelope.Payload.ServiceType,
            envelope.Payload.Type);
    }
}
