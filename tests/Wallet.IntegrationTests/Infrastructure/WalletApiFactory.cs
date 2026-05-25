using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wallet.Infrastructure.Persistence;

namespace Wallet.IntegrationTests.Infrastructure;

public sealed class WalletApiFactory(
    string sqlConnectionString,
    string redisConnectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTest");

        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = sqlConnectionString,
                ["Kafka:Enabled"] = "false",
                ["Redis:Enabled"] = "true",
                ["Redis:Configuration"] = redisConnectionString,
                ["Redis:InstanceName"] = "wallet-it:",
                ["CircuitBreaker:FailureThreshold"] = "2",
                ["CircuitBreaker:BreakDurationSeconds"] = "1"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<DbContextOptions<WalletDbContext>>();
            services.RemoveAll<WalletDbContext>();

            services.AddDbContext<WalletDbContext>(options =>
                options.UseSqlServer(sqlConnectionString));

            services.AddLogging(logging => logging.ClearProviders());
        });
    }
}
