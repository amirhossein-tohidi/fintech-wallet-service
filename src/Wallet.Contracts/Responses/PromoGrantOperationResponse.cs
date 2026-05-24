using Wallet.Contracts.Enums;

namespace Wallet.Contracts.Responses;

public sealed record PromoGrantOperationResponse(
    long WalletId,
    long PromoGrantId,
    ContractWalletServiceType ServiceType,
    decimal OriginalAmount,
    decimal RemainingAmount,
    DateTime ExpiresAt);
