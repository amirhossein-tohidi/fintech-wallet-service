using Grpc.Core;
using Wallet.Api.Grpc.Common;
using Wallet.Api.Grpc.Mapping;
using Wallet.Application.Queries.GetPromoBalances;
using Wallet.Application.Queries.GetTransactions;
using Wallet.Application.Queries.GetWalletBalance;
using Wallet.GrpcContracts;

namespace Wallet.Api.Grpc.Wallet;

public sealed partial class WalletGrpcService
{
    public override async Task<WalletBalanceGrpcResponse> GetWalletBalance(
        GetWalletBalanceGrpcRequest request,
        ServerCallContext context)
    {
        var balance = await _mediator.Send(
            new GetWalletBalanceQuery(WalletId: request.WalletId),
            context.CancellationToken);

        return balance?.ToGrpc()
               ?? throw GrpcExceptionMapper.ToRpcException("Wallet not found.");
    }

    public override async Task<GetTransactionsGrpcResponse> GetTransactions(
        GetTransactionsGrpcRequest request,
        ServerCallContext context)
    {
        var transactions = await _mediator.Send(
            new GetTransactionsQuery(
                WalletId: request.WalletId,
                ServiceType: request.ServiceType.ToContract()),
            context.CancellationToken);

        var response = new GetTransactionsGrpcResponse();
        response.Transactions.AddRange(transactions.Select(x => x.ToGrpc()));
        return response;
    }

    public override async Task<GetPromoBalancesGrpcResponse> GetPromoBalances(
        GetPromoBalancesGrpcRequest request,
        ServerCallContext context)
    {
        var balances = await _mediator.Send(
            new GetPromoBalancesQuery(
                WalletId: request.WalletId,
                ServiceType: request.ServiceType.ToContract()),
            context.CancellationToken);

        var response = new GetPromoBalancesGrpcResponse();
        response.Balances.AddRange(balances.Select(x => x.ToGrpc()));
        return response;
    }
}
