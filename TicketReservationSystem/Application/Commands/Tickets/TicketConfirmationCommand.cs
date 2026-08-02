using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.Commands.Tickets
{
    public class TicketConfirmationCommand : ICommand<TicketConfirmationResult>
    {
        public TicketId TicketId { get; }
        public UserId UserId { get; }
        public DateTime ConfirmedAt { get; }

        public TicketConfirmationCommand(TicketId ticketId, UserId userId, DateTime confirmedAt)
        {
            TicketId = ticketId;
            UserId = userId;
            ConfirmedAt = confirmedAt;
        }
    }
}
