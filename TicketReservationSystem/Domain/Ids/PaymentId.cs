namespace TicketReservationSystem.Domain.Ids
{
    public readonly record struct PaymentId(Guid Value)
    {
        public static PaymentId Create(Guid Value)
        {
            return new PaymentId(Value);
        }

        public static PaymentId CreateUnique()
        {
            return new PaymentId(Guid.NewGuid());
        }
    }
}