using MediatR;
using Wallet.Application.Abstractions.ReadModels;
using Wallet.Contracts.Responses;

namespace Wallet.Application.Queries.GetWalletBalance;

public class GetWalletBalanceHandler(IWalletReadRepository readRepository)
    : IRequestHandler<GetWalletBalanceQuery, WalletBalanceResponse?>
{
    public Task<WalletBalanceResponse?> Handle(GetWalletBalanceQuery request, CancellationToken ct)
    {
        return readRepository.GetBalanceAsync(walletId: request.WalletId, ct: ct);
    }
}
