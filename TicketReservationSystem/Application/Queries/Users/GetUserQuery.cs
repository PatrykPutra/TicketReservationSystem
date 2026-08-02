using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.Queries.Users
{
    public class GetUserQuery : IQuery<GetUserResult>
    {
        public UserId? UserId { get; }
        public string? Email { get; }

        public GetUserQuery(UserId? userId = null, string? email = null)
        {
            UserId = userId;
            Email = email;
        }
    }
}
