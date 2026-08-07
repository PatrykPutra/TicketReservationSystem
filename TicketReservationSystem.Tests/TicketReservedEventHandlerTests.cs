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

public class TicketReservedEventHandlerTests
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

    private static (SocialEventId EventId, TicketId TicketId, UserId UserId) SeedReservedTicket(ServiceProvider provider, string email = "user@test.com")
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

        var user = new User(userId);
        user.Register(email, "Test", "User", "123456789");

        uow.Events.Add(socialEvent);
        uow.Tickets.Add(ticket);
        uow.Users.Add(user);
        uow.SaveChangesAsync().GetAwaiter().GetResult();

        return (eventId, ticketId, userId);
    }

    private static TicketReservedEventHandler CreateHandler(ServiceProvider provider, out Mock<IEmailSender> emailSender)
    {
        emailSender = new Mock<IEmailSender>();
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new TicketReservedEventHandler(
            provider.CreateScope().ServiceProvider.GetRequiredService<IUnitOfWork>(),
            emailSender.Object,
            NullLogger<TicketReservedEventHandler>.Instance);
    }

    [Fact]
    public async Task TicketReserved_ForResolvedData_SendsEmail()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);
        var (eventId, ticketId, userId) = SeedReservedTicket(serviceProvider);
        var handler = CreateHandler(serviceProvider, out var emailSender);

        var domainEvent = new TicketReservedEvent(ticketId, userId, eventId);

        await handler.Handle(domainEvent, CancellationToken.None);

        emailSender.Verify(
            s => s.SendAsync(
                "user@test.com",
                "Ticket reserved",
                It.Is<string>(b => b.Contains("Test Event") && b.Contains("A1")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TicketReserved_WhenUserMissing_DoesNothing()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);
        var (eventId, ticketId, _) = SeedReservedTicket(serviceProvider);
        var handler = CreateHandler(serviceProvider, out var emailSender);

        var domainEvent = new TicketReservedEvent(ticketId, null, eventId);

        await handler.Handle(domainEvent, CancellationToken.None);

        emailSender.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TicketReserved_WhenTicketMissing_DoesNothing()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);
        var userId = UserId.CreateUnique();
        var handler = CreateHandler(serviceProvider, out var emailSender);

        var domainEvent = new TicketReservedEvent(TicketId.CreateUnique(), userId, SocialEventId.CreateUnique());

        await handler.Handle(domainEvent, CancellationToken.None);

        emailSender.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TicketReserved_WhenSenderThrows_SwallowsException()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);
        var (eventId, ticketId, userId) = SeedReservedTicket(serviceProvider);

        var emailSender = new Mock<IEmailSender>();
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP down"));

        var handler = new TicketReservedEventHandler(
            serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IUnitOfWork>(),
            emailSender.Object,
            NullLogger<TicketReservedEventHandler>.Instance);

        var domainEvent = new TicketReservedEvent(ticketId, userId, eventId);

        await handler.Handle(domainEvent, CancellationToken.None);
    }
}
