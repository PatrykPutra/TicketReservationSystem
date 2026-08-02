namespace TicketReservationSystem.Domain.Events
{
    using TicketReservationSystem.Domain.Ids;

        public class TicketReservedEvent : DomainEvent
        {
            public TicketId TicketId { get; }
            public UserId? UserId { get; }
            public SocialEventId EventId { get; }
            public DateTime ReservedAt { get; }

            public TicketReservedEvent(TicketId ticketId, UserId? userId, SocialEventId eventId, DateTime? reservedAt = null)
            {
                TicketId = ticketId;
                UserId = userId;
                EventId = eventId;
                ReservedAt = reservedAt ?? DateTime.UtcNow;
            }

        }
}
