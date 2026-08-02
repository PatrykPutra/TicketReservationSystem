using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.Commands.Tickets
{
    public record TicketConfirmationResponse(TicketId Id, TicketStatus Status, DateTime ConfirmedAt);
}