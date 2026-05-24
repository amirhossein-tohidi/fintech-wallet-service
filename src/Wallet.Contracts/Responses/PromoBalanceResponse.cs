using Wallet.Contracts.Enums;

namespace Wallet.Contracts.Responses;

public sealed class PromoBalanceResponse
{
    public long PromoGrantId { get; set; }
    public ContractWalletServiceType ServiceType { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime ExpiresAt { get; set; }

    public decimal ConsumedAmount => CalculateConsumedAmount();
    public bool IsExpired => HasExpired();

    private decimal CalculateConsumedAmount()
    {
        return OriginalAmount - RemainingAmount;
    }

    private bool HasExpired()
    {
        return DateTime.UtcNow >= ExpiresAt;
    }
}
