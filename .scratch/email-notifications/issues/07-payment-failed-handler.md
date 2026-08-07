# 07 — PaymentFailedEventHandler: payment failure email

**What to build:** After a payment fails, the user receives an email with the failed amount and ticket reference. The handler listens for `PaymentFailedEvent`, resolves recipient email + payment amount through `IUnitOfWork` lookups, and sends via `IEmailSender`. Missing lookup targets no-op quietly; sender exceptions are caught and logged. Registers explicitly in DI.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] Payment-failure email sent with resolved user email as recipient
- [ ] Body contains the payment amount and ticket reference
- [ ] Missing user/payment quietly no-ops
- [ ] Sender exception is swallowed and logged
- [ ] Registered explicitly in DI
- [ ] Tests: recipient/body with seeded data; quiet no-op; swallowed sender exception
