using ReservationAggregate = Wallet.Domain.Aggregates.Reservation;
using Wallet.Domain.Events.Abstractions;

namespace Wallet.Domain.Events.Reservation;

public record ReservationCreated(Guid UserId, long WalletId, ReservationAggregate Reservation) : BaseDomainEvent
{
    public long ReservationId => Reservation.Id;
    public decimal Amount => Reservation.Amount;
}
