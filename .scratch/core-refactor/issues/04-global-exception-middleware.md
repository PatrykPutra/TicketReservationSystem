# 04 — Global exception middleware

**What to build:** Any unhandled exception is caught by a single global middleware, logged, and converted into a generic HTTP 500 problem response — clients never see exception details and every unexpected failure looks identical.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] Middleware catches all unhandled exceptions, logs them, and returns 500 with a generic "An unexpected error occurred" ProblemDetails body.
- [ ] Registered at the start of the request pipeline (before auth/authorization/controllers).
- [ ] Controllers' existing `Result`-error mapping switches are left unchanged (exceptions-only middleware).
- [ ] `dotnet build` and existing tests pass.
