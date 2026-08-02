namespace TicketReservationSystem.Application.Commands.Authentication
{
    public record TokenResponse(string Token, DateTime ExpiresAt);
}