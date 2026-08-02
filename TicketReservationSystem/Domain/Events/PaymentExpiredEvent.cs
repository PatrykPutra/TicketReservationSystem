namespace TicketReservationSystem.Domain.Events
{
    using TicketReservationSystem.Domain.Ids;

    public class PaymentExpiredEvent : DomainEvent
    {
        public PaymentId PaymentId { get; }
        public TicketId TicketId { get; }
        public UserId UserId { get; }
        public DateTime ExpiredAt { get; }

        public PaymentExpiredEvent(PaymentId paymentId, TicketId ticketId, UserId userId, DateTime? expiredAt = null)
        {
            PaymentId = paymentId;
            TicketId = ticketId;
            UserId = userId;
            ExpiredAt = expiredAt ?? DateTime.UtcNow;
        }
    }
}