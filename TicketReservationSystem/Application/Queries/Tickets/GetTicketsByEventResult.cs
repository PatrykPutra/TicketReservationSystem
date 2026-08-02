using TicketReservationSystem.Application.DTOs;

namespace TicketReservationSystem.Application.Queries.Tickets
{
    public class GetTicketsByEventResult
    {
        public List<TicketDto> Tickets { get; }

        public GetTicketsByEventResult(List<TicketDto> tickets)
        {
            Tickets = tickets;
        }
    }
}
