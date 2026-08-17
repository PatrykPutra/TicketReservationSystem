using Moq;
using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Application.Queries.Events;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Domain.ValueObjects;

namespace TicketReservationSystem.Tests;

public class GetEventsHandlerTests
{
    private static readonly Money DefaultPrice = new(150, "PLN");

    [Fact]
    public async Task GetEvents_ForExistingEvents_ReturnsMappedDtos()
    {
        var eventId = SocialEventId.CreateUnique();
        var socialEvent = CreateEvent(eventId);
        socialEvent.ReserveTicket();

        var eventsRepo = new Mock<IEventRepository>();
        eventsRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SocialEvent> { socialEvent });

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Events).Returns(eventsRepo.Object);

        var handler = new GetEventsHandler(uow.Object);

        var result = await handler.Handle(new GetEventsQuery(), CancellationToken.None);

        var expected = new EventDto(
            eventId,
            "Test Event",
            "Description",
            socialEvent.TimeRange.StartTime,
            socialEvent.TimeRange.EndTime,
            100,
            99,
            1,
            EventStatus.Scheduled,
            150m,
            "PLN");
        Assert.Equal(new List<EventDto> { expected }, result.Events);
    }

    [Fact]
    public async Task GetEvents_ForEmptyStore_ReturnsEmptyList()
    {
        var eventsRepo = new Mock<IEventRepository>();
        eventsRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SocialEvent>());

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Events).Returns(eventsRepo.Object);

        var handler = new GetEventsHandler(uow.Object);

        var result = await handler.Handle(new GetEventsQuery(), CancellationToken.None);

        Assert.Empty(result.Events);
    }

    private static SocialEvent CreateEvent(SocialEventId id)
    {
        var timeRange = new DateTimeRange(
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(4));
        return new SocialEvent(id, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
    }
}
