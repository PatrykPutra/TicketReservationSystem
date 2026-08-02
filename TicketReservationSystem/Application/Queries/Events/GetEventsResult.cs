using TicketReservationSystem.Application.DTOs;

namespace TicketReservationSystem.Application.Queries.Events
{
    public class GetEventsResult
    {
        public List<EventDto> Events { get; }

        public GetEventsResult(List<EventDto> events)
        {
            Events = events;
        }
    }
}