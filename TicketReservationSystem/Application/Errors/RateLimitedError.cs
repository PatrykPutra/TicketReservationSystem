using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Errors;

public sealed record RateLimitedError(string Description) : Error("RateLimited", Description);
