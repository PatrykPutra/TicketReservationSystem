using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.DomainEventHandlers;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Events;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Domain.ValueObjects;

namespace TicketReservationSystem.Tests;

public class TicketReservedEventHandlerTests
{
    private static readonly Money DefaultPrice = new(150, "PLN");

    private static (SocialEventId EventId, TicketId TicketId, UserId UserId) CreateSeededData(
        out User user,
        out Ticket ticket)
    {
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();

        var timeRange = new DateTimeRange(
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(4));
        var socialEvent = new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
        ticket = new Ticket(ticketId, eventId, socialEvent, "A1", DefaultPrice);
        ticket.Reserve(userId);
        user = User.Register("user@test.com", "Test", "User", "123456789");

        return (eventId, ticketId, userId);
    }

    private static (TicketReservedEventHandler Handler, Mock<IEmailSender> EmailSender) CreateHandler(
        User? user,
        Ticket? ticket,
        SocialEvent? socialEvent)
    {
        var usersRepo = new Mock<IUserRepository>();
        usersRepo.Setup(r => r.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var ticketsRepo = new Mock<ITicketRepository>();
        ticketsRepo.Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var eventsRepo = new Mock<IEventRepository>();
        eventsRepo.Setup(r => r.GetByIdAsync(It.IsAny<SocialEventId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(socialEvent);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Users).Returns(usersRepo.Object);
        uow.SetupGet(u => u.Tickets).Returns(ticketsRepo.Object);
        uow.SetupGet(u => u.Events).Returns(eventsRepo.Object);

        var emailSender = new Mock<IEmailSender>();
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new TicketReservedEventHandler(
            uow.Object,
            emailSender.Object,
            NullLogger<TicketReservedEventHandler>.Instance);

        return (handler, emailSender);
    }

    [Fact]
    public async Task TicketReserved_ForResolvedData_SendsEmail()
    {
        var (eventId, ticketId, userId) = CreateSeededData(out var user, out var ticket);
        var (handler, emailSender) = CreateHandler(user, ticket, ticket.SocialEvent);

        var domainEvent = new TicketReservedEvent(ticketId, userId, eventId);

        await handler.Handle(domainEvent, CancellationToken.None);

        emailSender.Verify(
            s => s.SendAsync(
                "user@test.com",
                "Ticket reserved",
                It.Is<string>(b => b.Contains("Test Event") && b.Contains("A1")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TicketReserved_WhenUserMissing_DoesNothing()
    {
        var (eventId, ticketId, _) = CreateSeededData(out _, out var ticket);
        var (handler, emailSender) = CreateHandler(null, ticket, ticket.SocialEvent);

        var domainEvent = new TicketReservedEvent(ticketId, null, eventId);

        await handler.Handle(domainEvent, CancellationToken.None);

        emailSender.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TicketReserved_WhenSenderThrows_SwallowsException()
    {
        var (eventId, ticketId, userId) = CreateSeededData(out var user, out var ticket);
        var (handler, emailSender) = CreateHandler(user, ticket, ticket.SocialEvent);
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP down"));

        var domainEvent = new TicketReservedEvent(ticketId, userId, eventId);

        await handler.Handle(domainEvent, CancellationToken.None);
    }
}