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

    private static SocialEvent CreateSocialEvent()
    {
        var timeRange = new DateTimeRange(
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(4));
        var socialEvent = new SocialEvent(SocialEventId.CreateUnique(), "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
        return socialEvent;
    }

    private static Ticket CreateTicket()
    {
        var ticketId = TicketId.CreateUnique();

        var socialEvent = CreateSocialEvent();
        var ticket = new Ticket(ticketId, socialEvent.Id, socialEvent, "A1", DefaultPrice);

        return ticket;
    }

    private static Ticket CreateReservedTicket(UserId userId)
    {
        var ticket = CreateTicket();
        ticket.Reserve(userId);

        return ticket;
    }

    [Fact]
    public async Task CreateCheckout_ForReservedTicket_ReturnsCheckoutSessionDetails()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var ticket = CreateReservedTicket(userId);

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>());

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var checkoutSessionResult = new CreateCheckoutSessionResult("https://checkout.url", "cs_test_123");
        var paymentsServiceMock = new Mock<IPaymentsService>();
        paymentsServiceMock
            .Setup(s => s.CreateCheckoutSessionAsync(It.IsAny<Money>(), It.IsAny<PaymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateCheckoutSessionResult>.Success(checkoutSessionResult));

        var handler = new CreateCheckoutHandler(unitOfWorkMock.Object, paymentsServiceMock.Object,TimeProvider.System);

        // Act
        var result = await handler.Handle(new CreateCheckoutCommand(ticket.Id, userId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(checkoutSessionResult.SessionId, result.Value.SessionId);
        Assert.Equal(checkoutSessionResult.CheckoutUrl, result.Value.CheckoutUrl);
    }

    [Fact]
    public async Task CreateCheckout_ForReservedTicket_AddsPendingPaymentWithExternalId()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var ticket = CreateReservedTicket(userId);

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>());
        
        Payment? capturedPayment = null;
        paymentsRepositoryMock
            .Setup(r => r.Add(It.IsAny<Payment>()))
            .Callback<Payment>(payment => capturedPayment = payment);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var checkoutSessionResult = new CreateCheckoutSessionResult("https://checkout.url", "cs_test_123");
        var paymentsServiceMock = new Mock<IPaymentsService>();
        paymentsServiceMock
            .Setup(s => s.CreateCheckoutSessionAsync(It.IsAny<Money>(), It.IsAny<PaymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateCheckoutSessionResult>.Success(checkoutSessionResult));

        var handler = new CreateCheckoutHandler(unitOfWorkMock.Object, paymentsServiceMock.Object, TimeProvider.System);

        // Act
        var result = await handler.Handle(new CreateCheckoutCommand(ticket.Id, userId), CancellationToken.None);

        Assert.NotNull(capturedPayment);
        Assert.Equal(PaymentStatus.Pending, capturedPayment.Status);
        Assert.Equal(checkoutSessionResult.SessionId, capturedPayment.ExternalId);
    }

    [Fact]
    public async Task CreateCheckout_ForReservedTicket_SavesChanges()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var ticket = CreateReservedTicket(userId);

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>());

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var checkoutSessionResult = new CreateCheckoutSessionResult("https://checkout.url", "cs_test_123");
        var paymentsServiceMock = new Mock<IPaymentsService>();
        paymentsServiceMock
            .Setup(s => s.CreateCheckoutSessionAsync(It.IsAny<Money>(), It.IsAny<PaymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateCheckoutSessionResult>.Success(checkoutSessionResult));

        var handler = new CreateCheckoutHandler(unitOfWorkMock.Object, paymentsServiceMock.Object, TimeProvider.System);

        // Act
        var result = await handler.Handle(new CreateCheckoutCommand(ticket.Id, userId), CancellationToken.None);
        
        // Assert
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCheckout_ForMissingTicket_ReturnsNotFoundErrorResult()
    {
        // Arrange
        Ticket? ticket = default;

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>());

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new CreateCheckoutHandler(unitOfWorkMock.Object, Mock.Of<IPaymentsService>(), TimeProvider.System);

        // Act
        var result = await handler.Handle(new CreateCheckoutCommand(TicketId.CreateUnique(), UserId.CreateUnique()), CancellationToken.None);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<NotFoundError>(result.Error);
    }

    [Fact]
    public async Task CreateCheckout_ForUnreservedTicket_ReturnsNotReservedErrorResult()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var ticket = CreateTicket();

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>());

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new CreateCheckoutHandler(unitOfWorkMock.Object, Mock.Of<IPaymentsService>(), TimeProvider.System);

        // Act
        var result = await handler.Handle(new CreateCheckoutCommand(ticket.Id, userId), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<TicketNotReservedError>(result.Error);
    }

    [Fact]
    public async Task CreateCheckout_WhenActivePaymentExists_ReturnsDuplicateErrorResult()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var ticket = CreateReservedTicket(userId);

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var existingPayment = new Payment(PaymentId.CreateUnique(), ticket.Id, userId, DefaultPrice, PaymentProvider.Stripe, DateTime.UtcNow);
        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>() { existingPayment });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new CreateCheckoutHandler(unitOfWorkMock.Object, Mock.Of<IPaymentsService>(), TimeProvider.System);

        // Act
        var result = await handler.Handle(new CreateCheckoutCommand(ticket.Id, userId), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<DuplicatePaymentError>(result.Error);
    }

    [Fact]
    public async Task CreateCheckout_ForCurrencyMismatch_ReturnsFailedResultWithCurrencyMismatchError()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var ticket = CreateReservedTicket(userId);

        var ticketsRepositoryMock = new Mock<ITicketRepository>();
        ticketsRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>());

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Tickets).Returns(ticketsRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Payments).Returns(paymentsRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var paymentsServiceMock = new Mock<IPaymentsService>();
        paymentsServiceMock
            .Setup(s => s.CreateCheckoutSessionAsync(It.IsAny<Money>(), It.IsAny<PaymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateCheckoutSessionResult>.Failure(
                new CurrencyMismatchError("Amount currency does not match configured currency")));

        var handler = new CreateCheckoutHandler(unitOfWorkMock.Object, paymentsServiceMock.Object, TimeProvider.System);

        // Act
        var result = await handler.Handle(new CreateCheckoutCommand(ticket.Id, userId), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<CurrencyMismatchError>(result.Error);
    }
}