using Wallet.Domain.Events.Abstractions;

namespace Wallet.Domain.Events.Reservation;

public record ReservationExpired(Guid UserId, long WalletId, long ReservationId) : BaseDomainEvent;