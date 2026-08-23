using System.Linq.Expressions;
using Moq;
using TicketReservationSystem.Application.Authentication;
using TicketReservationSystem.Application.Commands.Authentication;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Tests;

public class AuthenticationHandlerTests
{
    [Fact]
    public async Task SendAuthenticationCode_ForExistingUser_ReturnsSuccess()
    {
        // Arrange
        var user = User.Register("test@test.com", "Test", "User", "123456789");

        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var verificationCodesRepositoryMock = new Mock<IVerificationCodeRepository>();
        verificationCodesRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<VerificationCode, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VerificationCode>());

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.VerificationCodes).Returns(verificationCodesRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new SendAuthenticationCodeHandler(unitOfWorkMock.Object);
        
        // Act
        var result = await handler.Handle(new SendAuthenticationCodeCommand(user.Email), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SendAuthenticationCode_ForExistingUser_AddsVerificationCode()
    {
        // Arrange
        var user = User.Register("test@test.com", "Test", "User", "123456789");

        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var verificationCodesRepositoryMock = new Mock<IVerificationCodeRepository>();
        verificationCodesRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<VerificationCode, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VerificationCode>());
        
        VerificationCode? capturedCode = null;
        verificationCodesRepositoryMock.Setup(r => r.Add(It.IsAny<VerificationCode>()))
            .Callback<VerificationCode>(addedCode => capturedCode = addedCode);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.VerificationCodes).Returns(verificationCodesRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new SendAuthenticationCodeHandler(unitOfWorkMock.Object);
        
        // Act
        await handler.Handle(new SendAuthenticationCodeCommand(user.Email), CancellationToken.None);

        // Assert
        Assert.NotNull(capturedCode);
    }

    [Fact]
    public async Task SendAuthenticationCode_ForExistingUser_AddsVerificationCodeWithCorrectUserId()
    {
        // Arrange
        var user = User.Register("test@test.com", "Test", "User", "123456789");

        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var verificationCodesRepositoryMock = new Mock<IVerificationCodeRepository>();
        verificationCodesRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<VerificationCode, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VerificationCode>());

        VerificationCode? capturedCode = null;
        verificationCodesRepositoryMock.Setup(r => r.Add(It.IsAny<VerificationCode>()))
            .Callback<VerificationCode>(addedCode => capturedCode = addedCode);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.VerificationCodes).Returns(verificationCodesRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new SendAuthenticationCodeHandler(unitOfWorkMock.Object);

        // Act
        await handler.Handle(new SendAuthenticationCodeCommand(user.Email), CancellationToken.None);

        // Assert
        Assert.NotNull(capturedCode);
        Assert.Equal(user.Id, capturedCode.UserId);
    }

    [Fact]
    public async Task SendAuthenticationCode_ForExistingUser_AddsVerificationCodeWithValidExpiryDate()
    {
        // Arrange
        var user = User.Register("test@test.com", "Test", "User", "123456789");

        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var verificationCodesRepositoryMock = new Mock<IVerificationCodeRepository>();
        verificationCodesRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<VerificationCode, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VerificationCode>());

        VerificationCode? capturedCode = null;
        verificationCodesRepositoryMock.Setup(r => r.Add(It.IsAny<VerificationCode>()))
            .Callback<VerificationCode>(addedCode => capturedCode = addedCode);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.VerificationCodes).Returns(verificationCodesRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new SendAuthenticationCodeHandler(unitOfWorkMock.Object);

        // Act
        await handler.Handle(new SendAuthenticationCodeCommand(user.Email), CancellationToken.None);

        // Assert
        Assert.NotNull(capturedCode);
        Assert.True(capturedCode.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task SendAuthenticationCode_ForExistingUser_AddsNotUsedVerificationCode()
    {
        // Arrange
        var user = User.Register("test@test.com", "Test", "User", "123456789");

        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var verificationCodesRepositoryMock = new Mock<IVerificationCodeRepository>();
        verificationCodesRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<VerificationCode, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VerificationCode>());

        VerificationCode? capturedCode = null;
        verificationCodesRepositoryMock.Setup(r => r.Add(It.IsAny<VerificationCode>()))
            .Callback<VerificationCode>(addedCode => capturedCode = addedCode);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.VerificationCodes).Returns(verificationCodesRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new SendAuthenticationCodeHandler(unitOfWorkMock.Object);

        // Act
        await handler.Handle(new SendAuthenticationCodeCommand(user.Email), CancellationToken.None);

        // Assert
        Assert.NotNull(capturedCode);
        Assert.False(capturedCode.IsUsed);
    }

    [Fact]
    public async Task SendAuthenticationCode_ForExistingUser_SavesChanges()
    {
        // Arrange
        var user = User.Register("test@test.com", "Test", "User", "123456789");

        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var verificationCodesRepositoryMock = new Mock<IVerificationCodeRepository>();
        verificationCodesRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<VerificationCode, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VerificationCode>());

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.VerificationCodes).Returns(verificationCodesRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new SendAuthenticationCodeHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new SendAuthenticationCodeCommand(user.Email), CancellationToken.None);

        // Assert
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAuthenticationCode_ForUnknownUser_ReturnsUserNotFoundErrorResult()
    {
        // Arrange
        User? user = default;
        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var verificationCodesRepositoryMock = new Mock<IVerificationCodeRepository>();
        verificationCodesRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<VerificationCode, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VerificationCode>());

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.VerificationCodes).Returns(verificationCodesRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new SendAuthenticationCodeHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new SendAuthenticationCodeCommand("nonexistent@test.com"), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<UserNotFoundError>(result.Error);
    }

    [Fact]
    public async Task SendAuthenticationCode_ForRateLimitedRequest_ReturnsRateLimitedErrorResult()
    {
        // Arrange
        User? user = User.Register("test@test.com", "Test", "User", "123456789");
        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        List<VerificationCode> verificationCodes = [VerificationCode.Generate(user.Id, user.Email, "123456", DateTime.UtcNow.AddMinutes(10))];
        var verificationCodesRepositoryMock = new Mock<IVerificationCodeRepository>();
        verificationCodesRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<VerificationCode, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(verificationCodes);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.VerificationCodes).Returns(verificationCodesRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new SendAuthenticationCodeHandler(unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new SendAuthenticationCodeCommand(user.Email), CancellationToken.None);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<RateLimitedError>(result.Error);
    }

    [Fact]
    public async Task GenerateToken_ForValidVerificationCode_ReturnsSuccessResult()
    {
        // Arrange
        var user = User.Register("test@test.com", "Test", "User", "123456789");

        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        VerificationCode verificationCode = VerificationCode.Generate(user.Id, user.Email, "123456", DateTime.UtcNow.AddMinutes(10));
        var verificationCodesRepositoryMock = new Mock<IVerificationCodeRepository>();
        verificationCodesRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<VerificationCode, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VerificationCode>() { verificationCode });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.VerificationCodes).Returns(verificationCodesRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var jwtServiceMock = new Mock<IJwtService>();
        jwtServiceMock.Setup(s => s.GenerateToken(user.Id, user.Email)).Returns("test-token");

        var handler = new GenerateTokenHandler(unitOfWorkMock.Object,jwtServiceMock.Object);

        // Act
        var result = await handler.Handle(new GenerateTokenCommand(user.Email, verificationCode.Code), CancellationToken.None);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("test-token", result.Value.Token);
        Assert.True(result.Value.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task GenerateToken_ForValidVerificationCode_ReturnsValidToken()
    {
        // Arrange
        var user = User.Register("test@test.com", "Test", "User", "123456789");

        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        VerificationCode verificationCode = VerificationCode.Generate(user.Id, user.Email, "123456", DateTime.UtcNow.AddMinutes(10));
        var verificationCodesRepositoryMock = new Mock<IVerificationCodeRepository>();
        verificationCodesRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<VerificationCode, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VerificationCode>() { verificationCode });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.VerificationCodes).Returns(verificationCodesRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var jwtServiceMock = new Mock<IJwtService>();
        jwtServiceMock.Setup(s => s.GenerateToken(user.Id, user.Email)).Returns("test-token");

        var handler = new GenerateTokenHandler(unitOfWorkMock.Object, jwtServiceMock.Object);

        // Act
        var result = await handler.Handle(new GenerateTokenCommand(user.Email, verificationCode.Code), CancellationToken.None);

        // Assert
        Assert.Equal("test-token", result.Value.Token);
        Assert.True(result.Value.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task GenerateToken_ForValidVerificationCode_MarksVerificationCodeAsUsed()
    {
        // Arrange
        var user = User.Register("test@test.com", "Test", "User", "123456789");

        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        VerificationCode verificationCode = VerificationCode.Generate(user.Id, user.Email, "123456", DateTime.UtcNow.AddMinutes(10));
        var verificationCodesRepositoryMock = new Mock<IVerificationCodeRepository>();
        verificationCodesRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<VerificationCode, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VerificationCode>() { verificationCode });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.VerificationCodes).Returns(verificationCodesRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var jwtServiceMock = new Mock<IJwtService>();
        jwtServiceMock.Setup(s => s.GenerateToken(user.Id, user.Email)).Returns("test-token");

        var handler = new GenerateTokenHandler(unitOfWorkMock.Object, jwtServiceMock.Object);

        // Act
        var result = await handler.Handle(new GenerateTokenCommand(user.Email, verificationCode.Code), CancellationToken.None);

        // Assert
        Assert.True(verificationCode.IsUsed);
    }

    [Fact]
    public async Task GenerateToken_ForUnknownUser_ReturnsInvalidCredentialsErrorResult()
    {
        // Arrange
        User? user = default;

        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var verificationCodesRepositoryMock = new Mock<IVerificationCodeRepository>();
        verificationCodesRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<VerificationCode, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VerificationCode>());

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.VerificationCodes).Returns(verificationCodesRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var jwtServiceMock = new Mock<IJwtService>();

        var handler = new GenerateTokenHandler(unitOfWorkMock.Object, jwtServiceMock.Object);

        // Act
        var result = await handler.Handle(new GenerateTokenCommand("test@test.com", "123456"), CancellationToken.None);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<InvalidCredentialsError>(result.Error);
    }

    [Fact]
    public async Task GenerateToken_ForNullVerificationCodeDbQueryResult_ReturnsInvalidCredentialsErrorResult()
    {
        // Arrange
        var user = User.Register("test@test.com", "Test", "User", "123456789");

        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var verificationCodesRepositoryMock = new Mock<IVerificationCodeRepository>();
        verificationCodesRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<VerificationCode, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VerificationCode>());

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.VerificationCodes).Returns(verificationCodesRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var jwtServiceMock = new Mock<IJwtService>();
        jwtServiceMock.Setup(s => s.GenerateToken(user.Id, user.Email)).Returns("test-token");

        var handler = new GenerateTokenHandler(unitOfWorkMock.Object, jwtServiceMock.Object);

        // Act
        var result = await handler.Handle(new GenerateTokenCommand(user.Email, "000000"), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<InvalidCredentialsError>(result.Error);
    }

    [Fact]
    public async Task GenerateToken_ForAnyRequest_DoesNotLoadUsedCodes()
    {
        // Arrange
        var user = User.Register("test@test.com", "Test", "User", "123456789");

        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        VerificationCode verificationCode = VerificationCode.Generate(user.Id, user.Email, "123456", DateTime.UtcNow.AddMinutes(10));
        verificationCode.MarkAsUsed();

        Expression<Func<VerificationCode, bool>>? capturedPredicate = null;
        var verificationCodesRepositoryMock = new Mock<IVerificationCodeRepository>();
        verificationCodesRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<VerificationCode, bool>>>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<VerificationCode, bool>>, CancellationToken>((predicate, _) => capturedPredicate = predicate)
            .ReturnsAsync(new List<VerificationCode>() { verificationCode });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.VerificationCodes).Returns(verificationCodesRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var jwtServiceMock = new Mock<IJwtService>();
        jwtServiceMock.Setup(s => s.GenerateToken(user.Id, user.Email)).Returns("test-token");

        var handler = new GenerateTokenHandler(unitOfWorkMock.Object, jwtServiceMock.Object);

        // Act
        var result = await handler.Handle(new GenerateTokenCommand(user.Email, verificationCode.Code), CancellationToken.None);

        // Assert
        Assert.NotNull(capturedPredicate);
        Assert.False(capturedPredicate!.Compile()(verificationCode));
    }

    [Fact]
    public async Task GenerateToken_ForAnyRequest_DoesNotLoadExpiredCodes()
    {
        // Arrange
        var user = User.Register("test@test.com", "Test", "User", "123456789");

        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        VerificationCode verificationCode = VerificationCode.Generate(user.Id, user.Email, "123456", DateTime.UtcNow.AddMinutes(-1));

        Expression<Func<VerificationCode, bool>>? capturedPredicate = null;
        var verificationCodesRepositoryMock = new Mock<IVerificationCodeRepository>();
        verificationCodesRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<VerificationCode, bool>>>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<VerificationCode, bool>>, CancellationToken>((predicate, _) => capturedPredicate = predicate)
            .ReturnsAsync(new List<VerificationCode>() { verificationCode });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Users).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.VerificationCodes).Returns(verificationCodesRepositoryMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var jwtServiceMock = new Mock<IJwtService>();
        jwtServiceMock.Setup(s => s.GenerateToken(user.Id, user.Email)).Returns("test-token");

        var handler = new GenerateTokenHandler(unitOfWorkMock.Object, jwtServiceMock.Object);

        // Act
        var result = await handler.Handle(new GenerateTokenCommand(user.Email, verificationCode.Code), CancellationToken.None);

        // Assert
        Assert.NotNull(capturedPredicate);
        Assert.False(capturedPredicate!.Compile()(VerificationCode.Generate(user.Id, user.Email, "123456", DateTime.UtcNow.AddMinutes(-1))));
    }
}