using Wallet.Application.Abstractions.Messaging;
using Wallet.Worker.BackgroundJobs;
using Wallet.Worker.InboxHandlers;

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
