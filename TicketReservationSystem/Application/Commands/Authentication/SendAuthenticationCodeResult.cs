using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Commands.Authentication
{
    public class SendAuthenticationCodeResult : Result<SendAuthenticationCodeResponse>
    {
        private SendAuthenticationCodeResult(SendAuthenticationCodeResponse value) : base(value) { }
        public SendAuthenticationCodeResult(Error error) : base(error) { }

        public new static SendAuthenticationCodeResult Success()
            => new(new SendAuthenticationCodeResponse());
    }
}
