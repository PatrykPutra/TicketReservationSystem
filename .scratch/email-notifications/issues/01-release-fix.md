# 01 — Release fix: TicketReleasedEvent carries the user

**What to build:** When a ticket reservation is released (expiry cleanup job or Stripe webhook), the user who held the reservation is now identifiable by the email handler. `Ticket.ReleaseReservation()` captures the owner `UserId` before clearing it and passes it to `TicketReleasedEvent`, whose `UserId` becomes a required constructor parameter.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] `ReleaseReservation()` captures `UserId` before clearing it
- [ ] `TicketReleasedEvent` constructor requires a non-null `UserId`
- [ ] Existing ticket release-reservation tests still pass
- [ ] `dotnet build` and full test suite pass
