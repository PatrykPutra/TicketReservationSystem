using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Errors;

public sealed record NoneError() : Error("None", "");
