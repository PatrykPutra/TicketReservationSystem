using Microsoft.Extensions.Logging;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Domain.Events;

namespace TicketReservationSystem.Application.DomainEventHandlers
{
    public class UserRegistrationEventHandler : IDomainEventHandler<UserRegisteredEvent>
    {
        private readonly IEmailSender _emailSender;
        private readonly ILogger<UserRegistrationEventHandler> _logger;

        public UserRegistrationEventHandler(IEmailSender emailSender, ILogger<UserRegistrationEventHandler> logger)
        {
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task Handle(UserRegisteredEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var subject = "Welcome to TicketReservationSystem";
            var body = $"Hi,\n\nThank you for registering. Your account is now active.\n\nYou can now browse events and reserve tickets.\n\nBest regards,\nTicketReservationSystem Team";

            try
            {
                await _emailSender.SendAsync(domainEvent.Email, subject, body, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}", domainEvent.Email);
            }
        }
    }
}
