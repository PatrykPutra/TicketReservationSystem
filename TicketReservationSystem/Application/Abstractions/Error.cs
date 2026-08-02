namespace TicketReservationSystem.Application.Abstractions;

public abstract record Error(string Code, string Description);

public sealed record NoneError() : Error("None", "");
public sealed record NotFoundError(string Description) : Error("NotFound", Description);

public sealed record TicketNotAvailableError(string Description) : Error("TicketNotAvailable", Description);
public sealed record TicketNotReservedError(string Description) : Error("TicketNotReserved", Description);
public sealed record UnauthorizedUserError(string Description) : Error("UnauthorizedUser", Description);
public sealed record CurrencyMismatchError(string Description) : Error("CurrencyMismatch", Description);
public sealed record DuplicatePaymentError(string Description) : Error("DuplicatePayment", Description);

public sealed record InvalidCredentialsError(string Description) : Error("InvalidCredentials", Description);
public sealed record RateLimitedError(string Description) : Error("RateLimited", Description);
public sealed record UserNotFoundError(string Description) : Error("UserNotFound", Description);
public sealed record UserAlreadyExistsError(string Description) : Error("UserAlreadyExists", Description);
