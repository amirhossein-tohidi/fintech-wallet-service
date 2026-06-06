using System.Text.Json;
using Wallet.Application.Abstractions.Messaging;
using Wallet.Contracts.Enums;
using Wallet.Contracts.IntegrationEvents;

namespace Wallet.Worker.InboxHandlers;

public abstract class InboxMessageHandler<TPayload>(ILogger logger) : IInboxMessageHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public abstract IntegrationEventType EventType { get; }

    public async Task HandleAsync(string payload, CancellationToken ct)
    {
        var envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope<TPayload>>(
            json: payload,
            options: JsonOptions);

        if (envelope == null)
        {
            throw new InvalidOperationException(
                $"Inbox payload for event type {EventType} could not be deserialized.");
        }

        if (envelope.Type != EventType)
        {
            throw new InvalidOperationException(
                $"Inbox payload type mismatch. Expected {EventType}, received {envelope.Type}.");
        }

        await HandleEnvelopeAsync(envelope: envelope, ct: ct);
    }

    protected virtual Task HandleEnvelopeAsync(
        IntegrationEventEnvelope<TPayload> envelope,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Inbox event {EventType} consumed. EventId={EventId}, OccurredOn={OccurredOn}",
            envelope.Type,
            envelope.Id,
            envelope.OccurredOn);

        return Task.CompletedTask;
    }
}
