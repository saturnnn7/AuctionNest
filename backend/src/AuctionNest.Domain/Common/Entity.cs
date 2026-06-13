namespace AuctionNest.Domain.Common;

/// <summary>
/// Represents an entity in the domain.
/// </summary>
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity() {  }

    public Guid Id { get; protected set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public IReadOnlyList<IDomainEvent> GetDomainEvents()
        => _domainEvents.AsReadOnly();
    
    public void ClearDomainEvents()
        => _domainEvents.Clear();
    
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);
}