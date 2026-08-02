using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.Queries.Tickets
{
    public class GetTicketByIdQuery : IQuery<GetTicketByIdResult>
    {
        public TicketId TicketId { get; }

        public GetTicketByIdQuery(TicketId ticketId)
        {
            TicketId = ticketId;
        }
    }
}
