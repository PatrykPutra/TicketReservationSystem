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

public class ExpiredReservationsCleanupJobTests
{
    private static readonly Money DefaultPrice = new(150, "PLN");

    private static (SocialEvent SocialEvent, UserId UserId) CreateSeededData()
    {
        var eventId = SocialEventId.CreateUnique();
        var timeRange = new DateTimeRange(
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(4));
        return (new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice), UserId.CreateUnique());
    }

    private static Ticket CreateReservedTicket(SocialEvent socialEvent, UserId userId, DateTime? reservedAt)
    {
        var ticket = new Ticket(TicketId.CreateUnique(), socialEvent.Id, socialEvent, "A1", DefaultPrice);
        ticket.Reserve(userId);
        ticket.ReservedAt = reservedAt;
        return ticket;
    }

    private static (Mock<IServiceScopeFactory> ScopeFactory, Mock<IUnitOfWork> UnitOfWork, Mock<ITicketRepository> Tickets, Mock<IPaymentRepository> Payments) CreateJobMocks()
    {
        var tickets = new Mock<ITicketRepository>();
        tickets
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Ticket, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ticket>());

        var payments = new Mock<IPaymentRepository>();
        payments
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>());

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Tickets).Returns(tickets.Object);
        uow.SetupGet(u => u.Payments).Returns(payments.Object);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(IUnitOfWork))).Returns(uow.Object);

        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        return (scopeFactory, uow, tickets, payments);
    }

    [Fact]
    public async Task Execute_FilterPredicate_SelectsExpiredReservedAndExcludesFreshAndAvailable()
    {
        var (socialEvent, userId) = CreateSeededData();
        var (scopeFactory, _, tickets, _) = CreateJobMocks();

        var expiredTicket = CreateReservedTicket(socialEvent, userId, DateTime.UtcNow.AddMinutes(-20));
        var freshTicket = CreateReservedTicket(socialEvent, userId, DateTime.UtcNow);
        var availableTicket = new Ticket(TicketId.CreateUnique(), socialEvent.Id, socialEvent, "A2", DefaultPrice);

        var capturedPredicate = default(Expression<Func<Ticket, bool>>);
        tickets
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Ticket, bool>>>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Ticket, bool>>, CancellationToken>((predicate, _) => capturedPredicate = predicate)
            .ReturnsAsync(new List<Ticket>());

        var job = new ExpiredReservationsCleanupJob(scopeFactory.Object, new NullLogger<ExpiredReservationsCleanupJob>());
        await job.Execute(Mock.Of<IJobExecutionContext>());

        var predicate = capturedPredicate!.Compile();
        Assert.True(predicate(expiredTicket));
        Assert.False(predicate(freshTicket));
        Assert.False(predicate(availableTicket));
    }

    [Fact]
    public async Task Execute_ForExpiredReservation_ReleasesTicket()
    {
        var (socialEvent, userId) = CreateSeededData();
        var (scopeFactory, _, tickets, _) = CreateJobMocks();
        var expiredTicket = CreateReservedTicket(socialEvent, userId, DateTime.UtcNow.AddMinutes(-20));

        tickets
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Ticket, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ticket> { expiredTicket });

        var job = new ExpiredReservationsCleanupJob(scopeFactory.Object, new NullLogger<ExpiredReservationsCleanupJob>());
        await job.Execute(Mock.Of<IJobExecutionContext>());

        Assert.Equal(TicketStatus.Available, expiredTicket.Status);
        Assert.Null(expiredTicket.UserId);
        Assert.Null(expiredTicket.ReservedAt);
    }

    [Fact]
    public async Task Execute_ForExpiredReservation_SavesChanges()
    {
        var (socialEvent, userId) = CreateSeededData();
        var (scopeFactory, uow, tickets, _) = CreateJobMocks();
        var expiredTicket = CreateReservedTicket(socialEvent, userId, DateTime.UtcNow.AddMinutes(-20));

        tickets
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Ticket, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ticket> { expiredTicket });

        var job = new ExpiredReservationsCleanupJob(scopeFactory.Object, new NullLogger<ExpiredReservationsCleanupJob>());
        await job.Execute(Mock.Of<IJobExecutionContext>());

        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_WithPendingPayment_SkipsTicket()
    {
        var (socialEvent, userId) = CreateSeededData();
        var (scopeFactory, uow, tickets, payments) = CreateJobMocks();
        var expiredTicket = CreateReservedTicket(socialEvent, userId, DateTime.UtcNow.AddMinutes(-20));

        tickets
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Ticket, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ticket> { expiredTicket });

        var pendingPayment = new Payment(PaymentId.CreateUnique(), expiredTicket.Id, userId, DefaultPrice, PaymentProvider.Stripe);
        payments
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { pendingPayment });

        var job = new ExpiredReservationsCleanupJob(scopeFactory.Object, new NullLogger<ExpiredReservationsCleanupJob>());
        await job.Execute(Mock.Of<IJobExecutionContext>());

        Assert.Equal(TicketStatus.Reserved, expiredTicket.Status);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}