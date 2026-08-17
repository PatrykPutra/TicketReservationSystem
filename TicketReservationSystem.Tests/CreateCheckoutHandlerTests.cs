using Moq;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands.Payments;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Domain.ValueObjects;

namespace TicketReservationSystem.Tests;

public class CreateCheckoutHandlerTests
{
    private static readonly Money DefaultPrice = new(150, "PLN");

    private static (SocialEventId EventId, TicketId TicketId, UserId UserId) CreateReservedTicket(
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

        return (eventId, ticketId, userId);
    }

    private static Mock<IPaymentsService> CreateSuccessfulPaymentsService()
    {
        var paymentsService = new Mock<IPaymentsService>();
        paymentsService
            .Setup(s => s.CreateCheckoutSessionAsync(It.IsAny<Money>(), It.IsAny<PaymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateCheckoutSessionResult>.Success(new CreateCheckoutSessionResult("https://checkout.url", "cs_test_123")));
        return paymentsService;
    }

    private static (Mock<IUnitOfWork> Uow, Mock<ITicketRepository> Tickets, Mock<IPaymentRepository> Payments) CreateUnitOfWork(Ticket ticket, List<Payment>? existingPayments = null)
    {
        var ticketsRepo = new Mock<ITicketRepository>();
        ticketsRepo.Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var paymentsRepo = new Mock<IPaymentRepository>();
        paymentsRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPayments ?? new List<Payment>());

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Tickets).Returns(ticketsRepo.Object);
        uow.SetupGet(u => u.Payments).Returns(paymentsRepo.Object);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return (uow, ticketsRepo, paymentsRepo);
    }

    [Fact]
    public async Task CreateCheckout_ForReservedTicket_ReturnsCheckoutSessionDetails()
    {
        var (_, ticketId, userId) = CreateReservedTicket(out var ticket);
        var mocks = CreateUnitOfWork(ticket);
        var handler = new CreateCheckoutHandler(mocks.Uow.Object, CreateSuccessfulPaymentsService().Object);

        var result = await handler.Handle(new CreateCheckoutCommand(ticketId, userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("cs_test_123", result.Value.SessionId);
        Assert.Equal("https://checkout.url", result.Value.CheckoutUrl);
    }

    [Fact]
    public async Task CreateCheckout_ForReservedTicket_AddsPendingPaymentWithExternalId()
    {
        var (_, ticketId, userId) = CreateReservedTicket(out var ticket);
        var mocks = CreateUnitOfWork(ticket);
        var handler = new CreateCheckoutHandler(mocks.Uow.Object, CreateSuccessfulPaymentsService().Object);

        Payment? captured = null;
        mocks.Payments.Setup(r => r.Add(It.IsAny<Payment>()))
            .Callback<Payment>(payment => captured = payment);

        await handler.Handle(new CreateCheckoutCommand(ticketId, userId), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(PaymentStatus.Pending, captured!.Status);
        Assert.Equal("cs_test_123", captured.ExternalId);
    }

    [Fact]
    public async Task CreateCheckout_ForReservedTicket_SavesChanges()
    {
        var (_, ticketId, userId) = CreateReservedTicket(out var ticket);
        var mocks = CreateUnitOfWork(ticket);
        var handler = new CreateCheckoutHandler(mocks.Uow.Object, CreateSuccessfulPaymentsService().Object);

        await handler.Handle(new CreateCheckoutCommand(ticketId, userId), CancellationToken.None);

        mocks.Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCheckout_ForMissingTicket_ReturnsNotFound()
    {
        var mocks = CreateUnitOfWork(ticket: null!);
        var handler = new CreateCheckoutHandler(mocks.Uow.Object, Mock.Of<IPaymentsService>());

        var result = await handler.Handle(new CreateCheckoutCommand(TicketId.CreateUnique(), UserId.CreateUnique()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<NotFoundError>(result.Error);
    }

    [Fact]
    public async Task CreateCheckout_ForUnreservedTicket_ReturnsNotReservedError()
    {
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();

        var timeRange = new DateTimeRange(DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4));
        var socialEvent = new SocialEvent(SocialEventId.CreateUnique(), "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
        var ticket = new Ticket(ticketId, socialEvent.Id, socialEvent, "A1", DefaultPrice);

        var mocks = CreateUnitOfWork(ticket);
        var handler = new CreateCheckoutHandler(mocks.Uow.Object, Mock.Of<IPaymentsService>());

        var result = await handler.Handle(new CreateCheckoutCommand(ticketId, userId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<TicketNotReservedError>(result.Error);
    }

    [Fact]
    public async Task CreateCheckout_WhenActivePaymentExists_ReturnsDuplicateError()
    {
        var (_, ticketId, userId) = CreateReservedTicket(out var ticket);
        var existingPayment = new Payment(PaymentId.CreateUnique(), ticketId, userId, DefaultPrice, PaymentProvider.Stripe);
        var mocks = CreateUnitOfWork(ticket, new List<Payment> { existingPayment });
        var handler = new CreateCheckoutHandler(mocks.Uow.Object, Mock.Of<IPaymentsService>());

        var result = await handler.Handle(new CreateCheckoutCommand(ticketId, userId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<DuplicatePaymentError>(result.Error);
    }

    [Fact]
    public async Task CreateCheckout_OnCurrencyMismatch_PropagatesError()
    {
        var (_, ticketId, userId) = CreateReservedTicket(out var ticket);
        var mocks = CreateUnitOfWork(ticket);

        var paymentsService = new Mock<IPaymentsService>();
        paymentsService
            .Setup(s => s.CreateCheckoutSessionAsync(It.IsAny<Money>(), It.IsAny<PaymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateCheckoutSessionResult>.Failure(
                new CurrencyMismatchError("Amount currency does not match configured currency")));

        var handler = new CreateCheckoutHandler(mocks.Uow.Object, paymentsService.Object);

        var result = await handler.Handle(new CreateCheckoutCommand(ticketId, userId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<CurrencyMismatchError>(result.Error);
    }
}