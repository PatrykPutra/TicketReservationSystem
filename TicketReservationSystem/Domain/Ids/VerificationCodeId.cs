namespace TicketReservationSystem.Domain.Ids
{
    using System;

    public readonly record struct VerificationCodeId(Guid Value)
    {
        public static VerificationCodeId Create(Guid Value)
        {
            return new VerificationCodeId(Value);
        }

        public static VerificationCodeId CreateUnique()
        {
            return new VerificationCodeId(Guid.NewGuid());
        }
    }
}
