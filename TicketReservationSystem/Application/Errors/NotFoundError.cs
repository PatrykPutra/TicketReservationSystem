using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Errors;

public sealed record NotFoundError(string Description) : Error("NotFound", Description);
