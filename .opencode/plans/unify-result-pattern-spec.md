# Unify Result Pattern in CQRS Pipeline

## Problem Statement

The CQRS pipeline has three different, inconsistent error-handling patterns across modules:

1. **Authentication module** — each XxxResult class duplicates its own IsSuccess flag and string-based ErrorCode. Controllers must check both the dispatcher-level Result.IsFailure AND the result-level Value.IsSuccess/IsAuthenticated/ErrorCode, creating a confusing double-check pattern.

2. **Tickets module** — handlers throw domain exceptions (TicketStatusException, KeyNotFoundException) that the CommandDispatcher catches and maps to Error types. This uses exceptions for normal business flow control.

3. **Users module** — the AddUserResult is a plain record that always succeeds, with no way to represent failure.

No XxxResult class inherits from the existing Result or Result<T> base class. The CommandDispatcher wraps each handler's return value in Result<T>.Success(xxxResult), creating a pointless extra wrapping layer. The base Result<T> class is only used by the dispatcher, never by the handlers themselves.

## Solution

Make each Command XxxResult class directly inherit Result<TPayload>, where TPayload is a dedicated DTO record holding the command's output data. Handlers return the XxxResult directly, eliminating dispatcher wrapping. All error cases are represented as Error record instances from the existing Error hierarchy (plus new types). The CommandDispatcher becomes a pure pass-through. Controllers check a single esult.IsFailure and map esult.Error to the appropriate HTTP response.

## User Stories

1. As a developer, I want all CommandHandlers to return a consistent Result<TPayload> type, so that error handling follows one pattern across the entire codebase.
2. As a developer, I want the CommandDispatcher to pass through handler results without wrapping, so that there is no double-wrapping of results.
3. As a developer, I want domain errors to be returned as Result.Failure(), so that business flow control does not use exceptions.
4. As a developer, I want each command's output data to be defined as a dedicated DTO record, so that the payload shape is explicit and independent of the result wrapper.
5. As a developer, I want the ICommand<TResponse> interface to constrain TResponse : Result, so that the type system enforces the pattern.
6. As a developer, I want new Error types added to the Error hierarchy for auth-specific failures, so that controllers can map them to HTTP status codes consistently.
7. As a developer, I want AddUserHandler to reject duplicate emails, so that the system maintains email uniqueness.
8. As a developer, I want controllers to check a single esult.IsFailure and map esult.Error to an HTTP response, so that the controller logic is unified across modules.
9. As a developer, I want existing handler tests to continue passing after the refactoring (with updated assertions), so that the behavioral contract is preserved.
10. As a developer, I want a test for the new duplicate-email guard in AddUserHandler, so that the new business rule is covered.

## Implementation Decisions

### Inheritance Model

Each XxxResult class inherits from Result<TPayload> where TPayload is a dedicated ecord type in the same namespace:

`
AuthenticationResult : Result<AuthenticationResponse>
GenerateTokenResult   : Result<TokenResponse>
SendAuthenticationCodeResult : Result<SendAuthenticationCodeResponse>
TicketReservationResult     : Result<TicketReservationResponse>
TicketConfirmationResult    : Result<TicketConfirmationResponse>
TicketCancelationResult     : Result<TicketCancelationResponse>
AddUserResult               : Result<AddUserResponse>
`

### DTO Records

Each command gets a response record in its own folder. All payloads are wrapper records (no raw value types), for consistency:

| Record | Fields |
|--------|--------|
| AuthenticationResponse | UserId? UserId |
| TokenResponse | string Token, DateTime ExpiresAt |
| SendAuthenticationCodeResponse | *(empty record, no flags)* |
| TicketReservationResponse | TicketId Id, TicketStatus Status, DateTime ReservedAt |
| TicketConfirmationResponse | TicketId Id, TicketStatus Status, DateTime ConfirmedAt |
| TicketCancelationResponse | TicketId Id, TicketStatus Status |
| AddUserResponse | UserId Id |

### Result<T> Constructor Accessibility

The Result<T> constructors change from private to protected to allow subclassing.

### Error Hierarchy Additions

Three new Error records added to Application\Abstractions\Error.cs:

