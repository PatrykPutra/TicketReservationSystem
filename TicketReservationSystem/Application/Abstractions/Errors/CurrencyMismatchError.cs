namespace TicketReservationSystem.Application.Abstractions;

public sealed record CurrencyMismatchError(string Description) : Error("CurrencyMismatch", Description);
