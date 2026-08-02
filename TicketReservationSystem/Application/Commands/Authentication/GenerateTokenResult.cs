using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Commands.Authentication
{
    public class GenerateTokenResult : Result<TokenResponse>
    {
        private GenerateTokenResult(TokenResponse value) : base(value) { }
        public GenerateTokenResult(Error error) : base(error) { }

        public static GenerateTokenResult Success(string token, DateTime expiresAt)
            => new(new TokenResponse(token, expiresAt));
    }
}
