using Wallet.Api.Constants;
using Wallet.Application.Common;
using Wallet.Contracts.Responses;

namespace Wallet.Api.Endpoints.Common;

public static class EndpointResultExtensions
{
    public static RouteHandlerBuilder RequireIdempotencyKey(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;
            if (!httpContext.Request.Headers.TryGetValue(HeaderNames.IdempotencyKey, out var idempotencyKeyHeader) ||
                string.IsNullOrWhiteSpace(idempotencyKeyHeader.ToString()))
            {
                return Results.BadRequest(ApiErrorResponse.From(ApiErrorCode.CommonIdempotencyKeyRequired));
            }

            httpContext.Items[HttpContextItemKeys.IdempotencyKey] = idempotencyKeyHeader.ToString();

            return await next(context);
        });
    }

    public static string GetIdempotencyKey(this HttpContext context)
    {
        return context.Items.TryGetValue(HttpContextItemKeys.IdempotencyKey, out var value)
            ? value?.ToString() ?? string.Empty
            : context.Request.Headers[HeaderNames.IdempotencyKey].ToString();
    }

    public static IResult ToCommandResult<TResponse>(this Result<TResponse> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        return ToErrorResult(result.Error);
    }

    public static IResult ToCreatedCommandResult<TResponse>(
        this Result<TResponse> result,
        Func<TResponse, string> locationFactory)
    {
        if (result.IsFailure)
        {
            return ToErrorResult(result.Error);
        }

        var value = result.Value!;
        return Results.Created(locationFactory(value), value);
    }

    public static IResult NotFoundResult(ApiErrorCode errorCode, string? messageOverride = null)
    {
        return Results.NotFound(ApiErrorResponse.From(
            errorCode: errorCode,
            messageOverride: messageOverride));
    }

    private static IResult ToErrorResult(string? error)
    {
        if (error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
        {
            return NotFoundResult(
                errorCode: ApiErrorCode.CommonResourceNotFound,
                messageOverride: error);
        }

        if (error?.Contains("concurrency", StringComparison.OrdinalIgnoreCase) == true)
        {
            return Results.Conflict(ApiErrorResponse.From(
                errorCode: ApiErrorCode.WalletConcurrencyConflict,
                messageOverride: error));
        }

        return Results.BadRequest(ApiErrorResponse.From(
            errorCode: ApiErrorCode.WalletOperationRejected,
            messageOverride: error));
    }
}
