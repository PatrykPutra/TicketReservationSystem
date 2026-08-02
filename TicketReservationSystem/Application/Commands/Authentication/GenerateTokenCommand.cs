namespace TicketReservationSystem.Application.Commands.Authentication
{
    public class GenerateTokenCommand : ICommand<GenerateTokenResult>
    {
        public string Email { get; }
        public string Code { get; }

        public GenerateTokenCommand(string email, string code)
        {
            Email = email;
            Code = code;
        }
    }
}
