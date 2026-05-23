using MediatR;
using Wallet.Application.Abstractions.ReadModels;
using Wallet.Contracts.Responses;

namespace Wallet.Application.Queries.GetTransactions;

public class GetTransactionsHandler(IWalletReadRepository readRepository)
    : IRequestHandler<GetTransactionsQuery, IReadOnlyCollection<TransactionResponse>>
{
    public Task<IReadOnlyCollection<TransactionResponse>> Handle(GetTransactionsQuery request, CancellationToken ct)
    {
        return readRepository.GetTransactionsAsync(
            walletId: request.WalletId,
            serviceType: request.ServiceType,
            ct: ct);
    }
}
