# 03 — MimeKit email sender + event handler

**What to build:** Create `IEmailSender` interface with `SendAsync(to, subject, body)`. Implement with MimeKit + MailKit, configured from SMTP settings in `appsettings.json`. Create domain event handler `SendAuthenticationCodeEmailHandler` that listens for `AuthenticationCodeGeneratedEvent` and calls the email sender with the 6-digit code as the body. Register everything in DI.

**Blocked by:** 02 — POST /api/Authentication/send-code endpoint

**Status:** ready-for-agent

- [ ] SMTP config section added to `appsettings.json` + `appsettings.Development.json`
- [ ] `IEmailSender` interface created
- [ ] `MimeKitEmailSender` implementation created
- [ ] `SendAuthenticationCodeEmailHandler` created
- [ ] All registered in DI
- [ ] Email sends correctly on hit of send-code endpoint
