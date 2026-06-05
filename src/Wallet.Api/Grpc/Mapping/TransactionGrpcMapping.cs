using Wallet.Contracts.Responses;
using Wallet.GrpcContracts;

namespace Wallet.Api.Grpc.Mapping;

public static class TransactionGrpcMapping
{
    public static TransactionGrpcResponse ToGrpc(this TransactionResponse response)
    {
        var grpcResponse = new TransactionGrpcResponse
        {
            TransactionId = response.TransactionId,
            WalletId = response.WalletId,
            TransactionType = response.TransactionType.ToGrpc(),
            ServiceType = response.ServiceType.ToGrpc(),
            Amount = GrpcMappingHelpers.FormatDecimal(response.Amount),
            CreatedAt = GrpcMappingHelpers.ToTimestamp(response.CreatedAt)
        };

        if (response.ReferenceId.HasValue)
        {
            grpcResponse.ReferenceId = response.ReferenceId.Value;
        }

        return grpcResponse;
    }
}
