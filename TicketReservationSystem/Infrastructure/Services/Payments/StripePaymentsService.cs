using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.ValueObjects;

namespace TicketReservationSystem.Infrastructure.Services.Payments
{
    public class StripePaymentsService : IPaymentsService
    {
        private static readonly Dictionary<string, int> MinorUnitDivisors = CreateMinorUnitDivisors();

        private readonly StripeSettings _settings;
        private readonly SessionService _sessionService;

        public StripePaymentsService(IOptions<StripeSettings> settings, SessionService? sessionService = null)
        {
            _settings = settings.Value;
            _sessionService = sessionService ?? new SessionService();
        }

        public async Task<Result<CreateCheckoutSessionResult>> CreateCheckoutSessionAsync(
            Money amount,
            PaymentId paymentId,
            CancellationToken cancellationToken = default)
        {
            if (amount.Currency != _settings.Currency)
                return Result<CreateCheckoutSessionResult>.Failure(new CurrencyMismatchError(
                    $"Amount currency {amount.Currency} does not match configured currency {_settings.Currency}"));

            if (!MinorUnitDivisors.TryGetValue(_settings.Currency, out var minorUnitDivisor))
                return Result<CreateCheckoutSessionResult>.Failure(new UnsupportedCurrencyError(
                    $"Currency {_settings.Currency} is not supported by the payment provider"));

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
                            UnitAmount = ToMinorUnits(amount, minorUnitDivisor),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Ticket purchase",
                            },
                        },
                    },
                ],
            };

            var session = await _sessionService.CreateAsync(options, cancellationToken: cancellationToken);

            return Result<CreateCheckoutSessionResult>.Success(new CreateCheckoutSessionResult(session.Url, session.Id));
        }

        private static long ToMinorUnits(Money money, int minorUnitDivisor)
        {
            return (long)Math.Round(money.Amount * minorUnitDivisor, 0, MidpointRounding.AwayFromZero);
        }

        private static Dictionary<string, int> CreateMinorUnitDivisors()
        {
            var zeroDecimalCurrencies = new[]
            {
                "BIF", "CLP", "DJF", "GNF", "ISK", "JPY", "KMF", "KRW", "MGA",
                "PYG", "RWF", "UGX", "UYI", "VND", "VUV", "XAF", "XOF", "XPF",
            };

            var supportedCurrencies = new[]
            {
                "AED", "AFN", "ALL", "AMD", "ANG", "AOA", "ARS", "AUD", "AWG", "AZN",
                "BAM", "BBD", "BDT", "BGN", "BMD", "BND", "BOB", "BRL", "BSD", "BWP",
                "BYN", "BZD", "CAD", "CDF", "CHF", "CNY", "COP", "CRC", "CUP", "CVE",
                "CZK", "DKK", "DOP", "DZD", "EGP", "ERN", "ETB", "EUR", "FJD", "FKP",
                "GBP", "GEL", "GHS", "GIP", "GMD", "GTQ", "GYD", "HKD", "HNL", "HRK",
                "HTG", "HUF", "IDR", "ILS", "INR", "IQD", "JEP", "JMD", "KES", "KGS", "KHR",
                "KYD", "KZT", "LAK", "LBP", "LKR", "LRD", "LSL", "LYD", "MAD", "MDL",
                "MKD", "MMK", "MNT", "MOP", "MRU", "MUR", "MVR", "MWK", "MXN",
                "MYR", "MZN", "NAD", "NGN", "NIO", "NOK", "NPR", "NZD", "PAB", "PEN",
                "PGK", "PHP", "PKR", "PLN", "QAR", "RON", "RSD", "RUB", "SAR", "SBD", "SCR",
                "SDG", "SEK", "SGD", "SHP", "SLE", "SLL", "SOS", "SRD", "SSP", "STN",
                "SVC", "SYP", "SZL", "THB", "TJS", "TMT", "TOP", "TRY", "TTD", "TWD",
                "TZS", "UAH", "USD", "UYU", "UYW", "UZS", "VES", "WST", "XCD", "YER",
                "ZAR", "ZMW",
            };

            var divisors = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var code in supportedCurrencies)
                divisors[code] = 100;
            foreach (var code in zeroDecimalCurrencies)
                divisors[code] = 1;

            return divisors;
        }
    }
}
