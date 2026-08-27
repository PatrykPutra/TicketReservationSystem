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

public class TicketConfirmedEventHandlerTests
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

    private static TicketConfirmedEventHandler CreateHandler(User? user, Ticket? ticket, SocialEvent? socialEvent, IEmailSender emailSender)
    {
        var usersRepositoryMock = new Mock<IUserRepository>();
        usersRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var eventsRepositoryMock = new Mock<IEventRepository>();
        eventsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<SocialEventId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(socialEvent);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(usersRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Events).Returns(eventsRepositoryMock.Object);

        var handler = new TicketConfirmedEventHandler(
            unitOfWorkMock.Object,
            emailSender,
            NullLogger<TicketConfirmedEventHandler>.Instance);

        return handler;
    }

    [Fact]
    public async Task TicketConfirmed_ForResolvedData_SendsEmail()
    {
        // Arrange
        var eventId = SocialEventId.CreateUnique();
        var user = User.Register("user@test.com", "Test", "User", "123456789");
        var ticket = CreateTicket(eventId, TicketId.CreateUnique());
        ticket.Reserve(user.Id);
        ticket.Cancel(user.Id);

        var emailSenderMock = new Mock<IEmailSender>();
        emailSenderMock
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(user, ticket, ticket.SocialEvent, emailSenderMock.Object);

        var domainEvent = new TicketConfirmedEvent(ticket.Id, user.Id, eventId);

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        emailSenderMock.Verify(
            s => s.SendAsync(
                user.Email,
                "Ticket confirmed",
                It.Is<string>(b => b.Contains(ticket.SocialEvent.Name) && b.Contains(ticket.SeatNumber)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TicketConfirmed_WhenTicketMissing_DoesNotSendEmail()
    {
        // Arrange
        var eventId = SocialEventId.CreateUnique();
        var user = User.Register("user@test.com", "Test", "User", "123456789");
        var ticket = CreateTicket(eventId, TicketId.CreateUnique());
        ticket.Reserve(user.Id);
        ticket.Cancel(user.Id);

        var emailSenderMock = new Mock<IEmailSender>();
        emailSenderMock
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(user, ticket : null, ticket.SocialEvent, emailSenderMock.Object);

        var domainEvent = new TicketConfirmedEvent(ticket.Id, user.Id, eventId);

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        emailSenderMock.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TicketConfirmed_WhenSenderThrows_HandlesException()
    {
        // Arrange
        var eventId = SocialEventId.CreateUnique();
        var user = User.Register("user@test.com", "Test", "User", "123456789");
        var ticket = CreateTicket(eventId, TicketId.CreateUnique());
        ticket.Reserve(user.Id);
        ticket.Cancel(user.Id);

        var emailSenderMock = new Mock<IEmailSender>();
        emailSenderMock
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP down"));

        var handler = CreateHandler(user, ticket, ticket.SocialEvent, emailSenderMock.Object);

        var domainEvent = new TicketConfirmedEvent(ticket.Id, user.Id, eventId);

        // Act
        var exception = await Record.ExceptionAsync(() => handler.Handle(domainEvent, CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }
}