using Moq;
using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Application.Queries.Events;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Domain.ValueObjects;

namespace TicketReservationSystem.Tests;

public class GetEventByIdHandlerTests
{
    private static readonly Money DefaultPrice = new(150, "PLN");

    [Fact]
    public async Task GetEventById_ForExistingEvent_ReturnsMappedDto()
    {
        var eventId = SocialEventId.CreateUnique();
        var socialEvent = CreateEvent(eventId);

        var eventsRepo = new Mock<IEventRepository>();
        eventsRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(socialEvent);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Events).Returns(eventsRepo.Object);

        var handler = new GetEventByIdHandler(uow.Object);

        var result = await handler.Handle(new GetEventByIdQuery(eventId), CancellationToken.None);

        var dto = Assert.IsType<EventDto>(result.Event);
        Assert.Equal(eventId, dto.Id);
        Assert.Equal("Test Event", dto.Name);
        Assert.Equal("Description", dto.Description);
        Assert.Equal(socialEvent.TimeRange.StartTime, dto.StartTime);
        Assert.Equal(socialEvent.TimeRange.EndTime, dto.EndTime);
        Assert.Equal(100, dto.TotalTickets);
        Assert.Equal(100, dto.AvailableTickets);
        Assert.Equal(0, dto.ReservedTickets);
        Assert.Equal(EventStatus.Scheduled, dto.Status);
        Assert.Equal(150m, dto.PriceAmount);
        Assert.Equal("PLN", dto.PriceCurrency);
    }

    [Fact]
    public async Task GetEventById_ForMissingEvent_ReturnsNullDto()
    {
        var eventId = SocialEventId.CreateUnique();

        var eventsRepo = new Mock<IEventRepository>();
        eventsRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocialEvent?)null);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Events).Returns(eventsRepo.Object);

        var handler = new GetEventByIdHandler(uow.Object);

        var result = await handler.Handle(new GetEventByIdQuery(eventId), CancellationToken.None);

        Assert.Null(result.Event);
    }

    private static SocialEvent CreateEvent(SocialEventId id)
    {
        var timeRange = new DateTimeRange(
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(4));
        return new SocialEvent(id, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
    }
}
