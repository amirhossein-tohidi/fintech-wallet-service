using Grpc.Core;
using Wallet.Api.Grpc.Common;
using Wallet.Api.Grpc.Mapping;
using Wallet.Application.Mapping;
using Wallet.Application.Wallets.Commands.FastPay;
using Wallet.Application.Wallets.Commands.Refund;
using Wallet.Contracts.Requests;
using Wallet.GrpcContracts;

namespace Wallet.Api.Grpc.Wallet;

public sealed partial class WalletGrpcService
{
    public override async Task<WalletTransactionResultGrpcResponse> FastPay(
        FastPayGrpcRequest request,
        ServerCallContext context)
    {
        var walletRequest = new FastPayRequest(ParseAmount(request.Amount));
        await ValidateAsync(_fastPayValidator, walletRequest, context.CancellationToken);

        var result = await _mediator.Send(
            new FastPayCommand(
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

    public override async Task<WalletTransactionResultGrpcResponse> Refund(
        RefundGrpcRequest request,
        ServerCallContext context)
    {
        var walletRequest = new RefundRequest(ParseAmount(request.Amount));
        await ValidateAsync(_refundValidator, walletRequest, context.CancellationToken);

        var result = await _mediator.Send(
            new RefundCommand(
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
}
