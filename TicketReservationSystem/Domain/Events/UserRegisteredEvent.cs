namespace TicketReservationSystem.Domain.Events
{
    using TicketReservationSystem.Domain.Ids;

    public class UserRegisteredEvent : DomainEvent
    {
        public UserId UserId { get; }
        public string Email { get; }

        public UserRegisteredEvent(UserId userId, string email)
        {
            UserId = userId;
            Email = email;
        }
    }
}
