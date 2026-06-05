using Grpc.Core;
using Wallet.Api.Grpc.Common;
using Wallet.Api.Grpc.Mapping;
using Wallet.Application.Mapping;
using Wallet.Application.Wallets.Commands.AddPromoGrant;
using Wallet.Application.Wallets.Commands.ConsumePromo;
using Wallet.Contracts.Requests;
using Wallet.GrpcContracts;

namespace Wallet.Api.Grpc.Wallet;

public sealed partial class WalletGrpcService
{
    public override async Task<PromoGrantOperationGrpcResponse> AddPromoGrant(
        AddPromoGrantGrpcRequest request,
        ServerCallContext context)
    {
        var walletRequest = new AddPromoGrantRequest(
            Amount: ParseAmount(request.Amount),
            ExpiresAt: request.ExpiresAt.ToDateTime());
        await ValidateAsync(_addPromoGrantValidator, walletRequest, context.CancellationToken);

        var result = await _mediator.Send(
            new AddPromoGrantCommand(
                WalletId: request.WalletId,
                ServiceType: request.ServiceType.ToContract().ToDomain(),
                Amount: walletRequest.Amount,
                ExpiresAt: walletRequest.ExpiresAt,
                IdempotencyKey: GetIdempotencyKey(context),
                RouteInfo: GetRouteInfo(context)),
            context.CancellationToken);

        return result.IsSuccess
            ? result.Value!.ToGrpc()
            : throw GrpcExceptionMapper.ToRpcException(result.Error);
    }

    public override async Task<WalletTransactionResultGrpcResponse> ConsumePromo(
        ConsumePromoGrpcRequest request,
        ServerCallContext context)
    {
        var walletRequest = new ConsumePromoRequest(ParseAmount(request.Amount));
        await ValidateAsync(_consumePromoValidator, walletRequest, context.CancellationToken);

        var result = await _mediator.Send(
            new ConsumePromoCommand(
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
