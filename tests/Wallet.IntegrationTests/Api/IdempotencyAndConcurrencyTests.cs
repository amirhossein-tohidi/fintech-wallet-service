using System.Net;
using Microsoft.EntityFrameworkCore;
using Wallet.Contracts.Enums;
using Wallet.Contracts.Requests;
using Wallet.Contracts.Responses;
using Wallet.IntegrationTests.Infrastructure;

namespace Wallet.IntegrationTests.Api;

public sealed class IdempotencyAndConcurrencyTests(WalletIntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Idempotency_ReplaysCompletedResponseForSameKeyAndBody()
    {
        var userId = Guid.NewGuid();
        var idempotencyKey = $"replay-{Guid.NewGuid():N}";

        var firstResponse = await PostAsync(
            uri: $"/api/v1/wallet/users/{userId}/topups",
            body: new TopUpWalletRequest(250),
            idempotencyKey: idempotencyKey);
        await AssertStatusAsync(firstResponse, HttpStatusCode.OK);
        var first = await ReadRequiredJsonAsync<WalletTransactionResultResponse>(firstResponse);

        var secondResponse = await PostAsync(
            uri: $"/api/v1/wallet/users/{userId}/topups",
            body: new TopUpWalletRequest(250),
            idempotencyKey: idempotencyKey);
        await AssertStatusAsync(secondResponse, HttpStatusCode.OK);
        var second = await ReadRequiredJsonAsync<WalletTransactionResultResponse>(secondResponse);

        Assert.Equal(first, second);

        await using var dbContext = Fixture.CreateDbContext();
        Assert.Equal(1, await dbContext.LedgerTransactions.CountAsync());
        Assert.Equal(1, await dbContext.IdempotencyRequests.CountAsync());
    }

    [Fact]
    public async Task Idempotency_RejectsSameKeyForDifferentRequestBody()
    {
        var userId = Guid.NewGuid();
        var idempotencyKey = $"conflict-{Guid.NewGuid():N}";

        var firstResponse = await PostAsync(
            uri: $"/api/v1/wallet/users/{userId}/topups",
            body: new TopUpWalletRequest(100),
            idempotencyKey: idempotencyKey);
        await AssertStatusAsync(firstResponse, HttpStatusCode.OK);

        var secondResponse = await PostAsync(
            uri: $"/api/v1/wallet/users/{userId}/topups",
            body: new TopUpWalletRequest(101),
            idempotencyKey: idempotencyKey);
        await AssertStatusAsync(secondResponse, HttpStatusCode.Conflict);

        var error = await ReadRequiredJsonAsync<ApiErrorResponse>(secondResponse);
        Assert.Equal(ApiErrorCode.CommonIdempotencyKeyConflict.ToCode(), error.Code);
    }

    [Fact]
    public async Task Idempotency_ConcurrentRequestsWithSameKeyCreateOnlyOneTransaction()
    {
        var userId = Guid.NewGuid();
        var idempotencyKey = $"same-key-concurrent-{Guid.NewGuid():N}";

        var responses = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ =>
            PostAsync(
                uri: $"/api/v1/wallet/users/{userId}/topups",
                body: new TopUpWalletRequest(100),
                idempotencyKey: idempotencyKey)));

        Assert.All(responses, response =>
            Assert.True(
                response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict,
                $"Unexpected status code: {response.StatusCode}"));

        await using var dbContext = Fixture.CreateDbContext();
        var wallet = await dbContext.UserWallets.SingleAsync();
        Assert.Equal(100, wallet.AvailableBalance);
        Assert.Equal(1, await dbContext.LedgerTransactions.CountAsync());

        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task Concurrency_ParallelDistinctPaymentsCannotOverspendWallet()
    {
        var userId = Guid.NewGuid();
        var topUpResponse = await PostAsync(
            uri: $"/api/v1/wallet/users/{userId}/topups",
            body: new TopUpWalletRequest(100),
            idempotencyKey: $"topup-{Guid.NewGuid():N}");
        var topUp = await ReadRequiredJsonAsync<WalletTransactionResultResponse>(topUpResponse);

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(i =>
            PostAsync(
                uri: $"/api/v1/wallet/{topUp.WalletId}/services/{ContractWalletServiceType.Shop}/fast-pay",
                body: new FastPayRequest(30),
                idempotencyKey: $"pay-{Guid.NewGuid():N}-{i}")));

        var successfulPayments = responses.Count(response => response.StatusCode == HttpStatusCode.OK);
        Assert.InRange(successfulPayments, 1, 3);
        Assert.All(responses, response =>
            Assert.True(
                response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest or HttpStatusCode.Conflict,
                $"Unexpected status code: {response.StatusCode}"));

        await using var dbContext = Fixture.CreateDbContext();
        var wallet = await dbContext.UserWallets.SingleAsync(x => x.Id == topUp.WalletId);
        Assert.Equal(100 - successfulPayments * 30, wallet.AvailableBalance);
        Assert.True(wallet.AvailableBalance >= 0);

        var paymentCount = await dbContext.LedgerTransactions
            .CountAsync(x => x.WalletId == topUp.WalletId && x.ServiceType == Wallet.Domain.Enums.DomainWalletServiceType.Shop);
        Assert.Equal(successfulPayments, paymentCount);

        foreach (var response in responses)
        {
            response.Dispose();
        }
    }
}
