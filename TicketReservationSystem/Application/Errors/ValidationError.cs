using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Errors;

public sealed record ValidationError(string Description) : Error("Validation", Description);

