using System.Net;
using System.Net.Http.Json;
using Wallet.Contracts.Enums;
using Wallet.Contracts.Requests;
using Wallet.Contracts.Responses;
using Wallet.Domain.Enums;
using Wallet.IntegrationTests.Infrastructure;

namespace Wallet.IntegrationTests.Api;

public sealed class WalletApiScenarioTests(WalletIntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task WalletApi_ExecutesFullMoneyReservationAndPromoFlow()
    {
        var userId = Guid.NewGuid();

        var topUpResponse = await PostAsync(
            uri: $"/api/v1/wallet/users/{userId}/topups",
            body: new TopUpWalletRequest(1000),
            idempotencyKey: $"topup-{Guid.NewGuid():N}");
        await AssertStatusAsync(topUpResponse, HttpStatusCode.OK);
        var topUp = await ReadRequiredJsonAsync<WalletTransactionResultResponse>(topUpResponse);

        Assert.Equal(1000, topUp.AvailableBalance);
        Assert.Equal(0, topUp.ReservedBalance);

        var fastPayResponse = await PostAsync(
            uri: $"/api/v1/wallet/{topUp.WalletId}/services/{ContractWalletServiceType.Food}/fast-pay",
            body: new FastPayRequest(200),
            idempotencyKey: $"fastpay-{Guid.NewGuid():N}");
        await AssertStatusAsync(fastPayResponse, HttpStatusCode.OK);
        var fastPay = await ReadRequiredJsonAsync<WalletTransactionResultResponse>(fastPayResponse);
        Assert.Equal(800, fastPay.AvailableBalance);

        var refundResponse = await PostAsync(
            uri: $"/api/v1/wallet/{topUp.WalletId}/services/{ContractWalletServiceType.Food}/refunds",
            body: new RefundRequest(50),
            idempotencyKey: $"refund-{Guid.NewGuid():N}");
        await AssertStatusAsync(refundResponse, HttpStatusCode.OK);
        var refund = await ReadRequiredJsonAsync<WalletTransactionResultResponse>(refundResponse);
        Assert.Equal(850, refund.AvailableBalance);

        var reserveResponse = await PostAsync(
            uri: $"/api/v1/wallet/{topUp.WalletId}/services/{ContractWalletServiceType.Travel}/reservations",
            body: new ReserveRequest(300),
            idempotencyKey: $"reserve-{Guid.NewGuid():N}");
        await AssertStatusAsync(reserveResponse, HttpStatusCode.Created);
        var reservation = await ReadRequiredJsonAsync<ReservationOperationResponse>(reserveResponse);
        Assert.Equal(550, reservation.AvailableBalance);
        Assert.Equal(300, reservation.ReservedBalance);
        Assert.Equal(nameof(ReservationStatus.Created), reservation.Status);

        var confirmResponse = await PostAsync(
            uri: $"/api/v1/wallet/{topUp.WalletId}/reservations/{reservation.ReservationId}/confirm",
            idempotencyKey: $"confirm-{Guid.NewGuid():N}");
        await AssertStatusAsync(confirmResponse, HttpStatusCode.OK);
        var confirmed = await ReadRequiredJsonAsync<ReservationOperationResponse>(confirmResponse);
        Assert.Equal(550, confirmed.AvailableBalance);
        Assert.Equal(0, confirmed.ReservedBalance);
        Assert.Equal(nameof(ReservationStatus.Confirmed), confirmed.Status);

        var promoGrantResponse = await PostAsync(
            uri: $"/api/v1/promo/{topUp.WalletId}/services/{ContractWalletServiceType.Food}/promo-grants",
            body: new AddPromoGrantRequest(120, DateTime.UtcNow.AddHours(1)),
            idempotencyKey: $"promo-grant-{Guid.NewGuid():N}");
        await AssertStatusAsync(promoGrantResponse, HttpStatusCode.Created);
        var promoGrant = await ReadRequiredJsonAsync<PromoGrantOperationResponse>(promoGrantResponse);
        Assert.Equal(120, promoGrant.RemainingAmount);

        var promoConsumeResponse = await PostAsync(
            uri: $"/api/v1/promo/{topUp.WalletId}/services/{ContractWalletServiceType.Food}/promo-consumptions",
            body: new ConsumePromoRequest(70),
            idempotencyKey: $"promo-consume-{Guid.NewGuid():N}");
        await AssertStatusAsync(promoConsumeResponse, HttpStatusCode.OK);

        var balance = await Client.GetFromJsonAsync<WalletBalanceResponse>(
            $"/api/v1/wallet/{topUp.WalletId}/balance",
            JsonOptions);
        Assert.NotNull(balance);
        Assert.Equal(550, balance.AvailableBalance);
        Assert.Equal(0, balance.ReservedBalance);

        var promoBalances = await Client.GetFromJsonAsync<IReadOnlyCollection<PromoBalanceResponse>>(
            $"/api/v1/promo/{topUp.WalletId}/services/{ContractWalletServiceType.Food}/promo-balances",
            JsonOptions);
        Assert.NotNull(promoBalances);
        Assert.Single(promoBalances);
        Assert.Equal(50, promoBalances.Single().RemainingAmount);

        var travelTransactions = await Client.GetFromJsonAsync<IReadOnlyCollection<TransactionResponse>>(
            $"/api/v1/wallet/{topUp.WalletId}/services/{ContractWalletServiceType.Travel}/transactions",
            JsonOptions);
        Assert.NotNull(travelTransactions);
        Assert.Contains(travelTransactions, tx => tx.TransactionType == ContractLedgerTransactionType.Hold);
        Assert.Contains(travelTransactions, tx => tx.TransactionType == ContractLedgerTransactionType.Capture);
    }

    [Fact]
    public async Task WalletApi_ReturnsExpectedErrorsForMissingResourcesAndRejectedOperations()
    {
        var missingBalance = await Client.GetAsync("/api/v1/wallet/999999/balance");
        await AssertStatusAsync(missingBalance, HttpStatusCode.NotFound);

        var missingWalletFastPay = await PostAsync(
            uri: $"/api/v1/wallet/999999/services/{ContractWalletServiceType.Food}/fast-pay",
            body: new FastPayRequest(50),
            idempotencyKey: $"missing-fastpay-{Guid.NewGuid():N}");
        await AssertStatusAsync(missingWalletFastPay, HttpStatusCode.NotFound);

        var userId = Guid.NewGuid();
        var topUpResponse = await PostAsync(
            uri: $"/api/v1/wallet/users/{userId}/topups",
            body: new TopUpWalletRequest(100),
            idempotencyKey: $"topup-{Guid.NewGuid():N}");
        var wallet = await ReadRequiredJsonAsync<WalletTransactionResultResponse>(topUpResponse);

        var insufficientFastPay = await PostAsync(
            uri: $"/api/v1/wallet/{wallet.WalletId}/services/{ContractWalletServiceType.Food}/fast-pay",
            body: new FastPayRequest(150),
            idempotencyKey: $"insufficient-{Guid.NewGuid():N}");
        await AssertStatusAsync(insufficientFastPay, HttpStatusCode.BadRequest);

        var error = await ReadRequiredJsonAsync<ApiErrorResponse>(insufficientFastPay);
        Assert.Equal(ApiErrorCode.WalletOperationRejected.ToCode(), error.Code);
    }

    [Fact]
    public async Task WalletApi_ValidationFailureDoesNotPoisonIdempotencyKey()
    {
        var userId = Guid.NewGuid();
        var idempotencyKey = $"validation-retry-{Guid.NewGuid():N}";

        var invalidResponse = await PostAsync(
            uri: $"/api/v1/wallet/users/{userId}/topups",
            body: new TopUpWalletRequest(0),
            idempotencyKey: idempotencyKey);
        await AssertStatusAsync(invalidResponse, HttpStatusCode.BadRequest);

        var validResponse = await PostAsync(
            uri: $"/api/v1/wallet/users/{userId}/topups",
            body: new TopUpWalletRequest(100),
            idempotencyKey: idempotencyKey);
        await AssertStatusAsync(validResponse, HttpStatusCode.OK);

        var topUp = await ReadRequiredJsonAsync<WalletTransactionResultResponse>(validResponse);
        Assert.Equal(100, topUp.AvailableBalance);
    }

    [Fact]
    public async Task WalletApi_RequiresIdempotencyKeyForCommands()
    {
        var response = await PostAsync(
            uri: $"/api/v1/wallet/users/{Guid.NewGuid()}/topups",
            body: new TopUpWalletRequest(100));

        await AssertStatusAsync(response, HttpStatusCode.BadRequest);
        var error = await ReadRequiredJsonAsync<ApiErrorResponse>(response);
        Assert.Equal(ApiErrorCode.CommonIdempotencyKeyRequired.ToCode(), error.Code);
    }
}
