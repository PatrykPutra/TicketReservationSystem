using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands.Users;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Infrastructure.Persistence;
using TicketReservationSystem.Infrastructure.Repository;

namespace TicketReservationSystem.Tests;

public class AddUserHandlerTests
{
    private static ServiceProvider CreateServiceProvider(string dbName)
    {
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IVerificationCodeRepository, VerificationCodeRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<Infrastructure.DomainEventsDispatcher.IDomainEventsDispatcher>(
            Mock.Of<Infrastructure.DomainEventsDispatcher.IDomainEventsDispatcher>());

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task AddUser_ForNewEmail_CreatesUserAndReturnsSuccess()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var handler = new AddUserHandler(uow);

            var result = await handler.Handle(
                new AddUserCommand("new@test.com", "New", "User", "987654321"),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotEqual(Guid.Empty, result.Value.Id.Value);

            var saved = await uow.Users.GetByEmailAsync("new@test.com");
            Assert.NotNull(saved);
            Assert.Equal("New", saved.FirstName);
            Assert.Equal("User", saved.LastName);
            Assert.Equal("987654321", saved.PhoneNumber);
        }
    }

    [Fact]
    public async Task AddUser_ForDuplicateEmail_ReturnsError()
    {
        var dbName = Guid.NewGuid().ToString();
        var serviceProvider = CreateServiceProvider(dbName);
        var email = "existing@test.com";

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var user = new User(UserId.CreateUnique());
            user.Register(email, "Existing", "User", "123456789");
            uow.Users.Add(user);
            await uow.SaveChangesAsync();
        }

        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var handler = new AddUserHandler(uow);

            var result = await handler.Handle(
                new AddUserCommand(email, "New", "User", "987654321"),
                CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.IsType<UserAlreadyExistsError>(result.Error);
        }
    }
}
