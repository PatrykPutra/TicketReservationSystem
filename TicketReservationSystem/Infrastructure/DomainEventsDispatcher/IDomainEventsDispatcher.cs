using TicketReservationSystem.Domain.Events;

namespace TicketReservationSystem.Infrastructure.DomainEventsDispatcher
{
    public interface IDomainEventsDispatcher
    {
        Task DispatchAsync(
            IEnumerable<DomainEvent> domainEvents,
            CancellationToken cancellationToken = default);
    }
}
