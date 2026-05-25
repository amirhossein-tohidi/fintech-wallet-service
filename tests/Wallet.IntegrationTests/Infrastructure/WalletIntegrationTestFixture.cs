using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Testcontainers.MsSql;
using Testcontainers.Redis;
using Wallet.Application;
using Wallet.Infrastructure;
using Wallet.Infrastructure.Persistence;
using Wallet.Worker;

namespace Wallet.IntegrationTests.Infrastructure;

public sealed class WalletIntegrationTestFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPortBinding(1433, assignRandomHostPort: true)
        .WithCleanUp(cleanUp: true)
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:latest")
        .WithPortBinding(6379, assignRandomHostPort: true)
        .WithCleanUp(cleanUp: true)
        .Build();
    private readonly SemaphoreSlim _resetLock = new(1, 1);
    private IConnectionMultiplexer? _redisConnection;

    public WalletApiFactory ApiFactory { get; private set; } = null!;

    public string SqlConnectionString => _sql.GetConnectionString();
    public string RedisConnectionString => _redis.GetConnectionString();

    public HttpClient CreateClient()
    {
        return ApiFactory.CreateClient();
    }

    public WalletDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WalletDbContext>()
            .UseSqlServer(SqlConnectionString)
            .Options;

        return new WalletDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();
        await _redis.StartAsync();

        await using (var dbContext = CreateDbContext())
        {
            await dbContext.Database.MigrateAsync();
        }

        _redisConnection = await ConnectionMultiplexer.ConnectAsync(RedisConnectionString);
        ApiFactory = new WalletApiFactory(SqlConnectionString, RedisConnectionString);
    }

    public async Task DisposeAsync()
    {
        if (ApiFactory is not null)
        {
            await ApiFactory.DisposeAsync();
        }

        if (_redisConnection is not null)
        {
            await _redisConnection.DisposeAsync();
        }

        await Task.WhenAll(
            _redis.DisposeAsync().AsTask(),
            _sql.DisposeAsync().AsTask());
    }

    public async Task ResetAsync()
    {
        await _resetLock.WaitAsync();

        try
        {
            await using var dbContext = CreateDbContext();

            await dbContext.Database.ExecuteSqlRawAsync("""
                DELETE FROM [LedgerEntries];
                DELETE FROM [LedgerTransactions];
                DELETE FROM [Reservations];
                DELETE FROM [PromoGrants];
                DELETE FROM [IdempotencyRequests];
                DELETE FROM [InboxMessages];
                DELETE FROM [OutboxMessages];
                DELETE FROM [UserWallets];
                """);

            if (_redisConnection is not null)
            {
                var database = _redisConnection.GetDatabase();
                var server = _redisConnection.GetServer(_redisConnection.GetEndPoints().Single());
                foreach (var key in server.Keys(pattern: "wallet-it:*"))
                {
                    await database.KeyDeleteAsync(key);
                }
            }
        }
        finally
        {
            _resetLock.Release();
        }
    }

    public async Task<IHost> StartWorkerAsync(
        bool outboxEnabled = false,
        bool inboxEnabled = false,
        bool reservationExpiryEnabled = false,
        bool redisEnabled = true)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = "IntegrationTest"
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = SqlConnectionString,
            ["Kafka:Enabled"] = "false",
            ["Redis:Enabled"] = redisEnabled.ToString(),
            ["Redis:Configuration"] = RedisConnectionString,
            ["Redis:InstanceName"] = "wallet-it:",
            ["OutboxProcessor:Enabled"] = outboxEnabled.ToString(),
            ["OutboxProcessor:BatchSize"] = "20",
            ["OutboxProcessor:PollingIntervalSeconds"] = "1",
            ["OutboxProcessor:LockSeconds"] = "3",
            ["OutboxProcessor:MaxRetryCount"] = "2",
            ["OutboxProcessor:DeadLetterBatchSize"] = "20",
            ["OutboxProcessor:DeadLetterPollingIntervalSeconds"] = "1",
            ["InboxProcessor:Enabled"] = inboxEnabled.ToString(),
            ["InboxProcessor:BatchSize"] = "20",
            ["InboxProcessor:PollingIntervalSeconds"] = "1",
            ["InboxProcessor:LockSeconds"] = "3",
            ["InboxProcessor:MaxRetryCount"] = "2",
            ["InboxProcessor:DeadLetterBatchSize"] = "20",
            ["InboxProcessor:DeadLetterPollingIntervalSeconds"] = "1",
            ["ReservationExpiry:Enabled"] = reservationExpiryEnabled.ToString(),
            ["ReservationExpiry:BatchSize"] = "20",
            ["ReservationExpiry:PollingIntervalSeconds"] = "1",
            ["CircuitBreaker:FailureThreshold"] = "2",
            ["CircuitBreaker:BreakDurationSeconds"] = "1"
        });

        builder.Services.AddLogging(logging => logging.ClearProviders());
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddWorkerServices(builder.Configuration);

        var host = builder.Build();
        await host.StartAsync();
        return host;
    }

    public async Task<string?> GetRedisStringAsync(string key)
    {
        if (_redisConnection is null)
        {
            return null;
        }

        return await _redisConnection.GetDatabase().StringGetAsync(key);
    }
}
