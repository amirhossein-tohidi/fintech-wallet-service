using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Wallet.Api.Constants;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Persistence;
using Wallet.Contracts.Responses;
using Wallet.Domain.Enums;
using Wallet.Domain.Idempotency;

namespace Wallet.Api.Middlewares;

public sealed class IdempotencyMiddleware(
    RequestDelegate next,
    ILogger<IdempotencyMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(
        HttpContext context,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        IIdempotencyPolicy idempotencyPolicy,
        IRouteInfo routeInfo)
    {
        if (!ShouldHandle(context.Request))
        {
            await next(context);
            return;
        }

        var idempotencyKey = GetIdempotencyKey(context.Request);
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await next(context);
            return;
        }

        var endpoint = routeInfo.Endpoint;
        var requestHash = await ComputeRequestBodyHashAsync(context.Request);

        var existingRequest = await dbContext.IdempotencyRequests
            .FirstOrDefaultAsync(x => x.Key == idempotencyKey, context.RequestAborted);

        if (existingRequest != null)
        {
            if (existingRequest.Endpoint != endpoint || existingRequest.RequestHash != requestHash)
            {
                await WriteConflictAsync(context, ApiErrorCode.CommonIdempotencyKeyConflict);
                return;
            }

            if (existingRequest.Status == IdempotencyStatus.Completed)
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(existingRequest.ResponseBody, context.RequestAborted);
                return;
            }

            if (existingRequest.IsPending() && !existingRequest.IsExpired())
            {
                await WriteConflictAsync(context, ApiErrorCode.CommonIdempotencyRequestInProgress);
                return;
            }

            if (existingRequest.Status == IdempotencyStatus.Failed)
            {
                await WriteConflictAsync(context, ApiErrorCode.CommonIdempotencyPreviousRequestFailed);
                return;
            }

            dbContext.IdempotencyRequests.Remove(existingRequest);
            await dbContext.SaveChangesAsync(context.RequestAborted);
        }

        var idempotencyRequest = new IdempotencyRequest(
            key: idempotencyKey,
            endpoint: endpoint,
            requestHash: requestHash,
            expireAt: DateTime.UtcNow.Add(idempotencyPolicy.GetExpiration()));

        var originalBody = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        try
        {
            await unitOfWork.BeginTransactionAsync(context.RequestAborted);

            dbContext.IdempotencyRequests.Add(idempotencyRequest);
            await dbContext.SaveChangesAsync(context.RequestAborted);

            await next(context);

            responseBuffer.Seek(offset: 0, loc: SeekOrigin.Begin);
            var responseBody = await new StreamReader(responseBuffer, Encoding.UTF8)
                .ReadToEndAsync(context.RequestAborted);

            if (context.Response.StatusCode is >= 200 and < 300)
            {
                idempotencyRequest.MarkAsCompleted(responseBody);
            }
            else if (ShouldSkipPersistence(context))
            {
                dbContext.IdempotencyRequests.Remove(idempotencyRequest);
            }
            else
            {
                idempotencyRequest.MarkAsFailed(responseBody);
            }

            await dbContext.SaveChangesAsync(context.RequestAborted);
            await unitOfWork.CommitAsync(context.RequestAborted);

            responseBuffer.Seek(offset: 0, loc: SeekOrigin.Begin);
            await responseBuffer.CopyToAsync(originalBody, context.RequestAborted);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            logger.LogWarning(ex, "Concurrent idempotency request detected for key {IdempotencyKey}.", idempotencyKey);

            context.Response.Body = originalBody;
            await WriteConflictAsync(context, ApiErrorCode.CommonIdempotencyRequestInProgress);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            logger.LogError(ex, "Request failed for idempotency key {IdempotencyKey}", idempotencyKey);

            throw;
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static bool ShouldHandle(HttpRequest request)
    {
        return HttpMethods.IsPost(request.Method) ||
               HttpMethods.IsPut(request.Method) ||
               HttpMethods.IsPatch(request.Method) ||
               HttpMethods.IsDelete(request.Method);
    }

    private static string? GetIdempotencyKey(HttpRequest request)
    {
        return request.Headers.TryGetValue(HeaderNames.IdempotencyKey, out var values)
            ? values.FirstOrDefault()
            : null;
    }

    private static bool ShouldSkipPersistence(HttpContext context)
    {
        return context.Items.TryGetValue(HttpContextItemKeys.SkipIdempotencyPersistence, out var value) &&
               value is true;
    }

    private static async Task<string> ComputeRequestBodyHashAsync(HttpRequest request)
    {
        request.EnableBuffering();
        request.Body.Position = 0;

        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(request.Body, request.HttpContext.RequestAborted);

        request.Body.Position = 0;
        return Convert.ToBase64String(hash);
    }

    private static async Task WriteConflictAsync(HttpContext context, ApiErrorCode errorCode)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(ApiErrorResponse.From(errorCode), JsonOptions),
            context.RequestAborted);
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
