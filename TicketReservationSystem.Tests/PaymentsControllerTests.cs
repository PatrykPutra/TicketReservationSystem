using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Reflection;
using System.Security.Claims;
using TicketReservationSystem.API.Controllers;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands.Payments;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Application.Requests;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Tests;

public class PaymentsControllerTests
{
    private sealed record UnexpectedError(string Description) : Error("Unexpected", Description);
    public static TheoryData<Error, int> ErrorResponseCases => new()
    {
        { new TicketNotReservedError("Not reserved"), 409 },
        { new TicketNotAvailableError("Not available"), 409 },
        { new DuplicatePaymentError("Duplicate"), 409 },
        { new UnauthorizedUserError("Forbidden"), 401 },
        { new NotFoundError("Missing"), 404 },
        { new CurrencyMismatchError("Mismatch"), 400 },
        { new UnsupportedCurrencyError("Unsupported"), 400},
        { new UnexpectedError("Unexpected"), 500 }

    };

    private static PaymentsController CreateController(Action<Mock<ICommandDispatcher>>? commandSetup = null)
    {
        var commandDispatcherMock = new Mock<ICommandDispatcher>();
        commandSetup?.Invoke(commandDispatcherMock);

        var controller = new PaymentsController(commandDispatcherMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return controller;
    }

    [Fact]
    public async Task CreateCheckout_WhenIdClaimMissing_ReturnsUnauthorized()
    {
        // Arrange
        Guid UserIdValue = Guid.NewGuid();
        var controller = CreateController();
        var claims = Array.Empty<Claim>();
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = await controller.CreateCheckout(new PaymentCheckoutRequest
        {
            TicketId = Guid.NewGuid(),
            UserId = UserIdValue
        });

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task CreateCheckout_WhenIdClaimDoesNotMatchUserId_ReturnsUnauthorized()
    {
        // Arrange
        var controller = CreateController();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = await controller.CreateCheckout(new PaymentCheckoutRequest
        {
            TicketId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        });

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task CreateCheckout_ForCorrectRequest_ReturnsOk()
    {
        // Arrange
        Guid userIdValue = Guid.NewGuid();
        var paymentId = PaymentId.CreateUnique();
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<CreateCheckoutCommand, CreateCheckoutResult>(It.IsAny<CreateCheckoutCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateCheckoutResult.Success("https://checkout.example.com", "cs_test_123", paymentId));
        });

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userIdValue.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = await controller.CreateCheckout(new PaymentCheckoutRequest
        {
            TicketId = Guid.NewGuid(),
            UserId = userIdValue
        });

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    private static async Task<IActionResult> CreateFailedCheckoutResult(Error error)
    {
        Guid userIdValue = Guid.NewGuid();
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<CreateCheckoutCommand, CreateCheckoutResult>(It.IsAny<CreateCheckoutCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateCheckoutResult(error));
        });

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userIdValue.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

        return await controller.CreateCheckout(new PaymentCheckoutRequest
        {
            TicketId = Guid.NewGuid(),
            UserId = userIdValue
        });
    }

    [Theory]
    [MemberData(nameof(ErrorResponseCases))]
    public async Task CreateCheckout_ForErrorResult_ReturnsProperResponseCode(Error error, int expectedStatusCode)
    {
        // Arrange
        Guid userIdValue = Guid.NewGuid();
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<CreateCheckoutCommand, CreateCheckoutResult>(It.IsAny<CreateCheckoutCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateCheckoutResult(error));
        });

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userIdValue.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = await controller.CreateCheckout(new PaymentCheckoutRequest
        {
            TicketId = Guid.NewGuid(),
            UserId = userIdValue
        });

        // Assert
        var statusCode = Assert.IsAssignableFrom<StatusCodeResult>(result);
        Assert.Equal(expectedStatusCode, statusCode.StatusCode);
    }

    [Fact]
    public void CreateCheckout_Documentation_ContainsStatus200OK()
    {
        Assert.Contains(StatusCodes.Status200OK, GetDocumentedCodes(nameof(PaymentsController.CreateCheckout)));
    }

    [Fact]
    public void CreateCheckout_Documentation_ContainsStatus400BadRequest()
    {
        Assert.Contains(StatusCodes.Status400BadRequest, GetDocumentedCodes(nameof(PaymentsController.CreateCheckout)));
    }

    [Fact]
    public void CreateCheckout_Documentation_ContainsStatus401Unauthorized()
    {
        Assert.Contains(StatusCodes.Status401Unauthorized, GetDocumentedCodes(nameof(PaymentsController.CreateCheckout)));
    }

    [Fact]
    public void CreateCheckout_Documentation_ContainsStatus404NotFound()
    {
        Assert.Contains(StatusCodes.Status404NotFound, GetDocumentedCodes(nameof(PaymentsController.CreateCheckout)));
    }

    [Fact]
    public void CreateCheckout_Documentation_ContainsStatus409Conflict()
    {
        Assert.Contains(StatusCodes.Status409Conflict, GetDocumentedCodes(nameof(PaymentsController.CreateCheckout)));
    }

    [Fact]
    public void CreateCheckout_Documentation_ContainsStatus500InternalServerError()
    {
        Assert.Contains(StatusCodes.Status500InternalServerError, GetDocumentedCodes(nameof(PaymentsController.CreateCheckout)));
    }

    private static int[] GetDocumentedCodes(string methodName) =>
        typeof(PaymentsController).GetMethod(methodName)!
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Select(a => a.StatusCode)
            .ToArray();
}
