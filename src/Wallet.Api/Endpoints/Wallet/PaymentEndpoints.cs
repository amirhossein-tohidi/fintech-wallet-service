using AutoMapper;
using MediatR;
using Wallet.Api.Endpoints.Common;
using Wallet.Application.Abstractions;
using Wallet.Application.Wallets.Commands.FastPay;
using Wallet.Application.Wallets.Commands.Refund;
using Wallet.Contracts.Enums;
using Wallet.Contracts.Requests;
using Wallet.Domain.Enums;

namespace Wallet.Api.Endpoints.Wallet;

public static class PaymentEndpoints
{
    public static RouteGroupBuilder MapPaymentRoutes(this RouteGroupBuilder group)
    {
        group.MapPost("/{walletId:long}/services/{serviceType}/fast-pay", FastPay)
            .RequireIdempotencyKey()
            .Validate<FastPayRequest>();

        group.MapPost("/{walletId:long}/services/{serviceType}/refunds", Refund)
            .RequireIdempotencyKey()
            .Validate<RefundRequest>();

        return group;
    }

    private static async Task<IResult> FastPay(
        long walletId,
        ContractWalletServiceType serviceType,
        FastPayRequest request,
        IMediator mediator,
        IMapper mapper,
        IRouteInfo routeInfo,
        HttpContext context,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new FastPayCommand(
                WalletId: walletId,
                ServiceType: mapper.Map<DomainWalletServiceType>(serviceType),
                Amount: request.Amount,
                IdempotencyKey: context.GetIdempotencyKey(),
                RouteInfo: routeInfo), ct);

        return result.ToCommandResult();
    }

    private static async Task<IResult> Refund(
        long walletId,
        ContractWalletServiceType serviceType,
        RefundRequest request,
        IMediator mediator,
        IMapper mapper,
        IRouteInfo routeInfo,
        HttpContext context,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new RefundCommand(
                WalletId: walletId,
                ServiceType: mapper.Map<DomainWalletServiceType>(serviceType),
                Amount: request.Amount,
                IdempotencyKey: context.GetIdempotencyKey(),
                RouteInfo: routeInfo), ct);

        return result.ToCommandResult();
    }
}
