using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.DTOs
{
    public record EventDto(
        SocialEventId Id,
        string Name,
        string Description,
        DateTime StartTime,
        DateTime EndTime,
        int TotalTickets,
        int AvailableTickets,
        int ReservedTickets,
        EventStatus Status,
        decimal PriceAmount,
        string PriceCurrency);
}
