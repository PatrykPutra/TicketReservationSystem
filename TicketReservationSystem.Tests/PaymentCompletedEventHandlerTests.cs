using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.DomainEventHandlers;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Events;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Domain.ValueObjects;
using TicketReservationSystem.Infrastructure.Persistence;
using TicketReservationSystem.Infrastructure.Repository;

namespace TicketReservationSystem.Tests;

public class PaymentCompletedEventHandlerTests
{
    private static readonly Money DefaultPrice = new(150, "PLN");

    private static ServiceProvider CreateServiceProvider(string dbName)
    {
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<Infrastructure.DomainEventsDispatcher.IDomainEventsDispatcher>(
            Mock.Of<Infrastructure.DomainEventsDispatcher.IDomainEventsDispatcher>());

        return services.BuildServiceProvider();
    }

    private static (PaymentId PaymentId, TicketId TicketId, UserId UserId) SeedCompletedPayment(ServiceProvider provider, string email = "user@test.com")
    {
        var paymentId = PaymentId.CreateUnique();
        var userId = UserId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var eventId = SocialEventId.CreateUnique();

        using var scope = provider.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var timeRange = new DateTimeRange(
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(4));
        var socialEvent = new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
        var ticket = new Ticket(ticketId, eventId, socialEvent, "A1", DefaultPrice);

        var payment = new Payment(paymentId, ticketId, userId, DefaultPrice, PaymentProvider.Stripe);
        payment.MarkCompleted();

        var user = new User(userId);
        user.Register(email, "Test", "User", "123456789");

        uow.Events.Add(socialEvent);
        uow.Tickets.Add(ticket);
        uow.Payments.Add(payment);
        uow.Users.Add(user);
        uow.SaveChangesAsync().GetAwaiter().GetResult();

        return (paymentId, ticketId, userId);
    }

    private static PaymentCompletedEventHandler CreateHandler(ServiceProvider provider, out Mock<IEmailSender> emailSender)
    {
        emailSender = new Mock<IEmailSender>();
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new PaymentCompletedEventHandler(
            provider.CreateScope().ServiceProvider.GetRequiredService<IUnitOfWork>(),
            emailSender.Object,
            NullLogger<PaymentCompletedEventHandler>.Instance);
    }

    [Fact]
    public async Task PaymentCompleted_ForResolvedData_SendsEmail()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);
        var (paymentId, ticketId, userId) = SeedCompletedPayment(serviceProvider);
        var handler = CreateHandler(serviceProvider, out var emailSender);

        var domainEvent = new PaymentCompletedEvent(paymentId, ticketId, userId);

        await handler.Handle(domainEvent, CancellationToken.None);

        emailSender.Verify(
            s => s.SendAsync(
                "user@test.com",
                "Payment completed",
                It.Is<string>(b => b.Contains("Test Event") && b.Contains("A1") && b.Contains("150") && b.Contains("PLN")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PaymentCompleted_WhenUserMissing_DoesNothing()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);
        var paymentId = PaymentId.CreateUnique();
        var handler = CreateHandler(serviceProvider, out var emailSender);

        var domainEvent = new PaymentCompletedEvent(paymentId, TicketId.CreateUnique(), UserId.CreateUnique());

        await handler.Handle(domainEvent, CancellationToken.None);

        emailSender.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PaymentCompleted_WhenSenderThrows_SwallowsException()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);
        var (paymentId, ticketId, userId) = SeedCompletedPayment(serviceProvider);

        var emailSender = new Mock<IEmailSender>();
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP down"));

        var handler = new PaymentCompletedEventHandler(
            serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IUnitOfWork>(),
            emailSender.Object,
            NullLogger<PaymentCompletedEventHandler>.Instance);

        var domainEvent = new PaymentCompletedEvent(paymentId, ticketId, userId);

        await handler.Handle(domainEvent, CancellationToken.None);
    }
}
