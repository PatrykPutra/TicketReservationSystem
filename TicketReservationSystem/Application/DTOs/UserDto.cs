using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Application.DTOs
{
    public record UserDto(
        UserId Id,
        string Email,
        string FirstName,
        string LastName,
        string PhoneNumber,
        bool IsVerified);
}
