using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Errors;

public sealed record UserNotFoundError(string Description) : Error("UserNotFound", Description);
