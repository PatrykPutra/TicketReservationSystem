Status: done

# Spec: API Area Test Coverage

## Problem Statement

Line coverage in the API area is 27.9% (19/68 meaningful lines). Six endpoints/actions
and one middleware have no behavioral tests at all: `AuthenticationController.SendCode`
and `Token`, `EventsController.GetEventById` and `GetEvents`, `WebhooksController.StripeWebhook`,
`ExceptionHandlingMiddleware.InvokeAsync`, plus the ticket-browsing actions
`TicketsController.GetTicketById`/`GetTicketByEvent` (only attribute-reflection tests
exist) and `UserController.AddUser`. A developer cannot run the test suite and get
confidence that the HTTP boundary maps dispatcher outcomes to the correct status codes.

## Solution

Add controller-level unit tests for every uncovered API action and a direct unit test
for the exception-handling middleware, following the existing controller-test convention.
Two one-line production bug fixes are required to satisfy the specified behavior (see
Implementation Decisions). After this lands, API line coverage should exceed 90%.

## User Stories

1. As a developer, I want `SendCode` to return 200 for an existing user, so that the happy path of the auth code flow is verified.
2. As a developer, I want `SendCode` to return 404 when the dispatcher reports a user-not-found error, so that unknown emails surface correctly.
3. As a developer, I want `SendCode` to return 429 when the dispatcher reports a rate-limited error, so that throttling surfaces correctly.
4. As a developer, I want `SendCode` to return 500 for any other dispatch failure, so that unexpected failures are mapped to a server error.
5. As a developer, I want `Token` to return 200 with the issued token and its expiry for valid credentials, so that the token response contract is verified.
6. As a developer, I want `Token` to return 401 for invalid credentials, so that failed authentication is rejected.
7. As a developer, I want `GetEventById` to return 200 with the event for an existing event, so that catalog lookup is verified.
8. As a developer, I want `GetEventById` to return 404 for an unknown event, so that missing events surface correctly.
9. As a developer, I want `GetEvents` to return 200 with the event list, so that catalog listing is verified.
10. As a developer, I want `StripeWebhook` to return 400 for an invalid or missing Stripe signature, so that forged/malformed webhook deliveries are rejected.
11. As a developer, I want `StripeWebhook` to return 200 for a validly signed payload when the command succeeds, so that the full signature-validation + dispatch path is verified.
12. As a developer, I want `StripeWebhook` to return 500 when the dispatched command fails, so that payment-processing errors surface as server errors.
13. As a developer, I want `GetTicketById` to return 200 with the ticket and 404 for a missing ticket, so that anonymous ticket lookup is verified.
14. As a developer, I want `GetTicketByEvent` to return 200 with the ticket list, so that per-event ticket listing is verified.
15. As a developer, I want `AddUser` to return 200 with the created user id, so that user registration is verified.
16. As a developer, I want `AddUser` to return 409 when the email already exists, so that duplicates surface correctly.
17. As a developer, I want `AddUser` to return 500 for any other dispatch failure, so that unexpected failures are mapped to a server error.
18. As a developer, I want the exception middleware to pass requests through untouched when no exception is thrown, so that the happy path is unaffected.
19. As a developer, I want the exception middleware to respond 500 with an `application/problem+json` body when a downstream exception is thrown, so that unhandled exceptions produce a consistent error contract.
20. As a developer, I want every new test named `Method_Scenario_ExpectedResult`, so that the naming convention stays uniform across the suite.

## Implementation Decisions

- **Seam — controller level.** All controller tests construct the controller directly with
  mocked `ICommandDispatcher`/`IQueryDispatcher` over a `DefaultHttpContext`. This is the
  existing prior-art seam (`PaymentsControllerTests`, `TicketsControllerTests`,
  `UserControllerTests`) and the highest seam that can exercise request→status-code mapping
  without an HTTP host.
- **Seam — middleware.** `ExceptionHandlingMiddleware` is tested directly: instantiate with a
  `RequestDelegate` stub and `DefaultHttpContext` whose `Response.Body` is a `MemoryStream`.
  Not reachable via the controller seam, so a second, direct unit-test seam is used.
