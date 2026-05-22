namespace Wallet.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
    Task<int> ExecuteTransactionAsync(Func<Task<int>> action, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    
}