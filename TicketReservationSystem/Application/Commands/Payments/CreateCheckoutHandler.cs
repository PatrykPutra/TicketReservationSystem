using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Application.Commands.Payments
{
    public class CreateCheckoutHandler : ICommandHandler<CreateCheckoutCommand, CreateCheckoutResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentsService _paymentsService;
        private readonly TimeProvider _timeProvider;

        public CreateCheckoutHandler(IUnitOfWork unitOfWork, IPaymentsService paymentsService, TimeProvider timeProvider)
        {
            _unitOfWork = unitOfWork;
            _paymentsService = paymentsService;
            _timeProvider = timeProvider;
        }

        public async Task<CreateCheckoutResult> Handle(CreateCheckoutCommand request, CancellationToken cancellationToken)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(request.TicketId, cancellationToken);

            if (ticket is null)
                return new CreateCheckoutResult(new NotFoundError($"Ticket {request.TicketId} not found"));

            if (!ticket.IsReserved() || ticket.UserId != request.UserId)
                return new CreateCheckoutResult(new TicketNotReservedError("Ticket is not reserved by the calling user"));

            var existing = await _unitOfWork.Payments.FindAsync(
                p => p.TicketId == request.TicketId && p.Status == PaymentStatus.Pending,
                cancellationToken);

            if (existing.Count > 0)
                return new CreateCheckoutResult(new DuplicatePaymentError("An active payment already exists for this ticket"));

            var paymentId = PaymentId.CreateUnique();
            var payment = new Payment(paymentId, request.TicketId, request.UserId, ticket.Price, PaymentProvider.Stripe, _timeProvider.GetUtcNow().DateTime);
            _unitOfWork.Payments.Add(payment);

            var sessionResult = await _paymentsService.CreateCheckoutSessionAsync(ticket.Price, paymentId, cancellationToken);

            if (sessionResult.IsFailure)
                return new CreateCheckoutResult(sessionResult.Error);

            var session = sessionResult.Value;
            payment.SetExternalId(session.SessionId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return CreateCheckoutResult.Success(session.CheckoutUrl, session.SessionId, paymentId);
        }
    }
}
