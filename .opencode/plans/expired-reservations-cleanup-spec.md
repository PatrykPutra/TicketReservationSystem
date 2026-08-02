# Expired Reservations Cleanup — Spec

## Problem Statement

When a user reserves a ticket but does not confirm it within the reservation window, the ticket remains in `Reserved` status indefinitely — blocking other users from purchasing it. There is no automated mechanism to release expired reservations and return those tickets to the `Available` pool.

## Solution

A Quartz-scheduled background job runs every 10 minutes, queries all tickets in `Reserved` status whose `ReservedAt` timestamp exceeds a configurable threshold, and releases them back to `Available`. The release uses a new domain method `ReleaseReservation()` on the `Ticket` entity that does not require a userId (system-initiated release). Optimistic concurrency (via the existing `Version` row version) prevents race conditions if a user confirms the ticket at the same moment the job releases it.

## User Stories

1. As a ticket buyer, I want expired reservations to be automatically released, so that I can purchase tickets that were abandoned by other users.
2. As a system operator, I want the release interval to be configurable, so that I can adjust the reservation hold time without redeploying.
3. As a system operator, I want the cleanup to handle concurrent confirmations gracefully, so that no tickets are double-released or corrupted.
4. As a developer, I want a domain method that distinguishes system release from user cancellation, so that domain events accurately reflect the source of the state change.
5. As a developer, I want to query tickets by an arbitrary predicate in the repository, so that I can find expired reservations without coupling the repository to the domain logic.

## Implementation Decisions

- **Domain:** Add `ReleaseReservation()` to `Ticket` — guards `Reserved` status, clears `UserId`/`ReservedAt`/`ConfirmedAt`, sets `Status = Available`, fires existing `TicketReleasedEvent`. No `UserId` param, no auth check.
- **Repository:** Add `FindAsync(Expression<Func<Ticket, bool>> predicate, CancellationToken cancellationToken)` to `ITicketRepository` / `TicketRepository` — general-purpose filter that includes `SocialEvent` navigation property.
- **Scheduling:** Use `Quartz.Extensions.Hosting`. Register `ExpiredReservationsCleanupJob` with a `SimpleSchedule` firing every 10 minutes. Register via `AddQuartzHostedService` which runs inside the ASP.NET Core lifecycle.
- **Concurrency:** Each ticket release is individually wrapped — on `DbUpdateConcurrencyException`, log a warning and skip to the next ticket. The existing `byte[] Version` row version (already configured in `ApplicationDbContext`) enables EF Core optimistic concurrency.
- **Package:** Add `Quartz.Extensions.Hosting` (brings in `Quartz` core).
- **Architecture layer:** The job lives in `Infrastructure` (new `Jobs/` subfolder). Registration via `DependencyInjection.cs`.
- **Testing — Seam 1 (domain unit):** Instantiate a `Ticket`, call `ReleaseReservation()`, assert status is `Available`, properties are cleared, domain event is present. No infrastructure needed.
- **Testing — Seam 2 (job integration):** Seed `ApplicationDbContext` with reserved tickets, instantiate the job, run it, assert tickets are released. Uses the same in-memory provider as the application.

## Testing Decisions

- A good test asserts **external behavior**, not implementation details: status transitions, event emission, and property changes.
- **Seam 1 (domain)** is the primary testing surface — it validates the core logic. Pure unit test, no mocks needed.
- **Seam 2 (job integration)** validates that the query + loop + concurrency handling work end-to-end. It exercises the actual `ITicketRepository` and `IUnitOfWork` against the in-memory EF Core provider already used by the app.
- No existing test files exist in the project — these will be the first tests. A `tests/` directory (or xunit project) will be created alongside the source.

## Out of Scope

- Email or push notifications when a reservation expires.
- Soft-delete or audit log for expired reservations.
- Dashboard/UI for operators to view or manually release expired reservations.
- User-facing countdown timer showing remaining reservation time.
- Hanging off the event bus (e.g. `TicketReleasedEvent` handlers) — the event fires but no additional side-effects are implemented now.

## Further Notes

- The `TicketReleasedEvent` already existed in the codebase with a nullable `UserId` parameter — it aligns perfectly with system-initiated release (pass `null`).
- The `ReservedAt` timestamp is already set by `Ticket.Reserve()`. No schema changes needed.
- The Quartz schedule is idempotent — running it more frequently than 10 minutes is safe (only tickets past the threshold are affected).