# 01 — Enable Result subclassing + add Error types + DTO records

**What to build:** Make Result<T> constructors protected so XxxResult subclasses can call them. Add four new Error records (InvalidCredentialsError, RateLimitedError, UserNotFoundError, UserAlreadyExistsError). Create 7 DTO records for command response payloads.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] Change Result<T>.ctor(T) and Result<T>.ctor(Error) from private to protected
- [ ] Add InvalidCredentialsError, RateLimitedError, UserNotFoundError, UserAlreadyExistsError records to Error.cs
- [ ] Create AuthenticationResponse record
- [ ] Create TokenResponse record
- [ ] Create SendAuthenticationCodeResponse record
- [ ] Create TicketReservationResponse record
- [ ] Create TicketConfirmationResponse record
- [ ] Create TicketCancelationResponse record
- [ ] Create AddUserResponse record
