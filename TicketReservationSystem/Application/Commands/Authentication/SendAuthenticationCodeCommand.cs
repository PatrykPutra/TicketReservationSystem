namespace TicketReservationSystem.Application.Commands.Authentication
{
    public class SendAuthenticationCodeCommand : ICommand<SendAuthenticationCodeResult>
    {
        public string Email { get; }

        public SendAuthenticationCodeCommand(string email)
        {
            Email = email;
        }
    }
}
