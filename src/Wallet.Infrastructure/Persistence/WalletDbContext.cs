using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Wallet.Application.Abstractions.Persistence;
using Wallet.Domain.Aggregates;
using Wallet.Domain.Common;
using Wallet.Domain.Events.Abstractions;
using Wallet.Domain.Idempotency;
using Wallet.Infrastructure.Messaging;
using Wallet.Infrastructure.Persistence.Messaging;

namespace Wallet.Infrastructure.Persistence;

public class WalletDbContext(DbContextOptions<WalletDbContext> options)
    : DbContext(options), IApplicationDbContext, IUnitOfWork
{
    private IDbContextTransaction? _currentTransaction;

    public DbSet<UserWallet> UserWallets => Set<UserWallet>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<PromoGrant> PromoGrants => Set<PromoGrant>();
    public DbSet<LedgerTransaction> LedgerTransactions => Set<LedgerTransaction>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<IdempotencyRequest> IdempotencyRequests => Set<IdempotencyRequest>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        AddOutboxMessages();

        return await base.SaveChangesAsync(ct);
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction != null)
        {
            return;
        }

        _currentTransaction = await Database.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_currentTransaction == null)
        {
            return;
        }

        try
        {
            await SaveChangesAsync(ct);
            await _currentTransaction.CommitAsync(ct);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_currentTransaction == null)
        {
            return;
        }

        try
        {
            await _currentTransaction.RollbackAsync(ct);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task<int> ExecuteTransactionAsync(Func<Task<int>> action, CancellationToken ct = default)
    {
        await BeginTransactionAsync(ct);

        try
        {
            var result = await action();
            await CommitAsync(ct);

            return result;
        }
        catch
        {
            await RollbackAsync(ct);
            throw;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(WalletDbContext).Assembly);
    }

    private void AddOutboxMessages()
    {
        ChangeTracker.DetectChanges();

        var aggregates = ChangeTracker
            .Entries<AggregateRoot>()
            .Select(x => x.Entity)
            .Where(x => x.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = aggregates
            .SelectMany(x => x.DomainEvents.OfType<IDomainEvent>())
            .ToList();

        if (domainEvents.Count == 0)
        {
            return;
        }

        foreach (var domainEvent in domainEvents)
        {
            var message = IntegrationEventMapper.Map(domainEvent);
            OutboxMessages.Add(new OutboxMessage(
                eventType: message.EventType,
                payload: message.Payload,
                occurredOn: message.OccurredOn));
        }

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }
    }
}
