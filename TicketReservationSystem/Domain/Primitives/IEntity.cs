using TicketReservationSystem.Domain.Events;

namespace TicketReservationSystem.Domain.Primitives
{
    public interface IEntity
    {
        IReadOnlyCollection<DomainEvent> DomainEvents { get; }

        void ClearDomainEvents();
    }
}