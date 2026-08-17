using Microsoft.Extensions.Logging;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands.Authentication;
using TicketReservationSystem.Domain.Events;

namespace TicketReservationSystem.Application.DomainEventHandlers
{
    public class SendAuthenticationCodeEmailHandler : IDomainEventHandler<AuthenticationCodeGeneratedEvent>
    {
        private readonly IEmailSender _emailSender;
        private readonly ILogger<SendAuthenticationCodeEmailHandler> _logger;

        public SendAuthenticationCodeEmailHandler(IEmailSender emailSender, ILogger<SendAuthenticationCodeEmailHandler> logger)
        {
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task Handle(AuthenticationCodeGeneratedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var subject = "Your authentication code";
            var body = $"Your authentication code is: {domainEvent.Code}\n\nThis code expires in {SendAuthenticationCodeHandler.CodeLifetimeMinutes} minutes.\n\nIf you did not request this code, please ignore this email.";

            try
            {
                await _emailSender.SendAsync(domainEvent.Email, subject, body, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send authentication-code email to {Email}", domainEvent.Email);
            }
        }
    }
}
