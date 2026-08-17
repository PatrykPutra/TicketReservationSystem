using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Domain.ValueObjects;
using TicketReservationSystem.Infrastructure.Services.Jobs;

namespace TicketReservationSystem.Tests;

public class ExpiredPaymentsCleanupJobTests
{
    private static readonly Money DefaultPrice = new(100, "PLN");

    private static (Payment StalePayment, Ticket ReservedTicket) CreateStalePaymentAndReservedTicket()
    {
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();

        var timeRange = new DateTimeRange(
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(4));
        var socialEvent = new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);

        var ticket = new Ticket(ticketId, eventId, socialEvent, "A1", DefaultPrice);
        ticket.Reserve(userId);

        var payment = new Payment(PaymentId.CreateUnique(), ticketId, userId, DefaultPrice, PaymentProvider.Stripe);
        payment.CreatedAt = DateTime.UtcNow.AddHours(-30);

        return (payment, ticket);
    }

    private static (Mock<IServiceScopeFactory> ScopeFactory, Mock<IUnitOfWork> UnitOfWork, Mock<IPaymentRepository> Payments, Mock<ITicketRepository> Tickets) CreateJobMocks(Ticket? linkedTicket)
    {
        var payments = new Mock<IPaymentRepository>();
        payments
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>());

        var tickets = new Mock<ITicketRepository>();
        tickets
            .Setup(r => r.GetByIdAsync(It.IsAny<TicketId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(linkedTicket);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Payments).Returns(payments.Object);
        uow.SetupGet(u => u.Tickets).Returns(tickets.Object);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(IUnitOfWork))).Returns(uow.Object);

        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        return (scopeFactory, uow, payments, tickets);
    }

    [Fact]
    public async Task Execute_FilterPredicate_SelectsStalePendingAndExcludesFreshAndNonPending()
    {
        var (stalePayment, _) = CreateStalePaymentAndReservedTicket();

        var freshPayment = new Payment(PaymentId.CreateUnique(), stalePayment.TicketId, stalePayment.UserId, DefaultPrice, PaymentProvider.Stripe);
        var completedPayment = new Payment(PaymentId.CreateUnique(), stalePayment.TicketId, stalePayment.UserId, DefaultPrice, PaymentProvider.Stripe);
        completedPayment.MarkCompleted();

        var (scopeFactory, _, payments, _) = CreateJobMocks(linkedTicket: null);

        var capturedPredicate = default(Expression<Func<Payment, bool>>);
        payments
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Payment, bool>>, CancellationToken>((predicate, _) => capturedPredicate = predicate)
            .ReturnsAsync(new List<Payment>());

        var job = new ExpiredPaymentsCleanupJob(scopeFactory.Object, new NullLogger<ExpiredPaymentsCleanupJob>());
        await job.Execute(Mock.Of<IJobExecutionContext>());

        var predicate = capturedPredicate!.Compile();
        Assert.True(predicate(stalePayment));
        Assert.False(predicate(freshPayment));
        Assert.False(predicate(completedPayment));
    }

    [Fact]
    public async Task Execute_ForStalePayment_MarksPaymentExpired()
    {
        var (stalePayment, ticket) = CreateStalePaymentAndReservedTicket();
        var (scopeFactory, _, payments, _) = CreateJobMocks(ticket);

        payments
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { stalePayment });

        var job = new ExpiredPaymentsCleanupJob(scopeFactory.Object, new NullLogger<ExpiredPaymentsCleanupJob>());
        await job.Execute(Mock.Of<IJobExecutionContext>());

        Assert.Equal(PaymentStatus.Expired, stalePayment.Status);
    }

    [Fact]
    public async Task Execute_ForStalePayment_ReleasesLinkedReservedTicket()
    {
        var (stalePayment, ticket) = CreateStalePaymentAndReservedTicket();
        var (scopeFactory, _, payments, _) = CreateJobMocks(ticket);

        payments
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { stalePayment });

        var job = new ExpiredPaymentsCleanupJob(scopeFactory.Object, new NullLogger<ExpiredPaymentsCleanupJob>());
        await job.Execute(Mock.Of<IJobExecutionContext>());

        Assert.Equal(TicketStatus.Available, ticket.Status);
        Assert.Null(ticket.UserId);
    }

    [Fact]
    public async Task Execute_ForStalePayment_SavesChanges()
    {
        var (stalePayment, ticket) = CreateStalePaymentAndReservedTicket();
        var (scopeFactory, uow, payments, _) = CreateJobMocks(ticket);

        payments
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { stalePayment });

        var job = new ExpiredPaymentsCleanupJob(scopeFactory.Object, new NullLogger<ExpiredPaymentsCleanupJob>());
        await job.Execute(Mock.Of<IJobExecutionContext>());

        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ForStalePayment_WhenTicketMissing_StillMarksExpired()
    {
        var (stalePayment, _) = CreateStalePaymentAndReservedTicket();
        var (scopeFactory, _, payments, _) = CreateJobMocks(linkedTicket: null);

        payments
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { stalePayment });

        var job = new ExpiredPaymentsCleanupJob(scopeFactory.Object, new NullLogger<ExpiredPaymentsCleanupJob>());
        await job.Execute(Mock.Of<IJobExecutionContext>());

        Assert.Equal(PaymentStatus.Expired, stalePayment.Status);
    }
}