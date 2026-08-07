# 03 — TicketsController: anonymous catalog + reserve/cancel identity checks

**What to build:** Ticket catalog browsing (`GetTicketByEvent` and `GetTicketById`) becomes publicly accessible without authentication, while `Reserve`/`Cancel` stay authenticated and only operate on the caller's own identity. `Reserve`/`Cancel` reject a request whose body `TicketId` differs from the URL path with 400, and reject a request whose body `UserId` differs from the JWT claim (or whose claim is missing/malformed) with 401.

**Blocked by:** 01 — Extract Errors layer under Application, 02 — Shared JWT userId claim helper.

**Status:** ready-for-agent

- [ ] `GetTicketByEvent` and `GetTicketById` are browsable anonymously; `Reserve`/`Cancel` remain authenticated.
- [ ] `Reserve` returns 400 Bad Request when the body `TicketId` differs from the path `ticketId`.
- [ ] `Reserve` returns 401 Unauthorized when the JWT claim is missing/malformed or differs from the body `UserId`.
- [ ] `Cancel` has the same 400/401 behavior as `Reserve`.
- [ ] Matching requests reserve/cancel successfully as before.
- [ ] Controller tests cover the 400, 401, and success paths, and the AllowAnonymous attribute presence is asserted by reflection.
- [ ] `dotnet build` and the full test suite pass.
