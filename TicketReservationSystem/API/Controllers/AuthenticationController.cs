using Microsoft.AspNetCore.Mvc;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands.Authentication;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Application.Requests;

namespace TicketReservationSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly ICommandDispatcher _commandDispatcher;
        public AuthenticationController(ICommandDispatcher commandDispatcher)
        {
            _commandDispatcher = commandDispatcher;
        }

        [HttpPost("send-code")]
        public async Task<IActionResult> SendCode([FromBody] AuthenticationCodeRequest request)
        {
            var command = new SendAuthenticationCodeCommand(request.Email);
            var result = await _commandDispatcher.DispatchAsync<SendAuthenticationCodeCommand, SendAuthenticationCodeResult>(command);

            if (result.IsFailure)
                return result.Error switch
                {
                    UserNotFoundError => NotFound(),
                    RateLimitedError => StatusCode(429),
                    _ => StatusCode(500)
                };

            return Ok();
        }

        [HttpPost("token")]
        public async Task<IActionResult> Token([FromBody] AuthenticationTokenRequest request)
        {
            var command = new GenerateTokenCommand(request.Email, request.Code);
            var result = await _commandDispatcher.DispatchAsync<GenerateTokenCommand, GenerateTokenResult>(command);

            if (result.IsFailure)
                return Unauthorized();

            return Ok(new { token = result.Value.Token, expiresAt = result.Value.ExpiresAt });
        }
    }
}
