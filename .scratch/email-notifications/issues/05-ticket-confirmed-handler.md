# 05 — TicketConfirmedEventHandler: confirmation email

**What to build:** After a user's ticket is confirmed, they receive a confirmation email with the event name and seat number. The handler listens for `TicketConfirmedEvent`, resolves recipient email + display data through `IUnitOfWork` lookups, and sends via `IEmailSender`. Missing lookup targets no-op quietly; sender exceptions are caught and logged. Registers explicitly in DI.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] Confirmation email sent with resolved user email as recipient
- [ ] Body contains event name and seat number
- [ ] Missing user/ticket/event quietly no-ops
- [ ] Sender exception is swallowed and logged
- [ ] Registered explicitly in DI
- [ ] Tests: recipient/body with seeded data; quiet no-op; swallowed sender exception
