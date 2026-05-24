using Wallet.Domain.Enums;
using Wallet.Domain.Idempotency;

namespace Wallet.UnitTests.Domain;

public sealed class IdempotencyRequestTests
{
    [Fact]
    public void GivenNewRequest_WhenCreated_ThenStatusIsPending()
    {
        var request = new IdempotencyRequest(
            key: "key-1",
            endpoint: "POST /wallet",
            requestHash: "hash",
            expireAt: DateTime.UtcNow.AddMinutes(5));

        Assert.Equal("key-1", request.Key);
        Assert.Equal("POST /wallet", request.Endpoint);
        Assert.Equal("hash", request.RequestHash);
        Assert.Equal(IdempotencyStatus.Pending, request.Status);
        Assert.True(request.IsPending());
        Assert.False(request.IsExpired());
    }

    [Fact]
    public void GivenPendingRequest_WhenCompleted_ThenResponseAndStatusAreStored()
    {
        var request = CreateRequest();

        request.MarkAsCompleted(responseBody: "{\"ok\":true}");

        Assert.Equal(IdempotencyStatus.Completed, request.Status);
        Assert.Equal("{\"ok\":true}", request.ResponseBody);
        Assert.False(request.IsPending());
        Assert.NotNull(request.ModifiedAt);
    }

    [Fact]
    public void GivenPendingRequest_WhenFailed_ThenErrorAndStatusAreStored()
    {
        var request = CreateRequest();

        request.MarkAsFailed(error: "failed");

        Assert.Equal(IdempotencyStatus.Failed, request.Status);
        Assert.Equal("failed", request.ResponseBody);
        Assert.False(request.IsPending());
        Assert.NotNull(request.ModifiedAt);
    }

    [Fact]
    public void GivenExpiredRequest_WhenChecked_ThenIsExpiredReturnsTrue()
    {
        var request = new IdempotencyRequest(
            key: "key-1",
            endpoint: "POST /wallet",
            requestHash: "hash",
            expireAt: DateTime.UtcNow.AddMilliseconds(-1));

        Assert.True(request.IsExpired());
    }

    private static IdempotencyRequest CreateRequest()
    {
        return new IdempotencyRequest(
            key: "key-1",
            endpoint: "POST /wallet",
            requestHash: "hash",
            expireAt: DateTime.UtcNow.AddMinutes(5));
    }
}
