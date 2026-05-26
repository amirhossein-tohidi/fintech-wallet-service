using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Reqnroll;
using Wallet.AcceptanceTests.Support;
using Wallet.Api.Constants;
using Wallet.Contracts.Enums;
using Wallet.Contracts.Requests;
using Wallet.Contracts.Responses;
using Wallet.Domain.Enums;
using Xunit;

namespace Wallet.AcceptanceTests.StepDefinitions;

[Binding]
public sealed class WalletMoneyMovementSteps : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client = AcceptanceTestHooks.Fixture.CreateClient();
    private readonly List<HttpResponseMessage> _responses = [];
    private readonly Guid _userId = Guid.NewGuid();

    private IHost? _workerHost;
    private long _walletId;
    private long _reservationId;
    private HttpResponseMessage? _lastResponse;
    private int _successfulConcurrentPayments;

    [Given("a wallet user exists")]
    public void GivenAWalletUserExists()
    {
    }

    [Given("a wallet user has topped up {decimal}")]
    public async Task GivenAWalletUserHasToppedUp(decimal amount)
    {
        await TopUpAsync(amount, $"topup-{Guid.NewGuid():N}", HttpStatusCode.OK);
    }

    [Given("the user has paid {decimal} for {word}")]
    public async Task GivenTheUserHasPaidFor(decimal amount, ContractWalletServiceType serviceType)
    {
        await PayAsync(amount, serviceType, shouldSucceed: true);
    }

    [Given("the user has an expired {word} reservation of {decimal}")]
    public async Task GivenTheUserHasAnExpiredReservationOf(ContractWalletServiceType serviceType, decimal amount)
    {
        await ReserveAsync(amount, serviceType);

        await using var dbContext = AcceptanceTestHooks.Fixture.CreateDbContext();
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [Reservations] SET [ExpireAt] = DATEADD(second, -5, SYSUTCDATETIME()) WHERE [Id] = {_reservationId}");
    }

    [Given("the user has {word} promo credit of {decimal}")]
    public async Task GivenTheUserHasPromoCreditOf(ContractWalletServiceType serviceType, decimal amount)
    {
        var response = await PostJsonAsync(
            $"/api/v1/promo/{_walletId}/services/{serviceType}/promo-grants",
            new AddPromoGrantRequest(amount, DateTime.UtcNow.AddDays(1)),
            $"promo-grant-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [When("the user tops up {decimal} with idempotency key {string}")]
    public async Task WhenTheUserTopsUpWithIdempotencyKey(decimal amount, string idempotencyKey)
    {
        await TopUpAsync(amount, idempotencyKey, HttpStatusCode.OK);
    }

    [When("the user tops up {decimal} again with idempotency key {string}")]
    public async Task WhenTheUserTopsUpAgainWithIdempotencyKey(decimal amount, string idempotencyKey)
    {
        await TopUpAsync(amount, idempotencyKey, HttpStatusCode.OK);
    }

    [When("the user pays {decimal} for {word}")]
    public async Task WhenTheUserPaysFor(decimal amount, ContractWalletServiceType serviceType)
    {
        await PayAsync(amount, serviceType, shouldSucceed: true);
    }

    [When("the user tries to pay {decimal} for {word}")]
    public async Task WhenTheUserTriesToPayFor(decimal amount, ContractWalletServiceType serviceType)
    {
        await PayAsync(amount, serviceType, shouldSucceed: false);
    }

    [When("the user reserves {decimal} for {word}")]
    public async Task WhenTheUserReservesFor(decimal amount, ContractWalletServiceType serviceType)
    {
        await ReserveAsync(amount, serviceType);
    }

    [When("the user confirms the reservation")]
    public async Task WhenTheUserConfirmsTheReservation()
    {
        var response = await PostJsonAsync(
            $"/api/v1/wallet/{_walletId}/reservations/{_reservationId}/confirm",
            string.Empty,
            $"confirm-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [When("the user cancels the reservation")]
    public async Task WhenTheUserCancelsTheReservation()
    {
        var response = await PostJsonAsync(
            $"/api/v1/wallet/{_walletId}/reservations/{_reservationId}/cancel",
            string.Empty,
            $"cancel-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [When("the reservation expiry worker runs")]
    public async Task WhenTheReservationExpiryWorkerRuns()
    {
        _workerHost = await AcceptanceTestHooks.Fixture.StartWorkerAsync(
            reservationExpiryEnabled: true,
            redisEnabled: false);

        await WaitUntilAsync(async () =>
        {
            await using var dbContext = AcceptanceTestHooks.Fixture.CreateDbContext();
            var reservation = await dbContext.Reservations.SingleAsync(x => x.Id == _reservationId);
            return reservation.Status == ReservationStatus.Expired;
        });

        await _workerHost.StopAsync();
        _workerHost.Dispose();
        _workerHost = null;
    }

    [When("the user receives a refund of {decimal} for {word}")]
    public async Task WhenTheUserReceivesARefundOfFor(decimal amount, ContractWalletServiceType serviceType)
    {
        var response = await PostJsonAsync(
            $"/api/v1/wallet/{_walletId}/services/{serviceType}/refunds",
            new RefundRequest(amount),
            $"refund-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [When("the user consumes {decimal} {word} promo credit")]
    public async Task WhenTheUserConsumesPromoCredit(decimal amount, ContractWalletServiceType serviceType)
    {
        await ConsumePromoAsync(amount, serviceType, shouldSucceed: true);
    }

    [When("the user tries to consume {decimal} {word} promo credit")]
    public async Task WhenTheUserTriesToConsumePromoCredit(decimal amount, ContractWalletServiceType serviceType)
    {
        await ConsumePromoAsync(amount, serviceType, shouldSucceed: false);
    }

    [When("{int} concurrent {word} payments of {decimal} are submitted")]
    public async Task WhenConcurrentPaymentsOfAreSubmitted(int count, ContractWalletServiceType serviceType, decimal amount)
    {
        var tasks = Enumerable.Range(0, count)
            .Select(index => PostJsonAsync(
                $"/api/v1/wallet/{_walletId}/services/{serviceType}/fast-pay",
                new FastPayRequest(amount),
                $"concurrent-pay-{Guid.NewGuid():N}"));

        var responses = await Task.WhenAll(tasks);
        _successfulConcurrentPayments = responses.Count(x => x.StatusCode == HttpStatusCode.OK);
    }

    [Then("the wallet available balance should be {decimal}")]
    public async Task ThenTheWalletAvailableBalanceShouldBe(decimal expected)
    {
        var balance = await GetBalanceAsync();
        Assert.Equal(expected, balance.AvailableBalance);
    }

    [Then("the wallet available balance should never be negative")]
    public async Task ThenTheWalletAvailableBalanceShouldNeverBeNegative()
    {
        var balance = await GetBalanceAsync();
        Assert.True(balance.AvailableBalance >= 0);
    }

    [Then("the wallet reserved balance should be {decimal}")]
    public async Task ThenTheWalletReservedBalanceShouldBe(decimal expected)
    {
        var balance = await GetBalanceAsync();
        Assert.Equal(expected, balance.ReservedBalance);
    }

    [Then("{word} transactions should include {word}")]
    public async Task ThenTransactionsShouldInclude(ContractWalletServiceType serviceType, ContractLedgerTransactionType transactionType)
    {
        await AssertTransactionsIncludeAsync(serviceType, transactionType);
    }

    [Then("{word} transactions should include {word} and {word}")]
    public async Task ThenTransactionsShouldIncludeAnd(
        ContractWalletServiceType serviceType,
        ContractLedgerTransactionType firstTransactionType,
        ContractLedgerTransactionType secondTransactionType)
    {
        await AssertTransactionsIncludeAsync(serviceType, firstTransactionType, secondTransactionType);
    }

    [Then("the operation should be rejected")]
    public void ThenTheOperationShouldBeRejected()
    {
        Assert.NotNull(_lastResponse);
        Assert.Equal(HttpStatusCode.BadRequest, _lastResponse.StatusCode);
    }

    [Then("Food promo remaining balance should be {decimal}")]
    public async Task ThenFoodPromoRemainingBalanceShouldBe(decimal expected)
    {
        var balances = await _client.GetFromJsonAsync<IReadOnlyCollection<PromoBalanceResponse>>(
            $"/api/v1/promo/{_walletId}/services/{ContractWalletServiceType.Food}/promo-balances",
            JsonOptions);

        Assert.NotNull(balances);
        Assert.Equal(expected, balances.Sum(x => x.RemainingAmount));
    }

    [Then("only {int} ledger transaction should exist")]
    public async Task ThenOnlyLedgerTransactionShouldExist(int expected)
    {
        await using var dbContext = AcceptanceTestHooks.Fixture.CreateDbContext();
        var count = await dbContext.LedgerTransactions.CountAsync(x => x.WalletId == _walletId);
        Assert.Equal(expected, count);
    }

    [Then("at most {int} {word} payments should be successful")]
    public void ThenAtMostPaymentsShouldBeSuccessful(int expected, ContractWalletServiceType _)
    {
        Assert.InRange(_successfulConcurrentPayments, 0, expected);
    }

    public void Dispose()
    {
        _workerHost?.Dispose();
        _client.Dispose();

        foreach (var response in _responses)
        {
            response.Dispose();
        }
    }

    private async Task TopUpAsync(decimal amount, string idempotencyKey, HttpStatusCode expectedStatusCode)
    {
        var response = await PostJsonAsync(
            $"/api/v1/wallet/users/{_userId}/topups",
            new TopUpWalletRequest(amount),
            idempotencyKey);

        Assert.Equal(expectedStatusCode, response.StatusCode);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<WalletTransactionResultResponse>(JsonOptions);
            Assert.NotNull(result);
            _walletId = result.WalletId;
        }
    }

    private async Task PayAsync(decimal amount, ContractWalletServiceType serviceType, bool shouldSucceed)
    {
        var response = await PostJsonAsync(
            $"/api/v1/wallet/{_walletId}/services/{serviceType}/fast-pay",
            new FastPayRequest(amount),
            $"pay-{Guid.NewGuid():N}");

        if (shouldSucceed)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private async Task ReserveAsync(decimal amount, ContractWalletServiceType serviceType)
    {
        var response = await PostJsonAsync(
            $"/api/v1/wallet/{_walletId}/services/{serviceType}/reservations",
            new ReserveRequest(amount),
            $"reserve-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ReservationOperationResponse>(JsonOptions);
        Assert.NotNull(result);
        _reservationId = result.ReservationId;
    }

    private async Task ConsumePromoAsync(decimal amount, ContractWalletServiceType serviceType, bool shouldSucceed)
    {
        var response = await PostJsonAsync(
            $"/api/v1/promo/{_walletId}/services/{serviceType}/promo-consumptions",
            new ConsumePromoRequest(amount),
            $"promo-consume-{Guid.NewGuid():N}");

        if (shouldSucceed)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private async Task<WalletBalanceResponse> GetBalanceAsync()
    {
        var balance = await _client.GetFromJsonAsync<WalletBalanceResponse>(
            $"/api/v1/wallet/{_walletId}/balance",
            JsonOptions);

        Assert.NotNull(balance);
        return balance;
    }

    private async Task AssertTransactionsIncludeAsync(
        ContractWalletServiceType serviceType,
        params ContractLedgerTransactionType[] expectedTypes)
    {
        var transactions = await _client.GetFromJsonAsync<IReadOnlyCollection<TransactionResponse>>(
            $"/api/v1/wallet/{_walletId}/services/{serviceType}/transactions",
            JsonOptions);

        Assert.NotNull(transactions);

        foreach (var expectedType in expectedTypes)
        {
            Assert.Contains(transactions, x => x.TransactionType == expectedType);
        }
    }

    private async Task<HttpResponseMessage> PostJsonAsync<TRequest>(
        string requestUri,
        TRequest request,
        string idempotencyKey)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        httpRequest.Headers.Add(HeaderNames.IdempotencyKey, idempotencyKey);

        var response = await _client.SendAsync(httpRequest);
        _responses.Add(response);
        _lastResponse = response;
        return response;
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(250, cancellationTokenSource.Token);
        }

        Assert.Fail("The expected condition was not met before the timeout.");
    }
}
