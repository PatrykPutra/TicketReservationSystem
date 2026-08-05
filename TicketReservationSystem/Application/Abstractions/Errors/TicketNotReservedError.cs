namespace TicketReservationSystem.Application.Abstractions;

public sealed record TicketNotReservedError(string Description) : Error("TicketNotReserved", Description);
