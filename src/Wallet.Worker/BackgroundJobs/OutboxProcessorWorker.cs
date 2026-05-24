using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wallet.Application.Abstractions.Messaging;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Messaging;

namespace Wallet.Worker.BackgroundJobs;

public sealed class OutboxProcessorWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxProcessorOptions> options,
    ILogger<OutboxProcessorWorker> logger) : BackgroundService
{
    private readonly OutboxProcessorOptions _options = options.Value;
    private readonly string _workerId = $"{Environment.MachineName}-{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Outbox processor is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox processor failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();

        var messages = await ClaimMessagesAsync(dbContext, ct);

        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishAsync(
                    eventType: message.EventType,
                    payload: message.Payload,
                    ct: ct);

                message.MarkAsProcessed();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish outbox message {OutboxMessageId}.", message.Id);
                message.MarkAsFailed(error: ex.Message, maxRetryCount: _options.MaxRetryCount);
            }
        }

        if (messages.Count > 0)
        {
            await dbContext.SaveChangesAsync(ct);
        }
    }

    private async Task<List<OutboxMessage>> ClaimMessagesAsync(
        WalletDbContext dbContext,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var lockDuration = TimeSpan.FromSeconds(_options.LockSeconds);

        var messageIds = await dbContext.OutboxMessages
            .Where(x =>
                x.ProcessedAt == null &&
                x.DeadLetteredAt == null &&
                x.RetryCount < _options.MaxRetryCount &&
                (x.LockedUntil == null || x.LockedUntil < now))
            .OrderBy(x => x.CreatedAt)
            .Take(_options.BatchSize)
            .Select(x => x.Id)
            .ToListAsync(ct);

        if (messageIds.Count == 0)
        {
            return [];
        }

        var claimedCount = await dbContext.OutboxMessages
            .Where(x =>
                messageIds.Contains(x.Id) &&
                x.ProcessedAt == null &&
                x.DeadLetteredAt == null &&
                (x.LockedUntil == null || x.LockedUntil < now))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.LockedBy, _workerId)
                .SetProperty(x => x.LockedUntil, now.Add(lockDuration)), ct);

        if (claimedCount == 0)
        {
            return [];
        }

        return await dbContext.OutboxMessages
            .Where(x => messageIds.Contains(x.Id) && x.LockedBy == _workerId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);
    }
}
