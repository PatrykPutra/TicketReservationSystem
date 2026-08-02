## Problem Statement

Controllers in TicketsController and UserController always return 200 OK on command success, even when a domain invariant is violated — domain exceptions bubble up as unhandled 500 errors. There is no uniform way for controllers to determine the appropriate HTTP response code based on the outcome of a command. The command dispatcher has no error-handling layer, so every command handler either throws or returns a success DTO with no failure representation.

## Solution

Introduce a Result<T> type in the Application layer. Change ICommandDispatcher.DispatchAsync to return Result<T> with a two-parameter generic signature (TCommand, TValue). The dispatcher wraps every mediator.Send call in try/catch: on success it returns Result<T>.Success(value), on each domain exception it returns Result<T>.Failure(TypedError). Controllers inspect result.IsFailure and pattern-match result.Error to determine the HTTP response code. Domain exceptions become public so the dispatcher can catch them by type.

## User Stories

1. As a controller author, I want command handlers to never throw domain exceptions at the HTTP layer, so that unhandled exception middleware is not the primary error path.
2. As a controller author, I want to inspect a single result object after dispatching a command, so that I can decide the HTTP response code without try/catch.
3. As a controller author, I want result.Error to be a typed record that tells me exactly which domain rule was violated, so that I can map it to the correct status code (409 Conflict, 401 Unauthorized, 400 Bad Request, 404 Not Found).
4. As a controller author, I want the Success path to carry the original command result DTO unchanged, so that existing serialization contracts are preserved.
5. As a developer, I want command handlers to remain focused on business logic and not have to wrap their return values in Result or catch exceptions, so that the handler implementation stays clean.
6. As a domain layer owner, I want domain exceptions to remain the mechanism for expressing invariant violations inside entities, so that the domain model does not depend on the Application Result type.
7. As a maintainer, I want no reflection in the error-handling path, so that the code is simple and performant.
8. As a maintainer, I want the dispatcher generic signature to carry both the command type and the inner value type, so that compiler-inferred generics eliminate reflection entirely.
9. As a consumer of TicketsController, I want to receive a 409 Conflict when trying to reserve a ticket that is not available, rather than a 500 Internal Server Error.
10. As a consumer of TicketsController, I want to receive a 401 Unauthorized when trying to confirm or cancel a ticket reserved by another user, rather than a 500 Internal Server Error.
11. As a consumer of TicketsController, I want to receive a 404 Not Found when trying to reserve/confirm/cancel a non-existent ticket, rather than a 500 Internal Server Error.
12. As a consumer of UserController, I want to receive a 400 Bad Request when user-registration invariants are violated, rather than a 500 Internal Server Error.
13. As a maintainer, I want queries to remain unaffected by this change, so that query handlers are not forced into the Result pattern.

## Implementation Decisions

### Result type hierarchy

A non-generic Result class with IsSuccess, IsFailure, Error properties, and a generic Result<T> subclass carrying Value. Both use private constructors and static factory methods (Success(), Failure(Error)). Argument validation ensures that a success result never carries an error and vice versa.

### Dispatcher becomes the wrapping boundary

ICommandDispatcher.DispatchAsync changes its generic signature from DispatchAsync<TCommand, TResult> to DispatchAsync<TCommand, TValue> and returns Task<Result<TValue>>. The implementation wraps mediator.Send(command) in try/catch. Each domain exception type is caught individually and mapped to a typed Error record via Result<TValue>.Failure(...). On success, Result<TValue>.Success(value) wraps the handler's return value. No reflection is needed because TValue is a compiler-inferred generic parameter.

Command handlers remain unchanged — they still return raw value DTOs (e.g., TicketReservationResult) and throw domain exceptions as before. The dispatcher is the only new try/catch boundary.

### Typed Error records per domain exception

Each domain exception has a corresponding Error record in Application/Abstractions/Error.cs. HTTP status resolution stays in the controller — the dispatcher never references HTTP codes.

### DomainException visibility

DomainException and all 11 subtypes change from internal to public so the dispatcher (in the Application layer) can catch them by type. No structural changes to the exception classes other than the access modifier.

### Controller response logic

Controllers switch on result.Error after checking result.IsFailure. On success, result.Value is passed to Ok(). The UserController.AddUser endpoint changes from CreatedAtAction to Ok(result.Value).

### Controllers affected

- TicketsController — Reserve, Confirm, Cancel endpoints (GET endpoints use query dispatcher, unchanged)
- UserController — AddUser endpoint (GET endpoint uses query dispatcher, unchanged)
- AuthenticationController — not in scope
- EventsController — GET only, not in scope

### Queries

Unaffected. Query handlers and IQueryDispatcher / QueryDispatcher remain as-is.

## Testing Decisions

No existing test infrastructure is present in the repo. Testing the Result pattern is out of scope for this spec — it may be introduced as a separate ticket.

## Out of Scope

- Tests (no test project exists yet)
- Query result pattern — queries remain unchanged
- AuthenticationController — unchanged
- EventsController — unchanged
- Reflection-based alternatives — excluded by design
- Domain event handlers, SignalR hub, reservation timeout — unrelated to this change
- Pipeline behaviors / MediatR middleware — not used for this pattern
- Error response body shape — no API contract change for error payloads is specified in this scope

## Further Notes

- The UserController.AddUser currently returns CreatedAtAction(nameof(GetUser), ...). With "always 200 OK on success" it returns Ok(result.Value). The CreatedAt semantics are dropped as part of this change.
- No changes to Program.cs or DI registration — dispatchers are already registered via AddApplication().
