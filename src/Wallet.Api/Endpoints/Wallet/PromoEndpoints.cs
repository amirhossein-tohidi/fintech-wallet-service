using AutoMapper;
using MediatR;
using Wallet.Api.Endpoints.Common;
using Wallet.Application.Abstractions;
using Wallet.Application.Queries.GetPromoBalances;
using Wallet.Application.Wallets.Commands.AddPromoGrant;
using Wallet.Application.Wallets.Commands.ConsumePromo;
using Wallet.Contracts.Enums;
using Wallet.Contracts.Requests;
using Wallet.Domain.Enums;

namespace Wallet.Api.Endpoints.Wallet;

public static class PromoEndpoints
{
    public static RouteGroupBuilder MapPromoRoutes(this RouteGroupBuilder group)
    {
        group.MapPost("/{walletId:long}/services/{serviceType}/promo-grants", AddPromoGrant)
            .RequireIdempotencyKey()
            .Validate<AddPromoGrantRequest>();

        group.MapPost("/{walletId:long}/services/{serviceType}/promo-consumptions", ConsumePromo)
            .RequireIdempotencyKey()
            .Validate<ConsumePromoRequest>();

        group.MapGet("/{walletId:long}/services/{serviceType}/promo-balances", GetPromoBalances);

        return group;
    }

    private static async Task<IResult> AddPromoGrant(
        long walletId,
        ContractWalletServiceType serviceType,
        AddPromoGrantRequest request,
        IMediator mediator,
        IMapper mapper,
        IRouteInfo routeInfo,
        HttpContext context,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new AddPromoGrantCommand(
                WalletId: walletId,
                ServiceType: mapper.Map<DomainWalletServiceType>(serviceType),
                Amount: request.Amount,
                ExpiresAt: request.ExpiresAt,
                IdempotencyKey: context.GetIdempotencyKey(),
                RouteInfo: routeInfo), ct);

        return result.ToCreatedCommandResult(
            response => $"/api/v1/promo/{response.WalletId}/services/{response.ServiceType}/promo-balances");
    }

    private static async Task<IResult> ConsumePromo(
        long walletId,
        ContractWalletServiceType serviceType,
        ConsumePromoRequest request,
        IMediator mediator,
        IMapper mapper,
        IRouteInfo routeInfo,
        HttpContext context,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new ConsumePromoCommand(
                WalletId: walletId,
                ServiceType: mapper.Map<DomainWalletServiceType>(serviceType),
                Amount: request.Amount,
                IdempotencyKey: context.GetIdempotencyKey(),
                RouteInfo: routeInfo), ct);

        return result.ToCommandResult();
    }

    private static async Task<IResult> GetPromoBalances(
        long walletId,
        ContractWalletServiceType serviceType,
        IMediator mediator,
        CancellationToken ct)
    {
        var balances = await mediator.Send(
            new GetPromoBalancesQuery(WalletId: walletId, ServiceType: serviceType), ct);

        return Results.Ok(balances);
    }
}
