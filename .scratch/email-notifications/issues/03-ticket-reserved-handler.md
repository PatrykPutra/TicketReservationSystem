# 03 — TicketReservedEventHandler: reservation confirmation email

**What to build:** After a user reserves a ticket, they receive a confirmation email with the event name, seat number, and reservation time. The handler listens for `TicketReservedEvent`, resolves recipient email + display data through `IUnitOfWork` lookups, and sends via `IEmailSender`. Missing lookup targets no-op quietly; sender exceptions are caught and logged. Registers explicitly in DI.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] Confirmation email sent with resolved user email as recipient
- [ ] Body contains event name, seat number, reserved-at
- [ ] Missing user/ticket/event quietly no-ops
- [ ] Sender exception is swallowed and logged
- [ ] Registered explicitly in DI
- [ ] Tests: recipient/body with seeded data; quiet no-op; swallowed sender exception
