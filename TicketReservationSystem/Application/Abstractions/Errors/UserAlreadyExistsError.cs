namespace TicketReservationSystem.Application.Abstractions;

public sealed record UserAlreadyExistsError(string Description) : Error("UserAlreadyExists", Description);
