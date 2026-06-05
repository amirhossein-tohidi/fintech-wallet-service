using Grpc.Core;
using Wallet.Api.Grpc.Common;
using Wallet.Api.Grpc.Mapping;
using Wallet.Application.Wallets.Commands.TopUpWallet;
using Wallet.Contracts.Requests;
using Wallet.GrpcContracts;

namespace Wallet.Api.Grpc.Wallet;

public sealed partial class WalletGrpcService
{
    public override async Task<WalletTransactionResultGrpcResponse> TopUp(
        TopUpWalletGrpcRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
        {
            throw GrpcExceptionMapper.ValidationFailed("UserId must be a valid GUID.");
        }

        var walletRequest = new TopUpWalletRequest(ParseAmount(request.Amount));
        await ValidateAsync(_topUpValidator, walletRequest, context.CancellationToken);

        var result = await _mediator.Send(
            new TopUpWalletCommand(
                UserId: userId,
                Amount: walletRequest.Amount,
                IdempotencyKey: GetIdempotencyKey(context),
                RouteInfo: GetRouteInfo(context)),
            context.CancellationToken);

        return result.IsSuccess
            ? result.Value!.ToGrpc()
            : throw GrpcExceptionMapper.ToRpcException(result.Error);
    }
}
