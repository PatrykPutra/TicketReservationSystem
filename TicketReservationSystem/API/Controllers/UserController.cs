using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketReservationSystem.API;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands.Users;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Application.Queries.Users;
using TicketReservationSystem.Application.Requests;

namespace TicketReservationSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly ICommandDispatcher _commandDispatcher;
        public UserController(IQueryDispatcher queryDispatcher, ICommandDispatcher commandDispatcher)
        {
            _queryDispatcher = queryDispatcher;
            _commandDispatcher = commandDispatcher;
        }

        [Authorize]
        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> GetUser(Guid userId)
        {
            if (!User.TryGetUserId(out var authenticatedUserId) || authenticatedUserId != userId)
                return Unauthorized();

            var query = new GetUserQuery(Domain.Ids.UserId.Create(userId));
            var result = await _queryDispatcher.ExecuteAsync<GetUserQuery, GetUserResult>(query);

            if (result.User is null)
                return NotFound();

            return Ok(result.User);
        }

        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] AddUserRequest request)
        {
            var command = new AddUserCommand(
                request.Email,
                request.FirstName,
                request.LastName,
                request.PhoneNumber);

            var result = await _commandDispatcher.DispatchAsync<AddUserCommand, AddUserResult>(command);

            if (result.IsFailure)
                return ErrorToActionResult(result.Error);

            return Ok(result.Value);
        }

        private static IActionResult ErrorToActionResult(Error error)
        {
            return error switch
            {
                NotFoundError => new NotFoundResult(),
                CurrencyMismatchError => new BadRequestResult(),
                UserAlreadyExistsError => new ConflictResult(),
                _ => new StatusCodeResult(500)
            };
        }
    }
}
