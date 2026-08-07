using Microsoft.Extensions.Logging;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Domain.Events;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Application.DomainEventHandlers
{
    public class TicketReservedEventHandler : IDomainEventHandler<TicketReservedEvent>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<TicketReservedEventHandler> _logger;

        public TicketReservedEventHandler(IUnitOfWork unitOfWork, IEmailSender emailSender, ILogger<TicketReservedEventHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task Handle(TicketReservedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            if (domainEvent.UserId is null)
            {
                _logger.LogWarning("Ticket {TicketId} reserved without a user; no email sent", domainEvent.TicketId);
                return;
            }

            var user = await _unitOfWork.Users.GetByIdAsync(domainEvent.UserId.Value, cancellationToken);
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(domainEvent.TicketId, cancellationToken);
            var socialEvent = await _unitOfWork.Events.GetByIdAsync(domainEvent.EventId, cancellationToken);

            if (user is null || ticket is null || socialEvent is null)
            {
                _logger.LogWarning("Reservation data incomplete for ticket {TicketId}; no email sent", domainEvent.TicketId);
                return;
            }

            var subject = "Ticket reserved";
            var body = $"Your ticket has been reserved.\n\nEvent: {socialEvent.Name}\nSeat: {ticket.SeatNumber}\nReserved at: {domainEvent.ReservedAt:O}\n\nBest regards,\nTicketReservationSystem Team";

            try
            {
                await _emailSender.SendAsync(user.Email, subject, body, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reservation email to {Email} for ticket {TicketId}", user.Email, domainEvent.TicketId);
            }
        }
    }
}
