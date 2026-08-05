namespace TicketReservationSystem.Application.Abstractions;

public sealed record InvalidCredentialsError(string Description) : Error("InvalidCredentials", Description);
