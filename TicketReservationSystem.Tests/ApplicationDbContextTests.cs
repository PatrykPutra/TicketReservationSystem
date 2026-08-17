using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.ValueObjects;
using TicketReservationSystem.Infrastructure.Persistence;

namespace TicketReservationSystem.Tests;

public class ApplicationDbContextTests
{
    private static ServiceProvider CreateServiceProvider(string dbName)
    {
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddSingleton<Infrastructure.DomainEventsDispatcher.IDomainEventsDispatcher>(
            Mock.Of<Infrastructure.DomainEventsDispatcher.IDomainEventsDispatcher>());

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SaveChangesAsync_PaymentMoney_RoundTripsLosslessly()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);

        var paymentId = PaymentId.CreateUnique();
        var amount = new Money(150, "PLN");

        using (var scope = serviceProvider.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var payment = new Payment(paymentId, TicketId.CreateUnique(), UserId.CreateUnique(), amount, PaymentProvider.Stripe);
            ctx.Payments.Add(payment);
            await ctx.SaveChangesAsync();
        }

        using (var scope = serviceProvider.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var saved = await ctx.Payments.SingleAsync(p => p.Id == paymentId);

            Assert.Equal(amount, saved.Amount);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_EventTimeRange_RoundTripsLosslessly()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);

        var eventId = SocialEventId.CreateUnique();
        var timeRange = new DateTimeRange(
            new DateTime(2027, 1, 15, 19, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 15, 23, 0, 0, DateTimeKind.Utc));

        using (var scope = serviceProvider.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var socialEvent = new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, new Money(150, "PLN"));
            ctx.SocialEvents.Add(socialEvent);
            await ctx.SaveChangesAsync();
        }

        using (var scope = serviceProvider.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var saved = await ctx.SocialEvents.SingleAsync(e => e.Id == eventId);

            Assert.Equal(timeRange, saved.TimeRange);
            Assert.Equal(DateTimeKind.Utc, saved.TimeRange.StartTime.Kind);
            Assert.Equal(DateTimeKind.Utc, saved.TimeRange.EndTime.Kind);
        }
    }
}
