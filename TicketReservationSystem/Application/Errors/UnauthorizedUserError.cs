using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Errors;

public sealed record UnauthorizedUserError(string Description) : Error("UnauthorizedUser", Description);
