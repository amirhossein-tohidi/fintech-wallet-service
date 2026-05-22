using WalletAggregate = Wallet.Domain.Aggregates.UserWallet;
using Wallet.Domain.Events.Abstractions;

namespace Wallet.Domain.Events.Wallet;

public record WalletBalanceChanged(WalletAggregate Wallet, decimal AmountChanged) : BaseDomainEvent
{
    public Guid UserId => Wallet.UserId;
    public long WalletId => Wallet.Id;
    public decimal NewBalance => Wallet.AvailableBalance;
}
