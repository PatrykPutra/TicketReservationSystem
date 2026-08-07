using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Errors;

public sealed record UserAlreadyExistsError(string Description) : Error("UserAlreadyExists", Description);
