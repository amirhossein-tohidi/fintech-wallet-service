using Wallet.Application.Common;
using Wallet.Application;
using Microsoft.Extensions.DependencyInjection;
using Wallet.Application.Mapping;
using Wallet.Contracts.Enums;
using Wallet.Contracts.Responses;
using Wallet.Domain.Enums;

namespace Wallet.UnitTests.Application;

public sealed class MappingAndUtilityTests
{
    [Theory]
    [InlineData(DomainWalletServiceType.General, ContractWalletServiceType.General)]
    [InlineData(DomainWalletServiceType.Travel, ContractWalletServiceType.Travel)]
    [InlineData(DomainWalletServiceType.Food, ContractWalletServiceType.Food)]
    [InlineData(DomainWalletServiceType.Shop, ContractWalletServiceType.Shop)]
    public void GivenDomainServiceType_WhenMappedToContract_ThenExpectedValueIsReturned(
        DomainWalletServiceType domainValue,
        ContractWalletServiceType expected)
    {
        Assert.Equal(expected, domainValue.ToContract());
    }

    [Theory]
    [InlineData(ContractWalletServiceType.General, DomainWalletServiceType.General)]
    [InlineData(ContractWalletServiceType.Travel, DomainWalletServiceType.Travel)]
    [InlineData(ContractWalletServiceType.Food, DomainWalletServiceType.Food)]
    [InlineData(ContractWalletServiceType.Shop, DomainWalletServiceType.Shop)]
    public void GivenContractServiceType_WhenMappedToDomain_ThenExpectedValueIsReturned(
        ContractWalletServiceType contractValue,
        DomainWalletServiceType expected)
    {
        Assert.Equal(expected, contractValue.ToDomain());
    }

    [Theory]
    [InlineData(LedgerTransactionType.TopUp, ContractLedgerTransactionType.TopUp)]
    [InlineData(LedgerTransactionType.Payment, ContractLedgerTransactionType.Payment)]
    [InlineData(LedgerTransactionType.Hold, ContractLedgerTransactionType.Hold)]
    [InlineData(LedgerTransactionType.Capture, ContractLedgerTransactionType.Capture)]
    [InlineData(LedgerTransactionType.Release, ContractLedgerTransactionType.Release)]
    [InlineData(LedgerTransactionType.Refund, ContractLedgerTransactionType.Refund)]
    [InlineData(LedgerTransactionType.PromoConsume, ContractLedgerTransactionType.PromoConsume)]
    public void GivenLedgerTransactionType_WhenMappedToContract_ThenExpectedValueIsReturned(
        LedgerTransactionType domainValue,
        ContractLedgerTransactionType expected)
    {
        Assert.Equal(expected, domainValue.ToContract());
    }

    [Fact]
    public void GivenSameRequest_WhenHashedTwice_ThenHashIsStable()
    {
        var hasher = new RequestHasher();
        var request = new { WalletId = 10, Amount = 100 };

        var first = hasher.ComputeHash(request);
        var second = hasher.ComputeHash(request);

        Assert.Equal(first, second);
        Assert.False(string.IsNullOrWhiteSpace(first));
    }

    [Fact]
    public void GivenNullRequest_WhenHashed_ThenEmptyHashIsReturned()
    {
        var hasher = new RequestHasher();

        var hash = hasher.ComputeHash<object?>(null);

        Assert.Equal(string.Empty, hash);
    }

    [Fact]
    public void GivenSuccessResult_WhenCreated_ThenItHasSuccessState()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void GivenFailureResult_WhenCreated_ThenItHasFailureState()
    {
        var result = Result<int>.Failure("error");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("error", result.Error);
        Assert.Equal(default, result.Value);
    }

    [Fact]
    public void GivenApiErrorCode_WhenConverted_ThenCodeAndDescriptionAreReturned()
    {
        var response = ApiErrorResponse.From(ApiErrorCode.CommonIdempotencyKeyRequired);

        Assert.Equal("common_idempotency_key_required", response.Code);
        Assert.Equal("X-Idempotency-Key header is required.", response.Message);
    }

    [Fact]
    public void GivenApiErrorCodeWithOverride_WhenConverted_ThenOverrideMessageIsUsed()
    {
        var response = ApiErrorResponse.From(
            errorCode: ApiErrorCode.WalletOperationRejected,
            messageOverride: "custom");

        Assert.Equal("wallet_operation_rejected", response.Code);
        Assert.Equal("custom", response.Message);
    }

    [Fact]
    public void GivenRouteInfo_WhenEndpointIsRead_ThenHttpMethodAndPathAreCombined()
    {
        Wallet.Application.Abstractions.IRouteInfo routeInfo = new TestRouteInfo();

        Assert.Equal("POST /unit-test", routeInfo.Endpoint);
    }

    [Fact]
    public void GivenServiceCollection_WhenApplicationIsRegistered_ThenServicesAreAdded()
    {
        var services = new ServiceCollection();

        var result = services.AddApplication();

        Assert.Same(services, result);
        Assert.NotEmpty(services);
    }
}
