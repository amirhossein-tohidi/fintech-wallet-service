using Wallet.Domain.Aggregates;

namespace Wallet.Application.Abstractions.Persistence.Repositories;

public interface ILedgerTransactionRepository : IBaseRepository<LedgerTransaction>
{
}
