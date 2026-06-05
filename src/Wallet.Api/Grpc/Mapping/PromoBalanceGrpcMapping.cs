using Wallet.Contracts.Responses;
using Wallet.GrpcContracts;

namespace Wallet.Api.Grpc.Mapping;

public static class PromoBalanceGrpcMapping
{
    public static PromoBalanceGrpcResponse ToGrpc(this PromoBalanceResponse response)
    {
        return new PromoBalanceGrpcResponse
        {
            PromoGrantId = response.PromoGrantId,
            ServiceType = response.ServiceType.ToGrpc(),
            OriginalAmount = GrpcMappingHelpers.FormatDecimal(response.OriginalAmount),
            RemainingAmount = GrpcMappingHelpers.FormatDecimal(response.RemainingAmount),
            ConsumedAmount = GrpcMappingHelpers.FormatDecimal(response.ConsumedAmount),
            IsExpired = response.IsExpired,
            ExpiresAt = GrpcMappingHelpers.ToTimestamp(response.ExpiresAt)
        };
    }
}
