using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Tests;

public class UserDtoTests
{
    [Fact]
    public void UserDto_ForValidInputs_ExposesAllFields()
    {
        var id = UserId.CreateUnique();

        var dto = new UserDto(id, "user@test.com", "Jan", "Kowalski", "123456789", true);

        Assert.Equal(id, dto.Id);
        Assert.Equal("user@test.com", dto.Email);
        Assert.Equal("Jan", dto.FirstName);
        Assert.Equal("Kowalski", dto.LastName);
        Assert.Equal("123456789", dto.PhoneNumber);
        Assert.True(dto.IsVerified);
    }
}