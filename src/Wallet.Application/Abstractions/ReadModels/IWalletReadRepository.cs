using Wallet.Contracts.Enums;
using Wallet.Contracts.Responses;

namespace Wallet.Application.Abstractions.ReadModels;

public interface IWalletReadRepository
{
    Task<WalletBalanceResponse?> GetBalanceAsync(long walletId, CancellationToken ct);

    Task<IReadOnlyCollection<PromoBalanceResponse>> GetPromoBalancesAsync(
        long walletId,
        ContractWalletServiceType? serviceType,
        CancellationToken ct);

    Task<IReadOnlyCollection<TransactionResponse>> GetTransactionsAsync(
        long walletId,
        ContractWalletServiceType? serviceType,
        CancellationToken ct);
}
