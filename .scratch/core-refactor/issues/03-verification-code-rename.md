# 03 — VerificationCode rename (cascade)

**What to build:** The authentication code entity and its repository are renamed from `EmailVerificationCode` to `VerificationCode` so the domain vocabulary doesn't imply a delivery channel (email today, possibly SMS later). The rename cascades across every dependent symbol and call site.

**Blocked by:** 02 — shared auth-code handler file; keep the handler-constant work merged before this mechanical rename.

**Status:** ready-for-agent

- [ ] Entity, id, repository interface + implementation, EF DbSet + converter/config, DI registration, and all handlers/call sites use the `VerificationCode` naming.
- [ ] `AuthenticationCodeGeneratedEvent` keeps its name (it describes the event, not the entity).
- [ ] `dotnet build` and all tests pass.
