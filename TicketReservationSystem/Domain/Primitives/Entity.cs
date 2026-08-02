
using TicketReservationSystem.Domain.Events;

namespace TicketReservationSystem.Domain.Primitives
{
    public abstract class Entity<TId> : IEquatable<Entity<TId>>, IEntity where TId : notnull
    {
        public TId Id { get; protected set; }
        private readonly List<DomainEvent> _domainEvents = [];
        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents;

        protected Entity(TId id)
        {
            Id = id;
        }

        protected Entity()
        {
            //parameterless constructor for EF Core 
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (obj.GetType() != GetType()) return false;
            if (obj is not Entity<TId> entity) return false;

            return Id.Equals(entity.Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public bool Equals(Entity<TId>? other)
        {
            if (other is null) return false;
            if (other.GetType() != GetType()) return false;

            return Id.Equals(other.Id);
        }

        public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        {
            return left is not null && right is not null && left.Equals(right);
        }

        public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
        {
            return !(left == right);
        }

        protected void AddDomainEvent(DomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
