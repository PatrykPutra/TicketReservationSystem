using TicketReservationSystem.Application.DTOs;

namespace TicketReservationSystem.Application.Queries.Events
{
    public class GetEventByIdResult
    {
        public EventDto? Event { get; }

        public GetEventByIdResult(EventDto? eventDto)
        {
            Event = eventDto;
        }
    }
}