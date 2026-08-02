namespace TicketReservationSystem.Domain.Ids
{
    public readonly record struct TicketId(Guid Value)
    {
        public static TicketId Create(Guid Value)
        {
            return new TicketId(Value);
        }

        public static TicketId CreateUnique()
        {
            return new TicketId(Guid.NewGuid());
        }
    }
}
