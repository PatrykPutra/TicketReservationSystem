# 02 — Add `ReleaseReservation()` to Ticket entity

**What to build:** A new `ReleaseReservation()` method on the `Ticket` domain entity that system-initiated expiration cleanup can call. It guards that the ticket is in `Reserved` status, sets `Status = Available`, clears `UserId`/`ReservedAt`/`ConfirmedAt`, and fires the existing `TicketReleasedEvent` with no `UserId`. Domain unit tests verify: status transitions, property clearing, event emission, and that non-Reserved tickets throw.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] Add `ReleaseReservation()` method to `Ticket` — guard, state changes, event emission
- [ ] Write domain unit tests (Seam 1): verify Available->Reserved->Released lifecycle
- [ ] Write domain unit tests: verify exception on wrong status
