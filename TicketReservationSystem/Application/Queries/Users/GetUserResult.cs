using TicketReservationSystem.Application.DTOs;

namespace TicketReservationSystem.Application.Queries.Users
{
    public class GetUserResult
    {
        public UserDto? User { get; }

        public GetUserResult(UserDto? user)
        {
            User = user;
        }
    }
}