using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands.Payments;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Domain.ValueObjects;
using TicketReservationSystem.Infrastructure.Persistence;
using TicketReservationSystem.Infrastructure.Repository;

namespace TicketReservationSystem.Tests;

public class PaymentCheckoutHandlerTests
{
    private static readonly Money DefaultPrice = new(150, "PLN");

    private static ServiceProvider CreateServiceProvider(string dbName)
    {
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<Infrastructure.DomainEventsDispatcher.IDomainEventsDispatcher>(
            Mock.Of<Infrastructure.DomainEventsDispatcher.IDomainEventsDispatcher>());

        return services.BuildServiceProvider();
    }

    private static (SocialEventId EventId, TicketId TicketId, UserId UserId) SeedReservedTicket(ServiceProvider provider)
    {
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();

        using var scope = provider.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var timeRange = new DateTimeRange(
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(4));
        var socialEvent = new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
        var ticket = new Ticket(ticketId, eventId, socialEvent, "A1", DefaultPrice);
        ticket.Reserve(userId);

        uow.Events.Add(socialEvent);
        uow.Tickets.Add(ticket);
        uow.SaveChangesAsync().GetAwaiter().GetResult();

        return (eventId, ticketId, userId);
    }

    [Fact]
    public async Task Handle_creates_pending_payment_and_returns_checkout()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);
        var (_, ticketId, userId) = SeedReservedTicket(serviceProvider);

        var mockPaymentsService = new Mock<IPaymentsService>();
        mockPaymentsService
            .Setup(s => s.CreateCheckoutSessionAsync(It.IsAny<Money>(), It.IsAny<PaymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateCheckoutSessionResult>.Success(new CreateCheckoutSessionResult("https://checkout.url", "cs_test_123")));

        using var scope = serviceProvider.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new CreateCheckoutHandler(uow, mockPaymentsService.Object);

        var result = await handler.Handle(new CreateCheckoutCommand(ticketId, userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("cs_test_123", result.Value.SessionId);
        Assert.Equal("https://checkout.url", result.Value.CheckoutUrl);

        var saved = (await uow.Payments.FindAsync(p => p.TicketId == ticketId)).Single();
        Assert.Equal(PaymentStatus.Pending, saved.Status);
        Assert.Equal("cs_test_123", saved.ExternalId);
    }

    [Fact]
    public async Task Handle_returns_not_reserved_error_for_unreserved_ticket()
    {
        var dbName = Guid.NewGuid().ToString();
        var service = CreateServiceProvider(dbName);
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();

        using (var scope = service.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var timeRange = new DateTimeRange(DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4));
            var socialEvent = new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
            var ticket = new Ticket(ticketId, eventId, socialEvent, "A1", DefaultPrice);
            uow.Events.Add(socialEvent);
            uow.Tickets.Add(ticket);
            await uow.SaveChangesAsync();
        }

        var mockPaymentsService = new Mock<IPaymentsService>();

        using var scope2 = service.CreateScope();
        var uow2 = scope2.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new CreateCheckoutHandler(uow2, mockPaymentsService.Object);

        var result = await handler.Handle(new CreateCheckoutCommand(ticketId, userId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.IsType<TicketNotReservedError>(result.Error);
    }

    [Fact]
    public async Task Handle_returns_duplicate_error_when_active_payment_exists()
    {
        var dbName = Guid.NewGuid().ToString();
        var service = CreateServiceProvider(dbName);
        var (_, ticketId, userId) = SeedReservedTicket(service);

        using (var scope = service.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var payment = new Payment(PaymentId.CreateUnique(), ticketId, userId, DefaultPrice, PaymentProvider.Stripe);
            uow.Payments.Add(payment);
            await uow.SaveChangesAsync();
        }

        var mockPaymentsService = new Mock<IPaymentsService>();

        using var scope2 = service.CreateScope();
        var uow2 = scope2.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new CreateCheckoutHandler(uow2, mockPaymentsService.Object);

        var result = await handler.Handle(new CreateCheckoutCommand(ticketId, userId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.IsType<DuplicatePaymentError>(result.Error);
    }

    [Fact]
    public async Task Handle_propagates_currency_mismatch_error_from_service()
    {
        var dbName = Guid.NewGuid().ToString();
        var service = CreateServiceProvider(dbName);
        var (_, ticketId, userId) = SeedReservedTicket(service);

        var mockPaymentsService = new Mock<IPaymentsService>();
        mockPaymentsService
            .Setup(s => s.CreateCheckoutSessionAsync(It.IsAny<Money>(), It.IsAny<PaymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateCheckoutSessionResult>.Failure(
                new CurrencyMismatchError("Amount currency does not match configured currency")));

        using var scope = service.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new CreateCheckoutHandler(uow, mockPaymentsService.Object);

        var result = await handler.Handle(new CreateCheckoutCommand(ticketId, userId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.IsType<CurrencyMismatchError>(result.Error);
    }
}
