namespace TicketReservationSystem.Application.Requests
{
    public class TicketCancelationRequest
    {
        public Guid TicketId { get; set; }
        public Guid UserId { get; set; }
    }
}
