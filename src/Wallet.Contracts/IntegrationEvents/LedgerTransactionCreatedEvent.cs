using Wallet.Contracts.Enums;

namespace Wallet.Contracts.IntegrationEvents;

public sealed record LedgerTransactionCreatedEvent(
    Guid UserId,
    long WalletId,
    long TransactionId,
    ContractWalletServiceType ServiceType,
    decimal Amount,
    string Type);
