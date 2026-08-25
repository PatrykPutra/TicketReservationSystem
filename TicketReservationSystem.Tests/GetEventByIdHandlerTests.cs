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
        // Arrange
        var timeRange = new DateTimeRange(
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(4));
        var socialEvent =  new SocialEvent(SocialEventId.CreateUnique(), "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
        var expected = new EventDto ( socialEvent.Id, socialEvent.Name, socialEvent.Description, socialEvent.TimeRange.StartTime, socialEvent.TimeRange.EndTime, socialEvent.TotalTickets, socialEvent.AvailableTickets, socialEvent.ReservedTickets, EventStatus.Scheduled, socialEvent.TicketPrice.Amount, socialEvent.TicketPrice.Currency);
        
        var eventsRepositoryMock = new Mock<IEventRepository>();
        eventsRepositoryMock
            .Setup(r => r.GetByIdAsync(socialEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(socialEvent);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Events).Returns(eventsRepositoryMock.Object);

        var handler = new GetEventByIdHandler(unitOfWorkMock.Object);
        
        // Act
        var result = await handler.Handle(new GetEventByIdQuery(socialEvent.Id), CancellationToken.None);

        // Assert
        Assert.Equal(expected, result.Event);
    }

    [Fact]
    public async Task GetEventById_ForMissingEvent_ReturnsNullDto()
    {
        // Arrange
        var eventId = SocialEventId.CreateUnique();

        var eventsRepositoryMock = new Mock<IEventRepository>();
        eventsRepositoryMock.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocialEvent?)null);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Events).Returns(eventsRepositoryMock.Object);

        var handler = new GetEventByIdHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new GetEventByIdQuery(eventId), CancellationToken.None);

        // Assert
        Assert.Null(result.Event);
    }
}
