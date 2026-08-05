namespace TicketReservationSystem.Application.Abstractions;

public sealed record TicketNotAvailableError(string Description) : Error("TicketNotAvailable", Description);
