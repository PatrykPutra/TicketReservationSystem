using Stripe;
namespace TicketReservationSystem.API.Helpers
{
    public interface IStripeHelperService
    {
        Event ConstructEvent(string json, string secret);
    }
}
