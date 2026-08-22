using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.ValueObjects;
using TicketReservationSystem.Infrastructure.DomainEventsDispatcher;
using TicketReservationSystem.Infrastructure.Persistence;

namespace TicketReservationSystem.Tests;

public class ApplicationDbContextTests
{
    [Fact]
    public async Task SaveChangesAsync_ForValidInput_SavesPaymentWithoutLoss()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddSingleton<IDomainEventsDispatcher>(
            Mock.Of<IDomainEventsDispatcher>());

        var serviceProvider = services.BuildServiceProvider();

        var paymentId = PaymentId.CreateUnique();
        var amount = new Money(150, "PLN");

        // Act
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var payment = new Payment(paymentId, TicketId.CreateUnique(), UserId.CreateUnique(), amount, PaymentProvider.Stripe);
            dbContext.Payments.Add(payment);
            await dbContext.SaveChangesAsync();
        }

        // Assert
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var saved = await dbContext.Payments.SingleAsync(p => p.Id == paymentId);

            Assert.Equal(amount, saved.Amount);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_ForValidInput_SavesEventWithoutLoss()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddSingleton<IDomainEventsDispatcher>(
            Mock.Of<IDomainEventsDispatcher>());

        var serviceProvider = services.BuildServiceProvider();

        var eventId = SocialEventId.CreateUnique();
        var timeRange = new DateTimeRange(
            new DateTime(2027, 1, 15, 19, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 15, 23, 0, 0, DateTimeKind.Utc));

        var socialEvent = new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, new Money(150, "PLN"));

        // Act
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.SocialEvents.Add(socialEvent);
            await dbContext.SaveChangesAsync();
        }

        // Assert
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var saved = await dbContext.SocialEvents.SingleAsync(e => e.Id == eventId);
            Assert.Equal(socialEvent, saved);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_ForValidInput_SavesUserWithoutLoss()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddSingleton<IDomainEventsDispatcher>(
            Mock.Of<IDomainEventsDispatcher>());

        var serviceProvider = services.BuildServiceProvider();

        var user = User.Register("test@test.com", "Test", "User", "123456789");

        // Act
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        }

        // Assert
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var saved = await dbContext.Users.SingleAsync(u => u.Id == user.Id);
            Assert.Equal(user, saved);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_ForValidInput_SavesTicketWithoutLoss()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddSingleton<IDomainEventsDispatcher>(
            Mock.Of<IDomainEventsDispatcher>());

        var serviceProvider = services.BuildServiceProvider();

        var eventId = SocialEventId.CreateUnique();
        var timeRange = new DateTimeRange(
            new DateTime(2027, 1, 15, 19, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 15, 23, 0, 0, DateTimeKind.Utc));

        var socialEvent = new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, new Money(150, "PLN"));

        var ticketId = TicketId.CreateUnique();
        var ticket = new Ticket(ticketId, eventId, socialEvent, "A1", new Money(150, "PLN"));

        // Act
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Tickets.Add(ticket);
            await dbContext.SaveChangesAsync();
        }

        // Assert
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var saved = await dbContext.Tickets.SingleAsync(t => t.Id == ticketId);
            Assert.Equal(ticket, saved);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_ForValidInput_SavesVerificationCodeWithoutLoss()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddSingleton<IDomainEventsDispatcher>(
            Mock.Of<IDomainEventsDispatcher>());

        var serviceProvider = services.BuildServiceProvider();

        var code = VerificationCode.Generate(UserId.CreateUnique(), "test@test.com", "123456", DateTime.Now);

        // Act
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.VerificationCodes.Add(code);
            await dbContext.SaveChangesAsync();
        }

        // Assert
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var saved = await dbContext.VerificationCodes.SingleAsync(c => c.Id == code.Id);
            Assert.Equal(code, saved);
        }
    }
}
