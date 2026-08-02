using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.Commands.Tickets
{
    public class TicketCancelationResult : Result<TicketCancelationResponse>
    {
        private TicketCancelationResult(TicketCancelationResponse value) : base(value) { }
        public TicketCancelationResult(Error error) : base(error) { }

        public static TicketCancelationResult Success(TicketId id, TicketStatus status)
            => new(new TicketCancelationResponse(id, status));
    }
}
