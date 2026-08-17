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

        var dto = Assert.IsType<TicketDto>(result.Ticket);
        Assert.Equal(ticketId, dto.Id);
        Assert.Equal(eventId, dto.EventId);
        Assert.Equal("A1", dto.SeatNumber);
        Assert.Equal(TicketStatus.Reserved, dto.Status);
        Assert.Equal(userId, dto.UserId);
        Assert.Equal(150m, dto.PriceAmount);
        Assert.Equal("PLN", dto.PriceCurrency);
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