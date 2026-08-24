using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;
using System.Linq.Expressions;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Domain.ValueObjects;
using TicketReservationSystem.Infrastructure.Services.Jobs;

namespace TicketReservationSystem.Tests;

public class ExpiredPaymentsCleanupJobTests
{
    private static readonly Money DefaultPrice = new(100, "PLN");

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
        SocialEvent socialEvent = CreateSocialEvent();
        Ticket ticket = new Ticket(TicketId.CreateUnique(), socialEvent.Id, socialEvent, "A1", DefaultPrice);
        return ticket;
    }

    private static Payment CreatePayment(Ticket ticket, PaymentProvider paymentProvider, TimeProvider timeProvider)
    {
        Payment payment = new Payment(PaymentId.CreateUnique(), ticket.Id, UserId.CreateUnique(), DefaultPrice, paymentProvider, timeProvider.GetUtcNow().DateTime);
        return payment;
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
    public async Task Execute_FilterPredicate_SelectsStalePendingAndExcludesFreshAndNonPending()
    {
        // Arrange      
        var delayedTimeProviderMock = new Mock<TimeProvider>();
        delayedTimeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(DateTimeOffset.UtcNow.AddHours(-30));
        
        var timeProviderMock = new Mock<TimeProvider>();
        timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(DateTimeOffset.UtcNow);

        var freshPayment = CreatePayment(CreateTicket(), PaymentProvider.Stripe, timeProviderMock.Object);
        var delayedPayment = CreatePayment(CreateTicket(), PaymentProvider.Stripe, delayedTimeProviderMock.Object);
        var completedPayment = CreatePayment(CreateTicket(), PaymentProvider.Stripe, delayedTimeProviderMock.Object);
        completedPayment.MarkCompleted();

        var capturedPredicate = default(Expression<Func<Payment, bool>>);
        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Payment, bool>>, CancellationToken>((predicate, _) => capturedPredicate = predicate)
            .ReturnsAsync(new List<Payment>());

        var unitOfWorkMock = CreateUnitOfWorkMock(paymentsRepositoryMock.Object, Mock.Of<ITicketRepository>());
        var scopeFactoryMock = CreateScopeFactoryMock(unitOfWorkMock.Object);
        var job = new ExpiredPaymentsCleanupJob(scopeFactoryMock.Object, new NullLogger<ExpiredPaymentsCleanupJob>());

        // Act
        await job.Execute(Mock.Of<IJobExecutionContext>());

        // Assert
        var predicate = capturedPredicate!.Compile();
        Assert.True(predicate(delayedPayment));
        Assert.False(predicate(freshPayment));
        Assert.False(predicate(completedPayment));
    }

    [Fact]
    public async Task Execute_ForStalePayment_MarksPaymentExpired()
    {
        // Arrange      
        var delayedTimeProviderMock = new Mock<TimeProvider>();
        delayedTimeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(DateTimeOffset.UtcNow.AddHours(-30));

        var ticket = CreateTicket();
        var delayedPayment = CreatePayment(ticket, PaymentProvider.Stripe, delayedTimeProviderMock.Object);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>() { delayedPayment });

        var unitOfWorkMock = CreateUnitOfWorkMock(paymentsRepositoryMock.Object, Mock.Of<ITicketRepository>());
        var scopeFactoryMock = CreateScopeFactoryMock(unitOfWorkMock.Object);
        var job = new ExpiredPaymentsCleanupJob(scopeFactoryMock.Object, new NullLogger<ExpiredPaymentsCleanupJob>());

        // Act
        await job.Execute(Mock.Of<IJobExecutionContext>());
        
        // Assert
        Assert.Equal(PaymentStatus.Expired, delayedPayment.Status);
    }

    [Fact]
    public async Task Execute_ForStalePayment_ReleasesLinkedReservedTicket()
    {
        // Arrange      
        var delayedTimeProviderMock = new Mock<TimeProvider>();
        delayedTimeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(DateTimeOffset.UtcNow.AddHours(-30));

        var ticket = CreateTicket();
        var delayedPayment = CreatePayment(ticket, PaymentProvider.Stripe, delayedTimeProviderMock.Object);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>() { delayedPayment });

        var unitOfWorkMock = CreateUnitOfWorkMock(paymentsRepositoryMock.Object, Mock.Of<ITicketRepository>());
        var scopeFactoryMock = CreateScopeFactoryMock(unitOfWorkMock.Object);
        var job = new ExpiredPaymentsCleanupJob(scopeFactoryMock.Object, new NullLogger<ExpiredPaymentsCleanupJob>());

        // Act
        await job.Execute(Mock.Of<IJobExecutionContext>());

        // Assert
        Assert.Equal(TicketStatus.Available, ticket.Status);
        Assert.Null(ticket.UserId);
    }

    [Fact]
    public async Task Execute_ForStalePayment_SavesChanges()
    {
        // Arrange      
        var delayedTimeProviderMock = new Mock<TimeProvider>();
        delayedTimeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(DateTimeOffset.UtcNow.AddHours(-30));

        var ticket = CreateTicket();
        var delayedPayment = CreatePayment(ticket, PaymentProvider.Stripe, delayedTimeProviderMock.Object);

        var paymentsRepositoryMock = new Mock<IPaymentRepository>();
        paymentsRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>() { delayedPayment });

        var unitOfWorkMock = CreateUnitOfWorkMock(paymentsRepositoryMock.Object, Mock.Of<ITicketRepository>());
        var scopeFactoryMock = CreateScopeFactoryMock(unitOfWorkMock.Object);
        var job = new ExpiredPaymentsCleanupJob(scopeFactoryMock.Object, new NullLogger<ExpiredPaymentsCleanupJob>());

        // Act
        await job.Execute(Mock.Of<IJobExecutionContext>());

        // Assert
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}