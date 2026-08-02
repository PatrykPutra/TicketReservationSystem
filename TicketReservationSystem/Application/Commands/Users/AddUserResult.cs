using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.Commands.Users
{
    public class AddUserResult : Result<AddUserResponse>
    {
        private AddUserResult(AddUserResponse value) : base(value) { }
        public AddUserResult(Error error) : base(error) { }

        public static AddUserResult Success(UserId id)
            => new(new AddUserResponse(id));
    }
}
