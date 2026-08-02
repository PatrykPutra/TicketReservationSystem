namespace TicketReservationSystem.Domain.Events
{
    using TicketReservationSystem.Domain.Ids;

    public class PaymentFailedEvent : DomainEvent
    {
        public PaymentId PaymentId { get; }
        public TicketId TicketId { get; }
        public UserId UserId { get; }
        public DateTime FailedAt { get; }

        public PaymentFailedEvent(PaymentId paymentId, TicketId ticketId, UserId userId, DateTime? failedAt = null)
        {
            PaymentId = paymentId;
            TicketId = ticketId;
            UserId = userId;
            FailedAt = failedAt ?? DateTime.UtcNow;
        }
    }
}