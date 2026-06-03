namespace Wallet.PropertyTests.Support;

using FsCheck.Fluent;
using FluentArb = FsCheck.Fluent.Arb;
using FluentGen = FsCheck.Fluent.Gen;

public static class WalletPropertyArbitraries
{
    public static Arbitrary<PositiveMoney> PositiveMoney()
        => FluentArb.From(
            from whole in FluentGen.Choose(1, 100_000)
            from cents in FluentGen.Choose(0, 99)
            select new PositiveMoney(whole + cents / 100m));

    public static Arbitrary<WalletMoneyPair> WalletMoneyPair()
        => FluentArb.From(
            from balanceWhole in FluentGen.Choose(1, 100_000)
            from balanceCents in FluentGen.Choose(0, 99)
            from spendPercent in FluentGen.Choose(1, 100)
            let balance = balanceWhole + balanceCents / 100m
            let spend = Math.Round(balance * spendPercent / 100m, 2, MidpointRounding.AwayFromZero)
            select new WalletMoneyPair(balance, spend));

    public static Arbitrary<WalletService> WalletService()
        => FluentArb.From(
            FluentGen.Elements(
                DomainWalletServiceType.General,
                DomainWalletServiceType.Travel,
                DomainWalletServiceType.Food,
                DomainWalletServiceType.Shop)
            .Select(x => new WalletService(x)));

    public static Arbitrary<LedgerCase> LedgerCase()
        => FluentArb.From(
            from amount in PositiveMoney().Generator
            from service in WalletService().Generator
            from transactionType in FluentGen.Elements(Enum.GetValues<LedgerTransactionType>())
            from walletId in FluentGen.Choose(1, 10_000)
            from referenceId in FluentGen.Choose(1, 10_000)
            select new LedgerCase(
                walletId,
                referenceId,
                transactionType,
                service.Value,
                amount.Value,
                $"idem-{walletId}-{referenceId}-{transactionType}"));
}

public sealed record PositiveMoney(decimal Value);

public sealed record WalletMoneyPair(decimal Balance, decimal OperationAmount);

public sealed record WalletService(DomainWalletServiceType Value);

public sealed record LedgerCase(
    long WalletId,
    long ReferenceId,
    LedgerTransactionType Type,
    DomainWalletServiceType ServiceType,
    decimal Amount,
    string IdempotencyKey);
