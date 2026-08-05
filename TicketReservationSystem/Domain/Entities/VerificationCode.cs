namespace TicketReservationSystem.Domain.Entities
{
    using TicketReservationSystem.Domain.Events;
    using TicketReservationSystem.Domain.Ids;
    using TicketReservationSystem.Domain.Primitives;

    public class VerificationCode : AggregateRoot<VerificationCodeId>
    {
        public UserId UserId { get; private set; }
        public string Code { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public bool IsUsed { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public VerificationCode(VerificationCodeId id) : base(id)
        {
            CreatedAt = DateTime.UtcNow;
        }

        private VerificationCode()
        {
        }

        public static VerificationCode Generate(UserId userId, string email, string code, DateTime expiresAt)
        {
            var id = VerificationCodeId.CreateUnique();
            var entity = new VerificationCode(id)
            {
                UserId = userId,
                Code = code,
                ExpiresAt = expiresAt,
            };

            entity.AddDomainEvent(new AuthenticationCodeGeneratedEvent(userId, email, code, expiresAt));

            return entity;
        }

        public void MarkAsUsed()
        {
            IsUsed = true;
        }
    }
}
