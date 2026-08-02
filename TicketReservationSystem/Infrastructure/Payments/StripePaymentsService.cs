using Microsoft.Extensions.Options;
using Stripe.Checkout;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.ValueObjects;

namespace TicketReservationSystem.Infrastructure.Payments
{
    public class StripePaymentsService : IPaymentsService
    {
        private const int MinorUnitDivisor = 100;

        private readonly StripeSettings _settings;
        private readonly SessionService _sessionService;

        public StripePaymentsService(IOptions<StripeSettings> settings)
        {
            _settings = settings.Value;
            _sessionService = new SessionService();
        }

        public async Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(
            Money amount,
            PaymentId paymentId,
            CancellationToken cancellationToken = default)
        {
            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = _settings.SuccessUrl,
                CancelUrl = _settings.CancelUrl,
                ClientReferenceId = paymentId.Value.ToString(),
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = _settings.Currency,
                            UnitAmount = ToMinorUnits(amount),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Ticket purchase",
                            },
                        },
                    },
                ],
            };

            var session = await _sessionService.CreateAsync(options, cancellationToken: cancellationToken);

            return new CreateCheckoutSessionResult(session.Url, session.Id);
        }

        private static long ToMinorUnits(Money money)
        {
            return (long)Math.Round(money.Amount * MinorUnitDivisor, 0, MidpointRounding.AwayFromZero);
        }
    }
}