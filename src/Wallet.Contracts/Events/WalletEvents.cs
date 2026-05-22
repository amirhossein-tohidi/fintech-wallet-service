namespace Wallet.Contracts.Events;

public sealed record WalletBalanceChangedEvent(
    Guid UserId,
    long WalletId,
    decimal NewBalance,
    decimal AmountChanged);

public sealed record WalletRefundedEvent(
    Guid UserId,
    long WalletId,
    decimal Amount);
