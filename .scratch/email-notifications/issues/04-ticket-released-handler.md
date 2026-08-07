# 04 — TicketReleasedEventHandler: release cancellation email

**What to build:** When a user's reservation is released (expires or is released by the system), they receive a cancellation email with the event name and seat number. The handler listens for `TicketReleasedEvent`, resolves recipient email + display data through `IUnitOfWork` lookups (recipient known thanks to ticket 01), and sends via `IEmailSender`. Missing lookup targets no-op quietly; sender exceptions are caught and logged. Registers explicitly in DI.

**Blocked by:** 01 — Release fix: TicketReleasedEvent carries the user

**Status:** ready-for-agent

- [ ] Cancellation email sent with resolved user email as recipient
- [ ] Body contains event name and seat number
- [ ] Missing user/ticket/event quietly no-ops
- [ ] Sender exception is swallowed and logged
- [ ] Registered explicitly in DI
- [ ] Tests: recipient/body with seeded data; quiet no-op; swallowed sender exception
