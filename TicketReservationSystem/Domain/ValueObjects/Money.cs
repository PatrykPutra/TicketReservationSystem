namespace TicketReservationSystem.Domain.ValueObjects
{
    public record struct Money
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

    }
}
