using Microsoft.Extensions.Options;

namespace Wallet.Infrastructure.Redis;

public sealed class DistributedLockService(
    RedisConnectionFactory connectionFactory,
    IOptions<RedisOptions> options)
{
    private readonly RedisOptions _options = options.Value;

    public async Task<bool> AcquireAsync(
        string key,
        string value,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        var database = await connectionFactory.GetDatabaseAsync();
        if (database == null)
        {
            return true;
        }

        return await database.StringSetAsync(
            BuildKey(key),
            value,
            expiry,
            StackExchange.Redis.When.NotExists);
    }

    public async Task ReleaseAsync(
        string key,
        string value,
        CancellationToken ct = default)
    {
        var database = await connectionFactory.GetDatabaseAsync();
        if (database == null)
        {
            return;
        }

        var redisKey = BuildKey(key);
        var currentValue = await database.StringGetAsync(redisKey);

        if (currentValue == value)
        {
            await database.KeyDeleteAsync(redisKey);
        }
    }

    private string BuildKey(string key)
    {
        return $"{_options.InstanceName}lock:{key}";
    }
}
