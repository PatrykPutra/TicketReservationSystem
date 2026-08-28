using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Reflection;
using System.Security.Claims;
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

    private sealed record UnexpectedError(string Description) : Error("Unexpected", Description);

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
        // Arrange && Act
        var attribute = typeof(TicketsController)
            .GetMethod(nameof(TicketsController.GetTicketById))
            ?.GetCustomAttribute<AllowAnonymousAttribute>();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void GetTicketByEvent_HasAllowAnonymousAttribute_ReturnsTrue()
    {
        // Arrange && Act
        var attribute = typeof(TicketsController)
            .GetMethod(nameof(TicketsController.GetTicketByEvent))
            ?.GetCustomAttribute<AllowAnonymousAttribute>();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void Reserve_HasNoAllowAnonymousAttribute_ReturnsTrue()
    {
        // Arrange && Act
        var attribute = typeof(TicketsController)
            .GetMethod(nameof(TicketsController.Reserve))
            ?.GetCustomAttribute<AllowAnonymousAttribute>();

        // Assert
        Assert.Null(attribute);
    }

    [Fact]
    public void Cancel_HasNoAllowAnonymousAttribute_ReturnsTrue()
    {
        // Arrange && Act
        var attribute = typeof(TicketsController)
            .GetMethod(nameof(TicketsController.Cancel))
            ?.GetCustomAttribute<AllowAnonymousAttribute>();

        // Assert
        Assert.Null(attribute);
    }

    [Fact]
    public async Task GetTicketById_WhenTicketFound_ReturnsOk()
    {
        // Arrange
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

        // Act
        var result = await controller.GetTicketById(TicketIdValue);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(ticketDto, ok.Value);
    }

    [Fact]
    public async Task GetTicketById_WhenTicketMissing_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController(querySetup: mock =>
        {
            mock.Setup(d => d.ExecuteAsync<GetTicketByIdQuery, GetTicketByIdResult>(
                    It.IsAny<GetTicketByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetTicketByIdResult(null));
        });

        // Act
        var result = await controller.GetTicketById(TicketIdValue);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetTicketByEvent_ForExistingTickets_ReturnsOkWithTickets()
    {
        // Arrange
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

        // Act
        var result = await controller.GetTicketByEvent(Guid.NewGuid());

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(tickets, ok.Value);
    }

    [Fact]
    public async Task Reserve_WhenTicketIdDoesNotMatchPath_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        SetAuthenticatedUser(controller, UserIdValue);

        // Act
        var result = await controller.Reserve(Guid.NewGuid(), new TicketReservationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Reserve_WhenClaimMissing_ReturnsUnauthorized()
    {
        // Arrange
        var controller = CreateController();
        SetAuthenticatedUser(controller);

        // Act
        var result = await controller.Reserve(TicketIdValue, new TicketReservationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Reserve_WhenClaimMalformed_ReturnsUnauthorized()
    {
        // Arrange
        var controller = CreateController();
        SetRawClaim(controller, "not-a-guid");

        // Act
        var result = await controller.Reserve(TicketIdValue, new TicketReservationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Reserve_WhenClaimDoesNotMatchUserId_ReturnsUnauthorized()
    {
        // Arrange
        var controller = CreateController();
        SetAuthenticatedUser(controller, Guid.NewGuid());

        // Act
        var result = await controller.Reserve(TicketIdValue, new TicketReservationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Reserve_ForSuccessResult_ReturnsOk()
    {
        // Arrange
        var ticketId = TicketId.Create(TicketIdValue);
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<TicketReservationCommand, TicketReservationResult>(It.IsAny<TicketReservationCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TicketReservationResult.Success(ticketId, TicketStatus.Reserved, DateTime.UtcNow));
        });
        SetAuthenticatedUser(controller, UserIdValue);

        // Act
        var result = await controller.Reserve(TicketIdValue, new TicketReservationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Cancel_WhenTicketIdDoesNotMatchPath_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        SetAuthenticatedUser(controller, UserIdValue);

        // Act
        var result = await controller.Cancel(Guid.NewGuid(), new TicketCancelationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Cancel_WhenClaimDoesNotMatchUserId_ReturnsUnauthorized()
    {
        // Arrange
        var controller = CreateController();
        SetAuthenticatedUser(controller, Guid.NewGuid());

        // Act
        var result = await controller.Cancel(TicketIdValue, new TicketCancelationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Cancel_WhenIdClaimMissing_ReturnsUnauthorized()
    {
        // Arrange
        var controller = CreateController();
        SetAuthenticatedUser(controller);

        // Act
        var result = await controller.Cancel(TicketIdValue, new TicketCancelationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });
        
        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Cancel_WhenClaimMalformed_ReturnsUnauthorized()
    {
        // Arrange
        var controller = CreateController();
        SetRawClaim(controller, "not-a-guid");

        // Act
        var result = await controller.Cancel(TicketIdValue, new TicketCancelationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Cancel_ForSuccessResult_ReturnsOk()
    {
        // Arrange
        var ticketId = TicketId.Create(TicketIdValue);
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<TicketCancelationCommand, TicketCancelationResult>(It.IsAny<TicketCancelationCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TicketCancelationResult.Success(ticketId, TicketStatus.Available));
        });
        SetAuthenticatedUser(controller, UserIdValue);

        // Act
        var result = await controller.Cancel(TicketIdValue, new TicketCancelationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Reserve_ForTicketNotAvailableErrorResult_ReturnsConflict()
    {
        // Arrange
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<TicketReservationCommand, TicketReservationResult>(It.IsAny<TicketReservationCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TicketReservationResult(new TicketNotAvailableError("Not available")));
        });
        SetAuthenticatedUser(controller, UserIdValue);

        // Act
        var result = await controller.Reserve(TicketIdValue, new TicketReservationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        // Assert
        Assert.IsType<ConflictResult>(result);
    }

    [Fact]
    public async Task Reserve_ForUnauthorizedUserErrorResult_ReturnsUnauthorized()
    {
        // Arrange
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<TicketReservationCommand, TicketReservationResult>(It.IsAny<TicketReservationCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TicketReservationResult(new UnauthorizedUserError("Forbidden")));
        });
        SetAuthenticatedUser(controller, UserIdValue);

        // Act
        var result = await controller.Reserve(TicketIdValue, new TicketReservationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Reserve_WhenNotFoundError_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<TicketReservationCommand, TicketReservationResult>(It.IsAny<TicketReservationCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TicketReservationResult(new NotFoundError("Missing")));
        });
        SetAuthenticatedUser(controller, UserIdValue);

        // Act
        var result = await controller.Reserve(TicketIdValue, new TicketReservationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Reserve_ForUnexpectedErrorResult_Returns500()
    {
        // Arrange
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<TicketReservationCommand, TicketReservationResult>(It.IsAny<TicketReservationCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TicketReservationResult(new UnexpectedError("Unexpected")));
        });
        SetAuthenticatedUser(controller, UserIdValue);

        // Act
        var result = await controller.Reserve(TicketIdValue, new TicketReservationRequest
        {
            TicketId = TicketIdValue,
            UserId = UserIdValue
        });

        // Assert
        var statusCode = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, statusCode.StatusCode);
    }

    [Fact]
    public void GetTicketById_Documentation_ContainsStatus200OK()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status200OK, GetDocumentedCodes(nameof(TicketsController.GetTicketById)));
    }

    [Fact]
    public void GetTicketById_Documentation_ContainsStatus404NotFound()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status404NotFound, GetDocumentedCodes(nameof(TicketsController.GetTicketById)));
    }

    [Fact]
    public void GetTicketById_Documentation_ContainsStatus500InternalServerError()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status500InternalServerError, GetDocumentedCodes(nameof(TicketsController.GetTicketById)));
    }

    [Fact]
    public void GetTicketByEvent_Documentation_ContainsStatus200OK()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status200OK, GetDocumentedCodes(nameof(TicketsController.GetTicketByEvent)));
    }

    [Fact]
    public void GetTicketByEvent_Documentation_ContainsStatus500InternalServerError()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status500InternalServerError, GetDocumentedCodes(nameof(TicketsController.GetTicketByEvent)));
    }

    [Fact]
    public void Reserve_Documentation_ContainsStatus200OK()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status200OK, GetDocumentedCodes(nameof(TicketsController.Reserve)));
    }

    [Fact]
    public void Reserve_Documentation_ContainsStatus400BadRequest()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status400BadRequest, GetDocumentedCodes(nameof(TicketsController.Reserve)));
    }

    [Fact]
    public void Reserve_Documentation_ContainsStatus401Unauthorized()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status401Unauthorized, GetDocumentedCodes(nameof(TicketsController.Reserve)));
    }

    [Fact]
    public void Reserve_Documentation_ContainsStatus404NotFound()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status404NotFound, GetDocumentedCodes(nameof(TicketsController.Reserve)));
    }

    [Fact]
    public void Reserve_Documentation_ContainsStatus409Conflict()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status409Conflict, GetDocumentedCodes(nameof(TicketsController.Reserve)));
    }

    [Fact]
    public void Reserve_Documentation_ContainsStatus500InternalServerError()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status500InternalServerError, GetDocumentedCodes(nameof(TicketsController.Reserve)));
    }

    [Fact]
    public void Cancel_Documentation_ContainsStatus200OK()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status200OK, GetDocumentedCodes(nameof(TicketsController.Cancel)));
    }

    [Fact]
    public void Cancel_Documentation_ContainsStatus400BadRequest()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status400BadRequest, GetDocumentedCodes(nameof(TicketsController.Cancel)));
    }

    [Fact]
    public void Cancel_Documentation_ContainsStatus401Unauthorized()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status401Unauthorized, GetDocumentedCodes(nameof(TicketsController.Cancel)));
    }

    [Fact]
    public void Cancel_Documentation_ContainsStatus404NotFound()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status404NotFound, GetDocumentedCodes(nameof(TicketsController.Cancel)));
    }

    [Fact]
    public void Cancel_Documentation_ContainsStatus409Conflict()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status409Conflict, GetDocumentedCodes(nameof(TicketsController.Cancel)));
    }

    [Fact]
    public void Cancel_Documentation_ContainsStatus500InternalServerError()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status500InternalServerError, GetDocumentedCodes(nameof(TicketsController.Cancel)));
    }

    private static int[] GetDocumentedCodes(string methodName) =>
        typeof(TicketsController).GetMethod(methodName)!
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Select(a => a.StatusCode)
            .ToArray();
}
