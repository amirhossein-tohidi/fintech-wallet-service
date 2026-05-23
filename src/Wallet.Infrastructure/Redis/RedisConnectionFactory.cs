using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Wallet.Infrastructure.Redis;

public sealed class RedisConnectionFactory : IAsyncDisposable
{
    private readonly RedisOptions _options;
    private readonly Lazy<Task<IConnectionMultiplexer?>> _connection;

    public RedisConnectionFactory(IOptions<RedisOptions> options)
    {
        _options = options.Value;
        _connection = new Lazy<Task<IConnectionMultiplexer?>>(CreateConnectionAsync);
    }

    public async Task<IDatabase?> GetDatabaseAsync()
    {
        var connection = await _connection.Value;
        return connection?.GetDatabase();
    }

    private async Task<IConnectionMultiplexer?> CreateConnectionAsync()
    {
        if (!_options.Enabled)
        {
            return null;
        }

        return await ConnectionMultiplexer.ConnectAsync(_options.Configuration);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_connection.IsValueCreated)
        {
            return;
        }

        var connection = await _connection.Value;
        await (connection?.DisposeAsync() ?? ValueTask.CompletedTask);
    }
}
