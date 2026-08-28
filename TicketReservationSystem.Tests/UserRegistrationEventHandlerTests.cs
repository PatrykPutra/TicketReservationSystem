using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.DomainEventHandlers;
using TicketReservationSystem.Domain.Events;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Tests;

public class UserRegistrationEventHandlerTests
{
    [Fact]
    public async Task UserRegistered_ForNewUser_SendsWelcomeEmail()
    {
        // Arrange
        var emailSenderMock = new Mock<IEmailSender>();
        emailSenderMock
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new UserRegistrationEventHandler(
            emailSenderMock.Object,
            NullLogger<UserRegistrationEventHandler>.Instance);

        var userId = UserId.CreateUnique();
        var domainEvent = new UserRegisteredEvent(userId, "user@test.com");

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        emailSenderMock.Verify(
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
        // Arrange
        var emailSenderMock = new Mock<IEmailSender>();
        emailSenderMock
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new UserRegistrationEventHandler(
            emailSenderMock.Object,
            NullLogger<UserRegistrationEventHandler>.Instance);
        var domainEvent = new UserRegisteredEvent(UserId.CreateUnique(), "user@test.com");

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        emailSenderMock.Verify(
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
        // Arrange
        var emailSenderMock = new Mock<IEmailSender>();
        emailSenderMock
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new UserRegistrationEventHandler(
            emailSenderMock.Object,
            NullLogger<UserRegistrationEventHandler>.Instance);

        var domainEvent = new UserRegisteredEvent(UserId.CreateUnique(), "user@test.com");

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        emailSenderMock.Verify(
            s => s.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(b => b.Contains("Thank you for registering") && b.Contains("TicketReservationSystem")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UserRegistered_WhenSenderThrows_HandlesException()
    {
        // Arrange
        var emailSenderMock = new Mock<IEmailSender>();
        emailSenderMock
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP down"));

        var handler = new UserRegistrationEventHandler(
            emailSenderMock.Object,
            NullLogger<UserRegistrationEventHandler>.Instance);
        
        var domainEvent = new UserRegisteredEvent(UserId.CreateUnique(), "user@test.com");

        // Act
        var exception = await Record.ExceptionAsync(() => handler.Handle(domainEvent, CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }
}
