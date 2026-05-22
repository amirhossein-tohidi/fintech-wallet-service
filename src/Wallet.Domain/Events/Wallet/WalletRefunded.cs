using Wallet.Domain.Events.Abstractions;

namespace Wallet.Domain.Events.Wallet;

public record WalletRefunded(Guid UserId, long WalletId, decimal Amount) : BaseDomainEvent;