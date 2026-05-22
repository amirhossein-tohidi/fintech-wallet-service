namespace Wallet.Domain.Common;

public abstract class AggregateRoot : BaseEntity
{
    public byte[] RowVersion { get; private set; } = [];

    private readonly List<object> _domainEvents = [];
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(object @event)
        => _domainEvents.Add(@event);

    public void ClearDomainEvents()
        => _domainEvents.Clear();
}