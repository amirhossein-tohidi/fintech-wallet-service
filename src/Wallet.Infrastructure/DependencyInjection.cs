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
            options.ClusterName = section["ClusterName"] ?? options.ClusterName;
            options.ProducerName = section["ProducerName"] ?? options.ProducerName;
            options.ConsumerName = section["ConsumerName"] ?? options.ConsumerName;
            options.ConsumerGroupId = section["ConsumerGroupId"] ?? options.ConsumerGroupId;

            if (int.TryParse(section["ConsumerWorkers"], out var consumerWorkers))
            {
                options.ConsumerWorkers = consumerWorkers;
            }

            if (int.TryParse(section["ConsumerBufferSize"], out var consumerBufferSize))
            {
                options.ConsumerBufferSize = consumerBufferSize;
            }

            if (int.TryParse(section["ConsumerMinWorkers"], out var consumerMinWorkers))
            {
                options.ConsumerMinWorkers = consumerMinWorkers;
            }

            if (int.TryParse(section["ConsumerMaxWorkers"], out var consumerMaxWorkers))
            {
                options.ConsumerMaxWorkers = consumerMaxWorkers;
            }

            if (int.TryParse(section["AutoCommitIntervalMs"], out var autoCommitIntervalMs))
            {
                options.AutoCommitIntervalMs = autoCommitIntervalMs;
            }

            if (int.TryParse(section["StatisticsIntervalMs"], out var statisticsIntervalMs))
            {
                options.StatisticsIntervalMs = statisticsIntervalMs;
            }

            if (int.TryParse(section["ReconnectBackoffMs"], out var reconnectBackoffMs))
            {
                options.ReconnectBackoffMs = reconnectBackoffMs;
            }

            if (int.TryParse(section["ReconnectBackoffMaxMs"], out var reconnectBackoffMaxMs))
            {
                options.ReconnectBackoffMaxMs = reconnectBackoffMaxMs;
            }

            if (int.TryParse(section["RetryBackoffMs"], out var retryBackoffMs))
            {
                options.RetryBackoffMs = retryBackoffMs;
            }

            if (int.TryParse(section["MessageTimeoutMs"], out var messageTimeoutMs))
            {
                options.MessageTimeoutMs = messageTimeoutMs;
            }

            if (int.TryParse(section["RequestTimeoutMs"], out var requestTimeoutMs))
            {
                options.RequestTimeoutMs = requestTimeoutMs;
            }

            if (int.TryParse(section["SessionTimeoutMs"], out var sessionTimeoutMs))
            {
                options.SessionTimeoutMs = sessionTimeoutMs;
            }

            if (int.TryParse(section["MaxPollIntervalMs"], out var maxPollIntervalMs))
            {
                options.MaxPollIntervalMs = maxPollIntervalMs;
            }

            if (bool.TryParse(section["CreateTopicIfNotExists"], out var createTopicIfNotExists))
            {
                options.CreateTopicIfNotExists = createTopicIfNotExists;
            }

            if (int.TryParse(section["TopicPartitions"], out var topicPartitions))
            {
                options.TopicPartitions = topicPartitions;
            }

            if (short.TryParse(section["TopicReplicationFactor"], out var topicReplicationFactor))
            {
                options.TopicReplicationFactor = topicReplicationFactor;
            }
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
