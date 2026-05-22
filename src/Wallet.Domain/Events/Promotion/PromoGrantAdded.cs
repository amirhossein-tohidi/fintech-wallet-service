using Wallet.Domain.Aggregates;
using Wallet.Domain.Enums;
using Wallet.Domain.Events.Abstractions;

namespace Wallet.Domain.Events.Promotion;

public record PromoGrantAdded(
    Guid UserId,
    long WalletId,
    PromoGrant PromoGrant) : BaseDomainEvent
{
    public long PromoId => PromoGrant.Id;
    public DomainWalletServiceType ServiceType => PromoGrant.ServiceType;
    public decimal Amount => PromoGrant.Amount;
    public DateTime ExpireAt => PromoGrant.ExpiresAt;
}
