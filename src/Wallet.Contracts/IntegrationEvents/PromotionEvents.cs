using Wallet.Contracts.Enums;

namespace Wallet.Contracts.IntegrationEvents;

public sealed record PromoGrantAddedEvent(
    Guid UserId,
    long WalletId,
    long PromoId,
    ContractWalletServiceType ServiceType,
    decimal Amount,
    DateTime ExpireAt);

public sealed record PromoConsumedEvent(
    Guid UserId,
    long WalletId,
    ContractWalletServiceType ServiceType,
    decimal Amount);
