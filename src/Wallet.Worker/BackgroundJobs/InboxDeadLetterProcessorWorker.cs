using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wallet.Application.Abstractions.Messaging;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Messaging;

namespace Wallet.Worker.BackgroundJobs;

public sealed class InboxDeadLetterProcessorWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<InboxProcessorOptions> options,
    ILogger<InboxDeadLetterProcessorWorker> logger) : BackgroundService
{
    private readonly InboxProcessorOptions _options = options.Value;
    private readonly string _workerId = $"{Environment.MachineName}-inbox-deadletter-{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Inbox dead-letter processor is disabled.");
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
                logger.LogError(ex, "Inbox dead-letter processor failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.DeadLetterPollingIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessDeadLetterBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
        var handlers = scope.ServiceProvider
            .GetServices<IInboxMessageHandler>()
            .ToDictionary(handler => handler.EventType);

        var messages = await ClaimDeadLetterMessagesAsync(dbContext: dbContext, ct: ct);

        foreach (var message in messages)
        {
            try
            {
                if (!handlers.TryGetValue(message.EventType, out var handler))
                {
                    throw new InvalidOperationException(
                        $"No inbox handler is registered for event type {message.EventType}.");
                }

                await handler.HandleAsync(payload: message.Payload, ct: ct);
                message.MarkAsProcessed();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Dead-letter inbox message {InboxMessageId} still cannot be processed.", message.Id);
                message.MarkDeadLetterRetryFailed(error: ex.Message);
            }
        }

        if (messages.Count > 0)
        {
            await dbContext.SaveChangesAsync(ct);
        }
    }

    private async Task<List<InboxMessage>> ClaimDeadLetterMessagesAsync(
        WalletDbContext dbContext,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var lockDuration = TimeSpan.FromSeconds(_options.LockSeconds);

        var messageIds = await dbContext.InboxMessages
            .Where(x =>
                x.ProcessedAt == null &&
                x.DeadLetteredAt != null &&
                (x.LockedUntil == null || x.LockedUntil < now))
            .OrderBy(x => x.DeadLetteredAt)
            .ThenBy(x => x.ReceivedAt)
            .Take(_options.DeadLetterBatchSize)
            .Select(x => x.Id)
            .ToListAsync(ct);

        if (messageIds.Count == 0)
        {
            return [];
        }

        var claimedCount = await dbContext.InboxMessages
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

        return await dbContext.InboxMessages
            .Where(x => messageIds.Contains(x.Id) && x.LockedBy == _workerId)
            .OrderBy(x => x.DeadLetteredAt)
            .ThenBy(x => x.ReceivedAt)
            .ToListAsync(ct);
    }
}
