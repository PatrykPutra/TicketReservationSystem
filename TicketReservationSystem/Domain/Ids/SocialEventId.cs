namespace TicketReservationSystem.Domain.Ids
{
    public readonly record struct SocialEventId(Guid Value) 
    {
        public static SocialEventId Create(Guid Value)
        {
            return new SocialEventId(Value);
        }

        public static SocialEventId CreateUnique()
        {
            return new SocialEventId(Guid.NewGuid());
        }
    }
}
