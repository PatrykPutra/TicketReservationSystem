Status: ready-for-agent

# Spec: Controller Authorization & Errors Layer Cleanup

## Problem Statement

Five issues from the backlog, all in the API/Application layer:

1. **Anonymous ticket browsing is blocked.** `GetTicketByEvent` and `GetTicketById` require authentication, so clients cannot browse the catalog before logging in.
2. **IDOR on ticket reserve/cancel.** `Reserve`/`Cancel` trust the `UserId` in the request body rather than the authenticated caller's identity, so any caller can reserve or cancel tickets on behalf of other users.
3. **Ambiguous ticketId contract.** `Reserve`/`Cancel` ignore the `TicketId` in the request body and process only the path parameter; a request whose body and path disagree is silently processed.
4. **IDOR on profile fetch.** `GetUser` lets any authenticated caller read any user's profile by supplying a `userId`.
5. **Misaligned folder structure.** The `Errors` folder lives nested under `Application/Abstractions` even though error types are not abstractions; the namespace (`Application.Abstractions`) also doesn't match the folder.

## Solution

Allow anonymous access to the two ticket-browsing endpoints; enforce identity at the controller boundary — the caller's JWT `userId` claim must match the request's `UserId`, and the request body's `TicketId` must match the path parameter — returning 401 for identity mismatches and 400 for ticketId mismatches; reuse one shared claim-reading helper across all controllers (including the existing payment checkout, which currently has a latent claim-lookup bug); and extract the `Errors` folder to sit directly under `Application`, renaming its namespace and updating consumers.

## User Stories

1. As a user browsing the catalog, I want to fetch tickets for an event without logging in, so that I can discover events before authenticating.
2. As a user browsing the catalog, I want to fetch a single ticket by id without logging in, so that I can view ticket details before authenticating.
3. As an authenticated user, I want to reserve a ticket only for my own account, so that no one else can reserve tickets on my behalf.
4. As an authenticated user, I want to cancel a reservation only for my own account, so that no one else can cancel my tickets.
5. As an authenticated user, I want the API to reject a reserve/cancel request whose body `TicketId` differs from the URL path, so that the API contract is unambiguous and nothing is silently processed.
6. As an authenticated user, I want to fetch only my own profile, so that other users' personal data cannot be read by guessing ids.
7. As a developer, I want a single shared helper for reading the authenticated user id from the JWT, so that every controller reads the claim identically.
8. As a developer, I want the claim lookup to be resilient to ASP.NET's default inbound claim-name remapping, so that auth checks work regardless of JWT mapping configuration.
9. As a developer, I want the payment checkout to use the same claim helper, so that the claim-check pattern is consistent and the existing latent bug (the `sub` claim is never found) is fixed.
10. As a developer, I want an identity mismatch to return 401 Unauthorized, so that clients can react to auth failures correctly.
11. As a developer, I want a ticketId path/body mismatch to return 400 Bad Request, so that malformed requests surface as validation errors.
12. As a developer, I want the `Errors` folder directly under `Application`, so that the folder structure reflects the intended layering.
13. As a developer, I want error types in an `Application.Errors` namespace, so that namespaces match the folder structure.
14. As a developer, I want the extraction to preserve all existing behavior, so that only structure changes, not semantics.
15. As a developer, I want controller-level tests for the verification paths, so that the security checks are regression-protected.

## Implementation Decisions

