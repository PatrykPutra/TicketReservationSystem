using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TicketReservationSystem.API.Controllers;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands.Authentication;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Application.Requests;

namespace TicketReservationSystem.Tests;

public class AuthenticationControllerTests
{
    private static AuthenticationController CreateController(Action<Mock<ICommandDispatcher>>? commandSetup = null)
    {
        var commandDispatcherMock = new Mock<ICommandDispatcher>();
        commandSetup?.Invoke(commandDispatcherMock);

        var controller = new AuthenticationController(commandDispatcherMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return controller;
    }

    private record UnexpectedError : Error
    {
        public UnexpectedError(string message) : base("Unexpected", message)
        {
        }
    }

    [Fact]
    public async Task SendCode_ForExistingUser_ReturnsOk()
    {
        // Arrange
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<SendAuthenticationCodeCommand, SendAuthenticationCodeResult>(
                    It.IsAny<SendAuthenticationCodeCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SendAuthenticationCodeResult.Success());
        });

        // Act
        var result = await controller.SendCode(new AuthenticationCodeRequest { Email = "user@test.com" });

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task SendCode_ForNotRegisteredUser_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<SendAuthenticationCodeCommand, SendAuthenticationCodeResult>(
                    It.IsAny<SendAuthenticationCodeCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SendAuthenticationCodeResult(new UserNotFoundError("Unknown")));
        });

        // Act
        var result = await controller.SendCode(new AuthenticationCodeRequest { Email = "missing@test.com" });

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task SendCode_ForRateLimitedError_Returns429()
    {
        // Arrange
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<SendAuthenticationCodeCommand, SendAuthenticationCodeResult>(
                    It.IsAny<SendAuthenticationCodeCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SendAuthenticationCodeResult(new RateLimitedError("Slow down")));
        });

        // Act
        var result = await controller.SendCode(new AuthenticationCodeRequest { Email = "user@test.com" });

        // Assert
        var statusCode = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(429, statusCode.StatusCode);
    }

    [Fact]
    public async Task SendCode_ForUnexpectedError_Returns500()
    {
        // Arrange
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<SendAuthenticationCodeCommand, SendAuthenticationCodeResult>(
                    It.IsAny<SendAuthenticationCodeCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SendAuthenticationCodeResult(new UnexpectedError("Unexpected")));
        });

        // Act
        var result = await controller.SendCode(new AuthenticationCodeRequest { Email = "user@test.com" });

        // Assert
        var statusCode = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, statusCode.StatusCode);
    }

    [Fact]
    public async Task Token_ForValidCode_ReturnsOkWithTokenResponse()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<GenerateTokenCommand, GenerateTokenResult>(
                    It.IsAny<GenerateTokenCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenerateTokenResult.Success("jwt-token", expiresAt));
        });

        // Act
        var result = await controller.Token(new AuthenticationTokenRequest
        {
            Email = "user@test.com",
            Code = "123456"
        });

        // Assert
        var response = Assert.IsType<OkObjectResult>(result);
        var responseValue = Assert.IsType<TokenResponse>(response.Value);
        Assert.Equal("jwt-token", responseValue.Token);
        Assert.Equal(expiresAt, responseValue.ExpiresAt);
    }

    [Fact]
    public async Task Token_ForInvalidCode_ReturnsUnauthorized()
    {
        // Arrange
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<GenerateTokenCommand, GenerateTokenResult>(
                    It.IsAny<GenerateTokenCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GenerateTokenResult(new InvalidCredentialsError("Bad code")));
        });

        // Act
        var result = await controller.Token(new AuthenticationTokenRequest
        {
            Email = "user@test.com",
            Code = "000000"
        });

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void SendCode_Documentation_ContainsStatus200OK()
    {
        // Assert
        Assert.Contains(StatusCodes.Status200OK, GetDocumentedCodes(nameof(AuthenticationController.SendCode)));
    }

    [Fact]
    public void SendCode_Documentation_ContainsStatus404NotFound()
    {
        // Assert
        Assert.Contains(StatusCodes.Status404NotFound, GetDocumentedCodes(nameof(AuthenticationController.SendCode)));
    }

    [Fact]
    public void SendCode_Documentation_ContainsStatus429TooManyRequests()
    {
        // Assert
        Assert.Contains(StatusCodes.Status429TooManyRequests, GetDocumentedCodes(nameof(AuthenticationController.SendCode)));
    }

    [Fact]
    public void SendCode_Documentation_ContainsStatus500InternalServerError()
    {
        // Assert
        Assert.Contains(StatusCodes.Status500InternalServerError, GetDocumentedCodes(nameof(AuthenticationController.SendCode)));
    }

    [Fact]
    public void Token_Documentation_ContainsStatus200OK()
    {
        // Assert
        Assert.Contains(StatusCodes.Status200OK, GetDocumentedCodes(nameof(AuthenticationController.Token)));
    }

    [Fact]
    public void Token_Documentation_ContainsStatus401Unauthorized()
    {
        // Assert
        Assert.Contains(StatusCodes.Status401Unauthorized, GetDocumentedCodes(nameof(AuthenticationController.Token)));
    }

    [Fact]
    public void Token_Documentation_ContainsStatus500InternalServerError()
    {
        // Assert
        Assert.Contains(StatusCodes.Status500InternalServerError, GetDocumentedCodes(nameof(AuthenticationController.Token)));
    }

    private static int[] GetDocumentedCodes(string methodName) =>
        typeof(AuthenticationController).GetMethod(methodName)!
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Select(a => a.StatusCode)
            .ToArray();
}
