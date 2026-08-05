# 06 — Move Authentication + DomainEventHandlers to Application

**What to build:** JWT/Authentication infrastructure and the auth-code email domain event handler live in the Application layer, and the email sender abstraction moves with them, so Application no longer depends on Infrastructure types.

**Blocked by:** 05, 04 — the moved files reference the Services namespaces and the Program wiring that 05/04 also touch; keep those merges first.

**Status:** ready-for-agent

- [ ] Authentication (JWT service, settings, interface) moves to the Application layer; `GenerateTokenHandler` compiles without any Infrastructure reference.
- [ ] The auth-code email domain event handler moves to Application.
- [ ] The `IEmailSender` abstraction moves to Application.Abstractions; the MailKit implementation and `EmailSettings` stay in Infrastructure Services.
- [ ] All usings and dependency registrations updated.
- [ ] `dotnet build` and all tests pass.
