using Wallet.Domain.Enums;
using Wallet.Domain.Events.Abstractions;

namespace Wallet.Domain.Events.Promotion;

public record PromoConsumed(
    Guid UserId,
    long WalletId,
    DomainWalletServiceType ServiceType,
    decimal Amount) : BaseDomainEvent;
