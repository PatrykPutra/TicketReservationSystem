namespace TicketReservationSystem.Application.Commands.Users
{
    public class AddUserCommand : ICommand<AddUserResult>
    {
        public string Email { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string PhoneNumber { get; }

        public AddUserCommand(string email, string firstName, string lastName, string phoneNumber)
        {
            Email = email;
            FirstName = firstName;
            LastName = lastName;
            PhoneNumber = phoneNumber;
        }
    }
}
