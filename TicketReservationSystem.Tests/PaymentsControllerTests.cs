using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TicketReservationSystem.API.Controllers;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands.Payments;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Application.Requests;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Tests;

public class PaymentsControllerTests
{
    private static readonly Guid UserIdValue = Guid.NewGuid();

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

    private static void SetAuthenticatedUser(ControllerBase controller, Guid? userId = null)
    {
        var claims = userId is null
            ? Array.Empty<Claim>()
            : new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task CreateCheckout_WhenClaimMissing_ReturnsUnauthorized()
    {
        var controller = CreateController();
        SetAuthenticatedUser(controller);

        var result = await controller.CreateCheckout(new PaymentCheckoutRequest
        {
            TicketId = Guid.NewGuid(),
            UserId = UserIdValue
        });

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task CreateCheckout_WhenClaimDoesNotMatchUserId_ReturnsUnauthorized()
    {
        var controller = CreateController();
        SetAuthenticatedUser(controller, Guid.NewGuid());

        var result = await controller.CreateCheckout(new PaymentCheckoutRequest
        {
            TicketId = Guid.NewGuid(),
            UserId = UserIdValue
        });

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task CreateCheckout_WhenAllMatch_ReturnsOk()
    {
        var paymentId = PaymentId.CreateUnique();
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<CreateCheckoutCommand, CreateCheckoutResult>(It.IsAny<CreateCheckoutCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateCheckoutResult.Success("https://checkout.example.com", "cs_test_123", paymentId));
        });
        SetAuthenticatedUser(controller, UserIdValue);

        var result = await controller.CreateCheckout(new PaymentCheckoutRequest
        {
            TicketId = Guid.NewGuid(),
            UserId = UserIdValue
        });

        Assert.IsType<OkObjectResult>(result);
    }

    private static async Task<IActionResult> CreateCheckoutWithError(Error error)
    {
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<CreateCheckoutCommand, CreateCheckoutResult>(It.IsAny<CreateCheckoutCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateCheckoutResult(error));
        });
        SetAuthenticatedUser(controller, UserIdValue);

        return await controller.CreateCheckout(new PaymentCheckoutRequest
        {
            TicketId = Guid.NewGuid(),
            UserId = UserIdValue
        });
    }

    [Fact]
    public async Task CreateCheckout_WhenTicketNotReserved_ReturnsConflict()
    {
        var result = await CreateCheckoutWithError(new TicketNotReservedError("Not reserved"));
        Assert.IsType<ConflictResult>(result);
    }

    [Fact]
    public async Task CreateCheckout_WhenTicketNotAvailable_ReturnsConflict()
    {
        var result = await CreateCheckoutWithError(new TicketNotAvailableError("Not available"));
        Assert.IsType<ConflictResult>(result);
    }

    [Fact]
    public async Task CreateCheckout_WhenDuplicatePayment_ReturnsConflict()
    {
        var result = await CreateCheckoutWithError(new DuplicatePaymentError("Duplicate"));
        Assert.IsType<ConflictResult>(result);
    }

    [Fact]
    public async Task CreateCheckout_WhenUnauthorizedUserError_ReturnsUnauthorized()
    {
        var result = await CreateCheckoutWithError(new UnauthorizedUserError("Forbidden"));
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task CreateCheckout_WhenNotFoundError_ReturnsNotFound()
    {
        var result = await CreateCheckoutWithError(new NotFoundError("Missing"));
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreateCheckout_WhenCurrencyMismatch_ReturnsBadRequest()
    {
        var result = await CreateCheckoutWithError(new CurrencyMismatchError("Mismatch"));
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task CreateCheckout_WhenUnsupportedCurrency_ReturnsBadRequest()
    {
        var result = await CreateCheckoutWithError(new UnsupportedCurrencyError("Unsupported"));
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task CreateCheckout_WhenUnexpectedError_Returns500()
    {
        var result = await CreateCheckoutWithError(new InvalidCredentialsError("Unexpected"));
        var statusCode = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, statusCode.StatusCode);
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
