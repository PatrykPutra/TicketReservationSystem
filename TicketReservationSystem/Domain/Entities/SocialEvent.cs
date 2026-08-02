namespace TicketReservationSystem.Domain.Entities
{
    using TicketReservationSystem.Domain.Events;
    using TicketReservationSystem.Domain.Exceptions;
    using TicketReservationSystem.Domain.Ids;
    using TicketReservationSystem.Domain.Primitives;
    using TicketReservationSystem.Domain.ValueObjects;

        public class SocialEvent : AggregateRoot<SocialEventId>
        {
            public string Name { get; private set; }
            public string Description { get; private set; }
            public DateTimeRange TimeRange { get; private set; }
            public int TotalTickets { get; private set; }
            public int AvailableTickets { get; private set; }
            public int ReservedTickets { get; private set; }

            public EventStatus Status { get; private set; }

            public Money TicketPrice { get; private set; }

            public DateTime CreatedAt { get; private set; }
            public DateTime? ModifiedAt { get; private set; }

            public SocialEvent(SocialEventId id, string name, string description, DateTimeRange timeRange, int totalTickets, EventStatus status,
                              Money ticketPrice) : base(id)
            {
                Name = name;
                Description = description;
                TimeRange = timeRange;
                TotalTickets = totalTickets;
                ReservedTickets = 0;
                AvailableTickets = totalTickets;
                Status = status;
                TicketPrice = ticketPrice;
            }

            private SocialEvent()
            {
            
            }

            public bool IsOngoing()
            {
                return Status == EventStatus.Ongoing;
            }

            public bool IsEnded()
            {
                return Status == EventStatus.Ended;
            }

            public void ReserveTicket()
            {
                AvailableTickets--;
                ReservedTickets ++;
            }

            public void CancelReservation()
            {
                AvailableTickets ++;
                ReservedTickets --;
            }

            public void UpdateStatus(EventStatus newStatus)
            {
                Status = newStatus;
                ModifiedAt = DateTime.UtcNow;
            }

            public void UpdateTicketPrice(Money newPrice)
            {
                TicketPrice = newPrice;
                ModifiedAt = DateTime.UtcNow;
            // here should be an event raised to change price of all not reserved tickets
            }

            public void IncreaseTotalTickets(int quantityToAdd)
            {
                if (quantityToAdd <= 0) return; // change into domain exception
                TotalTickets += quantityToAdd;
                AvailableTickets += quantityToAdd;
            }
        }
}
