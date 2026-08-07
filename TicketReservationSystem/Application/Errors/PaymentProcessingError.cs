using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Errors;

public sealed record PaymentProcessingError(string Description) : Error("PaymentProcessing", Description);
