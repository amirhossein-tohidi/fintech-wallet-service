using Wallet.PropertyTests.Support;

namespace Wallet.PropertyTests.Domain;

public class LedgerTransactionProperties
{
    [Property(Arbitrary = [typeof(WalletPropertyArbitraries)], MaxTest = 200)]
    public void Ledger_transaction_entries_are_always_balanced(LedgerCase testCase)
    {
        var transaction = CreateTransaction(testCase);

        var debits = transaction.Entries
            .Where(x => x.Direction == EntryDirection.Debit)
            .Sum(x => x.Amount);

        var credits = transaction.Entries
            .Where(x => x.Direction == EntryDirection.Credit)
            .Sum(x => x.Amount);

        Assert.Equal(debits, credits);
        Assert.Equal(testCase.Amount, transaction.Amount);
        Assert.All(transaction.Entries, entry => Assert.Equal(testCase.Amount, entry.Amount));
        Assert.Equal(2, transaction.Entries.Count);
    }

    private static LedgerTransaction CreateTransaction(LedgerCase testCase)
        => testCase.Type switch
        {
            LedgerTransactionType.TopUp => LedgerTransaction.TopUp(
                testCase.WalletId,
                testCase.Amount,
                testCase.IdempotencyKey),

            LedgerTransactionType.Payment => LedgerTransaction.Payment(
                testCase.WalletId,
                testCase.ServiceType,
                testCase.Amount,
                testCase.IdempotencyKey),

            LedgerTransactionType.Hold => LedgerTransaction.Hold(
                testCase.WalletId,
                testCase.ReferenceId,
                testCase.ServiceType,
                testCase.Amount,
                testCase.IdempotencyKey),

            LedgerTransactionType.Capture => LedgerTransaction.Capture(
                testCase.WalletId,
                testCase.ReferenceId,
                testCase.ServiceType,
                testCase.Amount,
                testCase.IdempotencyKey),

            LedgerTransactionType.Release => LedgerTransaction.Release(
                testCase.WalletId,
                testCase.ReferenceId,
                testCase.ServiceType,
                testCase.Amount,
                testCase.IdempotencyKey),

            LedgerTransactionType.Refund => LedgerTransaction.Refund(
                testCase.WalletId,
                testCase.ServiceType,
                testCase.Amount,
                testCase.IdempotencyKey),

            LedgerTransactionType.PromoConsume => LedgerTransaction.PromoConsume(
                testCase.WalletId,
                testCase.ServiceType,
                testCase.Amount,
                testCase.IdempotencyKey),

            _ => throw new ArgumentOutOfRangeException(nameof(testCase), testCase.Type, "Unsupported transaction type.")
        };
}
