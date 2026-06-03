using Wallet.PropertyTests.Support;

namespace Wallet.PropertyTests.Domain;

public class PromoGrantProperties
{
    [Property(Arbitrary = [typeof(WalletPropertyArbitraries)], MaxTest = 200)]
    public void Consuming_promo_never_drives_remaining_credit_below_zero(
        NonEmptyArray<PositiveMoney> grantAmounts,
        WalletService service)
    {
        var wallet = new UserWallet(Guid.NewGuid());
        var activeGrantAmounts = grantAmounts.Get.Select(x => x.Value).ToArray();
        var consumeAmount = activeGrantAmounts.Sum() / 2m;

        foreach (var amount in activeGrantAmounts)
        {
            wallet.AddPromoGrant(service.Value, amount, DateTime.UtcNow.AddDays(1));
        }

        wallet.ConsumePromo(service.Value, consumeAmount, "promo-consume");

        Assert.All(wallet.PromoGrants, grant => Assert.True(grant.RemainingAmount >= 0m));
        Assert.Equal(activeGrantAmounts.Sum() - consumeAmount, wallet.PromoGrants.Sum(x => x.RemainingAmount));
        Assert.True(wallet.PromoGrants.Sum(x => x.ConsumedAmount) <= activeGrantAmounts.Sum());
    }
}
