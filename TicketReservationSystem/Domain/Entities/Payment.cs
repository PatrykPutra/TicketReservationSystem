namespace TicketReservationSystem.Domain.Entities
{
    using TicketReservationSystem.Domain.Events;
    using TicketReservationSystem.Domain.Exceptions;
    using TicketReservationSystem.Domain.Ids;
    using TicketReservationSystem.Domain.Primitives;
    using TicketReservationSystem.Domain.ValueObjects;

    public class Payment : AggregateRoot<PaymentId>
    {
        public TicketId TicketId { get; private set; }
        public UserId UserId { get; private set; }
        public Money Amount { get; private set; }
        public string? StripeSessionId { get; private set; }
        public PaymentStatus Status { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime? ModifiedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }

        public Payment(PaymentId id, TicketId ticketId, UserId userId, Money amount) : base(id)
        {
            TicketId = ticketId;
            UserId = userId;
            Amount = amount;
            Status = PaymentStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }

        private Payment()
        {
        }

        public bool IsPending()
        {
            return Status == PaymentStatus.Pending;
        }

        public void SetStripeSessionId(string sessionId)
        {
            StripeSessionId = sessionId;
            ModifiedAt = DateTime.UtcNow;
        }

        public void MarkCompleted(DateTime? completedAt = null)
        {
            if (Status != PaymentStatus.Pending)
                throw new PaymentStatusException($"Payment {Id} is not pending and cannot be completed");

            Status = PaymentStatus.Completed;
            CompletedAt = completedAt ?? DateTime.UtcNow;
            ModifiedAt = CompletedAt;

            AddDomainEvent(new PaymentCompletedEvent(Id, TicketId, UserId, CompletedAt));
        }

        public void MarkFailed(DateTime? failedAt = null)
        {
            if (Status != PaymentStatus.Pending)
                throw new PaymentStatusException($"Payment {Id} is not pending and cannot be marked failed");

            Status = PaymentStatus.Failed;
            ModifiedAt = failedAt ?? DateTime.UtcNow;

            AddDomainEvent(new PaymentFailedEvent(Id, TicketId, UserId, ModifiedAt));
        }

        public void MarkExpired(DateTime? expiredAt = null)
        {
            if (Status != PaymentStatus.Pending)
                throw new PaymentStatusException($"Payment {Id} is not pending and cannot be marked expired");

            Status = PaymentStatus.Expired;
            ModifiedAt = expiredAt ?? DateTime.UtcNow;

            AddDomainEvent(new PaymentExpiredEvent(Id, TicketId, UserId, ModifiedAt));
        }
    }
}