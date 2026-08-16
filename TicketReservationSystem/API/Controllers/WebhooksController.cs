using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands.Payments;
using TicketReservationSystem.Infrastructure.Services.Payments;

namespace TicketReservationSystem.API.Controllers
{
    [ApiController]
    [Route("webhooks")]
    [AllowAnonymous]
    public class WebhooksController : ControllerBase
    {
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly StripeSettings _stripeSettings;

        public WebhooksController(ICommandDispatcher commandDispatcher, IOptions<StripeSettings> stripeSettings)
        {
            _commandDispatcher = commandDispatcher;
            _stripeSettings = stripeSettings.Value;
        }

        [HttpPost("stripe")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(Request.Body).ReadToEndAsync();
            var signatureHeader = Request.Headers["Stripe-Signature"];

            if (string.IsNullOrEmpty(signatureHeader))
                return BadRequest();

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
            var result = await _commandDispatcher.DispatchAsync<StripeWebhookCommand, Result>(command);

            if (result.IsFailure)
                return StatusCode(500);

            return Ok();
        }
    }
}
