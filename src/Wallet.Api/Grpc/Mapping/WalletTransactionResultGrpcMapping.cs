using Wallet.Contracts.Responses;
using Wallet.GrpcContracts;

namespace Wallet.Api.Grpc.Mapping;

public static class WalletTransactionResultGrpcMapping
{
    public static WalletTransactionResultGrpcResponse ToGrpc(this WalletTransactionResultResponse response)
    {
        return new WalletTransactionResultGrpcResponse
        {
            WalletId = response.WalletId,
            TransactionId = response.TransactionId,
            ServiceType = response.ServiceType.ToGrpc(),
            TransactionType = response.TransactionType,
            Amount = GrpcMappingHelpers.FormatDecimal(response.Amount),
            AvailableBalance = GrpcMappingHelpers.FormatDecimal(response.AvailableBalance),
            ReservedBalance = GrpcMappingHelpers.FormatDecimal(response.ReservedBalance)
        };
    }
}
