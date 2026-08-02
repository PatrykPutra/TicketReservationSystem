# 02 — POST /api/Authentication/send-code endpoint

**What to build:** Add a new `POST /api/Authentication/send-code` endpoint. Accept email in request body. Look up user — return 404 if not found. Check rate limit (any code created <60s ago for this user → 429). Generate random 6-digit code. Save `EmailVerificationCode`. Fire `AuthenticationCodeGeneratedEvent`. Return 200.

**Blocked by:** 01 — Foundation

**Status:** ready-for-agent

- [ ] Request DTO (`AuthenticationCodeRequest`) created
- [ ] `SendAuthenticationCodeCommand` + `SendAuthenticationCodeHandler` created
- [ ] Rate-limit logic implemented (reject if code exists from <60s ago)
- [ ] 6-digit code generation
- [ ] Controller action added
- [ ] Returns 200 on success, 404 if email not found, 429 if rate-limited
