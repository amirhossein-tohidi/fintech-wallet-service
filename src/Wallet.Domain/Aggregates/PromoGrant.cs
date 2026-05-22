using Wallet.Domain.Common;
using Wallet.Domain.Enums;

namespace Wallet.Domain.Aggregates;

public class PromoGrant : BaseEntity
{
    public long WalletId { get; private set; }
    public DomainWalletServiceType ServiceType { get; private set; }
    public decimal Amount { get; private set; }
    public decimal RemainingAmount { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public decimal ConsumedAmount => Amount - RemainingAmount;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => RemainingAmount > 0 && !IsExpired;

    private PromoGrant() { }

    public PromoGrant(long walletId, DomainWalletServiceType serviceType, decimal amount, DateTime expiresAt)
    {
        WalletId = walletId;
        ServiceType = serviceType;
        Amount = amount;
        RemainingAmount = amount;
        ExpiresAt = expiresAt;
    }

    public decimal Consume(decimal amount)
    {
        if (!IsActive)
            return 0;

        var usable = Math.Min(amount, RemainingAmount);
        RemainingAmount -= usable;
        MarkAsModified();
        
        return usable;
    }
}
