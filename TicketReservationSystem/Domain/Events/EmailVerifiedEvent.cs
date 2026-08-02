namespace TicketReservationSystem.Domain.Events
{
    using TicketReservationSystem.Domain.Ids;

    public class EmailVerifiedEvent : DomainEvent
    {
        public UserId UserId { get; }

        public EmailVerifiedEvent(UserId userId)
        {
            UserId = userId;
        }
    }
}