- **Shared claim helper (API layer).** A single helper on the `ClaimsPrincipal` exposes the authenticated user id. It reads the `ClaimTypes.NameIdentifier` claim first and falls back to the raw `sub` claim, because `JwtBearerOptions.MapInboundClaims` defaults to `true` and remaps `sub` to the long nameidentifier URI. The result is parsed as a GUID; failure to parse or find a claim means "no authenticated user id".
- **Verification location.** All checks live at the controller boundary, in front of command/query dispatch. The command/query pipeline is unchanged.
- **Status codes.** Identity mismatch (missing/malformed claim, or claim ≠ request `UserId`) → `401 Unauthorized`. Body `TicketId` ≠ path `ticketId` → `400 Bad Request`.
- **Request contracts unchanged.** `TicketReservationRequest` and `TicketCancelationRequest` keep both `UserId` and `TicketId` fields; the fields are validated and then passed through to the commands as today. No schema or response-shape changes.
- **TicketsController.** `[AllowAnonymous]` is added to the two catalog GET endpoints; the controller-level `[Authorize]` remains so `Reserve`/`Cancel` stay authenticated. `Reserve` and `Cancel` gain the ticketId check (400) and the claim check (401).
- **UserController.** `GetUser` gains the claim check (path `userId` must equal claim → else 401); `NotFound` for a missing user is retained, as is the existing `[Authorize]` on the action.
- **PaymentsController.** The checkout action is refactored to use the shared helper instead of its inline `FindFirst("sub")` lookup, fixing the latent always-401 behavior under default claim mapping.
- **Errors extraction.** The `Errors` folder moves from under `Abstractions` to directly under `Application`. Each error type's namespace changes to `TicketReservationSystem.Application.Errors`; each error file gains a using for the base `Error` record, which stays in `Application.Abstractions` along with `Result`/`Result<T>`. All consumer files (handlers, controllers, infrastructure services, and tests) gain a per-file using for the new namespace. No `.csproj` or DI changes are required (SDK-style globbing).
- **Behavioral fix note.** The PaymentsController change is a deliberate behavior correction (from "always 401" to actually authenticating).

## Testing Decisions

- **What makes a good test:** assert external behavior — the HTTP status code each verification path returns — through the controller seam, not the internals of the helper or dispatcher.
- **Seam — controller level (new).** Controllers are constructed directly with mocked `IQueryDispatcher`/`ICommandDispatcher` and a `ClaimsPrincipal` built over a `DefaultHttpContext` carrying a `NameIdentifier` claim. This is the only seam that can exercise the claim/ticketId verification, since the logic lives at the controller boundary. It is a new seam (the repo has no controller-test prior art; the existing suite tests handlers), introduced deliberately because the behavior under test lives here.
- **Prior art:** handler-level tests (`AddUserHandlerTests`, `PaymentCheckoutHandlerTests`, etc.) show the Mock + InMemory patterns; the new controller tests reuse Moq but target controllers.
- **Cases covered:** `Reserve` (body ticketId ≠ path → 400; claim ≠ body userId → 401; missing/malformed claim → 401; matching → 200), `Cancel` (same set), `GetUser` (path userId ≠ claim → 401; match + user found → 200; match + user missing → 404), `CreateCheckout` (mismatch → 401; match → 200).
- **AllowAnonymous:** verified by reflecting on the two action methods' attributes.
- **Errors extraction:** verified by `dotnet build` + the existing suite passing with the new usings.
- **New test names** follow the `MethodName_Scenario_ExpectedResult` convention (e.g. `Reserve_WhenTicketIdMismatchesPath_ReturnsBadRequest`).

## Out of Scope

- The remaining backlog items (JSON serialization of `Money`/`DateTimeRange`, event handlers and welcome/confirmation emails, test-naming convention cleanup across existing tests, general test-coverage improvements, lowercase route naming).
- Integration tests via an HTTP test host (`WebApplicationFactory`).
- Moving the verification into the command/query pipeline or handlers.
- Changing JWT configuration (`MapInboundClaims` stays at its default).
- Renaming existing tests or changing `Result`/`Error` semantics.

## Further Notes

- The repo's tracker is local markdown; this spec is published at `.scratch/api-authorization/spec.md` with `Status: ready-for-agent`.
- The `Errors` namespace change is the churn-heavy part: it touches ~19 files (source + tests) with mechanical `using` additions; the helper and controller changes are localized.
- Verification gate: `dotnet build` and `dotnet test` must pass.
