using Wallet.PropertyTests.Support;

namespace Wallet.PropertyTests.Domain;

public class UserWalletProperties
{
    [Property(Arbitrary = [typeof(WalletPropertyArbitraries)], MaxTest = 200)]
    public void Successful_payment_reduces_only_available_balance(WalletMoneyPair money, WalletService service)
    {
        var wallet = CreateFundedWallet(money.Balance);

        wallet.Pay(service.Value, money.OperationAmount, "payment");

        Assert.Equal(money.Balance - money.OperationAmount, wallet.AvailableBalance);
        Assert.Equal(0m, wallet.ReservedBalance);
        Assert.True(wallet.AvailableBalance >= 0m);
    }

    [Property(Arbitrary = [typeof(WalletPropertyArbitraries)], MaxTest = 200)]
    public void Cancelled_reservation_restores_the_original_available_balance(WalletMoneyPair money, WalletService service)
    {
        var wallet = CreateFundedWallet(money.Balance);

        var reservation = wallet.CreateReservation(
            service.Value,
            money.OperationAmount,
            DateTime.UtcNow.AddDays(1),
            "hold");

        wallet.CancelReservation(reservation.Id, "release");

        Assert.Equal(money.Balance, wallet.AvailableBalance);
        Assert.Equal(0m, wallet.ReservedBalance);
        Assert.True(wallet.Reservations.All(x => x.Status != ReservationStatus.Created));
    }

    [Property(Arbitrary = [typeof(WalletPropertyArbitraries)], MaxTest = 200)]
    public void Confirmed_reservation_moves_reserved_amount_out_of_the_wallet(WalletMoneyPair money, WalletService service)
    {
        var wallet = CreateFundedWallet(money.Balance);

        var reservation = wallet.CreateReservation(
            service.Value,
            money.OperationAmount,
            DateTime.UtcNow.AddDays(1),
            "hold");

        wallet.ConfirmReservation(reservation.Id, "capture");

        Assert.Equal(money.Balance - money.OperationAmount, wallet.AvailableBalance);
        Assert.Equal(0m, wallet.ReservedBalance);
        Assert.True(wallet.AvailableBalance >= 0m);
    }

    private static UserWallet CreateFundedWallet(decimal balance)
    {
        var wallet = new UserWallet(Guid.NewGuid());
        wallet.TopUp(balance, "top-up");
        return wallet;
    }
}
