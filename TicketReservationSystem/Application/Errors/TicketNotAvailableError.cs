using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Errors;

public sealed record TicketNotAvailableError(string Description) : Error("TicketNotAvailable", Description);
