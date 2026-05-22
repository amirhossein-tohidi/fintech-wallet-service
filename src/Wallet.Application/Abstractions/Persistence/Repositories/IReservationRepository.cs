using Wallet.Domain.Aggregates;

namespace Wallet.Application.Abstractions.Persistence.Repositories;

public interface IReservationRepository : IBaseRepository<Reservation>
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct);
}
