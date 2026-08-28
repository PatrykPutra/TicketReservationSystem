using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Reflection;
using System.Security.Claims;
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
    private sealed record UnexpectedError(string Description) : Error("Unexpected", Description);

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
        // Arrange
        var controller = CreateController();
        SetAuthenticatedUser(controller, Guid.NewGuid());

        // Act
        var result = await controller.GetUser(UserIdValue);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetUser_WhenIdClaimMissing_ReturnsUnauthorized()
    {
        // Arrange
        var controller = CreateController();
        SetAuthenticatedUser(controller);

        // Act
        var result = await controller.GetUser(UserIdValue);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetUser_WhenUserFound_ReturnsOk()
    {
        // Arrange
        var userId = UserId.Create(UserIdValue);
        var userDto = new UserDto(userId, "user@test.com", "John", "Doe", "123456789", true);
        var controller = CreateController(querySetup: mock =>
        {
            mock.Setup(d => d.ExecuteAsync<GetUserQuery, GetUserResult>(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetUserResult(userDto));
        });
        SetAuthenticatedUser(controller, UserIdValue);

        // Act
        var result = await controller.GetUser(UserIdValue);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetUser_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController(querySetup: mock =>
        {
            mock.Setup(d => d.ExecuteAsync<GetUserQuery, GetUserResult>(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetUserResult(null));
        });
        SetAuthenticatedUser(controller, UserIdValue);

        // Act
        var result = await controller.GetUser(UserIdValue);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AddUser_WhenUserCreated_ReturnsOk()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<AddUserCommand, AddUserResult>(
                    It.IsAny<AddUserCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AddUserResult.Success(userId));
        });

        // Act
        var result = await controller.AddUser(new AddUserRequest
        {
            Email = "new@test.com",
            FirstName = "New",
            LastName = "User",
            PhoneNumber = "123456789"
        });

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AddUserResponse>(ok.Value);
        Assert.Equal(userId, response.Id);
    }

    [Fact]
    public async Task AddUser_ForUserAlreadyExistsErrorResult_ReturnsConflict()
    {
        // Arrange
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<AddUserCommand, AddUserResult>(
                    It.IsAny<AddUserCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AddUserResult(new UserAlreadyExistsError("Email taken")));
        });

        // Act
        var result = await controller.AddUser(new AddUserRequest
        {
            Email = "new@test.com",
            FirstName = "New",
            LastName = "User",
            PhoneNumber = "123456789"
        });
        
        // Assert
        Assert.IsType<ConflictResult>(result);
    }

    [Fact]
    public async Task AddUser_ForUnexpectedErrorResult_Returns500()
    {
        // Arrange
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<AddUserCommand, AddUserResult>(
                    It.IsAny<AddUserCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AddUserResult(new UnexpectedError("Unexpected")));
        });

        // Act
        var result = await controller.AddUser(new AddUserRequest
        {
            Email = "new@test.com",
            FirstName = "New",
            LastName = "User",
            PhoneNumber = "123456789"
        });

        // Assert
        var statusCode = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, statusCode.StatusCode);
    }

    [Fact]
    public async Task AddUser_ForNotFoundErrorResult_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<AddUserCommand, AddUserResult>(
                    It.IsAny<AddUserCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AddUserResult(new NotFoundError("Missing")));
        });

        // Act
        var result = await controller.AddUser(new AddUserRequest
        {
            Email = "new@test.com",
            FirstName = "New",
            LastName = "User",
            PhoneNumber = "123456789"
        });

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AddUser_WhenCurrencyMismatchError_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController(commandSetup: mock =>
        {
            mock.Setup(d => d.DispatchAsync<AddUserCommand, AddUserResult>(
                    It.IsAny<AddUserCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AddUserResult(new CurrencyMismatchError("Mismatch")));
        });

        // Act
        var result = await controller.AddUser(new AddUserRequest
        {
            Email = "new@test.com",
            FirstName = "New",
            LastName = "User",
            PhoneNumber = "123456789"
        });

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public void GetUser_Documentation_ContainsStatus200OK()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status200OK, GetDocumentedCodes(nameof(UserController.GetUser)));
    }

    [Fact]
    public void GetUser_Documentation_ContainsStatus401Unauthorized()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status401Unauthorized, GetDocumentedCodes(nameof(UserController.GetUser)));
    }

    [Fact]
    public void GetUser_Documentation_ContainsStatus404NotFound()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status404NotFound, GetDocumentedCodes(nameof(UserController.GetUser)));
    }

    [Fact]
    public void GetUser_Documentation_ContainsStatus500InternalServerError()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status500InternalServerError, GetDocumentedCodes(nameof(UserController.GetUser)));
    }

    [Fact]
    public void AddUser_Documentation_ContainsStatus200OK()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status200OK, GetDocumentedCodes(nameof(UserController.AddUser)));
    }

    [Fact]
    public void AddUser_Documentation_ContainsStatus400BadRequest()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status400BadRequest, GetDocumentedCodes(nameof(UserController.AddUser)));
    }

    [Fact]
    public void AddUser_Documentation_ContainsStatus404NotFound()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status404NotFound, GetDocumentedCodes(nameof(UserController.AddUser)));
    }

    [Fact]
    public void AddUser_Documentation_ContainsStatus409Conflict()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status409Conflict, GetDocumentedCodes(nameof(UserController.AddUser)));
    }

    [Fact]
    public void AddUser_Documentation_ContainsStatus500InternalServerError()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status500InternalServerError, GetDocumentedCodes(nameof(UserController.AddUser)));
    }

    private static int[] GetDocumentedCodes(string methodName) =>
        typeof(UserController).GetMethod(methodName)!
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Select(a => a.StatusCode)
            .ToArray();
}
