using AutoMapper;
using MediatR;
using Wallet.Api.Endpoints.Common;
using Wallet.Application.Abstractions;
using Wallet.Application.Wallets.Commands.CancelReservation;
using Wallet.Application.Wallets.Commands.ConfirmReservation;
using Wallet.Application.Wallets.Commands.Reserve;
using Wallet.Contracts.Enums;
using Wallet.Contracts.Requests;
using Wallet.Domain.Enums;

namespace Wallet.Api.Endpoints.Wallet;

public static class ReservationEndpoints
{
    public static RouteGroupBuilder MapReservationRoutes(this RouteGroupBuilder group)
    {
        group.MapPost("/{walletId:long}/services/{serviceType}/reservations", Reserve)
            .RequireIdempotencyKey()
            .Validate<ReserveRequest>();

        group.MapPost("/{walletId:long}/reservations/{reservationId:long}/confirm", ConfirmReservation)
            .RequireIdempotencyKey();

        group.MapPost("/{walletId:long}/reservations/{reservationId:long}/cancel", CancelReservation)
            .RequireIdempotencyKey();

        return group;
    }

    private static async Task<IResult> Reserve(
        long walletId,
        ContractWalletServiceType serviceType,
        ReserveRequest request,
        IMediator mediator,
        IMapper mapper,
        IRouteInfo routeInfo,
        HttpContext context,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new ReserveCommand(
                WalletId: walletId,
                ServiceType: mapper.Map<DomainWalletServiceType>(serviceType),
                Amount: request.Amount,
                IdempotencyKey: context.GetIdempotencyKey(),
                RouteInfo: routeInfo), ct);

        return result.ToCreatedCommandResult(
            response => $"/api/v1/wallet/{response.WalletId}/reservations/{response.ReservationId}");
    }

    private static async Task<IResult> ConfirmReservation(
        long walletId,
        long reservationId,
        IMediator mediator,
        IRouteInfo routeInfo,
        HttpContext context,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new ConfirmReservationCommand(
                WalletId: walletId,
                ReservationId: reservationId,
                IdempotencyKey: context.GetIdempotencyKey(),
                RouteInfo: routeInfo), ct);

        return result.ToCommandResult();
    }

    private static async Task<IResult> CancelReservation(
        long walletId,
        long reservationId,
        IMediator mediator,
        IRouteInfo routeInfo,
        HttpContext context,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new CancelReservationCommand(
                WalletId: walletId,
                ReservationId: reservationId,
                IdempotencyKey: context.GetIdempotencyKey(),
                RouteInfo: routeInfo), ct);

        return result.ToCommandResult();
    }
}
