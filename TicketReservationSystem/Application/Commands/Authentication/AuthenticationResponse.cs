using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.Commands.Authentication
{
    public record AuthenticationResponse(UserId? UserId);
}