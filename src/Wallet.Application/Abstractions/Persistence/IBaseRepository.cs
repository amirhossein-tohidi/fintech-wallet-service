using System.Linq.Expressions;
using Wallet.Domain.Common;

namespace Wallet.Application.Abstractions.Persistence;

public interface IBaseRepository<TEntity>
    where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(long id, CancellationToken ct);

    Task<List<TEntity>> GetAllAsync(CancellationToken ct);

    Task<List<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct);

    Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct);

    Task AddAsync(TEntity entity, CancellationToken ct);

    Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken ct);

    Task UpdateAsync(TEntity entity, CancellationToken ct);

    Task RemoveAsync(TEntity entity, CancellationToken ct);

    Task RemoveRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken ct);
}
