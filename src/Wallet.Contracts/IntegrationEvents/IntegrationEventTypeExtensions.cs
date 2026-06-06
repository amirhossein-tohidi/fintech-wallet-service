using Wallet.Contracts.Enums;

namespace Wallet.Contracts.IntegrationEvents;

public static class IntegrationEventTypeExtensions
{
    public static Type GetPayloadType(this IntegrationEventType eventType)
    {
        return eventType switch
        {
            IntegrationEventType.LedgerTransactionCreated => typeof(LedgerTransactionCreatedEvent),
            IntegrationEventType.WalletBalanceChanged => typeof(WalletBalanceChangedEvent),
            IntegrationEventType.WalletRefunded => typeof(WalletRefundedEvent),
            IntegrationEventType.ReservationCreated => typeof(ReservationCreatedEvent),
            IntegrationEventType.ReservationConfirmed => typeof(ReservationConfirmedEvent),
            IntegrationEventType.ReservationCancelled => typeof(ReservationCancelledEvent),
            IntegrationEventType.ReservationExpired => typeof(ReservationExpiredEvent),
            IntegrationEventType.PromoGrantAdded => typeof(PromoGrantAddedEvent),
            IntegrationEventType.PromoConsumed => typeof(PromoConsumedEvent),
            _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, "Unsupported integration event type.")
        };
    }
}
