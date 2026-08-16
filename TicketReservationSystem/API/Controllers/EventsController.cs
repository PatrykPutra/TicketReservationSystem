using Microsoft.AspNetCore.Mvc;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Application.Queries.Events;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IQueryDispatcher _queryDispatcher;
        public EventsController(IQueryDispatcher queryDispatcher)
        {
            _queryDispatcher = queryDispatcher;
        }

        [HttpGet("{eventId:guid}")]
        [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEventById(Guid eventId)
        {
            var query = new GetEventByIdQuery(SocialEventId.Create(eventId));
            var result = await _queryDispatcher.ExecuteAsync<GetEventByIdQuery, GetEventByIdResult>(query);

            if (result.Event is null)
                return NotFound();

            return Ok(result.Event);
        }

        [HttpGet()]
        [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEvents()
        {
            var query = new GetEventsQuery();
            var result = await _queryDispatcher.ExecuteAsync<GetEventsQuery, GetEventsResult>(query);
            return Ok(result.Events);
        }
    }
}
