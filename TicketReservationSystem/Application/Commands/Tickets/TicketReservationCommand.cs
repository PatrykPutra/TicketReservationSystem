using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.Commands.Tickets
{
    public class TicketReservationCommand : ICommand<TicketReservationResult>
    {
        public TicketId TicketId { get; }
        public UserId UserId { get; }

        public TicketReservationCommand(TicketId ticketId, UserId userId)
        {
            TicketId = ticketId;
            UserId = userId;
        }
    }
}
