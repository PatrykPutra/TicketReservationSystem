using Moq;
using TicketReservationSystem.Application.Commands.Users;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Tests;

public class AddUserHandlerTests
{
    private sealed record UnitOfWorkMocks(
        Mock<IUnitOfWork> Uow,
        Mock<IUserRepository> Users);

    private static UnitOfWorkMocks CreateUnitOfWork(User? existingUser)
    {
        var usersRepo = new Mock<IUserRepository>();
        usersRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Users).Returns(usersRepo.Object);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return new UnitOfWorkMocks(uow, usersRepo);
    }

    [Fact]
    public async Task AddUser_ForNewEmail_ReturnsSuccessWithGeneratedId()
    {
        var mocks = CreateUnitOfWork(existingUser: null);
        var handler = new AddUserHandler(mocks.Uow.Object);

        var result = await handler.Handle(
            new AddUserCommand("new@test.com", "New", "User", "987654321"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id.Value);
    }

    [Fact]
    public async Task AddUser_ForNewEmail_AddsRegisteredUserToRepository()
    {
        var mocks = CreateUnitOfWork(existingUser: null);
        var handler = new AddUserHandler(mocks.Uow.Object);

        await handler.Handle(
            new AddUserCommand("new@test.com", "New", "User", "987654321"),
            CancellationToken.None);

        mocks.Users.Verify(u => u.Add(It.Is<User>(saved =>
            saved.Email == "new@test.com" &&
            saved.FirstName == "New" &&
            saved.LastName == "User" &&
            saved.PhoneNumber == "987654321")), Times.Once);
    }

    [Fact]
    public async Task AddUser_ForNewEmail_SavesChanges()
    {
        var mocks = CreateUnitOfWork(existingUser: null);
        var handler = new AddUserHandler(mocks.Uow.Object);

        await handler.Handle(
            new AddUserCommand("new@test.com", "New", "User", "987654321"),
            CancellationToken.None);

        mocks.Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddUser_ForDuplicateEmail_ReturnsError()
    {
        var existing = new User(UserId.CreateUnique());
        existing.Register("existing@test.com", "Existing", "User", "123456789");
        var mocks = CreateUnitOfWork(existingUser: existing);
        var handler = new AddUserHandler(mocks.Uow.Object);

        var result = await handler.Handle(
            new AddUserCommand("existing@test.com", "New", "User", "987654321"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<UserAlreadyExistsError>(result.Error);
    }

    [Fact]
    public async Task AddUser_ForDuplicateEmail_DoesNotSaveChanges()
    {
        var existing = new User(UserId.CreateUnique());
        existing.Register("existing@test.com", "Existing", "User", "123456789");
        var mocks = CreateUnitOfWork(existingUser: existing);
        var handler = new AddUserHandler(mocks.Uow.Object);

        await handler.Handle(
            new AddUserCommand("existing@test.com", "New", "User", "987654321"),
            CancellationToken.None);

        mocks.Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}