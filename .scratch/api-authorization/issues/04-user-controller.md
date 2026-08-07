# 04 — UserController: GetUser identity check

**What to build:** The profile fetch only returns the caller's own user. `GetUser` returns 401 Unauthorized when the path `userId` differs from the authenticated JWT claim (or the claim is missing/malformed). Matching requests keep the existing behavior: 200 with the user when found, 404 when not found.

**Blocked by:** 01 — Extract Errors layer under Application, 02 — Shared JWT userId claim helper.

**Status:** ready-for-agent

- [ ] `GetUser` returns 401 Unauthorized when the path `userId` differs from the JWT claim, or the claim is missing/malformed.
- [ ] Matching `userId` + user found returns 200 with the user.
- [ ] Matching `userId` + user missing returns 404.
- [ ] Controller tests cover the 401, 200, and 404 paths.
- [ ] `dotnet build` and the full test suite pass.
