namespace TicketReservationSystem.Domain.Ids
{
    using System;

        public readonly record struct UserId(Guid Value)
        {
        public static UserId Create(Guid Value)
        {
            return new UserId(Value);
        }

        public static UserId CreateUnique()
        {
            return new UserId(Guid.NewGuid());
        }
    }
}
