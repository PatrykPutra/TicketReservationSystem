using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Errors;

public sealed record CurrencyMismatchError(string Description) : Error("CurrencyMismatch", Description);
