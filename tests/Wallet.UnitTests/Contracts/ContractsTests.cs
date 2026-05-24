using Wallet.Contracts.Enums;
using Wallet.Contracts.Events;
using Wallet.Contracts.Responses;
using Wallet.Domain.Exceptions;

namespace Wallet.UnitTests.Contracts;

public sealed class ContractsTests
{
    [Theory]
    [InlineData(IntegrationEventType.LedgerTransactionCreated, typeof(LedgerTransactionCreatedEvent))]
    [InlineData(IntegrationEventType.WalletBalanceChanged, typeof(WalletBalanceChangedEvent))]
    [InlineData(IntegrationEventType.WalletRefunded, typeof(WalletRefundedEvent))]
    [InlineData(IntegrationEventType.ReservationCreated, typeof(ReservationCreatedEvent))]
    [InlineData(IntegrationEventType.ReservationConfirmed, typeof(ReservationConfirmedEvent))]
    [InlineData(IntegrationEventType.ReservationCancelled, typeof(ReservationCancelledEvent))]
    [InlineData(IntegrationEventType.ReservationExpired, typeof(ReservationExpiredEvent))]
    [InlineData(IntegrationEventType.PromoGrantAdded, typeof(PromoGrantAddedEvent))]
    [InlineData(IntegrationEventType.PromoConsumed, typeof(PromoConsumedEvent))]
    public void GivenIntegrationEventType_WhenPayloadTypeIsRequested_ThenExpectedTypeIsReturned(
        IntegrationEventType eventType,
        Type expectedPayloadType)
    {
        Assert.Equal(expectedPayloadType, eventType.GetPayloadType());
    }

    [Fact]
    public void GivenUnsupportedIntegrationEventType_WhenPayloadTypeIsRequested_ThenExceptionIsThrown()
    {
        var eventType = (IntegrationEventType)999;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => eventType.GetPayloadType());

        Assert.Equal("eventType", exception.ParamName);
    }

    [Fact]
    public void GivenWalletBalanceResponse_WhenTotalIsRead_ThenAvailableAndReservedAreSummed()
    {
        var response = new WalletBalanceResponse
        {
            AvailableBalance = 75,
            ReservedBalance = 25
        };

        Assert.Equal(100, response.TotalRealBalance);
    }

    [Fact]
    public void GivenPromoBalanceResponse_WhenCalculatedPropertiesAreRead_ThenConsumedAndExpiryAreCalculated()
    {
        var response = new PromoBalanceResponse
        {
            OriginalAmount = 100,
            RemainingAmount = 40,
            ExpiresAt = DateTime.UtcNow.AddMilliseconds(-1)
        };

        Assert.Equal(60, response.ConsumedAmount);
        Assert.True(response.IsExpired);
    }

    [Fact]
    public void GivenNotFoundExceptionWithEntityAndIdentifier_WhenCreated_ThenMessageAndPropertiesAreSet()
    {
        var exception = new NotFoundException(entityName: "Wallet", identifier: 10);

        Assert.Equal("Wallet", exception.EntityName);
        Assert.Equal(10, exception.Identifier);
        Assert.Equal("Wallet with identifier '10' was not found.", exception.Message);
    }

    [Fact]
    public void GivenValidationExceptionWithProperty_WhenCreated_ThenErrorsAreSet()
    {
        var exception = new ValidationException(
            propertyName: "Amount",
            errorMessage: "Amount is required.");

        Assert.Equal("VALIDATION_ERROR", exception.ErrorCode);
        Assert.True(exception.Errors.ContainsKey("Amount"));
        Assert.Equal("Amount is required.", exception.Errors["Amount"].Single());
    }

    [Fact]
    public void GivenNotFoundExceptionWithCustomMessage_WhenCreated_ThenCustomMessageIsUsed()
    {
        var exception = new NotFoundException(
            entityName: "Wallet",
            identifier: 10,
            message: "Custom not found.");

        Assert.Equal("Wallet", exception.EntityName);
        Assert.Equal(10, exception.Identifier);
        Assert.Equal("Custom not found.", exception.Message);
    }

    [Fact]
    public void GivenValidationExceptionWithDictionary_WhenCreated_ThenErrorsAreStored()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Amount"] = ["Amount must be positive."]
        };

        var exception = new ValidationException(errors);

        Assert.Same(errors, exception.Errors);
        Assert.Equal("One or more validation failures have occurred.", exception.Message);
    }

    [Fact]
    public void GivenResponseRecords_WhenCreated_ThenValuesAreExposed()
    {
        var reservation = new ReservationOperationResponse(
            WalletId: 1,
            ReservationId: 2,
            TransactionId: 3,
            ServiceType: ContractWalletServiceType.Shop,
            Amount: 100,
            ExpiresAt: DateTime.UtcNow.AddMinutes(9),
            Status: "Created",
            AvailableBalance: 900,
            ReservedBalance: 100);
        var transaction = new WalletTransactionResultResponse(
            WalletId: 1,
            TransactionId: 2,
            ServiceType: ContractWalletServiceType.Travel,
            TransactionType: "Payment",
            Amount: 100,
            AvailableBalance: 900,
            ReservedBalance: 0);
        var promoGrant = new PromoGrantOperationResponse(
            WalletId: 1,
            PromoGrantId: 2,
            ServiceType: ContractWalletServiceType.Food,
            OriginalAmount: 100,
            RemainingAmount: 80,
            ExpiresAt: DateTime.UtcNow.AddDays(1));
        var transactionHistory = new TransactionResponse(
            TransactionId: 1,
            WalletId: 2,
            TransactionType: ContractLedgerTransactionType.Payment,
            ServiceType: ContractWalletServiceType.Travel,
            Amount: 100,
            ReferenceId: null,
            CreatedAt: DateTime.UtcNow);

        Assert.Equal(100, reservation.Amount);
        Assert.Equal("Payment", transaction.TransactionType);
        Assert.Equal(80, promoGrant.RemainingAmount);
        Assert.Equal(ContractLedgerTransactionType.Payment, transactionHistory.TransactionType);
    }

    [Fact]
    public void GivenIntegrationEvents_WhenCreated_ThenPayloadValuesAreExposed()
    {
        var userId = Guid.NewGuid();
        var walletBalanceChanged = new WalletBalanceChangedEvent(
            UserId: userId,
            WalletId: 1,
            NewBalance: 100,
            AmountChanged: 25);
        var reservationExpired = new ReservationExpiredEvent(
            UserId: userId,
            WalletId: 1,
            ReservationId: 2);
        var envelope = new IntegrationEventEnvelope<WalletBalanceChangedEvent>(
            Id: Guid.NewGuid(),
            Type: IntegrationEventType.WalletBalanceChanged,
            OccurredOn: DateTime.UtcNow,
            Payload: walletBalanceChanged);

        Assert.Equal(25, walletBalanceChanged.AmountChanged);
        Assert.Equal(2, reservationExpired.ReservationId);
        Assert.Equal(IntegrationEventType.WalletBalanceChanged, envelope.Type);
        Assert.Same(walletBalanceChanged, envelope.Payload);
    }
}
