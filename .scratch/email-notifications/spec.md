Status: ready-for-agent
Type: task

# Spec: Email Domain Event Handlers

## Problem Statement

Users receive only one email today: the 6-digit authentication code. After that, every meaningful transaction — registering, reserving a ticket, confirming, cancelling, releasing, or paying — happens silently. A user who reserves a ticket and pays has no record of it; a user whose reservation expires has no idea why the seat vanished. The domain already raises the right events (`UserRegisteredEvent`, `TicketReservedEvent`, `TicketReleasedEvent`, `TicketConfirmedEvent`, `TicketCanceledEvent`, `PaymentFailedEvent`, `PaymentCompletedEvent`), but no handler consumes them to notify users.

## Solution

Add seven `IDomainEventHandler<T>` implementations in the Application layer, one per existing domain event, each sending a plain-text email through the existing `IEmailSender` abstraction. Recipient and display data (event name, seat number, price, amount, timestamps) are resolved at handle time through `IUnitOfWork` lookups — except for `UserRegistrationEventHandler`, which uses the email already carried on `UserRegisteredEvent` (the user row is not yet committed when the event dispatches). A fix to `Ticket.ReleaseReservation()` makes the released ticket's owner available to its handler. Email delivery is best-effort: a throwing email sender is caught and logged so it never fails the enclosing unit of work.

## User Stories

1. As a user, I want to receive a welcome email after registering, so that I know my account is active.
2. As a user, I want to receive a confirmation email after reserving a ticket, so that I have a record of the reservation with the event and seat.
3. As a user, I want to receive a cancellation email when my reservation is released (expires or is released by the system), so that I know my seat is no longer held.
4. As a user, I want to receive a confirmation email after my ticket is confirmed, so that I know my purchase is final.
5. As a user, I want to receive a cancellation email when I cancel my ticket, so that I have confirmation the cancellation went through.
6. As a user, I want to receive an email when my payment fails, so that I know the amount and can retry.
7. As a user, I want to receive an email when my payment completes, so that I have a receipt of the amount paid.
8. As a system, I want email sending to be best-effort, so that an SMTP outage never fails a booking or payment.
9. As a developer, I want handlers registered explicitly in DI like the existing auth-code handler, so that the wiring is discoverable.

## Implementation Decisions

- **Handler placement:** seven new classes in the Application layer's domain-event-handler area, alongside `SendAuthenticationCodeEmailHandler`, implementing `IDomainEventHandler<T>`.
- **Recipient/display resolution:** handlers inject `IUnitOfWork` and resolve the recipient email, event name, seat number, price, and payment amount by ID from already-committed rows. Events themselves stay lean (IDs only); no event payload enrichment except the one fix below.
- **Release fix:** `Ticket.ReleaseReservation()` captures `UserId` *before* clearing it and passes it to `TicketReleasedEvent`; the event's `UserId` becomes required (the optional default is removed). This single change covers both the expiry cleanup job and the Stripe webhook release path. Existing `TicketReleaseReservationTests` remain valid.
- **Registration exception:** `UserRegistrationEventHandler` reads `domainEvent.Email` directly; no lookup, because dispatch happens before `SaveChanges` commits the new user.
- **Email format:** inline plain-text subject/body per handler, built with interpolated data — same style as the auth-code handler. No template system.
- **Failure handling:** each handler wraps `IEmailSender.SendAsync` in a try/catch that logs via `ILogger<>` and swallows, so a throwing email never propagates into `ApplicationDbContext.SaveChangesAsync`.
- **DI registration:** explicit registrations in Infrastructure `DependencyInjection` beside the existing auth-code handler registration; `ILogger<>` resolves via the container.
- **Lookups that can be null:** handlers no-op quietly when a referenced user, ticket, event, or payment cannot be found rather than throwing.

## Testing Decisions

- Good tests assert external behavior: the exact recipient, subject, and body content passed to the email sender, and that a throwing sender does not propagate.
- **Module under test:** the seven handlers in the Application layer.
- **Seam:** handler-level — each test constructs the handler with a real `ServiceProvider` + InMemory DB (for lookups) and a mocked `IEmailSender`, then calls `Handle(domainEvent)` directly. No dispatcher/DbContext plumbing.
- **Prior art:** `AuthenticationHandlerTests` (real `ServiceProvider`, InMemory DB, Moq for seams). New test files per handler under `TicketReservationSystem.Tests`.
- **Coverage per handler:** recipient/body correctness with seeded data; quiet no-op when a lookup target is missing; exception swallowed when the email sender throws.

## Out of Scope

- HTML email or template system (stays plain text).
- Email retry/outbox/queueing; delivery is fire-and-forget.
- Changing what data existing domain events carry beyond the `TicketReleasedEvent.UserId` fix.
- Adding emails for events not in the list (`EmailVerifiedEvent`, `PaymentExpiredEvent`, `AuthenticationCodeGeneratedEvent` already handled).
- The Todo.txt items about test-naming conventions and wider coverage.
- SMTP provider work or email configuration changes.

## Further Notes

- Draft subjects/bodies (to be confirmed at implementation): registration → "Welcome to TicketReservationSystem"; reserved → "Ticket reserved" (event, seat, reserved-at); released → "Reservation released"; confirmed → "Ticket confirmed" (event, seat); cancelled → "Ticket cancelled" (event, seat); payment failed → "Payment failed" (amount, ticket); payment completed → "Payment completed" (amount).
- All emails go through the existing `IEmailSender` (MimeKit impl), so no new infrastructure.
