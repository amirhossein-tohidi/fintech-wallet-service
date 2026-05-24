using Wallet.Contracts.Enums;

namespace Wallet.Contracts.Responses;

public sealed record ReservationOperationResponse(
    long WalletId,
    long ReservationId,
    long? TransactionId,
    ContractWalletServiceType ServiceType,
    decimal Amount,
    DateTime ExpiresAt,
    string Status,
    decimal AvailableBalance,
    decimal ReservedBalance);
