using Moq;
using TicketReservationSystem.Application.Queries.Tickets;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Domain.ValueObjects;

namespace TicketReservationSystem.Tests;

public class GetTicketsByEventHandlerTests
{
    private static readonly Money DefaultPrice = new(150, "PLN");

    [Fact]
    public async Task GetTicketsByEvent_ForExistingTickets_ReturnsMappedDtos()
    {
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var ticket = CreateTicket(eventId, ticketId);

        var ticketsRepo = new Mock<ITicketRepository>();
        ticketsRepo.Setup(r => r.GetByEventIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ticket> { ticket });

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Tickets).Returns(ticketsRepo.Object);

        var handler = new GetTicketsByEventHandler(uow.Object);

        var result = await handler.Handle(new GetTicketsByEventQuery(eventId), CancellationToken.None);

        var dto = Assert.Single(result.Tickets);
        Assert.Equal(ticketId, dto.Id);
        Assert.Equal(eventId, dto.EventId);
        Assert.Equal("A1", dto.SeatNumber);
        Assert.Equal(TicketStatus.Available, dto.Status);
        Assert.Null(dto.UserId);
        Assert.Equal(150m, dto.PriceAmount);
        Assert.Equal("PLN", dto.PriceCurrency);
    }

    [Fact]
    public async Task GetTicketsByEvent_ForEventWithoutTickets_ReturnsEmptyList()
    {
        var eventId = SocialEventId.CreateUnique();

        var ticketsRepo = new Mock<ITicketRepository>();
        ticketsRepo.Setup(r => r.GetByEventIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ticket>());

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Tickets).Returns(ticketsRepo.Object);

        var handler = new GetTicketsByEventHandler(uow.Object);

        var result = await handler.Handle(new GetTicketsByEventQuery(eventId), CancellationToken.None);

        Assert.Empty(result.Tickets);
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