using Wallet.Domain.Aggregates;
using Wallet.Domain.Enums;

namespace Wallet.UnitTests.Domain;

public sealed class ReservationTests
{
    [Fact]
    public void GivenActiveReservation_WhenCreated_ThenItCanBeConfirmedAndCancelled()
    {
        var reservation = new Reservation(
            walletId: 1,
            serviceType: DomainWalletServiceType.Travel,
            amount: 100,
            expireAt: DateTime.UtcNow.AddMinutes(1));

        Assert.Equal(ReservationStatus.Created, reservation.Status);
        Assert.True(reservation.CanConfirm());
        Assert.True(reservation.CanCancel());
        Assert.False(reservation.IsExpired);
    }

    [Fact]
    public void GivenExpiredReservation_WhenCreated_ThenItCannotBeConfirmedButCanBeCancelled()
    {
        var reservation = new Reservation(
            walletId: 1,
            serviceType: DomainWalletServiceType.Travel,
            amount: 100,
            expireAt: DateTime.UtcNow.AddMilliseconds(-1));

        Assert.False(reservation.CanConfirm());
        Assert.True(reservation.CanCancel());
        Assert.True(reservation.IsExpired);
    }

    [Fact]
    public void GivenActiveReservation_WhenMarkedConfirmed_ThenStatusAndModifiedAtAreUpdated()
    {
        var reservation = new Reservation(
            walletId: 1,
            serviceType: DomainWalletServiceType.Food,
            amount: 100,
            expireAt: DateTime.UtcNow.AddMinutes(1));

        reservation.MarkConfirmed();

        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        Assert.NotNull(reservation.ModifiedAt);
        Assert.False(reservation.CanCancel());
    }

    [Fact]
    public void GivenCancelledReservation_WhenMarkedExpired_ThenOperationIsRejected()
    {
        var reservation = new Reservation(
            walletId: 1,
            serviceType: DomainWalletServiceType.Shop,
            amount: 100,
            expireAt: DateTime.UtcNow.AddMinutes(1));
        reservation.MarkCancelled();

        var exception = Assert.Throws<InvalidOperationException>(reservation.MarkExpired);

        Assert.Equal("Only active reservations can expire.", exception.Message);
    }
}
