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
    public async Task SaveAndRead_PaymentMoney_round_trips_losslessly()
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
    public void Money_converter_stores_amount_and_currency_as_JSON()
    {
        using var scope = CreateServiceProvider(Guid.NewGuid().ToString()).CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var converter = ctx.Model.FindEntityType(typeof(Payment))!
            .FindProperty(nameof(Payment.Amount))!
            .GetValueConverter();

        var stored = (string)converter!.ConvertToProvider(new Money(150, "PLN"))!;

        Assert.StartsWith("{", stored);
        Assert.Contains("\"amount\":", stored);
        Assert.Contains("\"currency\":", stored);

        var readBack = (Money)converter!.ConvertFromProvider("{\"Amount\":150,\"Currency\":\"PLN\"}")!;
        Assert.Equal(new Money(150, "PLN"), readBack);
    }

    [Fact]
    public async Task SaveAndRead_EventTimeRange_round_trips_losslessly()
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

    [Fact]
    public void TimeRange_converter_stores_start_and_end_as_JSON()
    {
        using var scope = CreateServiceProvider(Guid.NewGuid().ToString()).CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var converter = ctx.Model.FindEntityType(typeof(SocialEvent))!
            .FindProperty(nameof(SocialEvent.TimeRange))!
            .GetValueConverter();

        var timeRange = new DateTimeRange(
            new DateTime(2027, 1, 15, 19, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 15, 23, 0, 0, DateTimeKind.Utc));
        var stored = (string)converter!.ConvertToProvider(timeRange)!;

        Assert.StartsWith("{", stored);
        Assert.Contains("\"startTime\":", stored);
        Assert.Contains("\"endTime\":", stored);

        var readBack = (DateTimeRange)converter!.ConvertFromProvider(
            "{\"StartTime\":\"2027-01-15T19:00:00Z\",\"EndTime\":\"2027-01-15T23:00:00Z\"}")!;
        Assert.Equal(timeRange, readBack);
    }
}
