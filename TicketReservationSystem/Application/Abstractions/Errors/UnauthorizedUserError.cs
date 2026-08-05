namespace TicketReservationSystem.Application.Abstractions;

public sealed record UnauthorizedUserError(string Description) : Error("UnauthorizedUser", Description);
