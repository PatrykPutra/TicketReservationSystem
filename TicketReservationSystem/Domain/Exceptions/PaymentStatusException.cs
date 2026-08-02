namespace TicketReservationSystem.Domain.Exceptions
{
    public class PaymentStatusException : DomainException
    {
        public PaymentStatusException(string message)
            : base(message) { }
    }
}