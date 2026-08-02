# 05 — JWT middleware + [Authorize] attributes

**What to build:** Register `AddAuthentication().AddJwtBearer()` in `Program.cs` with validation: issuer, audience, signing key, lifetime. Add `app.UseAuthentication()` before `UseAuthorization()`. Apply `[Authorize]` to endpoints that require authentication (e.g., ticket purchase, user profile).

**Blocked by:** 04 — POST /api/Authentication/token endpoint

**Status:** ready-for-agent

- [ ] JWT bearer middleware configured in `Program.cs`
- [ ] `UseAuthentication()` wired up
- [ ] `[Authorize]` applied to relevant controllers/actions
- [ ] Secured endpoint returns 401 without token, 200 with valid token
