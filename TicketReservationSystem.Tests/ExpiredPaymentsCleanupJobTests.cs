using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Domain.ValueObjects;
using TicketReservationSystem.Infrastructure.Services.Jobs;
using TicketReservationSystem.Infrastructure.Persistence;
using TicketReservationSystem.Infrastructure.Repository;

namespace TicketReservationSystem.Tests;

public class ExpiredPaymentsCleanupJobTests
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

    private static async Task<(UserId UserId, TicketId TicketId)> SeedStalePendingPayment(ServiceProvider provider)
    {
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();
        var paymentId = PaymentId.CreateUnique();

        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var timeRange = new DateTimeRange(
            new DateTime(2027, 1, 15, 19, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 15, 23, 0, 0, DateTimeKind.Utc));
        var socialEvent = new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
        var ticket = new Ticket(ticketId, eventId, socialEvent, "A1", DefaultPrice);
        ticket.Reserve(userId);

        var payment = new Payment(paymentId, ticketId, userId, DefaultPrice, PaymentProvider.Stripe);

        uow.Events.Add(socialEvent);
        uow.Tickets.Add(ticket);
        uow.Payments.Add(payment);
        await uow.SaveChangesAsync();
        await ctx.Database.EnsureCreatedAsync();

        var savedPayment = await ctx.Payments.SingleAsync(p => p.Id == paymentId);
        typeof(Payment).GetProperty("CreatedAt")!.SetValue(savedPayment, DateTime.UtcNow.AddHours(-30));
        await ctx.SaveChangesAsync();

        return (userId, ticketId);
    }

    [Fact]
    public async Task Execute_ForStalePayments_MarksExpiredAndReleasesTicket()
    {
        var dbName = Guid.NewGuid().ToString();
        var service = CreateServiceProvider(dbName);
        var (_, ticketId) = await SeedStalePendingPayment(service);

        var scopeFactory = service.GetRequiredService<IServiceScopeFactory>();
        var job = new ExpiredPaymentsCleanupJob(scopeFactory, new NullLogger<ExpiredPaymentsCleanupJob>());
        await job.Execute(Mock.Of<IJobExecutionContext>());

        using var scope = service.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var ticket = await uow.Tickets.GetByIdAsync(ticketId);
        var payments = await uow.Payments.FindAsync(p => p.TicketId == ticketId);

        Assert.All(payments, p => Assert.Equal(PaymentStatus.Expired, p.Status));
        Assert.Equal(TicketStatus.Available, ticket!.Status);
        Assert.Null(ticket.UserId);
    }
}