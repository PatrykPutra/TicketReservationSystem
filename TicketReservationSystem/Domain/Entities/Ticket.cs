
namespace TicketReservationSystem.Domain.Entities
{
    using TicketReservationSystem.Domain.Events;
    using TicketReservationSystem.Domain.Exceptions;
    using TicketReservationSystem.Domain.Ids;
    using TicketReservationSystem.Domain.Primitives;
    using TicketReservationSystem.Domain.ValueObjects;

    public class Ticket : AggregateRoot<TicketId>
    {
        public SocialEventId EventId { get; private set; }
        public SocialEvent SocialEvent { get; private set; }

        public string SeatNumber { get; private set; }

        public TicketStatus Status { get; private set; }
        public UserId? UserId { get; private set; }

        public Money Price { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime? ModifiedAt { get; private set; }
        public DateTime? ConfirmedAt { get; private set; }
        public DateTime? ReservedAt { get; private set; }

        public Ticket(TicketId id, SocialEventId eventId, SocialEvent socialEvent, string seatNumber, Money price) : base(id)
        {
            EventId = eventId;
            SocialEvent = socialEvent;
            SeatNumber = seatNumber;
            Status = TicketStatus.Available;
            UserId = null;
            Price = price;
            CreatedAt = DateTime.UtcNow;
        }

        private Ticket()
        {
            
        }

        public bool IsAvailable()
        {
            return Status == TicketStatus.Available;
        }

        public bool IsReserved()
        {
            return Status == TicketStatus.Reserved;
        }

        public bool IsConfirmed()
        {
            return Status == TicketStatus.Confirmed;
        }

        public bool IsExpired()
        {
            return DateTime.UtcNow >= SocialEvent.TimeRange.EndTime;
        }

        public void Reserve(UserId userId)
        {
            if (Status != TicketStatus.Available)
                throw new TicketStatusException($"Ticket {SeatNumber} is not available");

            Status = TicketStatus.Reserved;
            UserId = userId;
            ReservedAt = DateTime.UtcNow;

            AddDomainEvent(new TicketReservedEvent(Id, userId, EventId));
        }

        public void Confirm(UserId userId, DateTime? confirmedAt = null)
        {
            if (Status != TicketStatus.Reserved)
                throw new TicketStatusException("Only reserved ticket can be confirmed");

            if (UserId != userId)
                throw new UnauthorizedUserException();

            Status = TicketStatus.Confirmed;
            ConfirmedAt = confirmedAt ?? DateTime.UtcNow;

            AddDomainEvent(new TicketConfirmedEvent(Id, userId, EventId));
        }

        public void ReleaseReservation()
        {
            if (Status != TicketStatus.Reserved)
                throw new TicketStatusException($"Ticket {SeatNumber} is not reserved");

            Status = TicketStatus.Available;
            UserId = null;
            ConfirmedAt = null;
            ReservedAt = null;

            AddDomainEvent(new TicketReleasedEvent(Id, EventId));
        }

        public void Cancel(UserId userId)
        {
            if (UserId != userId)
                throw new UnauthorizedUserException();

            Status = TicketStatus.Available;
            UserId = null;
            ConfirmedAt = null;
            ReservedAt = null;

            AddDomainEvent(new TicketCanceledEvent(Id, userId, EventId));
        }
    }
}
