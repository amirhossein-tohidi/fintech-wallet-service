using Wallet.Domain.Aggregates;

namespace Wallet.Application.Abstractions.Persistence.Repositories;

public interface IWalletRepository : IBaseRepository<UserWallet>
{
    Task<UserWallet?> GetByUserIdAsync(Guid userId);

    Task AddReservationAsync(Reservation reservation, CancellationToken ct);
}
