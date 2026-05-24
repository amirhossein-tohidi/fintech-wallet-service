using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wallet.Application.Abstractions.Messaging;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Messaging;

namespace Wallet.Worker.BackgroundJobs;

public sealed class InboxProcessorWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<InboxProcessorOptions> options,
    ILogger<InboxProcessorWorker> logger) : BackgroundService
{
    private readonly InboxProcessorOptions _options = options.Value;
    private readonly string _workerId = $"{Environment.MachineName}-inbox-{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Inbox processor is disabled.");
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
                logger.LogError(ex, "Inbox processor failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
        var handlers = scope.ServiceProvider
            .GetServices<IInboxMessageHandler>()
            .ToDictionary(handler => handler.EventType);

        var messages = await ClaimMessagesAsync(dbContext: dbContext, ct: ct);

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
                logger.LogError(ex, "Failed to process inbox message {InboxMessageId}.", message.Id);
                message.MarkAsFailed(error: ex.Message, maxRetryCount: _options.MaxRetryCount);
            }
        }

        if (messages.Count > 0)
        {
            await dbContext.SaveChangesAsync(ct);
        }
    }

    private async Task<List<InboxMessage>> ClaimMessagesAsync(
        WalletDbContext dbContext,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var lockDuration = TimeSpan.FromSeconds(_options.LockSeconds);

        var messageIds = await dbContext.InboxMessages
            .Where(x =>
                x.ProcessedAt == null &&
                x.DeadLetteredAt == null &&
                x.RetryCount < _options.MaxRetryCount &&
                (x.LockedUntil == null || x.LockedUntil < now))
            .OrderBy(x => x.ReceivedAt)
            .Take(_options.BatchSize)
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
                x.DeadLetteredAt == null &&
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
            .OrderBy(x => x.ReceivedAt)
            .ToListAsync(ct);
    }
}
