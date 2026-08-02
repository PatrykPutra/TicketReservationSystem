namespace TicketReservationSystem.Application.Requests
{
    public class TicketReservationRequest
    {
        public Guid TicketId { get; set; }
        public Guid UserId { get; set; }
    }
}
