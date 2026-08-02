namespace TicketReservationSystem.Infrastructure.Payments
{
    public class StripeSettings
    {
        public const string SectionName = "Stripe";

        public string SecretKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
        public string Currency { get; set; } = "PLN";
    }
}