namespace Wallet.Infrastructure.Redis;

public static class WalletCacheKeys
{
    public static string Balance(long walletId) => $"wallet:{walletId}:balance";

    public static string Transactions(long walletId) => $"wallet:{walletId}:transactions";

    public static string Reservations(long walletId) => $"wallet:{walletId}:reservations";

    public static string PromoBalances(long walletId, string serviceType)
        => $"wallet:{walletId}:promo-balances:{serviceType}";
}
