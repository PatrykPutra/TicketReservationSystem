using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Commands.Payments
{
    public class CreateCheckoutResult : Result<CreateCheckoutResponse>
    {
        private CreateCheckoutResult(CreateCheckoutResponse value) : base(value) { }
        public CreateCheckoutResult(Error error) : base(error) { }

        public static CreateCheckoutResult Success(string checkoutUrl, string sessionId, Domain.Ids.PaymentId paymentId)
            => new(new CreateCheckoutResponse(checkoutUrl, sessionId, paymentId));
    }
}