using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wallet.Application.Abstractions.Messaging;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Messaging;

namespace Wallet.Worker.BackgroundJobs;

public sealed class OutboxDeadLetterProcessorWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxProcessorOptions> options,
    ILogger<OutboxDeadLetterProcessorWorker> logger) : BackgroundService
{
    private readonly OutboxProcessorOptions _options = options.Value;
    private readonly string _workerId = $"{Environment.MachineName}-deadletter-{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Outbox dead-letter processor is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDeadLetterBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox dead-letter processor failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.DeadLetterPollingIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessDeadLetterBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();

        var messages = await ClaimDeadLetterMessagesAsync(dbContext: dbContext, ct: ct);

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
                logger.LogWarning(ex, "Dead-letter outbox message {OutboxMessageId} still cannot be published.", message.Id);
                message.MarkDeadLetterRetryFailed(error: ex.Message);
            }
        }

        if (messages.Count > 0)
        {
            await dbContext.SaveChangesAsync(ct);
        }
    }

    private async Task<List<OutboxMessage>> ClaimDeadLetterMessagesAsync(
        WalletDbContext dbContext,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var lockDuration = TimeSpan.FromSeconds(_options.LockSeconds);

        var messageIds = await dbContext.OutboxMessages
            .Where(x =>
                x.ProcessedAt == null &&
                x.DeadLetteredAt != null &&
                (x.LockedUntil == null || x.LockedUntil < now))
            .OrderBy(x => x.DeadLetteredAt)
            .ThenBy(x => x.CreatedAt)
            .Take(_options.DeadLetterBatchSize)
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
                x.DeadLetteredAt != null &&
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
            .OrderBy(x => x.DeadLetteredAt)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(ct);
    }
}
