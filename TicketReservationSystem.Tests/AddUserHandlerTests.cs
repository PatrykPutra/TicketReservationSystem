using Moq;
using TicketReservationSystem.Application.Commands.Users;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Tests;

public class AddUserHandlerTests
{    
    [Fact]
    public async Task Handle_ForValidInput_ReturnsSuccess()
    {
        // Arrange
        var usersRepositoryMock = new Mock<IUserRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(usersRepositoryMock.Object);
        var handler = new AddUserHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new AddUserCommand("new@test.com", "New", "User", "987654321"),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ForValidInput_ReturnsUserWwithNotEmptyId()
    {
        // Arrange
        var usersRepositoryMock = new Mock<IUserRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(usersRepositoryMock.Object);
        var handler = new AddUserHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new AddUserCommand("new@test.com", "New", "User", "987654321"),
            CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Value.Id.Value);
    }

    [Fact]
    public async Task Handle_ForValidInput_AddsRegisteredUserToRepository()
    {
        // Arrange
        var usersRepositoryMock = new Mock<IUserRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(usersRepositoryMock.Object);
        var handler = new AddUserHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new AddUserCommand("new@test.com", "New", "User", "987654321"),
            CancellationToken.None);

        // Assert
        usersRepositoryMock.Verify(u => u.Add(It.Is<User>(saved =>
            saved.Email == "new@test.com" &&
            saved.FirstName == "New" &&
            saved.LastName == "User" &&
            saved.PhoneNumber == "987654321")), Times.Once);
    }

    [Fact]
    public async Task Handle_ForValidInput_SavesChanges()
    {
        // Arrange
        var usersRepositoryMock = new Mock<IUserRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(usersRepositoryMock.Object);
        var handler = new AddUserHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new AddUserCommand("new@test.com", "New", "User", "987654321"),
            CancellationToken.None);

        // Assert
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ForDuplicatedEmail_ReturnsFailedResult()
    {
        // Arrange
        var user = User.Register("existing@test.com", "Existing", "User", "123456789");
        var usersRepositoryMock = new Mock<IUserRepository>();
        usersRepositoryMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(usersRepositoryMock.Object);
        var handler = new AddUserHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new AddUserCommand("existing@test.com", "Existing", "User", "123456789"),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ForDuplicatedEmail_ReturnsUserAlreadyExistsError()
    {
        // Arrange
        var user = User.Register("existing@test.com", "Existing", "User", "123456789");
        var usersRepositoryMock = new Mock<IUserRepository>();
        usersRepositoryMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(usersRepositoryMock.Object);
        var handler = new AddUserHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new AddUserCommand("existing@test.com", "Existing", "User", "123456789"),
            CancellationToken.None);

        // Assert
        Assert.IsType<UserAlreadyExistsError>(result.Error);
    }

    [Fact]
    public async Task Handle_ForDuplicatedEmail_DoesNotSaveChanges()
    {
        // Arrange
        var user = User.Register("existing@test.com", "Existing", "User", "123456789");
        var usersRepositoryMock = new Mock<IUserRepository>();
        usersRepositoryMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(usersRepositoryMock.Object);
        var handler = new AddUserHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new AddUserCommand("existing@test.com", "Existing", "User", "123456789"),
            CancellationToken.None);

        // Assert
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ForInvalidEmail_ReturnsFailedResult()
    {
        // Arrange
        var usersRepositoryMock = new Mock<IUserRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(usersRepositoryMock.Object);
        var handler = new AddUserHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new AddUserCommand("invalidEmailAddress", "New", "User", "987654321"),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ForInvalidEmail_ReturnsValidationError()
    {
        // Arrange
        var usersRepositoryMock = new Mock<IUserRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(usersRepositoryMock.Object);
        var handler = new AddUserHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new AddUserCommand("invalidEmailAddress", "New", "User", "987654321"),
            CancellationToken.None);

        // Assert
        Assert.IsType<ValidationError>(result.Error);
    }
}