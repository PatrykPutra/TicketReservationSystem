using Microsoft.Extensions.Logging;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Domain.Events;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Application.DomainEventHandlers
{
    public class PaymentCompletedEventHandler : IDomainEventHandler<PaymentCompletedEvent>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<PaymentCompletedEventHandler> _logger;

        public PaymentCompletedEventHandler(IUnitOfWork unitOfWork, IEmailSender emailSender, ILogger<PaymentCompletedEventHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task Handle(PaymentCompletedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(domainEvent.UserId, cancellationToken);
            var payment = await _unitOfWork.Payments.GetByIdAsync(domainEvent.PaymentId, cancellationToken);
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(domainEvent.TicketId, cancellationToken);
            var socialEvent = ticket is null ? null : await _unitOfWork.Events.GetByIdAsync(ticket.EventId, cancellationToken);

            if (user is null || payment is null || ticket is null || socialEvent is null)
            {
                _logger.LogWarning("Payment completion data incomplete for payment {PaymentId}; no email sent", domainEvent.PaymentId);
                return;
            }

            var subject = "Payment completed";
            var body = $"Your payment has been completed.\n\nEvent: {socialEvent.Name}\nSeat: {ticket.SeatNumber}\nAmount: {payment.Amount.Amount} {payment.Amount.Currency}\nCompleted at: {domainEvent.CompletedAt:O}\n\nThank you for your purchase!\n\nBest regards,\nTicketReservationSystem Team";

            try
            {
                await _emailSender.SendAsync(user.Email, subject, body, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payment-completion email to {Email} for payment {PaymentId}", user.Email, domainEvent.PaymentId);
            }
        }
    }
}
