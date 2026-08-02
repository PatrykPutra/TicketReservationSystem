using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.Commands.Tickets
{
    public record TicketCancelationResponse(TicketId Id, TicketStatus Status);
}