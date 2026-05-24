namespace Wallet.Domain.Common;

public abstract class BaseEntity
{
    public long Id { get; protected set; }

    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; protected set; }
    
    protected void MarkAsModified()
    {
        ModifiedAt = DateTime.UtcNow;
    }
}