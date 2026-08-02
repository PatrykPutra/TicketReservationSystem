using Stripe;
using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Commands.Payments
{
    public class StripeWebhookCommand : ICommand<Result>
    {
        public Event StripeEvent { get; }

        public StripeWebhookCommand(Event stripeEvent)
        {
            StripeEvent = stripeEvent;
        }
    }
}