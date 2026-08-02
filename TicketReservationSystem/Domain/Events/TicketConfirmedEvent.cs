namespace TicketReservationSystem.Domain.Events
{
    using TicketReservationSystem.Domain.Ids;

        public class TicketConfirmedEvent : DomainEvent
        {
            public TicketId TicketId { get; }
            public UserId UserId { get; }
            public SocialEventId EventId { get; }
            public DateTime ConfirmedAt { get; }

            public TicketConfirmedEvent(TicketId ticketId, UserId userId, SocialEventId eventId, DateTime? confirmedAt = null)
            {
                TicketId = ticketId;
                UserId = userId;
                EventId = eventId;
                ConfirmedAt = confirmedAt ?? DateTime.UtcNow;
            }

        }
}
