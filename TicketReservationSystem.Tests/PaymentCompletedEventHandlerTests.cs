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

public class PaymentCompletedEventHandlerTests
{
    private static readonly Money DefaultPrice = new(150, "PLN");

    private static SocialEvent CreateSocialEvent()
    {
        var eventId = SocialEventId.CreateUnique();
        var timeRange = new DateTimeRange(
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(4));
        return new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
    }
    
    [Fact]
    public async Task PaymentCompleted_ForResolvedData_SendsEmail()
    {
        // Arrange
        User user = User.Register("user@test.com", "Test", "User", "123456789");
        SocialEvent socialEvent = CreateSocialEvent();
        Ticket ticket = new Ticket(TicketId.CreateUnique(), socialEvent.Id, socialEvent, "A1", DefaultPrice);
        Payment payment = new Payment(PaymentId.CreateUnique(), ticket.Id, user.Id, DefaultPrice, PaymentProvider.Stripe, DateTime.UtcNow);

        var emailSender = new Mock<IEmailSender>();
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var usersRepositoryMock = new Mock<IUserRepository>();
        usersRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<PaymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var eventsRepositoryMock = new Mock<IEventRepository>();
        eventsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<SocialEventId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(socialEvent);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(usersRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Events).Returns(eventsRepositoryMock.Object);

        var handler = new PaymentCompletedEventHandler(
            unitOfWorkMock.Object,
            emailSender.Object,
            NullLogger<PaymentCompletedEventHandler>.Instance);


        var domainEvent = new PaymentCompletedEvent(payment.Id, ticket.Id, user.Id, DateTime.UtcNow);

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        emailSender.Verify(
            s => s.SendAsync(
                "user@test.com",
                "Payment completed",
                It.Is<string>(b => b.Contains("Test Event") && b.Contains("A1") && b.Contains("150") && b.Contains("PLN")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PaymentCompleted_WhenUserMissing_DoesNotSendEmail()
    {
        // Arrange
        UserId userId = UserId.CreateUnique();
        User? user = null;
        SocialEvent socialEvent = CreateSocialEvent();
        Ticket ticket = new Ticket(TicketId.CreateUnique(), socialEvent.Id, socialEvent, "A1", DefaultPrice);
        Payment payment = new Payment(PaymentId.CreateUnique(), ticket.Id, userId, DefaultPrice, PaymentProvider.Stripe, DateTime.UtcNow);

        var emailSender = new Mock<IEmailSender>();
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var usersRepositoryMock = new Mock<IUserRepository>();
        usersRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<PaymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var eventsRepositoryMock = new Mock<IEventRepository>();
        eventsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<SocialEventId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(socialEvent);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(usersRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Events).Returns(eventsRepositoryMock.Object);

        var handler = new PaymentCompletedEventHandler(
            unitOfWorkMock.Object,
            emailSender.Object,
            NullLogger<PaymentCompletedEventHandler>.Instance);


        var domainEvent = new PaymentCompletedEvent(payment.Id, ticket.Id, userId, DateTime.UtcNow);

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        emailSender.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PaymentCompleted_WhenSenderThrows_HandlesException()
    {
        // Arrange
        User user = User.Register("user@test.com", "Test", "User", "123456789");
        SocialEvent socialEvent = CreateSocialEvent();
        Ticket ticket = new Ticket(TicketId.CreateUnique(), socialEvent.Id, socialEvent, "A1", DefaultPrice);
        Payment payment = new Payment(PaymentId.CreateUnique(), ticket.Id, user.Id, DefaultPrice, PaymentProvider.Stripe, DateTime.UtcNow);

        var emailSender = new Mock<IEmailSender>();
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP down"));

        var usersRepositoryMock = new Mock<IUserRepository>();
        usersRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<PaymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var eventsRepositoryMock = new Mock<IEventRepository>();
        eventsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<SocialEventId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(socialEvent);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(usersRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Events).Returns(eventsRepositoryMock.Object);

        var handler = new PaymentCompletedEventHandler(
            unitOfWorkMock.Object,
            emailSender.Object,
            NullLogger<PaymentCompletedEventHandler>.Instance);


        var domainEvent = new PaymentCompletedEvent(payment.Id, ticket.Id, user.Id, DateTime.UtcNow);

        // Act
        var exception = await Record.ExceptionAsync(() => handler.Handle(domainEvent, CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }
}