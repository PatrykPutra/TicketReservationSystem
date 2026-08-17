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

    [Fact]
    public async Task TicketReservation_ForAvailableTicket_ReservesAndReturnsSuccess()
    {
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(eventId, ticketId);

        var ticketsRepo = new Mock<ITicketRepository>();
        ticketsRepo.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Tickets).Returns(ticketsRepo.Object);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new TicketReservationHandler(uow.Object);

        var result = await handler.Handle(new TicketReservationCommand(ticketId, userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ticketId, result.Value.Id);
        Assert.Equal(TicketStatus.Reserved, result.Value.Status);
        Assert.Equal(TicketStatus.Reserved, ticket.Status);
        Assert.Equal(userId, ticket.UserId);
        Assert.Equal(99, ticket.SocialEvent.AvailableTickets);
        Assert.Equal(1, ticket.SocialEvent.ReservedTickets);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TicketReservation_ForMissingTicket_ReturnsNotFound()
    {
        var ticketId = TicketId.CreateUnique();

        var ticketsRepo = new Mock<ITicketRepository>();
        ticketsRepo.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Tickets).Returns(ticketsRepo.Object);

        var handler = new TicketReservationHandler(uow.Object);

        var result = await handler.Handle(new TicketReservationCommand(ticketId, UserId.CreateUnique()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<NotFoundError>(result.Error);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TicketReservation_ForUnavailableTicket_ReturnsTicketNotAvailable()
    {
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var ticket = CreateTicket(eventId, ticketId);
        ticket.Reserve(UserId.CreateUnique());

        var ticketsRepo = new Mock<ITicketRepository>();
        ticketsRepo.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Tickets).Returns(ticketsRepo.Object);

        var handler = new TicketReservationHandler(uow.Object);

        var result = await handler.Handle(new TicketReservationCommand(ticketId, UserId.CreateUnique()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<TicketNotAvailableError>(result.Error);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Ticket CreateTicket(SocialEventId eventId, TicketId ticketId)
    {
        var timeRange = new DateTimeRange(
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(4));
        var socialEvent = new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
        return new Ticket(ticketId, eventId, socialEvent, "A1", DefaultPrice);
    }
}