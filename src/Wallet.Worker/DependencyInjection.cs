using KafkaFlow;
using Wallet.Application.Abstractions.Messaging;
using Wallet.Infrastructure.Messaging;
using Wallet.Worker.BackgroundJobs;
using Wallet.Worker.InboxHandlers;
using Wallet.Worker.Messaging;

namespace Wallet.Worker;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OutboxProcessorOptions>(options =>
        {
            var section = configuration.GetSection(OutboxProcessorOptions.SectionName);
            options.Enabled = !bool.TryParse(section["Enabled"], out var enabled) || enabled;

            if (int.TryParse(section["BatchSize"], out var batchSize))
            {
                options.BatchSize = batchSize;
            }

            if (int.TryParse(section["PollingIntervalSeconds"], out var pollingIntervalSeconds))
            {
                options.PollingIntervalSeconds = pollingIntervalSeconds;
            }

            if (int.TryParse(section["LockSeconds"], out var lockSeconds))
            {
                options.LockSeconds = lockSeconds;
            }

            if (int.TryParse(section["MaxRetryCount"], out var maxRetryCount))
            {
                options.MaxRetryCount = maxRetryCount;
            }

            if (int.TryParse(section["DeadLetterBatchSize"], out var deadLetterBatchSize))
            {
                options.DeadLetterBatchSize = deadLetterBatchSize;
            }

            if (int.TryParse(section["DeadLetterPollingIntervalSeconds"], out var deadLetterPollingIntervalSeconds))
            {
                options.DeadLetterPollingIntervalSeconds = deadLetterPollingIntervalSeconds;
            }
        });

        services.Configure<InboxProcessorOptions>(options =>
        {
            var section = configuration.GetSection(InboxProcessorOptions.SectionName);
            options.Enabled = bool.TryParse(section["Enabled"], out var enabled) && enabled;

            if (int.TryParse(section["BatchSize"], out var batchSize))
            {
                options.BatchSize = batchSize;
            }

            if (int.TryParse(section["PollingIntervalSeconds"], out var pollingIntervalSeconds))
            {
                options.PollingIntervalSeconds = pollingIntervalSeconds;
            }

            if (int.TryParse(section["LockSeconds"], out var lockSeconds))
            {
                options.LockSeconds = lockSeconds;
            }

            if (int.TryParse(section["MaxRetryCount"], out var maxRetryCount))
            {
                options.MaxRetryCount = maxRetryCount;
            }

            if (int.TryParse(section["DeadLetterBatchSize"], out var deadLetterBatchSize))
            {
                options.DeadLetterBatchSize = deadLetterBatchSize;
            }

            if (int.TryParse(section["DeadLetterPollingIntervalSeconds"], out var deadLetterPollingIntervalSeconds))
            {
                options.DeadLetterPollingIntervalSeconds = deadLetterPollingIntervalSeconds;
            }
        });

        services.Configure<ReservationExpiryOptions>(options =>
        {
            var section = configuration.GetSection(ReservationExpiryOptions.SectionName);
            options.Enabled = !bool.TryParse(section["Enabled"], out var enabled) || enabled;

            if (int.TryParse(section["BatchSize"], out var batchSize))
            {
                options.BatchSize = batchSize;
            }

            if (int.TryParse(section["PollingIntervalSeconds"], out var pollingIntervalSeconds))
            {
                options.PollingIntervalSeconds = pollingIntervalSeconds;
            }
        });

        services.AddHostedService<OutboxProcessorWorker>();
        services.AddHostedService<OutboxDeadLetterProcessorWorker>();
        services.AddHostedService<InboxProcessorWorker>();
        services.AddHostedService<InboxDeadLetterProcessorWorker>();
        services.AddHostedService<ReservationExpiryWorker>();
        services.AddHostedService<PromoExpiryWorker>();

        services.AddScoped<InboxProjectionWriter>();
        services.AddInboxMessageHandlers();
        services.AddKafkaFlow(configuration);

        return services;
    }

    private static IServiceCollection AddKafkaFlow(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var kafkaOptions = new KafkaOptions();
        configuration.GetSection(KafkaOptions.SectionName).Bind(kafkaOptions);

        if (!kafkaOptions.Enabled)
        {
            return services;
        }

        services.AddKafkaFlowHostedService(kafka => kafka
            .AddCluster(cluster =>
            {
                var brokerList = kafkaOptions.BootstrapServers
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                cluster
                    .WithName(kafkaOptions.ClusterName)
                    .WithBrokers(brokerList)
                    .AddProducer(kafkaOptions.ProducerName, producer => producer
                        .DefaultTopic(kafkaOptions.Topic)
                        .WithAcks(Acks.All)
                        .WithLingerMs(5)
                        .WithCompression(Confluent.Kafka.CompressionType.Lz4)
                        .WithProducerConfig(new Confluent.Kafka.ProducerConfig
                        {
                            ClientId = kafkaOptions.ClientId,
                            EnableIdempotence = true,
                            MessageSendMaxRetries = int.MaxValue,
                            RetryBackoffMs = kafkaOptions.RetryBackoffMs,
                            ReconnectBackoffMs = kafkaOptions.ReconnectBackoffMs,
                            ReconnectBackoffMaxMs = kafkaOptions.ReconnectBackoffMaxMs,
                            MessageTimeoutMs = kafkaOptions.MessageTimeoutMs,
                            RequestTimeoutMs = kafkaOptions.RequestTimeoutMs,
                            SocketKeepaliveEnable = true,
                            EnableDeliveryReports = true
                        }))
                    .AddConsumer(consumer => consumer
                        .WithName(kafkaOptions.ConsumerName)
                        .Topic(kafkaOptions.Topic)
                        .WithGroupId(kafkaOptions.ConsumerGroupId)
                        .WithAutoOffsetReset(AutoOffsetReset.Earliest)
                        .WithAutoCommitIntervalMs(kafkaOptions.AutoCommitIntervalMs)
                        .WithManualMessageCompletion()
                        .WithWorkersCount(kafkaOptions.ConsumerWorkers)
                        .WithConsumerLagWorkerBalancer(
                            totalWorkers: kafkaOptions.ConsumerWorkers,
                            minInstanceWorkers: kafkaOptions.ConsumerMinWorkers,
                            maxInstanceWorkers: kafkaOptions.ConsumerMaxWorkers)
                        .WithBufferSize(kafkaOptions.ConsumerBufferSize)
                        .WithWorkerStopTimeout(TimeSpan.FromSeconds(30))
                        .WithConsumerConfig(new Confluent.Kafka.ConsumerConfig
                        {
                            ClientId = $"{kafkaOptions.ClientId}-consumer",
                            EnableAutoCommit = false,
                            EnableAutoOffsetStore = false,
                            SessionTimeoutMs = kafkaOptions.SessionTimeoutMs,
                            MaxPollIntervalMs = kafkaOptions.MaxPollIntervalMs,
                            ReconnectBackoffMs = kafkaOptions.ReconnectBackoffMs,
                            ReconnectBackoffMaxMs = kafkaOptions.ReconnectBackoffMaxMs,
                            RetryBackoffMs = kafkaOptions.RetryBackoffMs,
                            SocketKeepaliveEnable = true,
                            AllowAutoCreateTopics = kafkaOptions.CreateTopicIfNotExists
                        })
                        .WithPendingOffsetsStatisticsHandler((resolver, offsets) =>
                        {
                            var logger = resolver.Resolve<ILogger<KafkaInboxMiddleware>>();
                            logger.LogDebug("KafkaFlow has {PendingOffsetCount} pending offsets.", offsets.Count());
                        }, TimeSpan.FromSeconds(30))
                        .AddMiddlewares(middlewares => middlewares
                            .Add<KafkaInboxMiddleware>(MiddlewareLifetime.Message)));

                if (kafkaOptions.CreateTopicIfNotExists)
                {
                    cluster.CreateTopicIfNotExists(
                        kafkaOptions.Topic,
                        kafkaOptions.TopicPartitions,
                        kafkaOptions.TopicReplicationFactor);
                }
            }));

        return services;
    }

    private static IServiceCollection AddInboxMessageHandlers(this IServiceCollection services)
    {
        services.AddScoped<IInboxMessageHandler, LedgerTransactionCreatedInboxHandler>();
        services.AddScoped<IInboxMessageHandler, WalletBalanceChangedInboxHandler>();
        services.AddScoped<IInboxMessageHandler, WalletRefundedInboxHandler>();
        services.AddScoped<IInboxMessageHandler, ReservationCreatedInboxHandler>();
        services.AddScoped<IInboxMessageHandler, ReservationConfirmedInboxHandler>();
        services.AddScoped<IInboxMessageHandler, ReservationCancelledInboxHandler>();
        services.AddScoped<IInboxMessageHandler, ReservationExpiredInboxHandler>();
        services.AddScoped<IInboxMessageHandler, PromoGrantAddedInboxHandler>();
        services.AddScoped<IInboxMessageHandler, PromoConsumedInboxHandler>();

        return services;
    }
}
