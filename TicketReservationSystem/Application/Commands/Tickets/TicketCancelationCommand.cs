using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.Commands.Tickets
{
    public class TicketCancelationCommand : ICommand<TicketCancelationResult>
    {
        public TicketId TicketId { get; }
        public UserId UserId { get; }

        public TicketCancelationCommand(TicketId ticketId, UserId userId)
        {
            TicketId = ticketId;
            UserId = userId;
        }
    }
}
