using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.Queries.Events
{
    public class GetEventByIdQuery : IQuery<GetEventByIdResult>
    {
        public SocialEventId Id { get; }

        public GetEventByIdQuery(SocialEventId id)
        {
            Id = id;
        }
    }
}
