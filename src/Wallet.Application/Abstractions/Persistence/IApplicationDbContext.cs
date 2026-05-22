using Microsoft.EntityFrameworkCore;
using Wallet.Domain.Aggregates;
using Wallet.Domain.Idempotency;

namespace Wallet.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<UserWallet> UserWallets { get; }
    DbSet<Reservation> Reservations { get; }
    DbSet<PromoGrant> PromoGrants { get; }
    DbSet<LedgerTransaction> LedgerTransactions { get; }
    DbSet<IdempotencyRequest> IdempotencyRequests { get; }
    
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
