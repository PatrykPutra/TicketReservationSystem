using Microsoft.Extensions.Options;
using Stripe;
using TicketReservationSystem.Infrastructure.Services.Payments;

namespace TicketReservationSystem.API.Helpers
{
    public class StripeHelperService : IStripeHelperService
    {
        private readonly StripeSettings _stripeSettings;
        public StripeHelperService(IOptions<StripeSettings> stripeSettings)
        {
            _stripeSettings = stripeSettings.Value;
        }
        public Event ConstructEvent(string json, string secret)
        {
            return EventUtility.ConstructEvent(json, secret, _stripeSettings.WebhookSecret);
        }
    }
}
