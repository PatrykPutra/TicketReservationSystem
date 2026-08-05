namespace TicketReservationSystem.Application.Abstractions;

public sealed record UnsupportedCurrencyError(string Description) : Error("UnsupportedCurrency", Description);
