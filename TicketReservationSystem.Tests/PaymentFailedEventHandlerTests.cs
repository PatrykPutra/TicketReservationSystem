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

public class PaymentFailedEventHandlerTests
{
    private static readonly Money DefaultPrice = new(150, "PLN");

    private static (PaymentId PaymentId, TicketId TicketId, UserId UserId) CreateSeededData(
        out User user,
        out Payment payment,
        out Ticket ticket)
    {
        var paymentId = PaymentId.CreateUnique();
        var userId = UserId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var eventId = SocialEventId.CreateUnique();

        var timeRange = new DateTimeRange(
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(4));
        var socialEvent = new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
        ticket = new Ticket(ticketId, eventId, socialEvent, "A1", DefaultPrice);
        payment = new Payment(paymentId, ticketId, userId, DefaultPrice, PaymentProvider.Stripe, DateTime.UtcNow);
        payment.MarkFailed();
        user = User.Register("user@test.com", "Test", "User", "123456789");

        return (paymentId, ticketId, userId);
    }

    private static (PaymentFailedEventHandler Handler, Mock<IEmailSender> EmailSender) CreateHandler(
        User? user,
        Payment? payment,
        Ticket? ticket,
        SocialEvent? socialEvent)
    {
        var usersRepo = new Mock<IUserRepository>();
        usersRepo.Setup(r => r.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var paymentsRepo = new Mock<IPaymentRepository>();
        paymentsRepo.Setup(r => r.GetByIdAsync(It.IsAny<PaymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var ticketsRepo = new Mock<ITicketRepository>();
        ticketsRepo.Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var eventsRepo = new Mock<IEventRepository>();
        eventsRepo.Setup(r => r.GetByIdAsync(It.IsAny<SocialEventId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(socialEvent);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Users).Returns(usersRepo.Object);
        uow.SetupGet(u => u.Payments).Returns(paymentsRepo.Object);
        uow.SetupGet(u => u.Tickets).Returns(ticketsRepo.Object);
        uow.SetupGet(u => u.Events).Returns(eventsRepo.Object);

        var emailSender = new Mock<IEmailSender>();
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new PaymentFailedEventHandler(
            uow.Object,
            emailSender.Object,
            NullLogger<PaymentFailedEventHandler>.Instance);

        return (handler, emailSender);
    }

    [Fact]
    public async Task PaymentFailed_ForResolvedData_SendsEmail()
    {
        var (paymentId, ticketId, userId) = CreateSeededData(out var user, out var payment, out var ticket);
        var (handler, emailSender) = CreateHandler(user, payment, ticket, ticket.SocialEvent);

        var domainEvent = new PaymentFailedEvent(paymentId, ticketId, userId, DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        emailSender.Verify(
            s => s.SendAsync(
                "user@test.com",
                "Payment failed",
                It.Is<string>(b => b.Contains("Test Event") && b.Contains("A1") && b.Contains("150") && b.Contains("PLN")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PaymentFailed_WhenUserMissing_DoesNothing()
    {
        var (paymentId, ticketId, userId) = CreateSeededData(out _, out var payment, out var ticket);
        var (handler, emailSender) = CreateHandler(user: null, payment, ticket, ticket.SocialEvent);

        var domainEvent = new PaymentFailedEvent(paymentId, ticketId, userId, DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        emailSender.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PaymentFailed_WhenSenderThrows_SwallowsException()
    {
        var (paymentId, ticketId, userId) = CreateSeededData(out var user, out var payment, out var ticket);
        var (handler, emailSender) = CreateHandler(user, payment, ticket, ticket.SocialEvent);
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP down"));

        var domainEvent = new PaymentFailedEvent(paymentId, ticketId, userId, DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);
    }
}