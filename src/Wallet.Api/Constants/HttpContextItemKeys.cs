namespace Wallet.Api.Constants;

public static class HttpContextItemKeys
{
    public static readonly object IdempotencyKey = new();
    public static readonly object SkipIdempotencyPersistence = new();
}
