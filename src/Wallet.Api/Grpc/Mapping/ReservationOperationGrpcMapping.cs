using Wallet.Contracts.Responses;
using Wallet.GrpcContracts;

namespace Wallet.Api.Grpc.Mapping;

public static class ReservationOperationGrpcMapping
{
    public static ReservationOperationGrpcResponse ToGrpc(this ReservationOperationResponse response)
    {
        var grpcResponse = new ReservationOperationGrpcResponse
        {
            WalletId = response.WalletId,
            ReservationId = response.ReservationId,
            ServiceType = response.ServiceType.ToGrpc(),
            Amount = GrpcMappingHelpers.FormatDecimal(response.Amount),
            ExpiresAt = GrpcMappingHelpers.ToTimestamp(response.ExpiresAt),
            Status = response.Status,
            AvailableBalance = GrpcMappingHelpers.FormatDecimal(response.AvailableBalance),
            ReservedBalance = GrpcMappingHelpers.FormatDecimal(response.ReservedBalance)
        };

        if (response.TransactionId.HasValue)
        {
            grpcResponse.TransactionId = response.TransactionId.Value;
        }

        return grpcResponse;
    }
}
