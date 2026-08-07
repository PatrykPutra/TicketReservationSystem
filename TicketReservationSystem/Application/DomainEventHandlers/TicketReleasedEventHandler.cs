using Microsoft.Extensions.Logging;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Domain.Events;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Application.DomainEventHandlers
{
    public class TicketReleasedEventHandler : IDomainEventHandler<TicketReleasedEvent>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<TicketReleasedEventHandler> _logger;

        public TicketReleasedEventHandler(IUnitOfWork unitOfWork, IEmailSender emailSender, ILogger<TicketReleasedEventHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task Handle(TicketReleasedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(domainEvent.UserId, cancellationToken);
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(domainEvent.TicketId, cancellationToken);
            var socialEvent = await _unitOfWork.Events.GetByIdAsync(domainEvent.EventId, cancellationToken);

            if (user is null || ticket is null || socialEvent is null)
            {
                _logger.LogWarning("Release data incomplete for ticket {TicketId}; no email sent", domainEvent.TicketId);
                return;
            }

            var subject = "Reservation released";
            var body = $"Your reservation has been released.\n\nEvent: {socialEvent.Name}\nSeat: {ticket.SeatNumber}\nReleased at: {domainEvent.ReleasedAt:O}\n\nIf this was unexpected, please rebook or contact support.\n\nBest regards,\nTicketReservationSystem Team";

            try
            {
                await _emailSender.SendAsync(user.Email, subject, body, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send release email to {Email} for ticket {TicketId}", user.Email, domainEvent.TicketId);
            }
        }
    }
}
