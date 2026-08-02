using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.DTOs
{
    public record TicketDto(
        TicketId Id,
        SocialEventId EventId,
        string SeatNumber,
        TicketStatus Status,
        UserId? UserId,
        decimal PriceAmount,
        string PriceCurrency);
}
