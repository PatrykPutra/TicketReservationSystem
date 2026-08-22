namespace TicketReservationSystem.Domain.Entities
{
    using TicketReservationSystem.Domain.Events;
    using TicketReservationSystem.Domain.Ids;
    using TicketReservationSystem.Domain.Primitives;

        public class User : AggregateRoot<UserId>
        {
            public string Email { get; private set; }
            public string FirstName { get; private set; }
            public string LastName { get; private set; }
            public string PhoneNumber { get; private set; }

            public bool IsVerified { get; private set; }
            public bool IsActive { get; private set; }
            public DateTime? EmailVerifiedAt { get; private set; }

            public DateTime CreatedAt { get; private set; }
            public DateTime LastLoginAt { get; private set; }

            private User(UserId id, string email, string firstName, string lastName, string phoneNumber) : base(id)
            {
                Email = email;
                FirstName = firstName;
                LastName = lastName;
                PhoneNumber = phoneNumber;
                IsActive = true;
                CreatedAt = DateTime.UtcNow;
            }

            private User()
            {
            
            }

            public bool CanPurchaseTickets()
            {
                return IsActive && IsVerified;
            }

            public static User Register(string email, string firstName, string lastName, string phoneNumber)
            {
                UserId id = UserId.CreateUnique();
                User user = new User(id, email, firstName, lastName, phoneNumber);
                user.AddDomainEvent(new UserRegisteredEvent(id, email));
                return user;
            }

            public void VerifyEmail()
            {
                IsVerified = true;
                EmailVerifiedAt = DateTime.UtcNow;
                AddDomainEvent(new EmailVerifiedEvent(Id));
            }

            public void Login()
            {
                LastLoginAt = DateTime.UtcNow;
            }

        }
}
