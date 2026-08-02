namespace TicketReservationSystem.Application.Commands.Authentication
{
    public class AuthenticationCommand : ICommand<AuthenticationResult>
    {
        public string Email { get; }

        public AuthenticationCommand(string email)
        {
            Email = email;
        }
    }
}
