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

    [Fact]
    public async Task GetTicketById_ForExistingTicket_ReturnsMappedDto()
    {
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(eventId, ticketId);
        ticket.Reserve(userId);

        var ticketsRepo = new Mock<ITicketRepository>();
        ticketsRepo.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Tickets).Returns(ticketsRepo.Object);

        var handler = new GetTicketByIdHandler(uow.Object);

        var result = await handler.Handle(new GetTicketByIdQuery(ticketId), CancellationToken.None);

        var expected = new TicketDto(ticketId, eventId, "A1", TicketStatus.Reserved, userId, 150m, "PLN");
        Assert.Equal(expected, result.Ticket);
    }

    [Fact]
    public async Task GetTicketById_ForMissingTicket_ReturnsNullDto()
    {
        var ticketId = TicketId.CreateUnique();

        var ticketsRepo = new Mock<ITicketRepository>();
        ticketsRepo.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Tickets).Returns(ticketsRepo.Object);

        var handler = new GetTicketByIdHandler(uow.Object);

        var result = await handler.Handle(new GetTicketByIdQuery(ticketId), CancellationToken.None);

        Assert.Null(result.Ticket);
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