using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Errors;

public sealed record InvalidCredentialsError(string Description) : Error("InvalidCredentials", Description);
