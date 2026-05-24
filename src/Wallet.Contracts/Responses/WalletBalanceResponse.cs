namespace Wallet.Contracts.Responses;

public sealed class WalletBalanceResponse
{
    public long WalletId { get; set; }
    public Guid UserId { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal ReservedBalance { get; set; }

    public decimal TotalRealBalance => CalculateTotalRealBalance();

    private decimal CalculateTotalRealBalance()
    {
        return AvailableBalance + ReservedBalance;
    }
}
