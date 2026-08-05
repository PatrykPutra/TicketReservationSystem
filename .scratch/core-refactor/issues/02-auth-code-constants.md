# 02 — Named constants for authentication code flow

**What to build:** The send-authentication-code handler uses named constants instead of magic numbers for the rate-limit window, the generated code range, and the code lifetime, and the emailed expiry message stays consistent with the same lifetime.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] Rate-limit window (60s), code range (100000–999999), and code lifetime (5 min) are declared as private constants in the handler; no magic numbers remain in the flow.
- [ ] The email body's "expires in …" sentence is derived from the same lifetime constant so the two can never drift apart.
- [ ] `dotnet build` and existing tests pass.
