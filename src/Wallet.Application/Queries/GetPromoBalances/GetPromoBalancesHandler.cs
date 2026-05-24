using MediatR;
using Wallet.Application.Abstractions.ReadModels;
using Wallet.Contracts.Responses;

namespace Wallet.Application.Queries.GetPromoBalances;

public class GetPromoBalancesHandler(IWalletReadRepository readRepository)
    : IRequestHandler<GetPromoBalancesQuery, IReadOnlyCollection<PromoBalanceResponse>>
{
    public Task<IReadOnlyCollection<PromoBalanceResponse>> Handle(GetPromoBalancesQuery request, CancellationToken ct)
    {
        return readRepository.GetPromoBalancesAsync(
            walletId: request.WalletId,
            serviceType: request.ServiceType,
            ct: ct);
    }
}
