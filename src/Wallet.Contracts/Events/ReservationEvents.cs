namespace Wallet.Contracts.Events;

public sealed record ReservationCreatedEvent(
    Guid UserId,
    long WalletId,
    long ReservationId,
    decimal Amount);

public sealed record ReservationConfirmedEvent(
    Guid UserId,
    long WalletId,
    long ReservationId);

public sealed record ReservationCancelledEvent(
    Guid UserId,
    long WalletId,
    long ReservationId);

public sealed record ReservationExpiredEvent(
    Guid UserId,
    long WalletId,
    long ReservationId);
