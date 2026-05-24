using Wallet.Contracts.Enums;

namespace Wallet.Infrastructure.Persistence.Messaging;

public class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public OutboxMessage(
        IntegrationEventType eventType,
        string payload,
        DateTime occurredOn)
    {
        Id = Guid.NewGuid();
        EventType = eventType;
        Payload = payload;
        OccurredOn = occurredOn;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public IntegrationEventType EventType { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredOn { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastAttemptedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? DeadLetteredAt { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public string? LockedBy { get; private set; }
    public int RetryCount { get; private set; }
    public int DeadLetterRetryCount { get; private set; }
    public string? Error { get; private set; }

    public void Lock(string workerId, TimeSpan duration)
    {
        LockedBy = workerId;
        LockedUntil = DateTime.UtcNow.Add(duration);
    }

    public void MarkAsProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
        LockedBy = null;
        LockedUntil = null;
        DeadLetteredAt = null;
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
