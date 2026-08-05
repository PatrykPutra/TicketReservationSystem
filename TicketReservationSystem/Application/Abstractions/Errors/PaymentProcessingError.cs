namespace TicketReservationSystem.Application.Abstractions;

public sealed record PaymentProcessingError(string Description) : Error("PaymentProcessing", Description);
