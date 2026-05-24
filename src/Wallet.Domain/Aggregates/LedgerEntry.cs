using Wallet.Domain.Common;
using Wallet.Domain.Enums;

namespace Wallet.Domain.Aggregates;

public class LedgerEntry : BaseEntity
{
    public long TransactionId { get; private set; }
    public AccountType Account { get; private set; }
    public EntryDirection Direction { get; private set; }
    public decimal Amount { get; private set; }

    private LedgerEntry() { }

    public LedgerEntry(AccountType account, EntryDirection direction, decimal amount)
    {
        Account = account;
        Direction = direction;
        Amount = amount;
    }
    
    internal void SetTransactionId(long transactionId)
    {
        TransactionId = transactionId;
    }
}