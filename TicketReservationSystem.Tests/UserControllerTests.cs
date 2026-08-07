using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TicketReservationSystem.API.Controllers;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Application.Queries.Users;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Tests;

public class UserControllerTests
{
    private static readonly Guid UserIdValue = Guid.NewGuid();

    private static UserController CreateController(Action<Mock<IQueryDispatcher>>? querySetup = null)
    {
        var queryDispatcherMock = new Mock<IQueryDispatcher>();
        querySetup?.Invoke(queryDispatcherMock);

        var controller = new UserController(queryDispatcherMock.Object, Mock.Of<ICommandDispatcher>())
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
}
