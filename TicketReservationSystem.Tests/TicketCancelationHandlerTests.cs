using Moq;
using TicketReservationSystem.Application.Commands.Tickets;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Domain.ValueObjects;

namespace TicketReservationSystem.Tests;

public class TicketCancelationHandlerTests
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
    public async Task TicketCancelation_ForValidCommand_ReturnsSuccess()
    {
        // Arrange
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(eventId, ticketId);
        ticket.Reserve(userId);
        ticket.SocialEvent.ReserveTicket();

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new TicketCancelationHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new TicketCancelationCommand(ticketId, userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ticketId, result.Value.Id);
        Assert.Equal(TicketStatus.Available, result.Value.Status);
        Assert.Equal(TicketStatus.Available, ticket.Status);
        Assert.Null(ticket.UserId);
        Assert.Equal(100, ticket.SocialEvent.AvailableTickets);
        Assert.Equal(0, ticket.SocialEvent.ReservedTickets);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TicketCancelation_ForValidCommand_ReturnsCorrectResultData()
    {
        // Arrange
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(eventId, ticketId);
        ticket.Reserve(userId);
        ticket.SocialEvent.ReserveTicket();

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new TicketCancelationHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new TicketCancelationCommand(ticketId, userId), CancellationToken.None);

        // Assert
        Assert.Equal(ticketId, result.Value.Id);
        Assert.Equal(TicketStatus.Available, result.Value.Status);
    }

    [Fact]
    public async Task TicketCancelation_ForValidCommand_ReleasesTicket()
    {
        // Arrange
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(eventId, ticketId);
        ticket.Reserve(userId);
        ticket.SocialEvent.ReserveTicket();

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new TicketCancelationHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new TicketCancelationCommand(ticketId, userId), CancellationToken.None);

        // Assert
        Assert.Equal(TicketStatus.Available, ticket.Status);
        Assert.Null(ticket.UserId);
    }

    [Fact]
    public async Task TicketCancelation_ForValidCommand_UpdatesSocialEventAvailableTickets()
    {
        // Arrange
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(eventId, ticketId);
        ticket.Reserve(userId);
        ticket.SocialEvent.ReserveTicket();

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new TicketCancelationHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new TicketCancelationCommand(ticketId, userId), CancellationToken.None);

        // Assert
        Assert.Equal(100, ticket.SocialEvent.AvailableTickets);
        Assert.Equal(0, ticket.SocialEvent.ReservedTickets);
    }

    [Fact]
    public async Task TicketCancelation_ForValidCommand_InvokesSaveChanges()
    {
        // Arrange
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(eventId, ticketId);
        ticket.Reserve(userId);
        ticket.SocialEvent.ReserveTicket();

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new TicketCancelationHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new TicketCancelationCommand(ticketId, userId), CancellationToken.None);

        // Assert
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TicketCancelation_ForMissingTicket_ReturnsNotFoundErrorResult()
    {
        // Arrange
        var ticketId = TicketId.CreateUnique();
        Ticket? ticket = null;

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);

        var handler = new TicketCancelationHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new TicketCancelationCommand(ticketId, UserId.CreateUnique()), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<NotFoundError>(result.Error);
    }

    [Fact]
    public async Task TicketCancelation_ForMissingTicket_DoesNotInvokeSaveChanges()
    {
        // Arrange
        var ticketId = TicketId.CreateUnique();
        Ticket? ticket = null;

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);

        var handler = new TicketCancelationHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new TicketCancelationCommand(ticketId, UserId.CreateUnique()), CancellationToken.None);

        // Asser
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TicketCancelation_ForAnotherUsersTicket_ReturnsUnauthorizedUserErrorResult()
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

        var handler = new TicketCancelationHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new TicketCancelationCommand(ticketId, UserId.CreateUnique()), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<UnauthorizedUserError>(result.Error);
    }

    [Fact]
    public async Task TicketCancelation_ForAnotherUsersTicket_DoesNotInvokeSaveChanges()
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

        var handler = new TicketCancelationHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new TicketCancelationCommand(ticketId, UserId.CreateUnique()), CancellationToken.None);

        // Assert
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}