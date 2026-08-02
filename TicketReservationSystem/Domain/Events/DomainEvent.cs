namespace TicketReservationSystem.Domain.Events
{
    public abstract class DomainEvent
    {
        public DateTime OccurredOn { get; private set; } = DateTime.UtcNow;
        public string EventType => GetType().Name;

        protected DomainEvent()
        {
        }
    }
}
