using System.Text;
using System.Text.Json;
using KafkaFlow;
using Microsoft.EntityFrameworkCore;
using Wallet.Contracts.Enums;
using Wallet.Contracts.IntegrationEvents;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Persistence.Messaging;

namespace Wallet.Worker.Messaging;

public sealed class KafkaInboxMiddleware(
    IServiceScopeFactory scopeFactory,
    ILogger<KafkaInboxMiddleware> logger) : IMessageMiddleware
{
    public async Task Invoke(IMessageContext context, MiddlewareDelegate next)
    {
        var payload = GetPayload(context);
        var metadata = ReadEnvelopeMetadata(payload);

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WalletDbContext>();

        if (await dbContext.InboxMessages.AnyAsync(x => x.Id == metadata.Id, context.ConsumerContext.WorkerStopped))
        {
            logger.LogDebug(
                "Kafka event {EventId} already exists in inbox. Topic={Topic}, Partition={Partition}, Offset={Offset}",
                metadata.Id,
                context.ConsumerContext.Topic,
                context.ConsumerContext.Partition,
                context.ConsumerContext.Offset);

            context.ConsumerContext.Complete();
            return;
        }

        dbContext.InboxMessages.Add(new InboxMessage(
            id: metadata.Id,
            eventType: metadata.EventType,
            payload: payload,
            occurredOn: metadata.OccurredOn));

        try
        {
            await dbContext.SaveChangesAsync(context.ConsumerContext.WorkerStopped);
        }
        catch (DbUpdateException)
        {
            if (!await dbContext.InboxMessages.AnyAsync(x => x.Id == metadata.Id, context.ConsumerContext.WorkerStopped))
            {
                throw;
            }

            logger.LogDebug(
                "Kafka event {EventId} was inserted by another worker while this message was being consumed.",
                metadata.Id);
        }

        await next(context);
        context.ConsumerContext.Complete();
    }

    private static string GetPayload(IMessageContext context)
    {
        return context.Message.Value switch
        {
            string value => value,
            byte[] value => Encoding.UTF8.GetString(value),
            _ => throw new InvalidOperationException(
                $"Kafka message value type {context.Message.Value?.GetType().Name ?? "<null>"} is not supported.")
        };
    }

    private static KafkaEnvelopeMetadata ReadEnvelopeMetadata(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        var eventType = ReadEventType(root.GetProperty("type"));
        _ = eventType.GetPayloadType();

        return new KafkaEnvelopeMetadata(
            Id: root.GetProperty("id").GetGuid(),
            EventType: eventType,
            OccurredOn: root.GetProperty("occurredOn").GetDateTime());
    }

    private static IntegrationEventType ReadEventType(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => (IntegrationEventType)element.GetInt32(),
            JsonValueKind.String when Enum.TryParse<IntegrationEventType>(
                element.GetString(),
                ignoreCase: true,
                out var eventType) => eventType,
            _ => throw new InvalidOperationException("Kafka event envelope has an invalid integration event type.")
        };
    }

    private sealed record KafkaEnvelopeMetadata(
        Guid Id,
        IntegrationEventType EventType,
        DateTime OccurredOn);
}
