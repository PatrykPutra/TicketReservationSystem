using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Stripe.Checkout;
using Stripe;
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

public class StripeWebhookHandlerTests
{
    private static readonly Money DefaultPrice = new(100, "PLN");

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

    private static (TicketId TicketId, UserId UserId, PaymentId PaymentId) SeedReservedWithPendingPayment(ServiceProvider provider)
    {
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();
        var paymentId = PaymentId.CreateUnique();

        using var scope = provider.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var timeRange = new DateTimeRange(DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(4));
        var socialEvent = new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
        var ticket = new Ticket(ticketId, eventId, socialEvent, "A1", DefaultPrice);
        ticket.Reserve(userId);

        var payment = new Payment(paymentId, ticketId, userId, DefaultPrice, PaymentProvider.Stripe);
        payment.SetExternalId("cs_test_123");

        uow.Events.Add(socialEvent);
        uow.Tickets.Add(ticket);
        uow.Payments.Add(payment);
        uow.SaveChangesAsync().GetAwaiter().GetResult();

        return (ticketId, userId, paymentId);
    }

    private static Event CreateStripeEvent(string type, PaymentId paymentId)
    {
        return new Event
        {
            Type = type,
            Data = new EventData
            {
                Object = new Session { ClientReferenceId = paymentId.Value.ToString() },
            },
        };
    }

    private static Event CreateStripeEventWithClientReference(string type, string clientReferenceId)
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
    public async Task StripeWebhook_OnCompletedEvent_MarksPaymentPaidAndConfirmsTicket()
    {
        var dbName = Guid.NewGuid().ToString();
        var service = CreateServiceProvider(dbName);
        var (ticketId, userId, paymentId) = SeedReservedWithPendingPayment(service);

        using var scope = service.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler2 = new StripeWebhookHandler(uow);

        var result = await handler2.Handle(
            new StripeWebhookCommand(CreateStripeEvent("checkout.session.completed", paymentId)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var payment = (await uow.Payments.FindAsync(p => p.Id == paymentId)).Single();
        var ticket = await uow.Tickets.GetByIdAsync(ticketId);

        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.NotNull(ticket);
        Assert.Equal(TicketStatus.Confirmed, ticket.Status);
        Assert.Equal(userId, ticket.UserId);
    }

    [Fact]
    public async Task StripeWebhook_OnCompletedEventRedelivery_IsIdempotentNoop()
    {
        var dbName = Guid.NewGuid().ToString();
        var service = CreateServiceProvider(dbName);
        var (_, _, paymentId) = SeedReservedWithPendingPayment(service);

        using var scope = service.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new StripeWebhookHandler(uow);

        await handler.Handle(new StripeWebhookCommand(CreateStripeEvent("checkout.session.completed", paymentId)), CancellationToken.None);
        await handler.Handle(new StripeWebhookCommand(CreateStripeEvent("checkout.session.completed", paymentId)), CancellationToken.None);

        var payment = (await uow.Payments.FindAsync(p => p.Id == paymentId)).Single();
        Assert.Equal(PaymentStatus.Completed, payment.Status);
    }

    [Fact]
    public async Task StripeWebhook_OnExpiredEvent_MarksPaymentExpiredAndReleasesTicket()
    {
        var dbName = Guid.NewGuid().ToString();
        var service = CreateServiceProvider(dbName);
        var (ticketId, _, paymentId) = SeedReservedWithPendingPayment(service);

        using var scope = service.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new StripeWebhookHandler(uow);

        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("checkout.session.expired", paymentId)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var payment = (await uow.Payments.FindAsync(p => p.Id == paymentId)).Single();
        var ticket = await uow.Tickets.GetByIdAsync(ticketId);

        Assert.Equal(PaymentStatus.Expired, payment.Status);
        Assert.Equal(TicketStatus.Available, ticket!.Status);
        Assert.Null(ticket.UserId);
    }

    [Fact]
    public async Task StripeWebhook_OnPaymentFailedEvent_MarksPaymentFailedAndReleasesTicket()
    {
        var dbName = Guid.NewGuid().ToString();
        var service = CreateServiceProvider(dbName);
        var (ticketId, _, paymentId) = SeedReservedWithPendingPayment(service);

        using var scope = service.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new StripeWebhookHandler(uow);

        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("payment_intent.payment_failed", paymentId)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var payment = (await uow.Payments.FindAsync(p => p.Id == paymentId)).Single();
        var ticket = await uow.Tickets.GetByIdAsync(ticketId);

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal(TicketStatus.Available, ticket!.Status);
    }

    [Fact]
    public async Task StripeWebhook_OnUnknownEventType_IsNoop()
    {
        var dbName = Guid.NewGuid().ToString();
        var service = CreateServiceProvider(dbName);
        var (ticketId, _, paymentId) = SeedReservedWithPendingPayment(service);

        using var scope = service.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new StripeWebhookHandler(uow);

        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("some.other.event", paymentId)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var payment = (await uow.Payments.FindAsync(p => p.Id == paymentId)).Single();
        var ticket = await uow.Tickets.GetByIdAsync(ticketId);

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(TicketStatus.Reserved, ticket!.Status);
    }

    [Fact]
    public async Task StripeWebhook_OnUnknownEventWithNonSessionPayload_IsNoop()
    {
        var dbName = Guid.NewGuid().ToString();
        var service = CreateServiceProvider(dbName);
        var (ticketId, _, paymentId) = SeedReservedWithPendingPayment(service);

        using var scope = service.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new StripeWebhookHandler(uow);

        var nonSessionEvent = new Event
        {
            Type = "payment_intent.succeeded",
            Data = new EventData { Object = new PaymentIntent() },
        };

        var result = await handler.Handle(new StripeWebhookCommand(nonSessionEvent), CancellationToken.None);

        Assert.True(result.IsSuccess);

        var payment = (await uow.Payments.FindAsync(p => p.Id == paymentId)).Single();
        var ticket = await uow.Tickets.GetByIdAsync(ticketId);

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(TicketStatus.Reserved, ticket!.Status);
    }

    [Fact]
    public async Task StripeWebhook_OnNonSessionEventObject_ReturnsPaymentProcessingError()
    {
        var dbName = Guid.NewGuid().ToString();
        var service = CreateServiceProvider(dbName);
        var (_, _, paymentId) = SeedReservedWithPendingPayment(service);

        using var scope = service.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new StripeWebhookHandler(uow);

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
        var dbName = Guid.NewGuid().ToString();
        var service = CreateServiceProvider(dbName);
        var (_, _, paymentId) = SeedReservedWithPendingPayment(service);

        using var scope = service.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new StripeWebhookHandler(uow);

        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEventWithClientReference("checkout.session.completed", "not-a-guid")),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<PaymentProcessingError>(result.Error);
    }

    [Fact]
    public async Task StripeWebhook_WhenPaymentNotFound_ReturnsPaymentProcessingError()
    {
        var dbName = Guid.NewGuid().ToString();
        var service = CreateServiceProvider(dbName);
        var (_, _, _) = SeedReservedWithPendingPayment(service);

        using var scope = service.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new StripeWebhookHandler(uow);

        var unknownPaymentId = PaymentId.CreateUnique();
        var result = await handler.Handle(
            new StripeWebhookCommand(CreateStripeEvent("checkout.session.completed", unknownPaymentId)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<PaymentProcessingError>(result.Error);
    }
}
