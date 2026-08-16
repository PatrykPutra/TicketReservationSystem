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

    [Fact]
    public async Task SendCode_ForExistingUser_ReturnsOk()
    {
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<SendAuthenticationCodeCommand, SendAuthenticationCodeResult>(
                    It.IsAny<SendAuthenticationCodeCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SendAuthenticationCodeResult.Success());
        });

        var result = await controller.SendCode(new AuthenticationCodeRequest { Email = "user@test.com" });

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task SendCode_WhenUserNotFound_ReturnsNotFound()
    {
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<SendAuthenticationCodeCommand, SendAuthenticationCodeResult>(
                    It.IsAny<SendAuthenticationCodeCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SendAuthenticationCodeResult(new UserNotFoundError("Unknown")));
        });

        var result = await controller.SendCode(new AuthenticationCodeRequest { Email = "missing@test.com" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task SendCode_WhenRateLimited_Returns429()
    {
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<SendAuthenticationCodeCommand, SendAuthenticationCodeResult>(
                    It.IsAny<SendAuthenticationCodeCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SendAuthenticationCodeResult(new RateLimitedError("Slow down")));
        });

        var result = await controller.SendCode(new AuthenticationCodeRequest { Email = "user@test.com" });

        var statusCode = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(429, statusCode.StatusCode);
    }

    [Fact]
    public async Task SendCode_WhenUnexpectedError_Returns500()
    {
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<SendAuthenticationCodeCommand, SendAuthenticationCodeResult>(
                    It.IsAny<SendAuthenticationCodeCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SendAuthenticationCodeResult(new InvalidCredentialsError("Unexpected")));
        });

        var result = await controller.SendCode(new AuthenticationCodeRequest { Email = "user@test.com" });

        var statusCode = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, statusCode.StatusCode);
    }

    [Fact]
    public async Task Token_ForValidCode_ReturnsOkWithTokenAndExpiry()
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<GenerateTokenCommand, GenerateTokenResult>(
                    It.IsAny<GenerateTokenCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenerateTokenResult.Success("jwt-token", expiresAt));
        });

        var result = await controller.Token(new AuthenticationTokenRequest
        {
            Email = "user@test.com",
            Code = "123456"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var value = ok.Value!;
        var token = value.GetType().GetProperty("token")!.GetValue(value);
        var expiresAtValue = value.GetType().GetProperty("expiresAt")!.GetValue(value);

        Assert.Equal("jwt-token", (string)token!);
        Assert.Equal(expiresAt, (DateTime)expiresAtValue!);
    }

    [Fact]
    public async Task Token_ForInvalidCode_ReturnsUnauthorized()
    {
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<GenerateTokenCommand, GenerateTokenResult>(
                    It.IsAny<GenerateTokenCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GenerateTokenResult(new InvalidCredentialsError("Bad code")));
        });

        var result = await controller.Token(new AuthenticationTokenRequest
        {
            Email = "user@test.com",
            Code = "000000"
        });

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void SendCode_Documentation_ContainsStatus200OK()
    {
        Assert.Contains(StatusCodes.Status200OK, GetDocumentedCodes(nameof(AuthenticationController.SendCode)));
    }

    [Fact]
    public void SendCode_Documentation_ContainsStatus404NotFound()
    {
        Assert.Contains(StatusCodes.Status404NotFound, GetDocumentedCodes(nameof(AuthenticationController.SendCode)));
    }

    [Fact]
    public void SendCode_Documentation_ContainsStatus429TooManyRequests()
    {
        Assert.Contains(StatusCodes.Status429TooManyRequests, GetDocumentedCodes(nameof(AuthenticationController.SendCode)));
    }

    [Fact]
    public void SendCode_Documentation_ContainsStatus500InternalServerError()
    {
        Assert.Contains(StatusCodes.Status500InternalServerError, GetDocumentedCodes(nameof(AuthenticationController.SendCode)));
    }

    [Fact]
    public void Token_Documentation_ContainsStatus200OK()
    {
        Assert.Contains(StatusCodes.Status200OK, GetDocumentedCodes(nameof(AuthenticationController.Token)));
    }

    [Fact]
    public void Token_Documentation_ContainsStatus401Unauthorized()
    {
        Assert.Contains(StatusCodes.Status401Unauthorized, GetDocumentedCodes(nameof(AuthenticationController.Token)));
    }

    [Fact]
    public void Token_Documentation_ContainsStatus500InternalServerError()
    {
        Assert.Contains(StatusCodes.Status500InternalServerError, GetDocumentedCodes(nameof(AuthenticationController.Token)));
    }

    private static int[] GetDocumentedCodes(string methodName) =>
        typeof(AuthenticationController).GetMethod(methodName)!
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Select(a => a.StatusCode)
            .ToArray();
}
