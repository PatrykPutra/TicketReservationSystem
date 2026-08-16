using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands.Tickets;
using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Application.Queries.Tickets;
using TicketReservationSystem.Application.Requests;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly ICommandDispatcher _commandDispatcher;
        public TicketsController(IQueryDispatcher queryDispatcher, ICommandDispatcher commandDispatcher)
        {
            _queryDispatcher = queryDispatcher;
            _commandDispatcher = commandDispatcher;
        }

        [AllowAnonymous]
        [HttpGet("{ticketId:guid}")]
        [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTicketById(Guid ticketId)
        {
            var query = new GetTicketByIdQuery(TicketId.Create(ticketId));
            var result = await _queryDispatcher.ExecuteAsync<GetTicketByIdQuery, GetTicketByIdResult>(query);

            if (result.Ticket is null)
                return NotFound();

            return Ok(result.Ticket);
        }

        [AllowAnonymous]
        [HttpGet("{eventId:guid}/tickets")]
        [ProducesResponseType(typeof(List<TicketDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTicketByEvent(Guid eventId)
        {
            var query = new GetTicketsByEventQuery(SocialEventId.Create(eventId));
            var result = await _queryDispatcher.ExecuteAsync<GetTicketsByEventQuery, GetTicketsByEventResult>(query);
            return Ok(result.Tickets);
        }

        [HttpPost("{ticketId:guid}/reserve")]
        [ProducesResponseType(typeof(TicketReservationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Reserve(Guid ticketId, [FromBody] TicketReservationRequest request)
        {
            if (request.TicketId != ticketId)
                return BadRequest();

            if (!User.TryGetUserId(out var userId) || userId != request.UserId)
                return Unauthorized();

            var command = new TicketReservationCommand(
                TicketId.Create(ticketId),
                UserId.Create(request.UserId));

            var result = await _commandDispatcher.DispatchAsync<TicketReservationCommand, TicketReservationResult>(command);

            if (result.IsFailure)
                return ErrorToActionResult(result.Error);

            return Ok(result.Value);
        }

        [HttpPost("{ticketId:guid}/cancel")]
        [ProducesResponseType(typeof(TicketCancelationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Cancel(Guid ticketId, [FromBody] TicketCancelationRequest request)
        {
            if (request.TicketId != ticketId)
                return BadRequest();

            if (!User.TryGetUserId(out var userId) || userId != request.UserId)
                return Unauthorized();

            var command = new TicketCancelationCommand(
                TicketId.Create(ticketId),
                UserId.Create(request.UserId));

            var result = await _commandDispatcher.DispatchAsync<TicketCancelationCommand, TicketCancelationResult>(command);

            if (result.IsFailure)
                return ErrorToActionResult(result.Error);

            return Ok(result.Value);
        }

        private static IActionResult ErrorToActionResult(Error error)
        {
            return error switch
            {
                TicketNotAvailableError => new ConflictResult(),
                UnauthorizedUserError => new UnauthorizedResult(),
                NotFoundError => new NotFoundResult(),
                _ => new StatusCodeResult(500)
            };
        }
    }
}
