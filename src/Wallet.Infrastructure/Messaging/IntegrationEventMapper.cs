using System.Text.Json;
using Wallet.Application.Mapping;
using Wallet.Contracts.Enums;
using Wallet.Contracts.IntegrationEvents;
using Wallet.Domain.Events.Abstractions;
using Wallet.Domain.Events.Ledger;
using Wallet.Domain.Events.Promotion;
using Wallet.Domain.Events.Reservation;
using Wallet.Domain.Events.Wallet;

namespace Wallet.Infrastructure.Messaging;

public static class IntegrationEventMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IntegrationEventMessage Map(IDomainEvent domainEvent)
    {
        var payload = ToContract(domainEvent);
        var eventType = ToEventType(domainEvent);
        var envelopeType = typeof(IntegrationEventEnvelope<>).MakeGenericType(payload.GetType());
        var envelope = Activator.CreateInstance(
            envelopeType,
            Guid.NewGuid(),
            eventType,
            domainEvent.OccurredOn,
            payload);

        return new IntegrationEventMessage(
            EventType: eventType,
            Payload: JsonSerializer.Serialize(envelope, envelopeType, JsonOptions),
            OccurredOn: domainEvent.OccurredOn);
    }

    private static object ToContract(IDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            LedgerTransactionCreated e => new LedgerTransactionCreatedEvent(
                UserId: e.UserId,
                WalletId: e.WalletId,
                TransactionId: e.TransactionId,
                ServiceType: e.ServiceType.ToContract(),
                Amount: e.Amount,
                Type: e.Type.ToString()),

            WalletBalanceChanged e => new WalletBalanceChangedEvent(
                UserId: e.UserId,
                WalletId: e.WalletId,
                NewBalance: e.NewBalance,
                AmountChanged: e.AmountChanged),

            WalletRefunded e => new WalletRefundedEvent(
                UserId: e.UserId,
                WalletId: e.WalletId,
                Amount: e.Amount),

            ReservationCreated e => new ReservationCreatedEvent(
                UserId: e.UserId,
                WalletId: e.WalletId,
                ReservationId: e.ReservationId,
                Amount: e.Amount),

            ReservationConfirmed e => new ReservationConfirmedEvent(
                UserId: e.UserId,
                WalletId: e.WalletId,
                ReservationId: e.ReservationId),

            ReservationCancelled e => new ReservationCancelledEvent(
                UserId: e.UserId,
                WalletId: e.WalletId,
                ReservationId: e.ReservationId),

            ReservationExpired e => new ReservationExpiredEvent(
                UserId: e.UserId,
                WalletId: e.WalletId,
                ReservationId: e.ReservationId),

            PromoGrantAdded e => new PromoGrantAddedEvent(
                UserId: e.UserId,
                WalletId: e.WalletId,
                PromoId: e.PromoId,
                ServiceType: e.ServiceType.ToContract(),
                Amount: e.Amount,
                ExpireAt: e.ExpireAt),

            PromoConsumed e => new PromoConsumedEvent(
                UserId: e.UserId,
                WalletId: e.WalletId,
                ServiceType: e.ServiceType.ToContract(),
                Amount: e.Amount),

            _ => throw new InvalidOperationException(
                $"Domain event {domainEvent.GetType().Name} does not have an integration contract.")
        };
    }

    private static IntegrationEventType ToEventType(IDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            LedgerTransactionCreated => IntegrationEventType.LedgerTransactionCreated,
            WalletBalanceChanged => IntegrationEventType.WalletBalanceChanged,
            WalletRefunded => IntegrationEventType.WalletRefunded,
            ReservationCreated => IntegrationEventType.ReservationCreated,
            ReservationConfirmed => IntegrationEventType.ReservationConfirmed,
            ReservationCancelled => IntegrationEventType.ReservationCancelled,
            ReservationExpired => IntegrationEventType.ReservationExpired,
            PromoGrantAdded => IntegrationEventType.PromoGrantAdded,
            PromoConsumed => IntegrationEventType.PromoConsumed,
            _ => throw new InvalidOperationException(
                $"Domain event {domainEvent.GetType().Name} does not have an integration event type.")
        };
    }
}

public sealed record IntegrationEventMessage(
    IntegrationEventType EventType,
    string Payload,
    DateTime OccurredOn);
