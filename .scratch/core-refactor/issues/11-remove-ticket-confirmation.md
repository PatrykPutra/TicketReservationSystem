# 11 — Remove standalone ticket confirmation

**What to build:** Ticket confirmation is reachable only through payment completion. The public confirm endpoint and its CQRS pipeline are removed, while the domain confirmation behavior used by the webhook flow remains.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] The `POST {ticketId}/confirm` endpoint and the full confirmation CQRS pipeline (command, handler, result, response, request) are deleted.
- [ ] `Ticket.Confirm()` and `TicketConfirmedEvent` remain — the webhook flow still confirms tickets on payment completion.
- [ ] The tickets controller error switch is cleaned of branches no longer reachable.
- [ ] `dotnet build` and all tests pass.
