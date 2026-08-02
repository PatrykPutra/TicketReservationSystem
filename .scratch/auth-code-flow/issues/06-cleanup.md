# 06 — Cleanup old endpoint + seeder

**What to build:** Remove the old `POST /api/Authentication` endpoint (or mark as deprecated). Remove `AuthenticationCommand`, `AuthenticationHandler`, `AuthenticationResult` if no longer referenced. Update `InMemorySeeder` to not call `VerifyEmail()` (verification is now done via the code flow).

**Blocked by:** 02 — POST /api/Authentication/send-code endpoint, 04 — POST /api/Authentication/token endpoint

**Status:** ready-for-agent

- [ ] Old `POST /api/Authentication` removed
- [ ] Old command/result classes removed if unreferenced
- [ ] `InMemorySeeder` updated
