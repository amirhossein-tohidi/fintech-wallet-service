using Wallet.Contracts.Responses;
using Wallet.GrpcContracts;

namespace Wallet.Api.Grpc.Mapping;

public static class WalletBalanceGrpcMapping
{
    public static WalletBalanceGrpcResponse ToGrpc(this WalletBalanceResponse response)
    {
        return new WalletBalanceGrpcResponse
        {
            WalletId = response.WalletId,
            UserId = response.UserId.ToString(),
            AvailableBalance = GrpcMappingHelpers.FormatDecimal(response.AvailableBalance),
            ReservedBalance = GrpcMappingHelpers.FormatDecimal(response.ReservedBalance),
            TotalRealBalance = GrpcMappingHelpers.FormatDecimal(response.TotalRealBalance)
        };
    }
}
