using Wallet.Contracts.Enums;

namespace Wallet.Contracts.Responses;

public sealed record TransactionResponse(
    long TransactionId,
    long WalletId,
    ContractLedgerTransactionType TransactionType,
    ContractWalletServiceType ServiceType,
    decimal Amount,
    long? ReferenceId,
    DateTime CreatedAt);
