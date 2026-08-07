namespace TicketReservationSystem.Domain.Events
{
    using TicketReservationSystem.Domain.Ids;

        public class TicketReleasedEvent : DomainEvent
        {
            public TicketId TicketId { get; }
            public UserId UserId { get; }
            public SocialEventId EventId { get; }
            public DateTime ReleasedAt { get; }
            public TicketReleasedEvent(TicketId ticketId, UserId userId, SocialEventId eventId, DateTime? releasedAt = null)
            {
                TicketId = ticketId;
                UserId = userId;
                EventId = eventId;
                ReleasedAt = releasedAt ?? DateTime.UtcNow;
            }
        }
}
