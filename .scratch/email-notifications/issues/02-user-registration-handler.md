# 02 — UserRegistrationEventHandler: welcome email

**What to build:** After a user registers, they receive a plain-text welcome email. The handler listens for `UserRegisteredEvent`, uses the email carried on the event (no lookup — the user row is not yet committed at dispatch time), and sends via `IEmailSender`. A throwing email sender is caught and logged so registration is never failed by an SMTP error. Registers explicitly in DI like the existing auth-code handler. Establishes the handler + DI + test pattern the other email handlers follow.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] Welcome email sent with the event's email as recipient
- [ ] Subject/body contain the expected copy
- [ ] Sender exception is swallowed and logged, registration unaffected
- [ ] Registered explicitly in DI
- [ ] Tests: correct recipient/body; swallowed sender exception
