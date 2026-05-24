using Wallet.Contracts.Enums;

namespace Wallet.Contracts.Events;

public sealed record LedgerTransactionCreatedEvent(
    Guid UserId,
    long WalletId,
    long TransactionId,
    ContractWalletServiceType ServiceType,
    decimal Amount,
    string Type);
