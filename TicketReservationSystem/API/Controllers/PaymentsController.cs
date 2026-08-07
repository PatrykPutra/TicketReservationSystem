using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketReservationSystem.API;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands.Payments;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Application.Requests;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.API.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly ICommandDispatcher _commandDispatcher;

        public PaymentsController(ICommandDispatcher commandDispatcher)
        {
            _commandDispatcher = commandDispatcher;
        }

        [Authorize]
        [HttpPost("checkout")]
        public async Task<IActionResult> CreateCheckout([FromBody] PaymentCheckoutRequest request)
        {
            if (!User.TryGetUserId(out var userId) || userId != request.UserId)
                return Unauthorized();

            var command = new CreateCheckoutCommand(
                TicketId.Create(request.TicketId),
                UserId.Create(request.UserId));

            var result = await _commandDispatcher.DispatchAsync<CreateCheckoutCommand, CreateCheckoutResult>(command);

            if (result.IsFailure)
                return ErrorToActionResult(result.Error);

            return Ok(result.Value);
        }

        private static IActionResult ErrorToActionResult(Error error)
        {
            return error switch
            {
                TicketNotReservedError => new ConflictResult(),
                TicketNotAvailableError => new ConflictResult(),
                DuplicatePaymentError => new ConflictResult(),
                UnauthorizedUserError => new UnauthorizedResult(),
                NotFoundError => new NotFoundResult(),
                CurrencyMismatchError => new BadRequestResult(),
                UnsupportedCurrencyError => new BadRequestResult(),
                _ => new StatusCodeResult(500)
            };
        }
    }
}
