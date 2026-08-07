# 02 — Serialize DateTimeRange as JSON in persistence

**What to build:** DateTimeRange is stored and loaded as a JSON object
(`{"startTime":"...","endTime":"..."}`) through the ApplicationDbContext value
converter, replacing the current pipe-delimited string format and reusing the
shared `JsonSerializerOptions` introduced in ticket 01. The DateTimeRange parse
helper becomes a `JsonSerializer.Deserialize<DateTimeRange>` call; all
pipe-splitting logic is removed (clean switch, no legacy fallback).

**Blocked by:** 01 — Serialize Money as JSON in persistence

**Status:** ready-for-agent

- [ ] DateTimeRange round-trips losslessly: a saved value reads back equal after a fresh read from the context
- [ ] Stored format is JSON with camelCase property names
- [ ] All legacy pipe-delimited parsing logic removed
- [ ] Existing test suite still passes
