using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Errors;

public sealed record TicketNotReservedError(string Description) : Error("TicketNotReserved", Description);
