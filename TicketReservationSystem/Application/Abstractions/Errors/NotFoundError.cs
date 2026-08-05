namespace TicketReservationSystem.Application.Abstractions;

public sealed record NotFoundError(string Description) : Error("NotFound", Description);
