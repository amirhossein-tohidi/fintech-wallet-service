using Wallet.Domain.Events.Abstractions;

namespace Wallet.Domain.Events.Reservation;

public record ReservationConfirmed(Guid UserId, long WalletId, long ReservationId) : BaseDomainEvent;