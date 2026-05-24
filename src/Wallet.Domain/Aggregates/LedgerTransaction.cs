using Wallet.Domain.Common;
using Wallet.Domain.Enums;

namespace Wallet.Domain.Aggregates;

public class LedgerTransaction : BaseEntity
{
    public long WalletId { get; private set; }
    public LedgerTransactionType Type { get; private set; }
    public DomainWalletServiceType ServiceType { get; private set; }
    public string IdempotencyKey { get; private set; } = "";
    public decimal Amount { get; private set; }
    public long? ReferenceId { get; private set; }

    private readonly List<LedgerEntry> _entries = new();
    public IReadOnlyCollection<LedgerEntry> Entries => _entries.AsReadOnly();

    private LedgerTransaction()
    {
    }

    private LedgerTransaction(
        long walletId,
        LedgerTransactionType type,
        DomainWalletServiceType serviceType,
        decimal amount,
        string idem,
        long? reference)
    {
        WalletId = walletId;
        Type = type;
        ServiceType = serviceType;
        Amount = amount;
        IdempotencyKey = idem;
        ReferenceId = reference;
    }

    public static LedgerTransaction TopUp(long walletId, decimal amount, string idem)
    {
        var tx = new LedgerTransaction(walletId, LedgerTransactionType.TopUp, DomainWalletServiceType.General, amount, idem, null);

        tx.AddEntry(account: AccountType.Cash, direction: EntryDirection.Credit, amount: amount);
        tx.AddEntry(account: AccountType.Wallet, direction: EntryDirection.Debit, amount: amount);
        return tx;
    }

    public static LedgerTransaction Payment(long walletId, DomainWalletServiceType serviceType, decimal amount, string idem)
    {
        var tx = new LedgerTransaction(walletId, LedgerTransactionType.Payment, serviceType, amount, idem, null);

        tx.AddEntry(account: AccountType.Wallet, direction: EntryDirection.Credit, amount: amount);
        tx.AddEntry(account: AccountType.Spent, direction: EntryDirection.Debit, amount: amount);
        return tx;
    }

    public static LedgerTransaction Hold(long walletId, long reservationId, DomainWalletServiceType serviceType, decimal amount, string idem)
    {
        var tx = new LedgerTransaction(walletId, LedgerTransactionType.Hold, serviceType, amount, idem, reservationId);

        tx.AddEntry(account: AccountType.Wallet, direction: EntryDirection.Credit, amount: amount);
        tx.AddEntry(account: AccountType.Reserved, direction: EntryDirection.Debit, amount: amount);
        return tx;
    }

    public static LedgerTransaction Capture(long walletId, long reservationId, DomainWalletServiceType serviceType, decimal amount, string idem)
    {
        var tx = new LedgerTransaction(walletId, LedgerTransactionType.Capture, serviceType, amount, idem, reservationId);

        tx.AddEntry(account: AccountType.Reserved, direction: EntryDirection.Credit, amount: amount);
        tx.AddEntry(account: AccountType.Spent, direction: EntryDirection.Debit, amount: amount);
        return tx;
    }

    public static LedgerTransaction Release(long walletId, long reservationId, DomainWalletServiceType serviceType, decimal amount, string idem)
    {
        var tx = new LedgerTransaction(walletId, LedgerTransactionType.Release, serviceType, amount, idem, reservationId);

        tx.AddEntry(account: AccountType.Reserved, direction: EntryDirection.Credit, amount: amount);
        tx.AddEntry(account: AccountType.Wallet, direction: EntryDirection.Debit, amount: amount);
        return tx;
    }

    public static LedgerTransaction Refund(long walletId, DomainWalletServiceType serviceType, decimal amount, string idem)
    {
        var tx = new LedgerTransaction(walletId, LedgerTransactionType.Refund, serviceType, amount, idem, null);

        tx.AddEntry(account: AccountType.Spent, direction: EntryDirection.Credit, amount: amount);
        tx.AddEntry(account: AccountType.Wallet, direction: EntryDirection.Debit, amount: amount);
        return tx;
    }

    public static LedgerTransaction PromoConsume(long walletId, DomainWalletServiceType serviceType, decimal amount, string idem)
    {
        var tx = new LedgerTransaction(walletId, LedgerTransactionType.PromoConsume, serviceType, amount, idem, null);

        tx.AddEntry(account: AccountType.Promo, direction: EntryDirection.Credit, amount: amount);
        tx.AddEntry(account: AccountType.Spent, direction: EntryDirection.Debit, amount: amount);
        return tx;
    }

    private void AddEntry(AccountType account, EntryDirection direction, decimal amount)
        => _entries.Add(new LedgerEntry(account: account, direction: direction, amount: amount));
}
