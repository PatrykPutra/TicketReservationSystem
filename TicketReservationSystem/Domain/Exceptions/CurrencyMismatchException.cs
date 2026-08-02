namespace TicketReservationSystem.Domain.Exceptions
{

    public class CurrencyMismatchException : DomainException
    {
        public CurrencyMismatchException()
            : base("Currency mismatch between monetary values.") { }

        public CurrencyMismatchException(string message)
            : base(message) { }
    }
}
