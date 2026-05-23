using Microsoft.EntityFrameworkCore;
using Wallet.Api.Constants;
using Wallet.Contracts.Responses;
using Wallet.Domain.Exceptions;

namespace Wallet.Api.Middlewares
{
    public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var idempotencyKey = GetIdempotencyKey(context);

            try
            {
                await next(context);
            }
            catch (NotFoundException ex)
            {
                await HandleExceptionAsync(context, ex, StatusCodes.Status404NotFound, ApiErrorCode.CommonResourceNotFound, idempotencyKey);
            }
            catch (ValidationException ex)
            {
                await HandleValidationExceptionAsync(context, ex, idempotencyKey);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await HandleExceptionAsync(context, ex, StatusCodes.Status409Conflict, ApiErrorCode.WalletConcurrencyConflict, idempotencyKey);
            }
            catch (InvalidOperationException ex)
            {
                await HandleExceptionAsync(context, ex, StatusCodes.Status400BadRequest, ApiErrorCode.WalletOperationRejected, idempotencyKey);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, StatusCodes.Status500InternalServerError, ApiErrorCode.CommonInternalServerError, idempotencyKey);
            }
        }

        private async Task HandleExceptionAsync(
            HttpContext context,
            Exception ex,
            int statusCode,
            ApiErrorCode errorCode,
            string? idempotencyKey)
        {
            var isDevelopment = IsDevelopmentEnvironment();
            var message = isDevelopment || statusCode < StatusCodes.Status500InternalServerError
                ? ex.Message
                : errorCode.GetDescription();

            logger.LogError(ex, "Exception handling: Status={StatusCode}, ErrorCode={ErrorCode}, Message={Message}, IdempotencyKey={IdempotencyKey}",
                statusCode, errorCode.ToCode(), ex.Message, idempotencyKey ?? "N/A");

            if (context.Response.HasStarted)
            {
                logger.LogWarning("Response has already started, skipping exception handling.");
                return;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            await context.Response.WriteAsJsonAsync(ApiErrorResponse.From(errorCode, message));
        }

        private async Task HandleValidationExceptionAsync(HttpContext context, ValidationException ex, string? idempotencyKey)
        {
            logger.LogWarning(ex, "Validation failed. IdempotencyKey={IdempotencyKey}", idempotencyKey ?? "N/A");

            if (context.Response.HasStarted)
            {
                logger.LogWarning("Response has already started, skipping exception handling.");
                return;
            }

            context.Items[HttpContextItemKeys.SkipIdempotencyPersistence] = true;
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(ex.Errors);
        }

        private string? GetIdempotencyKey(HttpContext context)
        {
            return context.Request.Headers.TryGetValue(HeaderNames.IdempotencyKey, out var key)
                ? key.FirstOrDefault()
                : null;
        }

        private bool IsDevelopmentEnvironment()
        {
            return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        }
    }
}
