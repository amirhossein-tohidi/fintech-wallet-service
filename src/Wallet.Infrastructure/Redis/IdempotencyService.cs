using Microsoft.Extensions.Options;

namespace Wallet.Infrastructure.Redis;

public sealed class IdempotencyService(
    RedisConnectionFactory connectionFactory,
    IOptions<RedisOptions> options)
{
    private readonly RedisOptions _options = options.Value;

    public async Task<string?> GetResponseAsync(string key, CancellationToken ct = default)
    {
        var database = await connectionFactory.GetDatabaseAsync();
        if (database == null)
        {
            return null;
        }

        return await database.StringGetAsync(BuildKey(key));
    }

    public async Task CacheResponseAsync(
        string key,
        string response,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        var database = await connectionFactory.GetDatabaseAsync();
        if (database == null)
        {
            return;
        }

        await database.StringSetAsync(BuildKey(key), response, expiry);
    }

    private string BuildKey(string key)
    {
        return $"{_options.InstanceName}idempotency:{key}";
    }
}
