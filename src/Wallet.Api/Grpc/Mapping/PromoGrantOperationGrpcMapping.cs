using Wallet.Contracts.Responses;
using Wallet.GrpcContracts;

namespace Wallet.Api.Grpc.Mapping;

public static class PromoGrantOperationGrpcMapping
{
    public static PromoGrantOperationGrpcResponse ToGrpc(this PromoGrantOperationResponse response)
    {
        return new PromoGrantOperationGrpcResponse
        {
            WalletId = response.WalletId,
            PromoGrantId = response.PromoGrantId,
            ServiceType = response.ServiceType.ToGrpc(),
            OriginalAmount = GrpcMappingHelpers.FormatDecimal(response.OriginalAmount),
            RemainingAmount = GrpcMappingHelpers.FormatDecimal(response.RemainingAmount),
            ExpiresAt = GrpcMappingHelpers.ToTimestamp(response.ExpiresAt)
        };
    }
}
