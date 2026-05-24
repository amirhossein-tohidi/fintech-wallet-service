using Wallet.Domain.Enums;

namespace Wallet.Infrastructure.Dapper;

internal sealed record TransactionReadRow(
    long TransactionId,
    long WalletId,
    LedgerTransactionType Type,
    DomainWalletServiceType ServiceType,
    decimal Amount,
    long? ReferenceId,
    DateTime CreatedAt);