| Error | HTTP Mapping |
|-------|-------------|
| InvalidCredentialsError | 401 Unauthorized |
| UserNotFoundError | 404 Not Found |
| RateLimitedError | 429 Too Many Requests |
| UserAlreadyExistsError | 409 Conflict |

### Interface Constraints

- ICommand<out TResponse> changes from where TResponse : notnull to where TResponse : Result
- ICommandHandler<in TCommand, TResponse> follows the same constraint
- ICommandDispatcher changes from Task<Result<TValue>> DispatchAsync<TCommand, TValue>() to Task<TResponse> DispatchAsync<TCommand, TResponse>() with where TResponse : Result

### CommandDispatcher Simplification

The dispatcher removes its entire try/catch block and becomes:

`
Task<TResponse> DispatchAsync<TCommand, TResponse>(...) =>
    mediator.Send(command, cancellationToken);
`

All domain error handling moves into handlers. Unexpected infrastructure exceptions bubble up to ASP.NET middleware as 500s.

### Handler Changes

- **Auth handlers**: Replace custom factory calls (GenerateTokenResult.InvalidCredentials()) with 
ew GenerateTokenResult(new InvalidCredentialsError(...)). Replace custom IsSuccess bool returns with Result.Failure(new XxxError()) / Result.Success(new Dto(...)).

- **Ticket handlers**: Replace 	hrow statements with eturn new XxxResult(new XxxError(...)). Handlers check preconditions before calling domain methods that can throw (e.g., check ticket state before calling 	icket.Confirm()).

- **AddUserHandler**: Add guard — query by email first, return UserAlreadyExistsError if found, otherwise proceed with creation.

### Controller Simplification

Controllers use a single esult.IsFailure check and switch on esult.Error to produce HTTP responses. No double-checking of esult.Value.IsSuccess/IsAuthenticated/ErrorCode.

The TicketsController and UserController ErrorToActionResult methods are updated to include the new error types.

## Testing Decisions

### Testing Philosophy

Tests should verify external behavior of handlers (correct result type for each input), not implementation details (which error constructor was called).

### Test Seam: Handler Level

Tests call handler.Handle() directly, the same pattern as existing AuthenticationHandlerTests.cs. This is the highest seam that tests the behavioral contract without requiring HTTP infrastructure.

### Tests to Update

| Test File | Changes |
|-----------|---------|
| AuthenticationHandlerTests.cs | Replace esult.ErrorCode assertions with Assert.IsType<XxxError>(result.Error). Replace esult.Token with esult.Value.Token. Replace esult.IsSuccess with inherited property (same name, no change needed). |
| TicketReleaseReservationTests.cs | No changes (tests domain entity behavior, not handler results). |

### Test to Add

| Test | Handler | Scenario |
|------|---------|----------|
| AddUserHandler_duplicate_email_returns_error | AddUserHandler | Creating a user with an existing email returns UserAlreadyExistsError failure. |

## Out of Scope

- Query results (GetTicketByIdResult, GetEventsResult, GetUserResult, etc.) — they remain standalone classes without Result inheritance.
- CommandDispatcher exception catch for domain exceptions — removed entirely. Infrastructure exceptions (DbUpdateConcurrencyException) are not caught by the dispatcher and bubble to middleware.
- ExpiredReservationsCleanupJob — not touched.
- EventsController — not touched (uses query pipeline only).
- API contracts — response shapes change slightly (nested DTOs) but the JSON serialization produces the same fields since the DTOs mirror the old result properties.

## Further Notes

- The Result<T> class's Failure() static factory returns Result<T>, not the specific subclass. Handlers use the subclass constructor directly for failure cases: 
ew GenerateTokenResult(new InvalidCredentialsError(...)).
- Domain entity methods (e.g., 	icket.Cancel(), 	icket.Confirm()) may still throw domain exceptions as internal invariant guards. Handlers must check preconditions before calling these methods to prevent unhandled exceptions from propagating.
- The UserController.ErrorToActionResult method gains a UserAlreadyExistsError => Conflict() branch.
- The AuthenticationController loses its alue.ErrorCode string switch and gains an Error type switch across UserNotFoundError, RateLimitedError, and InvalidCredentialsError.
