using Wallet.Domain.Aggregates;
using Wallet.Domain.Enums;

namespace Wallet.UnitTests.Domain;

public sealed class LedgerTransactionTests
{
    [Fact]
    public void GivenTopUpTransaction_WhenCreated_ThenItIsBalancedBetweenCashAndWallet()
    {
        var transaction = LedgerTransaction.TopUp(walletId: 10, amount: 100, idem: "idem");

        Assert.Equal(LedgerTransactionType.TopUp, transaction.Type);
        Assert.Equal(DomainWalletServiceType.General, transaction.ServiceType);
        Assert.Null(transaction.ReferenceId);
        AssertEntries(
            transaction,
            (AccountType.Cash, EntryDirection.Credit, 100),
            (AccountType.Wallet, EntryDirection.Debit, 100));
    }

    [Fact]
    public void GivenPaymentTransaction_WhenCreated_ThenItIsBalancedBetweenWalletAndSpent()
    {
        var transaction = LedgerTransaction.Payment(
            walletId: 10,
            serviceType: DomainWalletServiceType.Travel,
            amount: 100,
            idem: "idem");

        Assert.Equal(LedgerTransactionType.Payment, transaction.Type);
        AssertEntries(
            transaction,
            (AccountType.Wallet, EntryDirection.Credit, 100),
            (AccountType.Spent, EntryDirection.Debit, 100));
    }

    [Fact]
    public void GivenReservationLifecycleTransactions_WhenCreated_ThenReferenceIdIsSet()
    {
        var hold = LedgerTransaction.Hold(
            walletId: 10,
            reservationId: 20,
            serviceType: DomainWalletServiceType.Shop,
            amount: 100,
            idem: "hold");
        var capture = LedgerTransaction.Capture(
            walletId: 10,
            reservationId: 20,
            serviceType: DomainWalletServiceType.Shop,
            amount: 100,
            idem: "capture");
        var release = LedgerTransaction.Release(
            walletId: 10,
            reservationId: 20,
            serviceType: DomainWalletServiceType.Shop,
            amount: 100,
            idem: "release");

        Assert.Equal(20, hold.ReferenceId);
        Assert.Equal(20, capture.ReferenceId);
        Assert.Equal(20, release.ReferenceId);
        Assert.Equal(LedgerTransactionType.Hold, hold.Type);
        Assert.Equal(LedgerTransactionType.Capture, capture.Type);
        Assert.Equal(LedgerTransactionType.Release, release.Type);
    }

    [Fact]
    public void GivenRefundAndPromoConsumeTransactions_WhenCreated_ThenExpectedLedgerEntriesAreUsed()
    {
        var refund = LedgerTransaction.Refund(
            walletId: 10,
            serviceType: DomainWalletServiceType.Food,
            amount: 100,
            idem: "refund");
        var promo = LedgerTransaction.PromoConsume(
            walletId: 10,
            serviceType: DomainWalletServiceType.Food,
            amount: 100,
            idem: "promo");

        AssertEntries(
            refund,
            (AccountType.Spent, EntryDirection.Credit, 100),
            (AccountType.Wallet, EntryDirection.Debit, 100));
        AssertEntries(
            promo,
            (AccountType.Promo, EntryDirection.Credit, 100),
            (AccountType.Spent, EntryDirection.Debit, 100));
    }

    private static void AssertEntries(
        LedgerTransaction transaction,
        params (AccountType Account, EntryDirection Direction, decimal Amount)[] expectedEntries)
    {
        Assert.Equal(expectedEntries.Length, transaction.Entries.Count);

        foreach (var expected in expectedEntries)
        {
            Assert.Contains(transaction.Entries, entry =>
                entry.Account == expected.Account &&
                entry.Direction == expected.Direction &&
                entry.Amount == expected.Amount);
        }
    }
}
