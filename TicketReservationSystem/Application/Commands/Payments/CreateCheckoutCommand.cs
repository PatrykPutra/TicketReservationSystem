using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.Commands.Payments
{
    public class CreateCheckoutCommand : ICommand<CreateCheckoutResult>
    {
        public TicketId TicketId { get; }
        public UserId UserId { get; }

        public CreateCheckoutCommand(TicketId ticketId, UserId userId)
        {
            TicketId = ticketId;
            UserId = userId;
        }
    }
}