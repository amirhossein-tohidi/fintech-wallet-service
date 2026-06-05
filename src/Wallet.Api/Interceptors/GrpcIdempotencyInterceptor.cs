using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.EntityFrameworkCore;
using Wallet.Api.Constants;
using Wallet.Api.Grpc.Common;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Persistence;
using Wallet.Contracts.Responses;
using Wallet.Domain.Enums;
using Wallet.Domain.Idempotency;

namespace Wallet.Api.Interceptors;

public sealed class GrpcIdempotencyInterceptor(
    IServiceProvider serviceProvider,
    ILogger<GrpcIdempotencyInterceptor> logger) : Interceptor
{
    private static readonly HashSet<string> IdempotentMethods = new(StringComparer.Ordinal)
    {
        "/wallet.v1.WalletGrpc/TopUp",
        "/wallet.v1.WalletGrpc/FastPay",
        "/wallet.v1.WalletGrpc/Refund",
        "/wallet.v1.WalletGrpc/Reserve",
        "/wallet.v1.WalletGrpc/ConfirmReservation",
        "/wallet.v1.WalletGrpc/CancelReservation",
        "/wallet.v1.WalletGrpc/AddPromoGrant",
        "/wallet.v1.WalletGrpc/ConsumePromo"
    };

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        if (!IdempotentMethods.Contains(context.Method))
        {
            try
            {
                return await continuation(request, context);
            }
            catch (NotImplementedException ex)
            {
                logger.LogError(ex, "gRPC request hit an unimplemented mapping for method {Method}.", context.Method);
                throw GrpcExceptionMapper.NotImplemented(ex);
            }
        }

        var idempotencyKey = context.RequestHeaders
            .FirstOrDefault(header => string.Equals(header.Key, HeaderNames.IdempotencyKey, StringComparison.OrdinalIgnoreCase))
            ?.Value;

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw GrpcExceptionMapper.MissingIdempotencyKey();
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var idempotencyPolicy = scope.ServiceProvider.GetRequiredService<IIdempotencyPolicy>();
        var endpoint = $"GRPC {context.Method}";
        var requestHash = ComputeRequestHash(request);

        var existingRequest = await dbContext.IdempotencyRequests
            .FirstOrDefaultAsync(x => x.Key == idempotencyKey, context.CancellationToken);

        if (existingRequest != null)
        {
            if (existingRequest.Endpoint != endpoint || existingRequest.RequestHash != requestHash)
            {
                throw GrpcExceptionMapper.IdempotencyConflict(ApiErrorCode.CommonIdempotencyKeyConflict);
            }

            if (existingRequest.Status == IdempotencyStatus.Completed)
            {
                return DeserializeResponse<TResponse>(existingRequest.ResponseBody);
            }

            if (existingRequest.IsPending() && !existingRequest.IsExpired())
            {
                throw GrpcExceptionMapper.IdempotencyConflict(ApiErrorCode.CommonIdempotencyRequestInProgress);
            }

            if (existingRequest.Status == IdempotencyStatus.Failed)
            {
                throw GrpcExceptionMapper.IdempotencyConflict(ApiErrorCode.CommonIdempotencyPreviousRequestFailed);
            }

            dbContext.IdempotencyRequests.Remove(existingRequest);
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }

        var idempotencyRequest = new IdempotencyRequest(
            key: idempotencyKey,
            endpoint: endpoint,
            requestHash: requestHash,
            expireAt: DateTime.UtcNow.Add(idempotencyPolicy.GetExpiration()));

        try
        {
            await unitOfWork.BeginTransactionAsync(context.CancellationToken);

            dbContext.IdempotencyRequests.Add(idempotencyRequest);
            await dbContext.SaveChangesAsync(context.CancellationToken);

            var response = await continuation(request, context);
            idempotencyRequest.MarkAsCompleted(SerializeResponse(response));

            await dbContext.SaveChangesAsync(context.CancellationToken);
            await unitOfWork.CommitAsync(context.CancellationToken);

            return response;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            logger.LogWarning(ex, "Concurrent gRPC idempotency request detected for key {IdempotencyKey}.", idempotencyKey);

            throw GrpcExceptionMapper.IdempotencyConflict(ApiErrorCode.CommonIdempotencyRequestInProgress);
        }
        catch (RpcException ex)
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            idempotencyRequest.MarkAsFailed(ex.Status.Detail);

            throw;
        }
        catch (NotImplementedException ex)
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            idempotencyRequest.MarkAsFailed(ex.Message);
            logger.LogError(ex, "gRPC request hit an unimplemented mapping for method {Method}.", context.Method);

            throw GrpcExceptionMapper.NotImplemented(ex);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            logger.LogError(ex, "gRPC request failed for idempotency key {IdempotencyKey}.", idempotencyKey);

            throw GrpcExceptionMapper.InternalServerError();
        }
    }

    private static string ComputeRequestHash<TRequest>(TRequest request)
    {
        return request is IMessage message
            ? Convert.ToBase64String(message.ToByteArray())
            : request?.GetHashCode().ToString() ?? string.Empty;
    }

    private static string SerializeResponse<TResponse>(TResponse response)
    {
        return response is IMessage message
            ? Convert.ToBase64String(message.ToByteArray())
            : string.Empty;
    }

    private static TResponse DeserializeResponse<TResponse>(string responseBody)
        where TResponse : class
    {
        var parser = typeof(TResponse).GetProperty("Parser")?.GetValue(null) as MessageParser;
        if (parser == null)
        {
            throw GrpcExceptionMapper.InternalServerError();
        }

        return (TResponse)parser.ParseFrom(Convert.FromBase64String(responseBody));
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        const int uniqueConstraintViolation = 2627;
        const int duplicateKey = 2601;

        return exception.InnerException is Microsoft.Data.SqlClient.SqlException sqlException &&
               sqlException.Errors
                   .Cast<Microsoft.Data.SqlClient.SqlError>()
                   .Any(error => error.Number is uniqueConstraintViolation or duplicateKey);
    }
}
