# 07 — Tests

**What to build:** Handler-level tests. For `SendAuthenticationCodeHandler`: test success (code saved + event fired), email not found, rate-limited. For `GenerateTokenHandler`: test success (JWT returned + code used + Login called), invalid code, expired code, already-used code. Use the existing test project patterns.

**Blocked by:** 02 — POST /api/Authentication/send-code endpoint, 04 — POST /api/Authentication/token endpoint

**Status:** ready-for-agent

- [ ] `SendAuthenticationCodeHandler` tests pass
- [ ] `GenerateTokenHandler` tests pass
