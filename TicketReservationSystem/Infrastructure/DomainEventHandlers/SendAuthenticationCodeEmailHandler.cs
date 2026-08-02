using TicketReservationSystem.Domain.Events;
using TicketReservationSystem.Infrastructure.Email;

namespace TicketReservationSystem.Infrastructure.DomainEventHandlers
{
    public class SendAuthenticationCodeEmailHandler : IDomainEventHandler<AuthenticationCodeGeneratedEvent>
    {
        private readonly IEmailSender _emailSender;

        public SendAuthenticationCodeEmailHandler(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task Handle(AuthenticationCodeGeneratedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var subject = "Your authentication code";
            var body = $"Your authentication code is: {domainEvent.Code}\n\nThis code expires in 5 minutes.\n\nIf you did not request this code, please ignore this email.";

            await _emailSender.SendAsync(domainEvent.Email, subject, body, cancellationToken);
        }
    }
}
