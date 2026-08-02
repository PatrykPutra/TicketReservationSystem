using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.Commands.Tickets
{
    public record TicketReservationResponse(TicketId Id, TicketStatus Status, DateTime ReservedAt);
}