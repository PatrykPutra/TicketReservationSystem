Status: ready-for-agent
Type: task

# Spec: Stripe Checkout Payment Service

## Problem Statement

Users can reserve and confirm tickets, but there is no way to collect payment. A user reserves a ticket but the flow stops at `Reserved` — nothing moves it to `Confirmed`, and nothing requires or records money. Buyers want to pay online.

## Solution

Add a Stripe-hosted checkout flow. A user reserves a ticket, then starts a Stripe Checkout Session for it. Stripe collects payment; a webhook event confirms success and the system confirms the ticket. Failed/expired payments release the ticket back to `Available`. Payments are recorded as first-class entities alongside tickets, following the project's existing CQRS + Result architecture.

Scope is exactly **two endpoints**: create-checkout (user-facing) and the Stripe webhook (verification). Payment success is determined exclusively by Stripe's webhook — there is no client-facing "verify" call.

## User Stories

1. As a buyer, I want to pay for a reserved ticket through Stripe, so that I complete my purchase.
2. As a buyer, I want to create a checkout session only for a ticket I've already reserved, so that the purchase is tied to my reservation.
3. As a buyer, I want the checkout to redirect me to Stripe's hosted page, so that I can pay securely.
4. As a buyer, I want my ticket confirmed when payment succeeds, so that my reservation becomes final.
5. As a buyer, I want my ticket returned to `Available` if my payment fails or expires, so that I can retry or others can buy it.
6. As a buyer, I want my payment identity and session traceable, so that I can relate a payment to my reservation and user.
7. As a system, I want payment success determined by Stripe's webhook rather than client polling, so that verification is trustworthy.
8. As a system, I want the webhook endpoint protected by Stripe's signature rather than user auth, so that only Stripe can deliver events to it.
9. As a system, I want duplicate/re-delivered webhook events to be no-ops, so that confirmations are idempotent.
10. As a system, I want a buyer prevented from creating multiple active checkout sessions for the same ticket, so that a ticket isn't double-booked.
11. As a system, I want stale `Pending` payments marked `Expired` automatically, so that abandoned checkouts are cleaned up.
12. As a system, I want the existing reservation-expiry job to not release a ticket that has an in-flight payment, so that a payment isn't invalidated mid-checkout.
13. As a system, I want the successful payment to also confirm the reserved ticket, so that ticket and payment stay consistent.
14. As a developer, I want the payment flow expressed as commands/handlers returning Results, so that it matches the existing CQRS patterns.
15. As a developer, I want the payment entity to raise domain events on completion/failure, so that future side effects can hook in.

## Implementation Decisions

