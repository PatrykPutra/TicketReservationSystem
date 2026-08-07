using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TicketReservationSystem.API.Controllers;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands.Tickets;
using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Application.Queries.Tickets;
using TicketReservationSystem.Application.Requests;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Tests;

public class TicketsControllerTests
{
    private static readonly Guid UserIdValue = Guid.NewGuid();
    private static readonly Guid TicketIdValue = Guid.NewGuid();

    private static TicketsController CreateController(
        Action<Mock<IQueryDispatcher>>? querySetup = null,
        Action<Mock<ICommandDispatcher>>? commandSetup = null)
    {
        var queryDispatcherMock = new Mock<IQueryDispatcher>();
        querySetup?.Invoke(queryDispatcherMock);

        var commandDispatcherMock = new Mock<ICommandDispatcher>();
        commandSetup?.Invoke(commandDispatcherMock);

        var controller = new TicketsController(queryDispatcherMock.Object, commandDispatcherMock.Object)
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

    private static void SetRawClaim(ControllerBase controller, string claimValue)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, claimValue) }, "TestAuth");
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
    }

    [Fact]
    public void GetTicketById_HasAllowAnonymousAttribute_ReturnsTrue()
    {
        var attribute = typeof(TicketsController)
            .GetMethod(nameof(TicketsController.GetTicketById))
            ?.GetCustomAttribute<AllowAnonymousAttribute>();

        Assert.NotNull(attribute);
    }

    [Fact]
    public void GetTicketByEvent_HasAllowAnonymousAttribute_ReturnsTrue()
    {
        var attribute = typeof(TicketsController)
            .GetMethod(nameof(TicketsController.GetTicketByEvent))
            ?.GetCustomAttribute<AllowAnonymousAttribute>();

        Assert.NotNull(attribute);
    }

    [Fact]
    public void Reserve_HasNoAllowAnonymousAttribute_ReturnsTrue()
    {
        var attribute = typeof(TicketsController)
            .GetMethod(nameof(TicketsController.Reserve))
            ?.GetCustomAttribute<AllowAnonymousAttribute>();

        Assert.Null(attribute);
    }

    [Fact]
    public void Cancel_HasNoAllowAnonymousAttribute_ReturnsTrue()
    {
        var attribute = typeof(TicketsController)
            .GetMethod(nameof(TicketsController.Cancel))
            ?.GetCustomAttribute<AllowAnonymousAttribute>();

        Assert.Null(attribute);
    }

    [Fact]
    public async Task GetTicketById_WhenTicketFound_ReturnsOk()
    {
        var ticketDto = new TicketDto(
            TicketId.Create(TicketIdValue),
            SocialEventId.CreateUnique(),
            "A1",
            TicketStatus.Available,
            null,
            150m,
            "PLN");
        var controller = CreateController(querySetup: mock =>
        {
            mock.Setup(d => d.ExecuteAsync<GetTicketByIdQuery, GetTicketByIdResult>(
                    It.IsAny<GetTicketByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetTicketByIdResult(ticketDto));
        });

        var result = await controller.GetTicketById(TicketIdValue);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(ticketDto, ok.Value);
    }

    [Fact]
    public async Task GetTicketById_WhenTicketMissing_ReturnsNotFound()
    {
        var controller = CreateController(querySetup: mock =>
        {
            mock.Setup(d => d.ExecuteAsync<GetTicketByIdQuery, GetTicketByIdResult>(
                    It.IsAny<GetTicketByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetTicketByIdResult(null));
        });

        var result = await controller.GetTicketById(TicketIdValue);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetTicketByEvent_ReturnsOkWithTickets()
    {
        var tickets = new List<TicketDto>
        {
            new(TicketId.CreateUnique(), SocialEventId.CreateUnique(), "A1", TicketStatus.Available, null, 150m, "PLN")
        };
        var controller = CreateController(querySetup: mock =>
        {
            mock.Setup(d => d.ExecuteAsync<GetTicketsByEventQuery, GetTicketsByEventResult>(
                    It.IsAny<GetTicketsByEventQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetTicketsByEventResult(tickets));
        });

        var result = await controller.GetTicketByEvent(Guid.NewGuid());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(tickets, ok.Value);
    }

    [Fact]
    public async Task Reserve_WhenTicketIdDoesNotMatchPath_ReturnsBadRequest()
    {
        var controller = CreateController();
        SetAuthenticatedUser(controller, UserIdValue);

        var result = await controller.Reserve(Guid.NewGuid(), new TicketReservationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Reserve_WhenClaimMissing_ReturnsUnauthorized()
    {
        var controller = CreateController();
        SetAuthenticatedUser(controller);

        var result = await controller.Reserve(TicketIdValue, new TicketReservationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Reserve_WhenClaimMalformed_ReturnsUnauthorized()
    {
        var controller = CreateController();
        SetRawClaim(controller, "not-a-guid");

        var result = await controller.Reserve(TicketIdValue, new TicketReservationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Reserve_WhenClaimDoesNotMatchUserId_ReturnsUnauthorized()
    {
        var controller = CreateController();
        SetAuthenticatedUser(controller, Guid.NewGuid());

        var result = await controller.Reserve(TicketIdValue, new TicketReservationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Reserve_WhenAllMatch_ReturnsOk()
    {
        var ticketId = TicketId.Create(TicketIdValue);
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<TicketReservationCommand, TicketReservationResult>(It.IsAny<TicketReservationCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TicketReservationResult.Success(ticketId, TicketStatus.Reserved, DateTime.UtcNow));
        });
        SetAuthenticatedUser(controller, UserIdValue);

        var result = await controller.Reserve(TicketIdValue, new TicketReservationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Cancel_WhenTicketIdDoesNotMatchPath_ReturnsBadRequest()
    {
        var controller = CreateController();
        SetAuthenticatedUser(controller, UserIdValue);

        var result = await controller.Cancel(Guid.NewGuid(), new TicketCancelationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Cancel_WhenClaimDoesNotMatchUserId_ReturnsUnauthorized()
    {
        var controller = CreateController();
        SetAuthenticatedUser(controller, Guid.NewGuid());

        var result = await controller.Cancel(TicketIdValue, new TicketCancelationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Cancel_WhenClaimMissing_ReturnsUnauthorized()
    {
        var controller = CreateController();
        SetAuthenticatedUser(controller);

        var result = await controller.Cancel(TicketIdValue, new TicketCancelationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Cancel_WhenClaimMalformed_ReturnsUnauthorized()
    {
        var controller = CreateController();
        SetRawClaim(controller, "not-a-guid");

        var result = await controller.Cancel(TicketIdValue, new TicketCancelationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Cancel_WhenAllMatch_ReturnsOk()
    {
        var ticketId = TicketId.Create(TicketIdValue);
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<TicketCancelationCommand, TicketCancelationResult>(It.IsAny<TicketCancelationCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TicketCancelationResult.Success(ticketId, TicketStatus.Available));
        });
        SetAuthenticatedUser(controller, UserIdValue);

        var result = await controller.Cancel(TicketIdValue, new TicketCancelationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Reserve_WhenTicketNotAvailable_ReturnsConflict()
    {
        var result = await ReserveWithError(new TicketNotAvailableError("Not available"));

        Assert.IsType<ConflictResult>(result);
    }

    [Fact]
    public async Task Reserve_WhenUnauthorizedUserError_ReturnsUnauthorized()
    {
        var result = await ReserveWithError(new UnauthorizedUserError("Forbidden"));

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Reserve_WhenNotFoundError_ReturnsNotFound()
    {
        var result = await ReserveWithError(new NotFoundError("Missing"));

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Reserve_WhenUnexpectedError_Returns500()
    {
        var result = await ReserveWithError(new InvalidCredentialsError("Unexpected"));

        var statusCode = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, statusCode.StatusCode);
    }

    private static async Task<IActionResult> ReserveWithError(Error error)
    {
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<TicketReservationCommand, TicketReservationResult>(It.IsAny<TicketReservationCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TicketReservationResult(error));
        });
        SetAuthenticatedUser(controller, UserIdValue);

        return await controller.Reserve(TicketIdValue, new TicketReservationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });
    }
}
