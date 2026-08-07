using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Errors;

public sealed record DuplicatePaymentError(string Description) : Error("DuplicatePayment", Description);
