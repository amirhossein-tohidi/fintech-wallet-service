using Wallet.Domain.Common;
using Wallet.Domain.Enums;

namespace Wallet.Domain.Idempotency;


public class IdempotencyRequest : BaseEntity
{
    public string Key { get; private set; } = string.Empty;
    public string Endpoint { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public string ResponseBody { get; private set; } = string.Empty;
    public IdempotencyStatus Status { get; private set; }
    public DateTime ExpireAt { get; private set; }

    private IdempotencyRequest() { }

    public IdempotencyRequest(
        string key,
        string endpoint,
        string requestHash,
        DateTime expireAt)
    {
        Key = key;
        Endpoint = endpoint;
        RequestHash = requestHash;
        ExpireAt = expireAt;
        Status = IdempotencyStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsCompleted(string responseBody)
    {
        ResponseBody = responseBody;
        Status = IdempotencyStatus.Completed;
        MarkAsModified();
    }

    public void MarkAsFailed(string error)
    {
        ResponseBody = error;
        Status = IdempotencyStatus.Failed;
        MarkAsModified();
    }

    public bool IsPending() => Status == IdempotencyStatus.Pending;
    
    public bool IsExpired() => DateTime.UtcNow > ExpireAt;
}