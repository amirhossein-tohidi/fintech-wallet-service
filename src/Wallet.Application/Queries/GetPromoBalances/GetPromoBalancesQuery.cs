using MediatR;
using Wallet.Contracts.Enums;
using Wallet.Contracts.Responses;

namespace Wallet.Application.Queries.GetPromoBalances;

public record GetPromoBalancesQuery(
    long WalletId,
    ContractWalletServiceType? ServiceType = null) : IRequest<IReadOnlyCollection<PromoBalanceResponse>>;
