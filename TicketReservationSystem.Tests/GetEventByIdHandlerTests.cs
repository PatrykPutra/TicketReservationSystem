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

        var expected = new EventDto(
            eventId,
            "Test Event",
            "Description",
            socialEvent.TimeRange.StartTime,
            socialEvent.TimeRange.EndTime,
            100,
            100,
            0,
            EventStatus.Scheduled,
            150m,
            "PLN");
        Assert.Equal(expected, result.Event);
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
