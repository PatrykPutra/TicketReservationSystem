using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands.Payments;
using TicketReservationSystem.Application.Requests;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Infrastructure.Payments;

namespace TicketReservationSystem.API.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly StripeSettings _stripeSettings;

        public PaymentsController(ICommandDispatcher commandDispatcher, IOptions<StripeSettings> stripeSettings)
        {
            _commandDispatcher = commandDispatcher;
            _stripeSettings = stripeSettings.Value;
        }

        [Authorize]
        [HttpPost("checkout")]
        public async Task<IActionResult> CreateCheckout([FromBody] PaymentCheckoutRequest request)
        {
            var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId) || userId != request.UserId)
                return Unauthorized();

            var command = new CreateCheckoutCommand(
                TicketId.Create(request.TicketId),
                UserId.Create(request.UserId));

            var result = await _commandDispatcher.DispatchAsync<CreateCheckoutCommand, CreateCheckoutResult>(command);

            if (result.IsFailure)
                return ErrorToActionResult(result.Error);

            return Ok(result.Value);
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(Request.Body).ReadToEndAsync();
            var signatureHeader = Request.Headers["Stripe-Signature"];

            Stripe.Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, _stripeSettings.WebhookSecret);
            }
            catch (StripeException)
            {
                return BadRequest();
            }

            var command = new StripeWebhookCommand(stripeEvent);
            await _commandDispatcher.DispatchAsync<StripeWebhookCommand, Result>(command);

            return Ok();
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
                _ => new StatusCodeResult(500)
            };
        }
    }
}