using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TicketReservationSystem.API.Controllers;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands.Users;
using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Application.Queries.Users;
using TicketReservationSystem.Application.Requests;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Tests;

public class UserControllerTests
{
    private static readonly Guid UserIdValue = Guid.NewGuid();

    private static UserController CreateController(
        Action<Mock<IQueryDispatcher>>? querySetup = null,
        Action<Mock<ICommandDispatcher>>? commandSetup = null)
    {
        var queryDispatcherMock = new Mock<IQueryDispatcher>();
        querySetup?.Invoke(queryDispatcherMock);

        var commandDispatcherMock = new Mock<ICommandDispatcher>();
        commandSetup?.Invoke(commandDispatcherMock);

        var controller = new UserController(queryDispatcherMock.Object, commandDispatcherMock.Object)
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
    public async Task GetUser_WhenUserIdDoesNotMatchClaim_ReturnsUnauthorized()
    {
        var controller = CreateController();
        SetAuthenticatedUser(controller, Guid.NewGuid());

        var result = await controller.GetUser(UserIdValue);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetUser_WhenClaimMissing_ReturnsUnauthorized()
    {
        var controller = CreateController();
        SetAuthenticatedUser(controller);

        var result = await controller.GetUser(UserIdValue);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetUser_WhenUserIdMatchesAndUserFound_ReturnsOk()
    {
        var userId = UserId.Create(UserIdValue);
        var userDto = new UserDto(userId, "user@test.com", "John", "Doe", "123456789", true);
        var controller = CreateController(querySetup: mock =>
        {
            mock.Setup(d => d.ExecuteAsync<GetUserQuery, GetUserResult>(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetUserResult(userDto));
        });
        SetAuthenticatedUser(controller, UserIdValue);

        var result = await controller.GetUser(UserIdValue);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetUser_WhenUserIdMatchesAndUserMissing_ReturnsNotFound()
    {
        var controller = CreateController(querySetup: mock =>
        {
            mock.Setup(d => d.ExecuteAsync<GetUserQuery, GetUserResult>(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetUserResult(null));
        });
        SetAuthenticatedUser(controller, UserIdValue);

        var result = await controller.GetUser(UserIdValue);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AddUser_WhenUserCreated_ReturnsOk()
    {
        var userId = UserId.CreateUnique();
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<AddUserCommand, AddUserResult>(
                    It.IsAny<AddUserCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AddUserResult.Success(userId));
        });

        var result = await controller.AddUser(new AddUserRequest
        {
            Email = "new@test.com",
            FirstName = "New",
            LastName = "User",
            PhoneNumber = "123456789"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AddUserResponse>(ok.Value);
        Assert.Equal(userId, response.Id);
    }

    [Fact]
    public async Task AddUser_WhenUserAlreadyExists_ReturnsConflict()
    {
        var result = await AddUserWithError(new UserAlreadyExistsError("Email taken"));

        Assert.IsType<ConflictResult>(result);
    }

    [Fact]
    public async Task AddUser_WhenUnexpectedError_Returns500()
    {
        var result = await AddUserWithError(new InvalidCredentialsError("Unexpected"));

        var statusCode = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, statusCode.StatusCode);
    }

    [Fact]
    public async Task AddUser_WhenNotFoundError_ReturnsNotFound()
    {
        var result = await AddUserWithError(new NotFoundError("Missing"));

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AddUser_WhenCurrencyMismatchError_ReturnsBadRequest()
    {
        var result = await AddUserWithError(new CurrencyMismatchError("Mismatch"));

        Assert.IsType<BadRequestResult>(result);
    }

    private static async Task<IActionResult> AddUserWithError(Error error)
    {
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<AddUserCommand, AddUserResult>(
                    It.IsAny<AddUserCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AddUserResult(error));
        });

        return await controller.AddUser(new AddUserRequest
        {
            Email = "new@test.com",
            FirstName = "New",
            LastName = "User",
            PhoneNumber = "123456789"
        });
    }

    [Fact]
    public void GetUser_Documentation_ContainsStatus200OK()
    {
        Assert.Contains(StatusCodes.Status200OK, GetDocumentedCodes(nameof(UserController.GetUser)));
    }

    [Fact]
    public void GetUser_Documentation_ContainsStatus401Unauthorized()
    {
        Assert.Contains(StatusCodes.Status401Unauthorized, GetDocumentedCodes(nameof(UserController.GetUser)));
    }

    [Fact]
    public void GetUser_Documentation_ContainsStatus404NotFound()
    {
        Assert.Contains(StatusCodes.Status404NotFound, GetDocumentedCodes(nameof(UserController.GetUser)));
    }

    [Fact]
    public void GetUser_Documentation_ContainsStatus500InternalServerError()
    {
        Assert.Contains(StatusCodes.Status500InternalServerError, GetDocumentedCodes(nameof(UserController.GetUser)));
    }

    [Fact]
    public void AddUser_Documentation_ContainsStatus200OK()
    {
        Assert.Contains(StatusCodes.Status200OK, GetDocumentedCodes(nameof(UserController.AddUser)));
    }

    [Fact]
    public void AddUser_Documentation_ContainsStatus400BadRequest()
    {
        Assert.Contains(StatusCodes.Status400BadRequest, GetDocumentedCodes(nameof(UserController.AddUser)));
    }

    [Fact]
    public void AddUser_Documentation_ContainsStatus404NotFound()
    {
        Assert.Contains(StatusCodes.Status404NotFound, GetDocumentedCodes(nameof(UserController.AddUser)));
    }

    [Fact]
    public void AddUser_Documentation_ContainsStatus409Conflict()
    {
        Assert.Contains(StatusCodes.Status409Conflict, GetDocumentedCodes(nameof(UserController.AddUser)));
    }

    [Fact]
    public void AddUser_Documentation_ContainsStatus500InternalServerError()
    {
        Assert.Contains(StatusCodes.Status500InternalServerError, GetDocumentedCodes(nameof(UserController.AddUser)));
    }

    private static int[] GetDocumentedCodes(string methodName) =>
        typeof(UserController).GetMethod(methodName)!
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Select(a => a.StatusCode)
            .ToArray();
}
