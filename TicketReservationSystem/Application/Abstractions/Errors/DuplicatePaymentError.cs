namespace TicketReservationSystem.Application.Abstractions;

public sealed record DuplicatePaymentError(string Description) : Error("DuplicatePayment", Description);
