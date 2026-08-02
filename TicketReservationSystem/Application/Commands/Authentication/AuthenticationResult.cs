using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.Commands.Authentication
{
    public class AuthenticationResult : Result<AuthenticationResponse>
    {
        private AuthenticationResult(AuthenticationResponse value) : base(value) { }
        public AuthenticationResult(Error error) : base(error) { }

        public static AuthenticationResult Success(UserId? userId)
            => new(new AuthenticationResponse(userId));
    }
}