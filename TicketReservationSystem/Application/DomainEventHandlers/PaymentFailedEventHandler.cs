using Microsoft.Extensions.Logging;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Domain.Events;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Application.DomainEventHandlers
{
    public class PaymentFailedEventHandler : IDomainEventHandler<PaymentFailedEvent>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<PaymentFailedEventHandler> _logger;

        public PaymentFailedEventHandler(IUnitOfWork unitOfWork, IEmailSender emailSender, ILogger<PaymentFailedEventHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task Handle(PaymentFailedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(domainEvent.UserId, cancellationToken);
            var payment = await _unitOfWork.Payments.GetByIdAsync(domainEvent.PaymentId, cancellationToken);
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(domainEvent.TicketId, cancellationToken);
            var socialEvent = ticket is null ? null : await _unitOfWork.Events.GetByIdAsync(ticket.EventId, cancellationToken);

            if (user is null || payment is null || ticket is null || socialEvent is null)
            {
                _logger.LogWarning("Payment failure data incomplete for payment {PaymentId}; no email sent", domainEvent.PaymentId);
                return;
            }

            var subject = "Payment failed";
            var body = $"Your payment has failed.\n\nEvent: {socialEvent.Name}\nSeat: {ticket.SeatNumber}\nAmount: {payment.Amount.Amount} {payment.Amount.Currency}\nFailed at: {domainEvent.FailedAt:O}\n\nPlease try again or contact support.\n\nBest regards,\nTicketReservationSystem Team";

            try
            {
                await _emailSender.SendAsync(user.Email, subject, body, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payment-failure email to {Email} for payment {PaymentId}", user.Email, domainEvent.PaymentId);
            }
        }
    }
}
