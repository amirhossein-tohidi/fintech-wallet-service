using Wallet.Domain.Aggregates;
using Wallet.Domain.Enums;

namespace Wallet.UnitTests.Domain;

public sealed class PromoGrantTests
{
    [Fact]
    public void GivenActivePromoGrant_WhenPartiallyConsumed_ThenRemainingAndConsumedAmountsAreUpdated()
    {
        var grant = new PromoGrant(
            walletId: 1,
            serviceType: DomainWalletServiceType.Food,
            amount: 100,
            expiresAt: DateTime.UtcNow.AddDays(1));

        var consumed = grant.Consume(amount: 40);

        Assert.Equal(40, consumed);
        Assert.Equal(60, grant.RemainingAmount);
        Assert.Equal(40, grant.ConsumedAmount);
        Assert.True(grant.IsActive);
        Assert.NotNull(grant.ModifiedAt);
    }

    [Fact]
    public void GivenActivePromoGrant_WhenConsumeExceedsRemaining_ThenOnlyRemainingAmountIsConsumed()
    {
        var grant = new PromoGrant(
            walletId: 1,
            serviceType: DomainWalletServiceType.Food,
            amount: 25,
            expiresAt: DateTime.UtcNow.AddDays(1));

        var consumed = grant.Consume(amount: 40);

        Assert.Equal(25, consumed);
        Assert.Equal(0, grant.RemainingAmount);
        Assert.False(grant.IsActive);
    }

    [Fact]
    public void GivenExpiredPromoGrant_WhenConsumed_ThenNothingIsConsumed()
    {
        var grant = new PromoGrant(
            walletId: 1,
            serviceType: DomainWalletServiceType.Food,
            amount: 25,
            expiresAt: DateTime.UtcNow.AddMilliseconds(-1));

        var consumed = grant.Consume(amount: 10);

        Assert.Equal(0, consumed);
        Assert.Equal(25, grant.RemainingAmount);
        Assert.True(grant.IsExpired);
        Assert.False(grant.IsActive);
    }
}
