using Wallet.Contracts.Enums;

namespace Wallet.Contracts.Responses;

public sealed record WalletTransactionResultResponse(
    long WalletId,
    long TransactionId,
    ContractWalletServiceType ServiceType,
    string TransactionType,
    decimal Amount,
    decimal AvailableBalance,
    decimal ReservedBalance);
