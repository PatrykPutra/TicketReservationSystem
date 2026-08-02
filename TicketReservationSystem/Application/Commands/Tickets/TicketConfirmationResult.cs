using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.Commands.Tickets
{
    public class TicketConfirmationResult : Result<TicketConfirmationResponse>
    {
        private TicketConfirmationResult(TicketConfirmationResponse value) : base(value) { }
        public TicketConfirmationResult(Error error) : base(error) { }

        public static TicketConfirmationResult Success(TicketId id, TicketStatus status, DateTime confirmedAt)
            => new(new TicketConfirmationResponse(id, status, confirmedAt));
    }
}
