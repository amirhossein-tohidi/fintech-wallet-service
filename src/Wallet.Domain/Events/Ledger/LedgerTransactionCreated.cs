using Wallet.Domain.Enums;
using Wallet.Domain.Events.Abstractions;
using Wallet.Domain.Aggregates;

namespace Wallet.Domain.Events.Ledger;

public record LedgerTransactionCreated(
    Guid UserId,
    LedgerTransaction Transaction) : BaseDomainEvent
{
    public long WalletId => Transaction.WalletId;
    public long TransactionId => Transaction.Id;
    public DomainWalletServiceType ServiceType => Transaction.ServiceType;
    public decimal Amount => Transaction.Amount;
    public LedgerTransactionType Type => Transaction.Type;
}
