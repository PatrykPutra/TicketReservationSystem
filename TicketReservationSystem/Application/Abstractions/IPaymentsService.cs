namespace TicketReservationSystem.Application.Abstractions
{
    public record CreateCheckoutSessionResult(string CheckoutUrl, string SessionId);

    public interface IPaymentsService
    {
        Task<Result<CreateCheckoutSessionResult>> CreateCheckoutSessionAsync(
            Domain.ValueObjects.Money amount,
            Domain.Ids.PaymentId paymentId,
            CancellationToken cancellationToken = default);
    }
}