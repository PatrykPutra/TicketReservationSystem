# 05 — TicketsController catalog GET tests

**What to build:** Behavioral tests for the anonymous ticket-browsing actions so a ticket
lookup returns 200/404 and per-event listing returns 200 (currently covered only by
attribute-reflection tests).

**Blocked by:** None — can start immediately

**Status:** done

- [x] GetTicketById returns 200 with the ticket when it exists
- [x] GetTicketById returns 404 when the ticket does not exist
- [x] GetTicketByEvent returns 200 with the ticket list
- [x] Existing reserve/cancel/attribute tests remain green
- [x] Test names follow the MethodName_Scenario_ExpectedResult convention
