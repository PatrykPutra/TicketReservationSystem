using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.DomainEventHandlers;
using TicketReservationSystem.Domain.Events;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Tests;

public class UserRegistrationEventHandlerTests
{
    private static UserRegistrationEventHandler CreateHandler(out Mock<IEmailSender> emailSender)
    {
        emailSender = new Mock<IEmailSender>();
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new UserRegistrationEventHandler(
            emailSender.Object,
            NullLogger<UserRegistrationEventHandler>.Instance);
    }

    [Fact]
    public async Task UserRegistered_ForNewUser_SendsWelcomeEmail()
    {
        var handler = CreateHandler(out var emailSender);
        var userId = UserId.CreateUnique();
        var domainEvent = new UserRegisteredEvent(userId, "user@test.com");

        await handler.Handle(domainEvent, CancellationToken.None);

        emailSender.Verify(
            s => s.SendAsync(
                "user@test.com",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UserRegistered_ForNewUser_SendsWelcomeEmailWithExpectedSubject()
    {
        var handler = CreateHandler(out var emailSender);
        var domainEvent = new UserRegisteredEvent(UserId.CreateUnique(), "user@test.com");

        await handler.Handle(domainEvent, CancellationToken.None);

        emailSender.Verify(
            s => s.SendAsync(
                It.IsAny<string>(),
                "Welcome to TicketReservationSystem",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UserRegistered_ForNewUser_SendsWelcomeEmailWithWelcomeBody()
    {
        var handler = CreateHandler(out var emailSender);
        var domainEvent = new UserRegisteredEvent(UserId.CreateUnique(), "user@test.com");

        await handler.Handle(domainEvent, CancellationToken.None);

        emailSender.Verify(
            s => s.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(b => b.Contains("Thank you for registering") && b.Contains("TicketReservationSystem")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UserRegistered_WhenSenderThrows_SwallowsException()
    {
        var emailSender = new Mock<IEmailSender>();
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP down"));

        var handler = new UserRegistrationEventHandler(emailSender.Object, NullLogger<UserRegistrationEventHandler>.Instance);
        var domainEvent = new UserRegisteredEvent(UserId.CreateUnique(), "user@test.com");

        await handler.Handle(domainEvent, CancellationToken.None);
    }
}
