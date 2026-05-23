using Wallet.Contracts.Enums;

namespace Wallet.Infrastructure.Persistence.Messaging;

public class InboxMessage
{
    private InboxMessage()
    {
    }

    public InboxMessage(
        Guid id,
        IntegrationEventType eventType,
        string payload,
        DateTime occurredOn)
    {
        Id = id;
        EventType = eventType;
        Payload = payload;
        OccurredOn = occurredOn;
        ReceivedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public IntegrationEventType EventType { get; private set; }
    public DateTime OccurredOn { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public DateTime? LastAttemptedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? DeadLetteredAt { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public string? LockedBy { get; private set; }
    public int RetryCount { get; private set; }
    public int DeadLetterRetryCount { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    public string? Error { get; private set; }

    public void Lock(string workerId, TimeSpan duration)
    {
        LockedBy = workerId;
        LockedUntil = DateTime.UtcNow.Add(duration);
    }

    public void MarkAsProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
        DeadLetteredAt = null;
        LockedBy = null;
        LockedUntil = null;
        Error = null;
    }

    public void MarkAsFailed(string error, int maxRetryCount)
    {
        RetryCount++;
        LastAttemptedAt = DateTime.UtcNow;
        Error = error;
        LockedBy = null;
        LockedUntil = null;

        if (RetryCount >= maxRetryCount)
        {
            DeadLetteredAt = DateTime.UtcNow;
        }
    }

    public void MarkDeadLetterRetryFailed(string error)
    {
        DeadLetterRetryCount++;
        LastAttemptedAt = DateTime.UtcNow;
        Error = error;
        LockedBy = null;
        LockedUntil = null;
    }
}
