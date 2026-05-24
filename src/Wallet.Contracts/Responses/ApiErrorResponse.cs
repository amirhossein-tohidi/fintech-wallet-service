using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Wallet.Contracts.Responses;

public sealed record ApiErrorResponse(string Code, string Message)
{
    public static ApiErrorResponse From(ApiErrorCode errorCode, string? messageOverride = null)
    {
        return new ApiErrorResponse(
            Code: errorCode.ToCode(),
            Message: messageOverride ?? errorCode.GetDescription());
    }
}

public enum ApiErrorCode
{
    [Description("X-Idempotency-Key header is required.")]
    CommonIdempotencyKeyRequired = 1000,

    [Description("Idempotency key has already been used with a different request.")]
    CommonIdempotencyKeyConflict = 1001,

    [Description("Request is currently being processed.")]
    CommonIdempotencyRequestInProgress = 1002,

    [Description("Previous request failed. Please retry with a new idempotency key.")]
    CommonIdempotencyPreviousRequestFailed = 1003,

    [Description("Request validation failed.")]
    CommonValidationFailed = 1400,

    [Description("Requested resource was not found.")]
    CommonResourceNotFound = 1404,

    [Description("An unexpected server error occurred.")]
    CommonInternalServerError = 1500,

    [Description("Wallet not found.")]
    WalletNotFound = 2000,

    [Description("Wallet operation was rejected.")]
    WalletOperationRejected = 2001,

    [Description("Wallet was modified by another request. Please retry.")]
    WalletConcurrencyConflict = 2002
}

public static class ApiErrorCodeExtensions
{
    public static string ToCode(this ApiErrorCode errorCode)
    {
        var name = errorCode.ToString();
        var builder = new StringBuilder(capacity: name.Length + 8);

        for (var i = 0; i < name.Length; i++)
        {
            var character = name[i];
            if (char.IsUpper(character) && i > 0)
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }

    public static string GetDescription(this ApiErrorCode errorCode)
    {
        var member = typeof(ApiErrorCode)
            .GetMember(errorCode.ToString())
            .FirstOrDefault();

        return member?.GetCustomAttribute<DescriptionAttribute>()?.Description
               ?? errorCode.ToString();
    }
}
