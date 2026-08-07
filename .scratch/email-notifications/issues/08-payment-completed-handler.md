# 08 — PaymentCompletedEventHandler: payment completion email

**What to build:** After a payment completes, the user receives a receipt-style email with the paid amount. The handler listens for `PaymentCompletedEvent`, resolves recipient email + payment amount through `IUnitOfWork` lookups, and sends via `IEmailSender`. Missing lookup targets no-op quietly; sender exceptions are caught and logged. Registers explicitly in DI.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] Payment-completion email sent with resolved user email as recipient
- [ ] Body contains the payment amount
- [ ] Missing user/payment quietly no-ops
- [ ] Sender exception is swallowed and logged
- [ ] Registered explicitly in DI
- [ ] Tests: recipient/body with seeded data; quiet no-op; swallowed sender exception
