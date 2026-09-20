using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Errors;

public sealed record ConcurrencyConflictError(string Description) : Error("ConcurrencyConflict", Description);
