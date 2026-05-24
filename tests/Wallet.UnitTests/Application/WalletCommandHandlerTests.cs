using Wallet.Application.Wallets.Commands.AddPromoGrant;
using Wallet.Application.Wallets.Commands.CancelReservation;
using Wallet.Application.Wallets.Commands.ConfirmReservation;
using Wallet.Application.Wallets.Commands.ConsumePromo;
using Wallet.Application.Wallets.Commands.FastPay;
using Wallet.Application.Wallets.Commands.Refund;
using Wallet.Application.Wallets.Commands.Reserve;
using Wallet.Application.Wallets.Commands.TopUpWallet;
using Wallet.Contracts.Enums;
using Wallet.Domain.Aggregates;
using Wallet.Domain.Enums;

namespace Wallet.UnitTests.Application;

public sealed class WalletCommandHandlerTests
{
    private readonly TestRouteInfo _routeInfo = new();

    [Fact]
    public async Task GivenNoWalletExists_WhenTopUpCommandIsHandled_ThenWalletIsCreated()
    {
        await using var context = WalletApplicationTestFixture.CreateDbContext();
        var handler = new TopUpWalletCommandHandler(context, WalletApplicationTestFixture.CreateMapper());
        var userId = Guid.NewGuid();

        var result = await handler.Handle(
            new TopUpWalletCommand(
                UserId: userId,
                Amount: 100,
                IdempotencyKey: "topup-command-1",
                RouteInfo: _routeInfo),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(100, result.Value.AvailableBalance);
        Assert.Equal(ContractWalletServiceType.General, result.Value.ServiceType);
        Assert.Single(context.UserWallets);
        Assert.True(context.OutboxMessages.Count() >= 2);
    }

    [Fact]
    public async Task GivenWalletExists_WhenFastPayCommandIsHandled_ThenBalanceIsDebited()
    {
        await using var context = WalletApplicationTestFixture.CreateDbContext();
        var wallet = await AddWalletWithBalanceAsync(context, amount: 100);
        var handler = new FastPayCommandHandler(context, WalletApplicationTestFixture.CreateMapper());

        var result = await handler.Handle(
            new FastPayCommand(
                WalletId: wallet.Id,
                ServiceType: DomainWalletServiceType.Travel,
                Amount: 35,
                IdempotencyKey: "pay-command-1",
                RouteInfo: _routeInfo),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(65, result.Value!.AvailableBalance);
        Assert.Equal(ContractWalletServiceType.Travel, result.Value.ServiceType);
        Assert.Equal("Payment", result.Value.TransactionType);
    }

    [Fact]
    public async Task GivenWalletDoesNotExist_WhenFastPayCommandIsHandled_ThenFailureIsReturned()
    {
        await using var context = WalletApplicationTestFixture.CreateDbContext();
        var handler = new FastPayCommandHandler(context, WalletApplicationTestFixture.CreateMapper());

        var result = await handler.Handle(
            new FastPayCommand(
                WalletId: 999,
                ServiceType: DomainWalletServiceType.Food,
                Amount: 10,
                IdempotencyKey: "pay-command-2",
                RouteInfo: _routeInfo),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Wallet not found.", result.Error);
    }

    [Fact]
    public async Task GivenInvalidTopUpAmount_WhenTopUpCommandIsHandled_ThenFailureIsReturned()
    {
        await using var context = WalletApplicationTestFixture.CreateDbContext();
        var handler = new TopUpWalletCommandHandler(context, WalletApplicationTestFixture.CreateMapper());

        var result = await handler.Handle(
            new TopUpWalletCommand(
                UserId: Guid.NewGuid(),
                Amount: 0,
                IdempotencyKey: "topup-command-invalid",
                RouteInfo: _routeInfo),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Amount must be positive.", result.Error);
    }

    [Fact]
    public async Task GivenWalletDoesNotExist_WhenReserveCommandIsHandled_ThenFailureIsReturned()
    {
        await using var context = WalletApplicationTestFixture.CreateDbContext();
        var handler = new ReserveHandler(context, WalletApplicationTestFixture.CreateMapper());

        var result = await handler.Handle(
            new ReserveCommand(
                WalletId: 404,
                ServiceType: DomainWalletServiceType.Food,
                Amount: 10,
                IdempotencyKey: "reserve-command-missing",
                RouteInfo: _routeInfo),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Wallet not found.", result.Error);
    }

    [Fact]
    public async Task GivenWalletHasInsufficientBalance_WhenReserveCommandIsHandled_ThenFailureIsReturned()
    {
        await using var context = WalletApplicationTestFixture.CreateDbContext();
        var wallet = await AddWalletWithBalanceAsync(context, amount: 10);
        var handler = new ReserveHandler(context, WalletApplicationTestFixture.CreateMapper());

        var result = await handler.Handle(
            new ReserveCommand(
                WalletId: wallet.Id,
                ServiceType: DomainWalletServiceType.Food,
                Amount: 11,
                IdempotencyKey: "reserve-command-insufficient",
                RouteInfo: _routeInfo),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Insufficient balance.", result.Error);
    }

    [Fact]
    public async Task GivenWalletHasBalance_WhenReserveAndConfirmAreHandled_ThenReservationIsCaptured()
    {
        await using var context = WalletApplicationTestFixture.CreateDbContext();
        var wallet = await AddWalletWithBalanceAsync(context, amount: 100);
        var mapper = WalletApplicationTestFixture.CreateMapper();
        var reserveHandler = new ReserveHandler(context, mapper);
        var confirmHandler = new ConfirmReservationHandler(context, mapper);

        var reserveResult = await reserveHandler.Handle(
            new ReserveCommand(
                WalletId: wallet.Id,
                ServiceType: DomainWalletServiceType.Shop,
                Amount: 40,
                IdempotencyKey: "reserve-command-1",
                RouteInfo: _routeInfo),
            CancellationToken.None);
        var confirmResult = await confirmHandler.Handle(
            new ConfirmReservationCommand(
                WalletId: wallet.Id,
                ReservationId: reserveResult.Value!.ReservationId,
                IdempotencyKey: "confirm-command-1",
                RouteInfo: _routeInfo),
            CancellationToken.None);

        Assert.True(confirmResult.IsSuccess);
        Assert.Equal(60, confirmResult.Value!.AvailableBalance);
        Assert.Equal(0, confirmResult.Value.ReservedBalance);
        Assert.Equal("Confirmed", confirmResult.Value.Status);
        Assert.NotNull(confirmResult.Value.TransactionId);
    }

    [Fact]
    public async Task GivenWalletHasBalance_WhenReserveAndCancelAreHandled_ThenFundsAreReleased()
    {
        await using var context = WalletApplicationTestFixture.CreateDbContext();
        var wallet = await AddWalletWithBalanceAsync(context, amount: 100);
        var mapper = WalletApplicationTestFixture.CreateMapper();
        var reserveHandler = new ReserveHandler(context, mapper);
        var cancelHandler = new CancelReservationHandler(context, mapper);

        var reserveResult = await reserveHandler.Handle(
            new ReserveCommand(
                WalletId: wallet.Id,
                ServiceType: DomainWalletServiceType.Food,
                Amount: 40,
                IdempotencyKey: "reserve-command-2",
                RouteInfo: _routeInfo),
            CancellationToken.None);
        var cancelResult = await cancelHandler.Handle(
            new CancelReservationCommand(
                WalletId: wallet.Id,
                ReservationId: reserveResult.Value!.ReservationId,
                IdempotencyKey: "cancel-command-1",
                RouteInfo: _routeInfo),
            CancellationToken.None);

        Assert.True(cancelResult.IsSuccess);
        Assert.Equal(100, cancelResult.Value!.AvailableBalance);
        Assert.Equal(0, cancelResult.Value.ReservedBalance);
        Assert.Equal("Cancelled", cancelResult.Value.Status);
    }

    [Fact]
    public async Task GivenReservationDoesNotExist_WhenConfirmCommandIsHandled_ThenFailureIsReturned()
    {
        await using var context = WalletApplicationTestFixture.CreateDbContext();
        var handler = new ConfirmReservationHandler(context, WalletApplicationTestFixture.CreateMapper());

        var result = await handler.Handle(
            new ConfirmReservationCommand(
                WalletId: 1,
                ReservationId: 404,
                IdempotencyKey: "confirm-missing",
                RouteInfo: _routeInfo),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Reservation not found.", result.Error);
    }

    [Fact]
    public async Task GivenReservationDoesNotExist_WhenCancelCommandIsHandled_ThenFailureIsReturned()
    {
        await using var context = WalletApplicationTestFixture.CreateDbContext();
        var handler = new CancelReservationHandler(context, WalletApplicationTestFixture.CreateMapper());

        var result = await handler.Handle(
            new CancelReservationCommand(
                WalletId: 1,
                ReservationId: 404,
                IdempotencyKey: "cancel-missing",
                RouteInfo: _routeInfo),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Reservation not found.", result.Error);
    }

    [Fact]
    public async Task GivenWalletExists_WhenRefundCommandIsHandled_ThenBalanceIsCredited()
    {
        await using var context = WalletApplicationTestFixture.CreateDbContext();
        var wallet = await AddWalletWithBalanceAsync(context, amount: 10);
        var handler = new RefundHandler(context, WalletApplicationTestFixture.CreateMapper());

        var result = await handler.Handle(
            new RefundCommand(
                WalletId: wallet.Id,
                ServiceType: DomainWalletServiceType.Travel,
                Amount: 25,
                IdempotencyKey: "refund-command-1",
                RouteInfo: _routeInfo),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(35, result.Value!.AvailableBalance);
        Assert.Equal("Refund", result.Value.TransactionType);
    }

    [Fact]
    public async Task GivenWalletDoesNotExist_WhenRefundCommandIsHandled_ThenFailureIsReturned()
    {
        await using var context = WalletApplicationTestFixture.CreateDbContext();
        var handler = new RefundHandler(context, WalletApplicationTestFixture.CreateMapper());

        var result = await handler.Handle(
            new RefundCommand(
                WalletId: 404,
                ServiceType: DomainWalletServiceType.Travel,
                Amount: 25,
                IdempotencyKey: "refund-missing",
                RouteInfo: _routeInfo),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Wallet not found.", result.Error);
    }

    [Fact]
    public async Task GivenWalletHasPromoCredit_WhenConsumePromoCommandIsHandled_ThenPromoIsConsumed()
    {
        await using var context = WalletApplicationTestFixture.CreateDbContext();
        var wallet = new UserWallet(Guid.NewGuid());
        wallet.AddPromoGrant(
            serviceType: DomainWalletServiceType.Food,
            amount: 50,
            expiresAt: DateTime.UtcNow.AddDays(1));
        context.UserWallets.Add(wallet);
        await context.SaveChangesAsync();
        var handler = new ConsumePromoHandler(context, WalletApplicationTestFixture.CreateMapper());

        var result = await handler.Handle(
            new ConsumePromoCommand(
                WalletId: wallet.Id,
                ServiceType: DomainWalletServiceType.Food,
                Amount: 20,
                IdempotencyKey: "promo-command-1",
                RouteInfo: _routeInfo),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("PromoConsume", result.Value!.TransactionType);
        Assert.Equal(30, wallet.PromoGrants.Single().RemainingAmount);
    }

    [Fact]
    public async Task GivenWalletDoesNotExist_WhenConsumePromoCommandIsHandled_ThenFailureIsReturned()
    {
        await using var context = WalletApplicationTestFixture.CreateDbContext();
        var handler = new ConsumePromoHandler(context, WalletApplicationTestFixture.CreateMapper());

        var result = await handler.Handle(
            new ConsumePromoCommand(
                WalletId: 404,
                ServiceType: DomainWalletServiceType.Food,
                Amount: 20,
                IdempotencyKey: "promo-missing",
                RouteInfo: _routeInfo),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Wallet not found.", result.Error);
    }

    [Fact]
    public async Task GivenWalletExists_WhenAddPromoGrantCommandIsHandled_ThenPromoGrantIsCreated()
    {
        await using var context = WalletApplicationTestFixture.CreateDbContext();
        var wallet = new UserWallet(Guid.NewGuid());
        context.UserWallets.Add(wallet);
        await context.SaveChangesAsync();
        var handler = new AddPromoGrantHandler(context, WalletApplicationTestFixture.CreateMapper());
        var expiresAt = DateTime.UtcNow.AddDays(1);

        var result = await handler.Handle(
            new AddPromoGrantCommand(
                WalletId: wallet.Id,
                ServiceType: DomainWalletServiceType.Shop,
                Amount: 70,
                ExpiresAt: expiresAt,
                IdempotencyKey: "grant-command-1",
                RouteInfo: _routeInfo),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(70, result.Value!.OriginalAmount);
        Assert.Equal(70, result.Value.RemainingAmount);
        Assert.Equal(ContractWalletServiceType.Shop, result.Value.ServiceType);
    }

    [Fact]
    public async Task GivenWalletDoesNotExist_WhenAddPromoGrantCommandIsHandled_ThenFailureIsReturned()
    {
        await using var context = WalletApplicationTestFixture.CreateDbContext();
        var handler = new AddPromoGrantHandler(context, WalletApplicationTestFixture.CreateMapper());

        var result = await handler.Handle(
            new AddPromoGrantCommand(
                WalletId: 404,
                ServiceType: DomainWalletServiceType.Shop,
                Amount: 70,
                ExpiresAt: DateTime.UtcNow.AddDays(1),
                IdempotencyKey: "grant-missing",
                RouteInfo: _routeInfo),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Wallet not found.", result.Error);
    }

    private static async Task<UserWallet> AddWalletWithBalanceAsync(
        Wallet.Infrastructure.Persistence.WalletDbContext context,
        decimal amount)
    {
        var wallet = new UserWallet(Guid.NewGuid());
        wallet.TopUp(amount: amount, idem: $"seed-{Guid.NewGuid():N}");
        context.UserWallets.Add(wallet);
        await context.SaveChangesAsync();
        wallet.ClearDomainEvents();
        return wallet;
    }
}
