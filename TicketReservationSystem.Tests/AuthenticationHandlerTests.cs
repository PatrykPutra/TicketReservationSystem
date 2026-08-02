using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands.Authentication;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Infrastructure.Authentication;
using TicketReservationSystem.Infrastructure.Persistence;
using TicketReservationSystem.Infrastructure.Repository;

namespace TicketReservationSystem.Tests;

public class AuthenticationHandlerTests
{
    private static ServiceProvider CreateServiceProvider(string dbName)
    {
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEmailVerificationCodeRepository, EmailVerificationCodeRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<Infrastructure.DomainEventsDispatcher.IDomainEventsDispatcher>(
            Mock.Of<Infrastructure.DomainEventsDispatcher.IDomainEventsDispatcher>());

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SendAuthenticationCodeHandler_user_exists_saves_code_and_returns_success()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);
        var userId = UserId.CreateUnique();

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var user = new User(userId);
            user.Register("test@test.com", "Test", "User", "123456789");
            uow.Users.Add(user);
            await uow.SaveChangesAsync();
        }

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var handler = new SendAuthenticationCodeHandler(uow);

            var result = await handler.Handle(new SendAuthenticationCodeCommand("test@test.com"), CancellationToken.None);

            Assert.True(result.IsSuccess);
        }

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var codes = await uow.EmailVerificationCodes.FindAsync(e => e.UserId == userId, CancellationToken.None);
            var codeEntity = codes.Single();

            Assert.NotNull(codeEntity);
            Assert.Equal(userId, codeEntity.UserId);
            Assert.False(codeEntity.IsUsed);
            Assert.True(codeEntity.ExpiresAt > DateTime.UtcNow);
        }
    }

    [Fact]
    public async Task SendAuthenticationCodeHandler_user_not_found_returns_user_not_found()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var handler = new SendAuthenticationCodeHandler(uow);

            var result = await handler.Handle(new SendAuthenticationCodeCommand("nonexistent@test.com"), CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.IsType<UserNotFoundError>(result.Error);
        }
    }

    [Fact]
    public async Task SendAuthenticationCodeHandler_rate_limited_returns_rate_limited()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);
        var userId = UserId.CreateUnique();

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var user = new User(userId);
            user.Register("test@test.com", "Test", "User", "123456789");
            uow.Users.Add(user);
            await uow.SaveChangesAsync();
        }

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var handler = new SendAuthenticationCodeHandler(uow);

            await handler.Handle(new SendAuthenticationCodeCommand("test@test.com"), CancellationToken.None);

            var result = await handler.Handle(new SendAuthenticationCodeCommand("test@test.com"), CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.IsType<RateLimitedError>(result.Error);
        }
    }

    [Fact]
    public async Task GenerateTokenHandler_valid_code_returns_token_and_marks_code_used()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);
        var userId = UserId.CreateUnique();
        var mockJwtService = new Mock<IJwtService>();
        mockJwtService.Setup(x => x.GenerateToken(userId, "test@test.com")).Returns("test-token");

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var user = new User(userId);
            user.Register("test@test.com", "Test", "User", "123456789");
            uow.Users.Add(user);
            await uow.SaveChangesAsync();
        }

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var sendHandler = new SendAuthenticationCodeHandler(uow);
            await sendHandler.Handle(new SendAuthenticationCodeCommand("test@test.com"), CancellationToken.None);
        }

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var codes = await uow.EmailVerificationCodes.FindAsync(e => e.UserId == userId, CancellationToken.None);
            var codeEntity = codes.Single();

            var handler = new GenerateTokenHandler(uow, mockJwtService.Object);
            var result = await handler.Handle(new GenerateTokenCommand("test@test.com", codeEntity!.Code), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("test-token", result.Value.Token);
            Assert.NotNull(result.Value.ExpiresAt);
        }

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var codes = await uow.EmailVerificationCodes.FindAsync(e => e.UserId == userId, CancellationToken.None);
            var codeEntity = codes.Single();
            Assert.True(codeEntity!.IsUsed);
        }
    }

    [Fact]
    public async Task GenerateTokenHandler_invalid_code_returns_invalid_credentials()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);
        var userId = UserId.CreateUnique();
        var mockJwtService = new Mock<IJwtService>();

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var user = new User(userId);
            user.Register("test@test.com", "Test", "User", "123456789");
            uow.Users.Add(user);
            await uow.SaveChangesAsync();
        }

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var handler = new GenerateTokenHandler(uow, mockJwtService.Object);

            var result = await handler.Handle(new GenerateTokenCommand("test@test.com", "000000"), CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.IsType<InvalidCredentialsError>(result.Error);
        }
    }

    [Fact]
    public async Task GenerateTokenHandler_used_code_returns_invalid_credentials()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);
        var userId = UserId.CreateUnique();
        var mockJwtService = new Mock<IJwtService>();
        mockJwtService.Setup(x => x.GenerateToken(userId, "test@test.com")).Returns("test-token");

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var user = new User(userId);
            user.Register("test@test.com", "Test", "User", "123456789");
            uow.Users.Add(user);
            await uow.SaveChangesAsync();
        }

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var sendHandler = new SendAuthenticationCodeHandler(uow);
            await sendHandler.Handle(new SendAuthenticationCodeCommand("test@test.com"), CancellationToken.None);
        }

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var codes = await uow.EmailVerificationCodes.FindAsync(e => e.UserId == userId, CancellationToken.None);
            var codeEntity = codes.Single();

            var handler = new GenerateTokenHandler(uow, mockJwtService.Object);
            await handler.Handle(new GenerateTokenCommand("test@test.com", codeEntity!.Code), CancellationToken.None);

            var result = await handler.Handle(new GenerateTokenCommand("test@test.com", codeEntity!.Code), CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.IsType<InvalidCredentialsError>(result.Error);
        }
    }

    [Fact]
    public async Task GenerateTokenHandler_expired_code_returns_invalid_credentials()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);
        var userId = UserId.CreateUnique();
        var mockJwtService = new Mock<IJwtService>();

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var user = new User(userId);
            user.Register("test@test.com", "Test", "User", "123456789");
            uow.Users.Add(user);
            await uow.SaveChangesAsync();
        }

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var expiredCode = EmailVerificationCode.Generate(userId, "test@test.com", "123456", DateTime.UtcNow.AddMinutes(-1));
            uow.EmailVerificationCodes.Add(expiredCode);
            await uow.SaveChangesAsync();
        }

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var handler = new GenerateTokenHandler(uow, mockJwtService.Object);

            var result = await handler.Handle(new GenerateTokenCommand("test@test.com", "123456"), CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.IsType<InvalidCredentialsError>(result.Error);
        }
    }
}
