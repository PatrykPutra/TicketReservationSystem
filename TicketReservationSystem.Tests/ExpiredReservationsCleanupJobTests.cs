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
    private static SocialEvent CreateSocialEvent()
    {
        var timeRange = new DateTimeRange(
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(4));
        var socialEvent = new SocialEvent(SocialEventId.CreateUnique(), "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
        return socialEvent;
    }

    private static Ticket CreateTicket(SocialEvent socialEvent)
    {
        Ticket ticket = new Ticket(TicketId.CreateUnique(), socialEvent.Id, socialEvent, "A1", DefaultPrice);
        return ticket;
    }

    private static Mock<IUnitOfWork> CreateUnitOfWorkMock(IPaymentRepository paymentRepository, ITicketRepository ticketRepository)
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(uow => uow.Payments).Returns(paymentRepository);
        unitOfWorkMock.Setup(uow => uow.Tickets).Returns(ticketRepository);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return unitOfWorkMock;
    }

    private static Mock<IServiceScopeFactory> CreateScopeFactoryMock(IUnitOfWork unitOfWork)
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IUnitOfWork))).Returns(unitOfWork);

        var scopeServiceMock = new Mock<IServiceScope>();
        scopeServiceMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeServiceMock.Object);

        return scopeFactoryMock;
    }

    [Fact]
    public async Task Execute_FilterPredicate_SelectsExpiredReservedAndExcludesFreshAndAvailable()
    {
        var socialEvent = CreateSocialEvent();
        var userId = UserId.CreateUnique();
        
        var expiredTicket = CreateTicket(socialEvent);
        expiredTicket.Reserve(userId,DateTime.UtcNow.AddMinutes(-20));
        
        var activeTicket = CreateTicket(socialEvent);
        activeTicket.Reserve(userId, DateTime.UtcNow.AddMinutes(-9));
        
        var availableTicket = CreateTicket(socialEvent);

        var capturedPredicate = default(Expression<Func<Ticket, bool>>);
        var ticketRepositoryMock = new Mock<ITicketRepository>();
        ticketRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Ticket, bool>>>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Ticket, bool>>, CancellationToken>((predicate, _) => capturedPredicate = predicate)
            .ReturnsAsync(new List<Ticket>());

        var unitOfWorkMock = CreateUnitOfWorkMock(Mock.Of<IPaymentRepository>(), ticketRepositoryMock.Object);
        var scopeFactoryMock = CreateScopeFactoryMock(unitOfWorkMock.Object);

        var job = new ExpiredReservationsCleanupJob(scopeFactoryMock.Object, new NullLogger<ExpiredReservationsCleanupJob>());

        // Act
        await job.Execute(Mock.Of<IJobExecutionContext>());

        // Assert
        var predicate = capturedPredicate!.Compile();
        Assert.True(predicate(expiredTicket));
        Assert.False(predicate(activeTicket));
        Assert.False(predicate(availableTicket));
    }

    [Fact]
    public async Task Execute_ForExpiredReservation_ReleasesTicket()
    {
        // Arrange
        var socialEvent = CreateSocialEvent();
        var userId = UserId.CreateUnique();

        var expiredTicket = CreateTicket(socialEvent);
        expiredTicket.Reserve(userId, DateTime.UtcNow.AddMinutes(-20));

        var ticketRepositoryMock = new Mock<ITicketRepository>();
        ticketRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Ticket, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ticket>() { expiredTicket });

        var paymentRepositoryMock = new Mock<IPaymentRepository>();
        paymentRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>());

        var unitOfWorkMock = CreateUnitOfWorkMock(paymentRepositoryMock.Object, ticketRepositoryMock.Object);
        var scopeFactoryMock = CreateScopeFactoryMock(unitOfWorkMock.Object);

        var job = new ExpiredReservationsCleanupJob(scopeFactoryMock.Object, new NullLogger<ExpiredReservationsCleanupJob>());

        // Act
        await job.Execute(Mock.Of<IJobExecutionContext>());
        
        // Assert
        Assert.Equal(TicketStatus.Available, expiredTicket.Status);
        Assert.Null(expiredTicket.UserId);
        Assert.Null(expiredTicket.ReservedAt);
    }

    [Fact]
    public async Task Execute_ForExpiredReservation_SavesChanges()
    {
        // Arrange
        var socialEvent = CreateSocialEvent();
        var userId = UserId.CreateUnique();

        var expiredTicket = CreateTicket(socialEvent);
        expiredTicket.Reserve(userId, DateTime.UtcNow.AddMinutes(-20));

        var ticketRepositoryMock = new Mock<ITicketRepository>();
        ticketRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Ticket, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ticket>() { expiredTicket });

        var paymentRepositoryMock = new Mock<IPaymentRepository>();
        paymentRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>());

        var unitOfWorkMock = CreateUnitOfWorkMock(paymentRepositoryMock.Object, ticketRepositoryMock.Object);
        var scopeFactoryMock = CreateScopeFactoryMock(unitOfWorkMock.Object);

        var job = new ExpiredReservationsCleanupJob(scopeFactoryMock.Object, new NullLogger<ExpiredReservationsCleanupJob>());

        // Act
        await job.Execute(Mock.Of<IJobExecutionContext>());

        //Assert
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ForTicketWithPendingPayment_SkipsCleanup()
    {
        var socialEvent = CreateSocialEvent();
        var userId = UserId.CreateUnique();

        var expiredTicket = CreateTicket(socialEvent);
        expiredTicket.Reserve(userId, DateTime.UtcNow.AddMinutes(-20));

        var ticketRepositoryMock = new Mock<ITicketRepository>();
        ticketRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Ticket, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ticket>() { expiredTicket });

        var pendingPayment = new Payment(PaymentId.CreateUnique(), expiredTicket.Id, userId, DefaultPrice, PaymentProvider.Stripe, DateTime.UtcNow);
        var paymentRepositoryMock = new Mock<IPaymentRepository>();
        paymentRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { pendingPayment });

        var unitOfWorkMock = CreateUnitOfWorkMock(paymentRepositoryMock.Object, ticketRepositoryMock.Object);
        var scopeFactoryMock = CreateScopeFactoryMock(unitOfWorkMock.Object);

        var job = new ExpiredReservationsCleanupJob(scopeFactoryMock.Object, new NullLogger<ExpiredReservationsCleanupJob>());

        // Act
        await job.Execute(Mock.Of<IJobExecutionContext>());
        
        // Assert
        Assert.Equal(TicketStatus.Reserved, expiredTicket.Status);
    }

    [Fact]
    public async Task Execute_ForJobWithoutChanges_DoesNotCallSaveChanges()
    {
        var ticketRepositoryMock = new Mock<ITicketRepository>();
        ticketRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Ticket, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ticket>());

        var paymentRepositoryMock = new Mock<IPaymentRepository>();
        paymentRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>());

        var unitOfWorkMock = CreateUnitOfWorkMock(paymentRepositoryMock.Object, ticketRepositoryMock.Object);
        var scopeFactoryMock = CreateScopeFactoryMock(unitOfWorkMock.Object);

        var job = new ExpiredReservationsCleanupJob(scopeFactoryMock.Object, new NullLogger<ExpiredReservationsCleanupJob>());

        // Act
        await job.Execute(Mock.Of<IJobExecutionContext>());

        // Assert
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}