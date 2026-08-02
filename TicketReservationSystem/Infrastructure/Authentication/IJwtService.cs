using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Infrastructure.Authentication
{
    public interface IJwtService
    {
        string GenerateToken(UserId userId, string email);
    }
}
