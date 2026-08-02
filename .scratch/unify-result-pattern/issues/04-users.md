# 04 — Unify result pattern in Users module + duplicate-email guard

**What to build:** Refactor AddUserResult to inherit Result<AddUserResponse>. Add duplicate-email guard to AddUserHandler. Refactor UserController error mapping.

**Blocked by:** 01 — Enable Result subclassing + add Error types + DTO records

**Status:** ready-for-agent

- [ ] AddUserResult : Result<AddUserResponse> — success/error constructors
- [ ] Refactor AddUserHandler — query by email first, return UserAlreadyExistsError if found
- [ ] Refactor UserController — add UserAlreadyExistsError => Conflict() to error switch
