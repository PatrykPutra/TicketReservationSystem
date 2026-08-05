Status: ready-for-agent

# Spec: Core Refactor — Payments, Webhooks, Tickets & Infrastructure Layering

## Problem Statement

The codebase has accumulated technical debt across several areas: error types and handler constants are duplicated/centralized awkwardly; global exception handling is absent (controller-level error mapping has drifted apart); the Infrastructure layer mixes services with cross-cutting concerns and leaks abstractions into Application; Stripe payment logic hardcodes currency assumptions; the Payment entity is coupled to a single provider; the webhook handler's success-only return value is misleading; and ticket confirmation can happen outside the payment flow.

## Solution

Restructure errors, configuration, layers, and the payment/webhook/ticket flows so that: (1) one type lives per file and new error types are cheap to add; (2) unhandled exceptions are handled uniformly; (3) Infrastructure is organized by service and only Application owns business abstractions; (4) Stripe payment handling validates currency and converts to minor units per-currency; (5) the Payment entity is provider-agnostic; (6) webhooks are a first-class endpoint that reports failures honestly; and (7) ticket confirmation happens only when payment completes.

## User Stories

1. As a developer, I want each error type in its own file under one folder, so that I can find and extend error types without opening a large monolithic file.
2. As a developer, I want an `UnsupportedCurrencyError` distinct from `CurrencyMismatchError`, so that config/Stripe-unsupported currency is distinguishable from a Money/app currency mismatch.
3. As a developer, I want the send-authentication-code handler's magic numbers as local constants, so that the rate-limit window, code range, and code lifetime are named and single-sourced.
4. As a user receiving an auth email, I want the stated code expiry to match the configured lifetime, so that I'm never told a wrong expiry.
5. As a developer, I want the auth code entity and repository renamed from `EmailVerificationCode` to `VerificationCode`, so that the domain language doesn't imply a delivery channel.
6. As a developer, I want any unhandled exception to return a generic 500 response, so that clients never see stack traces and all failures look consistent.
7. As a developer, I want exceptions-only global middleware, so that controllers keep their existing Result-error mapping.
8. As a developer, I want Infrastructure services (Email, Jobs, InMemory, Payments) grouped under a `Services` folder, so that the layer structure is navigable.
9. As a developer, I want the JWT/Authentication infrastructure moved to the Application layer, so that Application doesn't depend on Infrastructure for token generation.
10. As a developer, I want domain event handlers living in Application, so that business reactions to events are close to the events they handle.
11. As a developer, I want the `IEmailSender` abstraction in Application, so that Application handlers depend only on Application abstractions.
12. As a developer, I want StripePaymentsService to verify the Money currency matches the configured app currency before charging, so that no payment is created in the wrong currency.
13. As a developer, I want StripePaymentsService to fail explicitly on an unsupported/unknown currency, so that misconfiguration surfaces loudly instead of silently producing wrong amounts.
14. As a developer, I want minor-unit conversion to be per-currency, so that zero-decimal currencies (e.g. JPY) are charged correctly.
15. As a developer, I want the Payment entity to store a `PaymentProvider` enum and a generic `ExternalId`, so that Payment is not coupled to Stripe's session concept.
16. As a developer, I want `SetStripeSessionId` replaced with `SetExternalId`, so that the entity API is provider-agnostic.
17. As a developer, I want the webhook handler to return a failed result when it structurally cannot handle an event, so that the controller can signal Stripe to retry.
18. As a developer, I want idempotent redelivery and unknown event types to remain successes, so that normal Stripe behavior isn't treated as an error.
19. As a developer, I want a dedicated `WebhooksController` at `webhooks/stripe`, so that webhook endpoints are separated from the payment checkout API.
20. As a developer, I want the old `api/payments/webhook` endpoint removed, so that there is exactly one webhook route.
21. As a user, I want ticket confirmation to occur only via payment completion, so that paid tickets are confirmed and free/unpaid confirmation is impossible.

## Implementation Decisions

