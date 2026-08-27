using Stripe.Checkout;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Application.Commands.Payments
{
    public class StripeWebhookHandler : ICommandHandler<StripeWebhookCommand, Result>
    {
        private const string CheckoutSessionCompleted = "checkout.session.completed";
        private const string CheckoutSessionExpired = "checkout.session.expired";
        private const string CheckoutSessionAsyncPaymentFailed = "checkout.session.async_payment_failed";
        private const string PaymentIntentPaymentFailed = "payment_intent.payment_failed";

        private readonly IUnitOfWork _unitOfWork;

        public StripeWebhookHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(StripeWebhookCommand request, CancellationToken cancellationToken)
        {
            var stripeEvent = request.StripeEvent;

            switch (stripeEvent.Type)
            {
                case CheckoutSessionCompleted:
                case CheckoutSessionExpired:
                case CheckoutSessionAsyncPaymentFailed:
                case PaymentIntentPaymentFailed:
                    break;
                default:
                    return Result.Failure(new PaymentProcessingError("Not supported event type."));
            }

            if (stripeEvent.Data.Object is not Session session)
                return Result.Failure(new PaymentProcessingError("Webhook event does not contain a checkout session"));

            if (!Guid.TryParse(session.ClientReferenceId, out var paymentIdValue))
                return Result.Failure(new PaymentProcessingError("Webhook session has an invalid client reference id"));

            var paymentId = PaymentId.Create(paymentIdValue);
            var payments = await _unitOfWork.Payments.FindAsync(p => p.Id == paymentId, cancellationToken);
            var payment = payments.SingleOrDefault();

            if (payment is null)
                return Result.Failure(new PaymentProcessingError("No payment found for webhook session"));

            var ticket = await _unitOfWork.Tickets.GetByIdAsync(payment.TicketId, cancellationToken);

            switch (stripeEvent.Type)
            {
                case CheckoutSessionCompleted:
                    if (!payment.IsPending())
                        return Result.Success();

                    payment.MarkCompleted();
                    if (ticket is not null && ticket.IsReserved())
                        ticket.Confirm(payment.UserId);
                    break;

                case CheckoutSessionExpired:
                    if (!payment.IsPending())
                        return Result.Success();

                    payment.MarkExpired();
                    if (ticket is not null && ticket.IsReserved())
                        ticket.ReleaseReservation();
                    break;

                case CheckoutSessionAsyncPaymentFailed:
                case PaymentIntentPaymentFailed:
                    if (!payment.IsPending())
                        return Result.Success();

                    payment.MarkFailed();
                    if (ticket is not null && ticket.IsReserved())
                        ticket.ReleaseReservation();
                    break;

                default:
                    return Result.Failure(new PaymentProcessingError("Not supported event type."));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
