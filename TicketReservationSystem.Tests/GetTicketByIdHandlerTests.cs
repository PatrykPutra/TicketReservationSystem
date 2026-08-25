using Moq;
using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Application.Queries.Tickets;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Domain.ValueObjects;

namespace TicketReservationSystem.Tests;

public class GetTicketByIdHandlerTests
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
    public async Task GetTicketById_ForExistingTicket_ReturnsMappedDto()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket();
        ticket.Reserve(userId);
        var expected = new TicketDto(ticket.Id, ticket.EventId, ticket.SeatNumber, ticket.Status, userId, ticket.Price.Amount, ticket.Price.Currency);

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);

        var handler = new GetTicketByIdHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new GetTicketByIdQuery(ticket.Id), CancellationToken.None);

        // Assert
        Assert.Equal(expected, result.Ticket);
    }

    [Fact]
    public async Task GetTicketById_ForMissingTicket_ReturnsNullDto()
    {
        // Arrange
        var ticketId = TicketId.CreateUnique();
        Ticket? ticket = null;

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);

        var handler = new GetTicketByIdHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new GetTicketByIdQuery(ticketId), CancellationToken.None);

        // Assert
        Assert.Null(result.Ticket);
    }
}