namespace TicketReservationSystem.Domain.Exceptions
{
    public class UnauthorizedUserException : DomainException
    {
        public UnauthorizedUserException()
            : base("The user is not authorized to perform this action.") { }

        public UnauthorizedUserException(string message)
            : base(message) { }
    }
}
