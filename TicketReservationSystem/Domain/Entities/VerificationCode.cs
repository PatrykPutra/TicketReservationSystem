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

        private VerificationCode(VerificationCodeId id, UserId userId, string code, DateTime expiresAt) : base(id)
        {
            UserId = userId;
            Code = code;
            ExpiresAt = expiresAt;
            IsUsed = false;
            CreatedAt = DateTime.UtcNow;
        }

        private VerificationCode()
        {
        }

        public static VerificationCode Generate(UserId userId, string email, string code, DateTime expiresAt)
        {
            var id = VerificationCodeId.CreateUnique();
            VerificationCode verificationCode = new VerificationCode(id, userId, code, expiresAt);
            verificationCode.AddDomainEvent(new AuthenticationCodeGeneratedEvent(verificationCode.UserId, email, verificationCode.Code, verificationCode.ExpiresAt));

            return verificationCode;
        }

        public void MarkAsUsed()
        {
            IsUsed = true;
        }
    }
}
