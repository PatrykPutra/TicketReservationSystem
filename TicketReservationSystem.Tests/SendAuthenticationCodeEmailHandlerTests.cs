using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.DomainEventHandlers;
using TicketReservationSystem.Domain.Events;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Tests;

public class SendAuthenticationCodeEmailHandlerTests
{
    [Fact]
    public async Task SendAuthenticationCodeEmail_ForCodeGeneratedEvent_SendsCodeEmail()
    {
        // Arrange
        var emailAddress = "user@test.com";
        var authenticationCode = "123456";
        var emailSender = new Mock<IEmailSender>();
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new SendAuthenticationCodeEmailHandler(emailSender.Object, NullLogger<SendAuthenticationCodeEmailHandler>.Instance);
        var domainEvent = new AuthenticationCodeGeneratedEvent(
            UserId.CreateUnique(),
            emailAddress,
            authenticationCode,
            DateTime.UtcNow.AddMinutes(5));

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        emailSender.Verify(
            s => s.SendAsync(
                emailAddress,
                "Your authentication code",
                It.Is<string>(b => b.Contains(authenticationCode) && b.Contains("expires in 5 minutes")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAuthenticationCodeEmail_WhenSenderThrows_HandlesException()
    {
        // Arrange
        var emailSender = new Mock<IEmailSender>();
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP down"));

        var handler = new SendAuthenticationCodeEmailHandler(emailSender.Object, NullLogger<SendAuthenticationCodeEmailHandler>.Instance);
        var domainEvent = new AuthenticationCodeGeneratedEvent(
            UserId.CreateUnique(),
            "user@test.com",
            "123456",
            DateTime.UtcNow.AddMinutes(5));

        // Act
        var exception = await Record.ExceptionAsync(() => handler.Handle(domainEvent, CancellationToken.None));

        // Assert
        Assert.Null(exception); 


    }
}