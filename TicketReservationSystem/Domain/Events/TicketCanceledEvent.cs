using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Domain.Events
{
    public class TicketCanceledEvent : DomainEvent
    {
        public TicketId TicketId { get; }
        public UserId UserId { get; }
        public SocialEventId EventId { get; }
        public DateTime CanceledAt { get; }

        public TicketCanceledEvent(TicketId ticketId, UserId userId, SocialEventId eventId, DateTime? canceledAt = null)
        {
            TicketId = ticketId;
            UserId = userId;
            EventId = eventId;
            CanceledAt = canceledAt ?? DateTime.UtcNow;
        }
    }
}
