using Wallet.Application.Validation;
using Wallet.Contracts.Requests;

namespace Wallet.UnitTests.Application;

public sealed class RequestValidatorTests
{
    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void GivenTopUpRequest_WhenValidated_ThenAmountMustBePositive(decimal amount, bool expectedIsValid)
    {
        var validator = new TopUpWalletRequestValidator();

        var result = validator.Validate(new TopUpWalletRequest(amount));

        Assert.Equal(expectedIsValid, result.IsValid);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void GivenFastPayRequest_WhenValidated_ThenAmountMustBePositive(decimal amount, bool expectedIsValid)
    {
        var validator = new FastPayRequestValidator();

        var result = validator.Validate(new FastPayRequest(amount));

        Assert.Equal(expectedIsValid, result.IsValid);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void GivenReserveRequest_WhenValidated_ThenAmountMustBePositive(decimal amount, bool expectedIsValid)
    {
        var validator = new ReserveRequestValidator();

        var result = validator.Validate(new ReserveRequest(amount));

        Assert.Equal(expectedIsValid, result.IsValid);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void GivenRefundRequest_WhenValidated_ThenAmountMustBePositive(decimal amount, bool expectedIsValid)
    {
        var validator = new RefundRequestValidator();

        var result = validator.Validate(new RefundRequest(amount));

        Assert.Equal(expectedIsValid, result.IsValid);
    }

    [Fact]
    public void GivenPromoGrantRequest_WhenExpiresAtIsInPast_ThenValidationFails()
    {
        var validator = new AddPromoGrantRequestValidator();

        var result = validator.Validate(new AddPromoGrantRequest(
            Amount: 10,
            ExpiresAt: DateTime.UtcNow.AddMinutes(-1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddPromoGrantRequest.ExpiresAt));
    }

    [Fact]
    public void GivenPromoGrantRequest_WhenAmountAndExpiresAtAreValid_ThenValidationSucceeds()
    {
        var validator = new AddPromoGrantRequestValidator();

        var result = validator.Validate(new AddPromoGrantRequest(
            Amount: 10,
            ExpiresAt: DateTime.UtcNow.AddMinutes(1)));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void GivenConsumePromoRequest_WhenValidated_ThenAmountMustBePositive(decimal amount, bool expectedIsValid)
    {
        var validator = new ConsumePromoRequestValidator();

        var result = validator.Validate(new ConsumePromoRequest(amount));

        Assert.Equal(expectedIsValid, result.IsValid);
    }
}
