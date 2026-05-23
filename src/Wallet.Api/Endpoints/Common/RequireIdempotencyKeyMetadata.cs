namespace Wallet.Api.Endpoints.Common;

public sealed class RequireIdempotencyKeyMetadata
{
    public static readonly RequireIdempotencyKeyMetadata Instance = new();

    private RequireIdempotencyKeyMetadata()
    {
    }
}
