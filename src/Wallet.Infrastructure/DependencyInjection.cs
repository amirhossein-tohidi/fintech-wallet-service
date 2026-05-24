using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Messaging;
using Wallet.Application.Abstractions.Persistence;
using Wallet.Application.Abstractions.ReadModels;
using Wallet.Infrastructure.Dapper;
using Wallet.Infrastructure.Messaging;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Redis;
using Wallet.Infrastructure.Resilience;
using Wallet.Infrastructure.Services.Idempotency;

namespace Wallet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(options =>
        {
            var section = configuration.GetSection(KafkaOptions.SectionName);
            options.Enabled = bool.TryParse(section["Enabled"], out var enabled) && enabled;
            options.BootstrapServers = section["BootstrapServers"] ?? options.BootstrapServers;
            options.Topic = section["Topic"] ?? options.Topic;
            options.ClientId = section["ClientId"] ?? options.ClientId;
        });

        services.Configure<RedisOptions>(options =>
        {
            var section = configuration.GetSection(RedisOptions.SectionName);
            options.Enabled = bool.TryParse(section["Enabled"], out var enabled) && enabled;
            options.Configuration = section["Configuration"] ?? options.Configuration;
            options.InstanceName = section["InstanceName"] ?? options.InstanceName;
        });

        services.Configure<CircuitBreakerOptions>(options =>
        {
            var section = configuration.GetSection(CircuitBreakerOptions.SectionName);

            if (int.TryParse(section["FailureThreshold"], out var failureThreshold))
            {
                options.FailureThreshold = failureThreshold;
            }

            if (int.TryParse(section["BreakDurationSeconds"], out var breakDurationSeconds))
            {
                options.BreakDurationSeconds = breakDurationSeconds;
            }
        });

        services.AddMediatR(mediatR =>
            mediatR.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddDbContext<WalletDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<WalletDbContext>());

        services.AddScoped<IUnitOfWork>(provider =>
            provider.GetRequiredService<WalletDbContext>());

        services.AddScoped<IWalletReadRepository, WalletReadRepository>();

        services.AddSingleton<IIdempotencyPolicy, DefaultIdempotencyPolicy>();
        services.AddSingleton<CircuitBreakerState>();
        services.AddSingleton<IIntegrationEventPublisher, KafkaIntegrationEventPublisher>();
        services.AddSingleton<RedisConnectionFactory>();
        services.AddScoped<DistributedLockService>();
        services.AddScoped<IdempotencyService>();

        return services;
    }
}
