namespace TicketReservationSystem.Application.Requests
{
    public class TicketConfirmationRequest
    {
        public Guid TicketId { get; set; }
        public Guid UserId { get; set; }
        public DateTime ConfirmedAt { get; set; }
    }
}
