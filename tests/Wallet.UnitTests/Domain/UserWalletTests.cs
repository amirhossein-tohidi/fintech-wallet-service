using Wallet.Domain.Aggregates;
using Wallet.Domain.Enums;
using Wallet.Domain.Events.Ledger;
using Wallet.Domain.Events.Promotion;
using Wallet.Domain.Events.Reservation;
using Wallet.Domain.Events.Wallet;

namespace Wallet.UnitTests.Domain;

public sealed class UserWalletTests
{
    [Fact]
    public void GivenNewWallet_WhenTopUp_ThenAvailableBalanceAndLedgerAreUpdated()
    {
        var wallet = new UserWallet(Guid.NewGuid());

        var transaction = wallet.TopUp(amount: 100, idem: "topup-1");

        Assert.Equal(100, wallet.AvailableBalance);
        Assert.Equal(0, wallet.ReservedBalance);
        Assert.Equal(LedgerTransactionType.TopUp, transaction.Type);
        Assert.Equal(DomainWalletServiceType.General, transaction.ServiceType);
        Assert.Equal("topup-1", transaction.IdempotencyKey);
        AssertLedgerEntries(
            transaction,
            (AccountType.Cash, EntryDirection.Credit, 100),
            (AccountType.Wallet, EntryDirection.Debit, 100));
        Assert.Contains(wallet.DomainEvents, x => x is WalletBalanceChanged);
        Assert.Contains(wallet.DomainEvents, x => x is LedgerTransactionCreated);
    }

    [Fact]
    public void GivenWalletHasBalance_WhenFastPay_ThenAvailableBalanceDecreases()
    {
        var wallet = CreateWalletWithBalance(100);

        var transaction = wallet.Pay(
            serviceType: DomainWalletServiceType.Travel,
            amount: 40,
            idem: "pay-1");

        Assert.Equal(60, wallet.AvailableBalance);
        Assert.Equal(LedgerTransactionType.Payment, transaction.Type);
        Assert.Equal(DomainWalletServiceType.Travel, transaction.ServiceType);
        AssertLedgerEntries(
            transaction,
            (AccountType.Wallet, EntryDirection.Credit, 40),
            (AccountType.Spent, EntryDirection.Debit, 40));
    }

    [Fact]
    public void GivenWalletHasInsufficientBalance_WhenFastPay_ThenOperationIsRejected()
    {
        var wallet = CreateWalletWithBalance(25);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            wallet.Pay(
                serviceType: DomainWalletServiceType.Food,
                amount: 30,
                idem: "pay-2"));

