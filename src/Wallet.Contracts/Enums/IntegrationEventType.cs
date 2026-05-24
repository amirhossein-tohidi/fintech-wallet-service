namespace Wallet.Contracts.Enums;

public enum IntegrationEventType
{
    LedgerTransactionCreated = 1,
    WalletBalanceChanged = 2,
    WalletRefunded = 3,
    ReservationCreated = 4,
    ReservationConfirmed = 5,
    ReservationCancelled = 6,
    ReservationExpired = 7,
    PromoGrantAdded = 8,
    PromoConsumed = 9
}
