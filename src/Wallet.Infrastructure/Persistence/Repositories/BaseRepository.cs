using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Persistence;
using Wallet.Domain.Common;

namespace Wallet.Infrastructure.Persistence.Repositories;

public class BaseRepository<TEntity>(WalletDbContext context) : IBaseRepository<TEntity>
    where TEntity : BaseEntity
{
    protected readonly WalletDbContext Context = context;
    private readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(long id, CancellationToken ct)
    {
        return await _dbSet
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<List<TEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _dbSet
            .ToListAsync(ct);
    }

    public async Task<List<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct)
    {
        return await _dbSet
            .Where(predicate)
            .ToListAsync(ct);
    }

    public async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct)
    {
        return await _dbSet
            .AnyAsync(predicate, ct);
    }

    public async Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct)
    {
        return await _dbSet
            .CountAsync(predicate, ct);
    }

    public async Task AddAsync(TEntity entity, CancellationToken ct)
    {
        await _dbSet.AddAsync(entity, ct);
    }

    public async Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken ct)
    {
        await _dbSet.AddRangeAsync(entities, ct);
    }

    public Task UpdateAsync(TEntity entity, CancellationToken ct)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(TEntity entity, CancellationToken ct)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public Task RemoveRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken ct)
    {
        _dbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }
}
