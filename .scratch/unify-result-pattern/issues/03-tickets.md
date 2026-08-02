# 03 — Unify result pattern in Tickets module

**What to build:** Refactor TicketReservationResult, TicketConfirmationResult, TicketCancelationResult to inherit Result<TPayload>. Replace exception-based error flow with Result.Failure(). Refactor TicketsController to check esult.IsFailure once.

**Blocked by:** 01 — Enable Result subclassing + add Error types + DTO records

**Status:** ready-for-agent

- [ ] TicketReservationResult : Result<TicketReservationResponse> — success/error constructors
- [ ] TicketConfirmationResult : Result<TicketConfirmationResponse> — success/error constructors
- [ ] TicketCancelationResult : Result<TicketCancelationResponse> — success/error constructors
- [ ] Refactor TicketReservationHandler — check preconditions, return Result.Failure instead of 	hrow
- [ ] Refactor TicketConfirmationHandler — check preconditions, return Result.Failure instead of 	hrow
- [ ] Refactor TicketCancelationHandler — check preconditions, return Result.Failure instead of 	hrow
- [ ] Refactor TicketsController — ErrorToActionResult handles existing errors, single IsFailure check
