# 07 — Add AddUserHandler duplicate-email test

**What to build:** Write a new test verifying that creating a user with an already-registered email returns UserAlreadyExistsError as a failure result.

**Blocked by:** 04 — Unify result pattern in Users module

**Status:** ready-for-agent

- [ ] Create AddUserHandler_duplicate_email_returns_error test: seed a user, attempt to create another with same email, assert result.IsFailure and error type
