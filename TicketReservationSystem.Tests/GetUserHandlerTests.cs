using Moq;
using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Application.Queries.Users;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Tests;

public class GetUserHandlerTests
{
    [Fact]
    public async Task GetUser_WhenQueryByEmail_ReturnsMappedDto()
    {
        var userId = UserId.CreateUnique();
        var user = CreateUser(userId);

        var usersRepo = new Mock<IUserRepository>();
        usersRepo.Setup(r => r.GetByEmailAsync("user@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Users).Returns(usersRepo.Object);

        var handler = new GetUserHandler(uow.Object);

        var result = await handler.Handle(new GetUserQuery(email: "user@test.com"), CancellationToken.None);

        var expected = new UserDto(userId, "user@test.com", "Jan", "Kowalski", "123456789", false);
        Assert.Equal(expected, result.User);
    }

    [Fact]
    public async Task GetUser_WhenQueryById_ReturnsMappedDto()
    {
        var userId = UserId.CreateUnique();
        var user = CreateUser(userId);

        var usersRepo = new Mock<IUserRepository>();
        usersRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Users).Returns(usersRepo.Object);

        var handler = new GetUserHandler(uow.Object);

        var result = await handler.Handle(new GetUserQuery(userId: userId), CancellationToken.None);

        var expected = new UserDto(userId, "user@test.com", "Jan", "Kowalski", "123456789", false);
        Assert.Equal(expected, result.User);
    }

    [Fact]
    public async Task GetUser_WhenUserNotFound_ReturnsNullDto()
    {
        var usersRepo = new Mock<IUserRepository>();
        usersRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Users).Returns(usersRepo.Object);

        var handler = new GetUserHandler(uow.Object);

        var result = await handler.Handle(new GetUserQuery(email: "missing@test.com"), CancellationToken.None);

        Assert.Null(result.User);
    }

    [Fact]
    public async Task GetUser_WhenNoKeyProvided_ReturnsNullDtoWithoutQuerying()
    {
        var usersRepo = new Mock<IUserRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Users).Returns(usersRepo.Object);

        var handler = new GetUserHandler(uow.Object);

        var result = await handler.Handle(new GetUserQuery(), CancellationToken.None);

        Assert.Null(result.User);
        usersRepo.Verify(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        usersRepo.Verify(r => r.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static User CreateUser(UserId userId)
    {
        var user = new User(userId);
        user.Register("user@test.com", "Jan", "Kowalski", "123456789");
        return user;
    }
}