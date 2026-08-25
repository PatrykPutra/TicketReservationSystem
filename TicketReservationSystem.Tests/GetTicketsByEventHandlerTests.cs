using Moq;
using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Application.Queries.Tickets;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Domain.ValueObjects;

namespace TicketReservationSystem.Tests;

public class GetTicketsByEventHandlerTests
{
    private static readonly Money DefaultPrice = new(150, "PLN");

    private static Ticket CreateTicket()
    {
        var timeRange = new DateTimeRange(
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(4));
        var socialEvent = new SocialEvent(SocialEventId.CreateUnique(), "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
        return new Ticket(TicketId.CreateUnique(), socialEvent.Id, socialEvent, "A1", DefaultPrice);
    }

    [Fact]
    public async Task GetTicketsByEvent_ForExistingTickets_ReturnsMappedDtos()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket();
        ticket.Reserve(userId);
        var expected = new TicketDto(ticket.Id, ticket.EventId, ticket.SeatNumber, ticket.Status, userId, ticket.Price.Amount, ticket.Price.Currency);

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByEventIdAsync(ticket.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ticket> { ticket });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);

        var handler = new GetTicketsByEventHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new GetTicketsByEventQuery(ticket.EventId), CancellationToken.None);

        // Assert
        Assert.Equal(new List<TicketDto> { expected }, result.Tickets);
    }

    [Fact]
    public async Task GetTicketsByEvent_ForEventWithoutTickets_ReturnsEmptyList()
    {
        // Arrange
        var eventId = SocialEventId.CreateUnique();

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByEventIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ticket>());

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);

        var handler = new GetTicketsByEventHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new GetTicketsByEventQuery(eventId), CancellationToken.None);

        // Assert
        Assert.Empty(result.Tickets);
    }
}