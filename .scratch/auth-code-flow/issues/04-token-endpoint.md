# 04 — POST /api/Authentication/token endpoint

**What to build:** Add `POST /api/Authentication/token`. Accept email + code in request body. Look up user (404 if not found). Find matching `EmailVerificationCode` (valid: matches email + code + not expired + not used → 401 if not found). Mark code as used. Call `user.Login()`. Generate JWT with `sub` (UserId) and `email` claims, signed with HMAC key from config. Return `{ token, expiresAt }`.

**Blocked by:** 01 — Foundation

**Status:** ready-for-agent

- [ ] JWT config section added to `appsettings.json` + `appsettings.Development.json`
- [ ] JWT generation service created
- [ ] Request DTO (`AuthenticationTokenRequest`) created
- [ ] `GenerateTokenCommand` + `GenerateTokenHandler` created
- [ ] Controller action added
- [ ] Returns `{ token, expiresAt }` on success, 401 on failure
