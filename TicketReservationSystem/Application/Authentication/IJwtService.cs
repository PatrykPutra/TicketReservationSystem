using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.Authentication
{
    public interface IJwtService
    {
        string GenerateToken(UserId userId, string email);
    }
}
