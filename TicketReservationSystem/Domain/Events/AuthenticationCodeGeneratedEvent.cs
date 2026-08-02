namespace TicketReservationSystem.Domain.Events
{
    using TicketReservationSystem.Domain.Ids;

    public class AuthenticationCodeGeneratedEvent : DomainEvent
    {
        public UserId UserId { get; }
        public string Email { get; }
        public string Code { get; }
        public DateTime ExpiresAt { get; }

        public AuthenticationCodeGeneratedEvent(UserId userId, string email, string code, DateTime expiresAt)
        {
            UserId = userId;
            Email = email;
            Code = code;
            ExpiresAt = expiresAt;
        }
    }
}
