namespace TicketReservationSystem.Application.Requests
{
    public class PaymentCheckoutRequest
    {
        public Guid TicketId { get; set; }
        public Guid UserId { get; set; }
    }
}