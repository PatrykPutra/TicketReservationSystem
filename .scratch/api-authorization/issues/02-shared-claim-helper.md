# 02 — Shared JWT userId claim helper

**What to build:** A single helper on the `ClaimsPrincipal` that exposes the authenticated user id from the JWT, used by every controller. It reads the `ClaimTypes.NameIdentifier` claim first and falls back to the raw `sub` claim, because the default inbound claim mapping remaps `sub` to the long nameidentifier URI. A missing, malformed, or non-GUID claim means "no authenticated user id".

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] A `ClaimsPrincipal` extension returns the authenticated user id as a GUID.
- [ ] It resolves the id from a `ClaimTypes.NameIdentifier` claim and falls back to a raw `sub` claim.
- [ ] Unit tests cover: NameIdentifier claim present, `sub`-only claim, non-GUID value, and missing claim.
- [ ] `dotnet build` and the test suite pass.
