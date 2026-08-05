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

public class ExpiredReservationsCleanupJobTests
{
    private static readonly Money DefaultPrice = new(150, "PLN");

    private static ServiceProvider CreateServiceProvider(string dbName)
    {
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<Infrastructure.DomainEventsDispatcher.IDomainEventsDispatcher>(
            Mock.Of<Infrastructure.DomainEventsDispatcher.IDomainEventsDispatcher>());

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Execute_releases_expired_reserved_tickets()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var logger = new NullLogger<ExpiredReservationsCleanupJob>();

        var eventId = SocialEventId.CreateUnique();
        var timeRange = new DateTimeRange(
            new DateTime(2027, 1, 15, 19, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 15, 23, 0, 0, DateTimeKind.Utc));
        var socialEvent = new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
        var userId = UserId.CreateUnique();

        using (var scope = scopeFactory.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            uow.Events.Add(socialEvent);

            var user = new User(userId);
            user.Register("test@test.com", "Test", "User", "123456789");
            user.VerifyEmail();
            uow.Users.Add(user);

            var expiredTicket = new Ticket(TicketId.CreateUnique(), eventId, socialEvent, "A1", DefaultPrice);
            expiredTicket.Reserve(userId);
            uow.Tickets.Add(expiredTicket);

            var availableTicket = new Ticket(TicketId.CreateUnique(), eventId, socialEvent, "A2", DefaultPrice);
            uow.Tickets.Add(availableTicket);

            var freshReserved = new Ticket(TicketId.CreateUnique(), eventId, socialEvent, "A3", DefaultPrice);
            freshReserved.Reserve(UserId.CreateUnique());
            uow.Tickets.Add(freshReserved);

            await uow.SaveChangesAsync();

            await ctx.Database.EnsureCreatedAsync();

            var savedTicket = await ctx.Tickets.SingleAsync(t => t.SeatNumber == "A1");
            typeof(Ticket).GetProperty("ReservedAt")!.SetValue(savedTicket, DateTime.UtcNow.AddMinutes(-20));
            await ctx.SaveChangesAsync();

            var verifyTicket = await ctx.Tickets.SingleAsync(t => t.SeatNumber == "A1");
            Assert.True(verifyTicket.ReservedAt < DateTime.UtcNow.AddMinutes(-15));
        }

        var job = new ExpiredReservationsCleanupJob(scopeFactory, logger);
        await job.Execute(Mock.Of<IJobExecutionContext>());

        using (var scope = scopeFactory.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tickets = await ctx.Tickets.ToListAsync();

            var released = tickets.Single(t => t.SeatNumber == "A1");
            Assert.Equal(TicketStatus.Available, released.Status);
            Assert.Null(released.UserId);

            var stillAvailable = tickets.Single(t => t.SeatNumber == "A2");
            Assert.Equal(TicketStatus.Available, stillAvailable.Status);

            var stillReserved = tickets.Single(t => t.SeatNumber == "A3");
            Assert.Equal(TicketStatus.Reserved, stillReserved.Status);
        }
    }

    [Fact]
    public async Task Execute_does_not_release_tickets_within_threshold()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var logger = new NullLogger<ExpiredReservationsCleanupJob>();

        var eventId = SocialEventId.CreateUnique();
        var timeRange = new DateTimeRange(
            new DateTime(2027, 1, 15, 19, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 15, 23, 0, 0, DateTimeKind.Utc));
        var socialEvent = new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
        var userId = UserId.CreateUnique();

        using (var scope = scopeFactory.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            uow.Events.Add(socialEvent);

            var user = new User(userId);
            user.Register("test@test.com", "Test", "User", "123456789");
            user.VerifyEmail();
            uow.Users.Add(user);

            var freshReserved = new Ticket(TicketId.CreateUnique(), eventId, socialEvent, "B1", DefaultPrice);
            freshReserved.Reserve(userId);
            uow.Tickets.Add(freshReserved);

            await uow.SaveChangesAsync();
        }

        var job = new ExpiredReservationsCleanupJob(scopeFactory, logger);
        await job.Execute(Mock.Of<IJobExecutionContext>());

        using (var scope = scopeFactory.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var ticket = await ctx.Tickets.SingleAsync();
            Assert.Equal(TicketStatus.Reserved, ticket.Status);
        }
    }

    [Fact]
    public async Task Execute_does_not_release_reserved_ticket_with_active_pending_payment()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var logger = new NullLogger<ExpiredReservationsCleanupJob>();

        var eventId = SocialEventId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();
        var timeRange = new DateTimeRange(
            new DateTime(2027, 1, 15, 19, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 15, 23, 0, 0, DateTimeKind.Utc));
        var socialEvent = new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
        var user = new User(userId);
        user.Register("test@test.com", "Test", "User", "123456789");
        user.VerifyEmail();

        using (var scope = scopeFactory.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            uow.Events.Add(socialEvent);
            uow.Users.Add(user);

            var reservedTicket = new Ticket(ticketId, eventId, socialEvent, "A1", DefaultPrice);
            reservedTicket.Reserve(userId);
            uow.Tickets.Add(reservedTicket);

            var payment = new Payment(PaymentId.CreateUnique(), ticketId, userId, DefaultPrice, PaymentProvider.Stripe);
            uow.Payments.Add(payment);

            await uow.SaveChangesAsync();
            await ctx.Database.EnsureCreatedAsync();

            var saved = await ctx.Tickets.SingleAsync(t => t.Id == ticketId);
            typeof(Ticket).GetProperty("ReservedAt")!.SetValue(saved, DateTime.UtcNow.AddMinutes(-20));
            await ctx.SaveChangesAsync();
        }

        var job = new ExpiredReservationsCleanupJob(scopeFactory, logger);
        await job.Execute(Mock.Of<IJobExecutionContext>());

        using (var scope = scopeFactory.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var ticket = await ctx.Tickets.SingleAsync();
            Assert.Equal(TicketStatus.Reserved, ticket.Status);
        }
    }
}
