using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Tests;

public class UserDtoTests
{
    [Theory]
    [InlineData("user@test.com", "Test", "User", "123456789", true)]
    [InlineData("test@email.com", "TestUser", "UserTest", "987654321", false)]
    public void UserDto_ForValidInputs_ExposesAllFields(string email, string firstName, string lastName, string phoneNumber, bool isVerified)
    {
        var id = UserId.CreateUnique();

        var dto = new UserDto(id, email, firstName, lastName, phoneNumber, isVerified);

        Assert.Equal(id, dto.Id);
        Assert.Equal(email, dto.Email);
        Assert.Equal(firstName, dto.FirstName);
        Assert.Equal(lastName, dto.LastName);
        Assert.Equal(phoneNumber, dto.PhoneNumber);
        Assert.Equal(isVerified, dto.IsVerified);
    }
}