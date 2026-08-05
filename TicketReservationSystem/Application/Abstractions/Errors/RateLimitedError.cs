namespace TicketReservationSystem.Application.Abstractions;

public sealed record RateLimitedError(string Description) : Error("RateLimited", Description);
