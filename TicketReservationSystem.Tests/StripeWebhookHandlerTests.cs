using System.Linq.Expressions;
using Moq;
using Stripe;
using Stripe.Checkout;
using TicketReservationSystem.Application.Abstractions;
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

    private static (TicketId TicketId, UserId UserId, PaymentId PaymentId) CreateReservedWithPendingPayment(
        out Payment payment,
        out Ticket ticket)
    {
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();
        var paymentId = PaymentId.CreateUnique();

        var timeRange = new DateTimeRange(DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4));
        var socialEvent = new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
        ticket = new Ticket(ticketId, eventId, socialEvent, "A1", DefaultPrice);
        ticket.Reserve(userId);

        payment = new Payment(paymentId, ticketId, userId, DefaultPrice, PaymentProvider.Stripe);
        payment.SetExternalId("cs_test_123");

        return (ticketId, userId, paymentId);
    }

    private static (Mock<IUnitOfWork> Uow, Mock<IPaymentRepository> Payments, Mock<ITicketRepository> Tickets) CreateUnitOfWork(
        Payment? payment,
        Ticket? ticket)
    {
        var paymentsRepo = new Mock<IPaymentRepository>();
        paymentsRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment is null ? new List<Payment>() : new List<Payment> { payment });

        var ticketsRepo = new Mock<ITicketRepository>();
        ticketsRepo.Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Payments).Returns(paymentsRepo.Object);
        uow.SetupGet(u => u.Tickets).Returns(ticketsRepo.Object);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return (uow, paymentsRepo, ticketsRepo);
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
        var (_, _, paymentId) = CreateReservedWithPendingPayment(out var payment, out var ticket);
        var mocks = CreateUnitOfWork(payment, ticket);
        var handler = new StripeWebhookHandler(mocks.Uow.Object);

        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("checkout.session.completed", paymentId.Value.ToString())),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
    }

    [Fact]
    public async Task StripeWebhook_OnCompletedEvent_ConfirmsTicket()
    {
        var (_, userId, paymentId) = CreateReservedWithPendingPayment(out var payment, out var ticket);
        var mocks = CreateUnitOfWork(payment, ticket);
        var handler = new StripeWebhookHandler(mocks.Uow.Object);

        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("checkout.session.completed", paymentId.Value.ToString())),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketStatus.Confirmed, ticket.Status);
        Assert.Equal(userId, ticket.UserId);
    }

    [Fact]
    public async Task StripeWebhook_OnCompletedEventRedelivery_IsIdempotentNoop()
    {
        var (_, _, paymentId) = CreateReservedWithPendingPayment(out var payment, out var ticket);
        var mocks = CreateUnitOfWork(payment, ticket);
        var handler = new StripeWebhookHandler(mocks.Uow.Object);
        var command = new StripeWebhookCommand(CreateStripeEvent("checkout.session.completed", paymentId.Value.ToString()));

        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(PaymentStatus.Completed, payment.Status);
    }

    [Fact]
    public async Task StripeWebhook_OnExpiredEvent_MarksPaymentExpired()
    {
        var (_, _, paymentId) = CreateReservedWithPendingPayment(out var payment, out var ticket);
        var mocks = CreateUnitOfWork(payment, ticket);
        var handler = new StripeWebhookHandler(mocks.Uow.Object);

        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("checkout.session.expired", paymentId.Value.ToString())),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Expired, payment.Status);
    }

    [Fact]
    public async Task StripeWebhook_OnExpiredEvent_ReleasesTicket()
    {
        var (_, _, paymentId) = CreateReservedWithPendingPayment(out var payment, out var ticket);
        var mocks = CreateUnitOfWork(payment, ticket);
        var handler = new StripeWebhookHandler(mocks.Uow.Object);

        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("checkout.session.expired", paymentId.Value.ToString())),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketStatus.Available, ticket.Status);
        Assert.Null(ticket.UserId);
    }

    [Fact]
    public async Task StripeWebhook_OnPaymentFailedEvent_MarksPaymentFailed()
    {
        var (_, _, paymentId) = CreateReservedWithPendingPayment(out var payment, out var ticket);
        var mocks = CreateUnitOfWork(payment, ticket);
        var handler = new StripeWebhookHandler(mocks.Uow.Object);

        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("payment_intent.payment_failed", paymentId.Value.ToString())),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
    }

    [Fact]
    public async Task StripeWebhook_OnPaymentFailedEvent_ReleasesTicket()
    {
        var (_, _, paymentId) = CreateReservedWithPendingPayment(out var payment, out var ticket);
        var mocks = CreateUnitOfWork(payment, ticket);
        var handler = new StripeWebhookHandler(mocks.Uow.Object);

        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("payment_intent.payment_failed", paymentId.Value.ToString())),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketStatus.Available, ticket.Status);
    }

    [Fact]
    public async Task StripeWebhook_OnUnknownEventType_IsNoop()
    {
        var (_, _, paymentId) = CreateReservedWithPendingPayment(out var payment, out var ticket);
        var mocks = CreateUnitOfWork(payment, ticket);
        var handler = new StripeWebhookHandler(mocks.Uow.Object);

        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("some.other.event", paymentId.Value.ToString())),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(TicketStatus.Reserved, ticket.Status);
    }

    [Fact]
    public async Task StripeWebhook_OnNonSessionEventObject_ReturnsPaymentProcessingError()
    {
        var (_, _, paymentId) = CreateReservedWithPendingPayment(out var payment, out var ticket);
        var mocks = CreateUnitOfWork(payment, ticket);
        var handler = new StripeWebhookHandler(mocks.Uow.Object);

        var nonSessionEvent = new Event
        {
            Type = "checkout.session.completed",
            Data = new EventData { Object = new Charge() },
        };

        var result = await handler.Handle(new StripeWebhookCommand(nonSessionEvent), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<PaymentProcessingError>(result.Error);
    }

    [Fact]
    public async Task StripeWebhook_OnInvalidClientReferenceId_ReturnsPaymentProcessingError()
    {
        var (_, _, paymentId) = CreateReservedWithPendingPayment(out var payment, out var ticket);
        var mocks = CreateUnitOfWork(payment, ticket);
        var handler = new StripeWebhookHandler(mocks.Uow.Object);

        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("checkout.session.completed", "not-a-guid")),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<PaymentProcessingError>(result.Error);
    }

    [Fact]
    public async Task StripeWebhook_WhenPaymentNotFound_ReturnsPaymentProcessingError()
    {
        var (_, _, paymentId) = CreateReservedWithPendingPayment(out var payment, out var ticket);
        var mocks = CreateUnitOfWork(payment: null, ticket);
        var handler = new StripeWebhookHandler(mocks.Uow.Object);

        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("checkout.session.completed", paymentId.Value.ToString())),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<PaymentProcessingError>(result.Error);
    }
}