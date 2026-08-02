using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.Queries.Tickets
{
    public class GetTicketsByEventQuery : IQuery<GetTicketsByEventResult>
    {
        public SocialEventId EventId { get; }

        public GetTicketsByEventQuery(SocialEventId eventId)
        {
            EventId = eventId;
        }
    }
}
