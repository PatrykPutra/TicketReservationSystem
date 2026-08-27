using Moq;
using Stripe;
using Stripe.Checkout;
using System.Linq.Expressions;
using TicketReservationSystem.Application.Commands.Payments;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Domain.ValueObjects;

namespace TicketReservationSystem.Tests;

public class StripeWebhookHandlerTests
{
    private static readonly Money DefaultPrice = new(100, "PLN");

    private static SocialEvent CreateSocialEvent()
    {
        var eventId = SocialEventId.CreateUnique();
        var timeRange = new DateTimeRange(
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(4));
        return new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
    }

    private static Ticket CreateTicket(SocialEvent socialEvent)
    {
        return new Ticket(TicketId.CreateUnique(), socialEvent.Id, socialEvent, "A1", DefaultPrice);
    }

    private static Payment CreatePayment(TicketId ticketId, UserId userId, PaymentProvider paymentProvider)
    {
        return new Payment(PaymentId.CreateUnique(), ticketId, userId, DefaultPrice, paymentProvider, DateTime.UtcNow);
    }

    private static Event CreateStripeEvent(string type, string clientReferenceId)
    {
        return new Event
        {
            Type = type,
            Data = new EventData
            {
                Object = new Session { ClientReferenceId = clientReferenceId },
            },
        };
    }

    [Fact]
    public async Task StripeWebhook_OnCompletedEvent_MarksPaymentCompleted()
    {
        // Arrange
        var ticket = CreateTicket(CreateSocialEvent());
        var payment = CreatePayment(ticket.Id, UserId.CreateUnique(), PaymentProvider.Stripe);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { payment });

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new StripeWebhookHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("checkout.session.completed", payment.Id.Value.ToString())),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
    }

    [Fact]
    public async Task StripeWebhook_ForPaymentCompletedEvent_ConfirmsTicket()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(CreateSocialEvent());
        ticket.Reserve(userId);
        var payment = CreatePayment(ticket.Id, userId, PaymentProvider.Stripe);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { payment });

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new StripeWebhookHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("checkout.session.completed", payment.Id.Value.ToString())),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TicketStatus.Confirmed, ticket.Status);
    }

    [Fact]
    public async Task StripeWebhook_OnCompletedEventRedelivery_IsIdempotentNoop()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(CreateSocialEvent());
        var payment = CreatePayment(ticket.Id, userId, PaymentProvider.Stripe);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { payment });

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new StripeWebhookHandler(unitOfWorkMock.Object);
        var command = new StripeWebhookCommand(CreateStripeEvent("checkout.session.completed", payment.Id.Value.ToString()));

        // Act
        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(PaymentStatus.Completed, payment.Status);
    }

    [Fact]
    public async Task StripeWebhook_ForSessionExpiredEvent_MarksPaymentExpired()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(CreateSocialEvent());
        var payment = CreatePayment(ticket.Id, userId, PaymentProvider.Stripe);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { payment });

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new StripeWebhookHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("checkout.session.expired", payment.Id.Value.ToString())),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Expired, payment.Status);
    }

    [Fact]
    public async Task StripeWebhook_ForSessionExpiredEvent_ReleasesTicket()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(CreateSocialEvent());
        var payment = CreatePayment(ticket.Id, userId, PaymentProvider.Stripe);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { payment });

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new StripeWebhookHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("checkout.session.expired", payment.Id.Value.ToString())),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TicketStatus.Available, ticket.Status);
        Assert.Null(ticket.UserId);
    }

    [Fact]
    public async Task StripeWebhook_OnPaymentFailedEvent_MarksPaymentFailed()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(CreateSocialEvent());
        var payment = CreatePayment(ticket.Id, userId, PaymentProvider.Stripe);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { payment });

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new StripeWebhookHandler(unitOfWorkMock.Object);
        
        // Act
        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("payment_intent.payment_failed", payment.Id.Value.ToString())),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
    }

    [Fact]
    public async Task StripeWebhook_ForPaymentFailedEvent_ReleasesTicket()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(CreateSocialEvent());
        var payment = CreatePayment(ticket.Id, userId, PaymentProvider.Stripe);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { payment });

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new StripeWebhookHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("payment_intent.payment_failed", payment.Id.Value.ToString())),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TicketStatus.Available, ticket.Status);
    }

    [Fact]
    public async Task StripeWebhook_ForUnknownEventType_IsNoop()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(CreateSocialEvent());
        ticket.Reserve(userId);
        var payment = CreatePayment(ticket.Id, userId, PaymentProvider.Stripe);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { payment });

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new StripeWebhookHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("some.other.event", payment.Id.Value.ToString())),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(TicketStatus.Reserved, ticket.Status);
    }

    [Fact]
    public async Task StripeWebhook_ForNonSessionEventObject_ReturnsPaymentProcessingError()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(CreateSocialEvent());
        var payment = CreatePayment(ticket.Id, userId, PaymentProvider.Stripe);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { payment });

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new StripeWebhookHandler(unitOfWorkMock.Object);

        var nonSessionEvent = new Event
        {
            Type = "checkout.session.completed",
            Data = new EventData { Object = new Charge() },
        };

        // Act
        var result = await handler.Handle(new StripeWebhookCommand(nonSessionEvent), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<PaymentProcessingError>(result.Error);
    }

    [Fact]
    public async Task StripeWebhook_ForInvalidClientReferenceId_ReturnsPaymentProcessingError()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(CreateSocialEvent());
        var payment = CreatePayment(ticket.Id, userId, PaymentProvider.Stripe);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { payment });

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new StripeWebhookHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("checkout.session.completed", "not-a-guid")),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<PaymentProcessingError>(result.Error);
    }

    [Fact]
    public async Task StripeWebhook_WhenPaymentNotFound_ReturnsPaymentProcessingError()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket(CreateSocialEvent());
        var paymentId = PaymentId.CreateUnique();

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>());

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new StripeWebhookHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("checkout.session.completed", paymentId.Value.ToString())),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<PaymentProcessingError>(result.Error);
    }
}