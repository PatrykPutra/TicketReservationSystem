namespace TicketReservationSystem.Application.Abstractions;

public sealed record UserNotFoundError(string Description) : Error("UserNotFound", Description);
