using Microsoft.Extensions.Logging;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Domain.Events;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Application.DomainEventHandlers
{
    public class TicketConfirmedEventHandler : IDomainEventHandler<TicketConfirmedEvent>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<TicketConfirmedEventHandler> _logger;

        public TicketConfirmedEventHandler(IUnitOfWork unitOfWork, IEmailSender emailSender, ILogger<TicketConfirmedEventHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task Handle(TicketConfirmedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(domainEvent.UserId, cancellationToken);
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(domainEvent.TicketId, cancellationToken);
            var socialEvent = await _unitOfWork.Events.GetByIdAsync(domainEvent.EventId, cancellationToken);

            if (user is null || ticket is null || socialEvent is null)
            {
                _logger.LogWarning("Confirmation data incomplete for ticket {TicketId}; no email sent", domainEvent.TicketId);
                return;
            }

            var subject = "Ticket confirmed";
            var body = $"Your ticket has been confirmed.\n\nEvent: {socialEvent.Name}\nSeat: {ticket.SeatNumber}\nConfirmed at: {domainEvent.ConfirmedAt:O}\n\nBest regards,\nTicketReservationSystem Team";

            try
            {
                await _emailSender.SendAsync(user.Email, subject, body, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send confirmation email to {Email} for ticket {TicketId}", user.Email, domainEvent.TicketId);
            }
        }
    }
}
