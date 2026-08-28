using Moq;
using TicketReservationSystem.Application.Commands.Tickets;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Domain.ValueObjects;

namespace TicketReservationSystem.Tests;

public class TicketReservationHandlerTests
{
    private static readonly Money DefaultPrice = new(150, "PLN");

    private static Ticket CreateTicket(SocialEventId eventId, TicketId ticketId)
    {
        var timeRange = new DateTimeRange(
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(4));
        var socialEvent = new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
        return new Ticket(ticketId, eventId, socialEvent, "A1", DefaultPrice);
    }

    [Fact]
    public async Task TicketReservation_ForAvailableTicket_InvokesSaveChanges()
    {
        // Arrange
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(eventId, ticketId);

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new TicketReservationHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new TicketReservationCommand(ticketId, userId), CancellationToken.None);

        // Assert
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TicketReservation_ForAvailableTicket_SetsTicketProperties()
    {
        // Arrange
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(eventId, ticketId);

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new TicketReservationHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new TicketReservationCommand(ticketId, userId), CancellationToken.None);

        // Assert
        Assert.Equal(TicketStatus.Reserved, ticket.Status);
        Assert.Equal(userId, ticket.UserId);
    }

    [Fact]
    public async Task TicketReservation_ForAvailableTicket_SetsSocialEventProperties()
    {
        // Arrange
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(eventId, ticketId);

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new TicketReservationHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new TicketReservationCommand(ticketId, userId), CancellationToken.None);

        // Assert
        Assert.Equal(99, ticket.SocialEvent.AvailableTickets);
        Assert.Equal(1, ticket.SocialEvent.ReservedTickets);
    }

    [Fact]
    public async Task TicketReservation_ForAvailableTicket_ReturnsCorrectResult()
    {
        // Arrange
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(eventId, ticketId);

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new TicketReservationHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new TicketReservationCommand(ticketId, userId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ticketId, result.Value.Id);
        Assert.Equal(TicketStatus.Reserved, result.Value.Status);
    }

    [Fact]
    public async Task TicketReservation_ForMissingTicket_ReturnsNotFoundResult()
    {
        // Arrange
        var ticketId = TicketId.CreateUnique();

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);

        var handler = new TicketReservationHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new TicketReservationCommand(ticketId, UserId.CreateUnique()), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<NotFoundError>(result.Error);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TicketReservation_ForMissingTicket_DoesNotInvokeSaveChanges()
    {
        // Arrange
        var ticketId = TicketId.CreateUnique();

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);

        var handler = new TicketReservationHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new TicketReservationCommand(ticketId, UserId.CreateUnique()), CancellationToken.None);

        // Assert
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TicketReservation_ForUnavailableTicket_ReturnsTicketNotAvailableResult()
    {
        // Arrange
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var ticket = CreateTicket(eventId, ticketId);
        ticket.Reserve(UserId.CreateUnique());

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);

        var handler = new TicketReservationHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new TicketReservationCommand(ticketId, UserId.CreateUnique()), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<TicketNotAvailableError>(result.Error);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TicketReservation_ForUnavailableTicket_DoesNotInvokeSaveChanges()
    {
        // Arrange
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var ticket = CreateTicket(eventId, ticketId);
        ticket.Reserve(UserId.CreateUnique());

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);

        var handler = new TicketReservationHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new TicketReservationCommand(ticketId, UserId.CreateUnique()), CancellationToken.None);

        // Assert
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}