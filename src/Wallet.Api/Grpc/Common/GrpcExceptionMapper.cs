using Grpc.Core;
using Wallet.Contracts.Responses;

namespace Wallet.Api.Grpc.Common;

public static class GrpcExceptionMapper
{
    private const string ErrorCodeTrailer = "x-error-code";

    public static RpcException ToRpcException(string? error)
    {
        if (error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
        {
            return Create(StatusCode.NotFound, ApiErrorCode.CommonResourceNotFound, error);
        }

        if (error?.Contains("concurrency", StringComparison.OrdinalIgnoreCase) == true)
        {
            return Create(StatusCode.Aborted, ApiErrorCode.WalletConcurrencyConflict, error);
        }

        return Create(StatusCode.InvalidArgument, ApiErrorCode.WalletOperationRejected, error);
    }

    public static RpcException MissingIdempotencyKey()
    {
        return Create(
            StatusCode.InvalidArgument,
            ApiErrorCode.CommonIdempotencyKeyRequired,
            ApiErrorCode.CommonIdempotencyKeyRequired.GetDescription());
    }

    public static RpcException IdempotencyConflict(ApiErrorCode errorCode)
    {
        return Create(StatusCode.AlreadyExists, errorCode, errorCode.GetDescription());
    }

    public static RpcException ValidationFailed(string message)
    {
        return Create(StatusCode.InvalidArgument, ApiErrorCode.CommonValidationFailed, message);
    }

    public static RpcException InternalServerError()
    {
        return Create(
            StatusCode.Internal,
            ApiErrorCode.CommonInternalServerError,
            ApiErrorCode.CommonInternalServerError.GetDescription());
    }

    public static RpcException NotImplemented(NotImplementedException exception)
    {
        return Create(
            StatusCode.Unimplemented,
            ApiErrorCode.CommonInternalServerError,
            exception.Message);
    }

    private static RpcException Create(StatusCode statusCode, ApiErrorCode errorCode, string? message)
    {
        var metadata = new Metadata
        {
            { ErrorCodeTrailer, errorCode.ToCode() }
        };

        return new RpcException(new Status(statusCode, message ?? errorCode.GetDescription()), metadata);
    }
}
