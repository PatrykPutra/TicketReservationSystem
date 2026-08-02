namespace TicketReservationSystem.Domain.Entities
{
    using TicketReservationSystem.Domain.Events;
    using TicketReservationSystem.Domain.Ids;
    using TicketReservationSystem.Domain.Primitives;

        public class EmailVerificationCode : AggregateRoot<EmailVerificationCodeId>
        {
            public UserId UserId { get; private set; }
            public string Code { get; private set; }
            public DateTime ExpiresAt { get; private set; }
            public bool IsUsed { get; private set; }
            public DateTime CreatedAt { get; private set; }

            public EmailVerificationCode(EmailVerificationCodeId id) : base(id)
            {
                CreatedAt = DateTime.UtcNow;
            }

            private EmailVerificationCode()
            {
            }

            public static EmailVerificationCode Generate(UserId userId, string email, string code, DateTime expiresAt)
            {
                var id = EmailVerificationCodeId.CreateUnique();
                var entity = new EmailVerificationCode(id)
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