- **Error organization:** abstract `Error` stays; each sealed error record moves to its own file in an `Errors` folder under Application.Abstractions, keeping the `...Abstractions` namespace so existing usings are unaffected. Two new errors are added: `PaymentProcessingError` (code `PaymentProcessing`) and `UnsupportedCurrencyError` (code `UnsupportedCurrency`).
- **Magic numbers:** the send-auth-code handler defines private constants for the rate-limit window (60s), code range (100000–999999), and code lifetime (5 min). The email body's expiry sentence is derived from the same lifetime constant.
- **Rename:** `EmailVerificationCode` → `VerificationCode` cascades to `VerificationCodeId`, `IVerificationCodeRepository`, the implementation, the EF DbSet `VerificationCodes` + converter/config, DI registration, handlers, and tests. The `AuthenticationCodeGeneratedEvent` name is unchanged.
- **Global middleware:** a single exception-handling middleware catches all unhandled exceptions, logs them, and returns HTTP 500 with a ProblemDetails body containing a generic message ("An unexpected error occurred"). It is registered first in the pipeline. Controllers retain their `ErrorToActionResult` mapping for `Result` failures.
- **Infrastructure layering:** new `Services` folder with `Email`, `Jobs`, `InMemory`, `Payments` subfolders (namespaces become `...Infrastructure.Services.*`). `Authentication` moves to Application. `DomainEventHandlers` moves to Application. `IEmailSender` moves to Application.Abstractions; `EmailSettings` and `MimeKitEmailSender` stay in Infrastructure. `DomainEventsDispatcher`, `Persistence`, and `Repository` remain at Infrastructure root. All affected usings/registrations updated.
- **Currency check:** `IPaymentsService.CreateCheckoutSessionAsync` returns `Result<CreateCheckoutSessionResult>`. On Money/currency mismatch the service returns a failed `CurrencyMismatchError`; `CreateCheckoutHandler` propagates it into `CreateCheckoutResult`.
- **Per-currency minor units:** StripePaymentsService keeps a static known-currency lookup (Stripe's full list; zero-decimal → 1, 2-decimal → 100). Unknown currency → failed `UnsupportedCurrencyError`. `UnsupportedCurrencyError` maps to 400 and `PaymentProcessingError` to 500 in the controller error switches.
- **Payment entity:** `StripeSessionId` and `SetStripeSessionId` are removed. A `PaymentProvider` enum (single `Stripe` member, new file) is added via constructor parameter. A string `ExternalId` with a `SetExternalId(string)` method (preserving `ModifiedAt` behavior) replaces the Stripe session id. EF mapping updated (ExternalId maxlength 200, PaymentProvider stored as string).
- **Webhook handler:** returns `Result.Failure(PaymentProcessingError)` when the event object is not a Session, the `ClientReferenceId` isn't a parseable payment id, or no matching payment exists. Idempotent no-ops (payment not pending) and unknown event types still return success. Exceptions continue to bubble to the middleware.
- **Webhooks controller:** new controller at route `webhooks`, action `StripeWebhook` at `POST webhooks/stripe`, anonymous, with Stripe signature verification moved from the payments controller. Handler failure → 500 (Stripe retries). The old `api/payments/webhook` endpoint is removed.
- **Ticket confirmation removal:** the confirm endpoint and the full confirm CQRS pipeline (command, handler, result, response, request) are deleted. `Ticket.Confirm()` and `TicketConfirmedEvent` remain for the webhook-driven flow. The tickets controller's error switch is cleaned of the now-unused currency case.

## Testing Decisions

- **What makes a good test:** assert external behavior through public seams (handler results, entity state/events, service results), not implementation details.
- **Seam 1 — handler level (existing pattern):** real InMemory `ApplicationDbContext` + real repositories with external services mocked. Covers `CreateCheckoutHandler` propagating `CurrencyMismatchError`/`UnsupportedCurrencyError` from a mocked `IPaymentsService`, and `StripeWebhookHandler` failure paths (non-Session object, bad `ClientReferenceId`, missing payment → `PaymentProcessingError`) plus unchanged idempotent/unknown-type successes. Prior art: `PaymentCheckoutHandlerTests`, `StripeWebhookHandlerTests`.
- **Seam 2 — service level (new):** `StripePaymentsService` tested directly. Currency mismatch → `CurrencyMismatchError` failure; unknown currency → `UnsupportedCurrencyError` failure; known currencies → correct `UnitAmount` for zero-decimal (JPY, divisor 1) and 2-decimal (PLN, divisor 100). Enabling change: `SessionService` becomes constructor-injectable (default `new SessionService()`).
- **Updated existing tests:** `PaymentTests` (SetExternalId), `PaymentCheckoutHandlerTests` (`Result<CreateCheckoutSessionResult>` mock, `ExternalId`), `StripeWebhookHandlerTests` (`SetExternalId` seed), plus any test referencing renamed/removed symbols.
- **Not tested:** controller error-switch mappings (no controller-test prior art in this repo).

## Out of Scope

- Controller-level/integration tests (no existing seam).
- Persistence migrations (InMemory database).
- Adding new payment providers.
- Replacing the `Result` pattern or introducing a full exception-based error model.
- Non-Stripe webhook event types.

## Further Notes

- All namespaces/usings updated across Program.cs, DependencyInjection files, controllers, handlers, and tests as a result of the layer moves.
- The 500 body is intentionally generic and identical for every exception category (per decision).
- Verification: `dotnet build` and `dotnet test` must pass.
