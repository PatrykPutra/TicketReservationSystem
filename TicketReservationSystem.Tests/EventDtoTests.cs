using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Tests;

public class EventDtoTests
{
    [Fact]
    public void EventDto_ForValidInputs_ExposesAllFields()
    {
        var id = SocialEventId.CreateUnique();
        var start = DateTime.UtcNow.AddDays(30);
        var end = start.AddHours(4);

        var dto = new EventDto(id, "Test Event", "Description", start, end, 100, 90, 10, EventStatus.Scheduled, 150m, "PLN");

        Assert.Equal(id, dto.Id);
        Assert.Equal("Test Event", dto.Name);
        Assert.Equal("Description", dto.Description);
        Assert.Equal(start, dto.StartTime);
        Assert.Equal(end, dto.EndTime);
        Assert.Equal(100, dto.TotalTickets);
        Assert.Equal(90, dto.AvailableTickets);
        Assert.Equal(10, dto.ReservedTickets);
        Assert.Equal(EventStatus.Scheduled, dto.Status);
        Assert.Equal(150m, dto.PriceAmount);
        Assert.Equal("PLN", dto.PriceCurrency);
    }
}