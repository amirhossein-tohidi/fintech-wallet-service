using MediatR;
using Wallet.Api.Endpoints.Common;
using Wallet.Application.Queries.GetTransactions;
using Wallet.Application.Queries.GetWalletBalance;
using Wallet.Contracts.Enums;
using Wallet.Contracts.Responses;

namespace Wallet.Api.Endpoints.Wallet;

public static class WalletReportEndpoints
{
    public static RouteGroupBuilder MapWalletReportRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/{walletId:long}/balance", GetBalance);
        group.MapGet("/{walletId:long}/services/{serviceType}/transactions", GetTransactions);

        return group;
    }

    private static async Task<IResult> GetBalance(long walletId, IMediator mediator, CancellationToken ct)
    {
        var balance = await mediator.Send(new GetWalletBalanceQuery(WalletId: walletId), ct);

        return balance == null
            ? EndpointResultExtensions.NotFoundResult(ApiErrorCode.WalletNotFound)
            : Results.Ok(balance);
    }

    private static async Task<IResult> GetTransactions(
        long walletId,
        ContractWalletServiceType serviceType,
        IMediator mediator,
        CancellationToken ct)
    {
        var transactions = await mediator.Send(
            new GetTransactionsQuery(WalletId: walletId, ServiceType: serviceType), ct);

        return Results.Ok(transactions);
    }
}
