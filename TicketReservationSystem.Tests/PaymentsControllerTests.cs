using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TicketReservationSystem.API.Controllers;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands.Payments;
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
}
