namespace TicketReservationSystem.Domain.Ids
{
    using System;

        public readonly record struct EmailVerificationCodeId(Guid Value)
        {
        public static EmailVerificationCodeId Create(Guid Value)
        {
            return new EmailVerificationCodeId(Value);
        }

        public static EmailVerificationCodeId CreateUnique()
        {
            return new EmailVerificationCodeId(Guid.NewGuid());
        }
    }
}
