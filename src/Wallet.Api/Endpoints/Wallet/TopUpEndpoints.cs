using MediatR;
using Wallet.Api.Endpoints.Common;
using Wallet.Application.Abstractions;
using Wallet.Application.Wallets.Commands.TopUpWallet;
using Wallet.Contracts.Requests;

namespace Wallet.Api.Endpoints.Wallet;

public static class TopUpEndpoints
{
    public static RouteGroupBuilder MapTopUpRoutes(this RouteGroupBuilder group)
    {
        group.MapPost("/users/{userId:guid}/topups", TopUp)
            .RequireIdempotencyKey()
            .Validate<TopUpWalletRequest>();

        return group;
    }

    private static async Task<IResult> TopUp(
        Guid userId,
        TopUpWalletRequest request,
        IMediator mediator,
        IRouteInfo routeInfo,
        HttpContext context,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new TopUpWalletCommand(
                UserId: userId,
                Amount: request.Amount,
                IdempotencyKey: context.GetIdempotencyKey(),
                RouteInfo: routeInfo), ct);

        return result.ToCommandResult();
    }
}
