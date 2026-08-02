namespace TicketReservationSystem.Domain.Events
{
    using TicketReservationSystem.Domain.Ids;

    public class PaymentCompletedEvent : DomainEvent
    {
        public PaymentId PaymentId { get; }
        public TicketId TicketId { get; }
        public UserId UserId { get; }
        public DateTime CompletedAt { get; }

        public PaymentCompletedEvent(PaymentId paymentId, TicketId ticketId, UserId userId, DateTime? completedAt = null)
        {
            PaymentId = paymentId;
            TicketId = ticketId;
            UserId = userId;
            CompletedAt = completedAt ?? DateTime.UtcNow;
        }
    }
}