- **New `Payment` aggregate root** owning a `PaymentId`, referencing a `TicketId`, `UserId`, a snapshot `Money` (amount + currency captured at creation), a Stripe session id, a status, and timestamps. Statuses: `Pending → Completed / Failed / Expired`.
- **Payment status model:** `Pending` created with the session; `Completed` (paid); `Failed`; `Expired`. `Expired` applies to abandoned/stale sessions.
- **Domain events:** `PaymentCompletedEvent`, `PaymentFailedEvent` raised on transitions, mirroring ticket events; raised even if no consumer handles them yet.
- **One ticket per Checkout Session.** No cart/order.
- **Checkout precondition:** the ticket must already be `Reserved` by the acting caller; an existing active `Pending` payment for that ticket is not allowed.
- **Acting user identity — request body + JWT guard.** The acting user id comes from the request body, matching the existing controller convention. In addition, the authorized `checkout` endpoint cross-checks the body `UserId` against the user id extracted from the authenticated JWT claim; on mismatch the request fails (no command executes). A small claims-extraction helper reads the JWT's user id (infrastructure folder containing JwtService). The webhook endpoint is signed-only and has no claims.
- **Stripe integration:** official `Stripe.net` SDK behind a service abstraction `IPaymentsService` (interface + impl), mirroring the email-sender abstraction. `StripeSettings` holds `SecretKey`, `WebhookSecret`, `SuccessUrl`, `CancelUrl`, `Currency`, bound to the Stripe configuration section. The `Stripe.net` package must be added (not currently referenced).
- **Webhook matching:** `client_reference_id = PaymentId` on the session; the webhook maps the Stripe event back to the stored payment via that id.
- **Endpoints:**
  - `POST create-checkout` — requires prior reservation, creates the `Pending` payment, calls the service to create the session, persists the session id, returns `{ CheckoutUrl, SessionId, PaymentId }`. Success/cancel URLs come from settings.
  - `POST webhook` — anonymous, reads the raw body + `Stripe-Signature` header, verifies via Stripe signature, dispatches the event as a CQRS command, and branches on event type:
    - completion event → mark the payment `Completed` and confirm the ticket (reserved → confirmed, owned by the payment's user); no-op if already completed.
    - failure/expiry event types → mark the payment `Failed` or `Expired` and release the reservation; no-op if already terminal.
    - unknown event types → no-op.
  - Returns success except on signature-verification failure (HTTP 400).
- **Repository:** `IPaymentRepository` mirrors the existing ticket repository method set, and registers on `IUnitOfWork`. All lookups (webhook by id, checkout by ticket, job by predicate) use the predicate find.
- **Money conversion:** decimal amount → Stripe minor units is handled inside the payment service impl; currency read from settings.
- **Jobs:**
  - New `ExpiredPaymentsCleanupJob` marks stale `Pending` payments `Expired` and releases their tickets.
  - The existing reservation-expiry job gains a guard to skip tickets that have an active `Pending` payment, so an in-flight payment isn't invalidated.
- **Scope:** exactly the two endpoints; no read/query endpoint for payments yet.

## Implementation Decisions

- CQRS disposed of via command/handlers and a custom dispatcher returning `Result`, mirroring the ticket commands.
- New domain errors reused from the existing set where possible (`UnauthorizedUserError`, not-available); add payment-not-found/duplicate errors as needed.
- The webhook handler is a CQRS command-handler pair (idempotent no-op on re-delivery and unknown types), so Stripe callers are decoupled from the response body.
- raw body read, signature verification, currency, and minor-unit conversion all live in the service; handler/controller pass the money.

## Testing Decisions

- Good tests assert external behavior — state transitions, raised domain events, and resulting persisted state — not internal implementation details.
- **Payment entity tests** assert state transitions (Pending→Completed/Failed/Expired) and raised domain events; invalid transition throws. Prior art: ticket release-reservation tests.
- **Checkout handler tests** — real `ServiceProvider`, InMemory DB, mocked payment service and mocked domain-event dispatcher; assert a payment is created and persisted, the session is created through the service, failure paths (not-reserved, existing active payment), and the body-vs-JWT mismatch returning an unauthorized error with no payment persisted. Prior art: reservation/authentication handler tests.
- **Webhook handler tests** — completed event confirms the ticket and raises `TicketConfirmedEvent`; failure event releases the reservation; re-delivered event is a no-op; unknown event is a no-op.
- **Job tests** — `ExpiredPaymentsCleanupJob` marks stale `Pending` → `Expired` and releases; the reservation-expiry job skips a reserved ticket holding an active pending payment.
- Webhook signature verification itself is exercised at the integration level, not unit-tested, in line with existing repo conventions.

## Out of Scope

- A read/query endpoint for payments (get by id / list) — deferred.
- Client-side verify or status polling.
- Webhook events beyond completion and failure types.
- Local card tokenization, refunds, partial/needs-payment flows beyond checkout sessions.
- Cart/multi-ticket sessions.
- Payment portal/dashboard.

## Further Notes

- Single-class-library Web project (`net10.0`) with Application/ Domain / Infrastructure / API layers; the `Stripe.net` package is not yet referenced.
- The successful payment must also confirm the ticket so ticket state and payment state stay in sync; the failed path releases the ticket reservation.