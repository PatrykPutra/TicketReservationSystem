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
        // Arrange
        var timeRange = new DateTimeRange(
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(4));
        var socialEvent = new SocialEvent(SocialEventId.CreateUnique(), "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
        var expected = new EventDto(socialEvent.Id, socialEvent.Name, socialEvent.Description, socialEvent.TimeRange.StartTime, socialEvent.TimeRange.EndTime, socialEvent.TotalTickets, socialEvent.AvailableTickets, socialEvent.ReservedTickets, EventStatus.Scheduled, socialEvent.TicketPrice.Amount, socialEvent.TicketPrice.Currency);

        var eventsRepositoryMock = new Mock<IEventRepository>();
        eventsRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SocialEvent> { socialEvent });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Events).Returns(eventsRepositoryMock.Object);

        var handler = new GetEventsHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new GetEventsQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(new List<EventDto> { expected }, result.Events);
    }

    [Fact]
    public async Task GetEvents_ForEmptyStore_ReturnsEmptyList()
    {
        // Arrange
        var eventsRepositoryMock = new Mock<IEventRepository>();
        eventsRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SocialEvent>());

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Events).Returns(eventsRepositoryMock.Object);

        var handler = new GetEventsHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new GetEventsQuery(), CancellationToken.None);

        // Assert
        Assert.Empty(result.Events);
    }
}
