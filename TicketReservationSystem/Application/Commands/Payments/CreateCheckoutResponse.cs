using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.Commands.Payments
{
    public record CreateCheckoutResponse(string CheckoutUrl, string SessionId, PaymentId PaymentId);
}