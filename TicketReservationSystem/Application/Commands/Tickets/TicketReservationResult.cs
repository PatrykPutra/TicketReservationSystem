using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.Commands.Tickets
{
    public class TicketReservationResult : Result<TicketReservationResponse>
    {
        private TicketReservationResult(TicketReservationResponse value) : base(value) { }
        public TicketReservationResult(Error error) : base(error) { }

        public static TicketReservationResult Success(TicketId id, TicketStatus status, DateTime reservedAt)
            => new(new TicketReservationResponse(id, status, reservedAt));
    }
}
