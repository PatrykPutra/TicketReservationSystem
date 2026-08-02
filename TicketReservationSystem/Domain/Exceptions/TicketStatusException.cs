namespace TicketReservationSystem.Domain.Exceptions
{
    public class TicketStatusException : DomainException
    {
        public TicketStatusException(string message)
            : base(message) { }
    }
}
