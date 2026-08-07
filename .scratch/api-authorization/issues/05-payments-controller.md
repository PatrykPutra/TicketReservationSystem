# 05 — PaymentsController: adopt shared claim helper

**What to build:** The checkout action reads the authenticated user id through the shared claim helper instead of its inline raw-`sub` lookup. This makes the claim-check pattern consistent across controllers and fixes the latent bug where the raw `sub` claim is never found under the default inbound claim mapping (which caused every checkout to fail with 401).

**Blocked by:** 01 — Extract Errors layer under Application, 02 — Shared JWT userId claim helper.

**Status:** ready-for-agent

- [ ] `CreateCheckout` uses the shared claim helper to resolve the authenticated user id.
- [ ] `CreateCheckout` returns 401 Unauthorized when the JWT claim is missing/malformed or differs from the body `UserId`.
- [ ] Matching requests proceed to checkout successfully (the latent always-401 behavior is fixed).
- [ ] Controller tests cover the 401 and success paths.
- [ ] `dotnet build` and the full test suite pass.