        Assert.Equal("Insufficient balance.", exception.Message);
        Assert.Equal(25, wallet.AvailableBalance);
    }

    [Fact]
    public void GivenWalletHasBalance_WhenReserve_ThenAmountMovesFromAvailableToReserved()
    {
        var wallet = CreateWalletWithBalance(100);

        var reservation = wallet.CreateReservation(
            serviceType: DomainWalletServiceType.Shop,
            amount: 30,
            expireAt: DateTime.UtcNow.AddMinutes(9),
            idem: "reserve-1");

        var transaction = Assert.Single(wallet.LedgerTransactions, x => x.Type == LedgerTransactionType.Hold);
        Assert.Equal(70, wallet.AvailableBalance);
        Assert.Equal(30, wallet.ReservedBalance);
        Assert.Equal(ReservationStatus.Created, reservation.Status);
        AssertLedgerEntries(
            transaction,
            (AccountType.Wallet, EntryDirection.Credit, 30),
            (AccountType.Reserved, EntryDirection.Debit, 30));
        Assert.Contains(wallet.DomainEvents, x => x is ReservationCreated);
    }

    [Fact]
    public void GivenReservationExists_WhenConfirmed_ThenReservedBalanceIsCaptured()
    {
        var wallet = CreateWalletWithBalance(100);
        var reservation = wallet.CreateReservation(
            serviceType: DomainWalletServiceType.Travel,
            amount: 45,
            expireAt: DateTime.UtcNow.AddMinutes(9),
            idem: "reserve-2");

        var transaction = wallet.ConfirmReservation(
            reservationId: reservation.Id,
            idem: "confirm-1");

        Assert.Equal(55, wallet.AvailableBalance);
        Assert.Equal(0, wallet.ReservedBalance);
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        Assert.Equal(LedgerTransactionType.Capture, transaction.Type);
        AssertLedgerEntries(
            transaction,
            (AccountType.Reserved, EntryDirection.Credit, 45),
            (AccountType.Spent, EntryDirection.Debit, 45));
        Assert.Contains(wallet.DomainEvents, x => x is ReservationConfirmed);
    }

    [Fact]
    public void GivenReservationExists_WhenCancelled_ThenReservedAmountIsReleased()
    {
        var wallet = CreateWalletWithBalance(100);
        var reservation = wallet.CreateReservation(
            serviceType: DomainWalletServiceType.Food,
            amount: 20,
            expireAt: DateTime.UtcNow.AddMinutes(9),
            idem: "reserve-3");

        var transaction = wallet.CancelReservation(
            reservationId: reservation.Id,
            idem: "cancel-1");

        Assert.Equal(100, wallet.AvailableBalance);
        Assert.Equal(0, wallet.ReservedBalance);
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.Equal(LedgerTransactionType.Release, transaction.Type);
        AssertLedgerEntries(
            transaction,
            (AccountType.Reserved, EntryDirection.Credit, 20),
            (AccountType.Wallet, EntryDirection.Debit, 20));
        Assert.Contains(wallet.DomainEvents, x => x is ReservationCancelled);
    }

    [Fact]
    public void GivenReservationIsExpired_WhenExpired_ThenReservedAmountIsReleased()
    {
        var wallet = CreateWalletWithBalance(100);
        var reservation = wallet.CreateReservation(
            serviceType: DomainWalletServiceType.Shop,
            amount: 35,
            expireAt: DateTime.UtcNow.AddMilliseconds(-1),
            idem: "reserve-4");

        var transaction = wallet.ExpireReservation(reservationId: reservation.Id);

        Assert.Equal(100, wallet.AvailableBalance);
        Assert.Equal(0, wallet.ReservedBalance);
        Assert.Equal(ReservationStatus.Expired, reservation.Status);
        Assert.Equal($"expire-{reservation.Id}", transaction.IdempotencyKey);
        Assert.Contains(wallet.DomainEvents, x => x is ReservationExpired);
    }

    [Fact]
    public void GivenReservationIsActive_WhenExpired_ThenOperationIsRejected()
    {
        var wallet = CreateWalletWithBalance(100);
        var reservation = wallet.CreateReservation(
            serviceType: DomainWalletServiceType.Shop,
            amount: 35,
            expireAt: DateTime.UtcNow.AddMinutes(1),
            idem: "reserve-5");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            wallet.ExpireReservation(reservationId: reservation.Id));

        Assert.Equal("Reservation not eligible for expiry.", exception.Message);
        Assert.Equal(65, wallet.AvailableBalance);
        Assert.Equal(35, wallet.ReservedBalance);
    }

    [Fact]
    public void GivenWallet_WhenRefunded_ThenAvailableBalanceIncreases()
    {
        var wallet = CreateWalletWithBalance(50);

        var transaction = wallet.Refund(
            serviceType: DomainWalletServiceType.Travel,
            amount: 15,
            idem: "refund-1");

        Assert.Equal(65, wallet.AvailableBalance);
        Assert.Equal(LedgerTransactionType.Refund, transaction.Type);
        AssertLedgerEntries(
            transaction,
            (AccountType.Spent, EntryDirection.Credit, 15),
            (AccountType.Wallet, EntryDirection.Debit, 15));
        Assert.Contains(wallet.DomainEvents, x => x is WalletRefunded);
    }

    [Fact]
    public void GivenPromoGrants_WhenPromoIsConsumed_ThenEarliestActiveGrantIsConsumedFirst()
    {
        var wallet = new UserWallet(Guid.NewGuid());
        var later = wallet.AddPromoGrant(
            serviceType: DomainWalletServiceType.Food,
            amount: 50,
            expiresAt: DateTime.UtcNow.AddDays(2));
        var earlier = wallet.AddPromoGrant(
            serviceType: DomainWalletServiceType.Food,
            amount: 30,
            expiresAt: DateTime.UtcNow.AddDays(1));

        var transaction = wallet.ConsumePromo(
            serviceType: DomainWalletServiceType.Food,
            amount: 40,
            idem: "promo-1");

        Assert.Equal(0, earlier.RemainingAmount);
        Assert.Equal(40, later.RemainingAmount);
        Assert.Equal(LedgerTransactionType.PromoConsume, transaction.Type);
        AssertLedgerEntries(
            transaction,
            (AccountType.Promo, EntryDirection.Credit, 40),
            (AccountType.Spent, EntryDirection.Debit, 40));
        Assert.Contains(wallet.DomainEvents, x => x is PromoConsumed);
    }

    [Fact]
    public void GivenPromoCreditIsInsufficient_WhenPromoIsConsumed_ThenOperationIsRejected()
    {
        var wallet = new UserWallet(Guid.NewGuid());
        var grant = wallet.AddPromoGrant(
            serviceType: DomainWalletServiceType.Travel,
            amount: 10,
            expiresAt: DateTime.UtcNow.AddDays(1));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            wallet.ConsumePromo(
                serviceType: DomainWalletServiceType.Travel,
                amount: 11,
                idem: "promo-2"));

        Assert.Equal("Insufficient promo credit.", exception.Message);
        Assert.Equal(10, grant.RemainingAmount);
        Assert.DoesNotContain(wallet.LedgerTransactions, x => x.Type == LedgerTransactionType.PromoConsume);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GivenNonPositiveAmount_WhenFinancialOperationRuns_ThenOperationIsRejected(decimal amount)
    {
        var wallet = new UserWallet(Guid.NewGuid());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            wallet.TopUp(amount: amount, idem: "invalid"));

        Assert.Equal("Amount must be positive.", exception.Message);
    }

    private static UserWallet CreateWalletWithBalance(decimal balance)
    {
        var wallet = new UserWallet(Guid.NewGuid());
        wallet.TopUp(amount: balance, idem: $"topup-{Guid.NewGuid():N}");
        wallet.ClearDomainEvents();
        return wallet;
    }

    private static void AssertLedgerEntries(
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
