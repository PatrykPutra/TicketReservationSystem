using System.Linq.Expressions;
using Moq;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Authentication;
using TicketReservationSystem.Application.Commands.Authentication;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Tests;

public class AuthenticationHandlerTests
{
    private sealed record UnitOfWorkMocks(
        Mock<IUnitOfWork> Uow,
        Mock<IUserRepository> Users,
        Mock<IVerificationCodeRepository> Codes);

    private static User CreateUser()
    {
        var user = User.Register("test@test.com", "Test", "User", "123456789");
        return user;
    }

    private static UnitOfWorkMocks CreateUnitOfWork(
        User? user,
        List<VerificationCode>? codes = null)
    {
        var usersRepo = new Mock<IUserRepository>();
        usersRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var codesRepo = new Mock<IVerificationCodeRepository>();
        codesRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<VerificationCode, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(codes ?? new List<VerificationCode>());

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Users).Returns(usersRepo.Object);
        uow.SetupGet(u => u.VerificationCodes).Returns(codesRepo.Object);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return new UnitOfWorkMocks(uow, usersRepo, codesRepo);
    }

    private static Mock<IJwtService> CreateJwtService(UserId userId)
    {
        var jwtService = new Mock<IJwtService>();
        jwtService.Setup(s => s.GenerateToken(userId, "test@test.com")).Returns("test-token");
        return jwtService;
    }

    [Fact]
    public async Task SendAuthenticationCode_ForExistingUser_ReturnsSuccess()
    {
        var userId = UserId.CreateUnique();
        var mocks = CreateUnitOfWork(CreateUser());
        var handler = new SendAuthenticationCodeHandler(mocks.Uow.Object);

        var result = await handler.Handle(new SendAuthenticationCodeCommand("test@test.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SendAuthenticationCode_ForExistingUser_AddsVerificationCode()
    {
        var user = CreateUser();
        var mocks = CreateUnitOfWork(user);
        var handler = new SendAuthenticationCodeHandler(mocks.Uow.Object);

        VerificationCode? captured = null;
        mocks.Codes.Setup(r => r.Add(It.IsAny<VerificationCode>()))
            .Callback<VerificationCode>(code => captured = code);

        await handler.Handle(new SendAuthenticationCodeCommand("test@test.com"), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(user.Id, captured!.UserId);
        Assert.False(captured.IsUsed);
        Assert.True(captured.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task SendAuthenticationCode_ForExistingUser_SavesChanges()
    {
        var user = CreateUser();
        var mocks = CreateUnitOfWork(user);
        var handler = new SendAuthenticationCodeHandler(mocks.Uow.Object);

        await handler.Handle(new SendAuthenticationCodeCommand("test@test.com"), CancellationToken.None);

        mocks.Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAuthenticationCode_ForUnknownUser_ReturnsUserNotFound()
    {
        var mocks = CreateUnitOfWork(user: null);
        var handler = new SendAuthenticationCodeHandler(mocks.Uow.Object);

        var result = await handler.Handle(new SendAuthenticationCodeCommand("nonexistent@test.com"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<UserNotFoundError>(result.Error);
    }

    [Fact]
    public async Task SendAuthenticationCode_WhenRateLimited_ReturnsRateLimited()
    {
        var user = CreateUser();
        var recentCode = VerificationCode.Generate(user.Id, "test@test.com", "123456", DateTime.UtcNow.AddMinutes(5));
        var mocks = CreateUnitOfWork(user, new List<VerificationCode> { recentCode });
        var handler = new SendAuthenticationCodeHandler(mocks.Uow.Object);

        var result = await handler.Handle(new SendAuthenticationCodeCommand("test@test.com"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<RateLimitedError>(result.Error);
    }

    [Fact]
    public async Task GenerateToken_ForValidCode_ReturnsTokenWithExpiry()
    {
        var user = CreateUser();
        var validCode = VerificationCode.Generate(user.Id, "test@test.com", "123456", DateTime.UtcNow.AddMinutes(5));
        var mocks = CreateUnitOfWork(user, new List<VerificationCode> { validCode });
        var handler = new GenerateTokenHandler(mocks.Uow.Object, CreateJwtService(user.Id).Object);

        var result = await handler.Handle(new GenerateTokenCommand("test@test.com", "123456"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("test-token", result.Value.Token);
        Assert.True(result.Value.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task GenerateToken_ForValidCode_MarksCodeUsed()
    {
        var user = CreateUser();
        var validCode = VerificationCode.Generate(user.Id, "test@test.com", "123456", DateTime.UtcNow.AddMinutes(5));
        var mocks = CreateUnitOfWork(user, new List<VerificationCode> { validCode });
        var handler = new GenerateTokenHandler(mocks.Uow.Object, CreateJwtService(user.Id).Object);

        await handler.Handle(new GenerateTokenCommand("test@test.com", "123456"), CancellationToken.None);

        Assert.True(validCode.IsUsed);
    }

    [Fact]
    public async Task GenerateToken_ForUnknownUser_ReturnsInvalidCredentials()
    {
        var mocks = CreateUnitOfWork(user: null);
        var handler = new GenerateTokenHandler(mocks.Uow.Object, Mock.Of<IJwtService>());

        var result = await handler.Handle(new GenerateTokenCommand("test@test.com", "123456"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<InvalidCredentialsError>(result.Error);
    }

    [Fact]
    public async Task GenerateToken_ForInvalidCode_ReturnsInvalidCredentials()
    {
        var user = CreateUser();
        var mocks = CreateUnitOfWork(user);
        var handler = new GenerateTokenHandler(mocks.Uow.Object, Mock.Of<IJwtService>());

        var result = await handler.Handle(new GenerateTokenCommand("test@test.com", "000000"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<InvalidCredentialsError>(result.Error);
    }

    [Fact]
    public async Task GenerateToken_ForUsedCode_FiltersCodeOutOfLookup()
    {
        var user = CreateUser();
        var usedCode = VerificationCode.Generate(user.Id, "test@test.com", "123456", DateTime.UtcNow.AddMinutes(5));
        usedCode.MarkAsUsed();
        var mocks = CreateUnitOfWork(user);
        var handler = new GenerateTokenHandler(mocks.Uow.Object, Mock.Of<IJwtService>());

        Expression<Func<VerificationCode, bool>>? predicate = null;
        mocks.Codes
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<VerificationCode, bool>>>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<VerificationCode, bool>>, CancellationToken>((expr, _) => predicate = expr)
            .ReturnsAsync(new List<VerificationCode>());

        await handler.Handle(new GenerateTokenCommand("test@test.com", "123456"), CancellationToken.None);

        Assert.NotNull(predicate);
        Assert.False(predicate!.Compile()(usedCode));
    }

    [Fact]
    public async Task GenerateToken_ForExpiredCode_FiltersCodeOutOfLookup()
    {
        var user = CreateUser();
        var expiredCode = VerificationCode.Generate(user.Id, "test@test.com", "123456", DateTime.UtcNow.AddMinutes(-1));
        var mocks = CreateUnitOfWork(user);
        var handler = new GenerateTokenHandler(mocks.Uow.Object, Mock.Of<IJwtService>());

        Expression<Func<VerificationCode, bool>>? predicate = null;
        mocks.Codes
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<VerificationCode, bool>>>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<VerificationCode, bool>>, CancellationToken>((expr, _) => predicate = expr)
            .ReturnsAsync(new List<VerificationCode>());

        await handler.Handle(new GenerateTokenCommand("test@test.com", "123456"), CancellationToken.None);

        Assert.NotNull(predicate);
        Assert.False(predicate!.Compile()(expiredCode));
    }
}