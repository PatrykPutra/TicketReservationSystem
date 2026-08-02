namespace TicketReservationSystem.Domain.Entities
{
    using TicketReservationSystem.Domain.Events;
    using TicketReservationSystem.Domain.Exceptions;
    using TicketReservationSystem.Domain.Ids;
    using TicketReservationSystem.Domain.Primitives;
    using TicketReservationSystem.Domain.ValueObjects;

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

            public User(UserId id) : base(id)
            {
                CreatedAt = DateTime.UtcNow;
            }

            private User()
            {
            
            }

            public bool CanPurchaseTickets()
            {
                return IsActive && IsVerified;
            }

            public void Register(string email, string firstName, string lastName, string phoneNumber)
            {
                Email = email;
                FirstName = firstName;
                LastName = lastName;
                PhoneNumber = phoneNumber;
                IsActive = true;

                AddDomainEvent(new UserRegisteredEvent(Id, email));
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
