using TicketReservationSystem.Application.DTOs;

namespace TicketReservationSystem.Application.Queries.Tickets
{
    public class GetTicketByIdResult
    {
        public TicketDto? Ticket { get; }

        public GetTicketByIdResult(TicketDto? ticket)
        {
            Ticket = ticket;
        }
    }
}
