using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Errors;

public sealed record UnsupportedCurrencyError(string Description) : Error("UnsupportedCurrency", Description);
