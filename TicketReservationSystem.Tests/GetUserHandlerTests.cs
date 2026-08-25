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
        // Arrange
        var user = User.Register("user@test.com", "Firstname", "Lastname", "123456789");
        var expected = new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.PhoneNumber, false);

        var usersRepositoryMock = new Mock<IUserRepository>();
        usersRepositoryMock
            .Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(usersRepositoryMock.Object);

        var handler = new GetUserHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new GetUserQuery(email: user.Email), CancellationToken.None);

        // Assert
        Assert.Equal(expected, result.User);
    }

    [Fact]
    public async Task GetUser_WhenQueryById_ReturnsMappedDto()
    {
        // Arrange
        var user = User.Register("user@test.com", "Firstname", "Lastname", "123456789");
        var expected = new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.PhoneNumber, false);

        var usersRepositoryMock = new Mock<IUserRepository>();
        usersRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(usersRepositoryMock.Object);

        var handler = new GetUserHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new GetUserQuery(userId: user.Id), CancellationToken.None);

        // Assert
        Assert.Equal(expected, result.User);
    }

    [Fact]
    public async Task GetUser_WhenUserNotFound_ReturnsNullDto()
    {
        // Arrange
        User? user = null;
        var usersRepositoryMock = new Mock<IUserRepository>();
        usersRepositoryMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(usersRepositoryMock.Object);

        var handler = new GetUserHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new GetUserQuery(email: "missing@test.com"), CancellationToken.None);

        // Assert
        Assert.Null(result.User);
    }

    [Fact]
    public async Task GetUser_WhenNoKeyProvided_ReturnsNullDtoWithoutQuerying()
    {
        // Arrange
        var usersRepositoryMock = new Mock<IUserRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(usersRepositoryMock.Object);

        var handler = new GetUserHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new GetUserQuery(), CancellationToken.None);

        // Assert
        Assert.Null(result.User);
        usersRepositoryMock.Verify(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        usersRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}