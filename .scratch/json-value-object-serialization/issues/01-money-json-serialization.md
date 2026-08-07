# 01 — Serialize Money as JSON in persistence

**What to build:** Money is stored and loaded as a JSON object
(`{"amount":150,"currency":"PLN"}`) through the ApplicationDbContext value
converter, replacing the current pipe-delimited string format. Introduce the
shared `JsonSerializerOptions` (configured with `JsonSerializerDefaults.Web` —
camelCase, case-insensitive reads) that both this converter and the
DateTimeRange converter will use. The Money parse helper becomes a
`JsonSerializer.Deserialize<Money>` call; no legacy pipe-format fallback.

**Blocked by:** None — can start immediately

**Status:** ready-for-agent

- [ ] Money round-trips losslessly: a saved value reads back equal after a fresh read from the context
- [ ] Stored format is JSON with camelCase property names
- [ ] Shared JsonSerializerOptions introduced for reuse by the DateTimeRange converter
- [ ] Existing test suite still passes
