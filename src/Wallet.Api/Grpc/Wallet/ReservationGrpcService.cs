using Grpc.Core;
using Wallet.Api.Grpc.Common;
using Wallet.Api.Grpc.Mapping;
using Wallet.Application.Mapping;
using Wallet.Application.Wallets.Commands.CancelReservation;
using Wallet.Application.Wallets.Commands.ConfirmReservation;
using Wallet.Application.Wallets.Commands.Reserve;
using Wallet.Contracts.Requests;
using Wallet.GrpcContracts;

namespace Wallet.Api.Grpc.Wallet;

public sealed partial class WalletGrpcService
{
    public override async Task<ReservationOperationGrpcResponse> Reserve(
        ReserveGrpcRequest request,
        ServerCallContext context)
    {
        var walletRequest = new ReserveRequest(ParseAmount(request.Amount));
        await ValidateAsync(_reserveValidator, walletRequest, context.CancellationToken);

        var result = await _mediator.Send(
            new ReserveCommand(
                WalletId: request.WalletId,
                ServiceType: request.ServiceType.ToContract().ToDomain(),
                Amount: walletRequest.Amount,
                IdempotencyKey: GetIdempotencyKey(context),
                RouteInfo: GetRouteInfo(context)),
            context.CancellationToken);

        return result.IsSuccess
            ? result.Value!.ToGrpc()
            : throw GrpcExceptionMapper.ToRpcException(result.Error);
    }

    public override async Task<ReservationOperationGrpcResponse> ConfirmReservation(
        ConfirmReservationGrpcRequest request,
        ServerCallContext context)
    {
        var result = await _mediator.Send(
            new ConfirmReservationCommand(
                WalletId: request.WalletId,
                ReservationId: request.ReservationId,
                IdempotencyKey: GetIdempotencyKey(context),
                RouteInfo: GetRouteInfo(context)),
            context.CancellationToken);

        return result.IsSuccess
            ? result.Value!.ToGrpc()
            : throw GrpcExceptionMapper.ToRpcException(result.Error);
    }

    public override async Task<ReservationOperationGrpcResponse> CancelReservation(
        CancelReservationGrpcRequest request,
        ServerCallContext context)
    {
        var result = await _mediator.Send(
            new CancelReservationCommand(
                WalletId: request.WalletId,
                ReservationId: request.ReservationId,
                IdempotencyKey: GetIdempotencyKey(context),
                RouteInfo: GetRouteInfo(context)),
            context.CancellationToken);

        return result.IsSuccess
            ? result.Value!.ToGrpc()
            : throw GrpcExceptionMapper.ToRpcException(result.Error);
    }
}
