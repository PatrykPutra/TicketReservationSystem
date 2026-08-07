# 06 — TicketCanceledEventHandler: cancellation email

**What to build:** After a user cancels their ticket, they receive a cancellation email with the event name and seat number. The handler listens for `TicketCanceledEvent`, resolves recipient email + display data through `IUnitOfWork` lookups, and sends via `IEmailSender`. Missing lookup targets no-op quietly; sender exceptions are caught and logged. Registers explicitly in DI.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] Cancellation email sent with resolved user email as recipient
- [ ] Body contains event name and seat number
- [ ] Missing user/ticket/event quietly no-ops
- [ ] Sender exception is swallowed and logged
- [ ] Registered explicitly in DI
- [ ] Tests: recipient/body with seeded data; quiet no-op; swallowed sender exception