- **Dispatcher stubbing.** Success paths stub `DispatchAsync<TCommand, TResponse>`/`ExecuteAsync`
  with the concrete result types (`SendAuthenticationCodeResult.Success()`,
  `GenerateTokenResult.Success(token, expiresAt)`, `GetEventByIdResult`, `GetEventsResult`,
  `GetTicketByIdResult`, `GetTicketsByEventResult`, `AddUserResult.Success`, `Result.Success()`
  for the webhook); failure paths stub the concrete error records (`UserNotFoundError`,
  `RateLimitedError`, `InvalidCredentialsError`, `NotFoundError`, `UserAlreadyExistsError`).
- **Webhook signature.** `StripeWebhook` happy/failure tests compute a real Stripe-compatible
  `Stripe-Signature` header — `t=<unix-seconds>,v1=<hex>` where the hex is an
  HMAC-SHA256 over `"{timestamp}.{payload}"` keyed with the webhook secret — so
  `EventUtility.ConstructEvent` passes. The payload is a minimal valid event JSON. The
  controller receives `Options.Create(new StripeSettings { WebhookSecret = ... })` and a
  `DefaultHttpContext` with `Request.Body` set to the payload stream and the header added.
- **Test files.** Four new files (`AuthenticationControllerTests`, `EventsControllerTests`,
  `WebhooksControllerTests`, `ExceptionHandlingMiddlewareTests`) and three extended
  (`TicketsControllerTests` gains a query-dispatcher setup helper and two GET-action tests plus
  `Reserve` error-mapping tests; `UserControllerTests` gains a command-dispatcher setup helper and
  `AddUser` tests; `PaymentsControllerTests` gains `CreateCheckout` error-mapping tests). The
  `Reserve`/`AddUser`/`CreateCheckout` error-path tests were required by the ≥90% coverage gate:
  the two `ErrorToActionResult` switches in `TicketsController`/`PaymentsController` and the
  corresponding branches in `UserController` were the only uncovered API lines.
- **Production bug fixes (required by user stories).** Two one-line production changes were
  needed for the specified behavior — they are the only deviations from "no production changes":
  - `ExceptionHandlingMiddleware`: `ContentType` is now set via the `WriteAsJsonAsync`
    `contentType` argument instead of a post-write assignment. The original code set the header
    then overwrote it (the write forces `application/json; charset=utf-8`), so US-19's
    "respond 500 with an `application/problem+json` body" was not satisfied; a post-write
    assignment would also throw under a real host where the response has already started.
  - `WebhooksController.StripeWebhook`: a missing/empty `Stripe-Signature` header previously
    caused an unhandled `NullReferenceException` inside `EventUtility.ConstructEvent` (not a
    `StripeException`), surfacing as 500. The added guard returns 400, satisfying US-10
    ("400 for an invalid or missing Stripe signature").

## Testing Decisions

- **What makes a good test:** assert only external behavior — the returned `IActionResult`
  type and, where relevant, the serialized payload — never dispatcher internals or how the
  command/query is constructed.
- **Modules tested:** `AuthenticationController`, `EventsController`, `WebhooksController`,
  `TicketsController` (GET actions + `Reserve` error mapping), `UserController` (`AddUser`),
  `PaymentsController` (`CreateCheckout` error mapping), `ExceptionHandlingMiddleware`.
- **Prior art:** `PaymentsControllerTests` / `TicketsControllerTests` / `UserControllerTests`
  (controller seam, `CreateController` helper + `SetAuthenticatedUser`); `StripeWebhookHandlerTests`
  (Stripe event construction).
- **Coverage gate:** re-run `dotnet test --collect:"XPlat Code Coverage"` after implementation;
  API-area line coverage should rise from 27.9% to ≥90%. The suite must remain green.
- **New test names** follow `Method_Scenario_ExpectedResult` (e.g.
  `SendCode_WhenRateLimited_Returns429`, `StripeWebhook_WhenSignatureInvalid_ReturnsBadRequest`,
  `InvokeAsync_WhenNextThrows_Sets500ProblemDetails`).

## Out of Scope

- HTTP integration tests via `WebApplicationFactory`.
- Testing the Application handlers/queries that the controllers dispatch (separate work).
- Raising coverage in Domain/Application/Infrastructure areas.
- Adding a coverage-threshold build gate or CI coverage workflow.
- Any production behavior changes beyond the two documented bug fixes above.

## Further Notes

- Repo tracker is local markdown; this spec is published at `.scratch/api-test-coverage/spec.md`
  with `Status: done`.
- Baseline: API 27.9% line (19/68); target ≥90%.
- Verification gate: `dotnet test TicketReservationSystem.slnx` green, then coverage re-run.
